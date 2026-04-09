using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Interfaces;
using Tms.Orders.Domain.ValueObjects;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;

namespace Tms.Orders.Application.Features.SplitOrder;

// ── Manual Split Handler ─────────────────────────────────────────────────────

/// <summary>
/// Manual Split: Planner กำหนด allocation ของ items เองต่อ Part
/// แต่ละ Part สร้าง child order ใหม่ ส่วน parent เปลี่ยน status = PartialSplit
/// </summary>
public sealed class SplitOrderHandler(IOrderRepository repo)
    : ICommandHandler<SplitOrderCommand, SplitOrderResult>
{
    public async Task<SplitOrderResult> Handle(SplitOrderCommand request, CancellationToken ct)
    {
        // 1. Load parent order
        var parent = await repo.GetByIdAsync(request.OrderId, ct)
            ?? throw new NotFoundException(nameof(TransportOrder), request.OrderId);

        // 2. Business rule validation (rest delegated to validator)
        var itemLookup = parent.Items.ToDictionary(i => i.Id);

        // 3. Build child orders
        var children = new List<TransportOrder>();

        for (int i = 0; i < request.Parts.Count; i++)
        {
            var part = request.Parts[i];
            var childNumber = await repo.GenerateOrderNumberAsync(ct);
            var splitReason = request.Reason ?? "Manual";

            // Override dropoff address if provided
            Address? overrideDropoff = null;
            if (part.OverrideDropoffAddress is { } addr)
            {
                overrideDropoff = Address.Create(
                    addr.Street, addr.SubDistrict, addr.District,
                    addr.Province, addr.PostalCode,
                    addr.Latitude, addr.Longitude);
            }

            // Override dropoff window if provided
            TimeWindow? overrideWindow = null;
            if (part.OverrideDropoffWindow is { } win)
                overrideWindow = TimeWindow.Create(win.From, win.To);

            var child = TransportOrder.CreateSplitChild(
                childNumber, parent, splitReason,
                overrideDropoff, overrideWindow,
                part.Notes);

            // Allocate items to child
            foreach (var alloc in part.Items)
            {
                var sourceItem = itemLookup[alloc.ItemId];
                var childItem = OrderItem.Create(
                    child.Id,
                    sourceItem.Description,
                    sourceItem.Weight,
                    sourceItem.Volume,
                    alloc.Quantity,
                    sourceItem.SKU,
                    sourceItem.IsDangerousGoods,
                    sourceItem.UNNumber,
                    sourceItem.DGClass);

                child.AddItemInternal(childItem);
            }

            children.Add(child);
        }

        // 4. Mark parent as split
        var childIds = children.Select(c => c.Id).ToList();
        parent.MarkAsSplit(request.Reason ?? "Manual", childIds, "Manual");

        // 5. Persist atomically: parent update + all children
        await repo.AddRangeAsync(children, ct);
        await repo.UpdateAsync(parent, ct);

        // 6. Build result
        return new SplitOrderResult(
            parent.Id,
            parent.OrderNumber,
            children.Select(c => new SplitChildSummary(
                c.Id, c.OrderNumber,
                c.TotalWeight, c.TotalVolume,
                c.Items.Count)).ToList());
    }
}

// ── Auto Split Handler ────────────────────────────────────────────────────────

