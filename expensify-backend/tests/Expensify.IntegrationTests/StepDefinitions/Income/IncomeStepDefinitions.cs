using System.Globalization;
using Reqnroll;
using Expensify.Api.Client;

namespace Expensify.IntegrationTests.StepDefinitions.Income;

[Binding]
[Scope(Feature = "Income")]
public sealed class IncomeStepDefinitions(IExpensifyV1Client apiClient, ScenarioContext scenarioContext)
{
    private const string CreateIncomeResponseKey = "CreateIncomeResponse";
    private const string GetIncomeResponseKey = "GetIncomeResponse";
    private const string UpdateIncomeResponseKey = "UpdateIncomeResponse";
    private const string IncomePageResponseKey = "IncomePageResponse";
    private const string MonthlySummaryResponseKey = "MonthlyIncomeSummaryResponse";
    private const string AdminMonthlySummaryResponseKey = "AdminMonthlyIncomeSummaryResponse";
    private const string CapturedUserIdKey = "CapturedIncomeUserId";
    private const string PaginationHeadersKey = "IncomePaginationHeaders";

    [Given(@"I capture my current user id for income")]
    public async Task GivenICaptureMyCurrentUserIdForIncome()
    {
        await ExecuteAsync(async () =>
        {
            GetUserResponse response = await apiClient.GetUserProfileAsync(Guid.NewGuid());
            scenarioContext.Set(response.Id, CapturedUserIdKey);
        });

        AssertRequestSucceeded();
    }

    [Given(@"I create incomes for filtering in period ""(.*)""")]
    public async Task GivenICreateIncomesForFilteringInPeriod(string period)
    {
        DateTime incomeDate = BuildIncomeDate(period);

        await ExecuteAsync(async () =>
        {
            await CreateIncomeAsync(500m, "GBP", incomeDate, "Client A", "Freelance", "Invoice A");
            await CreateIncomeAsync(700m, "GBP", incomeDate, "Client B", "Freelance", "Invoice B");
            await CreateIncomeAsync(2500m, "GBP", incomeDate, "Payroll", "Salary", "Monthly salary");
        });

        AssertRequestSucceeded();
    }

    [Given(@"I create an income amount (.*) currency ""(.*)"" source ""(.*)"" type ""(.*)"" note ""(.*)""")]
    [Then(@"I create an income amount (.*) currency ""(.*)"" source ""(.*)"" type ""(.*)"" note ""(.*)""")]
    [When(@"I create an income amount (.*) currency ""(.*)"" source ""(.*)"" type ""(.*)"" note ""(.*)""")]
    public async Task WhenICreateAnIncomeAmountCurrencySourceTypeNote(
        decimal amount,
        string currency,
        string source,
        string type,
        string note)
    {
        DateTime incomeDate = new(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);

        await ExecuteAsync(async () =>
        {
            IncomeResponse response = await CreateIncomeAsync(amount, currency, incomeDate, source, type, note);
            scenarioContext.Set(response, CreateIncomeResponseKey);
        });
    }

    [When(@"I fetch the created income")]
    public async Task WhenIFetchTheCreatedIncome()
    {
        if (!TryGet(CreateIncomeResponseKey, out IncomeResponse? createdIncome) || createdIncome is null)
        {
            throw new InvalidOperationException("No created income is available.");
        }

        await ExecuteAsync(async () =>
        {
            IncomeResponse response = await apiClient.GetIncomeAsync(createdIncome.Id);
            scenarioContext.Set(response, GetIncomeResponseKey);
        });
    }

    [When(@"I update the created income amount (.*) currency ""(.*)"" source ""(.*)"" type ""(.*)"" note ""(.*)""")]
    public async Task WhenIUpdateTheCreatedIncomeAmountCurrencySourceTypeNote(
        decimal amount,
        string currency,
        string source,
        string type,
        string note)
    {
        if (!TryGet(CreateIncomeResponseKey, out IncomeResponse? createdIncome) || createdIncome is null)
        {
            throw new InvalidOperationException("No created income is available.");
        }

        await ExecuteAsync(async () =>
        {
            UpdateIncomeRequest request = new(
                amount,
                currency,
                new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
                note,
                source,
                ParseIncomeType(type));

            IncomeResponse response = await apiClient.UpdateIncomeAsync(createdIncome.Id, request);
            scenarioContext.Set(response, UpdateIncomeResponseKey);
        });
    }

    [When(@"I delete the created income")]
    public async Task WhenIDeleteTheCreatedIncome()
    {
        if (!TryGet(CreateIncomeResponseKey, out IncomeResponse? createdIncome) || createdIncome is null)
        {
            throw new InvalidOperationException("No created income is available.");
        }

        await ExecuteAsync(async () =>
        {
            await apiClient.DeleteIncomeAsync(createdIncome.Id);
        });
    }

    [When(@"I request income for period ""(.*)""")]
    public async Task WhenIRequestIncomeForPeriod(string period)
    {
        await ExecuteAsync(async () =>
        {
            IncomePageResponse response = await apiClient.GetIncomesAsync(period, string.Empty, "date", "desc", 1, 20);
            scenarioContext.Set(response, IncomePageResponseKey);
            CaptureLastResponseHeaders();
        });
    }

    [When(@"I request income for period ""(.*)"" filtered by source ""(.*)"" page (.*) with page size (.*)")]
    public async Task WhenIRequestIncomeForPeriodFilteredBySourcePageWithPageSize(string period, string source, int page, int pageSize)
    {
        await ExecuteAsync(async () =>
        {
            IncomePageResponse response = await apiClient.GetIncomesAsync(period, source, "date", "desc", page, pageSize);
            scenarioContext.Set(response, IncomePageResponseKey);
            CaptureLastResponseHeaders();
        });
    }

