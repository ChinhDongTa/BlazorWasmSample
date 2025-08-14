using Microsoft.AspNetCore.Mvc;

namespace ApiServer.Helpers;

public record ApiResultProblem(
    string Type,
    string Title,
    int Status,
    string Detail,
    string? Instance = null,
    string? ErrorCode = null,
    object? Errors = null
)
{
    public static ApiResultProblem FromProblemDetails(
        ProblemDetails pd,
        string? errorCode = null,
        object? errors = null
    )
        => new(
            pd.Type ?? "about:blank",
            pd.Title ?? "Error",
            pd.Status ?? StatusCodes.Status500InternalServerError,
            pd.Detail ?? "",
            pd.Instance,
            errorCode,
            errors
        );
}