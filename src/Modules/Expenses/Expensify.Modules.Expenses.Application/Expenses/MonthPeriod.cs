using System.Globalization;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Domain.Expenses;

namespace Expensify.Modules.Expenses.Application.Expenses;

internal sealed record MonthPeriod(string Period, DateOnly StartDate, DateOnly EndDateExclusive)
{
    public static Result<MonthPeriod> Create(string period, int monthStartDay)
    {
        if (string.IsNullOrWhiteSpace(period) ||
            !DateOnly.TryParseExact($"{period}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly baseMonth))
        {
            return Result.Failure<MonthPeriod>(ExpenseErrors.PeriodInvalid(period));
        }

        int startDay = Math.Clamp(monthStartDay, 1, 28);
        int clampedStartDay = Math.Min(startDay, DateTime.DaysInMonth(baseMonth.Year, baseMonth.Month));
        var startDate = new DateOnly(baseMonth.Year, baseMonth.Month, clampedStartDay);

        DateOnly nextBase = baseMonth.AddMonths(1);
        int nextClampedDay = Math.Min(startDay, DateTime.DaysInMonth(nextBase.Year, nextBase.Month));
        var endDateExclusive = new DateOnly(nextBase.Year, nextBase.Month, nextClampedDay);

        return new MonthPeriod(period, startDate, endDateExclusive);
    }
}
