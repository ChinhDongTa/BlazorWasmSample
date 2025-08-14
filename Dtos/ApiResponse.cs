namespace Dtos;

public sealed record ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public OperationKind Operation { get; init; } = OperationKind.Retrieved;
    public T? Data { get; init; }
    public PaginationInfo? Pagination { get; init; }
    public Uri? Location { get; init; }
    public int? AffectedCount { get; init; }
    public string? ErrorCode { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    // ----- Factory methods -----

    public static ApiResult<T> Success(
        T? data = default,
        string message = "OK",
        OperationKind operation = OperationKind.Retrieved,
        PaginationInfo? pagination = null,
        int? affected = null
    ) => new()
    {
        IsSuccess = true,
        Message = message,
        Operation = operation,
        Data = data,
        Pagination = pagination,
        AffectedCount = affected
    };

    public static ApiResult<T> Created(
        T data,
        string message = "Created",
        Uri? location = null
    ) => new()
    {
        IsSuccess = true,
        Message = message,
        Operation = OperationKind.Created,
        Data = data,
        Location = location,
        AffectedCount = 1
    };

    public static ApiResult<T> Updated(
        T data,
        string message = "Updated"
    ) => new()
    {
        IsSuccess = true,
        Message = message,
        Operation = OperationKind.Updated,
        Data = data,
        AffectedCount = 1
    };

    public static ApiResult<T> Deleted(
        string message = "Deleted",
        int affected = 1
    ) => new()
    {
        IsSuccess = true,
        Message = message,
        Operation = OperationKind.Deleted,
        Data = default,
        AffectedCount = affected
    };

    public static ApiResult<T> Failure(
        string message,
        string? errorCode = null,
        IDictionary<string, string[]>? errors = null
    ) => new()
    {
        IsSuccess = false,
        Message = message,
        Operation = OperationKind.Failed,
        ErrorCode = errorCode,
        Errors = errors
    };
}