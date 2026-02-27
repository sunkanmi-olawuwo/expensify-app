namespace Expensify.Modules.Expenses.Application.Abstractions.SoftDelete;

public interface ISoftDeleteRetentionProvider
{
    int RetentionDays { get; }
}