using Reqnroll;
using Expensify.Api.Client;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Expensify.Common.Application.Caching;
using Expensify.Modules.Users.Domain.Tokens;
using Expensify.IntegrationTests.Driver;

namespace Expensify.IntegrationTests.StepDefinitions.Users;

[Binding]
public sealed class UserStepDefinitions(IExpensifyV1Client apiClient, ApiDriver apiDriver, ScenarioContext scenarioContext)
{
    private static int _scenarioCounter;

    private static class ScenarioKeys
    {
        public const string LoginCommand = nameof(LoginCommand);
        public const string RegisterUserCommand = nameof(RegisterUserCommand);
        public const string LoginUserResponse = nameof(LoginUserResponse);
        public const string RegisterUserResponse = nameof(RegisterUserResponse);
        public const string UserResponse = nameof(UserResponse);
        public const string SwaggerException = nameof(SwaggerException);
        public const string UnexpectedException = nameof(UnexpectedException);
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        int scenarioIndex = Interlocked.Increment(ref _scenarioCounter);
        int thirdOctet = 1 + scenarioIndex / 254 % 254;
        int fourthOctet = 1 + scenarioIndex % 254;
        SetForwardedFor($"203.0.{thirdOctet}.{fourthOctet}");
    }

    [AfterScenario]
    public void AfterScenario()
    {
        SetBearerToken(null);
        SetForwardedFor(null);
    }

    [Given(@"an existing user email ""(.*)"" with password ""(.*)""")]
    public void GivenAnExistingUserEmailWithPassword(string email, string password)
    {
        scenarioContext.Set(new LoginCommand(email, password), ScenarioKeys.LoginCommand);
        ResetExceptions();
    }

    [Given(@"a unique registration request with first name ""(.*)"" last name ""(.*)"" password ""(.*)"" role ""(.*)""")]
    public void GivenAUniqueRegistrationRequestWithFirstNameLastNamePasswordRole(
        string firstName,
        string lastName,
        string password,
        string role)
    {
        if (!Enum.TryParse(role, true, out RoleType roleType))
        {
            throw new ArgumentException($"Unknown role '{role}'.", nameof(role));
        }

        string uniqueEmail = $"it.{Guid.NewGuid():N}@example.com";
        scenarioContext.Set(
            new RegisterUserCommand(uniqueEmail, firstName, lastName, password, roleType),
            ScenarioKeys.RegisterUserCommand);
        ResetExceptions();
    }

    [Given(@"I am logged in as ""(.*)""")]
    public async Task GivenIAmLoggedInAs(string accountType)
    {
        (string email, string password) = accountType.ToLowerInvariant() switch
        {
            "admin" => ("admin@test.com", "Test1234!"),
            "user" => ("user@test.com", "Test1234!"),
            _ => throw new ArgumentException($"Unsupported account type '{accountType}'.", nameof(accountType))
        };

        scenarioContext.Set(new LoginCommand(email, password), ScenarioKeys.LoginCommand);
        await ExecuteAsync(async () =>
        {
            LoginCommand loginCommand = scenarioContext.Get<LoginCommand>(ScenarioKeys.LoginCommand);
            LoginUserResponse loginResponse = await apiClient.LoginAsync(loginCommand);
            scenarioContext.Set(loginResponse, ScenarioKeys.LoginUserResponse);
            SetBearerToken(loginResponse.Token);
        });
        AssertRequestSucceeded();
    }

    [Given(@"I am logged in as the newly registered user")]
    public async Task GivenIAmLoggedInAsTheNewlyRegisteredUser()
    {
        if (!TryGet(ScenarioKeys.RegisterUserCommand, out RegisterUserCommand? registerUserCommand) || registerUserCommand is null)
        {
            throw new InvalidOperationException("Registration command is required before logging in as the newly registered user.");
        }

        scenarioContext.Set(new LoginCommand(registerUserCommand.Email, registerUserCommand.Password), ScenarioKeys.LoginCommand);
        await ExecuteAsync(async () =>
        {
            LoginCommand loginCommand = scenarioContext.Get<LoginCommand>(ScenarioKeys.LoginCommand);
            LoginUserResponse loginResponse = await apiClient.LoginAsync(loginCommand);
            scenarioContext.Set(loginResponse, ScenarioKeys.LoginUserResponse);
            SetBearerToken(loginResponse.Token);
        });
        AssertRequestSucceeded();
    }

    [Given(@"I use an invalid bearer token")]
    public void GivenIUseAnInvalidBearerToken()
    {
        SetBearerToken("invalid.token.value");
        ResetExceptions();
    }

