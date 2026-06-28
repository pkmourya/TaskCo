using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskCo.Api.Data;
using TaskCo.Api.Exceptions;
using TaskCo.Api.Models.Dtos.Auth;
using TaskCo.Api.Models.Entities;
using TaskCo.Api.Services;
using TaskCo.Api.Services.Interfaces;

namespace TaskCo.Tests.Unit;

public class AuthServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AuthService CreateService(AppDbContext db) =>
        new(db, new PasswordHasher<User>(), new FakeTokenService());

    [Fact]
    public async Task RegisterAsync_NewEmail_ReturnsToken()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Password123"
        });

        Assert.Equal("fake-token", result.Token);
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsConflict()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var request = new RegisterRequest { Email = "user@example.com", Password = "Password123" };

        await service.RegisterAsync(request);

        await Assert.ThrowsAsync<ConflictException>(() => service.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_EmailStoredLowercase()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await service.RegisterAsync(new RegisterRequest { Email = "USER@EXAMPLE.COM", Password = "Password123" });

        var user = await db.Users.SingleAsync();
        Assert.Equal("user@example.com", user.Email);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        await service.RegisterAsync(new RegisterRequest { Email = "user@example.com", Password = "Password123" });

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "Password123"
        });

        Assert.Equal("fake-token", result.Token);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        await service.RegisterAsync(new RegisterRequest { Email = "user@example.com", Password = "Password123" });

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.LoginAsync(new LoginRequest { Email = "user@example.com", Password = "WrongPassword" }));
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorized()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = "Password123" }));
    }

    [Fact]
    public async Task LoginAsync_EmailIsCaseInsensitive()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        await service.RegisterAsync(new RegisterRequest { Email = "user@example.com", Password = "Password123" });

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "USER@EXAMPLE.COM",
            Password = "Password123"
        });

        Assert.Equal("fake-token", result.Token);
    }

    private class FakeTokenService : ITokenService
    {
        public string GenerateToken(User user) => "fake-token";
    }
}
