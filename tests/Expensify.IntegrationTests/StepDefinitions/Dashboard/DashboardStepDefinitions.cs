using System.Globalization;
using Expensify.Api.Client;
using Reqnroll;

namespace Expensify.IntegrationTests.StepDefinitions.Dashboard;

[Binding]
[Scope(Feature = "Dashboard")]
public sealed class DashboardStepDefinitions(IExpensifyV1Client apiClient, ScenarioContext scenarioContext)
{
    private const string DashboardSummaryResponseKey = nameof(DashboardSummaryResponse);
    private const string DashboardCashFlowTrendResponseKey = nameof(DashboardCashFlowTrendResponse);
    private const string DashboardIncomeBreakdownResponseKey = nameof(DashboardIncomeBreakdownResponse);
    private const string DashboardCategoryComparisonResponseKey = nameof(DashboardCategoryComparisonResponse);
    private const string DashboardTopCategoriesResponseKey = nameof(DashboardTopCategoriesResponse);
    private const string DashboardInvestmentAllocationResponseKey = nameof(DashboardInvestmentAllocationResponse);
    private const string DashboardInvestmentTrendResponseKey = nameof(DashboardInvestmentTrendResponse);
    private const string DashboardCategoriesKey = "DashboardCategories";
    private const string DashboardCreatedTransactionIdsKey = "DashboardCreatedTransactionIds";
    private const string DashboardInvestmentAccountsKey = "DashboardInvestmentAccounts";

    [Given(@"I create dashboard expense category ""(.*)""")]
    public async Task GivenICreateDashboardExpenseCategory(string categoryName)
    {
        await ExecuteAsync(async () =>
        {
            string uniqueCategoryName = $"{categoryName}-{Guid.NewGuid():N}".ToLowerInvariant();
            ExpenseCategoryResponse response = await apiClient.CreateExpenseCategoryAsync(new CategoryBody(uniqueCategoryName));
            GetCategories()[categoryName] = response;
        });

        AssertRequestSucceeded();
    }

    [Given(@"I create a dashboard expense amount (.*) currency ""(.*)"" category ""(.*)"" merchant ""(.*)"" note ""(.*)"" payment method ""(.*)"" on ""(.*)""")]
    public async Task GivenICreateADashboardExpenseAmountCurrencyCategoryMerchantNotePaymentMethodOn(
        decimal amount,
        string currency,
        string categoryName,
        string merchant,
        string note,
        string paymentMethod,
        string date)
    {
        if (!GetCategories().TryGetValue(categoryName, out ExpenseCategoryResponse? category))
        {
            throw new InvalidOperationException($"Dashboard expense category '{categoryName}' has not been created.");
        }

        if (!Enum.TryParse(paymentMethod, true, out PaymentMethod parsedPaymentMethod))
        {
            throw new ArgumentException($"Unknown payment method '{paymentMethod}'.", nameof(paymentMethod));
        }

        DateTime expenseDate = ParseDate(date);

        await ExecuteAsync(async () =>
        {
            ExpenseResponse response = await apiClient.CreateExpenseWithoutTagIdsAsync(
                amount,
                category.Id,
                currency,
                expenseDate,
                merchant,
                note,
                parsedPaymentMethod);

            GetCreatedTransactionIds().Add(response.Id);
        });

        AssertRequestSucceeded();
    }

    [Given(@"I create a dashboard income amount (.*) currency ""(.*)"" source ""(.*)"" type ""(.*)"" note ""(.*)"" on ""(.*)""")]
    public async Task GivenICreateADashboardIncomeAmountCurrencySourceTypeNoteOn(
        decimal amount,
        string currency,
        string source,
        string type,
        string note,
        string date)
    {
        DateTime incomeDate = ParseDate(date);

        await ExecuteAsync(async () =>
        {
            IncomeResponse response = await apiClient.CreateIncomeAsync(new CreateIncomeRequest(
                amount,
                currency,
                incomeDate,
                note,
                source,
                ParseIncomeType(type)));

            GetCreatedTransactionIds().Add(response.Id);
        });

        AssertRequestSucceeded();
    }

    [Given(@"I create a dashboard investment account ""(.*)"" in category slug ""(.*)"" with current balance (.*)")]
    public async Task GivenICreateADashboardInvestmentAccountInCategorySlugWithCurrentBalance(
        string accountName,
        string categorySlug,
        decimal currentBalance)
    {
        await ExecuteAsync(async () =>
        {
            InvestmentCategoryResponse category = (await apiClient.GetInvestmentCategoriesAsync())
                .Single(item => item.Slug == categorySlug);

            InvestmentAccountResponse response = await apiClient.CreateInvestmentAccountAsync(
                new CreateInvestmentAccountRequest(
                    category.Id,
                    "GBP",
                    currentBalance,
                    null,
                    null,
                    accountName,
                    null,
                    "Dashboard Provider"));

            GetInvestmentAccounts()[accountName] = response;
        });

        AssertRequestSucceeded();
    }

