using Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ApiServer.Helpers;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        if (ctx.Response.HasStarted)
        {
            _logger.LogWarning(ex, "Response đã bắt đầu trước khi bắt exception.");
            return; // Không throw; vì không trong catch trực tiếp
        }

        var map = MapException(ex, ctx);

        var pd = new ProblemDetails
        {
            Title = map.Title,
            Detail = map.Detail,
            Status = map.Status,
            Type = map.Type,
            Instance = ctx.Request.Path
        };

        var problem = ApiResultProblem.FromProblemDetails(pd, map.ErrorCode, map.Errors);

        var wrapper = ApiResult<object>.Failure(
            problem.Detail,
            problem.ErrorCode ?? problem.Status.ToString(),
            map.Errors
        );

        ctx.Response.Clear();
        ctx.Response.StatusCode = problem.Status;
        ctx.Response.ContentType = "application/json; charset=utf-8";

        if (problem.Status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
        else
            _logger.LogWarning(ex, "Handled exception ({Status}): {Message}", problem.Status, ex.Message);

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(wrapper, JsonOpts));
    }

    private static (int Status, string Title, string ErrorCode, string Detail, string Type, IDictionary<string, string[]>? Errors)
        MapException(Exception ex, HttpContext ctx)
    {
        return ex switch
        {
            DomainException de => (de.Status, "Lỗi nghiệp vụ", de.Code, de.Message,
                                $"https://httpstatuses.io/{de.Status}", (IDictionary<string, string[]>?)de.Errors),
            KeyNotFoundException knf => (404, "Không tìm thấy", "NOT_FOUND",
                                string.IsNullOrWhiteSpace(knf.Message) ? "Tài nguyên không tồn tại" : knf.Message,
                                "https://httpstatuses.io/404", null),
            UnauthorizedAccessException ua => (403, "Bị từ chối truy cập", "FORBIDDEN",
                                string.IsNullOrWhiteSpace(ua.Message) ? "Bạn không có quyền truy cập" : ua.Message,
                                "https://httpstatuses.io/403", null),
            BadHttpRequestException badReq => (badReq.StatusCode, "Yêu cầu không hợp lệ", "BAD_REQUEST",
                                badReq.Message, $"https://httpstatuses.io/{badReq.StatusCode}", null),
            _ => ((int Status, string Title, string ErrorCode, string Detail, string Type, IDictionary<string, string[]>? Errors))(500, "Lỗi máy chủ", "INTERNAL_SERVER_ERROR",
                                "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.",
                                "https://httpstatuses.io/500",
                                new Dictionary<string, string[]> { ["TraceId"] = [ctx.TraceIdentifier] }),
        };
    }
}