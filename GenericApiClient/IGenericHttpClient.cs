
namespace GenericApiClient
{
    public interface IGenericHttpClient
    {
        Task DeleteAsync(string url, CancellationToken ct = default);
        Task<Stream> DownloadFileAsync(string url, CancellationToken ct = default);
        Task DownloadFileToDiskAsync(string url, string filePath, CancellationToken ct = default);
        Task<TResponse?> GetAsync<TResponse>(string url, CancellationToken ct = default);
        Task<TResponse?> PatchAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default);
        Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default);
        Task<Stream> PostForFileAsync<TRequest>(string url, TRequest data, CancellationToken ct = default);
        Task PostForFileToDiskAsync<TRequest>(string url, TRequest data, string filePath, CancellationToken ct = default);
        Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default);
        Task<TResponse?> UploadFileAsync<TResponse>(string url, Stream fileStream, string fileName, string contentType = "application/octet-stream", CancellationToken ct = default);
    }
}