namespace ApiServer.Helpers;

public class DomainException(string message,
                             string code = "DOMAIN_ERROR",
                             int status = StatusCodes.Status400BadRequest,
                             object? errors = null) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
    public object? Errors { get; } = errors;
}