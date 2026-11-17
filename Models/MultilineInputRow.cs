namespace Stockroom.Models;

public sealed class MultilineInputRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
