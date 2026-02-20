using Reqnroll;
using Expensify.Api.Client;

namespace Expensify.IntegrationTests.StepDefinitions.Users;

[Binding]
public sealed class UserStepDefinitions(IExpensifyV1Client apiClient, ScenarioContext scenarioContext)
{
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

    [AfterScenario]
    public void AfterScenario()
    {
        SetBearerToken(null);
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
            "tutor" => ("tutor@test.com", "Test1234!"),
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

    [Given(@"I use an invalid bearer token")]
    public void GivenIUseAnInvalidBearerToken()
    {
        SetBearerToken("invalid.token.value");
        ResetExceptions();
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
            GetUserResponse userProfileResponse = await apiClient.GetUserProfileAsync(Guid.NewGuid());
            scenarioContext.Set(userProfileResponse, ScenarioKeys.UserResponse);
        });
    }

    [When(@"I update my profile to first name ""(.*)"" and last name ""(.*)""")]
    public async Task WhenIUpdateMyProfileToFirstNameAndLastName(string firstName, string lastName)
    {
        var data = new UpdateUserData(firstName, lastName);

        await ExecuteAsync(async () =>
        {
            await apiClient.UpdateUserProfileAsync(Guid.NewGuid(), data);
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
        }
    }

    [Then(@"the update profile request is successful")]
    public void ThenTheUpdateProfileRequestIsSuccessful()
    {
        AssertRequestSucceeded();
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
}
