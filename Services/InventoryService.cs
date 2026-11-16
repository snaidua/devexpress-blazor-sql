using Microsoft.EntityFrameworkCore;
using Stockroom.Data;
using Stockroom.Models;

namespace Stockroom.Services;

public sealed class InventoryService(IDbContextFactory<InventoryDbContext> contextFactory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (await db.Products.AnyAsync(cancellationToken))
            return;

        db.Products.AddRange(
            new Product { Sku = "EL-2048", Name = "Wireless headphones", Category = "Electronics", Quantity = 42, UnitPrice = 89.99m, ReorderLevel = 12 },
            new Product { Sku = "OF-1102", Name = "Desk organizer", Category = "Office", Quantity = 8, UnitPrice = 24.50m, ReorderLevel = 10 },
            new Product { Sku = "EL-3190", Name = "USB-C hub", Category = "Electronics", Quantity = 31, UnitPrice = 54.00m, ReorderLevel = 8 },
            new Product { Sku = "HM-0087", Name = "Ceramic mug set", Category = "Home", Quantity = 5, UnitPrice = 32.00m, ReorderLevel = 8 },
            new Product { Sku = "OF-2210", Name = "Notebook, dotted", Category = "Office", Quantity = 64, UnitPrice = 8.75m, ReorderLevel = 15 },
            new Product { Sku = "EL-4071", Name = "Portable SSD 1 TB", Category = "Electronics", Quantity = 14, UnitPrice = 119.00m, ReorderLevel = 6 },
            new Product { Sku = "HM-0148", Name = "Linen table runner", Category = "Home", Quantity = 3, UnitPrice = 38.00m, ReorderLevel = 5 },
            new Product { Sku = "OF-3099", Name = "LED desk lamp", Category = "Office", Quantity = 19, UnitPrice = 46.00m, ReorderLevel = 7 });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Products.AsNoTracking()
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        var sku = product.Sku.Trim().ToUpperInvariant();
        var name = product.Name.Trim();
        var category = product.Category.Trim();

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var duplicateSku = await db.Products.AnyAsync(
            existing => existing.Sku == sku && existing.Id != product.Id,
            cancellationToken);
        if (duplicateSku)
            throw new InvalidOperationException($"SKU {sku} is already in use.");

        if (product.Id == 0)
        {
            product.Sku = sku;
            product.Name = name;
            product.Category = category;
            product.UpdatedAt = DateTime.UtcNow;
            db.Products.Add(product);
        }
        else
        {
            var existing = await db.Products.FindAsync([product.Id], cancellationToken);
            if (existing is null)
                throw new InvalidOperationException("This product no longer exists. Refresh the inventory and try again.");

            existing.Sku = sku;
            existing.Name = name;
            existing.Category = category;
            existing.Quantity = product.Quantity;
            existing.UnitPrice = product.UnitPrice;
            existing.ReorderLevel = product.ReorderLevel;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var product = await db.Products.FindAsync([id], cancellationToken);
        if (product is null)
            throw new InvalidOperationException("This product no longer exists. Refresh the inventory and try again.");

        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
    }
}
