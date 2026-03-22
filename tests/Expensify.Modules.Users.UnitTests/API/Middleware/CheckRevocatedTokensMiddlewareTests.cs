using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Expensify.Api.Middleware;
using Expensify.Common.Application.Caching;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Domain.Tokens;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Expensify.Modules.Users.UnitTests.API.Middleware;

[TestFixture]
internal sealed class CheckRevocatedTokensMiddlewareTests
{
    private ICacheService _cacheService = null!;
    private IIdentitySecurityStampValidator _securityStampValidator = null!;

    [SetUp]
    public void SetUp()
    {
        _cacheService = Substitute.For<ICacheService>();
        _securityStampValidator = Substitute.For<IIdentitySecurityStampValidator>();
        _securityStampValidator.IsSecurityStampValidAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    [Test]
    public async Task InvokeAsync_WhenRequestIsLogin_ShouldSkipRevocationCheckAndCallNext()
    {
        bool nextCalled = false;
        CheckRevocatedTokensMiddleware middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/users/login";

        await middleware.InvokeAsync(context, _securityStampValidator);

        Assert.That(nextCalled, Is.True);
        await _cacheService.DidNotReceiveWithAnyArgs().GetAsync<RevocatedTokenType?>(default!);
    }

    [Test]
    public async Task InvokeAsync_WhenRequestIsRefresh_ShouldSkipRevocationCheckAndCallNext()
    {
        bool nextCalled = false;
        CheckRevocatedTokensMiddleware middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/users/refresh";

        await middleware.InvokeAsync(context, _securityStampValidator);

        Assert.That(nextCalled, Is.True);
        await _cacheService.DidNotReceiveWithAnyArgs().GetAsync<RevocatedTokenType?>(default!);
    }

    [Test]
    public async Task InvokeAsync_WhenJwtIdClaimMissing_ShouldCallNext()
    {
        bool nextCalled = false;
        CheckRevocatedTokensMiddleware middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/users/profile";
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(CustomClaims.Role, "User")
            ]));

        await middleware.InvokeAsync(context, _securityStampValidator);

        Assert.That(nextCalled, Is.True);
        await _cacheService.DidNotReceiveWithAnyArgs().GetAsync<RevocatedTokenType?>(default!);
    }

    [Test]
    public async Task InvokeAsync_WhenTokenIsRevoked_ShouldReturnUnauthorizedAndNotCallNext()
    {
        CheckRevocatedTokensMiddleware middleware = CreateMiddleware(_ => Task.CompletedTask);
        _cacheService.GetAsync<RevocatedTokenType?>("test-jti", Arg.Any<CancellationToken>())
            .Returns(RevocatedTokenType.RoleChanged);

        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/users/profile";
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Jti, "test-jti"),
                new Claim(CustomClaims.Role, "User")
            ]));

        await middleware.InvokeAsync(context, _securityStampValidator);

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
    }

    [Test]
    public async Task InvokeAsync_WhenSecurityStampIsValid_ShouldCallNext()
    {
        bool nextCalled = false;
        CheckRevocatedTokensMiddleware middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        _cacheService.GetAsync<RevocatedTokenType?>("test-jti", Arg.Any<CancellationToken>())
            .Returns((RevocatedTokenType?)null);
        _securityStampValidator.IsSecurityStampValidAsync("identity-1", "stamp-1", Arg.Any<CancellationToken>())
            .Returns(true);

        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/users/profile";
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Jti, "test-jti"),
                new Claim(CustomClaims.IdentityUserId, "identity-1"),
                new Claim(CustomClaims.SecurityStamp, "stamp-1"),
                new Claim(CustomClaims.Role, "User")
            ]));

        await middleware.InvokeAsync(context, _securityStampValidator);

        Assert.That(nextCalled, Is.True);
        await _securityStampValidator.Received(1)
            .IsSecurityStampValidAsync("identity-1", "stamp-1", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InvokeAsync_WhenSecurityStampIsInvalid_ShouldReturnUnauthorized()
    {
        CheckRevocatedTokensMiddleware middleware = CreateMiddleware(_ => Task.CompletedTask);
        _cacheService.GetAsync<RevocatedTokenType?>("test-jti", Arg.Any<CancellationToken>())
            .Returns((RevocatedTokenType?)null);
        _securityStampValidator.IsSecurityStampValidAsync("identity-1", "stale-stamp", Arg.Any<CancellationToken>())
            .Returns(false);

        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/users/profile";
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Jti, "test-jti"),
                new Claim(CustomClaims.IdentityUserId, "identity-1"),
                new Claim(CustomClaims.SecurityStamp, "stale-stamp"),
                new Claim(CustomClaims.Role, "User")
            ]));

        await middleware.InvokeAsync(context, _securityStampValidator);

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
    }

    private CheckRevocatedTokensMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, _cacheService);
}
