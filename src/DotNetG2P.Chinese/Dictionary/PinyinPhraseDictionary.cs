using System;
using System.Collections.Generic;
using System.IO;

namespace DotNetG2P.Chinese
{
    /// <summary>
    /// フレーズ（熟語）ピンイン辞書。複数文字フレーズからピンイン列へのマッピングを提供する。
    /// 最長一致検索で多音字（ポリフォン）を文脈に基づいて解決する。
    /// </summary>
    public sealed class PinyinPhraseDictionary
    {
        private readonly Dictionary<string, string[]> _entries;
        private int _maxPhraseLength;

        /// <summary>辞書エントリ数（ユニークなフレーズ数）</summary>
        public int Count => _entries.Count;

        private PinyinPhraseDictionary(Dictionary<string, string[]> entries, int maxPhraseLength)
        {
            _entries = entries;
            _maxPhraseLength = maxPhraseLength;
        }

        /// <summary>
        /// 埋め込みリソースからフレーズピンイン辞書を読み込む。
        /// </summary>
        /// <returns>読み込まれたフレーズピンイン辞書</returns>
        public static PinyinPhraseDictionary LoadEmbedded()
        {
            var assembly = typeof(PinyinPhraseDictionary).Assembly;
            using (var stream = assembly.GetManifestResourceStream("pinyin_phrase.txt"))
            {
                if (stream == null)
                    throw new InvalidOperationException("埋め込みフレーズピンイン辞書リソースが見つかりません。");

                using (var reader = new StreamReader(stream))
                {
                    return ParseFromReader(reader);
                }
            }
        }

        /// <summary>
        /// 外部ファイルからフレーズピンイン辞書を読み込む。
        /// </summary>
        /// <param name="path">辞書ファイルパス</param>
        /// <returns>読み込まれたフレーズピンイン辞書</returns>
        public static PinyinPhraseDictionary LoadFromFile(string path)
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
        /// ストリームからフレーズピンイン辞書を読み込む（Unity StreamingAssets / WebGL対応）。
        /// </summary>
        /// <param name="stream">辞書データのストリーム</param>
        /// <returns>読み込まれたフレーズピンイン辞書</returns>
        public static PinyinPhraseDictionary LoadFromStream(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            using (var reader = new StreamReader(stream, encoding: System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                return ParseFromReader(reader);
            }
        }

        /// <summary>
        /// フレーズの完全一致検索を行う。
        /// </summary>
        /// <param name="phrase">検索するフレーズ</param>
        /// <param name="pinyins">各文字のピンイン配列（見つからない場合はnull）</param>
        /// <returns>辞書に登録されている場合 true</returns>
        public bool TryLookup(string phrase, out string[] pinyins)
        {
            if (_entries.TryGetValue(phrase, out pinyins!))
                return true;

            pinyins = Array.Empty<string>();
            return false;
        }

        /// <summary>
        /// テキスト中の指定位置から最長一致フレーズを検索する。
        /// </summary>
        /// <param name="text">検索対象テキスト</param>
        /// <param name="startIndex">検索開始位置</param>
        /// <param name="pinyins">マッチしたフレーズの各文字ピンイン配列（マッチなしの場合はnull）</param>
        /// <returns>マッチしたフレーズの文字数（0=マッチなし）</returns>
        public int FindLongestMatch(string text, int startIndex, out string[] pinyins)
        {
            var remaining = text.Length - startIndex;
            var maxLen = Math.Min(_maxPhraseLength, remaining);

            for (int len = maxLen; len >= 2; len--)
            {
                var phrase = text.Substring(startIndex, len);
                if (_entries.TryGetValue(phrase, out pinyins!))
                    return len;
            }

            pinyins = Array.Empty<string>();
            return 0;
        }

        /// <summary>
        /// StreamReaderからフレーズピンイン辞書をパースする。
        /// 行形式: {フレーズ}\t{スペース区切りピンイン列}
        /// </summary>
        private static PinyinPhraseDictionary ParseFromReader(StreamReader reader)
        {
            var entries = new Dictionary<string, string[]>(300000);
            var maxPhraseLength = 0;

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                // 空行・コメント行をスキップ
                if (line.Length == 0 || line[0] == '#')
                    continue;

                ReadOnlySpan<char> lineSpan = line.AsSpan().Trim();
                if (lineSpan.IsEmpty)
                    continue;

                // タブ文字でフレーズとピンイン列を分割
                var tabIndex = lineSpan.IndexOf('\t');
                if (tabIndex < 0)
                    continue;

                ReadOnlySpan<char> phraseSpan = lineSpan.Slice(0, tabIndex).TrimEnd();
                ReadOnlySpan<char> pinyinSpan = lineSpan.Slice(tabIndex + 1).TrimStart();

                if (phraseSpan.IsEmpty || pinyinSpan.IsEmpty)
                    continue;

                var phrase = new string(phraseSpan);
                var pinyins = ParsePinyins(pinyinSpan);
                if (pinyins.Length == 0)
                    continue;

                entries[phrase] = pinyins;

                if (phrase.Length > maxPhraseLength)
                    maxPhraseLength = phrase.Length;
            }

            return new PinyinPhraseDictionary(entries, maxPhraseLength);
        }

        /// <summary>
        /// スペース区切りのピンイン文字列を配列にパースする。
        /// </summary>
        private static string[] ParsePinyins(ReadOnlySpan<char> span)
        {
            // スペース数を数えて配列サイズを推定
            var count = 1;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == ' ')
                    count++;
            }

            var result = new string[count];
            var index = 0;
            var start = 0;

            for (int i = 0; i <= span.Length; i++)
            {
                if (i == span.Length || span[i] == ' ')
                {
                    var token = span.Slice(start, i - start);
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
