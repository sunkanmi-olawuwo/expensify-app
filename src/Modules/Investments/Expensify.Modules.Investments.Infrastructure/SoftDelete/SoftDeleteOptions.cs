namespace Expensify.Modules.Investments.Infrastructure.SoftDelete;

internal sealed class SoftDeleteOptions
{
    public int IntervalInSeconds { get; init; }

    public int BatchSize { get; init; }

    public int RetentionDays { get; init; }
}
