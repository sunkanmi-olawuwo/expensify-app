using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Domain.Incomes;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.Application.Incomes.Command.RestoreIncome;

internal sealed class RestoreIncomeCommandHandler(
    IIncomeRepository incomeRepository,
    IIncomeUnitOfWork unitOfWork) : ICommandHandler<RestoreIncomeCommand>
{
    public async Task<Result> Handle(RestoreIncomeCommand request, CancellationToken cancellationToken)
    {
        IncomeEntity? income = await incomeRepository.GetByIdIncludingDeletedAsync(request.IncomeId, cancellationToken);
        if (income is null || income.UserId != request.UserId)
        {
            return Result.Failure(IncomeErrors.NotFound(request.IncomeId));
        }

        Result restoreResult = income.Restore();
        if (restoreResult.IsFailure)
        {
            return Result.Failure(IncomeErrors.NotFound(request.IncomeId));
        }

        incomeRepository.Update(income);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
