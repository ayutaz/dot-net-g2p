using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DotNetG2P.Tests.Packaging
{
    /// <summary>
    /// noEngineReferences: true な asmdef を持つパッケージで、
    /// using UnityEngine.Scripting / [Preserve] が #if UNITY_5_3_OR_NEWER で
    /// 正しくガードされていることを検証する。
    /// </summary>
    public class UnityPreserveGuardTests
    {
        /// <summary>
        /// noEngineReferences: true のパッケージ内で "using UnityEngine.Scripting;" を使用する
        /// .cs ファイルは、必ず #if UNITY_5_3_OR_NEWER ガードで囲まれていなければならない。
        /// </summary>
        [Fact]
        public void UsingUnityEngineScripting_MustBeGuarded_InNoEngineReferencesPackages()
        {
            var violations = FindUnguardedUsings();
            Assert.True(violations.Count == 0,
                "以下のファイルで using UnityEngine.Scripting; に #if UNITY_5_3_OR_NEWER ガードがありません:\n"
                + string.Join("\n", violations.Select(v => $"  {v.RelativePath} (行 {v.LineNumber})")));
        }

        /// <summary>
        /// noEngineReferences: true のパッケージ内で [Preserve] 属性を使用する
        /// .cs ファイルは、必ず #if UNITY_5_3_OR_NEWER ガードで囲まれていなければならない。
        /// </summary>
        [Fact]
        public void PreserveAttribute_MustBeGuarded_InNoEngineReferencesPackages()
        {
            var violations = FindUnguardedPreserveAttributes();
            Assert.True(violations.Count == 0,
                "以下のファイルで [Preserve] に #if UNITY_5_3_OR_NEWER ガードがありません:\n"
                + string.Join("\n", violations.Select(v => $"  {v.RelativePath} (行 {v.LineNumber})")));
        }

        /// <summary>
        /// noEngineReferences: true のパッケージ内の Internal/PreserveAttribute.cs は
        /// #if !UNITY_5_3_OR_NEWER で囲まれていなければならない。
        /// </summary>
        [Fact]
        public void InternalPreserveAttribute_MustBeGuarded_InNoEngineReferencesPackages()
        {
            var packageRoots = GetNoEngineReferencesPackageRoots();
            Assert.NotEmpty(packageRoots);

            foreach (var packageRoot in packageRoots)
            {
                var preserveAttrFile = Path.Combine(packageRoot, "Internal", "PreserveAttribute.cs");
                if (!File.Exists(preserveAttrFile))
                    continue;

                var content = File.ReadAllText(preserveAttrFile);
                var relativePath = Path.GetRelativePath(UnityPackageTestData.ResolveRepoRoot(), preserveAttrFile);

                Assert.True(content.Contains("#if !UNITY_5_3_OR_NEWER"),
                    $"{relativePath} に #if !UNITY_5_3_OR_NEWER ガードがありません。"
                    + " 非Unity環境でのみ内部 PreserveAttribute を定義する必要があります。");
            }
        }

        private static List<Violation> FindUnguardedUsings()
        {
            var violations = new List<Violation>();
            var repoRoot = UnityPackageTestData.ResolveRepoRoot();

            foreach (var packageRoot in GetNoEngineReferencesPackageRoots())
            {
                var csFiles = Directory.GetFiles(packageRoot, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !UnityPackageTestData.IsIgnoredPath(f))
                    .Where(f => !f.EndsWith("PreserveAttribute.cs", StringComparison.OrdinalIgnoreCase));

                foreach (var csFile in csFiles)
                {
                    var lines = File.ReadAllLines(csFile);
                    var insideUnityGuard = false;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        var trimmed = lines[i].Trim();

                        if (trimmed == "#if UNITY_5_3_OR_NEWER")
                            insideUnityGuard = true;
                        else if (trimmed == "#endif" && insideUnityGuard)
                            insideUnityGuard = false;

                        if (trimmed == "using UnityEngine.Scripting;" && !insideUnityGuard)
                        {
                            violations.Add(new Violation(
                                Path.GetRelativePath(repoRoot, csFile),
                                i + 1));
                        }
                    }
                }
            }

            return violations;
        }

        private static List<Violation> FindUnguardedPreserveAttributes()
        {
            var violations = new List<Violation>();
            var repoRoot = UnityPackageTestData.ResolveRepoRoot();
            var preservePattern = new Regex(@"^\s*\[Preserve\]", RegexOptions.Compiled);

            foreach (var packageRoot in GetNoEngineReferencesPackageRoots())
            {
                var csFiles = Directory.GetFiles(packageRoot, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !UnityPackageTestData.IsIgnoredPath(f))
                    .Where(f => !f.EndsWith("PreserveAttribute.cs", StringComparison.OrdinalIgnoreCase));

                foreach (var csFile in csFiles)
                {
                    var lines = File.ReadAllLines(csFile);
                    var insideUnityGuard = false;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        var trimmed = lines[i].Trim();

                        if (trimmed == "#if UNITY_5_3_OR_NEWER")
                            insideUnityGuard = true;
                        else if (trimmed == "#endif" && insideUnityGuard)
                            insideUnityGuard = false;

                        if (preservePattern.IsMatch(lines[i]) && !insideUnityGuard)
                        {
                            violations.Add(new Violation(
                                Path.GetRelativePath(repoRoot, csFile),
                                i + 1));
                        }
                    }
                }
            }

            return violations;
        }

        private static List<string> GetNoEngineReferencesPackageRoots()
        {
            var roots = new List<string>();

            foreach (var packageRoot in UnityPackageTestData.EnumeratePackageRoots())
            {
                var asmdefFiles = Directory.GetFiles(packageRoot, "*.asmdef", SearchOption.TopDirectoryOnly);
                if (asmdefFiles.Length == 0)
                    continue;

                var asmdefPath = asmdefFiles[0];
                using var doc = JsonDocument.Parse(File.ReadAllText(asmdefPath));

                if (doc.RootElement.TryGetProperty("noEngineReferences", out var prop)
                    && prop.GetBoolean())
                {
                    roots.Add(packageRoot);
                }
            }

            return roots;
        }

        private sealed record Violation(string RelativePath, int LineNumber);
    }
}
