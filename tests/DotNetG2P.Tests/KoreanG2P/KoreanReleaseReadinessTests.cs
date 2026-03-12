using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using DotNetG2P.Tests.KoreanG2P.Benchmarking;
using DotNetG2P.Tests.Packaging;

namespace DotNetG2P.Tests.KoreanG2P
{
    public class KoreanReleaseReadinessTests
    {
        [Fact]
        public void KoreanPackage_ReadmeAndNotice_DescribeUsageAndLimits()
        {
            var readmePath = Path.Combine(KoreanBenchmarkPaths.RepoRoot, "src", "DotNetG2P.Korean", "README.md");
            var noticePath = Path.Combine(KoreanBenchmarkPaths.RepoRoot, "src", "DotNetG2P.Korean", "THIRD-PARTY-NOTICES.md");

            Assert.True(File.Exists(readmePath), $"README not found: {readmePath}");
            Assert.True(File.Exists(noticePath), $"Notice not found: {noticePath}");

            var readme = File.ReadAllText(readmePath);
            var notice = File.ReadAllText(noticePath);

            Assert.Contains("Quick Start", readme, System.StringComparison.Ordinal);
            Assert.Contains("Known Limitations", readme, System.StringComparison.Ordinal);
            Assert.Contains("Thread Safety", readme, System.StringComparison.Ordinal);
            Assert.Contains("KoreanG2PEngine", readme, System.StringComparison.Ordinal);
            Assert.Contains("g2pk_parity", readme, System.StringComparison.Ordinal);
            Assert.Contains("does not bundle third-party code or datasets", notice, System.StringComparison.Ordinal);
        }

        [Fact]
        public void KoreanPackage_Metadata_ContainsReleaseFields()
        {
            var csprojPath = Path.Combine(KoreanBenchmarkPaths.RepoRoot, "src", "DotNetG2P.Korean", "DotNetG2P.Korean.csproj");
            var packageJsonPath = Path.Combine(KoreanBenchmarkPaths.RepoRoot, "src", "DotNetG2P.Korean", "package.json");

            var project = XDocument.Load(csprojPath);
            var propertyGroup = Assert.Single(project.Root!.Elements("PropertyGroup"));

            Assert.Equal("Apache-2.0", propertyGroup.Element("PackageLicenseExpression")?.Value);
            Assert.Equal("README.md", propertyGroup.Element("PackageReadmeFile")?.Value);
            Assert.Equal("ayutaz", propertyGroup.Element("Authors")?.Value);
            Assert.Equal("https://github.com/ayutaz/dot-net-g2p", propertyGroup.Element("PackageProjectUrl")?.Value);
            Assert.Equal("https://github.com/ayutaz/dot-net-g2p.git", propertyGroup.Element("RepositoryUrl")?.Value);

            var packedFiles = project.Root.Elements("ItemGroup")
                .Elements("None")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            Assert.Contains("README.md", packedFiles);
            Assert.Contains("THIRD-PARTY-NOTICES.md", packedFiles);

            using var packageDoc = JsonDocument.Parse(File.ReadAllText(packageJsonPath));
            var root = packageDoc.RootElement;
            Assert.Contains("Hangul-first", root.GetProperty("description").GetString(), System.StringComparison.Ordinal);

            var keywords = root.GetProperty("keywords").EnumerateArray()
                .Select(item => item.GetString())
                .Where(item => item != null)
                .Cast<string>()
                .ToArray();

            Assert.Contains("korean", keywords);
            Assert.Contains("hangul", keywords);
            Assert.Contains("phoneme", keywords);
            Assert.Contains("normalization", keywords);
        }

        [Fact]
        public void MultilingualPackage_MetadataAndDocs_IncludeKoreanRouting()
        {
            var readmePath = Path.Combine(KoreanBenchmarkPaths.RepoRoot, "src", "DotNetG2P.Multilingual", "README.md");
            var noticePath = Path.Combine(KoreanBenchmarkPaths.RepoRoot, "src", "DotNetG2P.Multilingual", "THIRD-PARTY-NOTICES.md");
            var packageJsonPath = Path.Combine(KoreanBenchmarkPaths.RepoRoot, "src", "DotNetG2P.Multilingual", "package.json");

            var readme = File.ReadAllText(readmePath);
            var notice = File.ReadAllText(noticePath);

            Assert.Contains("Supported Languages", readme, System.StringComparison.Ordinal);
            Assert.Contains("Korean", readme, System.StringComparison.Ordinal);
            Assert.Contains("KoreanOptions", readme, System.StringComparison.Ordinal);
            Assert.Contains("Known Limitations", readme, System.StringComparison.Ordinal);
            Assert.Contains("do not bundle third-party code or datasets", notice, System.StringComparison.Ordinal);

            using var packageDoc = JsonDocument.Parse(File.ReadAllText(packageJsonPath));
            var root = packageDoc.RootElement;
            Assert.Contains("Korean", root.GetProperty("description").GetString(), System.StringComparison.Ordinal);

            var dependencies = root.GetProperty("dependencies");
            var packageInfos = UnityPackageTestData.LoadPackageInfos();
            Assert.Equal(packageInfos["com.dotnetg2p.korean"].Version, dependencies.GetProperty("com.dotnetg2p.korean").GetString());
        }

        [Theory]
        [InlineData("README.md")]
        [InlineData("README_EN.md")]
        [InlineData("README_ZH.md")]
        public void RootReadmes_DocumentKoreanPackageAndMultilingualOptions(string fileName)
        {
            var path = Path.Combine(KoreanBenchmarkPaths.RepoRoot, fileName);
            var text = File.ReadAllText(path);

            Assert.Contains("DotNetG2P.Korean", text, System.StringComparison.Ordinal);
            Assert.Contains("KoreanG2PEngine", text, System.StringComparison.Ordinal);
            Assert.Contains("koreanOptions", text, System.StringComparison.Ordinal);
            Assert.Contains("MultilingualG2PEngine", text, System.StringComparison.Ordinal);
        }
    }
}
