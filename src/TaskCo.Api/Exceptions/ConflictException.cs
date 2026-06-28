namespace TaskCo.Api.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(string message) : base(409, "conflict", message) { }
}
