namespace Expensify.Modules.Income.Application.Abstractions.SoftDelete;

public interface ISoftDeleteRetentionProvider
{
    int RetentionDays { get; }
}