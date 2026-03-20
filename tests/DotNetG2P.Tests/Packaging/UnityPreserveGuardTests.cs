using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotNetG2P.Tests.Packaging
{
    /// <summary>
    /// 全 UPM パッケージで using UnityEngine.Scripting / [Preserve] / Internal PreserveAttribute が
    /// 正しく #if UNITY_5_3_OR_NEWER でガードされていることを検証する。
    /// .NET（非Unity）環境では Internal/PreserveAttribute.cs のフォールバック定義を使い、
    /// Unity 環境では UnityEngine.dll の定義を使う二重構造を保証する。
    /// </summary>
    public class UnityPreserveGuardTests
    {
        /// <summary>
        /// "using UnityEngine.Scripting;" を使用する .cs ファイルは、
        /// 必ず #if UNITY_5_3_OR_NEWER ガードで囲まれていなければならない。
        /// </summary>
        [Fact]
        public void UsingUnityEngineScripting_MustBeGuarded()
        {
            var violations = FindUnguardedPattern(
                "using UnityEngine.Scripting;",
                (trimmed) => trimmed == "using UnityEngine.Scripting;");

            Assert.True(violations.Count == 0,
                "以下のファイルで using UnityEngine.Scripting; に #if UNITY_5_3_OR_NEWER ガードがありません:\n"
                + string.Join("\n", violations.Select(v => $"  {v.RelativePath} (行 {v.LineNumber})")));
        }

        /// <summary>
        /// [Preserve] 属性を使用する .cs ファイルは、
        /// 必ず #if UNITY_5_3_OR_NEWER ガードで囲まれていなければならない。
        /// </summary>
        [Fact]
        public void PreserveAttribute_MustBeGuarded()
        {
            var preservePattern = new Regex(@"^\s*\[Preserve\]", RegexOptions.Compiled);

            var violations = FindUnguardedPattern(
                "[Preserve]",
                (trimmed) => preservePattern.IsMatch(trimmed));

            Assert.True(violations.Count == 0,
                "以下のファイルで [Preserve] に #if UNITY_5_3_OR_NEWER ガードがありません:\n"
                + string.Join("\n", violations.Select(v => $"  {v.RelativePath} (行 {v.LineNumber})")));
        }

        /// <summary>
        /// Internal/PreserveAttribute.cs は #if !UNITY_5_3_OR_NEWER で囲まれていなければならない。
        /// Unity 環境では UnityEngine.dll の定義と衝突するため、非Unity環境でのみ定義する。
        /// </summary>
        [Fact]
        public void InternalPreserveAttribute_MustBeGuarded()
        {
            var repoRoot = UnityPackageTestData.ResolveRepoRoot();
            var preserveFiles = UnityPackageTestData.EnumeratePackageRoots()
                .Select(root => Path.Combine(root, "Internal", "PreserveAttribute.cs"))
                .Where(File.Exists)
                .ToArray();

            Assert.NotEmpty(preserveFiles);

            foreach (var preserveAttrFile in preserveFiles)
            {
                var content = File.ReadAllText(preserveAttrFile);
                var relativePath = Path.GetRelativePath(repoRoot, preserveAttrFile);

                Assert.True(content.Contains("#if !UNITY_5_3_OR_NEWER"),
                    $"{relativePath} に #if !UNITY_5_3_OR_NEWER ガードがありません。"
                    + " 非Unity環境でのみ内部 PreserveAttribute を定義する必要があります。");
            }
        }

        /// <summary>
        /// [Preserve] を使用するパッケージの asmdef は noEngineReferences: false でなければならない。
        /// noEngineReferences: true だと Unity 環境で UnityEngine.dll が参照されず CS0246 になる。
        /// </summary>
        [Fact]
        public void PackagesUsingPreserve_MustNotHaveNoEngineReferences()
        {
            var repoRoot = UnityPackageTestData.ResolveRepoRoot();
            var violations = new List<string>();

            foreach (var packageRoot in UnityPackageTestData.EnumeratePackageRoots())
            {
                var csFiles = Directory.GetFiles(packageRoot, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !UnityPackageTestData.IsIgnoredPath(f))
                    .Where(f => !f.EndsWith("PreserveAttribute.cs", StringComparison.OrdinalIgnoreCase));

                bool usesPreserve = csFiles.Any(f =>
                    File.ReadLines(f).Any(line => line.Contains("[Preserve]")));

                if (!usesPreserve)
                    continue;

                var asmdefFiles = Directory.GetFiles(packageRoot, "*.asmdef", SearchOption.TopDirectoryOnly);
                if (asmdefFiles.Length == 0)
                    continue;

                var asmdefContent = File.ReadAllText(asmdefFiles[0]);
                if (asmdefContent.Contains("\"noEngineReferences\": true"))
                {
                    violations.Add(Path.GetRelativePath(repoRoot, asmdefFiles[0]));
                }
            }

            Assert.True(violations.Count == 0,
                "[Preserve] を使用するパッケージで noEngineReferences: true が設定されています。"
                + " Unity 環境で CS0246 エラーになるため false に変更してください:\n"
                + string.Join("\n", violations.Select(v => $"  {v}")));
        }

        private static List<Violation> FindUnguardedPattern(string description, Func<string, bool> matcher)
        {
            var violations = new List<Violation>();
            var repoRoot = UnityPackageTestData.ResolveRepoRoot();

            foreach (var packageRoot in UnityPackageTestData.EnumeratePackageRoots())
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

                        if (matcher(trimmed) && !insideUnityGuard)
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

        private sealed record Violation(string RelativePath, int LineNumber);
    }
}
