namespace DeveloperTools.Mcp.Abstractions.Services;

public interface IWebSearchService
{
    Task<IReadOnlyList<string>> SearchAsync(string query, CancellationToken ct = default);
    Task<string> ScrapeAsync(string url, CancellationToken ct = default);
}
