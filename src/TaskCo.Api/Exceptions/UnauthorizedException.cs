namespace TaskCo.Api.Exceptions;

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(401, "unauthorized", message) { }
}
