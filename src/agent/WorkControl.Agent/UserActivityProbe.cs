using System.Runtime.InteropServices;

namespace WorkControl.Agent;

public sealed record ActivitySnapshot(string State, long IdleSeconds);

public static class UserActivityProbe
{
    private const uint IdleThresholdSeconds = 5 * 60;

    public static ActivitySnapshot GetCurrent()
    {
        var lastInput = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref lastInput)) return new ActivitySnapshot("unknown", 0);

        var idleMilliseconds = unchecked((uint)Environment.TickCount - lastInput.Time);
        var idleSeconds = idleMilliseconds / 1000;
        return new ActivitySnapshot(idleSeconds >= IdleThresholdSeconds ? "idle" : "active", idleSeconds);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo lastInputInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }
}
