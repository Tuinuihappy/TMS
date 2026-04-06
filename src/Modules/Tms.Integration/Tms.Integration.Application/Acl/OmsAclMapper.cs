using System.Text.Json;
using Tms.Integration.Domain.Entities;
using Tms.Orders.Application.Features.CreateOrder;

namespace Tms.Integration.Application.Acl;

/// <summary>
/// Anti-Corruption Layer สำหรับ OMS.
/// แปลง OMS raw JSON payload → CreateOrderCommand โดยใช้ Field Mapping ที่ Admin ตั้งค่าไว้.
/// </summary>
public sealed class OmsAclMapper
{
    /// <summary>
    /// แปลง Raw OMS payload เป็น CreateOrderCommand โดยใช้ Field Mappings.
    /// หาก mapping ไม่ครบ throws InvalidOperationException พร้อมชื่อ field ที่หายไป.
    /// </summary>
    public CreateOrderCommand Map(string rawPayload, IList<OmsFieldMapping> mappings)
    {
        using var doc = JsonDocument.Parse(rawPayload);
        var root = doc.RootElement;

        string? GetValue(string omsField)
        {
            // รองรับ simple path เช่น "order_id" หรือ "customer.code"
            var parts = omsField.Split('.');
            JsonElement current = root;
            foreach (var part in parts)
            {
                if (!current.TryGetProperty(part, out current))
                    return null;
            }
            return current.ValueKind == JsonValueKind.Null ? null : current.ToString();
        }

        string Require(string tmsField)
        {
            var mapping = mappings.FirstOrDefault(m => m.TmsField == tmsField)
                ?? throw new InvalidOperationException($"No mapping configured for TMS field '{tmsField}'.");

            var value = GetValue(mapping.OmsField);
            if (value is null && mapping.IsRequired)
                throw new InvalidOperationException($"Required OMS field '{mapping.OmsField}' (→ {tmsField}) is missing.");

            return value ?? string.Empty;
        }

        string? Optional(string tmsField)
        {
            var mapping = mappings.FirstOrDefault(m => m.TmsField == tmsField);
            if (mapping is null) return null;
            return GetValue(mapping.OmsField);
        }

        // --- Map required fields ---
        var customerId = Guid.TryParse(Require("customerId"), out var cid) ? cid : Guid.Empty;
        var externalRef = Require("externalRef");

        // --- Build Address DTOs (สำหรับ MVP ใช้ค่า mock ถ้าไม่มี mapping) ---
        var pickupAddress = new AddressDto(
            Optional("pickupAddress.street") ?? "",
            Optional("pickupAddress.subDistrict") ?? "",
            Optional("pickupAddress.district") ?? "",
            Optional("pickupAddress.province") ?? "",
            Optional("pickupAddress.postalCode") ?? "");

        var dropoffAddress = new AddressDto(
            Optional("dropoffAddress.street") ?? "",
            Optional("dropoffAddress.subDistrict") ?? "",
            Optional("dropoffAddress.district") ?? "",
            Optional("dropoffAddress.province") ?? "",
            Optional("dropoffAddress.postalCode") ?? "");

        // --- Items: อ่านจาก array "items[]" ถ้ามี mapping ---
        var items = new List<OrderItemDto>();
        if (root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in itemsEl.EnumerateArray())
            {
                var desc = item.TryGetProperty("description", out var d) ? d.GetString() ?? "Item" : "Item";
                var qty = item.TryGetProperty("qty", out var q) && q.TryGetInt32(out var qi) ? qi : 1;
                var weight = item.TryGetProperty("gross_weight_kg", out var w) && w.TryGetDecimal(out var wd) ? wd : 0m;
                items.Add(new OrderItemDto(desc, weight, 0m, qty));
            }
        }

        if (items.Count == 0)
            items.Add(new OrderItemDto("OMS Import Item", 0m, 0m, 1));

        return new CreateOrderCommand(
            CustomerId: customerId,
            OrderNumber: null,       // TMS generate เอง
            PickupAddress: pickupAddress,
            DropoffAddress: dropoffAddress,
            Items: items,
            Notes: $"OMS Import | ExternalRef: {externalRef}");
    }
}
