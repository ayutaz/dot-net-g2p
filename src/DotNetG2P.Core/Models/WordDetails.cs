using System;

namespace DotNetG2P.Models
{
    /// <summary>
    /// 形態素の詳細情報。品詞、活用、読み、発音等を保持する。
    /// jpreprocess の WordDetails 構造体に準拠。
    /// </summary>
    public sealed class WordDetails
    {
        /// <summary>品詞情報</summary>
        public POS PartOfSpeech { get; }

        /// <summary>活用型（"*"=該当なし）</summary>
        public string ConjugationType { get; }

        /// <summary>活用形</summary>
        public string ConjugationForm { get; }

        /// <summary>原形</summary>
        public string OriginalForm { get; }

        /// <summary>読み（カタカナ）</summary>
        public string Reading { get; }

        /// <summary>発音情報（モーラ列+アクセント核位置）。発音が不明な場合は null。</summary>
        public Pronunciation? Pronunciation { get; }

        public WordDetails(POS partOfSpeech, string conjugationType, string conjugationForm,
            string originalForm, string reading, Pronunciation? pronunciation = null)
        {
            PartOfSpeech = partOfSpeech;
            ConjugationType = conjugationType ?? "*";
            ConjugationForm = conjugationForm ?? "*";
            OriginalForm = originalForm ?? "*";
            Reading = reading ?? "*";
            Pronunciation = pronunciation;
        }

        /// <summary>
        /// ITokenからWordDetailsを構築する。
        /// naist-jdic辞書のフィールドを解析し、構造化されたWordDetailsに変換する。
        /// </summary>
        public static WordDetails FromToken(DotNetG2P.IToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            var pos = POS.FromFeatures(token.POS, token.POSGroup1, token.POSGroup2, token.POSGroup3);

            // 発音の構築（カタカナ発音文字列 + アクセント情報から）
            Pronunciation? pronunciation = null;
            string pronStr = token.Pronunciation;
            if (!string.IsNullOrEmpty(pronStr) && pronStr != "*")
            {
                // アクセント核位置を取得（デフォルト0 = 平板型）
                int accentPosition = 0;
                string accentInfo = token.AccentInfo;
                if (!string.IsNullOrEmpty(accentInfo) && accentInfo != "*")
                {
                    // "核位置/モーラ数" 形式（例: "3/4"）
                    int slashIndex = accentInfo.IndexOf('/');
                    if (slashIndex > 0)
                    {
                        if (int.TryParse(accentInfo.Substring(0, slashIndex), out int acc))
                        {
                            accentPosition = acc;
                        }
                    }
                }

                try
                {
                    pronunciation = Pronunciation.FromKatakana(pronStr, accentPosition);
                }
                catch (ArgumentException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Pronunciation parse failed: {ex.Message}");
                    // 発音フィールドのパース失敗 → Readingフィールドからリトライ
                    string readingStr = token.Reading;
                    if (!string.IsNullOrEmpty(readingStr) && readingStr != "*" && readingStr != pronStr)
                    {
                        try
                        {
                            pronunciation = Pronunciation.FromKatakana(readingStr, accentPosition);
                        }
                        catch (ArgumentException)
                        {
                            // Readingからもパース失敗 → pronunciation = null のまま
                        }
                    }
                }
            }

            return new WordDetails(
                pos,
                token.ConjugationType,
                token.ConjugationForm,
                token.OriginalForm,
                token.Reading,
                pronunciation
            );
        }
    }
}
