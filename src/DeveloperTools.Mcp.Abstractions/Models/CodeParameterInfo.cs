namespace DeveloperTools.Mcp.Abstractions.Models;

public record CodeParameterInfo(
    string Name,
    string Type,
    bool IsOptional,
    string? DefaultValue);