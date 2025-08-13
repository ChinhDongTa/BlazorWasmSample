namespace GenericHttpClientBase;

// Exception tùy chỉnh để chứa thông tin lỗi chi tiết
public class ApiException(string message, System.Net.HttpStatusCode statusCode, ApiErrorResponse? apiError = null) : Exception(message)
{
    public System.Net.HttpStatusCode StatusCode { get; } = statusCode;
    public ApiErrorResponse? ApiError { get; } = apiError;
}