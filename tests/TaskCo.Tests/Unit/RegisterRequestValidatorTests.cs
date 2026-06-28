using TaskCo.Api.Models.Dtos.Auth;
using TaskCo.Api.Validators.Auth;

namespace TaskCo.Tests.Unit;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public async Task ValidRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Password123"
        });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-at")]
    public async Task InvalidEmail_FailsValidation(string email)
    {
        var result = await _validator.ValidateAsync(new RegisterRequest
        {
            Email = email,
            Password = "Password123"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("1234567")] // exactly 7 chars — one below minimum
    public async Task ShortOrEmptyPassword_FailsValidation(string password)
    {
        var result = await _validator.ValidateAsync(new RegisterRequest
        {
            Email = "user@example.com",
            Password = password
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Password_ExactlyEightChars_Passes()
    {
        var result = await _validator.ValidateAsync(new RegisterRequest
        {
            Email = "user@example.com",
            Password = "12345678"
        });

        Assert.True(result.IsValid);
    }
}
