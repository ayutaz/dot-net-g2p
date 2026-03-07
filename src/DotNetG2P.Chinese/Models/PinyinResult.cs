using System;
using System.Collections.Generic;

namespace DotNetG2P.Chinese
{
    /// <summary>
    /// ピンイン変換結果。入力テキストの各文字に対応するピンインを保持する。
    /// </summary>
    public sealed class PinyinResult
    {
        private readonly string[] _pinyins;
        private readonly PinyinSyllable?[] _syllables;

        /// <summary>元のテキスト</summary>
        public string OriginalText { get; }

        /// <summary>各文字のピンイン（声調記号付き）</summary>
        public IReadOnlyList<string> Pinyins => _pinyins;

        /// <summary>各文字のPinyinSyllable（構造化表現）。非漢字はnull。</summary>
        public IReadOnlyList<PinyinSyllable?> Syllables => _syllables;

        /// <summary>
        /// PinyinResultを初期化する。
        /// </summary>
        /// <param name="originalText">元のテキスト</param>
        /// <param name="pinyins">各文字のピンイン（声調記号付き）</param>
        /// <param name="syllables">各文字のPinyinSyllable（非漢字はnull）</param>
        internal PinyinResult(string originalText, string[] pinyins, PinyinSyllable?[] syllables)
        {
            OriginalText = originalText ?? throw new ArgumentNullException(nameof(originalText));
            _pinyins = pinyins ?? throw new ArgumentNullException(nameof(pinyins));
            _syllables = syllables ?? throw new ArgumentNullException(nameof(syllables));
        }

        /// <summary>
        /// 指定スタイルでフォーマットされたピンイン文字列を返す。
        /// </summary>
        /// <param name="style">出力スタイル（ToneMarked/ToneNumber/Normal）</param>
        /// <param name="separator">区切り文字（デフォルト: スペース）</param>
        /// <returns>フォーマットされたピンイン文字列</returns>
        public string ToString(PinyinStyle style, string separator = " ")
        {
            if (_pinyins.Length == 0)
                return "";

            if (style == PinyinStyle.ToneMarked)
                return string.Join(separator, _pinyins);

            var parts = new string[_pinyins.Length];
            for (var i = 0; i < _pinyins.Length; i++)
            {
                var syllable = _syllables[i];
                if (syllable.HasValue)
                {
                    parts[i] = FormatSyllable(syllable.Value, style);
                }
                else
                {
                    // 非漢字（記号・英字等）はそのまま出力
                    parts[i] = _pinyins[i];
                }
            }

            return string.Join(separator, parts);
        }

        /// <summary>
        /// 声調記号付きピンイン文字列を返す（デフォルトスタイル）。
        /// </summary>
        /// <returns>スペース区切りの声調記号付きピンイン文字列</returns>
        public override string ToString()
        {
            return ToString(PinyinStyle.ToneMarked);
        }

        /// <summary>
        /// PinyinSyllableを指定スタイルでフォーマットする。
        /// </summary>
        private static string FormatSyllable(PinyinSyllable syllable, PinyinStyle style)
        {
            var ini = PinyinSyllable.InitialToString(syllable.Initial);
            var fin = PinyinSyllable.FinalToString(syllable.Initial, syllable.Final);

            switch (style)
            {
                case PinyinStyle.ToneNumber:
                    var tone = syllable.Tone == Tone.Neutral ? "" : ((int)syllable.Tone).ToString();
                    return ini + fin + tone;

                case PinyinStyle.Normal:
                    return ini + fin;

                default:
                    return ini + fin;
            }
        }
    }
}
