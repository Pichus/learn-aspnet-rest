using TodoAPI.Dtos;

namespace TodoAPI.Services;

public interface IAuthService
{
    Task<GetUserAfterRegistrationDto> RegisterAsync(UserDto userDto);
    Task<TokenResponseDto> LoginAsync(UserDto userDto);
    Task<TokenResponseDto?> RefreshTokensAsync(string refreshTokenString);
}