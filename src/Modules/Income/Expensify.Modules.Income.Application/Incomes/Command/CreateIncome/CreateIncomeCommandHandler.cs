using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Abstractions.Users;
using Expensify.Modules.Income.Domain.Incomes;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.Application.Incomes.Command.CreateIncome;

internal sealed class CreateIncomeCommandHandler(
    IIncomeRepository incomeRepository,
    IUserSettingsService userSettingsService,
    IIncomeUnitOfWork unitOfWork)
    : ICommandHandler<CreateIncomeCommand, IncomeResponse>
{
    public async Task<Result<IncomeResponse>> Handle(CreateIncomeCommand request, CancellationToken cancellationToken)
    {
        Result<UserSettingsResponse> userSettingsResult = await userSettingsService.GetSettingsAsync(request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<IncomeResponse>(userSettingsResult.Error);
        }

        Result<IncomeEntity> incomeResult = IncomeEntity.Create(
            request.UserId,
            new Money(request.Amount, request.Currency),
            new IncomeDate(request.Date),
            request.Source,
            request.Type,
            request.Note,
            userSettingsResult.Value.Currency);

        if (incomeResult.IsFailure)
        {
            return Result.Failure<IncomeResponse>(incomeResult.Error);
        }

        incomeRepository.Add(incomeResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new IncomeResponse(
            incomeResult.Value.Id,
            incomeResult.Value.UserId,
            incomeResult.Value.Amount,
            incomeResult.Value.Currency,
            incomeResult.Value.IncomeDate,
            incomeResult.Value.Source,
            incomeResult.Value.Type.ToString(),
            incomeResult.Value.Note);
    }
}
