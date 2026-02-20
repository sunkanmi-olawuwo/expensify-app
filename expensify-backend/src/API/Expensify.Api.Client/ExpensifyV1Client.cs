using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Expensify.Api.Client;

public partial class ExpensifyV1Client
{
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

    private void AddAuthHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(BearerToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", BearerToken);
        }
    }
}
