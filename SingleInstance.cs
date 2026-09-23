namespace StandUpHero;

// Shared by every installation path in the current Windows session.
public sealed class SingleInstance : IDisposable
{
    private readonly Mutex mutex;
    private readonly EventWaitHandle showRequest;
    public bool IsPrimary { get; }

    public SingleInstance(string name = "StandUpHero.Desktop")
    {
        mutex = new Mutex(false, @"Local\" + name);
        showRequest = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\" + name + ".Show");
        try { IsPrimary = mutex.WaitOne(0); }
        catch (AbandonedMutexException) { IsPrimary = true; }
        if (IsPrimary) showRequest.Reset();
    }

    public void RequestShow() => showRequest.Set();
    public bool ConsumeShowRequest() => showRequest.WaitOne(0);
    public void Dispose()
    {
        if (IsPrimary) mutex.ReleaseMutex();
        showRequest.Dispose();
        mutex.Dispose();
    }
}
