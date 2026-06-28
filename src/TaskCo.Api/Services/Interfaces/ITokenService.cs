using TaskCo.Api.Models.Entities;

namespace TaskCo.Api.Services.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
