using System.Threading.Tasks;
using DeveloperTools.Mcp.Server.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Http;
using Xunit;

public class DuckDuckGoWebSearchServiceTests
{
    [Fact]
    public async Task ScrapeAsync_RemovesUnwantedTags_AndConvertsToMarkdown()
    {
        // Arrange
        var html = @"<html><head><title>Test</title><style>.foo{}</style><script>alert('x');</script></head><body><article><h1>Header</h1><p>This is <b>important</b> text.</p><script>bad()</script><style>bad{}</style><meta name='robots' content='noindex'><link rel='stylesheet' href='bad.css'><noscript>no js</noscript><div>More content</div></article><footer>Footer</footer></body></html>";
        var handler = new TestMessageHandler(html);
        var client = new HttpClient(handler);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<DuckDuckGoWebSearchService>.Instance;
        var service = new DuckDuckGoWebSearchService(client, cache, logger);

        // Act
        var markdown = await service.ScrapeAsync("https://example.com");

        // Assert
        Assert.Contains("# Header", markdown);
        Assert.Contains("This is **important** text.", markdown);
        Assert.Contains("More content", markdown);
        Assert.DoesNotContain("<script>", markdown);
        Assert.DoesNotContain("<style>", markdown);
        Assert.DoesNotContain("no js", markdown);
        Assert.DoesNotContain("bad.css", markdown);
        Assert.DoesNotContain("robots", markdown);
        Assert.DoesNotContain("Footer", markdown); // Footer is outside <article>
    }

    private class TestMessageHandler : HttpMessageHandler
    {
        private readonly string _html;
        public TestMessageHandler(string html) => _html = html;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(_html) });
    }
}
