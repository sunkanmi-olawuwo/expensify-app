using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Income.Application.Incomes.Command.RestoreIncome;

public sealed record RestoreIncomeCommand(Guid UserId, Guid IncomeId) : ICommand;