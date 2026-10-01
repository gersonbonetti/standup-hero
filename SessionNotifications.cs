using System.Runtime.InteropServices;

namespace StandUpHero;

internal static class SessionNotifications
{
    internal const int Message = 0x02B1;
    internal const int Locked = 7, Unlocked = 8;
    [DllImport("wtsapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WTSRegisterSessionNotification(IntPtr window, int flags);
    [DllImport("wtsapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WTSUnRegisterSessionNotification(IntPtr window);
    internal static bool Register(IntPtr handle) => WTSRegisterSessionNotification(handle, 0); // Only this Windows session.
    internal static void Unregister(IntPtr handle) => WTSUnRegisterSessionNotification(handle);
}
