namespace Dtos;

public sealed record PaginationInfo(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);
