using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Expensify.Api.Client;

public partial interface IExpensifyV1Client
{
    Task<ExpenseResponse> CreateExpenseWithoutTagIdsAsync(
        decimal amount,
        Guid categoryId,
        string currency,
        DateTime date,
        string merchant,
        string note,
        PaymentMethod paymentMethod,
        CancellationToken cancellationToken = default);
}

public partial class ExpensifyV1Client
{
    public IReadOnlyDictionary<string, IEnumerable<string>> LastResponseHeaders { get; private set; } =
        new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

    public string? BearerToken { get; set; }

    static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        settings.Converters.Add(new JsonStringEnumConverter());
    }

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
    {
        AddAuthHeader(request);
    }

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, StringBuilder urlBuilder)
    {
        AddAuthHeader(request);
    }

    partial void ProcessResponse(HttpClient client, HttpResponseMessage response)
    {
        Dictionary<string, IEnumerable<string>> headers = new(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
        {
            headers[header.Key] = header.Value;
        }

        if (response.Content is not null)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> header in response.Content.Headers)
            {
                headers[header.Key] = header.Value;
            }
        }

        LastResponseHeaders = headers;
    }

    private void AddAuthHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(BearerToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", BearerToken);
        }
    }

    public async Task<ExpenseResponse> CreateExpenseWithoutTagIdsAsync(
        decimal amount,
        Guid categoryId,
        string currency,
        DateTime date,
        string merchant,
        string note,
        PaymentMethod paymentMethod,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClient;

        using HttpRequestMessage request = new();
        request.Method = HttpMethod.Post;
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

        string payload = JsonSerializer.Serialize(new
        {
            amount,
            categoryId,
            currency,
            date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            merchant,
            note,
            paymentMethod
        }, JsonSerializerSettings);

        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        StringBuilder urlBuilder = new();
        urlBuilder.Append("expenses");

        PrepareRequest(client, request, urlBuilder);
        string url = urlBuilder.ToString();
        request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
        PrepareRequest(client, request, url);

        using HttpResponseMessage response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, IEnumerable<string>> headers = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, IEnumerable<string>> item in response.Headers)
        {
            headers[item.Key] = item.Value;
        }

        if (response.Content is not null)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> item in response.Content.Headers)
            {
                headers[item.Key] = item.Value;
            }
        }

        ProcessResponse(client, response);

        int status = (int)response.StatusCode;
        if (status == (int)HttpStatusCode.Created)
        {
            ObjectResponseResult<ExpenseResponse> objectResponse =
                await ReadObjectResponseAsync<ExpenseResponse>(response, headers, cancellationToken).ConfigureAwait(false);

            if (objectResponse.Object is null)
            {
                throw new SwaggerException("Response was null which was not expected.", status, objectResponse.Text, headers, null);
            }

            return objectResponse.Object;
        }

        string? responseData = response.Content is null
            ? null
            : await ReadAsStringAsync(response.Content, cancellationToken).ConfigureAwait(false);

        throw new SwaggerException(
            $"The HTTP status code of the response was not expected ({status}).",
            status,
            responseData,
            headers,
            null);
    }
}