    [Given(@"my current access token is revoked")]
    public async Task GivenMyCurrentAccessTokenIsRevoked()
    {
        if (!TryGet(ScenarioKeys.LoginUserResponse, out LoginUserResponse? loginResponse) || loginResponse is null)
        {
            throw new InvalidOperationException("Login response is required before revoking token.");
        }

        TokenValidationParameters tokenValidationParameters = apiDriver.Server.Services.GetRequiredService<TokenValidationParameters>();
        JwtSecurityTokenHandler tokenHandler = new();
        TokenValidationResult tokenValidationResult = await tokenHandler.ValidateTokenAsync(
            loginResponse.Token,
            tokenValidationParameters);
        if (!tokenValidationResult.IsValid || tokenValidationResult.ClaimsIdentity is null)
        {
            throw new InvalidOperationException("Unable to validate token for revocation setup.");
        }

        string? jwtId = tokenValidationResult.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
            ?? tokenValidationResult.ClaimsIdentity.Claims
                .FirstOrDefault(claim => claim.Type.EndsWith("/jti", StringComparison.OrdinalIgnoreCase))
                ?.Value;
        if (string.IsNullOrWhiteSpace(jwtId))
        {
            throw new InvalidOperationException("JWT does not contain a jti.");
        }

        ICacheService cacheService = apiDriver.Server.Services.GetRequiredService<ICacheService>();
        await cacheService.SetAsync(jwtId, RevocatedTokenType.RoleChanged);
    }

    [When(@"I log in with those credentials")]
    public async Task WhenILogInWithThoseCredentials()
    {
        if (!TryGet(ScenarioKeys.LoginCommand, out LoginCommand? loginCommand) || loginCommand is null)
        {
            throw new InvalidOperationException("Login command was not initialized.");
        }

        await ExecuteAsync(async () =>
        {
            LoginUserResponse loginResponse = await apiClient.LoginAsync(loginCommand);
            scenarioContext.Set(loginResponse, ScenarioKeys.LoginUserResponse);
        });
    }

    [When(@"I attempt to log in with those credentials (.*) times")]
    public async Task WhenIAttemptToLogInWithThoseCredentialsTimes(int attempts)
    {
        if (!TryGet(ScenarioKeys.LoginCommand, out LoginCommand? loginCommand) || loginCommand is null)
        {
            throw new InvalidOperationException("Login command was not initialized.");
        }

        await ExecuteAsync(async () =>
        {
            for (int i = 0; i < attempts; i++)
            {
                await apiClient.LoginAsync(loginCommand);
            }
        });
    }

    [When(@"I submit the user registration request")]
    public async Task WhenISubmitTheUserRegistrationRequest()
    {
        if (!TryGet(ScenarioKeys.RegisterUserCommand, out RegisterUserCommand? registerUserCommand) || registerUserCommand is null)
        {
            throw new InvalidOperationException("Register user command was not initialized.");
        }

        await ExecuteAsync(async () =>
        {
            RegisterUserResponse registerResponse = await apiClient.RegisterUserAsync(registerUserCommand);
            scenarioContext.Set(registerResponse, ScenarioKeys.RegisterUserResponse);
        });
    }

    [When(@"I request my user profile")]
    public async Task WhenIRequestMyUserProfile()
    {
        await ExecuteAsync(async () =>
        {
            GetUserResponse userProfileResponse = await apiClient.GetUserProfileAsync();
            scenarioContext.Set(userProfileResponse, ScenarioKeys.UserResponse);
        });
    }

    [Given(@"I update my profile to first name ""(.*)"" and last name ""(.*)"" currency ""(.*)"" timezone ""(.*)"" month start day (.*)")]
    [When(@"I update my profile to first name ""(.*)"" and last name ""(.*)"" currency ""(.*)"" timezone ""(.*)"" month start day (.*)")]
    public async Task WhenIUpdateMyProfileToFirstNameAndLastNameCurrencyTimezoneMonthStartDay(
        string firstName,
        string lastName,
        string currency,
        string timezone,
        int monthStartDay)
    {
        var data = new UpdateUserData(currency, firstName, lastName, monthStartDay, timezone);

        await ExecuteAsync(async () =>
        {
            await apiClient.UpdateUserProfileAsync(data);
        });
    }

