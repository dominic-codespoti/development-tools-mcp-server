using DeveloperTools.Mcp.Abstractions.Services;
using DeveloperTools.Mcp.Server.Services;
using DeveloperTools.Mcp.Server.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Error;
});

var informationalVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
var loggerFactory = LoggerFactory.Create(cfg => cfg.AddConsole());
var logger = loggerFactory.CreateLogger("Startup");
logger.LogInformation($"Running version {informationalVersion}");

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<DuckDuckGoWebSearchService>();
builder.Services.AddSingleton<IWebSearchService, DuckDuckGoWebSearchService>();
builder.Services.AddSingleton<ICodeAnalyzer, CSharpCodeAnalyzer>();
builder.Services.AddSingleton<CodeAnalyzerRegistry>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<CodeAnalysisTools>()
    .WithTools<WebTools>();

await builder.Build().RunAsync();
