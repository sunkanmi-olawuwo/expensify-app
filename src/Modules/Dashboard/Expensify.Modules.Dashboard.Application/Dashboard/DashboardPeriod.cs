using System.Globalization;

namespace Expensify.Modules.Dashboard.Application.Dashboard;

internal sealed record DashboardPeriod(
    DateOnly BaseMonth,
    int MonthStartDay,
    DateOnly StartDate,
    DateOnly EndDateExclusive,
    string DisplayLabel)
{
    public static DashboardPeriod CreateCurrent(DateTime utcNow, string timezoneId, int monthStartDay)
    {
        TimeZoneInfo timeZone = DashboardTimeZoneResolver.Resolve(timezoneId);
        DateTime normalizedUtcNow = utcNow.Kind == DateTimeKind.Utc
            ? utcNow
            : DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(normalizedUtcNow, timeZone));
        var currentBaseMonth = new DateOnly(localDate.Year, localDate.Month, 1);
        DateOnly currentMonthStart = CreateStartDate(currentBaseMonth, monthStartDay);
        DateOnly targetBaseMonth = localDate < currentMonthStart ? currentBaseMonth.AddMonths(-1) : currentBaseMonth;

        return CreateForMonth(targetBaseMonth, monthStartDay);
    }

    public static bool TryCreate(string? period, int monthStartDay, out DashboardPeriod? dashboardPeriod)
    {
        if (string.IsNullOrWhiteSpace(period) ||
            !DateOnly.TryParseExact($"{period}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly baseMonth))
        {
            dashboardPeriod = null;
            return false;
        }

        dashboardPeriod = CreateForMonth(baseMonth, monthStartDay);
        return true;
    }

    public static List<DashboardPeriod> CreateHistory(DashboardPeriod currentPeriod, int count)
    {
        return Enumerable.Range(0, count)
            .Select(offset => CreateForMonth(currentPeriod.BaseMonth.AddMonths(offset - (count - 1)), currentPeriod.MonthStartDay))
            .ToList();
    }

    public DashboardPeriod Previous() => CreateForMonth(BaseMonth.AddMonths(-1), MonthStartDay);

    public bool Contains(DateOnly date) => date >= StartDate && date < EndDateExclusive;

    public DateTimeOffset GetStartDateUtc(TimeZoneInfo timeZone) => DashboardTimeZoneResolver.ConvertLocalDateStartToUtc(StartDate, timeZone);

    public DateTimeOffset GetEndDateExclusiveUtc(TimeZoneInfo timeZone) =>
        DashboardTimeZoneResolver.ConvertLocalDateStartToUtc(EndDateExclusive, timeZone);

    private static DashboardPeriod CreateForMonth(DateOnly baseMonth, int monthStartDay)
    {
        DateOnly normalizedBaseMonth = new(baseMonth.Year, baseMonth.Month, 1);
        DateOnly nextBaseMonth = normalizedBaseMonth.AddMonths(1);
        DateOnly startDate = CreateStartDate(normalizedBaseMonth, monthStartDay);
        DateOnly endDateExclusive = CreateStartDate(nextBaseMonth, monthStartDay);

        return new DashboardPeriod(
            normalizedBaseMonth,
            monthStartDay,
            startDate,
            endDateExclusive,
            startDate.ToString("MMM yyyy", CultureInfo.InvariantCulture));
    }

    private static DateOnly CreateStartDate(DateOnly baseMonth, int monthStartDay)
    {
        int safeMonthStartDay = Math.Clamp(monthStartDay, 1, 31);
        int clampedDay = Math.Min(safeMonthStartDay, DateTime.DaysInMonth(baseMonth.Year, baseMonth.Month));
        return new DateOnly(baseMonth.Year, baseMonth.Month, clampedDay);
    }
}

internal static class DashboardTimeZoneResolver
{
    internal static TimeZoneInfo Resolve(string timezoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    internal static DateTimeOffset ConvertLocalDateStartToUtc(DateOnly date, TimeZoneInfo timeZone)
    {
        var localStart = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        DateTime utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        return new DateTimeOffset(utcStart, TimeSpan.Zero);
    }
}
