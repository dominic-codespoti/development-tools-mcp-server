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
        [Description("What to search for.")] string query,
        IWebSearchService search,
        CancellationToken ct = default)
        => search.SearchAsync(query, ct);

    [McpServerTool(Name = "scrape-url")]
    [Description("Extract readable text from a single web page.")]
    public static Task<string> ScrapeUrlAsync(
        [Description("Absolute URL to scrape.")] string url,
        IWebSearchService search,
        CancellationToken ct = default)
        => search.ScrapeAsync(url, ct);

    [McpServerTool(Name = "extract-textual-content")]
    [Description("Given URLs, scrape each and return cleaned text.")]
    public static async Task<IReadOnlyList<string>> ExtractRelevantContentAsync(
        List<string> urls,
        IWebSearchService search,
        CancellationToken ct = default)
    {
        var tasks = urls.Distinct()
                        .Where(x => Uri.IsWellFormedUriString(x, UriKind.Absolute))
                        .Select(u => search.ScrapeAsync(u, ct).ContinueWith(t => t.IsFaulted ? $"[error] {u}" : t.Result, ct));

        return await Task.WhenAll(tasks);
    }
}