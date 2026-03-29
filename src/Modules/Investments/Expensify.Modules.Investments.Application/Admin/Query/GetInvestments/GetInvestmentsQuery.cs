using Expensify.Common.Application.Messaging;
using Expensify.Modules.Investments.Application.Abstractions;

namespace Expensify.Modules.Investments.Application.Admin.Query.GetInvestments;

public sealed record GetInvestmentsQuery(int Page, int PageSize) : IQuery<InvestmentAccountsPageResponse>;
