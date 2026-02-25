using Expensify.Common.Domain;
using Expensify.Modules.Income.Domain.Incomes;
using System.Globalization;

namespace Expensify.Modules.Income.Application.Incomes;

public sealed record MonthPeriod(string Period, DateOnly StartDate, DateOnly EndDateExclusive)
{
    public static Result<MonthPeriod> Create(string period, int monthStartDay)
    {
        if (!DateOnly.TryParseExact($"{period}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly monthDate))
        {
            return Result.Failure<MonthPeriod>(IncomeErrors.PeriodInvalid(period));
        }

        int safeMonthStartDay = Math.Clamp(monthStartDay, 1, 28);
        DateOnly startDate = new(monthDate.Year, monthDate.Month, safeMonthStartDay);
        DateOnly endDateExclusive = startDate.AddMonths(1);

        return new MonthPeriod(period, startDate, endDateExclusive);
    }
}
