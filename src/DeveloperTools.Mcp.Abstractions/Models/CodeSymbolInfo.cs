namespace DeveloperTools.Mcp.Abstractions.Models;

public record CodeSymbolInfo(
    string SymbolName,
    string Kind,          // "class", "method", "property", …
    string Accessibility, // "public", "internal", …
    string? ReturnType,
    IReadOnlyList<CodeParameterInfo> Parameters,
    IReadOnlyList<string> GenericArgs,
    IReadOnlyList<string> Attributes,
    string? XmlDoc,
    IReadOnlyList<CodeSymbolInfo> Overloads);