using System.Globalization;
using Reqnroll;
using Expensify.Api.Client;

namespace Expensify.IntegrationTests.StepDefinitions.Expenses;

[Binding]
[Scope(Feature = "Expenses")]
public sealed class ExpensesStepDefinitions(IExpensifyV1Client apiClient, ScenarioContext scenarioContext)
{
    private const string CreateExpenseResponseKey = "CreateExpenseResponse";
    private const string GetExpenseResponseKey = "GetExpenseResponse";
    private const string UpdateExpenseResponseKey = "UpdateExpenseResponse";
    private const string ExpensesPageResponseKey = nameof(ExpensesPageResponse);
    private const string MonthlySummaryResponseKey = nameof(MonthlyExpensesSummaryResponse);
    private const string AdminMonthlySummaryResponseKey = "AdminMonthlySummaryResponse";
    private const string CategoryResponseKey = nameof(ExpenseCategoryResponse);
    private const string TagResponseKey = nameof(ExpenseTagResponse);
    private const string CapturedUserIdKey = "CapturedUserId";
    private const string PaginationHeadersKey = "ExpensePaginationHeaders";

    [AfterScenario]
    public void AfterScenario()
    {
        scenarioContext.Remove(PaginationHeadersKey);
    }

    [Given(@"I create expense category ""(.*)""")]
    public async Task GivenICreateExpenseCategory(string categoryName)
    {
        await ExecuteAsync(async () =>
        {
            string uniqueCategoryName = $"{categoryName}-{Guid.NewGuid():N}".ToLowerInvariant();
            ExpenseCategoryResponse response = await apiClient.CreateExpenseCategoryAsync(new CategoryBody(uniqueCategoryName));
            scenarioContext.Set(response, CategoryResponseKey);
        });

        AssertRequestSucceeded();
    }

    [Given(@"I create expense tag ""(.*)""")]
    public async Task GivenICreateExpenseTag(string tagName)
    {
        await ExecuteAsync(async () =>
        {
            string uniqueTagName = $"{tagName}-{Guid.NewGuid():N}".ToLowerInvariant();
            ExpenseTagResponse response = await apiClient.CreateExpenseTagAsync(new TagBody(uniqueTagName));
            scenarioContext.Set(response, TagResponseKey);
        });

        AssertRequestSucceeded();
    }

    [Given(@"I create expenses for filtering in period ""(.*)""")]
    public async Task GivenICreateExpensesForFilteringInPeriod(string period)
    {
        if (!TryGet(CategoryResponseKey, out ExpenseCategoryResponse? category) || category is null)
        {
            throw new InvalidOperationException("An expense category must be created before creating filter fixtures.");
        }

        if (!TryGet(TagResponseKey, out ExpenseTagResponse? tag) || tag is null)
        {
            throw new InvalidOperationException("An expense tag must be created before creating filter fixtures.");
        }

        DateTime expenseDate = BuildExpenseDate(period);

        await ExecuteAsync(async () =>
        {
            List<CreateExpenseRequest> requests =
            [
                new CreateExpenseRequest(25.00m, category.Id, "GBP", expenseDate, "Tesco", "Groceries", PaymentMethod.Card, [tag.Id]),
                new CreateExpenseRequest(30.00m, category.Id, "GBP", expenseDate, "Tesco Metro", "Top-up", PaymentMethod.Card, [tag.Id]),
                new CreateExpenseRequest(12.00m, category.Id, "GBP", expenseDate, "Shell", "Fuel", PaymentMethod.Card, [tag.Id])
            ];

            foreach (CreateExpenseRequest request in requests)
            {
                await apiClient.CreateExpenseAsync(request);
            }
        });

        AssertRequestSucceeded();
    }

    [Given(@"I capture my current user id")]
    public async Task GivenICaptureMyCurrentUserId()
    {
        await ExecuteAsync(async () =>
        {
            GetUserResponse response = await apiClient.GetUserProfileAsync(Guid.NewGuid());
            scenarioContext.Set(response.Id, CapturedUserIdKey);
        });

        AssertRequestSucceeded();
    }

