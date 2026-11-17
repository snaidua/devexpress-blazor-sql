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
        await EnsureExtendedSchemaAsync(db, cancellationToken);

        if (!await db.Products.AnyAsync(cancellationToken))
        {
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

        if (!await db.Vendors.AnyAsync(cancellationToken))
        {
            db.Vendors.AddRange(
                new Vendor { Code = "VEN-001", Name = "Northstar Supply", ContactName = "Maya Chen", Phone = "+1 555 0142", Email = "orders@northstarsupply.example", Address = "120 Market Street" },
                new Vendor { Code = "VEN-002", Name = "Brightline Wholesale", ContactName = "Evan Brooks", Phone = "+1 555 0178", Email = "sales@brightline.example", Address = "48 Industrial Way" },
                new Vendor { Code = "VEN-003", Name = "Harbor & Pine", ContactName = "Sofia Reed", Phone = "+1 555 0126", Email = "hello@harborpine.example", Address = "9 Harbor Road" });
            await db.SaveChangesAsync(cancellationToken);
        }
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
        if (await db.InventoryTransactions.AnyAsync(transaction => transaction.ProductId == id, cancellationToken))
            throw new InvalidOperationException("This item has posted transactions and cannot be deleted.");
        if (await db.StockAdjustmentLines.AnyAsync(line => line.ProductId == id, cancellationToken))
            throw new InvalidOperationException("This item has posted stock adjustments and cannot be deleted.");

        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vendor>> GetVendorsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Vendors.AsNoTracking().OrderBy(vendor => vendor.Name).ToListAsync(cancellationToken);
    }

    public async Task DeleteVendorAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var vendor = await db.Vendors.FindAsync([id], cancellationToken);
        if (vendor is null)
        throw new InvalidOperationException("This vendor no longer exists. Refresh the list and try again.");
        if (await db.InventoryTransactions.AnyAsync(transaction => transaction.VendorId == id, cancellationToken))
        throw new InvalidOperationException("This vendor has posted receipts and cannot be deleted.");

        db.Vendors.Remove(vendor);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveVendorAsync(Vendor vendor, CancellationToken cancellationToken = default)
    {
        var code = vendor.Code.Trim().ToUpperInvariant();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Vendors.AnyAsync(existing => existing.Code == code && existing.Id != vendor.Id, cancellationToken))
            throw new InvalidOperationException($"Vendor code {code} is already in use.");

        if (vendor.Id == 0)
        {
            vendor.Code = code;
            vendor.Name = vendor.Name.Trim();
            db.Vendors.Add(vendor);
        }
        else
        {
            var existing = await db.Vendors.FindAsync([vendor.Id], cancellationToken)
                ?? throw new InvalidOperationException("This vendor no longer exists. Refresh the list and try again.");
            existing.Code = code;
            existing.Name = vendor.Name.Trim();
            existing.ContactName = vendor.ContactName.Trim();
            existing.Phone = vendor.Phone.Trim();
            existing.Email = vendor.Email.Trim();
            existing.Address = vendor.Address.Trim();
            existing.IsActive = vendor.IsActive;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureExtendedSchemaAsync(InventoryDbContext db, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[dbo].[Vendors]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Vendors] (
                    [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Vendors] PRIMARY KEY,
                    [Code] nvarchar(20) NOT NULL,
                    [Name] nvarchar(100) NOT NULL,
                    [ContactName] nvarchar(80) NOT NULL,
                    [Phone] nvarchar(30) NOT NULL,
                    [Email] nvarchar(120) NOT NULL,
                    [Address] nvarchar(200) NOT NULL,
                    [IsActive] bit NOT NULL
                );
                CREATE UNIQUE INDEX [IX_Vendors_Code] ON [dbo].[Vendors] ([Code]);
            END;

            IF OBJECT_ID(N'[dbo].[InventoryTransactions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[InventoryTransactions] (
                    [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY,
                    [DocumentNumber] nvarchar(24) NOT NULL,
                    [Type] nvarchar(10) NOT NULL,
                    [ProductId] int NOT NULL,
                    [VendorId] int NULL,
                    [Quantity] int NOT NULL,
                    [UnitCost] decimal(10,2) NOT NULL,
                    [TransactionDate] datetime2 NOT NULL,
                    [Notes] nvarchar(250) NOT NULL,
                    CONSTRAINT [FK_InventoryTransactions_Products_ProductId]
                        FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]),
                    CONSTRAINT [FK_InventoryTransactions_Vendors_VendorId]
                        FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Vendors] ([Id])
                );
                CREATE UNIQUE INDEX [IX_InventoryTransactions_DocumentNumber]
                    ON [dbo].[InventoryTransactions] ([DocumentNumber]);
                CREATE INDEX [IX_InventoryTransactions_TransactionDate]
                    ON [dbo].[InventoryTransactions] ([TransactionDate]);
                CREATE INDEX [IX_InventoryTransactions_ProductId]
                    ON [dbo].[InventoryTransactions] ([ProductId]);
                CREATE INDEX [IX_InventoryTransactions_VendorId]
                    ON [dbo].[InventoryTransactions] ([VendorId]);
            END;

            IF OBJECT_ID(N'[dbo].[StockAdjustments]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[StockAdjustments] (
                    [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_StockAdjustments] PRIMARY KEY,
                    [DocumentNumber] nvarchar(24) NOT NULL,
                    [Reason] nvarchar(30) NOT NULL,
                    [AdjustmentDate] datetime2 NOT NULL,
                    [Reference] nvarchar(250) NOT NULL
                );
                CREATE UNIQUE INDEX [IX_StockAdjustments_DocumentNumber]
                    ON [dbo].[StockAdjustments] ([DocumentNumber]);
                CREATE INDEX [IX_StockAdjustments_AdjustmentDate]
                    ON [dbo].[StockAdjustments] ([AdjustmentDate]);
            END;

            IF OBJECT_ID(N'[dbo].[StockAdjustmentLines]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[StockAdjustmentLines] (
                    [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_StockAdjustmentLines] PRIMARY KEY,
                    [StockAdjustmentId] int NOT NULL,
                    [ProductId] int NOT NULL,
                    [SystemQuantity] int NOT NULL,
                    [CountedQuantity] int NOT NULL,
                    [UnitCost] decimal(10,2) NOT NULL,
                    CONSTRAINT [FK_StockAdjustmentLines_StockAdjustments_StockAdjustmentId]
                        FOREIGN KEY ([StockAdjustmentId]) REFERENCES [dbo].[StockAdjustments] ([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_StockAdjustmentLines_Products_ProductId]
                        FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id])
                );
                CREATE INDEX [IX_StockAdjustmentLines_StockAdjustmentId]
                    ON [dbo].[StockAdjustmentLines] ([StockAdjustmentId]);
                CREATE INDEX [IX_StockAdjustmentLines_ProductId]
                    ON [dbo].[StockAdjustmentLines] ([ProductId]);
            END;
            """,
            cancellationToken);
    }
}
