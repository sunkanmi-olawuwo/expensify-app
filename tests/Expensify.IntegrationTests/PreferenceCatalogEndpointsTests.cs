using Expensify.Api.Client;
using Expensify.IntegrationTests.Driver;

namespace Expensify.IntegrationTests;

[TestFixture]
internal sealed class PreferenceCatalogEndpointsTests
{
    private ApiDriver _driver = null!;
    private ExpensifyV1Client _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        _driver = await ApiDriver.CreateAsync();
        _client = (ExpensifyV1Client)_driver.ExpensifyV1Client;
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
    public async Task GetCurrenciesAsync_WhenAuthenticatedAsUser_ShouldReturnOnlyActiveCurrencies()
    {
        await LoginAsync("user@test.com", "Test1234!");

        ICollection<CurrencyResponse> currencies = await _client.GetCurrenciesAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(currencies, Is.Not.Empty);
            Assert.That(currencies.Any(currency => currency.Code == "GBP" && currency.IsDefault), Is.True);
            Assert.That(currencies.All(currency => currency.IsActive), Is.True);
        }
    }

    [Test]
    public async Task GetCatalogsAsync_WhenAdminIncludesInactive_ShouldReturnInactiveEntries()
    {
        await LoginAsync("admin@test.com", "Test1234!");

        const string CurrencyCode = "XCD";
        string timezoneId = $"Custom/Test-{Guid.NewGuid():N}";

        await _client.CreateCurrencyAsync(new CreateCurrencyBody(CurrencyCode, true, false, 2, "Integration Currency", 50, "$"));
        await _client.UpdateCurrencyAsync(CurrencyCode, new UpdateCurrencyBody(false, false, 2, "Integration Currency", 50, "$"));

        await _client.CreateTimezoneAsync(new CreateTimezoneBody("Integration Timezone", timezoneId, true, false, 50));
        await _client.UpdateTimezoneAsync(timezoneId, new UpdateTimezoneBody("Integration Timezone", false, false, 50));

        ICollection<CurrencyResponse> adminCurrencies = await _client.GetCurrenciesAsync(true);
        ICollection<TimezoneResponse> adminTimezones = await _client.GetTimezonesAsync(true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adminCurrencies.Any(currency => currency.Code == CurrencyCode && !currency.IsActive), Is.True);
            Assert.That(adminTimezones.Any(timezone => timezone.IanaId == timezoneId && !timezone.IsActive), Is.True);
        }

        await LoginAsync("user@test.com", "Test1234!");

        ICollection<CurrencyResponse> userCurrencies = await _client.GetCurrenciesAsync();
        ICollection<TimezoneResponse> userTimezones = await _client.GetTimezonesAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(userCurrencies.Any(currency => currency.Code == CurrencyCode), Is.False);
            Assert.That(userTimezones.Any(timezone => timezone.IanaId == timezoneId), Is.False);
        }
    }

    [Test]
    public async Task UpdateUserProfileAsync_WhenSwitchingToInactiveCurrency_ShouldReturnBadRequest()
    {
        await LoginAsync("admin@test.com", "Test1234!");

        const string CurrencyCode = "XCE";

        await _client.CreateCurrencyAsync(new CreateCurrencyBody(CurrencyCode, true, false, 2, "Inactive Currency", 60, "$"));
        await _client.UpdateCurrencyAsync(CurrencyCode, new UpdateCurrencyBody(false, false, 2, "Inactive Currency", 60, "$"));

        SwaggerException exception = Assert.ThrowsAsync<SwaggerException>(async () =>
            await _client.UpdateUserProfileAsync(new UpdateUserData(CurrencyCode, "Admin", "User", 1, "UTC")))!;

        Assert.That(exception.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task UpdateUserProfileAsync_WhenKeepingExistingInactiveCurrency_ShouldSucceed()
    {
        await LoginAsync("admin@test.com", "Test1234!");

        await _client.UpdateUserProfileAsync(new UpdateUserData("EUR", "Admin", "User", 1, "UTC"));
        await _client.UpdateCurrencyAsync("EUR", new UpdateCurrencyBody(false, false, 2, "Euro", 2, "EUR"));

        Assert.DoesNotThrowAsync(async () =>
            await _client.UpdateUserProfileAsync(new UpdateUserData("EUR", "AdminInactive", "UserInactive", 5, "UTC")));

        GetUserResponse profile = await _client.GetUserProfileAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(profile.Currency, Is.EqualTo("EUR"));
            Assert.That(profile.FirstName, Is.EqualTo("AdminInactive"));
            Assert.That(profile.MonthStartDay, Is.EqualTo(5));
        }
    }

    [Test]
    public async Task RegisterUserAsync_ShouldUseCatalogDefaults()
    {
        string email = $"catalog.{Guid.NewGuid():N}@example.com";
        const string Password = "Passw0rd!";

        await _client.RegisterUserAsync(new RegisterUserCommand(email, "Catalog", "User", Password, Expensify.Api.Client.RoleType.User));
        LoginUserResponse login = await _client.LoginAsync(new LoginCommand(email, Password));
        _client.BearerToken = login.Token;

        CurrencyResponse defaultCurrency = (await _client.GetCurrenciesAsync()).Single(currency => currency.IsDefault);
        TimezoneResponse defaultTimezone = (await _client.GetTimezonesAsync()).Single(timezone => timezone.IsDefault);
        GetUserResponse profile = await _client.GetUserProfileAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(profile.Currency, Is.EqualTo(defaultCurrency.Code));
            Assert.That(profile.Timezone, Is.EqualTo(defaultTimezone.IanaId));
        }
    }

    private async Task LoginAsync(string email, string password)
    {
        LoginUserResponse response = await _client.LoginAsync(new LoginCommand(email, password));
        _client.BearerToken = response.Token;
    }
}
