using DeveloperTools.Mcp.Abstractions.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DeveloperTools.Mcp.Server.Tools;

[McpServerToolType]
public class WebTools
{
    [McpServerTool(Name = "search-web")]
    [Description("Searches the web and returns up-to 5 URLs most relevant to the query. Should be chained with `read-webpage` to get the text content of the pages.")]
    public static Task<IReadOnlyList<string>> SearchWebAsync(
        [Description("A query to search the web for.")] string query,
        IWebSearchService search,
        CancellationToken ct = default)
    {
        return search.SearchAsync(query, ct);
    }

    [McpServerTool(Name = "read-webpage")]
    [Description("Scrapes the textual content from each URL and returns a list of strings containing the textual content.")]
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
}
