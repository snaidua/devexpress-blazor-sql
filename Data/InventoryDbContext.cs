using Microsoft.EntityFrameworkCore;
using Stockroom.Models;

namespace Stockroom.Data;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentLine> StockAdjustmentLines => Set<StockAdjustmentLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .Property(product => product.UnitPrice)
            .HasPrecision(10, 2);

        modelBuilder.Entity<InventoryTransaction>()
            .Property(transaction => transaction.UnitCost)
            .HasPrecision(10, 2);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(transaction => transaction.Product)
            .WithMany()
            .HasForeignKey(transaction => transaction.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(transaction => transaction.Vendor)
            .WithMany()
            .HasForeignKey(transaction => transaction.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockAdjustmentLine>()
            .HasOne(line => line.StockAdjustment)
            .WithMany(adjustment => adjustment.Lines)
            .HasForeignKey(line => line.StockAdjustmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockAdjustmentLine>()
            .HasOne(line => line.Product)
            .WithMany()
            .HasForeignKey(line => line.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockAdjustmentLine>()
            .Property(line => line.UnitCost)
            .HasPrecision(10, 2);
    }
}
