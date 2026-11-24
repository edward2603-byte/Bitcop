namespace WorkControl.Agent;

public sealed record ActivityOptions(long IdleThresholdSeconds, long LongAbsenceThresholdSeconds)
{
    public const long DefaultIdleThresholdSeconds = 60;
    public const long DefaultLongAbsenceThresholdSeconds = 300;

    public static ActivityOptions Load()
    {
        var idle = ReadPositiveInteger("WORKCONTROL_IDLE_THRESHOLD_SECONDS", DefaultIdleThresholdSeconds);
        var longAbsence = ReadPositiveInteger("WORKCONTROL_LONG_ABSENCE_THRESHOLD_SECONDS", DefaultLongAbsenceThresholdSeconds);
        if (longAbsence <= idle)
        {
            throw new InvalidOperationException(
                "WORKCONTROL_LONG_ABSENCE_THRESHOLD_SECONDS должен быть больше WORKCONTROL_IDLE_THRESHOLD_SECONDS.");
        }

        return new ActivityOptions(idle, longAbsence);
    }

    private static long ReadPositiveInteger(string variableName, long defaultValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(rawValue)) return defaultValue;
        if (long.TryParse(rawValue, out var value) && value > 0) return value;
        throw new InvalidOperationException($"{variableName} должен содержать целое число больше нуля.");
    }
}
