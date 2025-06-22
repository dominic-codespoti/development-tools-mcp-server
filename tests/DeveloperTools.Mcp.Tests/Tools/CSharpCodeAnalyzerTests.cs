using DeveloperTools.Mcp.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace DeveloperTools.Mcp.Tests.Tools;

public class CSharpCodeAnalyzerTests
{
    [Fact]
    public async Task AnalyzeAsync_ReturnsTypeInfo_ForTestClass()
    {
        // Arrange
        var logger = new Mock<ILogger<CSharpCodeAnalyzer>>();
        var analyzer = new CSharpCodeAnalyzer(logger.Object);
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var sourceFilePath = Path.Combine(projectRoot, "tests", "DeveloperTools.Mcp.Tests", "Tools", "CSharpCodeAnalyzerTests.cs");
        var symbolName = "AnalyzeAsync_ReturnsTypeInfo_ForTestClass";

        // Act
        var result = await analyzer.AnalyzeAsync(sourceFilePath, symbolName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("DeveloperTools.Mcp.Tests.Tools.CSharpCodeAnalyzerTests.AnalyzeAsync_ReturnsTypeInfo_ForTestClass", result!.SymbolName);
        Assert.Equal("Method", result.Kind);
        Assert.Equal("public", result.Accessibility);
        Assert.Equal("System.Threading.Tasks.Task", result.ReturnType);
        Assert.Contains("FactAttribute", result.Attributes);
    }

    [Fact]
    public async Task AnalyzeAsync_WorksForNugetPackageType_MoqMock()
    {
        // Arrange
        var logger = new Mock<ILogger<CSharpCodeAnalyzer>>();
        var analyzer = new CSharpCodeAnalyzer(logger.Object);
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var sourceFilePath = Path.Combine(projectRoot, "tests", "DeveloperTools.Mcp.Tests", "Tools", "CSharpCodeAnalyzerTests.cs");
        var symbolName = "Moq.Mock";

        // Act
        var result = await analyzer.AnalyzeAsync(sourceFilePath, symbolName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Moq.Mock", result!.SymbolName);
        Assert.Equal("Type", result.Kind);
        Assert.Equal("public", result.Accessibility);
        Assert.Null(result.ReturnType);
        Assert.Empty(result.Parameters);
        Assert.NotNull(result.GenericArgs);
        Assert.Empty(result.GenericArgs);
        Assert.Contains("Mock", result.SymbolName);
        Assert.NotNull(result.Attributes);
        Assert.NotNull(result.Overloads);
        Assert.Null(result.XmlDoc); // Most NuGet types don't ship XML docs by default
    }
}