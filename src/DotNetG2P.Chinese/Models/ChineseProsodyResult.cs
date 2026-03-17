using System;
using System.Collections.Generic;

namespace DotNetG2P.Chinese
{
    /// <summary>
    /// IPA音素配列と韻律情報配列を組み合わせた中国語韻律変換結果。
    /// </summary>
    public sealed class ChineseProsodyResult
    {
        /// <summary>各音節のIPA音素文字列配列。</summary>
        public IReadOnlyList<string> Phonemes { get; }

        /// <summary>各音節の韻律情報配列。Phonemesと同じ長さ。</summary>
        public IReadOnlyList<ChineseProsodyInfo> Prosody { get; }

        /// <summary>
        /// ChineseProsodyResultを初期化する。
        /// </summary>
        /// <param name="phonemes">IPA音素文字列配列</param>
        /// <param name="prosody">韻律情報配列</param>
        /// <exception cref="ArgumentNullException"><paramref name="phonemes"/>または<paramref name="prosody"/>がnullの場合。</exception>
        /// <exception cref="ArgumentException"><paramref name="phonemes"/>と<paramref name="prosody"/>の長さが一致しない場合。</exception>
        public ChineseProsodyResult(string[] phonemes, ChineseProsodyInfo[] prosody)
        {
            if (phonemes == null)
                throw new ArgumentNullException(nameof(phonemes));
            if (prosody == null)
                throw new ArgumentNullException(nameof(prosody));
            if (phonemes.Length != prosody.Length)
                throw new ArgumentException(
                    $"phonemesとprosodyの長さが一致しません（phonemes={phonemes.Length}, prosody={prosody.Length}）。",
                    nameof(prosody));

            Phonemes = phonemes;
            Prosody = prosody;
        }
    }
}
