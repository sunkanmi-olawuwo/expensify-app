namespace Expensify.Modules.Dashboard.Application.Dashboard;

internal static class DashboardCalculations
{
    private static readonly string[] ColorPalette = ["chart-1", "chart-2", "chart-3", "chart-4", "chart-5", "chart-6"];

    internal static decimal SumInPeriod(IEnumerable<DashboardIncomeRow> rows, DashboardPeriod period) =>
        rows.Where(row => period.Contains(row.TransactionDate)).Sum(row => row.Amount);

    internal static decimal SumInPeriod(IEnumerable<DashboardExpenseRow> rows, DashboardPeriod period) =>
        rows.Where(row => period.Contains(row.TransactionDate)).Sum(row => row.Amount);

    internal static decimal CalculateChangePercentage(decimal current, decimal previous)
    {
        if (previous == 0m)
        {
            return 0m;
        }

        return Math.Round((current - previous) / Math.Abs(previous) * 100m, 2, MidpointRounding.AwayFromZero);
    }

    internal static decimal CalculateSavingsRate(decimal income, decimal expenses)
    {
        if (income == 0m)
        {
            return 0m;
        }

        return Math.Round((income - expenses) / income * 100m, 2, MidpointRounding.AwayFromZero);
    }

    internal static IReadOnlyList<decimal> CalculatePercentages(
        IReadOnlyList<decimal> amounts,
        decimal total,
        bool correctFinalPercentage)
    {
        if (amounts.Count == 0)
        {
            return [];
        }

        if (total == 0m)
        {
            return Enumerable.Repeat(0m, amounts.Count).ToList();
        }

        var percentages = new List<decimal>(amounts.Count);
        decimal runningPercentage = 0m;

        for (int i = 0; i < amounts.Count; i++)
        {
            decimal percentage = correctFinalPercentage && i == amounts.Count - 1
                ? Math.Max(0m, Math.Round(100m - runningPercentage, 2, MidpointRounding.AwayFromZero))
                : Math.Round(amounts[i] / total * 100m, 2, MidpointRounding.AwayFromZero);

            percentages.Add(percentage);
            runningPercentage += percentage;
        }

        return percentages;
    }

    internal static string GetColorKey(int index) => ColorPalette[index % ColorPalette.Length];

    internal static int NormalizeTrendMonths(int months, int defaultMonths) =>
        months is 3 or 6 or 12 ? months : defaultMonths;

    internal static int NormalizeLookbackMonths(int months, int defaultMonths) =>
        months is 1 or 3 or 6 or 12 ? months : defaultMonths;

    internal static int NormalizeTopCategoryLimit(int limit, int defaultLimit) =>
        limit < 1 ? defaultLimit : Math.Clamp(limit, 1, 10);

    internal static string FormatLookbackPeriod(int months) => months == 1 ? "Last 1 month" : $"Last {months} months";

    internal static DateOnly ToLocalDate(DateTimeOffset timestamp, TimeZoneInfo timeZone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timestamp, timeZone).DateTime);
}
