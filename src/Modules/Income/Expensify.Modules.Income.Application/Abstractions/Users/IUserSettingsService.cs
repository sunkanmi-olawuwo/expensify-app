using Expensify.Common.Domain;

namespace Expensify.Modules.Income.Application.Abstractions.Users;

public interface IUserSettingsService
{
    Task<Result<UserSettingsResponse>> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record UserSettingsResponse(string Currency, string Timezone, int MonthStartDay);
