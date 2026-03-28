using System.Data.Common;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Domain.Currencies;
using Expensify.Modules.Users.Domain.Preferences;

namespace Expensify.Modules.Users.Application.Admin.Command.UpdateCurrency;

internal sealed class UpdateCurrencyCommandHandler(
    ICurrencyRepository currencyRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateCurrencyCommand, CurrencyResponse>
{
    public async Task<Result<CurrencyResponse>> Handle(UpdateCurrencyCommand request, CancellationToken cancellationToken)
    {
        string normalizedCode = request.Code.Trim().ToUpperInvariant();

        Currency? currency = await currencyRepository.GetByIdAsync(normalizedCode, cancellationToken);
        if (currency is null)
        {
            return Result.Failure<CurrencyResponse>(PreferenceCatalogErrors.CurrencyNotFound(normalizedCode));
        }

        if (request.IsDefault && !request.IsActive)
        {
            return Result.Failure<CurrencyResponse>(PreferenceCatalogErrors.CurrencyMustRemainActiveWhenDefault());
        }

        Currency? currentDefault = await currencyRepository.GetDefaultAsync(cancellationToken);

        if (currency.IsDefault && (!request.IsDefault || !request.IsActive))
        {
            return Result.Failure<CurrencyResponse>(PreferenceCatalogErrors.DefaultCurrencyRequired());
        }

        if (currentDefault is null && request.IsActive && !request.IsDefault)
        {
            return Result.Failure<CurrencyResponse>(PreferenceCatalogErrors.DefaultCurrencyRequired());
        }

        if (request.IsDefault && currentDefault is not null && !string.Equals(currentDefault.Code, currency.Code, StringComparison.Ordinal))
        {
            currentDefault.ClearDefault();
            currencyRepository.Update(currentDefault);
        }

        currency.Update(
            request.Name,
            request.Symbol,
            request.MinorUnit,
            request.IsActive,
            request.IsDefault,
            request.SortOrder);

        currencyRepository.Update(currency);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbException)
        {
            return Result.Failure<CurrencyResponse>(PreferenceCatalogErrors.DefaultCurrencyConflict());
        }

        return new CurrencyResponse(
            currency.Code,
            currency.Name,
            currency.Symbol,
            currency.MinorUnit,
            currency.IsActive,
            currency.IsDefault,
            currency.SortOrder);
    }
}
