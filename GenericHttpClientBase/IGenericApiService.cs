namespace GenericHttpClientBase;

public interface IGenericApiService<T> where T : class
{
    Task<List<T>?> GetAllAsync(string requestUri);

    Task<T?> GetByIdAsync(string requestUri, int id);

    Task<T?> CreateAsync(string requestUri, T entity);

    Task<T?> UpdateAsync(string requestUri, int id, T entity);

    Task<T?> DeleteAsync(string requestUri, int id);
}