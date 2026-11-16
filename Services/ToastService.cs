namespace Stockroom.Services;

public enum ToastKind
{
    Success,
    Info,
    Error
}

public sealed record ToastMessage(Guid Id, string Message, ToastKind Kind);

public sealed class ToastService
{
    public event Action<ToastMessage>? ToastAdded;

    public void Show(string message, ToastKind kind = ToastKind.Success)
    {
        ToastAdded?.Invoke(new ToastMessage(Guid.NewGuid(), message, kind));
    }
}
