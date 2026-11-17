using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Stockroom.Models;

public static class StockAdjustmentReasons
{
    public const string PhysicalCount = "Physical count";
    public const string Damage = "Damage";
    public const string Correction = "Correction";
    public const string OpeningBalance = "Opening balance";
    public const string Other = "Other";

    public static readonly string[] All =
    [
        PhysicalCount,
        Damage,
        Correction,
        OpeningBalance,
        Other
    ];
}

[Index(nameof(DocumentNumber), IsUnique = true)]
[Index(nameof(AdjustmentDate))]
public sealed class StockAdjustment
{
    public int Id { get; set; }

    [Required, StringLength(24)]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string Reason { get; set; } = StockAdjustmentReasons.PhysicalCount;

    public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;

    [StringLength(250)]
    public string Reference { get; set; } = string.Empty;

    public ICollection<StockAdjustmentLine> Lines { get; set; } = new List<StockAdjustmentLine>();
}

public sealed class StockAdjustmentLine
{
    public int Id { get; set; }

    public int StockAdjustmentId { get; set; }
    public StockAdjustment StockAdjustment { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int SystemQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int CountedQuantity { get; set; }

    [Range(typeof(decimal), "0", "999999")]
    public decimal UnitCost { get; set; }

    public int Variance => CountedQuantity - SystemQuantity;
}

public sealed record StockAdjustmentLineInput(int ProductId, int CountedQuantity);
