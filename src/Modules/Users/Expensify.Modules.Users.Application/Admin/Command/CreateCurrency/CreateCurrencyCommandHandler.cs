using System.Data.Common;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Domain.Currencies;
using Expensify.Modules.Users.Domain.Preferences;

namespace Expensify.Modules.Users.Application.Admin.Command.CreateCurrency;

internal sealed class CreateCurrencyCommandHandler(
    ICurrencyRepository currencyRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateCurrencyCommand, CurrencyResponse>
{
    public async Task<Result<CurrencyResponse>> Handle(CreateCurrencyCommand request, CancellationToken cancellationToken)
    {
        string normalizedCode = request.Code.Trim().ToUpperInvariant();

        Currency? existingCurrency = await currencyRepository.GetByIdAsync(normalizedCode, cancellationToken);
        if (existingCurrency is not null)
        {
            return Result.Failure<CurrencyResponse>(PreferenceCatalogErrors.CurrencyAlreadyExists(normalizedCode));
        }

        if (request.IsDefault && !request.IsActive)
        {
            return Result.Failure<CurrencyResponse>(PreferenceCatalogErrors.CurrencyMustRemainActiveWhenDefault());
        }

        Currency? currentDefault = await currencyRepository.GetDefaultAsync(cancellationToken);
        if (currentDefault is null && request.IsActive && !request.IsDefault)
        {
            return Result.Failure<CurrencyResponse>(PreferenceCatalogErrors.DefaultCurrencyRequired());
        }

        if (request.IsDefault && currentDefault is not null)
        {
            currentDefault.ClearDefault();
            currencyRepository.Update(currentDefault);
        }

        var currency = Currency.Create(
            normalizedCode,
            request.Name,
            request.Symbol,
            request.MinorUnit,
            request.IsActive,
            request.IsDefault,
            request.SortOrder);

        currencyRepository.Add(currency);

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
