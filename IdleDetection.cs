using System.Runtime.InteropServices;

namespace StandUpHero;

internal static class IdleDetection
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo { public uint Size; public uint Time; }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    // Reads only the timestamp Windows already maintains for this session.
    // No hooks, key values, pointer positions, application inspection, or history.
    internal static TimeSpan? Read()
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        return GetLastInputInfo(ref info) ? FromTicks(unchecked((uint)Environment.TickCount64), info.Time) : null;
    }

    internal static TimeSpan? FromTicks(uint now, uint lastInput)
    {
        uint milliseconds = unchecked(now - lastInput);
        // Handles the 32-bit wrap and rejects future/inconsistent timestamps.
        return milliseconds <= int.MaxValue ? TimeSpan.FromMilliseconds(milliseconds) : null;
    }
}

internal sealed class IdleSuggestionPolicy
{
    private bool observedBreak;
    private TimeSpan? lastSuggestion;
    internal void ClearObservation() => observedBreak = false;

    internal bool Observe(TimeSpan? idle, TimeSpan uptime, int thresholdMinutes, bool canSuggest)
    {
        if (idle is null || idle < TimeSpan.Zero) { ClearObservation(); return false; }
        if (idle >= TimeSpan.FromMinutes(thresholdMinutes)) { observedBreak = true; return false; }
        if (!observedBreak || idle > TimeSpan.FromSeconds(30)) return false;
        observedBreak = false;
        if (!canSuggest || lastSuggestion is TimeSpan last && uptime - last < TimeSpan.FromMinutes(30)) return false;
        lastSuggestion = uptime;
        return true;
    }
}