    [Given(@"I create an expense amount (.*) currency ""(.*)"" merchant ""(.*)"" note ""(.*)"" payment method ""(.*)""")]
    [When(@"I create an expense amount (.*) currency ""(.*)"" merchant ""(.*)"" note ""(.*)"" payment method ""(.*)""")]
    public async Task WhenICreateAnExpenseAmountCurrencyMerchantNotePaymentMethod(
        decimal amount,
        string currency,
        string merchant,
        string note,
        string paymentMethod)
    {
        if (!TryGet(CategoryResponseKey, out ExpenseCategoryResponse? category) || category is null)
        {
            throw new InvalidOperationException("An expense category must be created before creating expenses.");
        }

        if (!TryGet(TagResponseKey, out ExpenseTagResponse? tag) || tag is null)
        {
            throw new InvalidOperationException("An expense tag must be created before creating expenses.");
        }

        if (!Enum.TryParse(paymentMethod, true, out PaymentMethod parsedPaymentMethod))
        {
            throw new ArgumentException($"Unknown payment method '{paymentMethod}'.", nameof(paymentMethod));
        }

        DateTime expenseDate = new(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);

        await ExecuteAsync(async () =>
        {
            CreateExpenseRequest request = new(
                amount,
                category.Id,
                currency,
                expenseDate,
                merchant,
                note,
                parsedPaymentMethod,
                [tag.Id]);

            ExpenseResponse response = await apiClient.CreateExpenseAsync(request);
            scenarioContext.Set(response, CreateExpenseResponseKey);
        });
    }

    [When(@"I fetch the created expense")]
    public async Task WhenIFetchTheCreatedExpense()
    {
        if (!TryGet(CreateExpenseResponseKey, out ExpenseResponse? createdExpense) || createdExpense is null)
        {
            throw new InvalidOperationException("No created expense is available.");
        }

        await ExecuteAsync(async () =>
        {
            ExpenseResponse response = await apiClient.GetExpenseAsync(createdExpense.Id);
            scenarioContext.Set(response, GetExpenseResponseKey);
        });
    }

