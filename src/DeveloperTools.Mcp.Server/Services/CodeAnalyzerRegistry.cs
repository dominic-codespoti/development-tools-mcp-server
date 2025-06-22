using DeveloperTools.Mcp.Abstractions.Services;

namespace DeveloperTools.Mcp.Server.Services;

public sealed class CodeAnalyzerRegistry
{
    private readonly Dictionary<string, ICodeAnalyzer> _byExt;

    public CodeAnalyzerRegistry(IEnumerable<ICodeAnalyzer> analyzers)
        => _byExt = analyzers.ToDictionary(
               a => a switch
               {
                   CSharpCodeAnalyzer => ".cs",
                   _ => throw new NotSupportedException()
               }, a => a);

    public ICodeAnalyzer Resolve(string filePath, string? languageHint = null)
    {
        var ext = languageHint is null ? Path.GetExtension(filePath) : GuessExt(languageHint);
        return _byExt.TryGetValue(ext, out var analyzer)
               ? analyzer
               : throw new InvalidOperationException($"No analyzer for '{ext}'.");
    }

    private static string GuessExt(string lang) => lang.ToLowerInvariant() switch
    {
        "csharp" or "cs" => ".cs",
        _ => throw new NotSupportedException(lang)
    };
}
