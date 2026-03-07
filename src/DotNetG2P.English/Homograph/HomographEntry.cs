using System;
using System.Runtime.CompilerServices;

namespace DotNetG2P.English.Homograph
{
    /// <summary>
    /// 文脈ベースの発音バリアント選択ルール。
    /// 前方の単語や文中のキーワードをチェックして発音バリアントを決定する。
    /// </summary>
    internal readonly struct ContextRule
    {
        /// <summary>マッチした場合に選択するバリアントインデックス</summary>
        public int VariantIndex { get; }

        /// <summary>前方の単語をチェック（前方MaxDistance単語以内にいずれかが含まれるか）</summary>
        public string[]? PrecedingWords { get; }

        /// <summary>文中の任意の位置に含まれるキーワード</summary>
        public string[]? ContainsAny { get; }

        /// <summary>前方チェックの最大距離（デフォルト3）</summary>
        public int MaxDistance { get; }

        /// <summary>後続の単語をチェック（対象語の直後にいずれかが含まれるか）</summary>
        public string[]? FollowingWords { get; }

        public ContextRule(int variantIndex, string[]? precedingWords = null, string[]? containsAny = null, int maxDistance = 3, string[]? followingWords = null)
        {
            VariantIndex = variantIndex;
            PrecedingWords = precedingWords;
            ContainsAny = containsAny;
            MaxDistance = maxDistance;
            FollowingWords = followingWords;
        }

        /// <summary>
        /// 指定された単語列と位置に対してルールがマッチするかを判定する。
        /// </summary>
        /// <param name="words">単語列</param>
        /// <param name="index">対象単語のインデックス</param>
        /// <returns>マッチした場合true</returns>
        public bool Matches(string[] words, int index)
        {
            // PrecedingWords チェック: 前方MaxDistance単語以内にいずれかが含まれるか
            if (PrecedingWords != null)
            {
                var start = Math.Max(0, index - MaxDistance);
                var found = false;
                for (int i = start; i < index; i++)
                {
                    for (int j = 0; j < PrecedingWords.Length; j++)
                    {
                        if (string.Equals(words[i], PrecedingWords[j], StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                }
                if (!found) return false;
            }

            // FollowingWords チェック: 対象語の直後にいずれかが含まれるか
            if (FollowingWords != null)
            {
                if (index + 1 >= words.Length) return false;
                var nextWord = words[index + 1];
                var found = false;
                for (int j = 0; j < FollowingWords.Length; j++)
                {
                    if (string.Equals(nextWord, FollowingWords[j], StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }

            // ContainsAny チェック: 文中の任意の位置にいずれかが含まれるか
            if (ContainsAny != null)
            {
                var found = false;
                for (int i = 0; i < words.Length; i++)
                {
                    if (i == index) continue; // 対象語自身はスキップ
                    for (int j = 0; j < ContainsAny.Length; j++)
                    {
                        if (string.Equals(words[i], ContainsAny[j], StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                }
                if (!found) return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 品詞→発音バリアントのマッピングルール。
    /// </summary>
    internal readonly struct HomographRule
    {
        /// <summary>対象品詞</summary>
        public PosTag Pos { get; }

        /// <summary>この品詞の場合に選択するCMU辞書バリアントインデックス</summary>
        public int VariantIndex { get; }

        public HomographRule(PosTag pos, int variantIndex)
        {
            Pos = pos;
            VariantIndex = variantIndex;
        }
    }

    /// <summary>
    /// 同綴異音語エントリ。単語ごとの品詞→発音バリアントマッピングを保持する。
    /// </summary>
    internal sealed class HomographEntry
    {
        /// <summary>対象単語（大文字）</summary>
        public string Word { get; }

        /// <summary>品詞が判定できない場合のデフォルトバリアントインデックス</summary>
        public int DefaultVariantIndex { get; }

        /// <summary>品詞→バリアントのルール配列</summary>
        public HomographRule[] Rules { get; }

        /// <summary>文脈ルール配列（nullの場合は文脈ルールなし）</summary>
        public ContextRule[]? ContextRules { get; }

        /// <summary>Rulesの中にAdjective品詞のルールが含まれるかどうか</summary>
        public bool HasAdjectiveRule { get; }

        public HomographEntry(string word, int defaultVariantIndex, params HomographRule[] rules)
            : this(word, defaultVariantIndex, null, rules)
        {
        }

        public HomographEntry(string word, int defaultVariantIndex, ContextRule[]? contextRules, params HomographRule[] rules)
        {
            Word = word;
            DefaultVariantIndex = defaultVariantIndex;
            Rules = rules;
            ContextRules = contextRules;

            // Adjectiveルールの存在を事前計算
            HasAdjectiveRule = false;
            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i].Pos == PosTag.Adjective)
                {
                    HasAdjectiveRule = true;
                    break;
                }
            }
        }

        /// <summary>
        /// 指定品詞に対応するバリアントインデックスを返す。
        /// マッチするルールがなければデフォルトを返す。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetVariantIndex(PosTag pos)
        {
            for (int i = 0; i < Rules.Length; i++)
            {
                if (Rules[i].Pos == pos)
                    return Rules[i].VariantIndex;
            }
            return DefaultVariantIndex;
        }

        /// <summary>
        /// 文脈ルールとPOSルールを組み合わせてバリアントインデックスを返す。
        /// 文脈ルールが先にチェックされ、マッチすればそのバリアントを返す。
        /// </summary>
        /// <param name="pos">推定された品詞</param>
        /// <param name="words">単語列</param>
        /// <param name="index">対象単語のインデックス</param>
        /// <returns>バリアントインデックス</returns>
        public int GetVariantIndex(PosTag pos, string[] words, int index)
        {
            // 1. 文脈ルールを先にチェック
            if (ContextRules != null)
            {
                for (int i = 0; i < ContextRules.Length; i++)
                {
                    if (ContextRules[i].Matches(words, index))
                        return ContextRules[i].VariantIndex;
                }
            }

            // 2. 既存のPOSルール
            return GetVariantIndex(pos);
        }
    }
}
