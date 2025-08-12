namespace GenericHttpClientBase;

using System.Net.Http.Json;

public class GenericApiService<T>(IHttpClientFactory httpClientFactory) : IGenericApiService<T> where T : class
{
    private readonly HttpClient _apiClient = httpClientFactory.CreateClient("Api");

    public async Task<T?> CreateAsync(string requestUri, T entity)
    {
        var response = await _apiClient.PostAsJsonAsync(requestUri, entity);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<T?> DeleteAsync(string requestUri, int id)
    {
        var response = await _apiClient.DeleteAsync($"{requestUri}/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<List<T>?> GetAllAsync(string requestUri)
    {
        return await _apiClient.GetFromJsonAsync<List<T>>(requestUri);
    }

    public async Task<T?> GetByIdAsync(string requestUri, int id)
    {
        return await _apiClient.GetFromJsonAsync<T>($"{requestUri}/{id}");
    }

    public async Task<T?> UpdateAsync(string requestUri, int id, T entity)
    {
        var response = await _apiClient.PutAsJsonAsync($"{requestUri}/{id}", entity);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }
}