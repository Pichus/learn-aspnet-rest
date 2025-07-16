namespace TodoAPI.Dtos;

public class RefreshTokenRequestDto
{
    public int UserId { get; set; }
    public required string RefreshToken { get; set; }
}