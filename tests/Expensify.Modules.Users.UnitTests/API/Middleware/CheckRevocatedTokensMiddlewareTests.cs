using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Expensify.Api.Middleware;
using Expensify.Common.Application.Caching;
using Expensify.Modules.Users.Domain.Tokens;

namespace Expensify.Modules.Users.UnitTests.API.Middleware;

[TestFixture]
internal sealed class CheckRevocatedTokensMiddlewareTests
{
    private ICacheService _cacheService;

    [SetUp]
    public void SetUp()
    {
        _cacheService = Substitute.For<ICacheService>();
    }

    [Test]
    public async Task InvokeAsync_WhenRequestIsLogin_ShouldSkipRevocationCheckAndCallNext()
    {
        bool nextCalled = false;
        CheckRevocatedTokensMiddleware middleware = new(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            _cacheService);

        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/users/login";

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.True);
        await _cacheService.DidNotReceiveWithAnyArgs().GetAsync<RevocatedTokenType?>(default!);
    }

    [Test]
    public async Task InvokeAsync_WhenRequestIsRefresh_ShouldSkipRevocationCheckAndCallNext()
    {
        bool nextCalled = false;
        CheckRevocatedTokensMiddleware middleware = new(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            _cacheService);

        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/users/refresh";

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.True);
        await _cacheService.DidNotReceiveWithAnyArgs().GetAsync<RevocatedTokenType?>(default!);
    }

    [Test]
    public async Task InvokeAsync_WhenJwtIdClaimMissing_ShouldCallNext()
    {
        bool nextCalled = false;
        CheckRevocatedTokensMiddleware middleware = new(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            _cacheService);

        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/users/123/profile";
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim("role", "User")
            ]));

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.True);
        await _cacheService.DidNotReceiveWithAnyArgs().GetAsync<RevocatedTokenType?>(default!);
    }

    [Test]
    public async Task InvokeAsync_WhenRoleClaimMissing_ShouldStillCheckRevocationAndCallNextWhenNotRevoked()
    {
        bool nextCalled = false;
        CheckRevocatedTokensMiddleware middleware = new(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            _cacheService);

        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/users/123/profile";
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Jti, "test-jti")
            ]));

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.True);
        await _cacheService.Received(1).GetAsync<RevocatedTokenType?>("test-jti", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InvokeAsync_WhenTokenIsRevoked_ShouldReturnUnauthorizedAndNotCallNext()
    {
        CheckRevocatedTokensMiddleware middleware = new(_ => Task.CompletedTask, _cacheService);
        _cacheService.GetAsync<RevocatedTokenType?>("test-jti", Arg.Any<CancellationToken>())
            .Returns(RevocatedTokenType.RoleChanged);

        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/users/123/profile";
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Jti, "test-jti"),
                new Claim("role", "User")
            ]));

        await middleware.InvokeAsync(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
    }

    [Test]
    public async Task InvokeAsync_WhenTokenIsNotRevoked_ShouldCallNext()
    {
        bool nextCalled = false;
        CheckRevocatedTokensMiddleware middleware = new(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            _cacheService);
        _cacheService.GetAsync<RevocatedTokenType?>("test-jti", Arg.Any<CancellationToken>())
            .Returns((RevocatedTokenType?)null);

        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/users/123/profile";
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Jti, "test-jti"),
                new Claim("role", "User")
            ]));

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.True);
        await _cacheService.Received(1).GetAsync<RevocatedTokenType?>("test-jti", Arg.Any<CancellationToken>());
    }
}
