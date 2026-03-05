using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DotNetG2P.English
{
    /// <summary>
    /// CMU Pronouncing Dictionaryの読み込み・検索を行う。
    /// EmbeddedResourceまたは外部ファイルからcmudict.dict形式を読み込む。
    /// </summary>
    public sealed class CmuDictionary
    {
        private readonly Dictionary<string, EnglishPronunciation[]> _entries;

        /// <summary>辞書エントリ数（ユニークな単語数）</summary>
        public int Count => _entries.Count;

        private CmuDictionary(Dictionary<string, EnglishPronunciation[]> entries)
        {
            _entries = entries;
        }

        /// <summary>
        /// 埋め込みリソースからCMU辞書を読み込む。
        /// </summary>
        /// <returns>読み込まれたCMU辞書</returns>
        public static CmuDictionary LoadEmbedded()
        {
            var assembly = typeof(CmuDictionary).Assembly;
            using (var stream = assembly.GetManifestResourceStream("DotNetG2P.English.cmudict.dict"))
            {
                if (stream == null)
                    throw new InvalidOperationException("埋め込みCMU辞書リソースが見つかりません。");

                using (var reader = new StreamReader(stream))
                {
                    return ParseFromReader(reader);
                }
            }
        }

        /// <summary>
        /// 外部ファイルからCMU辞書を読み込む。
        /// </summary>
        /// <param name="path">辞書ファイルパス</param>
        /// <returns>読み込まれたCMU辞書</returns>
        public static CmuDictionary LoadFromFile(string path)
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
        /// 単語の発音を検索する。
        /// </summary>
        /// <param name="word">検索する単語（大文字小文字不問）</param>
        /// <param name="pronunciations">見つかった発音（1つ以上）</param>
        /// <returns>辞書に登録されている場合 true</returns>
        public bool TryLookup(string word, out EnglishPronunciation[] pronunciations)
        {
            if (word == null)
            {
                pronunciations = Array.Empty<EnglishPronunciation>();
                return false;
            }

            return _entries.TryGetValue(word.ToUpperInvariant(), out pronunciations!);
        }

        /// <summary>
        /// 単語が辞書に登録されているかを返す。
        /// </summary>
        /// <param name="word">検索する単語（大文字小文字不問）</param>
        /// <returns>登録されている場合 true</returns>
        public bool ContainsWord(string word)
        {
            if (word == null) return false;
            return _entries.ContainsKey(word.ToUpperInvariant());
        }

        /// <summary>
        /// StreamReaderからCMU辞書をパースする。
        /// </summary>
        private static CmuDictionary ParseFromReader(StreamReader reader)
        {
            // 一時的に List で蓄積し、後で配列に変換
            var tempDict = new Dictionary<string, List<EnglishPronunciation>>(150000, StringComparer.Ordinal);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                // 空行・コメント行をスキップ
                if (line.Length == 0 || line[0] == ';')
                    continue;

                // 行フォーマット: "word phoneme1 phoneme2 ..." または "word(2) phoneme1 phoneme2 ..."
                // '#' 以降はインラインコメント
                var commentIdx = line.IndexOf('#');
                if (commentIdx >= 0)
                {
                    line = line.Substring(0, commentIdx);
                }

                // 先頭の空白をトリムしてからsplit
                line = line.TrimEnd();
                if (line.Length == 0)
                    continue;

                // 最初のスペースで単語と音素列を分割
                var firstSpace = line.IndexOf(' ');
                if (firstSpace < 0)
                    continue;

                var rawWord = line.Substring(0, firstSpace);
                var phonemesPart = line.Substring(firstSpace + 1).TrimStart();

                if (phonemesPart.Length == 0)
                    continue;

                // "word(2)" → "word" に正規化（バリアント番号を除去）
                var baseWord = rawWord;
                var parenIdx = rawWord.IndexOf('(');
                if (parenIdx >= 0)
                {
                    baseWord = rawWord.Substring(0, parenIdx);
                }

                // 大文字に正規化
                baseWord = baseWord.ToUpperInvariant();

                // 音素トークンをパース
                var tokens = phonemesPart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var phonemes = new EnglishPhoneme[tokens.Length];
                var valid = true;
                for (var i = 0; i < tokens.Length; i++)
                {
                    if (!ArpabetParser.TryParse(tokens[i], out phonemes[i]))
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                    continue;

                var pronunciation = new EnglishPronunciation(phonemes);

                if (tempDict.TryGetValue(baseWord, out var list))
                {
                    list.Add(pronunciation);
                }
                else
                {
                    tempDict[baseWord] = new List<EnglishPronunciation> { pronunciation };
                }
            }

            // List → 配列に変換
            var entries = new Dictionary<string, EnglishPronunciation[]>(tempDict.Count, StringComparer.Ordinal);
            foreach (var kv in tempDict)
            {
                entries[kv.Key] = kv.Value.ToArray();
            }

            return new CmuDictionary(entries);
        }
    }
}
