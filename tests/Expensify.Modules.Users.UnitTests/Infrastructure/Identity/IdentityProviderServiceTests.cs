using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Expensify.Common.Application.Caching;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Domain.Identity;
using Expensify.Modules.Users.Domain.Tokens;
using Expensify.Modules.Users.Domain.Users;
using Expensify.Modules.Users.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace Expensify.Modules.Users.UnitTests.Infrastructure.Identity;

[TestFixture]
internal sealed class IdentityProviderServiceTests
{
    private const string TestSecurityKey = "ThisIsATestSecurityKeyThatIsLongEnoughForHmacSha256Algorithm!!";
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    private UserManager<IdentityUser> _userManager = null!;
    private SignInManager<IdentityUser> _signInManager = null!;
    private RoleManager<Role> _roleManager = null!;
    private IOptions<AuthSettings> _authOptions = null!;
    private TokenValidationParameters _tokenValidationParameters = null!;
    private IRefreshTokenRepository _refreshTokenRepository = null!;
    private IUserRepository _userRepository = null!;
    private IUserSessionService _userSessionService = null!;
    private IPasswordResetNotifier _passwordResetNotifier = null!;
    private IUnitOfWork _unitOfWork = null!;
    private ICacheService _cacheService = null!;
    private IdentityProviderService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        IUserStore<IdentityUser> userStore = Substitute.For<IUserStore<IdentityUser>>();
        _userManager = Substitute.For<UserManager<IdentityUser>>(
            userStore, null, null, null, null, null, null, null, null);

        IHttpContextAccessor contextAccessor = Substitute.For<IHttpContextAccessor>();
        IUserClaimsPrincipalFactory<IdentityUser> claimsFactory = Substitute.For<IUserClaimsPrincipalFactory<IdentityUser>>();
        _signInManager = Substitute.For<SignInManager<IdentityUser>>(
            _userManager, contextAccessor, claimsFactory, null, null, null, null);

        IRoleStore<Role> roleStore = Substitute.For<IRoleStore<Role>>();
        _roleManager = Substitute.For<RoleManager<Role>>(
            roleStore, null, null, null, null);

