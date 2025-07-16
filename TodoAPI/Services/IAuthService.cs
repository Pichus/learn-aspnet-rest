using TodoAPI.Dtos;
using TodoApi.Models;

namespace TodoAPI.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(UserDto userDto);
    Task<TokenResponseDto> LoginAsync(UserDto userDto);
    Task<TokenResponseDto?> RefreshTokensAsync(RefreshTokenRequestDto refreshTokenRequestDto);
}