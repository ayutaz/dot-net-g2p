using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotNetG2P.Tests.Packaging
{
    public class UnityPackageInstallMatrixTests
    {
        private static readonly IReadOnlyDictionary<string, string[]> s_languageUnits =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["Japanese"] = new[] { "com.dotnetg2p.core", "com.dotnetg2p.mecab" },
                ["English"] = new[] { "com.dotnetg2p.english" },
                ["Chinese"] = new[] { "com.dotnetg2p.chinese" },
                ["Korean"] = new[] { "com.dotnetg2p.korean" },
                ["Spanish"] = new[] { "com.dotnetg2p.spanish" },
                ["French"] = new[] { "com.dotnetg2p.french" },
                ["Portuguese"] = new[] { "com.dotnetg2p.portuguese" },
                ["Swedish"] = new[] { "com.dotnetg2p.swedish" },
            };

        public static IEnumerable<object[]> LanguageCombinationScenarios()
        {
            var names = s_languageUnits.Keys.ToArray();
            for (int mask = 1; mask < (1 << names.Length); mask++)
            {
                var selectedLanguages = new List<string>();
                var packageNames = new HashSet<string>(StringComparer.Ordinal);

                for (int bit = 0; bit < names.Length; bit++)
                {
                    if ((mask & (1 << bit)) == 0)
                        continue;

                    var languageName = names[bit];
                    selectedLanguages.Add(languageName);
                    foreach (var packageName in s_languageUnits[languageName])
                        packageNames.Add(packageName);
                }

                yield return new object[]
                {
                    string.Join("+", selectedLanguages),
                    packageNames.OrderBy(name => name, StringComparer.Ordinal).ToArray(),
                };
            }
        }

        public static IEnumerable<object[]> InvalidScenarioData()
        {
            yield return new object[]
            {
                "MeCabOnly",
                new[] { "com.dotnetg2p.mecab" },
                new[] { "com.dotnetg2p.core" },
            };

            yield return new object[]
            {
                "MultilingualOnly",
                new[] { "com.dotnetg2p.multilingual" },
                new[]
                {
                    "com.dotnetg2p.chinese",
                    "com.dotnetg2p.core",
                    "com.dotnetg2p.english",
                    "com.dotnetg2p.french",
                    "com.dotnetg2p.korean",
                    "com.dotnetg2p.mecab",
                    "com.dotnetg2p.portuguese",
                    "com.dotnetg2p.spanish",
                    "com.dotnetg2p.swedish",
                },
            };

            yield return new object[]
            {
                "MultilingualWithoutMeCab",
                new[]
                {
                    "com.dotnetg2p.multilingual",
                    "com.dotnetg2p.core",
                    "com.dotnetg2p.english",
                    "com.dotnetg2p.chinese",
                    "com.dotnetg2p.korean",
                    "com.dotnetg2p.spanish",
                    "com.dotnetg2p.french",
                    "com.dotnetg2p.portuguese",
                    "com.dotnetg2p.swedish",
                },
                new[] { "com.dotnetg2p.mecab" },
            };
        }

        [Theory]
        [MemberData(nameof(LanguageCombinationScenarios))]
        public void LanguageCombinationScenarios_AreDependencyClosed_AndCanBeExpressedAsGitUrls(string scenarioName, string[] packageNames)
        {
            var packageInfos = UnityPackageTestData.LoadPackageInfos();
            var missingDependencies = UnityPackageTestData.FindMissingDependencies(packageNames, packageInfos);

            Assert.True(missingDependencies.Count == 0, $"{scenarioName} is missing dependencies: {string.Join(", ", missingDependencies)}");

            var gitUrls = packageNames
                .Select(packageName => UnityPackageTestData.BuildGitUrl(packageInfos[packageName]))
                .ToArray();

            Assert.Equal(packageNames.Length, gitUrls.Length);
            Assert.Equal(gitUrls.Length, gitUrls.Distinct(StringComparer.Ordinal).Count());

            foreach (var packageName in packageNames)
            {
                var packageInfo = packageInfos[packageName];
                Assert.True(Directory.Exists(packageInfo.PackageRoot), $"Package root not found for {packageName}: {packageInfo.PackageRoot}");
                Assert.True(File.Exists(Path.Combine(packageInfo.PackageRoot, "package.json")), $"package.json missing for {packageName}.");
                Assert.Single(Directory.GetFiles(packageInfo.PackageRoot, "*.asmdef", SearchOption.TopDirectoryOnly));
                Assert.StartsWith("https://github.com/ayutaz/dot-net-g2p.git?path=src/", UnityPackageTestData.BuildGitUrl(packageInfo), StringComparison.Ordinal);
            }
        }

        [Fact]
        public void MultilingualScenario_IsDependencyClosed_AndIncludesAllSupportedPackages()
        {
            var packageInfos = UnityPackageTestData.LoadPackageInfos();
            var packageNames = new[]
            {
                "com.dotnetg2p.core",
                "com.dotnetg2p.mecab",
                "com.dotnetg2p.english",
                "com.dotnetg2p.chinese",
                "com.dotnetg2p.korean",
                "com.dotnetg2p.spanish",
                "com.dotnetg2p.french",
                "com.dotnetg2p.portuguese",
                "com.dotnetg2p.swedish",
                "com.dotnetg2p.multilingual",
            };

            var missingDependencies = UnityPackageTestData.FindMissingDependencies(packageNames, packageInfos);
            Assert.Empty(missingDependencies);
            Assert.Equal(10, packageNames.Length);

            var gitUrls = packageNames
                .Select(packageName => UnityPackageTestData.BuildGitUrl(packageInfos[packageName]))
                .ToArray();

            Assert.Equal(gitUrls.Length, gitUrls.Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public void CoreOnlyScenario_IsDependencyClosed_AndCanBeInstalledDirectly()
        {
            var packageInfos = UnityPackageTestData.LoadPackageInfos();
            var packageNames = new[] { "com.dotnetg2p.core" };

            var missingDependencies = UnityPackageTestData.FindMissingDependencies(packageNames, packageInfos);
            Assert.Empty(missingDependencies);

            var packageInfo = packageInfos[packageNames[0]];
            Assert.Equal("https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Core", UnityPackageTestData.BuildGitUrl(packageInfo));
        }

        [Theory]
        [MemberData(nameof(InvalidScenarioData))]
        public void InvalidScenarios_ReportExpectedMissingDependencies(string scenarioName, string[] packageNames, string[] expectedMissing)
        {
            var packageInfos = UnityPackageTestData.LoadPackageInfos();
            var missingDependencies = UnityPackageTestData.FindMissingDependencies(packageNames, packageInfos);

            Assert.True(missingDependencies.Count > 0, $"{scenarioName} should not be dependency-closed.");
            Assert.Equal(expectedMissing, missingDependencies.OrderBy(name => name, StringComparer.Ordinal).ToArray());
        }
    }
}
