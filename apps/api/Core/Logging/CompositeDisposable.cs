namespace GameGuild.Core.Logging;

/// <summary>
/// Helper class to dispose multiple disposables
/// </summary>
internal class CompositeDisposable : IDisposable
{
    private readonly List<IDisposable> _disposables;

    private bool _disposed;

    public CompositeDisposable(List<IDisposable> disposables) { _disposables = disposables ?? throw new ArgumentNullException(nameof(disposables)); }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var disposable in _disposables)
        {
            try { disposable?.Dispose(); }
            catch
            {
                // Ignore disposal errors
            }
        }

        _disposed = true;
    }
}
