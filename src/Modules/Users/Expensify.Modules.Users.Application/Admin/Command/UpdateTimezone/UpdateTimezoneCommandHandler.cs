using System.Data.Common;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Domain.Timezones;
using Expensify.Modules.Users.Domain.Preferences;

namespace Expensify.Modules.Users.Application.Admin.Command.UpdateTimezone;

internal sealed class UpdateTimezoneCommandHandler(
    ITimezoneRepository timezoneRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateTimezoneCommand, TimezoneResponse>
{
    public async Task<Result<TimezoneResponse>> Handle(UpdateTimezoneCommand request, CancellationToken cancellationToken)
    {
        string normalizedIanaId = request.IanaId.Trim();

        Timezone? timezone = await timezoneRepository.GetByIdAsync(normalizedIanaId, cancellationToken);
        if (timezone is null)
        {
            return Result.Failure<TimezoneResponse>(PreferenceCatalogErrors.TimezoneNotFound(normalizedIanaId));
        }

        if (request.IsDefault && !request.IsActive)
        {
            return Result.Failure<TimezoneResponse>(PreferenceCatalogErrors.TimezoneMustRemainActiveWhenDefault());
        }

        Timezone? currentDefault = await timezoneRepository.GetDefaultAsync(cancellationToken);

        if (timezone.IsDefault && (!request.IsDefault || !request.IsActive))
        {
            return Result.Failure<TimezoneResponse>(PreferenceCatalogErrors.DefaultTimezoneRequired());
        }

        if (currentDefault is null && request.IsActive && !request.IsDefault)
        {
            return Result.Failure<TimezoneResponse>(PreferenceCatalogErrors.DefaultTimezoneRequired());
        }

        if (request.IsDefault && currentDefault is not null && !string.Equals(currentDefault.IanaId, timezone.IanaId, StringComparison.Ordinal))
        {
            currentDefault.ClearDefault();
            timezoneRepository.Update(currentDefault);
        }

        timezone.Update(
            request.DisplayName,
            request.IsActive,
            request.IsDefault,
            request.SortOrder);

        timezoneRepository.Update(timezone);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbException)
        {
            return Result.Failure<TimezoneResponse>(PreferenceCatalogErrors.DefaultTimezoneConflict());
        }

        return new TimezoneResponse(
            timezone.IanaId,
            timezone.DisplayName,
            timezone.IsActive,
            timezone.IsDefault,
            timezone.SortOrder);
    }
}
