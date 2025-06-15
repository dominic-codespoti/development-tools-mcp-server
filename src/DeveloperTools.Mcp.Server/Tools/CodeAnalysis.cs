using System.ComponentModel;
using ModelContextProtocol.Server;

namespace DeveloperTools.Mcp.Server.Tools;

[McpServerToolType]
public static class CodeAnalysisTools
{
    [McpServerTool(Name = "analyze-code-symbol")]
    [Description("Return structural details (params, generics, overloads, docs) for a given class/function/etc.")]
    public static async Task<CodeSymbolInfo?> AnalyzeCodeSymbolAsync(
        [Description("Absolute or workspace-relative source file path.")] string filePath,
        [Description("Exact symbol name to analyse.")] string symbolName,
        CodeAnalyzerRegistry analyzers,
        CancellationToken ct = default)
    {
        var analyzer = analyzers.Resolve(filePath);
        return await analyzer.AnalyzeAsync(filePath, symbolName, ct);
    }
}