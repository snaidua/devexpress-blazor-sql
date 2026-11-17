using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Stockroom.Models;

[Index(nameof(Code), IsUnique = true)]
public sealed class Vendor
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(80)]
    public string ContactName { get; set; } = string.Empty;

    [OptionalPhone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [OptionalEmailAddress, StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public sealed class OptionalPhoneAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is not string phone || string.IsNullOrWhiteSpace(phone) || new PhoneAttribute().IsValid(phone);
}

public sealed class OptionalEmailAddressAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is not string email || string.IsNullOrWhiteSpace(email) || new EmailAddressAttribute().IsValid(email);
}
