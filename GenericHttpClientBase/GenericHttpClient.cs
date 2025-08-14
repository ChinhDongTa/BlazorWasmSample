using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenericHttpClientBase;

/// <summary>
/// Các phương thức HTTP cơ bản để tương tác với API.
/// Hướng dẫn sử dụng:
/// Đăng ký IGenericHttpClient với một HttpClient đã được cấu hình
/// builder.Services.AddHttpClient<IGenericHttpClient, GenericHttpClient>(client =>
/// {
///     client.BaseAddress = new Uri("https://api.example.com/");
///     client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
///    // Thêm các header mặc định khác ở đây nếu cần
/// }).TokenHandler();
/// </summary>
public class GenericHttpClient : IGenericHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    // Tùy chọn: Thêm ILogger để ghi log
    // private readonly ILogger<GenericHttpClient> _logger;

    // Pattern tốt hơn là inject trực tiếp HttpClient đã được cấu hình
    // thay vì IHttpClientFactory.
    public GenericHttpClient(HttpClient httpClient /*, ILogger<GenericHttpClient> logger */)
    {
        _httpClient = httpClient;
        // _logger = logger;

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        // Header mặc định nên được cấu hình khi đăng ký HttpClient trong Program.cs
        // nhưng vẫn có thể để ở đây nếu muốn.
        // _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default)
        => SendRequestAndProcessResponse<TResponse>(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken);

    public Task<TResponse?> DeleteAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default)
        => SendRequestAndProcessResponse<TResponse>(new HttpRequestMessage(HttpMethod.Delete, requestUri), cancellationToken);

    public Task<TResponse?> PostAsJsonAsync<TResponse, TRequest>(string requestUri, TRequest data, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(data, options: _jsonSerializerOptions)
        };
        return SendRequestAndProcessResponse<TResponse>(request, cancellationToken);
    }

    public Task<TResponse?> PutAsJsonAsync<TResponse, TRequest>(string requestUri, TRequest data, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
        {
            Content = JsonContent.Create(data, options: _jsonSerializerOptions)
        };
        return SendRequestAndProcessResponse<TResponse>(request, cancellationToken);
    }

    public Task<TResponse?> PostFileAsync<TResponse>(string requestUri, MultipartFormDataContent content, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = content
        };
        return SendRequestAndProcessResponse<TResponse>(request, cancellationToken);
    }

    public async Task<Stream> PostAndDownloadAsync<TRequest>(string requestUri, TRequest data, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(data, options: _jsonSerializerOptions)
        };

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponse(response, cancellationToken);
            // HandleErrorResponse sẽ ném exception, dòng này thực chất sẽ không được chạy
            return Stream.Null;
        }

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    /// <summary>
    /// Phương thức helper trung tâm để gửi request và xử lý response.
    /// Tuân thủ nguyên tắc DRY.
    /// </summary>
    private async Task<TResponse?> SendRequestAndProcessResponse<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Xử lý lỗi một cách chi tiết
            await HandleErrorResponse(response, cancellationToken);
        }

        // Nếu thành công, deserialize nội dung
        try
        {
            return await response.Content.ReadFromJsonAsync<TResponse>(_jsonSerializerOptions, cancellationToken);
        }
        catch (JsonException ex) // Bắt lỗi nếu nội dung response không phải là JSON hợp lệ
        {
            //Console.WriteLine($"Failed to deserialize JSON response: {ex.Message}");
            // _logger.LogError(ex, "Failed to deserialize JSON response from {RequestUri}", request.RequestUri);
            throw new ApiException(
                    $"Failed to deserialize JSON response. See inner exception for details. Uri: {request.RequestUri}",
                    System.Net.HttpStatusCode.InternalServerError,
                    new ApiErrorResponse()
                    {
                        Message = ex.Message,
                        Errors = ex.Data.Values.Cast<object>().Select(key => key.ToString() ?? "")
                    }
                );
        }
    }

    /// <summary>
    /// Phương thức helper để xử lý các response không thành công.
    /// </summary>
    private async Task HandleErrorResponse(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ApiErrorResponse? error = null;
        string errorMessage = $"Request failed with status code {response.StatusCode}.";

        try
        {
            // Cố gắng đọc nội dung lỗi từ API
            error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(_jsonSerializerOptions, cancellationToken);
            if (error != null)
            {
                errorMessage = error.Message ?? errorMessage;
            }
        }
        catch (JsonException ex)
        {
            // Nếu nội dung lỗi không phải là JSON, chỉ cần đọc nó dưới dạng chuỗi
            var rawError = await response.Content.ReadAsStringAsync(cancellationToken);
            // _logger.LogWarning("Could not deserialize error response as JSON. Raw response: {RawError}", rawError);
            errorMessage = rawError;
            error = new ApiErrorResponse() { Message = rawError, Errors = ex.Data.Values.Cast<object>().Select(key => key.ToString() ?? "") };
        }

        throw new ApiException(errorMessage, response.StatusCode, error);
    }
}