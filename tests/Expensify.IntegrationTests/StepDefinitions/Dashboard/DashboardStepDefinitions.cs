using System.Globalization;
using Expensify.Api.Client;
using Reqnroll;

namespace Expensify.IntegrationTests.StepDefinitions.Dashboard;

[Binding]
[Scope(Feature = "Dashboard")]
public sealed class DashboardStepDefinitions(IExpensifyV1Client apiClient, ScenarioContext scenarioContext)
{
    private const string DashboardSummaryResponseKey = nameof(DashboardSummaryResponse);
    private const string DashboardCategoriesKey = "DashboardCategories";
    private const string DashboardCreatedTransactionIdsKey = "DashboardCreatedTransactionIds";

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

    [When(@"I request dashboard summary")]
    public async Task WhenIRequestDashboardSummary()
    {
        await ExecuteAsync(async () =>
        {
            DashboardSummaryResponse response = await apiClient.GetDashboardSummaryAsync();
            scenarioContext.Set(response, DashboardSummaryResponseKey);
        });
    }

    [Then(@"the dashboard summary request is successful")]
    public void ThenTheDashboardSummaryRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(DashboardSummaryResponseKey, out DashboardSummaryResponse? response), Is.True);
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
        DashboardMonthlyPerformanceItemResponse? item = GetResponse().MonthlyPerformance.SingleOrDefault(entry => entry.Month == month);

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

    private DashboardSummaryResponse GetResponse()
    {
        if (!TryGet(DashboardSummaryResponseKey, out DashboardSummaryResponse? response) || response is null)
        {
            throw new InvalidOperationException("Dashboard summary response is not available.");
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
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsed))
        {
            throw new ArgumentException($"Expected date in yyyy-MM-dd format but got '{date}'.", nameof(date));
        }

        return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
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
