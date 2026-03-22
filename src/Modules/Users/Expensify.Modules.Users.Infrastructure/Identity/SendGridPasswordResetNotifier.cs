using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Expensify.Modules.Users.Application.Abstractions.Identity;

namespace Expensify.Modules.Users.Infrastructure.Identity;

internal sealed class SendGridPasswordResetNotifier(
    HttpClient httpClient,
    IOptions<PasswordResetOptions> passwordResetOptions,
    ILogger<SendGridPasswordResetNotifier> logger)
    : IPasswordResetNotifier
{
    private const string SendGridEndpoint = "v3/mail/send";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SendPasswordResetLinkAsync(string email, string encodedToken, CancellationToken cancellationToken = default)
    {
        string resetUrl = BuildResetUrl(passwordResetOptions.Value.ResetUrlBase, email, encodedToken);

        using HttpRequestMessage request = new(HttpMethod.Post, SendGridEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", passwordResetOptions.Value.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(CreateRequest(email, resetUrl), JsonOptions),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        logger.LogError(
            "SendGrid password reset email failed for '{MaskedEmail}' with status code {StatusCode}",
            MaskEmailAddress(email),
            (int)response.StatusCode);

        response.EnsureSuccessStatusCode();
    }

    internal static string BuildResetUrl(string resetUrlBase, string email, string encodedToken)
    {
        return QueryHelpers.AddQueryString(
            resetUrlBase,
            new Dictionary<string, string?>
            {
                ["email"] = email,
                ["token"] = encodedToken
            });
    }

    private static string MaskEmailAddress(string email)
    {
        int atIndex = email.IndexOf('@');
        if (atIndex <= 1 || atIndex == email.Length - 1)
        {
            return "***";
        }

        string localPart = email[..atIndex];
        string domainPart = email[(atIndex + 1)..];

        return $"{localPart[0]}***@{domainPart[0]}***";
    }

    private SendGridMailRequest CreateRequest(string email, string resetUrl)
    {
        string plainTextBody = $"Reset your Expensify password by visiting: {resetUrl}";
        string htmlBody = $"<p>Reset your Expensify password by visiting <a href=\"{resetUrl}\">this link</a>.</p>";

        return new SendGridMailRequest(
            [new SendGridPersonalization([new SendGridEmailAddress(email, null)], "Reset your Expensify password")],
            new SendGridEmailAddress(passwordResetOptions.Value.FromEmail, passwordResetOptions.Value.FromName),
            [
                new SendGridContent("text/plain", plainTextBody),
                new SendGridContent("text/html", htmlBody)
            ]);
    }

    private sealed record SendGridMailRequest(
        IReadOnlyCollection<SendGridPersonalization> Personalizations,
        SendGridEmailAddress From,
        IReadOnlyCollection<SendGridContent> Content);

    private sealed record SendGridPersonalization(
        IReadOnlyCollection<SendGridEmailAddress> To,
        string Subject);

    private sealed record SendGridEmailAddress(string Email, string? Name);

    private sealed record SendGridContent(string Type, string Value);
}
