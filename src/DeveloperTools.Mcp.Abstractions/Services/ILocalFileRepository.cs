namespace DeveloperTools.Mcp.Abstractions.Services;

public interface ILocalFileRepository<T>
{
    Task<List<T>> ListAsync();
    Task AddAsync(T item);
    Task DeleteAsync(Func<T, bool> predicate);
    Task UpdateAsync(Func<T, bool> predicate, T newItem);
}
