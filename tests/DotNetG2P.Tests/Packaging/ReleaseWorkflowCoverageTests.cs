using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DotNetG2P.Tests.Packaging
{
    public class ReleaseWorkflowCoverageTests
    {
        [Fact]
        public void InternalPackageDependencies_UseCurrentPackageVersions()
        {
            var packageInfos = UnityPackageTestData.LoadPackageInfos();
            Assert.NotEmpty(packageInfos);

            foreach (var packageInfo in packageInfos.Values)
            {
                foreach (var dependency in packageInfo.InternalDependencyVersions)
                {
                    Assert.True(
                        packageInfos.TryGetValue(dependency.Key, out var dependencyInfo),
                        $"Unknown internal dependency '{dependency.Key}' in {packageInfo.PackageName}.");
                    Assert.Equal(
                        dependencyInfo!.Version,
                        dependency.Value);
                }
            }
        }

        [Theory]
        [InlineData(".github/workflows/ci.yml")]
        [InlineData(".github/workflows/release.yml")]
        public void PackWorkflows_IncludeAllPackableProjects(string workflowRelativePath)
        {
            var repoRoot = UnityPackageTestData.ResolveRepoRoot();
            var workflowPath = Path.Combine(
                repoRoot,
                workflowRelativePath.Replace('/', Path.DirectorySeparatorChar));

            var workflowText = File.ReadAllText(workflowPath);
            var packedProjects = Regex.Matches(workflowText, @"dotnet pack\s+([^\s]+\.csproj)")
                .Select(match => match.Groups[1].Value.Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            var expectedProjects = Directory.GetFiles(Path.Combine(repoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
                .Where(IsPackableProject)
                .Select(path => Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.NotEmpty(expectedProjects);
            Assert.Equal(expectedProjects, packedProjects);
        }

        private static bool IsPackableProject(string csprojPath)
        {
            var project = XDocument.Load(csprojPath);
            var isPackable = project.Root?
                .Elements("PropertyGroup")
                .Elements("IsPackable")
                .Select(element => element.Value)
                .LastOrDefault();

            return string.Equals(isPackable, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
