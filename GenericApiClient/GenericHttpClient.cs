using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace GenericApiClient;

public class GenericHttpClient : IGenericHttpClient
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _options;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly ILogger<GenericHttpClient> _logger;

    public GenericHttpClient(HttpClient client, ILogger<GenericHttpClient> logger)
    {
        _client = client;
        _logger = logger;
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (result, time, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} for {Url} due to {StatusCode}", retryCount, context["url"], result.Result?.StatusCode);
                });
    }

    private void AddHeaders(HttpRequestMessage request, Dictionary<string, string>? headers)
    {
        if (headers == null) return;
        foreach (var kv in headers)
        {
            request.Headers.Remove(kv.Key);
            request.Headers.Add(kv.Key, kv.Value);
        }
    }

    private async Task HandleError(HttpResponseMessage response, CancellationToken ct)
    {
        var content = await response.Content.ReadAsStringAsync(ct);
        ApiProblemDetails? problem;
        try
        {
            problem = JsonSerializer.Deserialize<ApiProblemDetails>(content, _options);
        }
        catch
        {
            problem = new ApiProblemDetails { Status = (int)response.StatusCode, Title = "Unexpected error", Detail = content };
        }
        _logger.LogError("API error {Status}: {Detail}", response.StatusCode, problem?.Detail ?? content);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException(problem?.Detail ?? "Unauthorized");
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new AccessViolationException(problem?.Detail ?? "Forbidden");
        if ((int)response.StatusCode == 422)
            throw new ApiException(problem!) { Data = { ["ValidationErrors"] = problem?.Extensions } };

        throw new ApiException(problem!);
    }

    public async Task<TResponse?> GetAsync<TResponse>(string url, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        _logger.LogInformation("GET {Url}", url);
        using var response = await _retryPolicy.ExecuteAsync(ctx => _client.SendAsync(request, ct), new Context { ["url"] = url });
        if (!response.IsSuccessStatusCode)
            await HandleError(response, ct);
        return await response.Content.ReadFromJsonAsync<TResponse>(_options, ct);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(data, options: _options)
        };
        _logger.LogInformation("POST {Url}", url);
        using var response = await _retryPolicy.ExecuteAsync(ctx => _client.SendAsync(request, ct), new Context { ["url"] = url });
        if (!response.IsSuccessStatusCode)
            await HandleError(response, ct);
        return await response.Content.ReadFromJsonAsync<TResponse>(_options, ct);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(data, options: _options)
        };
        _logger.LogInformation("PUT {Url}", url);
        using var response = await _retryPolicy.ExecuteAsync(ctx => _client.SendAsync(request, ct), new Context { ["url"] = url });
        if (!response.IsSuccessStatusCode)
            await HandleError(response, ct);
        return await response.Content.ReadFromJsonAsync<TResponse>(_options, ct);
    }

    public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(data, _options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
        _logger.LogInformation("PATCH {Url}", url);
        using var response = await _retryPolicy.ExecuteAsync(
            ctx => _client.SendAsync(request, ct), new Context { ["url"] = url }
            );
        if (!response.IsSuccessStatusCode)
            await HandleError(response, ct);
        return await response.Content.ReadFromJsonAsync<TResponse>(_options, ct);
    }

    public async Task<TResponse?> DeleteAsync<TResponse>(string url, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        _logger.LogInformation("DELETE {Url}", url);
        using var response = await _retryPolicy.ExecuteAsync(
            ctx => _client.SendAsync(request, ct), new Context { ["url"] = url }
            );
        if (!response.IsSuccessStatusCode)
            await HandleError(response, ct);
        return await response.Content.ReadFromJsonAsync<TResponse>(_options, ct);
    }

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        _logger.LogInformation("DELETE {Url}", url);
        using var response = await _retryPolicy.ExecuteAsync(ctx => _client.SendAsync(request, ct), new Context { ["url"] = url });
        if (!response.IsSuccessStatusCode)
            await HandleError(response, ct);
    }

    public async Task<TResponse?> UploadFileAsync<TResponse>(string url, Stream fileStream, string fileName, string contentType = "application/octet-stream", CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
        _logger.LogInformation("UPLOAD FILE {Url}", url);
        using var response = await _retryPolicy.ExecuteAsync(ctx => _client.SendAsync(request, ct), new Context { ["url"] = url });
        if (!response.IsSuccessStatusCode)
            await HandleError(response, ct);
        return await response.Content.ReadFromJsonAsync<TResponse>(_options, ct);
    }

    public async Task<Stream> DownloadFileAsync(string url, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        _logger.LogInformation("DOWNLOAD FILE {Url}", url);
        using var response = await _retryPolicy.ExecuteAsync(
            ctx => _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct), new Context { ["url"] = url }
            );
        if (!response.IsSuccessStatusCode)
            await HandleError(response, ct);
        return await response.Content.ReadAsStreamAsync(ct);
    }

    public async Task DownloadFileToDiskAsync(string url, string filePath, CancellationToken ct = default)
    {
        using var stream = await DownloadFileAsync(url, ct);
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream, ct);
    }

    public async Task<Stream> PostForFileAsync<TRequest>(string url, TRequest data, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(data, _options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        _logger.LogInformation("POST FOR FILE {Url}", url);
        using var response = await _retryPolicy.ExecuteAsync(ctx => _client.SendAsync(request, ct), new Context { ["url"] = url });
        if (!response.IsSuccessStatusCode)
            await HandleError(response, ct);
        return await response.Content.ReadAsStreamAsync(ct);
    }

    public async Task PostForFileToDiskAsync<TRequest>(string url, TRequest data, string filePath, CancellationToken ct = default)
    {
        using var stream = await PostForFileAsync(url, data, ct);
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream, ct);
    }
}