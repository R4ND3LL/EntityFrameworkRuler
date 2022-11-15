namespace EntityFrameworkRuler.Extension;

internal static class DateTimeExtensions {
    /// <summary> Returns <see cref="Environment.TickCount"/> as a start time in milliseconds as a <see cref="uint"/>. </summary>
    public static uint GetTime() {
        return (uint)Environment.TickCount;
    }

    /// <summary> Returns <see cref="Environment.TickCount"/> as a start time in milliseconds as a <see cref="uint"/>. </summary>
    public static uint GetTime(this object o) {
        return (uint)Environment.TickCount;
    }
}