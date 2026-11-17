using Microsoft.EntityFrameworkCore;
using Stockroom.Data;
using Stockroom.Models;

namespace Stockroom.Services;

public sealed class StockAdjustmentService(IDbContextFactory<InventoryDbContext> contextFactory)
{
    public async Task<IReadOnlyList<StockAdjustment>> GetAdjustmentsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.StockAdjustments
            .AsNoTracking()
            .Include(adjustment => adjustment.Lines)
                .ThenInclude(line => line.Product)
            .OrderByDescending(adjustment => adjustment.AdjustmentDate)
            .ThenByDescending(adjustment => adjustment.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockAdjustment> PostAsync(
        DateTime adjustmentDate,
        string reason,
        string? reference,
        IReadOnlyCollection<StockAdjustmentLineInput> lines,
        CancellationToken cancellationToken = default)
    {
        if (!StockAdjustmentReasons.All.Contains(reason, StringComparer.Ordinal))
            throw new InvalidOperationException("Choose a valid adjustment reason.");
        if (lines.Count == 0)
            throw new InvalidOperationException("Add at least one item to the adjustment.");
        if (lines.Any(line => line.ProductId <= 0 || line.CountedQuantity < 0))
            throw new InvalidOperationException("Select an item and enter a counted quantity of zero or more.");
        if (lines.Select(line => line.ProductId).Distinct().Count() != lines.Count)
            throw new InvalidOperationException("An item can only appear once in an adjustment.");
        if ((reference?.Length ?? 0) > 250)
            throw new InvalidOperationException("Reference cannot exceed 250 characters.");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var productIds = lines.Select(line => line.ProductId).ToArray();
        var products = await db.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);
        if (products.Count != productIds.Length)
            throw new InvalidOperationException("One or more selected items no longer exist. Refresh the screen and try again.");

        var adjustment = new StockAdjustment
        {
            DocumentNumber = $"ADJ-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            AdjustmentDate = DateTime.SpecifyKind(adjustmentDate, DateTimeKind.Local).ToUniversalTime(),
            Reason = reason,
            Reference = (reference ?? string.Empty).Trim()
        };

        foreach (var input in lines)
        {
            var product = products[input.ProductId];
            adjustment.Lines.Add(new StockAdjustmentLine
            {
                Product = product,
                SystemQuantity = product.Quantity,
                CountedQuantity = input.CountedQuantity,
                UnitCost = product.UnitPrice
            });
            product.Quantity = input.CountedQuantity;
            product.UpdatedAt = DateTime.UtcNow;
        }

        db.StockAdjustments.Add(adjustment);
        await db.SaveChangesAsync(cancellationToken);
        return adjustment;
    }
}
