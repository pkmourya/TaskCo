namespace TaskCo.Api.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(404, "not_found", message) { }
}
