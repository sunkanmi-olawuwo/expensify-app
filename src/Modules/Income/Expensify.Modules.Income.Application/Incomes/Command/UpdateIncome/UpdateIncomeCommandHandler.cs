using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Abstractions.Users;
using Expensify.Modules.Income.Domain.Incomes;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.Application.Incomes.Command.UpdateIncome;

internal sealed class UpdateIncomeCommandHandler(
    IIncomeRepository incomeRepository,
    IUserSettingsService userSettingsService,
    IIncomeUnitOfWork unitOfWork)
    : ICommandHandler<UpdateIncomeCommand, IncomeResponse>
{
    public async Task<Result<IncomeResponse>> Handle(UpdateIncomeCommand request, CancellationToken cancellationToken)
    {
        IncomeEntity? income = await incomeRepository.GetByIdAsync(request.IncomeId, cancellationToken);
        if (income is null || income.UserId != request.UserId)
        {
            return Result.Failure<IncomeResponse>(IncomeErrors.NotFound(request.IncomeId));
        }

        Result<UserSettingsResponse> userSettingsResult = await userSettingsService.GetSettingsAsync(request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<IncomeResponse>(userSettingsResult.Error);
        }

        Result updateResult = income.Update(
            new Money(request.Amount, request.Currency),
            new IncomeDate(request.Date),
            request.Source,
            request.Type,
            request.Note,
            userSettingsResult.Value.Currency);

        if (updateResult.IsFailure)
        {
            return Result.Failure<IncomeResponse>(updateResult.Error);
        }

        incomeRepository.Update(income);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new IncomeResponse(
            income.Id,
            income.UserId,
            income.Amount,
            income.Currency,
            income.IncomeDate,
            income.Source,
            income.Type.ToString(),
            income.Note);
    }
}
