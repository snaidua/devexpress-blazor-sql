using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Stockroom.Models;

[Index(nameof(Sku), IsUnique = true)]
public sealed class Product
{
    public int Id { get; set; }

    [Required, StringLength(24)]
    public string Sku { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Category { get; set; } = "General";

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0", "999999")]
    public decimal UnitPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
