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

public class AuthService
// public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly TodoContext _context;

    public AuthService(IConfiguration configuration, TodoContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public async Task<User> RegisterAsync(UserDto userDto)
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

        return user;
    }

    // public async Task<TokenResponseDto> LoginAsync(UserDto userDto)
    // {
    //     var user = await _context.Users.FirstOrDefaultAsync(user => user.Username == userDto.Username);
    //
    //     if (user is null) throw new WrongUsernameOrPassword($"user with username \"{userDto.Username}\" not found");
    //
    //     if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, userDto.Password) ==
    //         PasswordVerificationResult.Failed)
    //         throw new WrongUsernameOrPassword("Wrong password");
    //
    //     return await CreateTokenResponse(user);
    // }

    // public async Task<TokenResponseDto?> RefreshTokensAsync(RefreshTokenRequestDto refreshTokenRequestDto)
    // {
    //     var user = await ValidateRefreshTokenAsync(refreshTokenRequestDto.UserId, refreshTokenRequestDto.RefreshToken);
    //
    //     if (user is null)
    //     {
    //         Console.WriteLine("user null wtf");
    //         return null;
    //     }
    //
    //     return await CreateTokenResponse(user);
    // }

    // private async Task<TokenResponseDto> CreateTokenResponse(User user)
    // {
    //     return new TokenResponseDto
    //     {
    //         AccessToken = CreateToken(user),
    //         RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
    //     };
    // }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
    {
        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();
        return refreshToken;
    }

    private async Task<User?> ValidateRefreshTokenAsync(int userId, string refreshToken)
    {
        var user = await _context.Users.FindAsync(userId);
        Console.WriteLine(userId);
    
        if (user is null)
        {
            Console.WriteLine("user not found");
            return null;
        }
    
        var tokenNotExpired = user.RefreshTokenExpiryTime > DateTime.UtcNow;
        Console.WriteLine($"token exprired {tokenNotExpired}");
        var tokenMatches = refreshToken == user.RefreshToken;
        Console.WriteLine($"token matches {tokenMatches}");
        var isRefreshTokenValid = tokenNotExpired && tokenMatches;
        
        if (!isRefreshTokenValid) return null;
    
        return user;
    }

    private string CreateToken(User user)
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

// {
// "accessToken": "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoidGVzdHVzZXIiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjkiLCJleHAiOjE3NTI1MDE1MzcsImlzcyI6InBpY2h1c1RoZUlzc3VlciIsImF1ZCI6InBpY2h1c1RoZUF1ZGllbmNlIn0.fkE2zuAjm7YNq9vpoABgdsNKHyr09AzFulTuM0q15zKCo3lmRYC_1pP5LzeaNF_1ZIENQq0crgO8F2rlrCeM3g",
// "refreshToken": "RLpkter7zapc49keVNRdpGxjShLXs88G/uw6W8TDiCE="
// }