using System.Text.Json;
using DeveloperTools.Mcp.Abstractions.Services;

namespace DeveloperTools.Mcp.Server.Services;

public class LocalFileRepository<T> : ILocalFileRepository<T>
{
    private readonly string _filePath;

    public LocalFileRepository()
    {
        var configDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var repoDir = Path.Combine(configDir, "dotnet-mcp-server");
        Directory.CreateDirectory(repoDir);
        var typeName = typeof(T).Name.ToLowerInvariant();
        _filePath = Path.Combine(repoDir, $"{typeName}.json");
        if (!File.Exists(_filePath))
            File.WriteAllText(_filePath, "[]");
    }

    public async Task<List<T>> ListAsync()
    {
        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<T>>(json) ?? [];
    }

    public async Task AddAsync(T item)
    {
        var items = await ListAsync();
        items.Add(item);
        await SaveAsync(items);
    }

    public async Task DeleteAsync(Func<T, bool> predicate)
    {
        var items = await ListAsync();
        items = [.. items.Where(x => !predicate(x))];
        await SaveAsync(items);
    }

    public async Task UpdateAsync(Func<T, bool> predicate, T newItem)
    {
        var items = await ListAsync();
        var idx = items.FindIndex(x => predicate(x));
        if (idx >= 0)
        {
            items[idx] = newItem;
            await SaveAsync(items);
        }
    }

    private async Task SaveAsync(List<T> items)
    {
        var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }
}
