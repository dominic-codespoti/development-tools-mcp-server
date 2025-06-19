using DeveloperTools.Mcp.Server.Services;
using Microsoft.Extensions.Logging;

namespace DeveloperTools.Mcp.Tests.Tools;

public class CSharpCodeAnalyzerTests
{
    private static readonly ILoggerFactory LoggerFactory;
    private static readonly ILogger<CSharpCodeAnalyzer> TestLogger;
    private static CSharpCodeAnalyzer AnalyzerInstance;
    static CSharpCodeAnalyzerTests()
    {
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder
                .AddSimpleConsole(options => { options.SingleLine = true; options.TimestampFormat = "hh:mm:ss "; })
                .SetMinimumLevel(LogLevel.Debug);
        });
        TestLogger = LoggerFactory.CreateLogger<CSharpCodeAnalyzer>();
        AnalyzerInstance = new CSharpCodeAnalyzer(TestLogger);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsNull_ForNonexistentSymbol()
    {
        var analyzer = AnalyzerInstance;
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "public class Foo {}", CancellationToken.None);
        var result = await analyzer.AnalyzeAsync(tempFile, "Bar", CancellationToken.None);
        Assert.Null(result);
        File.Delete(tempFile);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsInfo_ForClassSymbol()
    {
        var analyzer = AnalyzerInstance;
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "public class MyClass { }", CancellationToken.None);
        var result = await analyzer.AnalyzeAsync(tempFile, "MyClass", CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("MyClass", result!.SymbolName);
        Assert.Equal("NamedType", result.Kind);
        File.Delete(tempFile);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsInfo_ForMethodSymbol()
    {
        var analyzer = AnalyzerInstance;
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "public class Foo { public int Bar(int x) => x; }", CancellationToken.None);
        var result = await analyzer.AnalyzeAsync(tempFile, "Bar", CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("Bar", result!.SymbolName);
        Assert.Equal("Method", result.Kind);
        Assert.Equal("int", result.ReturnType);
        File.Delete(tempFile);
    }

    [Fact]
    public async Task AnalyzeAsync_WorksForNugetPackageType()
    {
        var analyzer = AnalyzerInstance;
        var tempFile = Path.GetTempFileName();
        // Reference JsonSerializer in a method signature to ensure Roslyn includes it
        await File.WriteAllTextAsync(tempFile, @"using System.Text.Json;\npublic class Foo { public void Bar(JsonSerializer s) { } }", CancellationToken.None);
        var result = await analyzer.AnalyzeAsync(tempFile, "JsonSerializer", CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("JsonSerializer", result!.SymbolName);
        File.Delete(tempFile);
    }

    [Fact]
    public async Task AnalyzeAsync_WorksForNugetPackageType_FullyQualified()
    {
        var analyzer = AnalyzerInstance;
        var tempFile = Path.GetTempFileName();
        // No need to reference JsonSerializer in code, just test fully qualified name
        await File.WriteAllTextAsync(tempFile, "public class Foo { }", CancellationToken.None);
        var result = await analyzer.AnalyzeAsync(tempFile, "System.Text.Json.JsonSerializer", CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("JsonSerializer", result!.SymbolName);
        File.Delete(tempFile);
    }
}