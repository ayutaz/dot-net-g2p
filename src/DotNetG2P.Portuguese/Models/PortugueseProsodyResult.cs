using System;

namespace DotNetG2P.Portuguese
{
    /// <summary>
    /// IPA 音素配列と韻律情報を組み合わせたポルトガル語韻律変換結果。
    /// </summary>
    public sealed class PortugueseProsodyResult
    {
        /// <summary>IPA 音素配列。</summary>
        public string[] Phonemes { get; }

        /// <summary>各音素に対応する韻律情報。Phonemes と同じ長さ。</summary>
        public PortugueseProsodyInfo[] Prosody { get; }

        /// <summary>
        /// 韻律結果を初期化する。
        /// </summary>
        /// <param name="phonemes">IPA 音素配列。</param>
        /// <param name="prosody">韻律情報配列。<paramref name="phonemes"/> と同じ長さでなければならない。</param>
        /// <exception cref="ArgumentNullException"><paramref name="phonemes"/> または <paramref name="prosody"/> が null の場合。</exception>
        /// <exception cref="ArgumentException">配列の長さが一致しない場合。</exception>
        public PortugueseProsodyResult(string[] phonemes, PortugueseProsodyInfo[] prosody)
        {
            Phonemes = phonemes ?? throw new ArgumentNullException(nameof(phonemes));
            Prosody = prosody ?? throw new ArgumentNullException(nameof(prosody));

            if (phonemes.Length != prosody.Length)
                throw new ArgumentException(
                    $"phonemes の長さ ({phonemes.Length}) と prosody の長さ ({prosody.Length}) が一致しません。",
                    nameof(prosody));
        }
    }
}
