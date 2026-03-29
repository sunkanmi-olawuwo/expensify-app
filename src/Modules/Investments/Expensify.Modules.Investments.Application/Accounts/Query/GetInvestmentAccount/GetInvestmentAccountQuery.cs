using Expensify.Common.Application.Messaging;
using Expensify.Modules.Investments.Application.Abstractions;

namespace Expensify.Modules.Investments.Application.Accounts.Query.GetInvestmentAccount;

public sealed record GetInvestmentAccountQuery(Guid UserId, Guid InvestmentId) : IQuery<InvestmentAccountResponse>;
