using System.Text;
using Moq;
using Tms.Orders.Application.Features.ImportOrder;
using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Interfaces;
using Xunit;

namespace Tms.Orders.UnitTests.Application;

public sealed class ImportOrderHandlerTests
{
    private static readonly Guid ValidCustomerId = Guid.Parse("cc43fd47-43b5-4777-8afc-6f9205e90f7b");
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private const string ValidCsvHeader =
        "CustomerCode,CustomerId,Priority,PickupName,PickupStreet,PickupSubDistrict,PickupDistrict," +
        "PickupProvince,PickupPostalCode,PickupLatitude,PickupLongitude," +
        "DropoffName,DropoffStreet,DropoffSubDistrict,DropoffDistrict," +
        "DropoffProvince,DropoffPostalCode,DropoffLatitude,DropoffLongitude," +
        "ItemDescription,WeightKg,VolumeCBM,Quantity";

    private static string ValidRow(Guid? customerId = null, string? description = null) =>
        $"CUST-001,{customerId ?? ValidCustomerId},Normal,คลัง A,1 ถ.สุขุมวิท," +
        $"คลองเตย,คลองเตย,กรุงเทพมหานคร,10110,13.72,100.56," +
        $"ปลายทาง B,2 ถ.พหลโยธิน,จตุจักร,จตุจักร,กรุงเทพมหานคร,10900,13.81,100.55," +
        $"{description ?? "สินค้าทดสอบ"},100.0,1.0,2";

    private static (ImportOrderHandler handler, Mock<IOrderRepository> repoMock) CreateHandler()
    {
        var repoMock = new Mock<IOrderRepository>();
        repoMock.Setup(r => r.GenerateOrderNumberAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("ORD-TEST-0001");
        repoMock.Setup(r => r.AddAsync(It.IsAny<TransportOrder>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var handler = new ImportOrderHandler(repoMock.Object);
        return (handler, repoMock);
    }

    private static Stream ToCsvStream(params string[] rows)
    {
        var csv = string.Join("\n", new[] { ValidCsvHeader }.Concat(rows));
        return new MemoryStream(Encoding.UTF8.GetBytes(csv));
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCsv_AllRowsSucceed()
    {
        var (handler, repoMock) = CreateHandler();
        var stream = ToCsvStream(ValidRow(), ValidRow(), ValidRow());
        var command = new ImportOrdersCommand(stream, "orders.csv", TenantId);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailCount);
        Assert.Empty(result.Errors);
        repoMock.Verify(r => r.AddAsync(It.IsAny<TransportOrder>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_ValidCsv_PassesTenantIdToOrder()
    {
        var (handler, repoMock) = CreateHandler();
        TransportOrder? capturedOrder = null;
        repoMock.Setup(r => r.AddAsync(It.IsAny<TransportOrder>(), It.IsAny<CancellationToken>()))
                .Callback<TransportOrder, CancellationToken>((o, _) => capturedOrder = o)
                .Returns(Task.CompletedTask);

        var stream = ToCsvStream(ValidRow());
        await handler.Handle(new ImportOrdersCommand(stream, "orders.csv", TenantId), CancellationToken.None);

        Assert.NotNull(capturedOrder);
        Assert.Equal(TenantId, capturedOrder!.TenantId);
    }

    // ── Empty / invalid CustomerId ─────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyCustomerId_CapturesPerRowError_DoesNotCrash()
    {
        var (handler, _) = CreateHandler();
        var stream = ToCsvStream(
            ValidRow(),             // row 2 — valid
            ValidRow(Guid.Empty),   // row 3 — empty Guid
            ValidRow());            // row 4 — valid

        var result = await handler.Handle(
            new ImportOrdersCommand(stream, "orders.csv", TenantId), CancellationToken.None);

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(1, result.FailCount);
        Assert.Single(result.Errors);
        Assert.Equal(3, result.Errors[0].Row);
        Assert.Contains("CustomerId", result.Errors[0].Message);
    }

    [Fact]
    public async Task Handle_InvalidGuidFormat_CapturesPerRowError_DoesNotCrash()
    {
        var (handler, _) = CreateHandler();
        // ส่ง string "NOT-A-GUID" แทน Guid — ต้องไม่ throw 500
        var csv = ValidCsvHeader + "\nCUST-BAD,NOT-A-GUID,Normal,A,1 ถ.,,,กรุงเทพ,10110,,,B,2 ถ.,,,กรุงเทพ,10900,,,สินค้า,100,1,1";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await handler.Handle(
            new ImportOrdersCommand(stream, "orders.csv", TenantId), CancellationToken.None);

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailCount);
        Assert.Contains("CustomerId", result.Errors[0].Message);
    }

    // ── Missing required fields ────────────────────────────────────────────

    [Fact]
    public async Task Handle_MissingItemDescription_CapturesPerRowError()
    {
        var (handler, _) = CreateHandler();
        var stream = ToCsvStream(
            ValidRow(),                        // row 2 — success
            ValidRow(description: ""),         // row 3 — fail: no description
            ValidRow());                       // row 4 — success

        var result = await handler.Handle(
            new ImportOrdersCommand(stream, "orders.csv", TenantId), CancellationToken.None);

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(1, result.FailCount);
        Assert.Equal(3, result.Errors[0].Row);
        Assert.Contains("ItemDescription", result.Errors[0].Message);
    }

    // ── Unsupported format ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UnsupportedExtension_ThrowsArgumentException()
    {
        var (handler, _) = CreateHandler();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new ImportOrdersCommand(stream, "orders.pdf", TenantId), CancellationToken.None));
    }

    // ── Mixed success/fail ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MixedRows_ReportsCorrectCounts()
    {
        var (handler, _) = CreateHandler();
        var stream = ToCsvStream(
            ValidRow(),             // success
            ValidRow(Guid.Empty),   // fail — bad CustomerId
            ValidRow(),             // success
            ValidRow(description: ""), // fail — no description
            ValidRow());            // success

        var result = await handler.Handle(
            new ImportOrdersCommand(stream, "orders.csv", TenantId), CancellationToken.None);

        Assert.Equal(5, result.TotalRows);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(2, result.FailCount);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal(3, result.Errors[0].Row); // row 3 (1-indexed + header)
        Assert.Equal(5, result.Errors[1].Row); // row 5
    }
}
