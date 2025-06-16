using DeveloperTools.Mcp.Abstractions.Services;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

public sealed class CSharpCodeAnalyzer : ICodeAnalyzer
{
    private readonly AdhocWorkspace _ws;
    private readonly ILogger<CSharpCodeAnalyzer> _logger;

    public CSharpCodeAnalyzer(ILogger<CSharpCodeAnalyzer> logger)
    {
        _logger = logger;
        _ws = new AdhocWorkspace();
    }

    public async Task<CodeSymbolInfo?> AnalyzeAsync(
        string filePath,
        string symbolName,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing symbol '{SymbolName}' in file '{FilePath}'", symbolName, filePath);

        var code = await File.ReadAllTextAsync(filePath, ct);

        // Get all reference assemblies
        var refDir = FindReferenceAssembliesDir();
        var refs = Directory.GetFiles(refDir, "*.dll").Select(path => MetadataReference.CreateFromFile(path)).ToList();

        // Get all user project DLLs
        var userDlls = FindUserProjectDlls();
        refs.AddRange(userDlls.Cast<PortableExecutableReference>());

        // Set up parse and compilation options for latest C#
        var parseOptions = new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview);
        var compilationOptions = new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, metadataReferenceResolver: null, assemblyIdentityComparer: AssemblyIdentityComparer.Default);
        var proj = _ws.AddProject("tmp", LanguageNames.CSharp)
            .WithMetadataReferences(refs)
            .WithParseOptions(parseOptions)
            .WithCompilationOptions(compilationOptions);

        var doc = _ws.AddDocument(proj.Id, Path.GetFileName(filePath), Microsoft.CodeAnalysis.Text.SourceText.From(code));
        proj = doc.Project.WithMetadataReferences(refs);

        var compilation = await proj.GetCompilationAsync(ct);
        var globalNs   = compilation!.GlobalNamespace;

        ISymbol? symbol = FindSymbol(globalNs, symbolName);
        if (symbol == null)
        {
            foreach (var asm in compilation.References)
            {
                var asmSym = compilation.GetAssemblyOrModuleSymbol(asm) as IAssemblySymbol;
                if (asmSym == null) continue;
                var found = FindSymbol(asmSym.GlobalNamespace, symbolName);
                if (found != null)
                {
                    symbol = found;
                    break;
                }
            }
        }

        if (symbol == null && symbolName.Contains("."))
        {
            var typeSym = compilation.GetTypeByMetadataName(symbolName);
            if (typeSym != null)
                symbol = typeSym;
        }

        return symbol is null
            ? null 
            : ToInfo(symbol);

        ISymbol? FindSymbol(INamespaceOrTypeSymbol scope, string name)
        {
            foreach (var member in scope.GetMembers())
            {
                if (member.Name.Equals(name, StringComparison.Ordinal))
                    return member;
                if (member is INamespaceOrTypeSymbol nested)
                {
                    var found = FindSymbol(nested, name);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        CodeSymbolInfo ToInfo(ISymbol s) =>
            new(
                s.Name,
                s.Kind.ToString(),
                s.DeclaredAccessibility.ToString().ToLowerInvariant(),
                (s as IMethodSymbol)?.ReturnType.ToDisplayString(),
                (s as IMethodSymbol)?.Parameters
                    .Select(p =>
                        new CodeParameterInfo(p.Name,
                            p.Type.ToDisplayString(),
                            p.IsOptional,
                            p.HasExplicitDefaultValue ? p.ExplicitDefaultValue?.ToString() : null))
                    .ToList() ?? [],
                (s as INamedTypeSymbol)?.TypeArguments.Select(a => a.Name).ToList() ?? [],
                [.. s.GetAttributes().Select(a => a.AttributeClass?.Name ?? "")],
                s.GetDocumentationCommentXml(),
                (s as IMethodSymbol)?.ContainingType
                    .GetMembers(s.Name)
                    .OfType<IMethodSymbol>()
                    .Where(o => !SymbolEqualityComparer.Default.Equals(o, s))
                    .Select(ToInfo)
                    .ToList()
                    ?? new List<CodeSymbolInfo>()
            );
    }

    private static string FindReferenceAssembliesDir()
    {
        string? refDir = null;
        var tfm = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (string.IsNullOrEmpty(dotnetRoot)) throw new InvalidOperationException("DOTNET_ROOT environment variable is not set. Please set DOTNET_ROOT to your .NET SDK root directory.");

        if (tfm.Contains(".NET", StringComparison.OrdinalIgnoreCase))
        {
            var packsDir = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref");
            if (Directory.Exists(packsDir))
            {
                var latest = Directory.GetDirectories(packsDir).OrderByDescending(x => x).FirstOrDefault();
                if (latest != null)
                {
                    var refPath = Path.Combine(latest, "ref");
                    if (Directory.Exists(refPath))
                    {
                        var tfmDir = Directory.GetDirectories(refPath)
                            .OrderByDescending(x => x)
                            .FirstOrDefault();
                        if (tfmDir != null)
                            refDir = tfmDir;
                    }
                }
            }
        }
        if (refDir == null || !Directory.Exists(refDir)) throw new InvalidOperationException("Could not locate .NET reference assemblies on this system.");
        return refDir;
    }

    private static IEnumerable<MetadataReference> FindUserProjectDlls()
    {
        var dlls = new List<MetadataReference>();
        var tfms = new[] { "net9.0", "net8.0", "net7.0", "net6.0", "net5.0", "netcoreapp3.1", "netstandard2.1", "netstandard2.0" };
        var binNames = new[] { "bin", "obj" };
        var configs = new[] { "Debug", "Release" };
        var root = AppContext.BaseDirectory;

        foreach (var bin in binNames)
        {
            foreach (var config in configs)
            {
                foreach (var tfm in tfms)
                {
                    var dir = Path.Combine(root, bin, config, tfm);
                    if (Directory.Exists(dir))
                    {
                        foreach (var dll in Directory.GetFiles(dir, "*.dll"))
                        {
                            try { dlls.Add(MetadataReference.CreateFromFile(dll)); } catch { }
                        }
                    }
                }
            }
        }

        return dlls;
    }
}
