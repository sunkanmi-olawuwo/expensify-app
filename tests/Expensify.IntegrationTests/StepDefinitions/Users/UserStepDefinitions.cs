using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text.Json;
using Expensify.Api.Client;
using Expensify.Common.Application.Caching;
using Expensify.IntegrationTests.Driver;
using Expensify.Modules.Users.Domain.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Reqnroll;

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
        public const string SecondaryLoginUserResponse = nameof(SecondaryLoginUserResponse);
        public const string RegisterUserResponse = nameof(RegisterUserResponse);
        public const string UserResponse = nameof(UserResponse);
        public const string SwaggerException = nameof(SwaggerException);
        public const string UnexpectedException = nameof(UnexpectedException);
        public const string PasswordResetEmail = nameof(PasswordResetEmail);
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        int scenarioIndex = Interlocked.Increment(ref _scenarioCounter);
        int thirdOctet = 1 + scenarioIndex / 254 % 254;
        int fourthOctet = 1 + scenarioIndex % 254;
        SetForwardedFor($"203.0.{thirdOctet}.{fourthOctet}");
        apiDriver.PasswordResetNotifier.Clear();
    }

    [AfterScenario]
    public void AfterScenario()
    {
        SetBearerToken(null);
        SetForwardedFor(null);
        apiDriver.PasswordResetNotifier.Clear();
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
        await LoginAsAsync(accountType, ScenarioKeys.LoginUserResponse, setBearerToken: true);
    }

    [Given(@"I also log in as ""(.*)"" in a secondary session")]
    public async Task GivenIAlsoLogInAsInASecondarySession(string accountType)
    {
        await LoginAsAsync(accountType, ScenarioKeys.SecondaryLoginUserResponse, setBearerToken: false);
    }

    [Given(@"I also log in as the newly registered user in a secondary session")]
    public async Task GivenIAlsoLogInAsTheNewlyRegisteredUserInASecondarySession()
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
            scenarioContext.Set(loginResponse, ScenarioKeys.SecondaryLoginUserResponse);
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

    [Given(@"I request a password reset for email ""(.*)""")]
    [When(@"I request a password reset for email ""(.*)""")]
    public async Task WhenIRequestAPasswordResetForEmail(string email)
    {
        await ExecuteAsync(async () =>
        {
            await apiClient.ForgotPasswordAsync(new ForgotPasswordRequest(email));
        });
    }

    [Given(@"I request a password reset for the newly registered user email")]
    [When(@"I request a password reset for the newly registered user email")]
    public async Task WhenIRequestAPasswordResetForTheNewlyRegisteredUserEmail()
    {
        if (!TryGet(ScenarioKeys.RegisterUserCommand, out RegisterUserCommand? registerUserCommand) || registerUserCommand is null)
        {
            throw new InvalidOperationException("Registration command is required before requesting a password reset.");
        }

        await WhenIRequestAPasswordResetForEmail(registerUserCommand.Email);
    }

    [Given(@"a password reset link is captured for email ""(.*)""")]
    [Then(@"a password reset link is captured for email ""(.*)""")]
    public void ThenAPasswordResetLinkIsCapturedForEmail(string email)
    {
        InMemoryPasswordResetNotifier.PasswordResetDelivery? delivery = apiDriver.PasswordResetNotifier.GetLatest(email);
        Assert.That(delivery, Is.Not.Null, $"Expected a password reset delivery for '{email}'.");
        scenarioContext.Set(email, ScenarioKeys.PasswordResetEmail);
    }

    [Then(@"no password reset link is captured for email ""(.*)""")]
    public void ThenNoPasswordResetLinkIsCapturedForEmail(string email)
    {
        InMemoryPasswordResetNotifier.PasswordResetDelivery? delivery = apiDriver.PasswordResetNotifier.GetLatest(email);
        Assert.That(delivery, Is.Null);
    }

    [Given(@"a password reset link is captured for the newly registered user email")]
    [Then(@"a password reset link is captured for the newly registered user email")]
    public void ThenAPasswordResetLinkIsCapturedForTheNewlyRegisteredUserEmail()
    {
        if (!TryGet(ScenarioKeys.RegisterUserCommand, out RegisterUserCommand? registerUserCommand) || registerUserCommand is null)
        {
            throw new InvalidOperationException("Registration command is required before asserting the captured password reset link.");
        }

        ThenAPasswordResetLinkIsCapturedForEmail(registerUserCommand.Email);
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

    [Given(@"the newly registered user email with password ""(.*)""")]
    public void GivenTheNewlyRegisteredUserEmailWithPassword(string password)
    {
        if (!TryGet(ScenarioKeys.RegisterUserCommand, out RegisterUserCommand? registerUserCommand) || registerUserCommand is null)
        {
            throw new InvalidOperationException("Registration command is required before reusing the newly registered user email.");
        }

        scenarioContext.Set(new LoginCommand(registerUserCommand.Email, password), ScenarioKeys.LoginCommand);
        ResetExceptions();
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

    [Given(@"I submit the user registration request")]
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

    [When(@"I log out of the current account")]
    public async Task WhenILogOutOfTheCurrentAccount()
    {
        await ExecuteAsync(async () => { await apiClient.LogoutAsync(); });
    }

    [When(@"I change my password from ""(.*)"" to ""(.*)""")]
    public async Task WhenIChangeMyPasswordFromTo(string currentPassword, string newPassword)
    {
        await ExecuteAsync(async () =>
        {
            await apiClient.ChangePasswordAsync(new ChangePasswordRequest(currentPassword, newPassword));
        });
    }

    [When(@"I reset the password for email ""(.*)"" to ""(.*)"" using the captured reset token")]
    public async Task WhenIResetThePasswordUsingTheCapturedResetToken(string email, string newPassword)
    {
        InMemoryPasswordResetNotifier.PasswordResetDelivery? delivery = apiDriver.PasswordResetNotifier.GetLatest(email);
        if (delivery is null)
        {
            throw new InvalidOperationException($"Expected a captured password reset delivery for '{email}'.");
        }

        await ExecuteAsync(async () =>
        {
            await apiClient.ResetPasswordAsync(new ResetPasswordRequest(email, newPassword, delivery.EncodedToken));
        });
    }

    [When(@"I reset the password for the newly registered user to ""(.*)"" using the captured reset token")]
    public async Task WhenIResetThePasswordForTheNewlyRegisteredUserUsingTheCapturedResetToken(string newPassword)
    {
        if (!TryGet(ScenarioKeys.RegisterUserCommand, out RegisterUserCommand? registerUserCommand) || registerUserCommand is null)
        {
            throw new InvalidOperationException("Registration command is required before resetting the newly registered user's password.");
        }

        await WhenIResetThePasswordUsingTheCapturedResetToken(registerUserCommand.Email, newPassword);
    }

    [When(@"I reset the password for email ""(.*)"" to ""(.*)"" using token ""(.*)""")]
    public async Task WhenIResetThePasswordUsingToken(string email, string newPassword, string token)
    {
        await ExecuteAsync(async () =>
        {
            await apiClient.ResetPasswordAsync(new ResetPasswordRequest(email, newPassword, token));
        });
    }

    [When(@"I reset the password for the newly registered user to ""(.*)"" using token ""(.*)""")]
    public async Task WhenIResetThePasswordForTheNewlyRegisteredUserUsingToken(string newPassword, string token)
    {
        if (!TryGet(ScenarioKeys.RegisterUserCommand, out RegisterUserCommand? registerUserCommand) || registerUserCommand is null)
        {
            throw new InvalidOperationException("Registration command is required before resetting the newly registered user's password.");
        }

        await WhenIResetThePasswordUsingToken(registerUserCommand.Email, newPassword, token);
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

    [Given(@"the registration request is successful")]
    [Then(@"the registration request is successful")]
    public void ThenTheRegistrationRequestIsSuccessful()
    {
        AssertRequestSucceeded();
        Assert.That(TryGet(ScenarioKeys.RegisterUserResponse, out RegisterUserResponse? registerResponse), Is.True);
        Assert.That(registerResponse, Is.Not.Null);
        Assert.That(registerResponse!.UserId, Is.Not.EqualTo(Guid.Empty));
    }

    [Then(@"the logout request is successful")]
    public void ThenTheLogoutRequestIsSuccessful()
    {
        AssertRequestSucceeded();
    }

    [Then(@"the change password request is successful")]
    public void ThenTheChangePasswordRequestIsSuccessful()
    {
        AssertRequestSucceeded();
    }

    [Then(@"the forgot password request is successful")]
    public void ThenTheForgotPasswordRequestIsSuccessful()
    {
        AssertRequestSucceeded();
    }

    [Then(@"the reset password request is successful")]
    public void ThenTheResetPasswordRequestIsSuccessful()
    {
        AssertRequestSucceeded();
    }

    [Then(@"the current session is rejected when I request my user profile")]
    public async Task ThenTheCurrentSessionIsRejectedWhenIRequestMyUserProfile()
    {
        LoginUserResponse loginResponse = scenarioContext.Get<LoginUserResponse>(ScenarioKeys.LoginUserResponse);

        await AssertProfileRequestFailsWithTokenAsync(loginResponse.Token, StatusCodes.Status401Unauthorized);
    }

    [Then(@"the secondary session is rejected when I request my user profile")]
    public async Task ThenTheSecondarySessionIsRejectedWhenIRequestMyUserProfile()
    {
        LoginUserResponse loginResponse = scenarioContext.Get<LoginUserResponse>(ScenarioKeys.SecondaryLoginUserResponse);

        await AssertProfileRequestFailsWithTokenAsync(loginResponse.Token, StatusCodes.Status401Unauthorized);
    }

    [Then(@"refreshing the secondary session fails")]
    public async Task ThenRefreshingTheSecondarySessionFails()
    {
        LoginUserResponse loginResponse = scenarioContext.Get<LoginUserResponse>(ScenarioKeys.SecondaryLoginUserResponse);

        await ExecuteAsync(async () =>
        {
            await apiClient.RefreshTokenAsync(new RefreshTokenCommand(loginResponse.Token, loginResponse.RefreshToken));
        });

        ThenTheRequestFailsWithStatusCode(StatusCodes.Status400BadRequest);
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

    private async Task LoginAsAsync(string accountType, string responseKey, bool setBearerToken)
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
            LoginUserResponse loginResponse = await apiClient.LoginAsync(new LoginCommand(email, password));
            scenarioContext.Set(loginResponse, responseKey);
            if (setBearerToken)
            {
                SetBearerToken(loginResponse.Token);
            }
        });
        AssertRequestSucceeded();
    }

    private async Task AssertProfileRequestFailsWithTokenAsync(string token, int expectedStatusCode)
    {
        SetBearerToken(token);
        await ExecuteAsync(async () => { await apiClient.GetUserProfileAsync(); });
        ThenTheRequestFailsWithStatusCode(expectedStatusCode);
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