    [When(@"I request income monthly summary for period ""(.*)""")]
    public async Task WhenIRequestIncomeMonthlySummaryForPeriod(string period)
    {
        await ExecuteAsync(async () =>
        {
            MonthlyIncomeSummaryResponse response = await apiClient.IncomeGetMonthlySummaryAsync(period);
            scenarioContext.Set(response, MonthlySummaryResponseKey);
        });
    }

    [When(@"I request admin income monthly summary for the captured user and period ""(.*)""")]
    public async Task WhenIRequestAdminIncomeMonthlySummaryForTheCapturedUserAndPeriod(string period)
    {
        if (!TryGet(CapturedUserIdKey, out Guid userId) || userId == Guid.Empty)
        {
            throw new InvalidOperationException("No captured user id is available for admin income summary request.");
        }

        await ExecuteAsync(async () =>
        {
            MonthlyIncomeSummaryResponse response = await apiClient.IncomeGetUserMonthlySummaryAsync(userId, period);
            scenarioContext.Set(response, AdminMonthlySummaryResponseKey);
        });
    }

    [When(@"I request admin income monthly summary for a non-existent user and period ""(.*)""")]
    public async Task WhenIRequestAdminIncomeMonthlySummaryForANonExistentUserAndPeriod(string period)
    {
        await ExecuteAsync(async () =>
        {
            await apiClient.IncomeGetUserMonthlySummaryAsync(Guid.NewGuid(), period);
        });
    }

    [Then(@"the income create request is successful")]
    public void ThenTheIncomeCreateRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(CreateIncomeResponseKey, out IncomeResponse? response), Is.True);
        Assert.That(response!.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Then(@"the income get request is successful")]
    public void ThenTheIncomeGetRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet<IncomeResponse>(GetIncomeResponseKey, out _), Is.True);
    }

    [Then(@"the income update request is successful")]
    public void ThenTheIncomeUpdateRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet<IncomeResponse>(UpdateIncomeResponseKey, out _), Is.True);
    }

    [Then(@"the income delete request is successful")]
    public void ThenTheIncomeDeleteRequestIsSuccessful()
    {
        AssertRequestSucceeded();
    }

    [Then(@"the income list request is successful")]
    public void ThenTheIncomeListRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet<IncomePageResponse>(IncomePageResponseKey, out _), Is.True);
    }

    [Then(@"income pagination headers are returned and match the response")]
    public void ThenIncomePaginationHeadersAreReturnedAndMatchTheResponse()
    {
        bool hasResponse = TryGet(IncomePageResponseKey, out IncomePageResponse? response);
        bool hasHeaders = TryGet(PaginationHeadersKey, out IReadOnlyDictionary<string, IEnumerable<string>>? headers);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(hasResponse, Is.True);
            Assert.That(hasHeaders, Is.True);
            Assert.That(headers, Is.Not.Null);
        }

        AssertHeaderValue(headers!, "X-Pagination-CurrentPage", response!.CurentPage);
        AssertHeaderValue(headers, "X-Pagination-PageSize", response.PageSize);
        AssertHeaderValue(headers, "X-Pagination-TotalCount", response.TotalCount);
        AssertHeaderValue(headers, "X-Pagination-TotalPages", response.TotalPages);
    }

    [Then(@"the income page is empty and pagination totals remain positive")]
    public void ThenTheIncomePageIsEmptyAndPaginationTotalsRemainPositive()
    {
        Assert.That(TryGet(IncomePageResponseKey, out IncomePageResponse? response), Is.True);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response!.Items.Count, Is.EqualTo(0));
            Assert.That(response.TotalCount, Is.GreaterThan(0));
            Assert.That(response.TotalPages, Is.GreaterThan(0));
        }
    }

    [Then(@"the income monthly summary request is successful")]
    public void ThenTheIncomeMonthlySummaryRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet<MonthlyIncomeSummaryResponse>(MonthlySummaryResponseKey, out _), Is.True);
    }

    [Then(@"the income monthly summary total amount is greater than 0")]
    public void ThenTheIncomeMonthlySummaryTotalAmountIsGreaterThan0()
    {
        Assert.That(TryGet(MonthlySummaryResponseKey, out MonthlyIncomeSummaryResponse? response), Is.True);
        Assert.That(response!.TotalAmount, Is.GreaterThan(0m));
    }

    [Then(@"the admin income monthly summary request is successful")]
    public void ThenTheAdminIncomeMonthlySummaryRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet<MonthlyIncomeSummaryResponse>(AdminMonthlySummaryResponseKey, out _), Is.True);
    }

    private async Task<IncomeResponse> CreateIncomeAsync(decimal amount, string currency, DateTime date, string source, string type, string note)
    {
        CreateIncomeRequest request = new(
            amount,
            currency,
            date,
            note,
            source,
            ParseIncomeType(type));

        return await apiClient.CreateIncomeAsync(request);
    }

    private static IncomeType ParseIncomeType(string type)
    {
        if (Enum.TryParse(type, true, out IncomeType parsed))
        {
            return parsed;
        }

        throw new ArgumentException($"Unsupported income type '{type}'.", nameof(type));
    }

    private void CaptureLastResponseHeaders()
    {
        if (apiClient is not ExpensifyV1Client client)
        {
            throw new InvalidOperationException("Expected ExpensifyV1Client implementation.");
        }

        scenarioContext.Set(client.LastResponseHeaders, PaginationHeadersKey);
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

    private static DateTime BuildIncomeDate(string period)
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
