using Microsoft.EntityFrameworkCore;
using Stockroom.Data;
using Stockroom.Models;

namespace Stockroom.Services;

public sealed class TransactionService(IDbContextFactory<InventoryDbContext> contextFactory)
{
    public async Task<IReadOnlyList<InventoryTransaction>> GetTransactionsAsync(
        string type,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.InventoryTransactions
            .AsNoTracking()
            .Include(transaction => transaction.Product)
            .Include(transaction => transaction.Vendor)
            .Where(transaction => transaction.Type == type)
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task PostAsync(
        string type,
        int productId,
        int? vendorId,
        int quantity,
        decimal unitCost,
        DateTime transactionDate,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (type is not (InventoryTransactionTypes.Receipt or InventoryTransactionTypes.Issue))
            throw new InvalidOperationException("Choose a valid receipt or issue transaction.");
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero.");
        if (unitCost < 0 || unitCost > 999999m)
            throw new InvalidOperationException("Unit cost must be between 0 and 999,999.");
        if (type == InventoryTransactionTypes.Receipt && vendorId is null)
            throw new InvalidOperationException("Select a vendor for this receipt.");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var product = await db.Products.SingleOrDefaultAsync(item => item.Id == productId, cancellationToken)
            ?? throw new InvalidOperationException("Select an item that exists in the item master.");

        if (type == InventoryTransactionTypes.Issue && quantity > product.Quantity)
            throw new InvalidOperationException($"Only {product.Quantity} units of {product.Name} are available.");
        if (type == InventoryTransactionTypes.Receipt && quantity > int.MaxValue - product.Quantity)
            throw new InvalidOperationException("The resulting stock quantity is too large.");

        Vendor? vendor = null;
        if (vendorId is not null)
        {
            vendor = await db.Vendors.SingleOrDefaultAsync(item => item.Id == vendorId, cancellationToken)
                ?? throw new InvalidOperationException("Select a vendor that exists in the vendor master.");
            if (!vendor.IsActive)
                throw new InvalidOperationException("The selected vendor is inactive.");
        }

        product.Quantity = type == InventoryTransactionTypes.Receipt
            ? product.Quantity + quantity
            : product.Quantity - quantity;

        db.InventoryTransactions.Add(new InventoryTransaction
        {
            DocumentNumber = $"{(type == InventoryTransactionTypes.Receipt ? "RCV" : "ISS")}-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            Type = type,
            ProductId = product.Id,
            VendorId = vendor?.Id,
            Quantity = quantity,
            UnitCost = unitCost,
            TransactionDate = DateTime.SpecifyKind(transactionDate, DateTimeKind.Local).ToUniversalTime(),
            Notes = (notes ?? string.Empty).Trim()
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateNotesAsync(int transactionId, string notes, CancellationToken cancellationToken = default)
    {
        if (notes.Length > 250)
            throw new InvalidOperationException("Notes cannot exceed 250 characters.");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var transaction = await db.InventoryTransactions.FindAsync([transactionId], cancellationToken)
            ?? throw new InvalidOperationException("This transaction no longer exists. Refresh the list and try again.");
        transaction.Notes = notes.Trim();
        await db.SaveChangesAsync(cancellationToken);
    }
}
