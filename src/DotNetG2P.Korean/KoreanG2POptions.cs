using System;

namespace DotNetG2P.Korean
{
    /// <summary>
    /// 韓国語 G2P エンジンの処理オプション。
    /// </summary>
    public sealed class KoreanG2POptions
    {
        /// <summary>デフォルトオプション。</summary>
        public static readonly KoreanG2POptions Default = new KoreanG2POptions();

        /// <summary>音素列出力の区切り文字。</summary>
        public string Separator { get; }

        /// <summary>Jamo 出力の音節区切り文字。</summary>
        public string SyllableSeparator { get; }

        /// <summary>Unicode 正規化を有効化するか。</summary>
        public bool EnableUnicodeNormalization { get; }

        /// <summary>軽量テキスト正規化を有効化するか。</summary>
        public bool EnableTextNormalization { get; }

        /// <summary>例外辞書を有効化するか。</summary>
        public bool EnableExceptionDictionary { get; }

        /// <summary>非 Hangul 文字をそのまま出力へ残すか。</summary>
        public bool PreserveNonHangul { get; }

        /// <summary>`의` 系変異の返却方針。</summary>
        public KoreanUiVariationMode UiVariationMode { get; }

        /// <summary>
        /// オプションを初期化する。
        /// </summary>
        public KoreanG2POptions(
            string separator = " ",
            string syllableSeparator = " ",
            bool enableUnicodeNormalization = true,
            bool preserveNonHangul = true,
            bool enableTextNormalization = true,
            bool enableExceptionDictionary = true,
            KoreanUiVariationMode uiVariationMode = KoreanUiVariationMode.Standard)
        {
            Separator = separator ?? throw new ArgumentNullException(nameof(separator));
            SyllableSeparator = syllableSeparator ?? throw new ArgumentNullException(nameof(syllableSeparator));
            EnableUnicodeNormalization = enableUnicodeNormalization;
            PreserveNonHangul = preserveNonHangul;
            EnableTextNormalization = enableTextNormalization;
            EnableExceptionDictionary = enableExceptionDictionary;
            UiVariationMode = uiVariationMode;
        }
    }
}
