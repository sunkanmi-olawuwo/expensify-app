using Expensify.Common.Application.Caching;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Domain.Tokens;
using Expensify.Modules.Users.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Expensify.Modules.Users.UnitTests.Infrastructure.Identity;

[TestFixture]
internal sealed class UserSessionServiceTests
{
    private UserManager<IdentityUser> _userManager = null!;
    private IRefreshTokenRepository _refreshTokenRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private ICacheService _cacheService = null!;
    private UserSessionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        IUserStore<IdentityUser> userStore = Substitute.For<IUserStore<IdentityUser>>();
        _userManager = Substitute.For<UserManager<IdentityUser>>(
            userStore, null, null, null, null, null, null, null, null);

        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _cacheService = Substitute.For<ICacheService>();

        _sut = new UserSessionService(
            _userManager,
            _refreshTokenRepository,
            _unitOfWork,
            _cacheService,
            NullLogger<UserSessionService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _userManager.Dispose();
    }

    [Test]
    public async Task InvalidateAllSessionsAsync_WhenSuccessful_ShouldInvalidateTokensUpdateSecurityStampAndCacheIt()
    {
        var domainUserId = Guid.NewGuid();
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };
        var refreshToken = RefreshToken.Create("refresh-token", "jwt-1", domainUserId);

        _refreshTokenRepository.GetValidTokenAsync(domainUserId, Arg.Any<CancellationToken>())
            .Returns([refreshToken]);
        _userManager.FindByIdAsync(identityUser.Id)
            .Returns(identityUser);
        _userManager.UpdateSecurityStampAsync(identityUser)
            .Returns(IdentityResult.Success);
        _userManager.GetSecurityStampAsync(identityUser)
            .Returns("new-stamp");

        Result result = await _sut.InvalidateAllSessionsAsync(
            domainUserId,
            identityUser.Id,
            RevocatedTokenType.Invalidated,
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(refreshToken.Invalidated, Is.True);
        }

        await _cacheService.Received(1)
            .SetAsync("jwt-1", RevocatedTokenType.Invalidated, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
        await _cacheService.Received(1)
            .SetAsync("users:security-stamp:identity-1", "new-stamp", Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InvalidateAllSessionsAsync_WhenSecurityStampUpdateFails_ShouldReturnFailure()
    {
        var domainUserId = Guid.NewGuid();
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };

        _refreshTokenRepository.GetValidTokenAsync(domainUserId, Arg.Any<CancellationToken>())
            .Returns([]);
        _userManager.FindByIdAsync(identityUser.Id)
            .Returns(identityUser);
        _userManager.UpdateSecurityStampAsync(identityUser)
            .Returns(IdentityResult.Failed(new IdentityError { Description = "failed" }));

        Result result = await _sut.InvalidateAllSessionsAsync(
            domainUserId,
            identityUser.Id,
            RevocatedTokenType.Invalidated,
            CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InvalidateAllSessionsAsync_WhenIdentityUserDoesNotExist_ShouldReturnFailure()
    {
        var domainUserId = Guid.NewGuid();

        _refreshTokenRepository.GetValidTokenAsync(domainUserId, Arg.Any<CancellationToken>())
            .Returns([]);
        _userManager.FindByIdAsync("missing")
            .Returns((IdentityUser?)null);

        Result result = await _sut.InvalidateAllSessionsAsync(
            domainUserId,
            "missing",
            RevocatedTokenType.Invalidated,
            CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task IsSecurityStampValidAsync_WhenValueExistsInCache_ShouldReturnComparisonResult()
    {
        _cacheService.GetAsync<string>("users:security-stamp:identity-1", Arg.Any<CancellationToken>())
            .Returns("cached-stamp");

        bool result = await _sut.IsSecurityStampValidAsync("identity-1", "cached-stamp", CancellationToken.None);

        Assert.That(result, Is.True);
        await _userManager.DidNotReceive().FindByIdAsync(Arg.Any<string>());
    }

    [Test]
    public async Task IsSecurityStampValidAsync_WhenCacheMiss_ShouldLoadUserAndCacheValue()
    {
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };

        _cacheService.GetAsync<string>("users:security-stamp:identity-1", Arg.Any<CancellationToken>())
            .Returns((string?)null);
        _userManager.FindByIdAsync("identity-1")
            .Returns(identityUser);
        _userManager.GetSecurityStampAsync(identityUser)
            .Returns("live-stamp");

        bool result = await _sut.IsSecurityStampValidAsync("identity-1", "live-stamp", CancellationToken.None);

        Assert.That(result, Is.True);
        await _cacheService.Received(1).SetAsync(
            "users:security-stamp:identity-1",
            "live-stamp",
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }
}
