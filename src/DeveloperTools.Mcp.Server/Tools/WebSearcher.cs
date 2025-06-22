using DeveloperTools.Mcp.Abstractions.Models;
using DeveloperTools.Mcp.Abstractions.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DeveloperTools.Mcp.Server.Tools;

[McpServerToolType]
public class WebTools
{
    [McpServerTool(Name = "search-web")]
    [Description("Search the web and return up to 5 URLs most relevant to the query. Should be chained with `Extract Textual Content` to get the text content of the pages.")]
    public static Task<IReadOnlyList<string>> SearchWebAsync(
        [Description("A query to search the web for.")] string query,
        IWebSearchService search,
        CancellationToken ct = default)
    {
        return search.SearchAsync(query, ct);
    }

    [McpServerTool(Name = "parse-url")]
    [Description("Scrapes the textual content from each URL and return a list of strings containing the text content.")]
    public static async Task<IReadOnlyList<string>> ExtractRelevantContentAsync(
        [Description("Comma-separated list of URLs to scrape.")] string urls,
        IWebSearchService search,
        CancellationToken ct = default)
    {
        var tasks = urls.Split(',')
                       .Select(u => search.ScrapeAsync(urls, ct))
                       .ToList();

        return (await Task.WhenAll(tasks))
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToList();
    }

    [McpServerTool(Name = "manage-bookmarks")]
    [Description("Manage bookmarks for URLs. Use `add`, `remove`, or `list` as the action.")]
    public static async Task<string> ManageBookmarksAsync(
        ILocalFileRepository<Bookmark> bookmarkRepository,
        [Description("Action to perform: add, remove, or list.")] string action,
        [Description("URL to add or remove, if applicable.")] string url = "",
        [Description("A description for the bookmark, if adding.")] string description = "",
        CancellationToken ct = default)
    {
        return action.ToLowerInvariant() switch
        {
            "add" => await bookmarkRepository.AddAsync(new Bookmark(url, description))
                .ContinueWith(_ => $"Bookmark added: {url}", ct),
            "remove" => await bookmarkRepository.DeleteAsync(b => b.Url == url)
                .ContinueWith(_ => $"Bookmark removed: {url}", ct),
            "list" => string.Join("\n", (await bookmarkRepository.ListAsync())
                .Select(b => $"{b.Url}: {b.Description}")),
            _ => "Invalid action. Use 'add', 'remove', or 'list'."
        };
    }
}