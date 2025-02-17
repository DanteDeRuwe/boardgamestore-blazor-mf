using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using BoardgameStore.Client;
using MicrofrontendFramework.Blazor;
using Microsoft.Extensions.FileProviders;

namespace BoardgameStore.Server;

internal static class ServerAssemblyLoader
{
    private const string LibraryExtension = ".dll";
    private const string LibraryExtensionPattern = @"\.dll$";
    private const string SymbolsExtension = ".pdb";

    internal static AssemblyCollection LoadAssemblies(bool isDevelopment, IFileProvider fileProvider)
    {
        var filePaths = fileProvider.GetDirectoryContents("/").Select(x => x.PhysicalPath).ToList();
        var dllPaths = filePaths.Where(f => f.EndsWith(LibraryExtension));

        var clientAssembly = Assembly.GetAssembly(typeof(IClientMarker)) ?? throw new InvalidOperationException("Client assembly not found");
        var assemblies = new AssemblyCollection([clientAssembly]);

        foreach (var dllPath in dllPaths)
        {
            var pdbPath = Regex.Replace(dllPath, LibraryExtensionPattern, SymbolsExtension);
            var pdbShouldBeLoaded = isDevelopment && filePaths!.Contains(pdbPath);

            using var dllStream = File.Open(dllPath, FileMode.Open);
            using var pdbStream = pdbShouldBeLoaded ? File.Open(pdbPath, FileMode.Open) : null;

            var assembly = AssemblyLoadContext.Default.LoadFromStream(dllStream, pdbStream);
            assemblies.Add(assembly);
        }

        return assemblies;
    }
}