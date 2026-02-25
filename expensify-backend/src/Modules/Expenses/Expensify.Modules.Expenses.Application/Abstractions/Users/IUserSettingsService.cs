using Expensify.Common.Domain;

namespace Expensify.Modules.Expenses.Application.Abstractions.Users;

public interface IUserSettingsService
{
    Task<Result<UserSettingsResponse>> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record UserSettingsResponse(Guid UserId, string Currency, string Timezone, int MonthStartDay);
