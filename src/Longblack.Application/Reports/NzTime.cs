namespace Longblack.Application.Reports;

// Report date ranges are whole days in NZ local time, not UTC.
public static class NzTime
{
    private static readonly TimeZoneInfo Zone = Find();

    private static TimeZoneInfo Find()
    {
        foreach (var id in new[] { "Pacific/Auckland", "New Zealand Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        }
        return TimeZoneInfo.Utc;
    }

    // Inclusive start of the given local day.
    public static DateTimeOffset? StartOfDay(DateOnly? day) =>
        day is null ? null : At(day.Value);

    // Exclusive end of the given local day (start of the next one).
    public static DateTimeOffset? EndOfDay(DateOnly? day) =>
        day is null ? null : At(day.Value.AddDays(1));

    public static DateOnly LocalDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);

    public static DateTimeOffset ToLocal(DateTimeOffset instant) =>
        TimeZoneInfo.ConvertTime(instant, Zone);

    private static DateTimeOffset At(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Zone.GetUtcOffset(local)).ToUniversalTime();
    }
}