/// <summary>
/// Auto Split: ระบบแยกอัตโนมัติโดยใช้ Bin Packing (greedy first-fit)
/// จัดกลุ่ม OrderItems ให้ไม่เกิน MaxWeightPerSplitKg และ MaxVolumePerSplitCBM
/// </summary>
public sealed class AutoSplitOrderHandler(IOrderRepository repo)
    : ICommandHandler<AutoSplitOrderCommand, SplitOrderResult>
{
    public async Task<SplitOrderResult> Handle(AutoSplitOrderCommand request, CancellationToken ct)
    {
        // 1. Load parent order
        var parent = await repo.GetByIdAsync(request.OrderId, ct)
            ?? throw new NotFoundException(nameof(TransportOrder), request.OrderId);

        if (parent.Items.Count == 0)
            throw new DomainException("Cannot auto-split an order with no items.", "ORDER_NO_ITEMS");

        // 2. Greedy bin-packing — expand items by unit (1 item line = Qty units)
        var bins = BinPack(
            parent.Items.ToList(),
            request.MaxWeightPerSplitKg,
            request.MaxVolumePerSplitCBM);

        if (bins.Count < 2)
            throw new DomainException(
                "Auto-split did not produce more than 1 group. The order already fits within capacity constraints.",
                "SPLIT_NOT_NEEDED");

        // 3. Build child orders per bin
        var children = new List<TransportOrder>();
        var splitReason = request.Reason ?? "CapacityExceeded";

        foreach (var bin in bins)
        {
            var childNumber = await repo.GenerateOrderNumberAsync(ct);
            var child = TransportOrder.CreateSplitChild(childNumber, parent, splitReason);

            foreach (var (item, qty) in bin)
            {
                var childItem = OrderItem.Create(
                    child.Id,
                    item.Description,
                    item.Weight,
                    item.Volume,
                    qty,
                    item.SKU,
                    item.IsDangerousGoods,
                    item.UNNumber,
                    item.DGClass);

                child.AddItemInternal(childItem);
            }

            children.Add(child);
        }

        // 4. Mark parent
        var childIds = children.Select(c => c.Id).ToList();
        parent.MarkAsSplit(splitReason, childIds, "Auto");

        // 5. Persist
        await repo.AddRangeAsync(children, ct);
        await repo.UpdateAsync(parent, ct);

        // 6. Result
        return new SplitOrderResult(
            parent.Id,
            parent.OrderNumber,
            children.Select(c => new SplitChildSummary(
                c.Id, c.OrderNumber,
                c.TotalWeight, c.TotalVolume,
                c.Items.Count)).ToList());
    }

    /// <summary>
    /// Greedy First-Fit Bin Packing:
    /// วนแต่ละ OrderItem (แยก Qty ได้ถ้า item เดียวไม่พอ)
    /// แล้วใส่ใน bin ที่มีที่ว่างพอก่อน, ถ้าไม่มี → เปิด bin ใหม่
    /// Returns: list of bins, each bin = list of (OrderItem, allocatedQty)
    /// </summary>
    private static List<List<(OrderItem Item, int Qty)>> BinPack(
        List<OrderItem> items,
        decimal maxWeightKg,
        decimal maxVolumeCBM)
    {
        var bins = new List<(decimal UsedWeight, decimal UsedVolume, List<(OrderItem, int)> Items)>();

        foreach (var item in items)
        {
            int remainingQty = item.Quantity;

            while (remainingQty > 0)
            {
                // Find first bin with enough capacity for at least 1 unit
                var fit = bins.FirstOrDefault(b =>
                    b.UsedWeight + item.Weight <= maxWeightKg &&
                    (maxVolumeCBM <= 0 || b.UsedVolume + item.Volume <= maxVolumeCBM));

                if (fit.Items is null)
                {
                    // Open new bin
                    fit = (0m, 0m, new List<(OrderItem, int)>());
                    bins.Add(fit);
                }

                // How many units fit in this bin?
                var fitByWeight = maxWeightKg > 0
                    ? (int)Math.Floor((maxWeightKg - fit.UsedWeight) / item.Weight)
                    : remainingQty;
                var fitByVolume = maxVolumeCBM > 0
                    ? (int)Math.Floor((maxVolumeCBM - fit.UsedVolume) / item.Volume)
                    : remainingQty;

                var canFit = Math.Min(fitByWeight, Math.Min(fitByVolume, remainingQty));
                if (canFit <= 0) canFit = 1; // Safety: at least 1 unit per bin

                // Rebuild bin (record struct is immutable)
                var idx = bins.IndexOf(fit);
                fit.Items.Add((item, canFit));
                bins[idx] = (
                    fit.UsedWeight + item.Weight * canFit,
                    fit.UsedVolume + (item.Volume > 0 ? item.Volume * canFit : 0m),
                    fit.Items);

                remainingQty -= canFit;
            }
        }

        return bins.Select(b => b.Items).ToList();
    }
}