    [Given(@"I add a dashboard investment contribution amount (.*) to account ""(.*)"" at ""(.*)"" note ""(.*)""")]
    public async Task GivenIAddADashboardInvestmentContributionAmountToAccountAtNote(
        decimal amount,
        string accountName,
        string timestamp,
        string note)
    {
        if (!GetInvestmentAccounts().TryGetValue(accountName, out InvestmentAccountResponse? account))
        {
            throw new InvalidOperationException($"Dashboard investment account '{accountName}' has not been created.");
        }

        DateTime contributionTimestamp = ParseTimestamp(timestamp);

        await ExecuteAsync(async () =>
        {
            await apiClient.CreateInvestmentContributionAsync(
                account.Id,
                new CreateInvestmentContributionRequest(amount, contributionTimestamp, note));
        });

        AssertRequestSucceeded();
    }

    [When(@"I request dashboard summary")]
    public async Task WhenIRequestDashboardSummary()
    {
        await ExecuteAsync(async () =>
        {
            DashboardSummaryResponse response = await apiClient.GetDashboardSummaryAsync();
            scenarioContext.Set(response, DashboardSummaryResponseKey);
        });
    }

    [When(@"I request dashboard cash flow trend with months (.*)")]
    public async Task WhenIRequestDashboardCashFlowTrendWithMonths(int months)
    {
        await ExecuteAsync(async () =>
        {
            DashboardCashFlowTrendResponse response = await apiClient.GetDashboardCashFlowTrendAsync(months);
            scenarioContext.Set(response, DashboardCashFlowTrendResponseKey);
        });
    }

    [When(@"I request dashboard income breakdown with months (.*)")]
    public async Task WhenIRequestDashboardIncomeBreakdownWithMonths(int months)
    {
        await ExecuteAsync(async () =>
        {
            DashboardIncomeBreakdownResponse response = await apiClient.GetDashboardIncomeBreakdownAsync(months);
            scenarioContext.Set(response, DashboardIncomeBreakdownResponseKey);
        });
    }

    [When(@"I request dashboard category comparison for month ""(.*)""")]
    public async Task WhenIRequestDashboardCategoryComparisonForMonth(string month)
    {
        await ExecuteAsync(async () =>
        {
            DashboardCategoryComparisonResponse response = await apiClient.GetDashboardCategoryComparisonAsync(ResolveMonthParam(month));
            scenarioContext.Set(response, DashboardCategoryComparisonResponseKey);
        });
    }

    [When(@"I request dashboard top categories with months (.*) and limit (.*)")]
    public async Task WhenIRequestDashboardTopCategoriesWithMonthsAndLimit(int months, int limit)
    {
        await ExecuteAsync(async () =>
        {
            DashboardTopCategoriesResponse response = await apiClient.GetDashboardTopCategoriesAsync(months, limit);
            scenarioContext.Set(response, DashboardTopCategoriesResponseKey);
        });
    }

    [When(@"I request dashboard investment allocation")]
    public async Task WhenIRequestDashboardInvestmentAllocation()
    {
        await ExecuteAsync(async () =>
        {
            DashboardInvestmentAllocationResponse response = await apiClient.GetDashboardInvestmentAllocationAsync();
            scenarioContext.Set(response, DashboardInvestmentAllocationResponseKey);
        });
    }

    [When(@"I request dashboard investment trend with months (.*)")]
    public async Task WhenIRequestDashboardInvestmentTrendWithMonths(int months)
    {
        await ExecuteAsync(async () =>
        {
            DashboardInvestmentTrendResponse response = await apiClient.GetDashboardInvestmentTrendAsync(months);
            scenarioContext.Set(response, DashboardInvestmentTrendResponseKey);
        });
    }

    [Then(@"the dashboard summary request is successful")]
    public void ThenTheDashboardSummaryRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(DashboardSummaryResponseKey, out DashboardSummaryResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
    }

    [Then(@"the dashboard cash flow trend request is successful")]
    public void ThenTheDashboardCashFlowTrendRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(DashboardCashFlowTrendResponseKey, out DashboardCashFlowTrendResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
    }

