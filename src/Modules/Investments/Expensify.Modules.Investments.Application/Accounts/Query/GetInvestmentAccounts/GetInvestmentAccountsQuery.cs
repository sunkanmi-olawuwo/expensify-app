using Expensify.Common.Application.Messaging;
using Expensify.Modules.Investments.Application.Abstractions;

namespace Expensify.Modules.Investments.Application.Accounts.Query.GetInvestmentAccounts;

public sealed record GetInvestmentAccountsQuery(Guid UserId, Guid? CategoryId, int Page, int PageSize)
    : IQuery<InvestmentAccountsPageResponse>;
