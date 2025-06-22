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

### Install as a .NET Tool
The MCP server is distributed as a .NET global tool via NuGet. To install it, run:

```sh
dotnet tool install --global developer-tools-mcp-server
```

To update to the latest version:

```sh
dotnet tool update --global developer-tools-mcp-server
```

### Run the Server
After installing, you can start the server with:

```sh
developer-tools-mcp-server
```

The server will start and listen for MCP requests via stdio transport.

### Local Build (for development)
If you want to build and run the server locally (for development or testing):

```sh
dotnet build
cd src/DeveloperTools.Mcp.Server
dotnet run
```

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
