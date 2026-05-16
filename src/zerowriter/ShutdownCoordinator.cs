namespace Zerowriter;

public sealed class ShutdownCoordinator : IDisposable
{
    private readonly CancellationTokenSource cts = new();
    private Action? cleanup;

    public ShutdownCoordinator()
    {
        Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    public CancellationToken Token => cts.Token;

    public void Register(Action cleanupAction)
    {
        cleanup = cleanupAction ?? throw new ArgumentNullException(nameof(cleanupAction));
    }

    public void Dispose()
    {
        Console.CancelKeyPress -= OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        cts.Dispose();
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        RequestCancellation();
    }

    private void OnProcessExit(object? sender, EventArgs e) => RequestShutdown();

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) => RequestShutdown();

    private void RequestShutdown()
    {
        RequestCancellation();
        cleanup?.Invoke();
    }

    private void RequestCancellation()
    {
        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
