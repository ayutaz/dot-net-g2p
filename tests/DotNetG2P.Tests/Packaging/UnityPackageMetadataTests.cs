using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotNetG2P.Tests.Packaging
{
    public class UnityPackageMetadataTests
    {
        [Fact]
        public void GitIgnore_DoesNotGloballyIgnoreMetaFiles()
        {
            var gitIgnorePath = Path.Combine(UnityPackageTestData.ResolveRepoRoot(), ".gitignore");
            var lines = File.ReadAllLines(gitIgnorePath);

            Assert.DoesNotContain(lines, line => string.Equals(line.Trim(), "*.meta", StringComparison.Ordinal));
        }

        [Fact]
        public void AllUnityPackageAssets_HaveMetaFiles()
        {
            var packageRoots = UnityPackageTestData.EnumeratePackageRoots().ToArray();
            Assert.NotEmpty(packageRoots);

            foreach (var packageRoot in packageRoots)
            {
                var assetPaths = Directory.GetFiles(packageRoot, "*", SearchOption.AllDirectories)
                    .Where(UnityPackageTestData.ShouldValidateAssetMeta)
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
            var directories = UnityPackageTestData.EnumeratePackageRoots()
                .SelectMany(packageRoot => Directory.GetDirectories(packageRoot, "*", SearchOption.AllDirectories))
                .Where(path => !UnityPackageTestData.IsIgnoredPath(path))
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
            var packageInfos = UnityPackageTestData.LoadPackageInfos();
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
            var metaFiles = UnityPackageTestData.EnumeratePackageRoots()
                .SelectMany(packageRoot => Directory.GetFiles(packageRoot, "*.meta", SearchOption.AllDirectories))
                .Where(path => !UnityPackageTestData.IsIgnoredPath(path))
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

                if (metaFile.EndsWith("package.json.meta", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Contains("PackageManifestImporter:", lines);
                    continue;
                }

                Assert.Contains("DefaultImporter:", lines);
            }
        }

    }
}
