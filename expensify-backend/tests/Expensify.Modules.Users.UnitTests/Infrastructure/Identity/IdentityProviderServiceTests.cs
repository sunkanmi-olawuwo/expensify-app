using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Expensify.Common.Application.Caching;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Domain.Identity;
using Expensify.Modules.Users.Domain.Tokens;
using Expensify.Modules.Users.Domain.Users;
using Expensify.Modules.Users.Infrastructure.Identity;

namespace Expensify.Modules.Users.UnitTests.Infrastructure.Identity;

[TestFixture]
internal sealed class IdentityProviderServiceTests
{
    private const string TestSecurityKey = "ThisIsATestSecurityKeyThatIsLongEnoughForHmacSha256Algorithm!!";
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    private UserManager<IdentityUser> _userManager;
    private SignInManager<IdentityUser> _signInManager;
    private RoleManager<Role> _roleManager;
    private IOptions<AuthSettings> _authOptions;
    private TokenValidationParameters _tokenValidationParameters;
    private IRefreshTokenRepository _refreshTokenRepository;
    private IUserRepository _userRepository;
    private IUnitOfWork _unitOfWork;
    private ICacheService _cacheService;
    private IdentityProviderService _sut;

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
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _cacheService = Substitute.For<ICacheService>();

        _sut = new IdentityProviderService(
            _userManager,
            _signInManager,
            _roleManager,
            _authOptions,
            _tokenValidationParameters,
            _refreshTokenRepository,
            _userRepository,
            _unitOfWork,
            _cacheService,
            NullLogger<IdentityProviderService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _userManager?.Dispose();
        _roleManager?.Dispose();
    }

    #region LoginAsync

    [Test]
    public async Task LoginAsync_WhenUserNotFoundByEmail_ShouldReturnFailure()
    {
        // Arrange
        _userManager.FindByEmailAsync("unknown@example.com")
            .Returns((IdentityUser?)null);

        // Act
        Result<LoginUserResponse> result = await _sut.LoginAsync("unknown@example.com", "password");

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task LoginAsync_WhenPasswordIsInvalid_ShouldReturnFailure()
    {
        // Arrange
        var identityUser = new IdentityUser { Id = "id-1", Email = "user@example.com" };
        _userManager.FindByEmailAsync("user@example.com")
            .Returns(identityUser);
        _signInManager.CheckPasswordSignInAsync(identityUser, "wrong-password", false)
            .Returns(SignInResult.Failed);

        // Act
        Result<LoginUserResponse> result = await _sut.LoginAsync("user@example.com", "wrong-password");

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task LoginAsync_WhenDomainUserNotFound_ShouldReturnEmptyTokens()
    {
        // Arrange
        var identityUser = new IdentityUser { Id = "id-1", Email = "user@example.com" };
        _userManager.FindByEmailAsync("user@example.com")
            .Returns(identityUser);
        _signInManager.CheckPasswordSignInAsync(identityUser, "Password1!", false)
            .Returns(SignInResult.Success);
        _userManager.GetRolesAsync(identityUser)
            .Returns(["Parent"]);
        _roleManager.FindByNameAsync("Parent")
            .Returns((Role?)null);
        _userRepository.GetByIdentityIdAsync("id-1", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        Result<LoginUserResponse> result = await _sut.LoginAsync("user@example.com", "Password1!");

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Token, Is.EqualTo(string.Empty));
            Assert.That(result.Value.RefreshToken, Is.EqualTo(string.Empty));
        }
    }

    [Test]
    public async Task LoginAsync_WhenCredentialsAreValid_ShouldReturnTokens()
    {
        // Arrange
        var identityUser = new IdentityUser { Id = "id-1", Email = "user@example.com" };
        var domainUser = User.Create("John", "Doe", "id-1");

        SetupSuccessfulTokenGeneration(identityUser, domainUser);

        _signInManager.CheckPasswordSignInAsync(identityUser, "Password1!", false)
            .Returns(SignInResult.Success);

        // Act
        Result<LoginUserResponse> result = await _sut.LoginAsync("user@example.com", "Password1!");

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Token, Is.Not.Empty);
            Assert.That(result.Value.RefreshToken, Is.Not.Empty);
        }
        _refreshTokenRepository.Received(1).Add(Arg.Any<RefreshToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region RegisterUserAsync

    [Test]
    public async Task RegisterUserAsync_WhenIdentityCreationFails_ShouldReturnFailure()
    {
        // Arrange
        var request = new RegisterUserRequest("test@example.com", "Password1!", "John", "Doe", RoleType.Parent);
        _userManager.CreateAsync(Arg.Any<IdentityUser>(), "Password1!")
            .Returns(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Creation failed" }));

        // Act
        Result<string> result = await _sut.RegisterUserAsync(request);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Test]
    public async Task RegisterUserAsync_WhenSucceeds_ShouldReturnIdentityId()
    {
        // Arrange
        var request = new RegisterUserRequest("test@example.com", "Password1!", "John", "Doe", RoleType.Parent);
        _userManager.CreateAsync(Arg.Any<IdentityUser>(), "Password1!")
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<IdentityUser>(), "Parent")
            .Returns(IdentityResult.Success);

        // Act
        Result<string> result = await _sut.RegisterUserAsync(request);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null.And.Not.Empty);
        }
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RegisterUserAsync_ShouldAssignCorrectRole()
    {
        // Arrange
        var request = new RegisterUserRequest("test@example.com", "Password1!", "John", "Doe", RoleType.Tutor);
        _userManager.CreateAsync(Arg.Any<IdentityUser>(), "Password1!")
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<IdentityUser>(), "Tutor")
            .Returns(IdentityResult.Success);

