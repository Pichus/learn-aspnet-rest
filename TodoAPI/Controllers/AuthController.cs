using Microsoft.AspNetCore.Mvc;
using TodoAPI.Dtos;
using TodoAPI.Exceptions;
using TodoApi.Models;
using TodoAPI.Services;

namespace TodoAPI.Controllers;


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
    public async Task<ActionResult<GetUserAfterRegistrationDto>> Register(UserDto userDto)
    {
        GetUserAfterRegistrationDto user;

        try
        {
            user = await _authService.RegisterAsync(userDto);
        }
        catch (UsernameTakenException exception)
        {
            return BadRequest(exception.Message);
        }

        return CreatedAtAction(nameof(Register), user);
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
    
    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto refreshTokenRequestDto)
    {
        var result = await _authService.RefreshTokensAsync(refreshTokenRequestDto.RefreshToken);

        if (result is null)
        {
            return Unauthorized("invalid refresh token");
        }

        return Ok(result);
    }
}