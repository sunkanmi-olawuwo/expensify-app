using System.Security.Claims;
using Expensify.Common.Application.Exceptions;

namespace Expensify.Common.Infrastructure.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal? principal)
    {
        string? userId = principal?.FindFirst("userid")?.Value;

        return Guid.TryParse(userId, out Guid parsedUserId) ?
            parsedUserId :
            throw new ExpensifyException("User identifier is unavailable");
    }
}
