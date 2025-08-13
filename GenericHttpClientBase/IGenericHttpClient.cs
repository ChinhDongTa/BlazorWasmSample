namespace GenericHttpClientBase;

public interface IGenericHttpClient
{
    Task<TResponse?> DeleteAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default);

    Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default);

    Task<TResponse?> PostAsJsonAsync<TResponse, TRequest>(string requestUri, TRequest data, CancellationToken cancellationToken = default);

    Task<TResponse?> PutAsJsonAsync<TResponse, TRequest>(string requestUri, TRequest data, CancellationToken cancellationToken = default);

    Task<TResponse?> PostFileAsync<TResponse>(string requestUri, MultipartFormDataContent content, CancellationToken cancellationToken = default);

    Task<Stream> PostAndDownloadAsync<TRequest>(string requestUri, TRequest data, CancellationToken cancellationToken = default);
}