        _authOptions = Options.Create(new AuthSettings
        {
            Key = TestSecurityKey,
            Issuer = TestIssuer,
            Audience = TestAudience
        });

        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TestIssuer,
            ValidateAudience = true,
            ValidAudience = TestAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecurityKey)),
            ValidateLifetime = true,
        };

        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _userSessionService = Substitute.For<IUserSessionService>();
        _passwordResetNotifier = Substitute.For<IPasswordResetNotifier>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _cacheService = Substitute.For<ICacheService>();

        _userSessionService.InvalidateAllSessionsAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<RevocatedTokenType>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _sut = new IdentityProviderService(
            _userManager,
            _signInManager,
            _roleManager,
            _authOptions,
            _tokenValidationParameters,
            _refreshTokenRepository,
            _userRepository,
            _userSessionService,
            _passwordResetNotifier,
            _unitOfWork,
            _cacheService,
            NullLogger<IdentityProviderService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _userManager.Dispose();
        _roleManager.Dispose();
    }

    [Test]
    public async Task LoginAsync_WhenUserNotFoundByEmail_ShouldReturnFailure()
    {
        _userManager.FindByEmailAsync("unknown@example.com")
            .Returns((IdentityUser?)null);

        Result<LoginUserResponse> result = await _sut.LoginAsync("unknown@example.com", "password");

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task LoginAsync_WhenPasswordIsInvalid_ShouldReturnFailure()
    {
        var identityUser = new IdentityUser { Id = "id-1", Email = "user@example.com" };
        _userManager.FindByEmailAsync("user@example.com")
            .Returns(identityUser);
        _signInManager.CheckPasswordSignInAsync(identityUser, "wrong-password", false)
            .Returns(SignInResult.Failed);

        Result<LoginUserResponse> result = await _sut.LoginAsync("user@example.com", "wrong-password");

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task LoginAsync_WhenDomainUserNotFound_ShouldReturnEmptyTokens()
    {
        var identityUser = new IdentityUser { Id = "id-1", Email = "user@example.com" };
        _userManager.FindByEmailAsync("user@example.com")
            .Returns(identityUser);
        _signInManager.CheckPasswordSignInAsync(identityUser, "Password1!", false)
            .Returns(SignInResult.Success);
        _userManager.GetRolesAsync(identityUser)
            .Returns(["User"]);
        _roleManager.FindByNameAsync("User")
            .Returns((Role?)null);
        _userRepository.GetByIdentityIdAsync("id-1", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        Result<LoginUserResponse> result = await _sut.LoginAsync("user@example.com", "Password1!");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Token, Is.EqualTo(string.Empty));
            Assert.That(result.Value.RefreshToken, Is.EqualTo(string.Empty));
        }
    }

    [Test]
    public async Task LoginAsync_WhenCredentialsAreValid_ShouldReturnTokens()
    {
        var identityUser = new IdentityUser { Id = "id-1", Email = "user@example.com" };
        var domainUser = User.Create("John", "Doe", "id-1");

        SetupSuccessfulTokenGeneration(identityUser, domainUser);
        _signInManager.CheckPasswordSignInAsync(identityUser, "Password1!", false)
            .Returns(SignInResult.Success);

        Result<LoginUserResponse> result = await _sut.LoginAsync("user@example.com", "Password1!");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Token, Is.Not.Empty);
            Assert.That(result.Value.RefreshToken, Is.Not.Empty);
        }

        _refreshTokenRepository.Received(1).Add(Arg.Any<RefreshToken>());
        await _userSessionService.Received(1)
            .CacheSecurityStampAsync("id-1", "stamp-1", Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RegisterUserAsync_WhenIdentityCreationFails_ShouldReturnFailure()
    {
        var request = new RegisterUserRequest("test@example.com", "Password1!", "John", "Doe", RoleType.User);
        _userManager.CreateAsync(Arg.Any<IdentityUser>(), "Password1!")
            .Returns(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Creation failed" }));

        Result<string> result = await _sut.RegisterUserAsync(request);

        Assert.That(result.IsFailure, Is.True);
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Test]
    public async Task RegisterUserAsync_WhenSucceeds_ShouldReturnIdentityId()
    {
        var request = new RegisterUserRequest("test@example.com", "Password1!", "John", "Doe", RoleType.User);
        _userManager.CreateAsync(Arg.Any<IdentityUser>(), "Password1!")
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<IdentityUser>(), "User")
            .Returns(IdentityResult.Success);

        Result<string> result = await _sut.RegisterUserAsync(request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null.And.Not.Empty);
        }

        _userRepository.DidNotReceive().Add(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RegisterUserAsync_ShouldAssignCorrectRole()
    {
        var request = new RegisterUserRequest("test@example.com", "Password1!", "John", "Doe", RoleType.User);
        _userManager.CreateAsync(Arg.Any<IdentityUser>(), "Password1!")
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<IdentityUser>(), "User")
            .Returns(IdentityResult.Success);

        await _sut.RegisterUserAsync(request);

        await _userManager.Received(1).AddToRoleAsync(Arg.Any<IdentityUser>(), "User");
    }

    [Test]
    public async Task RefreshTokenAsync_WhenTokenIsInvalid_ShouldReturnFailure()
    {
        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            "not-a-valid-jwt", "some-refresh-token", CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task RefreshTokenAsync_WhenStoredRefreshTokenNotFound_ShouldReturnFailure()
    {
        string jti = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        string token = GenerateTestJwtToken(userId.ToString(), jti);

        _refreshTokenRepository.GetByIdAsync("missing-refresh-token", Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            token, "missing-refresh-token", CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task RefreshTokenAsync_WhenRefreshTokenExpired_ShouldReturnFailure()
    {
        string jti = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        string token = GenerateTestJwtToken(userId.ToString(), jti);

        var storedToken = RefreshToken.Create("expired-refresh-token", jti, userId);
        storedToken.ExpiryDate = DateTime.UtcNow.AddMinutes(-1);

        _refreshTokenRepository.GetByIdAsync("expired-refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);

        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            token, "expired-refresh-token", CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task RefreshTokenAsync_WhenRefreshTokenInvalidated_ShouldReturnFailure()
    {
        string jti = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        string token = GenerateTestJwtToken(userId.ToString(), jti);

        var storedToken = RefreshToken.Create("invalidated-refresh-token", jti, userId);
        RefreshToken.Invalidate(storedToken);

        _refreshTokenRepository.GetByIdAsync("invalidated-refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);

        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            token, "invalidated-refresh-token", CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task RefreshTokenAsync_WhenJtiDoesNotMatch_ShouldReturnFailure()
    {
        string jti = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        string token = GenerateTestJwtToken(userId.ToString(), jti);

        var storedToken = RefreshToken.Create("refresh-token", "different-jti", userId);
        _refreshTokenRepository.GetByIdAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);

        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            token, "refresh-token", CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task RefreshTokenAsync_WhenDomainUserNotFound_ShouldReturnFailure()
    {
        string jti = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        string token = GenerateTestJwtToken(userId.ToString(), jti);

        var storedToken = RefreshToken.Create("refresh-token", jti, userId);
        _refreshTokenRepository.GetByIdAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            token, "refresh-token", CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task RefreshTokenAsync_WhenIdentityUserNotFound_ShouldReturnFailure()
    {
        string jti = Guid.NewGuid().ToString();
        var domainUser = User.Create("John", "Doe", "identity-1");
        string token = GenerateTestJwtToken(domainUser.Id.ToString(), jti);

        var storedToken = RefreshToken.Create("refresh-token", jti, domainUser.Id);
        _refreshTokenRepository.GetByIdAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);
        _userRepository.GetByIdAsync(domainUser.Id, Arg.Any<CancellationToken>())
            .Returns(domainUser);
        _userManager.FindByIdAsync("identity-1")
            .Returns((IdentityUser?)null);

        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            token, "refresh-token", CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task RefreshTokenAsync_WhenValid_ShouldReturnNewTokens()
    {
        string jti = Guid.NewGuid().ToString();
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };
        var domainUser = User.Create("John", "Doe", "identity-1");
        string token = GenerateTestJwtToken(domainUser.Id.ToString(), jti, identityUser.Id, "stamp-1");

        var storedToken = RefreshToken.Create("valid-refresh-token", jti, domainUser.Id);
        _refreshTokenRepository.GetByIdAsync("valid-refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);
        _userRepository.GetByIdAsync(domainUser.Id, Arg.Any<CancellationToken>())
            .Returns(domainUser);
        _userManager.FindByIdAsync("identity-1")
            .Returns(identityUser);

        SetupSuccessfulTokenGeneration(identityUser, domainUser);

        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            token, "valid-refresh-token", CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Token, Is.Not.Empty);
            Assert.That(result.Value.RefreshToken, Is.Not.Empty);
        }
    }

    [Test]
    public async Task LogoutAsync_WhenDomainUserExists_ShouldInvalidateAllSessions()
    {
        var domainUser = User.Create("John", "Doe", "identity-1");
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };

        _userRepository.GetByIdAsync(domainUser.Id, Arg.Any<CancellationToken>())
            .Returns(domainUser);
        _userManager.FindByIdAsync(domainUser.IdentityId)
            .Returns(identityUser);

        Result result = await _sut.LogoutAsync(domainUser.Id, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _userSessionService.Received(1).InvalidateAllSessionsAsync(
            domainUser.Id,
            identityUser.Id,
            RevocatedTokenType.Invalidated,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ChangePasswordAsync_WhenCurrentPasswordIsInvalid_ShouldReturnFailure()
    {
        var domainUser = User.Create("John", "Doe", "identity-1");
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };

        _userRepository.GetByIdAsync(domainUser.Id, Arg.Any<CancellationToken>())
            .Returns(domainUser);
        _userManager.FindByIdAsync(domainUser.IdentityId)
            .Returns(identityUser);
        _userManager.ChangePasswordAsync(identityUser, "wrong-password", "NewPassword1!")
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Current password is incorrect." }));

        Result result = await _sut.ChangePasswordAsync(domainUser.Id, "wrong-password", "NewPassword1!", CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        await _userSessionService.DidNotReceive().InvalidateAllSessionsAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<RevocatedTokenType>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ChangePasswordAsync_WhenSuccessful_ShouldInvalidateAllSessions()
    {
        var domainUser = User.Create("John", "Doe", "identity-1");
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };

        _userRepository.GetByIdAsync(domainUser.Id, Arg.Any<CancellationToken>())
            .Returns(domainUser);
        _userManager.FindByIdAsync(domainUser.IdentityId)
            .Returns(identityUser);
        _userManager.ChangePasswordAsync(identityUser, "Password1!", "NewPassword1!")
            .Returns(IdentityResult.Success);

        Result result = await _sut.ChangePasswordAsync(domainUser.Id, "Password1!", "NewPassword1!", CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _userSessionService.Received(1).InvalidateAllSessionsAsync(
            domainUser.Id,
            identityUser.Id,
            RevocatedTokenType.Invalidated,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ForgotPasswordAsync_WhenUserDoesNotExist_ShouldReturnSuccessWithoutSendingNotification()
    {
        _userManager.FindByEmailAsync("missing@example.com")
            .Returns((IdentityUser?)null);

        Result result = await _sut.ForgotPasswordAsync("missing@example.com", CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _passwordResetNotifier.DidNotReceive().SendPasswordResetLinkAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ForgotPasswordAsync_WhenUserExists_ShouldGenerateTokenAndSendNotification()
    {
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };

        _userManager.FindByEmailAsync(identityUser.Email!)
            .Returns(identityUser);
        _userManager.GeneratePasswordResetTokenAsync(identityUser)
            .Returns("raw-reset-token");

        Result result = await _sut.ForgotPasswordAsync(identityUser.Email!, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _passwordResetNotifier.Received(1).SendPasswordResetLinkAsync(
            identityUser.Email!,
            Arg.Is<string>(token => Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token)) == "raw-reset-token"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResetPasswordAsync_WhenTokenCannotBeDecoded_ShouldReturnFailure()
    {
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };

        _userManager.FindByEmailAsync(identityUser.Email!)
            .Returns(identityUser);

        Result result = await _sut.ResetPasswordAsync(identityUser.Email!, "**not-valid**", "NewPassword1!", CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        await _userSessionService.DidNotReceive().InvalidateAllSessionsAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<RevocatedTokenType>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResetPasswordAsync_WhenSuccessful_ShouldInvalidateAllSessions()
    {
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };
        var domainUser = User.Create("John", "Doe", "identity-1");
        string encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("raw-reset-token"));

        _userManager.FindByEmailAsync(identityUser.Email!)
            .Returns(identityUser);
        _userManager.ResetPasswordAsync(identityUser, "raw-reset-token", "NewPassword1!")
            .Returns(IdentityResult.Success);
        _userRepository.GetByIdentityIdAsync(identityUser.Id, Arg.Any<CancellationToken>())
            .Returns(domainUser);

        Result result = await _sut.ResetPasswordAsync(identityUser.Email!, encodedToken, "NewPassword1!", CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _userSessionService.Received(1).InvalidateAllSessionsAsync(
            domainUser.Id,
            identityUser.Id,
            RevocatedTokenType.Invalidated,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteUserAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        _userManager.FindByIdAsync("identity-1")
            .Returns((IdentityUser?)null);

        Result result = await _sut.DeleteUserAsync("identity-1");

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task DeleteUserAsync_WhenDeleteFails_ShouldReturnFailure()
    {
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };
        _userManager.FindByIdAsync("identity-1")
            .Returns(identityUser);
        _userManager.DeleteAsync(identityUser)
            .Returns(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Delete failed" }));

        Result result = await _sut.DeleteUserAsync("identity-1");

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task DeleteUserAsync_WhenSucceeds_ShouldReturnSuccess()
    {
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };
        _userManager.FindByIdAsync("identity-1")
            .Returns(identityUser);
        _userManager.DeleteAsync(identityUser)
            .Returns(IdentityResult.Success);

        Result result = await _sut.DeleteUserAsync("identity-1");

        Assert.That(result.IsSuccess, Is.True);
        await _userManager.Received(1).DeleteAsync(identityUser);
    }

    private void SetupSuccessfulTokenGeneration(IdentityUser identityUser, User domainUser)
    {
        _userManager.FindByEmailAsync(identityUser.Email!)
            .Returns(identityUser);
        _userManager.GetRolesAsync(identityUser)
            .Returns(["User"]);
        _roleManager.FindByNameAsync("User")
            .Returns(new Role { Name = "User" });
        _roleManager.GetClaimsAsync(Arg.Any<Role>())
            .Returns([]);
        _userRepository.GetByIdentityIdAsync(identityUser.Id, Arg.Any<CancellationToken>())
            .Returns(domainUser);
        _userManager.GetSecurityStampAsync(identityUser)
            .Returns("stamp-1");
    }

    private static string GenerateTestJwtToken(
        string userId,
        string jti,
        string identityUserId = "identity-1",
        string securityStamp = "stamp-1")
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecurityKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, "test@example.com"),
            new(CustomClaims.UserId, userId),
            new(CustomClaims.IdentityUserId, identityUserId),
            new(CustomClaims.Role, "User"),
            new(CustomClaims.SecurityStamp, securityStamp),
            new(JwtRegisteredClaimNames.Jti, jti)
        ];

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
