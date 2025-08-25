using Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ApiServer.Helpers;

public static class ApiResultExtensions
{
    // Thành công
    public static IResult ToOkResult<T>(
        this T? data,
        string message = "Thành công",
        PaginationInfo? pagination = null,
        int? affected = null,
        OperationKind operation = OperationKind.Retrieved
    ) => Results.Json(ApiResult<T>.Success(data, message, operation, pagination, affected));

    // Lỗi từ ProblemDetails
    public static IResult ToProblemResult(
        this ProblemDetails pd,
        string? errorCode = null,
        IDictionary<string, string[]>? errors = null
    )
    {
        var problem = ApiResultProblem.FromProblemDetails(pd, errorCode, errors);
        var wrapper = ApiResult<object>.Failure(
            problem.Detail,
            problem.ErrorCode ?? problem.Status.ToString(),
            errors
        );
        return Results.Json(wrapper, statusCode: problem.Status);
    }
}