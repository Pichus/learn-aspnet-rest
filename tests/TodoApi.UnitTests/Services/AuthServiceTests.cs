using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TodoAPI.Dtos;
using TodoAPI.Exceptions;
using TodoApi.Models;
using TodoAPI.Services;

namespace TodoApi.Tests.Unit;

public class AuthServiceTests : IDisposable
{
    private readonly AuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly TodoContext _context;

    public AuthServiceTests()
    {
        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<TodoContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TodoContext(options);

        // Setup test configuration
        var configurationData = new Dictionary<string, string>
        {
            { "JwtSettings:Key", "this-is-a-very-long-secret-key-for-testing-purposes-that-is-at-least-64-characters" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        _authService = new AuthService(_configuration, _context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task RegisterAsync_WithValidUser_ReturnsUserDto()
    {
        var userDto = new UserDto
        {
            Username = "testuser",
            Password = "password123"
        };
        
        var result = await _authService.RegisterAsync(userDto);
        
        result.Should().NotBeNull();
        result.Username.Should().Be(userDto.Username);
        result.Id.Should().BeGreaterThan(0);
        
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == userDto.Username);
        savedUser.Should().NotBeNull();
        savedUser.Username.Should().Be(userDto.Username);
        
        savedUser.PasswordHash.Should().NotBe(userDto.Password);
        savedUser.PasswordHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_ThrowsUsernameTakenException()
    {
        var userDto = new UserDto { Username = "duplicate", Password = "password123" };
        await _authService.RegisterAsync(userDto);

        var duplicateUserDto = new UserDto { Username = "duplicate", Password = "differentpassword" };
        
        Func<Task> action = async () => await _authService.RegisterAsync(duplicateUserDto);
        await action.Should().ThrowAsync<UsernameTakenException>();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenResponse()
    {
        var userDto = new UserDto { Username = "logintest", Password = "password123" };
        await _authService.RegisterAsync(userDto);
        
        var result = await _authService.LoginAsync(userDto);
        
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        
        var handler = new JwtSecurityTokenHandler();
        var canRead = handler.CanReadToken(result.AccessToken);
        canRead.Should().BeTrue();

        var token = handler.ReadJwtToken(result.AccessToken);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == userDto.Username);
        
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == result.RefreshToken);
        refreshToken.Should().NotBeNull();
        refreshToken.ExpirationDate.Should().BeAfter(DateTime.UtcNow);
        refreshToken.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidUsername_ThrowsWrongUsernameOrPassword()
    {
        var userDto = new UserDto { Username = "nonexistent", Password = "password123" };

        Func<Task> action = async () => await _authService.LoginAsync(userDto);

        await action.Should().ThrowAsync<WrongUsernameOrPassword>();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsWrongUsernameOrPassword()
    {
        var userDto = new UserDto { Username = "passwordtest", Password = "password123" };
        await _authService.RegisterAsync(userDto);

        var loginDto = new UserDto { Username = "passwordtest", Password = "wrongpassword" };
        
        Func<Task> action = async () => await _authService.LoginAsync(loginDto);

        await action.Should().ThrowAsync<WrongUsernameOrPassword>();
    }

    [Fact]
    public async Task RefreshTokensAsync_WithValidToken_ReturnsNewTokenResponse()
    {
        var userDto = new UserDto { Username = "refreshtest", Password = "password123" };
        await _authService.RegisterAsync(userDto);

        var loginResult = await _authService.LoginAsync(userDto);
        var originalRefreshToken = loginResult.RefreshToken;
        
        var result = await _authService.RefreshTokensAsync(originalRefreshToken);
        
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(originalRefreshToken);
        
        var oldToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == originalRefreshToken);
        oldToken.Should().NotBeNull();
        oldToken.IsRevoked.Should().BeTrue();
        
        var newToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == result.RefreshToken);
        newToken.Should().NotBeNull();
        newToken.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokensAsync_WithInvalidToken_ReturnsNull()
    {
        var invalidToken = "invalid-token-string";
        
        var result = await _authService.RefreshTokensAsync(invalidToken);
        
        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokensAsync_WithRevokedToken_ReturnsNull()
    {
        var userDto = new UserDto { Username = "revokedtest", Password = "password123" };
        await _authService.RegisterAsync(userDto);

        var loginResult = await _authService.LoginAsync(userDto);
        
        await _authService.RefreshTokensAsync(loginResult.RefreshToken);
        
        var result = await _authService.RefreshTokensAsync(loginResult.RefreshToken);
        
        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokensAsync_WithExpiredToken_ReturnsNull()
    {
        var userDto = new UserDto { Username = "expiredtest", Password = "password123" };
        var registeredUser = await _authService.RegisterAsync(userDto);
        
        var expiredToken = new RefreshToken
        {
            Token = "expired-token",
            ExpirationDate = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            IsRevoked = false,
            UserId = registeredUser.Id
        };

        _context.RefreshTokens.Add(expiredToken);
        await _context.SaveChangesAsync();
        
        var result = await _authService.RefreshTokensAsync("expired-token");
        
        result.Should().BeNull();
    }
}