namespace ApiServer.Helpers;

public class DomainException : Exception
{
    public string Code { get; }
    public int Status { get; }
    public object? Errors { get; }

    public DomainException(
        string message,
        string code = "DOMAIN_ERROR",
        int status = StatusCodes.Status400BadRequest,
        object? errors = null
    ) : base(message)
    {
        Code = code;
        Status = status;
        Errors = errors;
    }
}