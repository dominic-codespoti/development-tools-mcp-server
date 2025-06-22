using System.Text.RegularExpressions;
using System.Web;
using AngleSharp.Html.Parser;
using DeveloperTools.Mcp.Abstractions.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ReverseMarkdown;

namespace DeveloperTools.Mcp.Server.Services;

public sealed partial class DuckDuckGoWebSearchService(
    HttpClient client,
    IMemoryCache cache,
    ILogger<DuckDuckGoWebSearchService> logger) : IWebSearchService
{
    private const int MaxChars = 3_000;
    private static readonly HtmlParser _parser = new();
    private static readonly Regex _ws = WhitespaceRegex();

    public async Task<IReadOnlyList<string>> SearchAsync(string query, CancellationToken ct = default)
    {
        var uri = new Uri($"https://lite.duckduckgo.com/lite/?q={Uri.EscapeDataString(query)}");
        if (cache.TryGetValue(uri, out List<string>? hits)) return hits!;

        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        req.Headers.UserAgent.ParseAdd("Mozilla/5.0");

        var html = await (await client.SendAsync(req, ct)).Content.ReadAsStringAsync(ct);
        var doc = await _parser.ParseDocumentAsync(html, ct);

        hits = doc.QuerySelectorAll("a.result-link[href*='uddg=']")
                  .Select(a => HttpUtility.ParseQueryString(new Uri("https:" + a.GetAttribute("href")).Query).Get("uddg"))
                  .Where(u => !string.IsNullOrWhiteSpace(u))
                  .Distinct()
                  .Take(5)
                  .Cast<string>()
                  .ToList();

        logger.LogInformation("DuckDuckGo search for '{Query}' returned {Urls}", query, string.Join(", ", hits));

        cache.Set(uri, hits, TimeSpan.FromMinutes(30));
        return hits;
    }

    public async Task<string> ScrapeAsync(string url, CancellationToken ct = default)
    {
        var uri = new Uri(url, UriKind.Absolute);
        if (cache.TryGetValue(uri, out string? cached)) return cached!;

        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        req.Headers.UserAgent.ParseAdd("Mozilla/5.0");

        var res = await client.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadAsStringAsync(ct);
        var doc = await _parser.ParseDocumentAsync(body, ct);
        var main = doc.QuerySelector("article") ?? doc.Body ?? doc.DocumentElement;

        foreach (var node in main.QuerySelectorAll("script,style,meta,link,noscript"))
            node.Remove();

        var html = main.InnerHtml;
        var converter = new Converter();
        var markdown = converter.Convert(html);

        markdown = _ws.Replace(markdown, " ");
        markdown = markdown.Length > MaxChars ? markdown[..MaxChars] : markdown;

        cache.Set(uri, markdown, TimeSpan.FromMinutes(30));
        return markdown;
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
