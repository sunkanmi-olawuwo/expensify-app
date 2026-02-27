namespace Expensify.Modules.Income.Infrastructure.SoftDelete;

internal sealed class SoftDeleteOptions
{
    public int IntervalInSeconds { get; init; } = 300;

    public int BatchSize { get; init; } = 100;

    public int RetentionDays { get; init; } = 30;
}