    [Then(@"the dashboard income breakdown request is successful")]
    public void ThenTheDashboardIncomeBreakdownRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(DashboardIncomeBreakdownResponseKey, out DashboardIncomeBreakdownResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
    }

    [Then(@"the dashboard category comparison request is successful")]
    public void ThenTheDashboardCategoryComparisonRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(DashboardCategoryComparisonResponseKey, out DashboardCategoryComparisonResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
    }

    [Then(@"the dashboard top categories request is successful")]
    public void ThenTheDashboardTopCategoriesRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(DashboardTopCategoriesResponseKey, out DashboardTopCategoriesResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
    }

    [Then(@"the dashboard investment allocation request is successful")]
    public void ThenTheDashboardInvestmentAllocationRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(DashboardInvestmentAllocationResponseKey, out DashboardInvestmentAllocationResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
    }

    [Then(@"the dashboard investment trend request is successful")]
    public void ThenTheDashboardInvestmentTrendRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(DashboardInvestmentTrendResponseKey, out DashboardInvestmentTrendResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
    }

    [Then(@"dashboard monthly income is (.*) currency ""(.*)"" with change (.*)")]
    public void ThenDashboardMonthlyIncomeIsCurrencyWithChange(decimal totalAmount, string currency, decimal changePercentage)
    {
        AssertMetric(GetResponse().MonthlyIncome, totalAmount, currency, changePercentage);
    }

    [Then(@"dashboard monthly expenses is (.*) currency ""(.*)"" with change (.*)")]
    public void ThenDashboardMonthlyExpensesIsCurrencyWithChange(decimal totalAmount, string currency, decimal changePercentage)
    {
        AssertMetric(GetResponse().MonthlyExpenses, totalAmount, currency, changePercentage);
    }

    [Then(@"dashboard net cash flow is (.*) currency ""(.*)"" with change (.*)")]
    public void ThenDashboardNetCashFlowIsCurrencyWithChange(decimal totalAmount, string currency, decimal changePercentage)
    {
        AssertMetric(GetResponse().NetCashFlow, totalAmount, currency, changePercentage);
    }

    [Then(@"dashboard spending breakdown contains (.*) entries")]
    public void ThenDashboardSpendingBreakdownContainsEntries(int expectedCount)
    {
        Assert.That(GetResponse().SpendingBreakdown, Has.Count.EqualTo(expectedCount));
    }

