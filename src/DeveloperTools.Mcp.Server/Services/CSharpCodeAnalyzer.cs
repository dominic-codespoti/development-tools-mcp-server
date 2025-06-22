using System.Reflection;
using System.Xml.Linq;
using DeveloperTools.Mcp.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace DeveloperTools.Mcp.Server.Services;

public sealed class CSharpCodeAnalyzer(ILogger<CSharpCodeAnalyzer> Logger) : ICodeAnalyzer
{
    public Task<CodeSymbolInfo?> AnalyzeAsync(string sourceFilePath, string symbolName, CancellationToken ct = default)
    {
        Logger.LogInformation("Analyzing C#  file '{SourceFilePath}' for symbol '{SymbolName}'", sourceFilePath, symbolName);

        var csproj = FindCsproj(Path.GetDirectoryName(sourceFilePath)!) ?? throw new InvalidOperationException("No .csproj found for given file.");
        var binPath = FindBuildOutput(csproj) ?? throw new InvalidOperationException("No bin output folder found. Make sure the project is built.");
        var dlls = Directory.GetFiles(binPath, "*.dll");
        var version  = GetTargetFramework(csproj) ?? throw new InvalidOperationException("No target framework found in .csproj.");

        Logger.LogInformation("Found  {DllCount} DLLs in '{BinPath}' for target framework '{Version}'", dlls.Length, binPath, version);

        var refPack = FindRefPackFolder(version);
        var refDlls = Directory.GetFiles(refPack, "*.dll");
        var binDlls = Directory.GetFiles(binPath, "*.dll");
        var allDlls = binDlls.Concat(refDlls).Append(typeof(object).Assembly.Location);
        var resolver = new PathAssemblyResolver(allDlls);
        using var mlc = new MetadataLoadContext(resolver, "System.Private.CoreLib");

        foreach (var asmPath in dlls)
        {
            var asm = mlc.LoadFromAssemblyPath(asmPath);
            var hit = Find(asm, symbolName);
            if (hit != null) return Task.FromResult<CodeSymbolInfo?>(hit);
        }

        return Task.FromResult<CodeSymbolInfo?>(null);
    }

    private CodeSymbolInfo? Find(Assembly asm, string fqName)
    {
        foreach (var type in asm.GetTypes())
        {
            if (type.FullName == fqName) return ToInfo(type);

            foreach (var m in type.GetMembers(BindingFlags.Public |
                                              BindingFlags.NonPublic |
                                              BindingFlags.Static |
                                              BindingFlags.Instance))
            {
                var full = $"{type.FullName}.{m.Name}";
                if ((full == fqName || m.Name == fqName || fqName.EndsWith($".{m.Name}")) && (m is MethodInfo || m is Type))
                {
                    return ToInfo(m);
                }
            }
        }

        return null;
    }

    string? GetTargetFramework(string csprojPath)
    {
        var xml = XDocument.Load(csprojPath);

        var tfm = xml.Descendants()
            .Where(x => x.Name.LocalName == "TargetFramework" || x.Name.LocalName == "TargetFrameworks")
            .Select(x => x.Value.Split(';').FirstOrDefault()?.Trim())
            .FirstOrDefault();

        return tfm;
    }

    string FindRefPackFolder(string tfm)
    {
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? "/usr/share/dotnet";

        var refPackRoot = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref");

        var matching = Directory.GetDirectories(refPackRoot)
            .OrderByDescending(x => x)
            .FirstOrDefault();

        if (matching == null)
            throw new InvalidOperationException($"No versioned ref packs found in {refPackRoot}");

        var refDir = Path.Combine(matching, "ref", tfm);
        if (!Directory.Exists(refDir))
            throw new DirectoryNotFoundException($"Expected ref dir: {refDir}");

        return refDir;
    }

    static string? FindCsproj(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            var proj = dir.GetFiles("*.csproj").FirstOrDefault();
            if (proj != null) return proj.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    static string? FindBuildOutput(string csprojPath)
    {
        var projDir = Path.GetDirectoryName(csprojPath)!;

        var tfms = new[] { "net9.0", "net8.0", "net7.0", "net6.0", "netstandard2.1", "netstandard2.0" };
        var configs = new[] { "Release", "Debug" };

        foreach (var config in configs)
        {
            foreach (var tfm in tfms)
            {
                var binPath = Path.Combine(projDir, "bin", config, tfm);
                if (Directory.Exists(binPath)) return binPath;
            }
        }
        return null;
    }

    private static CodeSymbolInfo ToInfo(MemberInfo m)
    {
        return m switch
        {
            MethodInfo mi => new CodeSymbolInfo(
                                $"{mi.DeclaringType?.FullName}.{mi.Name}",
                                "Method",
                                mi.IsPublic ? "public" :
                                mi.IsAssembly ? "internal" :
                                mi.IsFamily ? "protected" : "private",
                                mi.ReturnType.FullName,
                                [.. mi.GetParameters()
                                  .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                                  .Select(p => new CodeParameterInfo(
                                      p.Name!, p.ParameterType.FullName!,
                                      p.IsOptional,
                                      p.HasDefaultValue ? p.RawDefaultValue?.ToString() : null))],
                                [],
                                [.. CustomAttributesFrom(mi)],
                                GetXmlDoc(mi),
                                GetOverloads(mi)),
            Type t => new CodeSymbolInfo(
                                t.FullName ?? t.Name,
                                "Type",
                                t.IsPublic ? "public" : t.IsNotPublic ? "internal" : "private",
                                null,
                                [],
                                t.IsGenericType ? t.GetGenericArguments().Select(a => a.Name).ToList() : [],
                                CustomAttributesFrom(t).ToList(),
                                GetXmlDoc(t),
                                []),
            _ => throw new NotSupportedException($"Unsupported member {m.MemberType}"),
        };

        static IEnumerable<string> CustomAttributesFrom(MemberInfo mi)
        {
            try
            {
                return CustomAttributeData.GetCustomAttributes(mi).Select(a => a.AttributeType.Name);
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        static string? GetXmlDoc(MemberInfo mi)
        {
            var xmlPath = Path.ChangeExtension(mi.Module.FullyQualifiedName, ".xml");
            if (!File.Exists(xmlPath)) return null;
            var xdoc = XDocument.Load(xmlPath);
            var id = $"M:{mi.DeclaringType!.FullName}.{mi.Name}";
            return xdoc.Root?.Elements("member")
                             .FirstOrDefault(e => e.Attribute("name")?.Value.StartsWith(id) == true)?
                             .Value.Trim();
        }

        static List<CodeSymbolInfo> GetOverloads(MethodInfo mi) =>
            mi.DeclaringType!
              .GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                          BindingFlags.Static | BindingFlags.Instance)
              .Where(o => o.Name == mi.Name && o != mi)
              .Select(o => ToInfo(o))
              .ToList();
    }
}

