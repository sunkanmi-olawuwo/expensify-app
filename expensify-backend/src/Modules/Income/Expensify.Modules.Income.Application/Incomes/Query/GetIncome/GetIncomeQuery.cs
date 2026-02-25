using Expensify.Common.Application.Messaging;
using Expensify.Modules.Income.Application.Abstractions;

namespace Expensify.Modules.Income.Application.Incomes.Query.GetIncome;

public sealed record GetIncomeQuery(Guid UserId, Guid IncomeId) : IQuery<IncomeResponse>;
