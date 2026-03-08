using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DotNetG2P.Chinese
{
    /// <summary>
    /// 単字ピンイン辞書。Unicodeコードポイントからピンイン候補へのマッピングを提供する。
    /// 埋め込みリソースまたは外部ファイルからpinyin_char.txt形式を読み込む。
    /// </summary>
    public sealed class PinyinCharDictionary
    {
        // 単一発音の漢字はstringを直接保持、複数発音の漢字はstring[]を保持
        // これにより大多数（約90%）の単一発音エントリで配列アロケーションを削減
        private readonly Dictionary<int, object> _entries;

        /// <summary>辞書エントリ数（ユニークな漢字数）</summary>
        public int Count => _entries.Count;

        private PinyinCharDictionary(Dictionary<int, object> entries)
        {
            _entries = entries;
        }

        /// <summary>
        /// 埋め込みリソースから単字ピンイン辞書を読み込む。
        /// </summary>
        /// <returns>読み込まれた単字ピンイン辞書</returns>
        public static PinyinCharDictionary LoadEmbedded()
        {
            var assembly = typeof(PinyinCharDictionary).Assembly;
            using (var stream = assembly.GetManifestResourceStream("pinyin_char.txt"))
            {
                if (stream == null)
                    throw new InvalidOperationException("埋め込みピンイン辞書リソースが見つかりません。");

                using (var reader = new StreamReader(stream))
                {
                    return ParseFromReader(reader);
                }
            }
        }

        /// <summary>
        /// 外部ファイルから単字ピンイン辞書を読み込む。
        /// </summary>
        /// <param name="path">辞書ファイルパス</param>
        /// <returns>読み込まれた単字ピンイン辞書</returns>
        public static PinyinCharDictionary LoadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("辞書ファイルパスが空です。", nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException("辞書ファイルが見つかりません。", path);

            using (var reader = new StreamReader(path))
            {
                return ParseFromReader(reader);
            }
        }

        /// <summary>
        /// 指定コードポイントの最優先ピンイン（最初の候補）を返す。
        /// </summary>
        /// <param name="codePoint">Unicodeコードポイント</param>
        /// <param name="pinyin">最優先ピンイン（見つからない場合はnull）</param>
        /// <returns>辞書に登録されている場合 true</returns>
        public bool TryLookup(int codePoint, out string pinyin)
        {
            if (_entries.TryGetValue(codePoint, out var entry))
            {
                pinyin = entry is string s ? s : ((string[])entry)[0];
                return true;
            }

            pinyin = null!;
            return false;
        }

        /// <summary>
        /// 指定コードポイントの全ピンイン候補を返す。
        /// </summary>
        /// <param name="codePoint">Unicodeコードポイント</param>
        /// <param name="pinyins">全ピンイン候補（見つからない場合はnull）</param>
        /// <returns>辞書に登録されている場合 true</returns>
        public bool TryLookupAll(int codePoint, out string[] pinyins)
        {
            if (_entries.TryGetValue(codePoint, out var entry))
            {
                pinyins = entry is string s ? new[] { s } : (string[])entry;
                return true;
            }

            pinyins = Array.Empty<string>();
            return false;
        }

        /// <summary>
        /// StreamReaderからピンイン辞書をパースする。
        /// 行形式: {16進コードポイント} {ピンイン1},{ピンイン2},...
        /// </summary>
        private static PinyinCharDictionary ParseFromReader(StreamReader reader)
        {
            var entries = new Dictionary<int, object>(45000);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                // 空行・コメント行をスキップ
                if (line.Length == 0 || line[0] == '#')
                    continue;

                ReadOnlySpan<char> lineSpan = line.AsSpan().Trim();
                if (lineSpan.IsEmpty)
                    continue;

                // 最初のスペースでコードポイントとピンイン列を分割
                var firstSpace = lineSpan.IndexOf(' ');
                if (firstSpace < 0)
                    continue;

                ReadOnlySpan<char> codePointSpan = lineSpan.Slice(0, firstSpace);
                ReadOnlySpan<char> pinyinSpan = lineSpan.Slice(firstSpace + 1).TrimStart();

                if (pinyinSpan.IsEmpty)
                    continue;

                // 16進コードポイントをパース
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                if (!int.TryParse(codePointSpan, System.Globalization.NumberStyles.HexNumber, null, out var codePoint))
                    continue;
#else
                if (!int.TryParse(new string(codePointSpan.ToArray()), System.Globalization.NumberStyles.HexNumber, null, out var codePoint))
                    continue;
#endif

                // カンマ区切りのピンインをパース
                var pinyins = ParsePinyins(pinyinSpan);
                if (pinyins.Length == 0)
                    continue;

                // 単一発音はstringを直接保持、複数発音はstring[]を保持
                entries[codePoint] = pinyins.Length == 1 ? (object)pinyins[0] : pinyins;
            }

            return new PinyinCharDictionary(entries);
        }

        /// <summary>
        /// カンマ区切りのピンイン文字列を配列にパースする。
        /// </summary>
        private static string[] ParsePinyins(ReadOnlySpan<char> span)
        {
            // カンマ数を数えて配列サイズを推定
            var count = 1;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == ',')
                    count++;
            }

            var result = new string[count];
            var index = 0;
            var start = 0;

            for (int i = 0; i <= span.Length; i++)
            {
                if (i == span.Length || span[i] == ',')
                {
                    var token = span.Slice(start, i - start).Trim();
                    if (token.IsEmpty)
                    {
                        // 空トークンが含まれる場合はスキップ
                        count--;
                    }
                    else
                    {
                        result[index++] = new string(token);
                    }
                    start = i + 1;
                }
            }

            // 空トークンがあった場合、配列をリサイズ
            if (index < result.Length)
            {
                if (index == 0)
                    return Array.Empty<string>();

                var trimmed = new string[index];
                Array.Copy(result, trimmed, index);
                return trimmed;
            }

            return result;
        }
    }
}
