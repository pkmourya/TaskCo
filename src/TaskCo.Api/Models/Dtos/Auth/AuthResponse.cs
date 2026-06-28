namespace TaskCo.Api.Models.Dtos.Auth;

public class AuthResponse
{
    public string Token { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; }

    public AuthResponse(string token, int userId, string email)
    {
        Token = token;
        UserId = userId;
        Email = email;
    }
}
