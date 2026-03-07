using System;

namespace DotNetG2P.English.Homograph
{
    /// <summary>
    /// 同綴異音語解決ファサード。
    /// PosGuesserとHomographDatabaseを組み合わせて、文脈に基づき最適な発音バリアントを選択する。
    /// </summary>
    internal static class HomographResolver
    {
        /// <summary>
        /// 単語列中の指定位置の単語について、最適な発音バリアントインデックスを返す。
        /// 同綴異音語でない場合や判定できない場合は 0（デフォルト）を返す。
        /// </summary>
        /// <param name="words">単語列（トークン化済み）</param>
        /// <param name="index">対象単語のインデックス</param>
        /// <returns>CMU辞書バリアントインデックス（0-based）</returns>
        public static int ResolveVariantIndex(string[] words, int index)
        {
            // 引数チェック
            if (words == null || index < 0 || index >= words.Length)
                return 0;

            var word = words[index];

            // HomographDatabaseで同綴異音語か確認
            if (!HomographDatabase.TryGetEntry(word, out var entry))
                return 0;

            // PosGuesserでPOSを推定
            var pos = PosGuesser.Guess(words, index);

            // Phase 2: 冠詞+X+名詞パターンの補正
            // PosGuesserがNounと判定し、エントリにAdjectiveルールがあり、
            // 後続に単語がある場合 → Adjective に変更
            // （例: "the close friend" → close は形容詞）
            if (pos == PosTag.Noun && entry.HasAdjectiveRule && index + 1 < words.Length)
            {
                pos = PosTag.Adjective;
            }

            // 文脈ルール + POSルールでバリアントインデックスを取得
            return entry.GetVariantIndex(pos, words, index);
        }
    }
}
