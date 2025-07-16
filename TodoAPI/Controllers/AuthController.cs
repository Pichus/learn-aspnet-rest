using Microsoft.AspNetCore.Mvc;
using TodoAPI.Dtos;
using TodoAPI.Exceptions;
using TodoApi.Models;
using TodoAPI.Services;

namespace TodoAPI.Controllers;

// {
//     "accessToken": "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoic29tZXVzZXIiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjYiLCJleHAiOjE3NTI1MDM5MTcsImlzcyI6InBpY2h1c1RoZUlzc3VlciIsImF1ZCI6InBpY2h1c1RoZUF1ZGllbmNlIn0.aB46jFYC-BcguiSXYJUicgFl16-FExknpCDzK3Zvqgw1MN8dNyccPf_APSCQ-lvxcpjCI6dl6pbpyPJkQfbFyw",
//     "refreshToken": "tR6C1+Puq+uwJBz8rSJMNcemCbQCGtQlRGbkDl1+AeY="
// }

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register(UserDto userDto)
    {
        User user;

        try
        {
            user = await _authService.RegisterAsync(userDto);
        }
        catch (UsernameTakenException exception)
        {
            return BadRequest(exception.Message);
        }

        return Ok(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login(UserDto userDto)
    {
        TokenResponseDto response;

        try
        {
            response = await _authService.LoginAsync(userDto);
        }
        catch (WrongUsernameOrPassword exception)
        {
            return BadRequest("wrong username or password");
        }

        return Ok(response);
    }

    // todo
    // fix refresh token:
    // should not pass user id within the request, the refresh token is enough to identify a user
    // 
    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto refreshTokenRequestDto)
    {
        var result = await _authService.RefreshTokensAsync(refreshTokenRequestDto);

        if (result is null)
        {
            return Unauthorized("invalid refresh token");
        }

        return Ok(result);
    }
}