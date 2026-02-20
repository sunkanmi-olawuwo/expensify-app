using Reqnroll;
using Expensify.Api.Client;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Expensify.IntegrationTests.StepDefinitions.Users;

[Binding]
[Scope(Feature = "Admin")]
public sealed class AdminStepDefinitions(IExpensifyV1Client apiClient, HttpClient httpClient, ScenarioContext scenarioContext)
{
    private const string GetUsersResponseKey = nameof(GetUsersResponse);
    private const string GetUsersHeadersKey = "GetUsersHeaders";

    [When(@"I delete the registered user")]
    public async Task WhenIDeleteTheRegisteredUser()
    {
        if (!TryGet(nameof(RegisterUserResponse), out RegisterUserResponse? registerResponse) || registerResponse is null)
        {
            throw new InvalidOperationException("No registered user is available for delete.");
        }

        ResetExceptions();

        try
        {
            await apiClient.DeleteUserAsync(registerResponse.UserId);
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

    [When(@"I request users with the API client")]
    public async Task WhenIRequestUsersWithTheApiClient()
    {
        ResetExceptions();

        try
        {
            GetUsersResponse response = await apiClient.GetUsersAsync();
            scenarioContext.Set(response, GetUsersResponseKey);
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

    [When(@"I request users page (.*) with page size (.*)")]
    public async Task WhenIRequestUsersPageWithPageSize(int page, int pageSize)
    {
        ResetExceptions();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users?Page={page}&PageSize={pageSize}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (apiClient is ExpensifyV1Client client && !string.IsNullOrWhiteSpace(client.BearerToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", client.BearerToken);
            }

            using HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new SwaggerException(
                    "The HTTP status code of the response was not expected.",
                    (int)response.StatusCode,
                    responseText,
                    ToHeaderDictionary(response),
                    null);
            }

            GetUsersResponse? users = JsonSerializer.Deserialize<GetUsersResponse>(responseText);
            if (users is null)
            {
                throw new InvalidOperationException("Expected users response payload.");
            }

            scenarioContext.Set(users, GetUsersResponseKey);
            scenarioContext.Set(ToHeaderDictionary(response), GetUsersHeadersKey);
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

    [Then(@"the get users request is successful")]
    public void ThenTheGetUsersRequestIsSuccessful()
    {
        TryGet(nameof(SwaggerException), out SwaggerException? swaggerException);
        TryGet("UnexpectedException", out Exception? unexpectedException);
        bool hasResponse = TryGet(GetUsersResponseKey, out GetUsersResponse? response);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unexpectedException, Is.Null, unexpectedException?.ToString());
            Assert.That(swaggerException, Is.Null, swaggerException?.Response);
            Assert.That(hasResponse, Is.True);
            Assert.That(response, Is.Not.Null);
        }
    }

    [Then(@"the pagination headers are returned and match the response")]
    public void ThenThePaginationHeadersAreReturnedAndMatchTheResponse()
    {
        bool hasResponse = TryGet(GetUsersResponseKey, out GetUsersResponse? response);
        bool hasHeaders = TryGet(GetUsersHeadersKey, out IReadOnlyDictionary<string, IEnumerable<string>>? headers);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(hasResponse, Is.True);
            Assert.That(response, Is.Not.Null);
            Assert.That(hasHeaders, Is.True);
            Assert.That(headers, Is.Not.Null);
        }

        AssertHeaderValue(headers!, "X-Pagination-CurrentPage", response!.CurentPage);
        AssertHeaderValue(headers!, "X-Pagination-PageSize", response.PageSize);
        AssertHeaderValue(headers!, "X-Pagination-TotalCount", response.TotalCount);
        AssertHeaderValue(headers!, "X-Pagination-TotalPages", response.TotalPages);
    }

    [Then(@"the delete request is successful")]
    public void ThenTheDeleteRequestIsSuccessful()
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
        scenarioContext.Remove(GetUsersResponseKey);
        scenarioContext.Remove(GetUsersHeadersKey);
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

    private static Dictionary<string, IEnumerable<string>> ToHeaderDictionary(HttpResponseMessage response)
    {
        var headers = response.Headers
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Content.Headers)
        {
            headers[header.Key] = header.Value;
        }

        return headers;
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
