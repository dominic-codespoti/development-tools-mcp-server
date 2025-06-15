# Developer Tools MCP Server

A .NET 9.0 implementation of a Model Context Protocol (MCP) server with tools intended to improve developer productivity.

## Overview

This project provides an MCP server that exposes web search, content extraction, and code analysis capabilities as tools for AI agents and clients. It is built using the official [ModelContextProtocol](https://modelcontextprotocol.io/) C# SDK and is designed for easy integration and extension.

### Features
- **Web Search Tool**: Search the web for relevant URLs using DuckDuckGo.
- **Content Extraction Tool**: Scrape and extract readable text from web pages.
- **Code Analysis Tool**: A code analysis interface that can be implemented for many languages. Currently supports C# code symbols (classes, methods, etc.) to extract structural details such as parameters, generics, overloads, and documentation.

## Project Structure
- `src/DeveloperTools.Mcp.Abstractions/` – Shared interfaces, including `IWebSearchService`.
- `src/DeveloperTools.Mcp.Server/` – Main server implementation, tool definitions, and service registration.

## Usage

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/)

### Build and Run
```sh
dotnet build
# Run the server (from the src/DeveloperTools.Mcp.Server directory):
dotnet run
```

The server will start and listen for MCP requests via stdio transport.

### Prebuilt Binaries
Self-contained, single-file executables for Linux, Windows, and macOS are built automatically for each release. You can download these from the GitHub Releases page or from workflow artifacts. These binaries include the .NET runtime and require no additional dependencies—just download and run on your platform:

- `mcp-linux-x64`
- `mcp-win-x64.exe`
- `mcp-osx-arm64`

## Extending
You can add new tools to the MCP server by creating static methods in any class and decorating them with the `[McpServerTool]` attribute. These methods will be exposed as tools to MCP clients. For example, you can add new capabilities for data access, computation, or integration with external services. See `Tools/WebSearcher.cs` for examples of tool definitions.

To add a new web search or scraping tool, you can implement the `IWebSearchService` interface and register your implementation in the DI container. For general-purpose tools, simply add new static methods with the appropriate attributes.

## Dependencies
- [ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol)
- [AngleSharp](https://www.nuget.org/packages/AngleSharp)
- [Microsoft.Extensions.*](https://learn.microsoft.com/en-us/dotnet/core/extensions/)

## References
- [Model Context Protocol Documentation](https://modelcontextprotocol.io/)
- [C# SDK GitHub](https://github.com/modelcontextprotocol/csharp-sdk)
- [Build a Model Context Protocol MCP Server in C# (Microsoft Blog)](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/)

---

*This project is intended for developers building AI-powered tools and integrations using the Model Context Protocol.*
