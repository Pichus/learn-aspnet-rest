using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TodoAPI.Dtos;
using TodoAPI.Exceptions;
using TodoApi.Models;

namespace TodoAPI.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly TodoContext _context;

    public AuthService(IConfiguration configuration, TodoContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public async Task<GetUserAfterRegistrationDto> RegisterAsync(UserDto userDto)
    {
        if (await _context.Users.AnyAsync(user => user.Username == userDto.Username))
            throw new UsernameTakenException($"username \"{userDto.Username}\" is taken");

        var user = new User();

        var hashedPassword = new PasswordHasher<User>()
            .HashPassword(user, userDto.Password);

        user.PasswordHash = hashedPassword;
        user.Username = userDto.Username;

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        var response = new GetUserAfterRegistrationDto
        {
            Id = user.Id,
            Username = user.Username
        };

        return response;
    }

    public async Task<TokenResponseDto> LoginAsync(UserDto userDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Username == userDto.Username);

        if (user is null) throw new WrongUsernameOrPassword($"user with username \"{userDto.Username}\" not found");

        if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, userDto.Password) ==
            PasswordVerificationResult.Failed)
            throw new WrongUsernameOrPassword("Wrong password");

        return await CreateTokenResponse(user);
    }

    public async Task<TokenResponseDto?> RefreshTokensAsync(string refreshTokenString)
    {
        var refreshToken = await GetRefreshTokenByTokenStringAsync(refreshTokenString);

        if (refreshToken is null)
        {
            return null;
        }

        var refreshTokenValid = ValidateRefreshToken(refreshToken);

        if (!refreshTokenValid)
        {
            return null;
        }

        await RevokeRefreshTokenAsync(refreshToken);

        return await CreateTokenResponse(refreshToken.User);
    }

    private async Task RevokeRefreshTokenAsync(RefreshToken refreshToken)
    {
        refreshToken.IsRevoked = true;
        await _context.SaveChangesAsync();
    }

    private async Task<RefreshToken?> GetRefreshTokenByTokenStringAsync(string refreshTokenString)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.Token == refreshTokenString);

        return refreshToken;
    }

    private async Task<TokenResponseDto> CreateTokenResponse(User user)
    {
        return new TokenResponseDto
        {
            AccessToken = CreateAccessToken(user),
            RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
        };
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
    {
        var tokenString = GenerateRefreshToken();
        var expiration = DateTime.UtcNow.AddDays(7);

        var refreshToken = new RefreshToken
        {
            Token = tokenString,
            ExpirationDate = expiration,
            IsRevoked = false,
            UserId = user.Id,
        };
        
        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync();

        return refreshToken.Token;
    }

    private bool ValidateRefreshToken(RefreshToken refreshToken)
    {
        var tokenExpired = refreshToken.ExpirationDate > DateTime.UtcNow;

        return tokenExpired;
    }

    private string CreateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var tokenDescriptor = new JwtSecurityToken(
            _configuration["JwtSettings:Issuer"],
            _configuration["JwtSettings:Audience"],
            claims,
            expires: DateTime.UtcNow.AddSeconds(30), // for testing purposes
            // expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}