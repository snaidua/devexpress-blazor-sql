namespace Stockroom.Services;

public sealed class BusyIndicatorService
{
    public bool IsBusy { get; private set; }
    public event Action? Changed;

    public void SetBusy(bool isBusy)
    {
        if (IsBusy == isBusy)
            return;

        IsBusy = isBusy;
        Changed?.Invoke();
    }
}
