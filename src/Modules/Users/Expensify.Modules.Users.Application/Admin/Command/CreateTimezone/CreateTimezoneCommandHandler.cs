using System.Data.Common;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Domain.Timezones;
using Expensify.Modules.Users.Domain.Preferences;

namespace Expensify.Modules.Users.Application.Admin.Command.CreateTimezone;

internal sealed class CreateTimezoneCommandHandler(
    ITimezoneRepository timezoneRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateTimezoneCommand, TimezoneResponse>
{
    public async Task<Result<TimezoneResponse>> Handle(CreateTimezoneCommand request, CancellationToken cancellationToken)
    {
        string normalizedIanaId = request.IanaId.Trim();

        Timezone? existingTimezone = await timezoneRepository.GetByIdAsync(normalizedIanaId, cancellationToken);
        if (existingTimezone is not null)
        {
            return Result.Failure<TimezoneResponse>(PreferenceCatalogErrors.TimezoneAlreadyExists(normalizedIanaId));
        }

        if (request.IsDefault && !request.IsActive)
        {
            return Result.Failure<TimezoneResponse>(PreferenceCatalogErrors.TimezoneMustRemainActiveWhenDefault());
        }

        Timezone? currentDefault = await timezoneRepository.GetDefaultAsync(cancellationToken);
        if (currentDefault is null && request.IsActive && !request.IsDefault)
        {
            return Result.Failure<TimezoneResponse>(PreferenceCatalogErrors.DefaultTimezoneRequired());
        }

        if (request.IsDefault && currentDefault is not null)
        {
            currentDefault.ClearDefault();
            timezoneRepository.Update(currentDefault);
        }

        var timezone = Timezone.Create(
            normalizedIanaId,
            request.DisplayName,
            request.IsActive,
            request.IsDefault,
            request.SortOrder);

        timezoneRepository.Add(timezone);

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
