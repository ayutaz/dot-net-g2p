using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DotNetG2P.Tests.Packaging
{
    public class UnityPackageMetadataTests
    {
        [Fact]
        public void GitIgnore_DoesNotGloballyIgnoreMetaFiles()
        {
            var gitIgnorePath = Path.Combine(ResolveRepoRoot(), ".gitignore");
            var lines = File.ReadAllLines(gitIgnorePath);

            Assert.DoesNotContain(lines, line => string.Equals(line.Trim(), "*.meta", StringComparison.Ordinal));
        }

        [Fact]
        public void AllUnityPackageAssets_HaveMetaFiles()
        {
            var packageRoots = EnumeratePackageRoots().ToArray();
            Assert.NotEmpty(packageRoots);

            foreach (var packageRoot in packageRoots)
            {
                var assetPaths = Directory.GetFiles(packageRoot, "*", SearchOption.AllDirectories)
                    .Where(ShouldValidateAssetMeta)
                    .ToArray();

                Assert.NotEmpty(assetPaths);
                Assert.All(assetPaths, assetPath =>
                {
                    var metaPath = assetPath + ".meta";
                    Assert.True(File.Exists(metaPath), $"Missing meta file for asset: {assetPath}");
                });
            }
        }

        [Fact]
        public void AllUnityPackageSubdirectories_HaveMetaFiles()
        {
            var directories = EnumeratePackageRoots()
                .SelectMany(packageRoot => Directory.GetDirectories(packageRoot, "*", SearchOption.AllDirectories))
                .Where(path => !IsIgnoredPath(path))
                .ToArray();

            Assert.NotEmpty(directories);
            Assert.All(directories, directory =>
            {
                var metaPath = directory + ".meta";
                Assert.True(File.Exists(metaPath), $"Missing meta file for directory: {directory}");
            });
        }

        [Fact]
        public void InternalPackageDependencies_HaveMatchingAsmdefReferences()
        {
            var packageInfos = EnumeratePackageRoots()
                .Select(ReadPackageInfo)
                .ToDictionary(info => info.PackageName, info => info, StringComparer.Ordinal);

            Assert.NotEmpty(packageInfos);

            foreach (var packageInfo in packageInfos.Values)
            {
                foreach (var dependency in packageInfo.InternalDependencies)
                {
                    Assert.True(packageInfos.TryGetValue(dependency, out var dependencyInfo), $"Unknown internal dependency '{dependency}' in {packageInfo.PackageName}.");
                    Assert.Contains(dependencyInfo.AssemblyName, packageInfo.AssemblyReferences);
                }
            }
        }

        [Fact]
        public void GeneratedMetaFiles_HaveUniqueGuids_AndExpectedImporters()
        {
            var metaFiles = EnumeratePackageRoots()
                .SelectMany(packageRoot => Directory.GetFiles(packageRoot, "*.meta", SearchOption.AllDirectories))
                .Where(path => !IsIgnoredPath(path))
                .ToArray();

            Assert.NotEmpty(metaFiles);

            var seenGuids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var metaFile in metaFiles)
            {
                var lines = File.ReadAllLines(metaFile);
                var guidLine = lines.FirstOrDefault(line => line.StartsWith("guid: ", StringComparison.Ordinal));
                Assert.False(string.IsNullOrWhiteSpace(guidLine), $"GUID line missing in meta file: {metaFile}");

                var guid = guidLine!.Substring("guid: ".Length);
                Assert.Matches("^[0-9a-f]{32}$", guid);
                Assert.True(seenGuids.Add(guid), $"Duplicate GUID found in meta files: {metaFile}");

                var originalPath = metaFile.Substring(0, metaFile.Length - ".meta".Length);
                if (Directory.Exists(originalPath))
                {
                    Assert.Contains("folderAsset: yes", lines);
                    continue;
                }

                if (metaFile.EndsWith(".cs.meta", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Contains("MonoImporter:", lines);
                    continue;
                }

                if (metaFile.EndsWith(".asmdef.meta", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Contains("AssemblyDefinitionImporter:", lines);
                    continue;
                }

                Assert.Contains("DefaultImporter:", lines);
            }
        }

        private static IEnumerable<string> EnumeratePackageRoots()
        {
            var srcRoot = Path.Combine(ResolveRepoRoot(), "src");
            return Directory.GetDirectories(srcRoot, "DotNetG2P.*", SearchOption.TopDirectoryOnly);
        }

        private static PackageInfo ReadPackageInfo(string packageRoot)
        {
            var packageJsonPath = Path.Combine(packageRoot, "package.json");
            var asmdefPath = Directory.GetFiles(packageRoot, "*.asmdef", SearchOption.TopDirectoryOnly).Single();

            using var packageJson = JsonDocument.Parse(File.ReadAllText(packageJsonPath));
            using var asmdefJson = JsonDocument.Parse(File.ReadAllText(asmdefPath));

            var packageName = packageJson.RootElement.GetProperty("name").GetString()
                ?? throw new InvalidDataException($"Package name missing in {packageJsonPath}.");

            var dependencyNames = new List<string>();
            if (packageJson.RootElement.TryGetProperty("dependencies", out var dependenciesElement))
            {
                foreach (var dependencyProperty in dependenciesElement.EnumerateObject())
                {
                    if (dependencyProperty.Name.StartsWith("com.dotnetg2p.", StringComparison.Ordinal))
                        dependencyNames.Add(dependencyProperty.Name);
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

            return new PackageInfo(packageName, assemblyName, dependencyNames, assemblyReferences);
        }

        private static bool ShouldValidateAssetMeta(string path)
        {
            if (IsIgnoredPath(path))
                return false;

            var fileName = Path.GetFileName(path);
            if (string.Equals(fileName, "package.json", StringComparison.OrdinalIgnoreCase))
                return false;

            return !fileName.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsIgnoredPath(string path)
        {
            return path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        }

        private static string ResolveRepoRoot()
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

        private sealed record PackageInfo(
            string PackageName,
            string AssemblyName,
            IReadOnlyList<string> InternalDependencies,
            IReadOnlyCollection<string> AssemblyReferences);
    }
}
