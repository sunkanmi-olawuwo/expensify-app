using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Domain.Incomes;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.Application.Incomes.Command.DeleteIncome;

internal sealed class DeleteIncomeCommandHandler(
    IIncomeRepository incomeRepository,
    IDateTimeProvider dateTimeProvider,
    IIncomeUnitOfWork unitOfWork) : ICommandHandler<DeleteIncomeCommand>
{
    public async Task<Result> Handle(DeleteIncomeCommand request, CancellationToken cancellationToken)
    {
        IncomeEntity? income = await incomeRepository.GetByIdIncludingDeletedAsync(request.IncomeId, cancellationToken);
        if (income is null || income.UserId != request.UserId)
        {
            return Result.Failure(IncomeErrors.NotFound(request.IncomeId));
        }

        Result markDeletedResult = income.MarkDeleted(dateTimeProvider.UtcNow);
        if (markDeletedResult.IsFailure)
        {
            return Result.Failure(IncomeErrors.NotFound(request.IncomeId));
        }

        incomeRepository.Update(income);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
