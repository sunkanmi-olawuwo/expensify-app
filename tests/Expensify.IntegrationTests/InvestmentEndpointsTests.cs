using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Expensify.Api.Client;
using Expensify.IntegrationTests.Driver;

namespace Expensify.IntegrationTests;

[TestFixture]
internal sealed class InvestmentEndpointsTests
{
    private ApiDriver _driver = null!;
    private ExpensifyV1Client _apiClient = null!;
    private HttpClient _httpClient = null!;

    [SetUp]
    public async Task SetUp()
    {
        _driver = await ApiDriver.CreateAsync();
        _apiClient = (ExpensifyV1Client)_driver.ExpensifyV1Client;
        _httpClient = _driver.HttpClient;
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_driver is not null)
        {
            await _driver.DisposeAsync();
        }
    }

    [Test]
    public async Task GetInvestments_WhenUnauthenticated_ShouldReturnUnauthorized()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("api/v1/investments");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task InvestmentLifecycle_ShouldCreateContributeAndSummarizePortfolio()
    {
        await LoginAsync("user@test.com", "Test1234!");

        InvestmentCategoryDto category = (await GetCategoriesAsync()).Single(c => c.Slug == "isa");

        HttpResponseMessage createResponse = await _httpClient.PostAsJsonAsync(
            "api/v1/investments",
            new
            {
                name = "Stocks ISA",
                provider = "Vanguard",
                categoryId = category.Id,
                currency = "GBP",
                interestRate = 2.5m,
                maturityDate = "2028-01-01T00:00:00Z",
                currentBalance = 1100m,
                notes = "Long term"
            });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        InvestmentAccountDto createdInvestment = (await createResponse.Content.ReadFromJsonAsync<InvestmentAccountDto>())!;

        HttpResponseMessage contributionResponse = await _httpClient.PostAsJsonAsync(
            $"api/v1/investments/{createdInvestment.Id}/contributions",
            new
            {
                amount = 400m,
                date = "2026-03-10T00:00:00Z",
                notes = "Monthly top-up"
            });

        Assert.That(contributionResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        HttpResponseMessage listResponse = await _httpClient.GetAsync("api/v1/investments?page=1&pageSize=10");
        string listResponseContent = await listResponse.Content.ReadAsStringAsync();
        Assert.That(listResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), listResponseContent);
        InvestmentAccountsPageDto investmentList =
            JsonSerializer.Deserialize<InvestmentAccountsPageDto>(listResponseContent, JsonSerializerOptions.Web)!;

        HttpResponseMessage detailResponse = await _httpClient.GetAsync($"api/v1/investments/{createdInvestment.Id}");
        InvestmentAccountDto investmentDetail = (await detailResponse.Content.ReadFromJsonAsync<InvestmentAccountDto>())!;

        HttpResponseMessage contributionsListResponse = await _httpClient.GetAsync($"api/v1/investments/{createdInvestment.Id}/contributions?page=1&pageSize=10");
        string contributionsListResponseContent = await contributionsListResponse.Content.ReadAsStringAsync();
        Assert.That(contributionsListResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), contributionsListResponseContent);
        InvestmentContributionsPageDto contributionsPage =
            JsonSerializer.Deserialize<InvestmentContributionsPageDto>(contributionsListResponseContent, JsonSerializerOptions.Web)!;

        HttpResponseMessage summaryResponse = await _httpClient.GetAsync("api/v1/investments/summary");
        PortfolioSummaryDto summary = (await summaryResponse.Content.ReadFromJsonAsync<PortfolioSummaryDto>())!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(createdInvestment.MaturityDate, Is.Null);
            Assert.That(investmentList.Items.Any(item => item.Id == createdInvestment.Id), Is.True);
            Assert.That(investmentDetail.TotalContributed, Is.EqualTo(400m));
            Assert.That(contributionsPage.Items, Has.Count.EqualTo(1));
            Assert.That(summary.TotalContributed, Is.EqualTo(400m));
            Assert.That(summary.CurrentValue, Is.EqualTo(1100m));
            Assert.That(summary.TotalGainLoss, Is.EqualTo(700m));
            Assert.That(summary.GainLossPercentage, Is.EqualTo(175m));
            Assert.That(summary.AccountCount, Is.EqualTo(1));
            Assert.That(summary.Currency, Is.EqualTo("GBP"));
        }
    }

    [Test]
    public async Task InactiveCategory_ShouldBeHiddenFromUserButExistingAccountCanKeepIt()
    {
        await LoginAsync("user@test.com", "Test1234!");

        InvestmentCategoryDto category = (await GetCategoriesAsync()).Single(c => c.Slug == "other");
        InvestmentAccountDto createdInvestment = await CreateInvestmentAsync(category.Id, "Other Bucket");

        await LoginAsync("admin@test.com", "Test1234!");

        HttpResponseMessage deactivateResponse = await _httpClient.PutAsJsonAsync(
            $"api/v1/admin/investments/categories/{category.Id}",
            new
            {
                isActive = false
            });

        Assert.That(deactivateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        List<InvestmentCategoryDto> adminCategories =
            (await _httpClient.GetFromJsonAsync<List<InvestmentCategoryDto>>("api/v1/admin/investments/categories"))!;

        await LoginAsync("user@test.com", "Test1234!");

        List<InvestmentCategoryDto> userCategories = await GetCategoriesAsync();

        HttpResponseMessage updateExistingResponse = await _httpClient.PutAsJsonAsync(
            $"api/v1/investments/{createdInvestment.Id}",
            new
            {
                name = "Other Bucket Updated",
                provider = "Provider",
                categoryId = category.Id,
                currency = "GBP",
                interestRate = 1.2m,
                maturityDate = "2028-01-01T00:00:00Z",
                currentBalance = 650m,
                notes = "kept"
            });

        HttpResponseMessage createWithInactiveCategoryResponse = await _httpClient.PostAsJsonAsync(
            "api/v1/investments",
            new
            {
                name = "Should Fail",
                provider = "Provider",
                categoryId = category.Id,
                currency = "GBP",
                interestRate = 1.2m,
                maturityDate = "2028-01-01T00:00:00Z",
                currentBalance = 650m,
                notes = "new"
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adminCategories.Any(c => c.Id == category.Id && !c.IsActive), Is.True);
            Assert.That(userCategories.Any(c => c.Id == category.Id), Is.False);
            Assert.That(updateExistingResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(createWithInactiveCategoryResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }

    [Test]
    public async Task DeleteInvestment_ShouldHideDeletedResourcesAndRequireAdminForAdminList()
    {
        await LoginAsync("user@test.com", "Test1234!");

        InvestmentCategoryDto category = (await GetCategoriesAsync()).Single(c => c.Slug == "isa");
        InvestmentAccountDto createdInvestment = await CreateInvestmentAsync(category.Id, "Delete Me");

        HttpResponseMessage contributionResponse = await _httpClient.PostAsJsonAsync(
            $"api/v1/investments/{createdInvestment.Id}/contributions",
            new
            {
                amount = 300m,
                date = "2026-03-11T00:00:00Z",
                notes = "to delete"
            });

        Assert.That(contributionResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        HttpResponseMessage forbiddenAdminResponse = await _httpClient.GetAsync("api/v1/admin/investments");
        Assert.That(forbiddenAdminResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        HttpResponseMessage deleteResponse = await _httpClient.DeleteAsync($"api/v1/investments/{createdInvestment.Id}");
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        HttpResponseMessage getDeletedInvestmentResponse = await _httpClient.GetAsync($"api/v1/investments/{createdInvestment.Id}");
        HttpResponseMessage getDeletedContributionsResponse = await _httpClient.GetAsync($"api/v1/investments/{createdInvestment.Id}/contributions");
        PortfolioSummaryDto summary = (await _httpClient.GetFromJsonAsync<PortfolioSummaryDto>("api/v1/investments/summary"))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(getDeletedInvestmentResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(getDeletedContributionsResponse.StatusCode, Is.Not.EqualTo(HttpStatusCode.OK));
            Assert.That(summary.TotalContributed, Is.EqualTo(0m));
            Assert.That(summary.CurrentValue, Is.EqualTo(0m));
            Assert.That(summary.AccountCount, Is.EqualTo(0));
        }

        await LoginAsync("admin@test.com", "Test1234!");
        HttpResponseMessage adminListResponse = await _httpClient.GetAsync("api/v1/admin/investments?page=1&pageSize=10");
        string adminListResponseContent = await adminListResponse.Content.ReadAsStringAsync();

        Assert.That(adminListResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), adminListResponseContent);
    }

    private async Task LoginAsync(string email, string password)
    {
        LoginUserResponse response = await _apiClient.LoginAsync(new LoginCommand(email, password));
        _apiClient.BearerToken = response.Token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", response.Token);
    }

    private async Task<List<InvestmentCategoryDto>> GetCategoriesAsync()
    {
        return (await _httpClient.GetFromJsonAsync<List<InvestmentCategoryDto>>("api/v1/investments/categories"))!;
    }

    private async Task<InvestmentAccountDto> CreateInvestmentAsync(Guid categoryId, string name)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "api/v1/investments",
            new
            {
                name,
                provider = "Provider",
                categoryId,
                currency = "GBP",
                interestRate = 1.2m,
                maturityDate = "2028-01-01T00:00:00Z",
                currentBalance = 600m,
                notes = "test"
            });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        return (await response.Content.ReadFromJsonAsync<InvestmentAccountDto>())!;
    }

    private sealed record InvestmentCategoryDto(Guid Id, string Name, string Slug, bool IsActive);

    private sealed record InvestmentAccountListItemDto(
        Guid Id,
        Guid UserId,
        string Name,
        string? Provider,
        Guid CategoryId,
        string CategoryName,
        string CategorySlug,
        string Currency,
        decimal? InterestRate,
        DateTimeOffset? MaturityDate,
        decimal CurrentBalance,
        string? Notes,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);

    private sealed record InvestmentAccountDto(
        Guid Id,
        Guid UserId,
        string Name,
        string? Provider,
        Guid CategoryId,
        string CategoryName,
        string CategorySlug,
        string Currency,
        decimal? InterestRate,
        DateTimeOffset? MaturityDate,
        decimal CurrentBalance,
        string? Notes,
        decimal TotalContributed,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);

    private sealed class InvestmentAccountsPageDto
    {
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
        public IReadOnlyCollection<InvestmentAccountListItemDto> Items { get; init; } = [];
    }

    private sealed record InvestmentContributionDto(
        Guid Id,
        Guid InvestmentId,
        decimal Amount,
        DateTimeOffset Date,
        string? Notes,
        DateTime CreatedAtUtc);

    private sealed class InvestmentContributionsPageDto
    {
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
        public IReadOnlyCollection<InvestmentContributionDto> Items { get; init; } = [];
    }

    private sealed record PortfolioSummaryDto(
        decimal TotalContributed,
        decimal CurrentValue,
        decimal TotalGainLoss,
        decimal GainLossPercentage,
        int AccountCount,
        string Currency);
}
