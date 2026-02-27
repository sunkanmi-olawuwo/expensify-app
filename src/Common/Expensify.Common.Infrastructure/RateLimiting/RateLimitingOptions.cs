namespace Expensify.Common.Infrastructure.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public FixedWindowLimitOptions Auth { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    public FixedWindowLimitOptions Write { get; set; } = new()
    {
        PermitLimit = 60,
        WindowSeconds = 60,
        QueueLimit = 0
    };
}

public sealed class FixedWindowLimitOptions
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }

    public int QueueLimit { get; set; }
}