    [When(@"I update the created expense amount (.*) currency ""(.*)"" merchant ""(.*)"" note ""(.*)"" payment method ""(.*)""")]
    public async Task WhenIUpdateTheCreatedExpenseAmountCurrencyMerchantNotePaymentMethod(
        decimal amount,
        string currency,
        string merchant,
        string note,
        string paymentMethod)
    {
        if (!TryGet(CreateExpenseResponseKey, out ExpenseResponse? createdExpense) || createdExpense is null)
        {
            throw new InvalidOperationException("No created expense is available.");
        }

        if (!Enum.TryParse(paymentMethod, true, out PaymentMethod parsedPaymentMethod))
        {
            throw new ArgumentException($"Unknown payment method '{paymentMethod}'.", nameof(paymentMethod));
        }

        await ExecuteAsync(async () =>
        {
            UpdateExpenseRequest request = new(
                amount,
                createdExpense.CategoryId,
                currency,
                new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc),
                merchant,
                note,
                parsedPaymentMethod,
                createdExpense.TagIds);

            ExpenseResponse response = await apiClient.UpdateExpenseAsync(createdExpense.Id, request);
            scenarioContext.Set(response, UpdateExpenseResponseKey);
        });
    }

    [When(@"I delete the created expense")]
    public async Task WhenIDeleteTheCreatedExpense()
    {
        if (!TryGet(CreateExpenseResponseKey, out ExpenseResponse? createdExpense) || createdExpense is null)
        {
            throw new InvalidOperationException("No created expense is available.");
        }

        await ExecuteAsync(async () =>
        {
            await apiClient.DeleteExpenseAsync(createdExpense.Id);
        });
    }

    [When(@"I request expenses for period ""(.*)""")]
    public async Task WhenIRequestExpensesForPeriod(string period)
    {
        await ExecuteAsync(async () =>
        {
            ExpensesPageResponse response = await apiClient.GetExpensesAsync(period, string.Empty, string.Empty, "date", "desc", 1, 20);
            scenarioContext.Set(response, ExpensesPageResponseKey);
            CaptureLastResponseHeaders();
        });
    }

    [When(@"I request expenses for period ""(.*)"" filtered by merchant ""(.*)"" page (.*) with page size (.*)")]
    public async Task WhenIRequestExpensesForPeriodFilteredByMerchantPageWithPageSize(string period, string merchant, int page, int pageSize)
    {
        await ExecuteAsync(async () =>
        {
            ExpensesPageResponse body = await apiClient.GetExpensesAsync(
                period,
                merchant,
                PaymentMethod.Card.ToString(),
                "date",
                "desc",
                page,
                pageSize);

            scenarioContext.Set(body, ExpensesPageResponseKey);
            CaptureLastResponseHeaders();
        });
    }

    [When(@"I request monthly summary for period ""(.*)""")]
    public async Task WhenIRequestMonthlySummaryForPeriod(string period)
    {
        await ExecuteAsync(async () =>
        {
            MonthlyExpensesSummaryResponse response = await apiClient.GetMonthlySummaryAsync(period);
            scenarioContext.Set(response, MonthlySummaryResponseKey);
        });
    }

    [When(@"I create an expense amount (.*) currency ""(.*)"" merchant ""(.*)"" note ""(.*)"" payment method ""(.*)"" without tag ids in payload")]
    public async Task WhenICreateAnExpenseWithoutTagIdsInPayload(
        decimal amount,
        string currency,
        string merchant,
        string note,
        string paymentMethod)
    {
        if (!TryGet(CategoryResponseKey, out ExpenseCategoryResponse? category) || category is null)
        {
            throw new InvalidOperationException("An expense category must be created before creating expenses.");
        }

        if (!Enum.TryParse(paymentMethod, true, out PaymentMethod parsedPaymentMethod))
        {
            throw new ArgumentException($"Unknown payment method '{paymentMethod}'.", nameof(paymentMethod));
        }

        await ExecuteAsync(async () =>
        {
            ExpenseResponse body = await apiClient.CreateExpenseWithoutTagIdsAsync(
                amount,
                category.Id,
                currency,
                new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc),
                merchant,
                note,
                parsedPaymentMethod);
            scenarioContext.Set(body, CreateExpenseResponseKey);
        });
    }

    [When(@"I update the created expense amount (.*) currency ""(.*)"" merchant ""(.*)"" note ""(.*)"" payment method ""(.*)"" with null tag ids in payload")]
    public async Task WhenIUpdateTheCreatedExpenseWithNullTagIdsInPayload(
        decimal amount,
        string currency,
        string merchant,
        string note,
        string paymentMethod)
    {
        if (!TryGet(CreateExpenseResponseKey, out ExpenseResponse? createdExpense) || createdExpense is null)
        {
            throw new InvalidOperationException("No created expense is available.");
        }

        if (!Enum.TryParse(paymentMethod, true, out PaymentMethod parsedPaymentMethod))
        {
            throw new ArgumentException($"Unknown payment method '{paymentMethod}'.", nameof(paymentMethod));
        }

        await ExecuteAsync(async () =>
        {
            UpdateExpenseRequest request = new(
                amount,
                createdExpense.CategoryId,
                currency,
                new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc),
                merchant,
                note,
                parsedPaymentMethod,
                null!);

            ExpenseResponse body = await apiClient.UpdateExpenseAsync(createdExpense.Id, request);
            scenarioContext.Set(body, UpdateExpenseResponseKey);
        });
    }

    [When(@"I delete the created expense category")]
    public async Task WhenIDeleteTheCreatedExpenseCategory()
    {
        if (!TryGet(CategoryResponseKey, out ExpenseCategoryResponse? category) || category is null)
        {
            throw new InvalidOperationException("No created expense category is available.");
        }

        await ExecuteAsync(async () =>
        {
            await apiClient.DeleteExpenseCategoryAsync(category.Id);
        });
    }

    [When(@"I request admin monthly summary for the captured user and period ""(.*)""")]
    public async Task WhenIRequestAdminMonthlySummaryForTheCapturedUserAndPeriod(string period)
    {
        if (!TryGet(CapturedUserIdKey, out Guid userId) || userId == Guid.Empty)
        {
            throw new InvalidOperationException("No captured user id is available for admin summary request.");
        }

        await ExecuteAsync(async () =>
        {
            MonthlyExpensesSummaryResponse response = await apiClient.GetUserMonthlySummaryAsync(userId, period);
            scenarioContext.Set(response, AdminMonthlySummaryResponseKey);
        });
    }

    [When(@"I request admin monthly summary for a non-existent user and period ""(.*)""")]
    public async Task WhenIRequestAdminMonthlySummaryForANonExistentUserAndPeriod(string period)
    {
        await ExecuteAsync(async () =>
        {
            await apiClient.GetUserMonthlySummaryAsync(Guid.NewGuid(), period);
        });
    }

    [Then(@"the expense create request is successful")]
    public void ThenTheExpenseCreateRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(CreateExpenseResponseKey, out ExpenseResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Then(@"the expense get request is successful")]
    public void ThenTheExpenseGetRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(GetExpenseResponseKey, out ExpenseResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
    }

    [Then(@"the expense update request is successful")]
    public void ThenTheExpenseUpdateRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(UpdateExpenseResponseKey, out ExpenseResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
    }

    [Then(@"the expense delete request is successful")]
    public void ThenTheExpenseDeleteRequestIsSuccessful()
    {
        AssertRequestSucceeded();
    }

    [Then(@"the expenses list request is successful")]
    public void ThenTheExpensesListRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(ExpensesPageResponseKey, out ExpensesPageResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
    }

    [Then(@"expense pagination headers are returned and match the response")]
    public void ThenExpensePaginationHeadersAreReturnedAndMatchTheResponse()
    {
        bool hasResponse = TryGet(ExpensesPageResponseKey, out ExpensesPageResponse? response);
        bool hasHeaders = TryGet(PaginationHeadersKey, out IReadOnlyDictionary<string, IEnumerable<string>>? headers);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(hasResponse, Is.True);
            Assert.That(response, Is.Not.Null);
            Assert.That(hasHeaders, Is.True);
            Assert.That(headers, Is.Not.Null);
        }

        AssertHeaderValue(headers!, "X-Pagination-CurrentPage", response!.CurentPage);
        AssertHeaderValue(headers, "X-Pagination-PageSize", response.PageSize);
        AssertHeaderValue(headers, "X-Pagination-TotalCount", response.TotalCount);
        AssertHeaderValue(headers, "X-Pagination-TotalPages", response.TotalPages);
    }

    [Then(@"all listed expenses contain merchant text ""(.*)""")]
    public void ThenAllListedExpensesContainMerchantText(string merchantText)
    {
        Assert.That(TryGet(ExpensesPageResponseKey, out ExpensesPageResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Items, Is.Not.Empty);
        Assert.That(response.Items.All(i => i.Merchant.Contains(merchantText, StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Then(@"the expenses page is empty and pagination totals remain positive")]
    public void ThenTheExpensesPageIsEmptyAndPaginationTotalsRemainPositive()
    {
        Assert.That(TryGet(ExpensesPageResponseKey, out ExpensesPageResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response!.Items, Is.Empty);
            Assert.That(response.TotalCount, Is.GreaterThan(0));
            Assert.That(response.TotalPages, Is.GreaterThan(0));
        }
    }

    [Then(@"the monthly summary request is successful")]
    public void ThenTheMonthlySummaryRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(MonthlySummaryResponseKey, out MonthlyExpensesSummaryResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
    }

    [Then(@"the monthly summary total amount is greater than 0")]
    public void ThenTheMonthlySummaryTotalAmountIsGreaterThan0()
    {
        Assert.That(TryGet(MonthlySummaryResponseKey, out MonthlyExpensesSummaryResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.TotalAmount, Is.GreaterThan(0m));
    }

    [Then(@"the admin monthly summary request is successful")]
    public void ThenTheAdminMonthlySummaryRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(AdminMonthlySummaryResponseKey, out MonthlyExpensesSummaryResponse? response), Is.True);
        Assert.That(response, Is.Not.Null);
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

    private void CaptureLastResponseHeaders()
    {
        if (apiClient is not ExpensifyV1Client client)
        {
            throw new InvalidOperationException("Expected ExpensifyV1Client implementation.");
        }

        scenarioContext.Set(client.LastResponseHeaders, PaginationHeadersKey);
    }

    private static DateTime BuildExpenseDate(string period)
    {
        if (!DateTime.TryParseExact($"{period}-28", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsed))
        {
            return new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);
        }

        return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
    }

    private static void AssertHeaderValue(
        IReadOnlyDictionary<string, IEnumerable<string>> headers,
        string headerName,
        int expectedValue)
    {
        bool headerExists = headers.TryGetValue(headerName, out IEnumerable<string>? values);
        string? actualValue = values?.FirstOrDefault();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(headerExists, Is.True, $"Expected header '{headerName}' to be present.");
            Assert.That(actualValue, Is.Not.Null.And.Not.Empty, $"Expected header '{headerName}' to have a value.");
            Assert.That(actualValue, Is.EqualTo(expectedValue.ToString(CultureInfo.InvariantCulture)));
        }
    }
}
