using System;
using System.Collections.Generic;
using System.IO;

namespace DotNetG2P.MeCab
{
    /// <summary>
    /// naist-jdic 辞書の既定インストール場所を解決する。
    /// </summary>
    public static class NaistJdicLocator
    {
        private static readonly string[] RequiredFiles =
        {
            "sys.dic",
            "matrix.bin",
            "char.bin",
            "unk.dic",
        };

        /// <summary>
        /// 既定のインストール先を返す。
        /// </summary>
        public static string GetDefaultInstallPath()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userProfile, "naist-jdic");
        }

        /// <summary>
        /// 利用可能な辞書パスを探す。
        /// </summary>
        public static bool TryResolve(out string? dictionaryPath)
        {
            foreach (var candidate in GetCandidates())
            {
                if (IsValidDictionaryDirectory(candidate))
                {
                    dictionaryPath = candidate;
                    return true;
                }
            }

            dictionaryPath = null;
            return false;
        }

        /// <summary>
        /// 利用可能な辞書パスを返し、見つからない場合は例外を送出する。
        /// </summary>
        public static string ResolveOrThrow()
        {
            if (TryResolve(out var dictionaryPath) && dictionaryPath != null)
                return dictionaryPath;

            throw new InvalidOperationException(
                "naist-jdic辞書が見つかりません。環境変数 NAIST_JDIC_PATH または DOTNETG2P_NAIST_JDIC_PATH を設定するか、tools/install_naist_jdic.ps1 を実行して %USERPROFILE%\\naist-jdic に配置してください。");
        }

        internal static bool IsValidDictionaryDirectory(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return false;

            for (int i = 0; i < RequiredFiles.Length; i++)
            {
                if (!File.Exists(Path.Combine(path, RequiredFiles[i])))
                    return false;
            }

            return true;
        }

        private static IEnumerable<string> GetCandidates()
        {
            var env = Environment.GetEnvironmentVariable("DOTNETG2P_NAIST_JDIC_PATH");
            if (!string.IsNullOrWhiteSpace(env))
                yield return env;

            env = Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
            if (!string.IsNullOrWhiteSpace(env))
                yield return env;

            yield return GetDefaultInstallPath();
            yield return Path.Combine(Environment.CurrentDirectory, "naist-jdic");
            yield return Path.Combine(Environment.CurrentDirectory, "open_jtalk_dic_utf_8-1.11");
            yield return @"C:\naist-jdic";
        }
    }
}