    [Then(@"the login request is successful")]
    public void ThenTheLoginRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(ScenarioKeys.LoginUserResponse, out LoginUserResponse? loginResponse), Is.True);
        Assert.That(loginResponse, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(loginResponse!.Token, Is.Not.Empty);
            Assert.That(loginResponse.RefreshToken, Is.Not.Empty);
        }
    }

    [Then(@"the registration request is successful")]
    public void ThenTheRegistrationRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(ScenarioKeys.RegisterUserResponse, out RegisterUserResponse? registerResponse), Is.True);
        Assert.That(registerResponse, Is.Not.Null);
        Assert.That(registerResponse!.UserId, Is.Not.EqualTo(Guid.Empty));
    }

    [Then(@"the get profile request is successful")]
    public void ThenTheGetProfileRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(ScenarioKeys.UserResponse, out GetUserResponse? userProfileResponse), Is.True);
        Assert.That(userProfileResponse, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(userProfileResponse!.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(userProfileResponse.FirstName, Is.Not.Empty);
            Assert.That(userProfileResponse.LastName, Is.Not.Empty);
            Assert.That(userProfileResponse.Currency, Is.Not.Empty);
            Assert.That(userProfileResponse.Timezone, Is.Not.Empty);
            Assert.That(userProfileResponse.MonthStartDay, Is.InRange(1, 28));
        }
    }

    [Then(@"the update profile request is successful")]
    public void ThenTheUpdateProfileRequestIsSuccessful()
    {
        AssertRequestSucceeded();
    }

    [Then(@"the get profile contains currency ""(.*)"" timezone ""(.*)"" and month start day (.*)")]
    public void ThenTheGetProfileContainsCurrencyTimezoneAndMonthStartDay(string currency, string timezone, int monthStartDay)
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(ScenarioKeys.UserResponse, out GetUserResponse? userProfileResponse), Is.True);
        Assert.That(userProfileResponse, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(userProfileResponse!.Currency, Is.EqualTo(currency));
            Assert.That(userProfileResponse.Timezone, Is.EqualTo(timezone));
            Assert.That(userProfileResponse.MonthStartDay, Is.EqualTo(monthStartDay));
        }
    }

    [Then(@"the request fails with status code (.*)")]
    public void ThenTheRequestFailsWithStatusCode(int statusCode)
    {
        TryGet(ScenarioKeys.UnexpectedException, out Exception? unexpectedException);
        TryGet(ScenarioKeys.SwaggerException, out SwaggerException? swaggerException);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unexpectedException, Is.Null, unexpectedException?.ToString());
            Assert.That(swaggerException, Is.Not.Null, "Expected a SwaggerException but none was thrown.");
        }
        Assert.That(swaggerException!.StatusCode, Is.EqualTo(statusCode));
    }

    [Then(@"the error response contains title ""(.*)""")]
    public void ThenTheErrorResponseContainsTitle(string expectedTitle)
    {
        TryGet(ScenarioKeys.SwaggerException, out SwaggerException? swaggerException);
        Assert.That(swaggerException, Is.Not.Null, "Expected a SwaggerException but none was thrown.");

        string? title = TryReadProblemPropertyFromResponse(swaggerException!, "title")
            ?? TryReadProblemPropertyFromTypedResult(swaggerException!, "Title");

        Assert.That(title, Is.EqualTo(expectedTitle));
    }

    [Then(@"the error response detail contains ""(.*)""")]
    public void ThenTheErrorResponseDetailContains(string expectedText)
    {
        TryGet(ScenarioKeys.SwaggerException, out SwaggerException? swaggerException);
        Assert.That(swaggerException, Is.Not.Null, "Expected a SwaggerException but none was thrown.");

        string? detail = TryReadProblemPropertyFromResponse(swaggerException!, "detail")
            ?? TryReadProblemPropertyFromTypedResult(swaggerException!, "Detail");

        Assert.That(detail, Does.Contain(expectedText));
    }

    private static string? TryReadProblemPropertyFromResponse(SwaggerException swaggerException, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(swaggerException.Response))
        {
            return null;
        }

        using var jsonDocument = JsonDocument.Parse(swaggerException.Response);
        return jsonDocument.RootElement.TryGetProperty(propertyName, out JsonElement propertyElement)
            ? propertyElement.GetString()
            : null;
    }

    private static string? TryReadProblemPropertyFromTypedResult(SwaggerException swaggerException, string propertyName)
    {
        object? typedResult = swaggerException.GetType().GetProperty("Result")?.GetValue(swaggerException);
        return typedResult?.GetType().GetProperty(propertyName)?.GetValue(typedResult) as string;
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
            scenarioContext.Set(ex, ScenarioKeys.SwaggerException);
        }
        catch (Exception ex)
        {
            scenarioContext.Set(ex, ScenarioKeys.UnexpectedException);
        }
    }

    private void AssertRequestSucceeded()
    {
        TryGet(ScenarioKeys.UnexpectedException, out Exception? unexpectedException);
        TryGet(ScenarioKeys.SwaggerException, out SwaggerException? swaggerException);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unexpectedException, Is.Null, unexpectedException?.ToString());
            Assert.That(swaggerException, Is.Null, swaggerException?.Response);
        }
    }

    private void ResetExceptions()
    {
        scenarioContext.Remove(ScenarioKeys.SwaggerException);
        scenarioContext.Remove(ScenarioKeys.UnexpectedException);
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

    private void SetBearerToken(string? token)
    {
        if (apiClient is ExpensifyV1Client client)
        {
            client.BearerToken = token;
            return;
        }

        throw new InvalidOperationException("Expected ExpensifyV1Client implementation.");
    }

    private void SetForwardedFor(string? forwardedFor)
    {
        if (apiClient is ExpensifyV1Client client)
        {
            client.ForwardedFor = forwardedFor;
            return;
        }

        throw new InvalidOperationException("Expected ExpensifyV1Client implementation.");
    }
}

