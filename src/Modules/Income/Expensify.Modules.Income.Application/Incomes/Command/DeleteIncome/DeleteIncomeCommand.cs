using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Income.Application.Incomes.Command.DeleteIncome;

public sealed record DeleteIncomeCommand(Guid UserId, Guid IncomeId) : ICommand;
