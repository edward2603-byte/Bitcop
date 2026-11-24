using System.Runtime.InteropServices;

namespace WorkControl.Agent;

public sealed record ActivitySnapshot(string State, long IdleSeconds, bool IsLongAbsence);

public static class UserActivityProbe
{
    public static ActivitySnapshot GetCurrent(ActivityOptions options)
    {
        var lastInput = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref lastInput)) return new ActivitySnapshot("unknown", 0, false);

        var idleMilliseconds = unchecked((uint)Environment.TickCount - lastInput.Time);
        return Classify(idleMilliseconds / 1000, options);
    }

    public static ActivitySnapshot Classify(long idleSeconds, ActivityOptions options)
    {
        if (idleSeconds < 0) throw new ArgumentOutOfRangeException(nameof(idleSeconds));
        var state = idleSeconds >= options.IdleThresholdSeconds ? "idle" : "active";
        var isLongAbsence = idleSeconds >= options.LongAbsenceThresholdSeconds;
        return new ActivitySnapshot(state, idleSeconds, isLongAbsence);
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
