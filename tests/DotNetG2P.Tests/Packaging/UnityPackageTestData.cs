using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DotNetG2P.Tests.Packaging
{
    internal static class UnityPackageTestData
    {
        public static IReadOnlyDictionary<string, PackageInfo> LoadPackageInfos()
        {
            return EnumeratePackageRoots()
                .Select(ReadPackageInfo)
                .ToDictionary(info => info.PackageName, info => info, StringComparer.Ordinal);
        }

        public static IEnumerable<string> EnumeratePackageRoots()
        {
            var srcRoot = Path.Combine(ResolveRepoRoot(), "src");
            return Directory.GetDirectories(srcRoot, "DotNetG2P.*", SearchOption.TopDirectoryOnly);
        }

        public static IReadOnlyList<string> FindMissingDependencies(IEnumerable<string> selectedPackages, IReadOnlyDictionary<string, PackageInfo> packageInfos)
        {
            var selected = new HashSet<string>(selectedPackages, StringComparer.Ordinal);
            var missing = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var packageName in selected)
            {
                if (!packageInfos.TryGetValue(packageName, out var packageInfo))
                {
                    missing.Add(packageName);
                    continue;
                }

                foreach (var dependency in packageInfo.InternalDependencies)
                {
                    if (!selected.Contains(dependency))
                        missing.Add(dependency);
                }
            }

            return missing.ToArray();
        }

        public static string BuildGitUrl(PackageInfo packageInfo)
        {
            return $"{packageInfo.RepositoryUrl}?path={packageInfo.RepositoryDirectory}";
        }

        public static bool ShouldValidateAssetMeta(string path)
        {
            if (IsIgnoredPath(path))
                return false;

            var fileName = Path.GetFileName(path);
            if (string.Equals(fileName, "package.json", StringComparison.OrdinalIgnoreCase))
                return false;

            return !fileName.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsIgnoredPath(string path)
        {
            return path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        }

        public static string ResolveRepoRoot()
        {
            var candidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
                Path.GetFullPath("."),
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(Path.Combine(candidate, "src"))
                    && Directory.Exists(Path.Combine(candidate, "tests")))
                {
                    return candidate;
                }
            }

            throw new DirectoryNotFoundException("Repository root could not be resolved.");
        }

        private static PackageInfo ReadPackageInfo(string packageRoot)
        {
            var packageJsonPath = Path.Combine(packageRoot, "package.json");
            var asmdefPath = Directory.GetFiles(packageRoot, "*.asmdef", SearchOption.TopDirectoryOnly).Single();

            using var packageJson = JsonDocument.Parse(File.ReadAllText(packageJsonPath));
            using var asmdefJson = JsonDocument.Parse(File.ReadAllText(asmdefPath));

            var packageName = packageJson.RootElement.GetProperty("name").GetString()
                ?? throw new InvalidDataException($"Package name missing in {packageJsonPath}.");
            var version = packageJson.RootElement.GetProperty("version").GetString()
                ?? throw new InvalidDataException($"Package version missing in {packageJsonPath}.");

            var dependencyNames = new List<string>();
            var dependencyVersions = new Dictionary<string, string>(StringComparer.Ordinal);
            if (packageJson.RootElement.TryGetProperty("dependencies", out var dependenciesElement))
            {
                foreach (var dependencyProperty in dependenciesElement.EnumerateObject())
                {
                    if (dependencyProperty.Name.StartsWith("com.dotnetg2p.", StringComparison.Ordinal))
                    {
                        dependencyNames.Add(dependencyProperty.Name);
                        dependencyVersions[dependencyProperty.Name] = dependencyProperty.Value.GetString()
                            ?? throw new InvalidDataException($"Dependency version missing for {dependencyProperty.Name} in {packageJsonPath}.");
                    }
                }
            }

            var assemblyName = asmdefJson.RootElement.GetProperty("name").GetString()
                ?? throw new InvalidDataException($"Assembly name missing in {asmdefPath}.");

            var assemblyReferences = new HashSet<string>(StringComparer.Ordinal);
            if (asmdefJson.RootElement.TryGetProperty("references", out var referencesElement))
            {
                foreach (var reference in referencesElement.EnumerateArray())
                {
                    var referenceValue = reference.GetString();
                    if (!string.IsNullOrWhiteSpace(referenceValue))
                        assemblyReferences.Add(referenceValue);
                }
            }

            var repositoryElement = packageJson.RootElement.GetProperty("repository");
            var repositoryUrl = repositoryElement.GetProperty("url").GetString()
                ?? throw new InvalidDataException($"Repository URL missing in {packageJsonPath}.");
            var repositoryDirectory = repositoryElement.GetProperty("directory").GetString()
                ?? throw new InvalidDataException($"Repository directory missing in {packageJsonPath}.");

            return new PackageInfo(
                packageName,
                version,
                packageRoot,
                assemblyName,
                dependencyNames,
                dependencyVersions,
                assemblyReferences,
                repositoryUrl,
                repositoryDirectory);
        }
    }

    internal sealed record PackageInfo(
        string PackageName,
        string Version,
        string PackageRoot,
        string AssemblyName,
        IReadOnlyList<string> InternalDependencies,
        IReadOnlyDictionary<string, string> InternalDependencyVersions,
        IReadOnlyCollection<string> AssemblyReferences,
        string RepositoryUrl,
        string RepositoryDirectory);
}
