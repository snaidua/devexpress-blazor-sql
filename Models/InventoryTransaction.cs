using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Stockroom.Models;

public static class InventoryTransactionTypes
{
    public const string Receipt = "Receipt";
    public const string Issue = "Issue";
}

[Index(nameof(DocumentNumber), IsUnique = true)]
[Index(nameof(TransactionDate))]
public sealed class InventoryTransaction
{
    public int Id { get; set; }

    [Required, StringLength(24)]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string Type { get; set; } = InventoryTransactionTypes.Receipt;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0", "999999")]
    public decimal UnitCost { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [StringLength(250)]
    public string Notes { get; set; } = string.Empty;
}
