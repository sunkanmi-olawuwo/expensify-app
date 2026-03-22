using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Expensify.Modules.Users.Infrastructure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Expensify.Modules.Users.UnitTests.Infrastructure.Identity;

[TestFixture]
internal sealed class SendGridPasswordResetNotifierTests
{
    [Test]
    public async Task SendPasswordResetLinkAsync_ShouldSendExpectedSendGridRequest()
    {
        const string encodedToken = "encoded-token";
        using var handler = new RecordingHandler(HttpStatusCode.Accepted);
        using HttpClient httpClient = CreateHttpClient(handler);
        SendGridPasswordResetNotifier sut = CreateNotifier(httpClient);

        await sut.SendPasswordResetLinkAsync("user@example.com", encodedToken, CancellationToken.None);

        Assert.That(handler.Requests, Has.Count.EqualTo(1));
        CapturedRequest request = handler.Requests.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.RequestUri, Is.EqualTo(new Uri("https://api.sendgrid.com/v3/mail/send")));
            Assert.That(request.Authorization, Is.EqualTo(new AuthenticationHeaderValue("Bearer", "sendgrid-key")));
        }

        using var json = JsonDocument.Parse(request.Body);
        string plainTextBody = json.RootElement
            .GetProperty("content")[0]
            .GetProperty("value")
            .GetString()!;

        Assert.That(plainTextBody, Does.Contain("https://app.expensify.test/reset-password?email=user@example.com&token=encoded-token"));
    }

    [Test]
    public async Task SendPasswordResetLinkAsync_WhenProviderFails_ShouldThrowWithoutLeakingToken()
    {
        const string encodedToken = "encoded-token";
        using var handler = new RecordingHandler(HttpStatusCode.BadRequest);
        using HttpClient httpClient = CreateHttpClient(handler);
        SendGridPasswordResetNotifier sut = CreateNotifier(httpClient);

        HttpRequestException exception = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await sut.SendPasswordResetLinkAsync("user@example.com", encodedToken, CancellationToken.None))!;

        Assert.That(exception.Message, Does.Not.Contain(encodedToken));
    }

    [Test]
    public void BuildResetUrl_ShouldIncludeEncodedEmailAndToken()
    {
        string url = SendGridPasswordResetNotifier.BuildResetUrl(
            "https://app.expensify.test/reset-password",
            "user@example.com",
            "encoded-token");

        Assert.That(url, Is.EqualTo("https://app.expensify.test/reset-password?email=user@example.com&token=encoded-token"));
    }

    private static SendGridPasswordResetNotifier CreateNotifier(HttpClient httpClient)
    {
        return new SendGridPasswordResetNotifier(
            httpClient,
            Options.Create(new PasswordResetOptions
            {
                ApiKey = "sendgrid-key",
                FromEmail = "noreply@expensify.test",
                FromName = "Expensify",
                ResetUrlBase = "https://app.expensify.test/reset-password"
            }),
            NullLogger<SendGridPasswordResetNotifier>.Instance);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.sendgrid.com/")
        };
    }

    private sealed class RecordingHandler(HttpStatusCode responseStatusCode) : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new CapturedRequest(request.RequestUri!, request.Headers.Authorization, body));
            return new HttpResponseMessage(responseStatusCode);
        }
    }

    private sealed record CapturedRequest(Uri RequestUri, AuthenticationHeaderValue? Authorization, string Body);
}
