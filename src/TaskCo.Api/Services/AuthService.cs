using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskCo.Api.Data;
using TaskCo.Api.Exceptions;
using TaskCo.Api.Models.Dtos.Auth;
using TaskCo.Api.Models.Entities;
using TaskCo.Api.Services.Interfaces;

namespace TaskCo.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;

    // Computed once at class init. A real bcrypt hash ensures VerifyHashedPassword always
    // runs the full work factor when the email doesn't exist, keeping response time constant
    // and preventing timing-based user enumeration.
    private static readonly string _dummyHash =
        new PasswordHasher<User>().HashPassword(new User(), Guid.NewGuid().ToString());

    public AuthService(AppDbContext db, PasswordHasher<User> passwordHasher, ITokenService tokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw new ConflictException("Email is already registered");

        var user = new User
        {
            Email = email,
            CreatedAt = DateTime.UtcNow,
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResponse(_tokenService.GenerateToken(user), user.Id, user.Email);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);

        // Always run VerifyHashedPassword — never short-circuit on a missing user.
        // Using _dummyHash when the user doesn't exist keeps bcrypt work constant,
        // preventing a timing attack that would otherwise reveal whether an email is registered.
        var hashToVerify = user?.PasswordHash ?? _dummyHash;
        var verified = _passwordHasher.VerifyHashedPassword(
            user ?? new User(), hashToVerify, request.Password);

        if (user is null || verified == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Invalid credentials");

        return new AuthResponse(_tokenService.GenerateToken(user), user.Id, user.Email);
    }
}