    [Then(@"dashboard spending breakdown contains category ""(.*)"" amount (.*) percentage (.*) color key ""(.*)""")]
    public void ThenDashboardSpendingBreakdownContainsCategoryAmountPercentageColorKey(
        string category,
        decimal amount,
        decimal percentage,
        string colorKey)
    {
        string expectedCategory = GetCategories().TryGetValue(category, out ExpenseCategoryResponse? createdCategory)
            ? createdCategory.Name
            : category;

        DashboardSpendingBreakdownItemResponse? item = GetResponse().SpendingBreakdown.SingleOrDefault(entry => entry.Category == expectedCategory);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item, Is.Not.Null);
            if (item is not null)
            {
                Assert.That(item.Amount, Is.EqualTo(amount));
                Assert.That(item.Percentage, Is.EqualTo(percentage));
                Assert.That(item.ColorKey, Is.EqualTo(colorKey));
            }
        }
    }

    [Then(@"dashboard spending breakdown is empty")]
    public void ThenDashboardSpendingBreakdownIsEmpty()
    {
        Assert.That(GetResponse().SpendingBreakdown, Is.Empty);
    }

    [Then(@"dashboard monthly performance contains (.*) entries")]
    public void ThenDashboardMonthlyPerformanceContainsEntries(int expectedCount)
    {
        Assert.That(GetResponse().MonthlyPerformance, Has.Count.EqualTo(expectedCount));
    }

    [Then(@"dashboard monthly performance month ""(.*)"" has income (.*) and expenses (.*)")]
    public void ThenDashboardMonthlyPerformanceMonthHasIncomeAndExpenses(string month, decimal income, decimal expenses)
    {
        string resolvedMonth = ResolveMonthLabel(month);
        DashboardMonthlyPerformanceItemResponse? item = GetResponse().MonthlyPerformance.SingleOrDefault(entry => entry.Month == resolvedMonth);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item, Is.Not.Null);
            Assert.That(item!.Income, Is.EqualTo(income));
            Assert.That(item.Expenses, Is.EqualTo(expenses));
        }
    }

    [Then(@"dashboard monthly performance contains 6 zeroed entries")]
    public void ThenDashboardMonthlyPerformanceContainsSixZeroedEntries()
    {
        DashboardMonthlyPerformanceItemResponse[] items = GetResponse().MonthlyPerformance.ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(items, Has.Length.EqualTo(6));
            Assert.That(items.All(item => item.Income == 0m && item.Expenses == 0m), Is.True);
        }
    }

    [Then(@"dashboard recent transactions are empty")]
    public void ThenDashboardRecentTransactionsAreEmpty()
    {
        Assert.That(GetResponse().RecentTransactions, Is.Empty);
    }

    [Then(@"dashboard recent transactions contain (.*) entries")]
    public void ThenDashboardRecentTransactionsContainEntries(int expectedCount)
    {
        Assert.That(GetResponse().RecentTransactions, Has.Count.EqualTo(expectedCount));
    }

    [Then(@"dashboard recent transactions are ordered newest first")]
    public void ThenDashboardRecentTransactionsAreOrderedNewestFirst()
    {
        DashboardRecentTransactionResponse[] items = GetResponse().RecentTransactions.ToArray();
        bool isOrderedNewestFirst = items
            .Zip(items.Skip(1), (current, next) => current.Timestamp >= next.Timestamp)
            .All(isOrdered => isOrdered);

        Assert.That(isOrderedNewestFirst, Is.True);
    }

    [Then(@"dashboard recent transactions do not include the created dashboard transaction ids")]
    public void ThenDashboardRecentTransactionsDoNotIncludeTheCreatedDashboardTransactionIds()
    {
        HashSet<Guid> createdIds = GetCreatedTransactionIds();
        var responseIds = GetResponse().RecentTransactions.Select(item => item.Id).ToHashSet();

        Assert.That(responseIds.Overlaps(createdIds), Is.False);
    }

    [Then(@"dashboard cash flow trend contains (.*) months")]
    public void ThenDashboardCashFlowTrendContainsMonths(int expectedCount)
    {
        Assert.That(GetCashFlowTrendResponse().Months, Has.Count.EqualTo(expectedCount));
    }

    [Then(@"dashboard cash flow month ""(.*)"" has income (.*) expenses (.*) net cash flow (.*) savings rate (.*)")]
    public void ThenDashboardCashFlowMonthHasIncomeExpensesNetCashFlowSavingsRate(
        string label,
        decimal income,
        decimal expenses,
        decimal netCashFlow,
        decimal savingsRate)
    {
        string resolvedLabel = ResolveMonthLabel(label);
        DashboardCashFlowTrendMonthResponse? item = GetCashFlowTrendResponse().Months.SingleOrDefault(entry => entry.Label == resolvedLabel);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item, Is.Not.Null);
            Assert.That(item!.Income, Is.EqualTo(income));
            Assert.That(item.Expenses, Is.EqualTo(expenses));
            Assert.That(item.NetCashFlow, Is.EqualTo(netCashFlow));
            Assert.That(item.SavingsRate, Is.EqualTo(savingsRate));
        }
    }

    [Then(@"dashboard cash flow trend contains (.*) zeroed months")]
    public void ThenDashboardCashFlowTrendContainsZeroedMonths(int expectedCount)
    {
        DashboardCashFlowTrendMonthResponse[] items = GetCashFlowTrendResponse().Months.ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(items, Has.Length.EqualTo(expectedCount));
            Assert.That(items.All(item => item.Income == 0m && item.Expenses == 0m && item.NetCashFlow == 0m && item.SavingsRate == 0m), Is.True);
        }
    }

    [Then(@"dashboard income breakdown period is ""(.*)"" currency ""(.*)"" total income (.*)")]
    public void ThenDashboardIncomeBreakdownPeriodIsCurrencyTotalIncome(string period, string currency, decimal totalIncome)
    {
        DashboardIncomeBreakdownResponse response = GetIncomeBreakdownResponse();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Period, Is.EqualTo(period));
            Assert.That(response.Currency, Is.EqualTo(currency));
            Assert.That(response.TotalIncome, Is.EqualTo(totalIncome));
        }
    }

    [Then(@"dashboard income breakdown contains (.*) sources")]
    public void ThenDashboardIncomeBreakdownContainsSources(int expectedCount)
    {
        Assert.That(GetIncomeBreakdownResponse().Sources, Has.Count.EqualTo(expectedCount));
    }

    [Then(@"dashboard income breakdown contains source ""(.*)"" amount (.*) percentage (.*) color key ""(.*)""")]
    public void ThenDashboardIncomeBreakdownContainsSourceAmountPercentageColorKey(
        string source,
        decimal amount,
        decimal percentage,
        string colorKey)
    {
        DashboardIncomeBreakdownSourceResponse? item = GetIncomeBreakdownResponse().Sources.SingleOrDefault(entry => entry.Source == source);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item, Is.Not.Null);
            Assert.That(item!.Amount, Is.EqualTo(amount));
            Assert.That(item.Percentage, Is.EqualTo(percentage));
            Assert.That(item.ColorKey, Is.EqualTo(colorKey));
        }
    }

    [Then(@"dashboard income breakdown sources are empty")]
    public void ThenDashboardIncomeBreakdownSourcesAreEmpty()
    {
        Assert.That(GetIncomeBreakdownResponse().Sources, Is.Empty);
    }

    [Then(@"dashboard category comparison current month ""(.*)"" previous month ""(.*)"" currency ""(.*)""")]
    public void ThenDashboardCategoryComparisonCurrentMonthPreviousMonthCurrency(string currentMonth, string previousMonth, string currency)
    {
        DashboardCategoryComparisonResponse response = GetCategoryComparisonResponse();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.CurrentMonth, Is.EqualTo(ResolveMonthLabel(currentMonth)));
            Assert.That(response.PreviousMonth, Is.EqualTo(ResolveMonthLabel(previousMonth)));
            Assert.That(response.Currency, Is.EqualTo(currency));
        }
    }

    [Then(@"dashboard category comparison contains (.*) categories")]
    public void ThenDashboardCategoryComparisonContainsCategories(int expectedCount)
    {
        Assert.That(GetCategoryComparisonResponse().Categories, Has.Count.EqualTo(expectedCount));
    }

    [Then(@"dashboard category comparison contains category ""(.*)"" current amount (.*) previous amount (.*) change amount (.*) change percentage (.*)")]
    public void ThenDashboardCategoryComparisonContainsCategoryCurrentAmountPreviousAmountChangeAmountChangePercentage(
        string category,
        decimal currentAmount,
        decimal previousAmount,
        decimal changeAmount,
        decimal changePercentage)
    {
        string expectedCategory = ResolveCategoryName(category);
        DashboardCategoryComparisonItemResponse? item = GetCategoryComparisonResponse().Categories.SingleOrDefault(entry => entry.Category == expectedCategory);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item, Is.Not.Null);
            Assert.That(item!.CurrentAmount, Is.EqualTo(currentAmount));
            Assert.That(item.PreviousAmount, Is.EqualTo(previousAmount));
            Assert.That(item.ChangeAmount, Is.EqualTo(changeAmount));
            Assert.That(item.ChangePercentage, Is.EqualTo(changePercentage));
        }
    }

    [Then(@"dashboard category comparison categories are empty")]
    public void ThenDashboardCategoryComparisonCategoriesAreEmpty()
    {
        Assert.That(GetCategoryComparisonResponse().Categories, Is.Empty);
    }

    [Then(@"dashboard top categories period ""(.*)"" currency ""(.*)"" total spent (.*)")]
    public void ThenDashboardTopCategoriesPeriodCurrencyTotalSpent(string period, string currency, decimal totalSpent)
    {
        DashboardTopCategoriesResponse response = GetTopCategoriesResponse();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Period, Is.EqualTo(period));
            Assert.That(response.Currency, Is.EqualTo(currency));
            Assert.That(response.TotalSpent, Is.EqualTo(totalSpent));
        }
    }

    [Then(@"dashboard top categories contains (.*) entries")]
    public void ThenDashboardTopCategoriesContainsEntries(int expectedCount)
    {
        Assert.That(GetTopCategoriesResponse().Categories, Has.Count.EqualTo(expectedCount));
    }

    [Then(@"dashboard top categories contains rank (.*) category ""(.*)"" amount (.*) percentage (.*) color key ""(.*)""")]
    public void ThenDashboardTopCategoriesContainsRankCategoryAmountPercentageColorKey(
        int rank,
        string category,
        decimal amount,
        decimal percentage,
        string colorKey)
    {
        string expectedCategory = ResolveCategoryName(category);
        DashboardTopCategoryResponse? item = GetTopCategoriesResponse().Categories.SingleOrDefault(entry => entry.Category == expectedCategory);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item, Is.Not.Null);
            Assert.That(item!.Rank, Is.EqualTo(rank));
            Assert.That(item.Amount, Is.EqualTo(amount));
            Assert.That(item.Percentage, Is.EqualTo(percentage));
            Assert.That(item.ColorKey, Is.EqualTo(colorKey));
        }
    }

    [Then(@"dashboard top categories are empty")]
    public void ThenDashboardTopCategoriesAreEmpty()
    {
        Assert.That(GetTopCategoriesResponse().Categories, Is.Empty);
    }

    [Then(@"dashboard investment allocation currency ""(.*)"" total value (.*) account count (.*)")]
    public void ThenDashboardInvestmentAllocationCurrencyTotalValueAccountCount(string currency, decimal totalValue, int accountCount)
    {
        DashboardInvestmentAllocationResponse response = GetInvestmentAllocationResponse();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Currency, Is.EqualTo(currency));
            Assert.That(response.TotalValue, Is.EqualTo(totalValue));
            Assert.That(response.AccountCount, Is.EqualTo(accountCount));
        }
    }

    [Then(@"dashboard investment allocation contains (.*) categories")]
    public void ThenDashboardInvestmentAllocationContainsCategories(int expectedCount)
    {
        Assert.That(GetInvestmentAllocationResponse().Categories, Has.Count.EqualTo(expectedCount));
    }

    [Then(@"dashboard investment allocation contains category ""(.*)"" slug ""(.*)"" total balance (.*) account count (.*) percentage (.*) color key ""(.*)""")]
    public void ThenDashboardInvestmentAllocationContainsCategorySlugTotalBalanceAccountCountPercentageColorKey(
        string categoryName,
        string slug,
        decimal totalBalance,
        int accountCount,
        decimal percentage,
        string colorKey)
    {
        DashboardInvestmentAllocationCategoryResponse? item = GetInvestmentAllocationResponse().Categories
            .SingleOrDefault(entry => entry.CategoryName == categoryName);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item, Is.Not.Null);
            Assert.That(item!.CategorySlug, Is.EqualTo(slug));
            Assert.That(item.TotalBalance, Is.EqualTo(totalBalance));
            Assert.That(item.AccountCount, Is.EqualTo(accountCount));
            Assert.That(item.Percentage, Is.EqualTo(percentage));
            Assert.That(item.ColorKey, Is.EqualTo(colorKey));
        }
    }

    [Then(@"dashboard investment allocation categories are empty")]
    public void ThenDashboardInvestmentAllocationCategoriesAreEmpty()
    {
        Assert.That(GetInvestmentAllocationResponse().Categories, Is.Empty);
    }

    [Then(@"dashboard investment trend currency ""(.*)"" total contributed (.*)")]
    public void ThenDashboardInvestmentTrendCurrencyTotalContributed(string currency, decimal totalContributed)
    {
        DashboardInvestmentTrendResponse response = GetInvestmentTrendResponse();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Currency, Is.EqualTo(currency));
            Assert.That(response.TotalContributed, Is.EqualTo(totalContributed));
        }
    }

    [Then(@"dashboard investment trend contains (.*) months")]
    public void ThenDashboardInvestmentTrendContainsMonths(int expectedCount)
    {
        Assert.That(GetInvestmentTrendResponse().Months, Has.Count.EqualTo(expectedCount));
    }

    [Then(@"dashboard investment trend month ""(.*)"" has contributions (.*) and account count (.*)")]
    public void ThenDashboardInvestmentTrendMonthHasContributionsAndAccountCount(string label, decimal contributions, int accountCount)
    {
        string resolvedLabel = ResolveMonthLabel(label);
        DashboardInvestmentTrendMonthResponse? item = GetInvestmentTrendResponse().Months.SingleOrDefault(entry => entry.Label == resolvedLabel);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item, Is.Not.Null);
            Assert.That(item!.Contributions, Is.EqualTo(contributions));
            Assert.That(item.ContributingAccountCount, Is.EqualTo(accountCount));
        }
    }

    [Then(@"dashboard investment trend contains (.*) zeroed months")]
    public void ThenDashboardInvestmentTrendContainsZeroedMonths(int expectedCount)
    {
        DashboardInvestmentTrendMonthResponse[] items = GetInvestmentTrendResponse().Months.ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(items, Has.Length.EqualTo(expectedCount));
            Assert.That(items.All(item => item.Contributions == 0m && item.ContributingAccountCount == 0), Is.True);
        }
    }

    private DashboardSummaryResponse GetResponse()
    {
        if (!TryGet(DashboardSummaryResponseKey, out DashboardSummaryResponse? response) || response is null)
        {
            throw new InvalidOperationException("Dashboard summary response is not available.");
        }

        return response;
    }

    private DashboardCashFlowTrendResponse GetCashFlowTrendResponse()
    {
        if (!TryGet(DashboardCashFlowTrendResponseKey, out DashboardCashFlowTrendResponse? response) || response is null)
        {
            throw new InvalidOperationException("Dashboard cash flow trend response is not available.");
        }

        return response;
    }

    private DashboardIncomeBreakdownResponse GetIncomeBreakdownResponse()
    {
        if (!TryGet(DashboardIncomeBreakdownResponseKey, out DashboardIncomeBreakdownResponse? response) || response is null)
        {
            throw new InvalidOperationException("Dashboard income breakdown response is not available.");
        }

        return response;
    }

    private DashboardCategoryComparisonResponse GetCategoryComparisonResponse()
    {
        if (!TryGet(DashboardCategoryComparisonResponseKey, out DashboardCategoryComparisonResponse? response) || response is null)
        {
            throw new InvalidOperationException("Dashboard category comparison response is not available.");
        }

        return response;
    }

    private DashboardTopCategoriesResponse GetTopCategoriesResponse()
    {
        if (!TryGet(DashboardTopCategoriesResponseKey, out DashboardTopCategoriesResponse? response) || response is null)
        {
            throw new InvalidOperationException("Dashboard top categories response is not available.");
        }

        return response;
    }

    private DashboardInvestmentAllocationResponse GetInvestmentAllocationResponse()
    {
        if (!TryGet(DashboardInvestmentAllocationResponseKey, out DashboardInvestmentAllocationResponse? response) || response is null)
        {
            throw new InvalidOperationException("Dashboard investment allocation response is not available.");
        }

        return response;
    }

    private DashboardInvestmentTrendResponse GetInvestmentTrendResponse()
    {
        if (!TryGet(DashboardInvestmentTrendResponseKey, out DashboardInvestmentTrendResponse? response) || response is null)
        {
            throw new InvalidOperationException("Dashboard investment trend response is not available.");
        }

        return response;
    }

    private Dictionary<string, ExpenseCategoryResponse> GetCategories()
    {
        if (scenarioContext.TryGetValue(DashboardCategoriesKey, out object? stored) &&
            stored is Dictionary<string, ExpenseCategoryResponse> categories)
        {
            return categories;
        }

        Dictionary<string, ExpenseCategoryResponse> created = new(StringComparer.Ordinal);
        scenarioContext.Set(created, DashboardCategoriesKey);
        return created;
    }

    private HashSet<Guid> GetCreatedTransactionIds()
    {
        if (scenarioContext.TryGetValue(DashboardCreatedTransactionIdsKey, out object? stored) &&
            stored is HashSet<Guid> ids)
        {
            return ids;
        }

        HashSet<Guid> created = [];
        scenarioContext.Set(created, DashboardCreatedTransactionIdsKey);
        return created;
    }

    private Dictionary<string, InvestmentAccountResponse> GetInvestmentAccounts()
    {
        if (scenarioContext.TryGetValue(DashboardInvestmentAccountsKey, out object? stored) &&
            stored is Dictionary<string, InvestmentAccountResponse> accounts)
        {
            return accounts;
        }

        Dictionary<string, InvestmentAccountResponse> created = new(StringComparer.Ordinal);
        scenarioContext.Set(created, DashboardInvestmentAccountsKey);
        return created;
    }

    private string ResolveCategoryName(string category)
    {
        return GetCategories().TryGetValue(category, out ExpenseCategoryResponse? createdCategory)
            ? createdCategory.Name
            : category;
    }

    private static void AssertMetric(DashboardMetricResponse metric, decimal totalAmount, string currency, decimal changePercentage)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(metric.TotalAmount, Is.EqualTo(totalAmount));
            Assert.That(metric.Currency, Is.EqualTo(currency));
            Assert.That(metric.ChangePercentage, Is.EqualTo(changePercentage));
        }
    }

    private async Task ExecuteAsync(Func<Task> action)
    {
        ResetExceptions();

        try
        {
            await action();
        }
        catch (SwaggerException ex)
        {
            scenarioContext.Set(ex, nameof(SwaggerException));
        }
        catch (Exception ex)
        {
            scenarioContext.Set(ex, "UnexpectedException");
        }
    }

    private void AssertRequestSucceeded()
    {
        TryGet(nameof(SwaggerException), out SwaggerException? swaggerException);
        TryGet("UnexpectedException", out Exception? unexpectedException);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unexpectedException, Is.Null, unexpectedException?.ToString());
            Assert.That(swaggerException, Is.Null, swaggerException?.Response);
        }
    }

    private void ResetExceptions()
    {
        scenarioContext.Remove(nameof(SwaggerException));
        scenarioContext.Remove("UnexpectedException");
    }

    private bool TryGet<T>(string key, out T? value)
    {
        if (scenarioContext.TryGetValue(key, out object? stored) && stored is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    private static DateTime ParseDate(string date)
    {
        string resolved = ResolveRelativeDate(date);

        if (!DateTime.TryParseExact(resolved, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsed))
        {
            throw new ArgumentException($"Expected date in yyyy-MM-dd or ~N/DD format but got '{date}'.", nameof(date));
        }

        return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
    }

    private static DateTime ParseTimestamp(string timestamp)
    {
        string resolved = ResolveRelativeTimestamp(timestamp);

        if (DateTime.TryParseExact(
                resolved,
                ["yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss.fffffffZ", "O"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        throw new ArgumentException($"Expected timestamp in ISO 8601, yyyy-MM-dd, or ~N/DDThh:mm:ssZ format but got '{timestamp}'.", nameof(timestamp));
    }

    /// <summary>
    /// Resolves relative date tokens: ~N/DD means "N months before the current UTC month, day DD".
    /// Example: ~0/10 on 2026-04-03 → 2026-04-10, ~1/10 → 2026-03-10, ~2/05 → 2026-02-05.
    /// </summary>
    private static string ResolveRelativeDate(string input)
    {
        if (!input.StartsWith('~'))
        {
            return input;
        }

        int slashIndex = input.IndexOf('/');
        if (slashIndex < 0)
        {
            throw new ArgumentException($"Relative date must use ~N/DD format but got '{input}'.", nameof(input));
        }

        int monthsBack = int.Parse(input[1..slashIndex], CultureInfo.InvariantCulture);
        int day = int.Parse(input[(slashIndex + 1)..], CultureInfo.InvariantCulture);
        DateOnly baseMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-monthsBack);
        int clampedDay = Math.Min(day, DateTime.DaysInMonth(baseMonth.Year, baseMonth.Month));

        return new DateOnly(baseMonth.Year, baseMonth.Month, clampedDay).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Resolves relative timestamp tokens: ~N/DDThh:mm:ssZ means "N months back, day DD, time hh:mm:ssZ".
    /// Example: ~2/28T23:30:00Z on 2026-04-03 → 2026-02-28T23:30:00Z.
    /// </summary>
    private static string ResolveRelativeTimestamp(string input)
    {
        if (!input.StartsWith('~'))
        {
            return input;
        }

        int slashIndex = input.IndexOf('/');
        if (slashIndex < 0)
        {
            throw new ArgumentException($"Relative timestamp must use ~N/DDThh:mm:ssZ format but got '{input}'.", nameof(input));
        }

        int monthsBack = int.Parse(input[1..slashIndex], CultureInfo.InvariantCulture);
        string remainder = input[(slashIndex + 1)..];

        int dayEnd = remainder.IndexOf('T');
        if (dayEnd < 0)
        {
            dayEnd = remainder.Length;
        }

        int day = int.Parse(remainder[..dayEnd], CultureInfo.InvariantCulture);
        string timePart = dayEnd < remainder.Length ? remainder[dayEnd..] : "T00:00:00Z";
        DateOnly baseMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-monthsBack);
        int clampedDay = Math.Min(day, DateTime.DaysInMonth(baseMonth.Year, baseMonth.Month));
        string datePart = new DateOnly(baseMonth.Year, baseMonth.Month, clampedDay).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return $"{datePart}{timePart}";
    }

    /// <summary>
    /// Resolves a month label token: ~N means "the display label N months before the current UTC month".
    /// Example: ~0 on 2026-04-03 → "Apr 2026", ~1 → "Mar 2026".
    /// </summary>
    private static string ResolveMonthLabel(string input)
    {
        if (!input.StartsWith('~'))
        {
            return input;
        }

        int monthsBack = int.Parse(input[1..], CultureInfo.InvariantCulture);
        DateOnly baseMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-monthsBack);

        return baseMonth.ToString("MMM yyyy", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Resolves a month parameter token: ~N means "yyyy-MM format N months before the current UTC month".
    /// Example: ~1 on 2026-04-03 → "2026-03".
    /// </summary>
    private static string ResolveMonthParam(string input)
    {
        if (!input.StartsWith('~'))
        {
            return input;
        }

        int monthsBack = int.Parse(input[1..], CultureInfo.InvariantCulture);
        DateOnly baseMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-monthsBack);

        return baseMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    }

    private static IncomeType ParseIncomeType(string type)
    {
        if (Enum.TryParse(type, true, out IncomeType parsed))
        {
            return parsed;
        }

        throw new ArgumentException($"Unsupported income type '{type}'.", nameof(type));
    }
}
