using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;
using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Enums;
using Tms.Orders.Domain.Interfaces;
using Tms.Orders.Domain.ValueObjects;
using Tms.SharedKernel.Application;

namespace Tms.Orders.Application.Features.ImportOrder;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record ImportOrdersCommand(
    Stream FileStream,
    string FileName) : ICommand<ImportOrdersResult>;

public sealed record ImportOrdersResult(
    int TotalRows,
    int SuccessCount,
    int FailCount,
    List<ImportRowError> Errors);

public sealed record ImportRowError(int Row, string Message);

// ── CSV Row Mapping ──────────────────────────────────────────────────────────

public sealed class ImportOrderRow
{
    public string? CustomerCode { get; set; }
    public Guid CustomerId { get; set; }
    public string? Priority { get; set; }
    public string? PickupStreet { get; set; }
    public string? PickupDistrict { get; set; }
    public string? PickupProvince { get; set; }
    public string? PickupPostalCode { get; set; }
    public double? PickupLatitude { get; set; }
    public double? PickupLongitude { get; set; }
    public string? DropoffStreet { get; set; }
    public string? DropoffDistrict { get; set; }
    public string? DropoffProvince { get; set; }
    public string? DropoffPostalCode { get; set; }
    public double? DropoffLatitude { get; set; }
    public double? DropoffLongitude { get; set; }
    public string? ItemDescription { get; set; }
    public decimal WeightKg { get; set; }
    public decimal VolumeCBM { get; set; }
    public int Quantity { get; set; } = 1;
}

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class ImportOrderHandler(IOrderRepository orderRepository)
    : ICommandHandler<ImportOrdersCommand, ImportOrdersResult>
{
    public async Task<ImportOrdersResult> Handle(ImportOrdersCommand request, CancellationToken ct)
    {
        var extension = Path.GetExtension(request.FileName)?.ToLowerInvariant();
        var rows = extension switch
        {
            ".csv" => ParseCsv(request.FileStream),
            ".xlsx" => ParseExcel(request.FileStream),
            _ => throw new ArgumentException($"Unsupported file format: {extension}. Use .csv or .xlsx")
        };

        var errors = new List<ImportRowError>();
        int successCount = 0;

        for (int i = 0; i < rows.Count; i++)
        {
            var rowNum = i + 2; // 1-indexed + header row
            try
            {
                var row = rows[i];
                await CreateOrderFromRow(row, ct);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportRowError(rowNum, ex.Message));
            }
        }

        return new ImportOrdersResult(rows.Count, successCount, errors.Count, errors);
    }

    private async Task CreateOrderFromRow(ImportOrderRow row, CancellationToken ct)
    {
        if (row.CustomerId == Guid.Empty)
            throw new ArgumentException("CustomerId is required.");
        if (string.IsNullOrWhiteSpace(row.ItemDescription))
            throw new ArgumentException("ItemDescription is required.");

        var orderNumber = await orderRepository.GenerateOrderNumberAsync(ct);

        var pickup = Address.Create(
            row.PickupStreet ?? "", "", row.PickupDistrict ?? "",
            row.PickupProvince ?? "", row.PickupPostalCode ?? "",
            row.PickupLatitude, row.PickupLongitude);

        var dropoff = Address.Create(
            row.DropoffStreet ?? "", "", row.DropoffDistrict ?? "",
            row.DropoffProvince ?? "", row.DropoffPostalCode ?? "",
            row.DropoffLatitude, row.DropoffLongitude);

        var priority = Enum.TryParse<OrderPriority>(row.Priority, true, out var p)
            ? p : OrderPriority.Normal;

        var order = TransportOrder.Create(
            orderNumber, row.CustomerId, pickup, dropoff, priority);

        var item = OrderItem.Create(
            order.Id, row.ItemDescription!,
            row.WeightKg, row.VolumeCBM, row.Quantity);
        order.AddItem(item);

        await orderRepository.AddAsync(order, ct);
    }

    // ── CSV Parsing ──────────────────────────────────────────────────────────

    private static List<ImportOrderRow> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant()
        });

        return csv.GetRecords<ImportOrderRow>().ToList();
    }

    // ── Excel Parsing ────────────────────────────────────────────────────────

    private static List<ImportOrderRow> ParseExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        var rows = new List<ImportOrderRow>();

        // Read headers from row 1
        var headerRow = worksheet.Row(1);
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int col = 1; col <= headerRow.LastCellUsed()?.Address.ColumnNumber; col++)
        {
            var header = headerRow.Cell(col).GetString().Trim();
            if (!string.IsNullOrEmpty(header))
                headers[header] = col;
        }

        // Read data rows
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int rowIdx = 2; rowIdx <= lastRow; rowIdx++)
        {
            var xlRow = worksheet.Row(rowIdx);
            // Skip completely empty rows
            if (xlRow.IsEmpty()) continue;

            rows.Add(new ImportOrderRow
            {
                CustomerCode = GetCell(xlRow, headers, "CustomerCode"),
                CustomerId = Guid.TryParse(GetCell(xlRow, headers, "CustomerId"), out var cid) ? cid : Guid.Empty,
                Priority = GetCell(xlRow, headers, "Priority"),
                PickupStreet = GetCell(xlRow, headers, "PickupStreet"),
                PickupDistrict = GetCell(xlRow, headers, "PickupDistrict"),
                PickupProvince = GetCell(xlRow, headers, "PickupProvince"),
                PickupPostalCode = GetCell(xlRow, headers, "PickupPostalCode"),
                PickupLatitude = double.TryParse(GetCell(xlRow, headers, "PickupLatitude"), out var plat) ? plat : null,
                PickupLongitude = double.TryParse(GetCell(xlRow, headers, "PickupLongitude"), out var plng) ? plng : null,
                DropoffStreet = GetCell(xlRow, headers, "DropoffStreet"),
                DropoffDistrict = GetCell(xlRow, headers, "DropoffDistrict"),
                DropoffProvince = GetCell(xlRow, headers, "DropoffProvince"),
                DropoffPostalCode = GetCell(xlRow, headers, "DropoffPostalCode"),
                DropoffLatitude = double.TryParse(GetCell(xlRow, headers, "DropoffLatitude"), out var dlat) ? dlat : null,
                DropoffLongitude = double.TryParse(GetCell(xlRow, headers, "DropoffLongitude"), out var dlng) ? dlng : null,
                ItemDescription = GetCell(xlRow, headers, "ItemDescription"),
                WeightKg = decimal.TryParse(GetCell(xlRow, headers, "WeightKg"), out var w) ? w : 0,
                VolumeCBM = decimal.TryParse(GetCell(xlRow, headers, "VolumeCBM"), out var v) ? v : 0,
                Quantity = int.TryParse(GetCell(xlRow, headers, "Quantity"), out var q) ? q : 1
            });
        }

        return rows;
    }

    private static string? GetCell(IXLRow row, Dictionary<string, int> headers, string key)
        => headers.TryGetValue(key, out var col) ? row.Cell(col).GetString()?.Trim() : null;
}
