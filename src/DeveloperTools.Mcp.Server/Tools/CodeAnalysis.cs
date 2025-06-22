using System.ComponentModel;
using DeveloperTools.Mcp.Abstractions.Models;
using DeveloperTools.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace DeveloperTools.Mcp.Server.Tools;

[McpServerToolType]
public class CodeAnalysisTools
{
    [McpServerTool(Name = "analyze-code-symbol")]
    [Description("Return structural details (params, generics, overloads, docs) for a given class/function/etc. Supports C#.")]
    public static async Task<CodeSymbolInfo?> AnalyzeCodeSymbolAsync(
        [Description("Absolute file path to the source file.")] string file_path,
        [Description("Fully qualified symbol name to analyse.")] string symbol_name,
        CodeAnalyzerRegistry analyzers,
        CancellationToken ct = default)
    {
        var analyzer = analyzers.Resolve(file_path);
        return await analyzer.AnalyzeAsync(file_path, symbol_name, ct);
    }
}