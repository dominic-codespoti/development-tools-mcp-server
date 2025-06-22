using DeveloperTools.Mcp.Abstractions.Models;

namespace DeveloperTools.Mcp.Abstractions.Services;

public interface ICodeAnalyzer
{
    /// <summary>Analyses a symbol and returns a rich description.</summary>
    Task<CodeSymbolInfo?> AnalyzeAsync(
        string filePath,
        string symbolName,
        CancellationToken ct = default);
}