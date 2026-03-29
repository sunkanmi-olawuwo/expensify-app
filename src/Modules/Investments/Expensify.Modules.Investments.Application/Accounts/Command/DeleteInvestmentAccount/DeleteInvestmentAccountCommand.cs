using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Investments.Application.Accounts.Command.DeleteInvestmentAccount;

public sealed record DeleteInvestmentAccountCommand(Guid UserId, Guid InvestmentId) : ICommand;