        // Act
        await _sut.RegisterUserAsync(request);

        // Assert
        await _userManager.Received(1).AddToRoleAsync(Arg.Any<IdentityUser>(), "Tutor");
    }

    #endregion

    #region RefreshTokenAsync

    [Test]
    public async Task RefreshTokenAsync_WhenTokenIsInvalid_ShouldReturnFailure()
    {
        // Arrange — invalid (unparseable) token
        // Act
        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            "not-a-valid-jwt", "some-refresh-token", CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task RefreshTokenAsync_WhenStoredRefreshTokenNotFound_ShouldReturnFailure()
    {
        // Arrange
        string jti = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        string token = GenerateTestJwtToken(userId.ToString(), jti);

        _refreshTokenRepository.GetByIdAsync("missing-refresh-token", Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        // Act
        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            token, "missing-refresh-token", CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task RefreshTokenAsync_WhenRefreshTokenExpired_ShouldReturnFailure()
    {
        // Arrange
        string jti = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        string token = GenerateTestJwtToken(userId.ToString(), jti);

        var storedToken = RefreshToken.Create("expired-refresh-token", jti, userId);
        _refreshTokenRepository.GetByIdAsync("expired-refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);

        // Act
        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            token, "expired-refresh-token", CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task RefreshTokenAsync_WhenRefreshTokenInvalidated_ShouldReturnFailure()
    {
        // Arrange
        string jti = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        string token = GenerateTestJwtToken(userId.ToString(), jti);

        var storedToken = RefreshToken.Create("invalidated-refresh-token", jti, userId);
        RefreshToken.Invalidate(storedToken);
        
        _refreshTokenRepository.GetByIdAsync("invalidated-refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);

        // Act
        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            token, "invalidated-refresh-token", CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task RefreshTokenAsync_WhenJtiDoesNotMatch_ShouldReturnFailure()
    {
        // Arrange
        string jti = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        string token = GenerateTestJwtToken(userId.ToString(), jti);

        var storedToken = RefreshToken.Create("refresh-token", "different-jti", userId);
        _refreshTokenRepository.GetByIdAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);

        // Act
        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            token, "refresh-token", CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task RefreshTokenAsync_WhenDomainUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        string jti = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        string token = GenerateTestJwtToken(userId.ToString(), jti);

        var storedToken = RefreshToken.Create("refresh-token", jti, userId);
        _refreshTokenRepository.GetByIdAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            token, "refresh-token", CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task RefreshTokenAsync_WhenIdentityUserNotFound_ShouldReturnFailure()
    {
        // Arrange
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

        // Act
        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            token, "refresh-token", CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task RefreshTokenAsync_WhenValid_ShouldReturnNewTokens()
    {
        // Arrange
        string jti = Guid.NewGuid().ToString();
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };
        var domainUser = User.Create("John", "Doe", "identity-1");
        string token = GenerateTestJwtToken(domainUser.Id.ToString(), jti);

        var storedToken = RefreshToken.Create("valid-refresh-token", jti, domainUser.Id);
        
        _refreshTokenRepository.GetByIdAsync("valid-refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);
        _userRepository.GetByIdAsync(domainUser.Id, Arg.Any<CancellationToken>())
            .Returns(domainUser);
        _userManager.FindByIdAsync("identity-1")
            .Returns(identityUser);

        SetupSuccessfulTokenGeneration(identityUser, domainUser);

        // Act
        Result<RefreshTokenResponse> result = await _sut.RefreshTokenAsync(
            token, "valid-refresh-token", CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Token, Is.Not.Empty);
            Assert.That(result.Value.RefreshToken, Is.Not.Empty);
        }
    }

    #endregion

    #region DeleteUserAsync

    [Test]
    public async Task DeleteUserAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        _userManager.FindByIdAsync("identity-1")
            .Returns((IdentityUser?)null);

        // Act
        Result result = await _sut.DeleteUserAsync("identity-1");

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task DeleteUserAsync_WhenDeleteFails_ShouldReturnFailure()
    {
        // Arrange
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };
        _userManager.FindByIdAsync("identity-1")
            .Returns(identityUser);
        _userManager.DeleteAsync(identityUser)
            .Returns(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Delete failed" }));

        // Act
        Result result = await _sut.DeleteUserAsync("identity-1");

        // Assert
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task DeleteUserAsync_WhenSucceeds_ShouldReturnSuccess()
    {
        // Arrange
        var identityUser = new IdentityUser { Id = "identity-1", Email = "user@example.com" };
        _userManager.FindByIdAsync("identity-1")
            .Returns(identityUser);
        _userManager.DeleteAsync(identityUser)
            .Returns(IdentityResult.Success);

        // Act
        Result result = await _sut.DeleteUserAsync("identity-1");

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        await _userManager.Received(1).DeleteAsync(identityUser);
    }

    #endregion

    #region Helpers

    private void SetupSuccessfulTokenGeneration(IdentityUser identityUser, User domainUser)
    {
        _userManager.FindByEmailAsync(identityUser.Email!)
            .Returns(identityUser);
        _userManager.GetRolesAsync(identityUser)
            .Returns(["Parent"]);
        _roleManager.FindByNameAsync("Parent")
            .Returns(new Role { Name = "Parent" });
        _roleManager.GetClaimsAsync(Arg.Any<Role>())
            .Returns([]);
        _userRepository.GetByIdentityIdAsync(identityUser.Id, Arg.Any<CancellationToken>())
            .Returns(domainUser);
    }

    private static string GenerateTestJwtToken(string userId, string jti)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecurityKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, "test@example.com"),
            new("userid", userId),
            new("role", "Parent"),
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

    #endregion
}
