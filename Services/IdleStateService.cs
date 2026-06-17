using System.Runtime.InteropServices;

namespace ScreenPeekr.Services;

internal static class IdleStateService
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    public static TimeSpan GetIdleTime()
    {
        var info = new LASTINPUTINFO
        {
            cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>()
        };

        if (!GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        var tickCount = Environment.TickCount64;
        var lastInput = info.dwTime;
        var idleMilliseconds = Math.Max(0, tickCount - lastInput);
        return TimeSpan.FromMilliseconds(idleMilliseconds);
    }

    public static bool IsIdle(TimeSpan threshold) => GetIdleTime() >= threshold;
}
