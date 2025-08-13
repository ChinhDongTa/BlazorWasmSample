namespace GenericHttpClientBase;

// Một model đơn giản để hứng lỗi từ API
public class ApiErrorResponse
{
    public string? Message { get; set; }
    public IEnumerable<string>? Errors { get; set; }
}