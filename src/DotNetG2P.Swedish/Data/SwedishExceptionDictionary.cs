using System;
using System.Collections.Generic;
using System.IO;

namespace DotNetG2P.Swedish.Data
{
    /// <summary>
    /// スウェーデン語例外辞書。埋め込みTSVリソースから不規則語・外来語・地名等の発音を提供する。
    /// </summary>
    internal static class SwedishExceptionDictionary
    {
        private const byte AnyDialectKey = byte.MaxValue;
        private static readonly Dictionary<string, Dictionary<byte, SwedishPronunciation>> s_entries = LoadEntries();

        /// <summary>
        /// 例外辞書から単語の発音を検索する。方言固有エントリが優先され、見つからなければ共通エントリを返す。
        /// </summary>
        public static bool TryLookup(string word, SwedishDialect dialect, out SwedishPronunciation pronunciation)
        {
            pronunciation = null!;
            if (word == null || !s_entries.TryGetValue(word, out var byDialect))
                return false;

            // 方言固有エントリを優先
            if (byDialect.TryGetValue((byte)dialect, out pronunciation))
                return true;

            // 共通エントリにフォールバック
            return byDialect.TryGetValue(AnyDialectKey, out pronunciation);
        }

        private static Dictionary<string, Dictionary<byte, SwedishPronunciation>> LoadEntries()
        {
            var entries = new Dictionary<string, Dictionary<byte, SwedishPronunciation>>(StringComparer.Ordinal);

            var assembly = typeof(SwedishExceptionDictionary).Assembly;
            using var stream = assembly.GetManifestResourceStream("DotNetG2P.Swedish.Data.swedish_exceptions.master.tsv");
            if (stream == null) return entries;
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                try
                {
                    line = line.Trim();
                    if (line.Length == 0 || line[0] == '#' || line.StartsWith("surface\t", StringComparison.Ordinal))
                        continue;

                    var parts = line.Split('\t');
                    // surface, dialect, category, accent, stress_index, phonemes の最低6フィールド必要
                    if (parts.Length < 6)
                        continue;

                    var w = parts[0];
                    if (!TryParseDialect(parts[1], out var dialectKey)
                        || !int.TryParse(parts[4], out var stressIndex))
                    {
                        continue;
                    }

                    byte accent = 0;
                    if (parts.Length > 3 && byte.TryParse(parts[3], out var accentVal) && accentVal <= 2)
                        accent = accentVal;

                    var pron = ParsePronunciation(parts[5], stressIndex, accent);
                    if (!entries.TryGetValue(w, out var byDialect))
                    {
                        byDialect = new Dictionary<byte, SwedishPronunciation>();
                        entries[w] = byDialect;
                    }

                    byDialect[dialectKey] = pron;
                }
                catch
                {
                    // 不正行はスキップして次の行を読み続ける
                    continue;
                }
            }

            return entries;
        }

        private static bool TryParseDialect(string token, out byte dialect)
        {
            switch (token)
            {
                case "*":
                    dialect = AnyDialectKey;
                    return true;
                case "central":
                    dialect = (byte)SwedishDialect.Central;
                    return true;
                case "finland":
                    dialect = (byte)SwedishDialect.FinlandSwedish;
                    return true;
                default:
                    dialect = AnyDialectKey;
                    return false;
            }
        }

        /// <summary>
        /// 音素文字列をパースして SwedishPronunciation を構築する。
        /// <c>|</c> で音節境界を表し、スペースで個々の音素トークンを区切る。
        /// </summary>
        private static SwedishPronunciation ParsePronunciation(string value, int stressIndex, byte accent = 0)
        {
            var syllableSpecs = value.Split('|');
            var syllableOffsets = new int[syllableSpecs.Length];
            var phonemes = new List<SwedishPhoneme>(8);

            for (var i = 0; i < syllableSpecs.Length; i++)
            {
                syllableOffsets[i] = phonemes.Count;
                var tokens = syllableSpecs[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                bool nucleusSet = false;
                foreach (var token in tokens)
                {
                    var ipa = ParsePhoneme(token);
                    bool isNucleus = false;
                    if (!nucleusSet && IsVowelPhoneme(ipa))
                    {
                        isNucleus = true;
                        nucleusSet = true;
                    }

                    bool isStressed = (stressIndex >= 0 && i == stressIndex);
                    phonemes.Add(new SwedishPhoneme(ipa, isStressed, isNucleus));
                }
            }

            return new SwedishPronunciation(phonemes.ToArray(), syllableOffsets, stressIndex, accent);
        }

        /// <summary>母音かどうか判定する（SwedishIpaPhoneme.Schwa以下が母音）。</summary>
        private static bool IsVowelPhoneme(SwedishIpaPhoneme phoneme)
        {
            return phoneme <= SwedishIpaPhoneme.Schwa;
        }

        /// <summary>
        /// IPAトークン文字列を SwedishIpaPhoneme 列挙値にマッピングする。
        /// IPA正式Unicode文字とASCIIフォールバックの両方をサポートする。
        /// </summary>
        private static SwedishIpaPhoneme ParsePhoneme(string token)
        {
            switch (token)
            {
                // === 長母音 ===
                case "i\u02D0": return SwedishIpaPhoneme.LongI;            // iː
                case "y\u02D0": return SwedishIpaPhoneme.LongY;            // yː
                case "\u0289\u02D0": return SwedishIpaPhoneme.LongUCentral; // ʉː
                case "u\u02D0": return SwedishIpaPhoneme.LongU;            // uː
                case "e\u02D0": return SwedishIpaPhoneme.LongE;            // eː
                case "\u00F8\u02D0": return SwedishIpaPhoneme.LongOe;      // øː
                case "\u025B\u02D0": return SwedishIpaPhoneme.LongEh;      // ɛː
                case "o\u02D0": return SwedishIpaPhoneme.LongO;            // oː
                case "\u0251\u02D0": return SwedishIpaPhoneme.LongA;       // ɑː
                // 長母音ASCIIフォールバック（コロン表記）
                case "i:": return SwedishIpaPhoneme.LongI;
                case "y:": return SwedishIpaPhoneme.LongY;
                case "\u0289:": return SwedishIpaPhoneme.LongUCentral;     // ʉ:
                case "u:": return SwedishIpaPhoneme.LongU;
                case "e:": return SwedishIpaPhoneme.LongE;
                case "\u00F8:": return SwedishIpaPhoneme.LongOe;           // ø:
                case "\u025B:": return SwedishIpaPhoneme.LongEh;           // ɛ:
                case "o:": return SwedishIpaPhoneme.LongO;
                case "\u0251:": return SwedishIpaPhoneme.LongA;            // ɑ:
                // 長母音（分離表記: 母音 + ː が別トークンで来る場合のため、
                // TSVでは "ɑː" 等と1トークンで記述する方針なので上記で処理される）

                // === 短母音 ===
                case "\u026A": return SwedishIpaPhoneme.ShortI;            // ɪ
                case "\u028F": return SwedishIpaPhoneme.ShortY;            // ʏ
                case "\u0275": return SwedishIpaPhoneme.ShortUCentral;     // ɵ
                case "\u028A": return SwedishIpaPhoneme.ShortU;            // ʊ
                case "\u025B": return SwedishIpaPhoneme.ShortE;            // ɛ (長母音版は ɛː で先にマッチ)
                case "\u0153": return SwedishIpaPhoneme.ShortOe;           // œ
                case "\u0254": return SwedishIpaPhoneme.ShortO;            // ɔ
                case "a": return SwedishIpaPhoneme.ShortA;                 // a
                case "\u0259": return SwedishIpaPhoneme.Schwa;             // ə

                // 長母音の追加表記（ː = U+02D0 以外の表記対応）
                case "\u0254\u02D0": return SwedishIpaPhoneme.LongO;       // ɔː → oː として扱う
                case "\u0254:": return SwedishIpaPhoneme.LongO;            // ɔ: → oː として扱う（方言差のため）

                // === 破裂音 ===
                case "p": return SwedishIpaPhoneme.P;
                case "b": return SwedishIpaPhoneme.B;
                case "t": return SwedishIpaPhoneme.T;
                case "d": return SwedishIpaPhoneme.D;
                case "k": return SwedishIpaPhoneme.K;
                case "\u0261": return SwedishIpaPhoneme.G;                 // ɡ (U+0261)
                case "g": return SwedishIpaPhoneme.G;                      // g (U+0067, ASCIIフォールバック)

                // === 摩擦音 ===
                case "f": return SwedishIpaPhoneme.F;
                case "v": return SwedishIpaPhoneme.V;
                case "s": return SwedishIpaPhoneme.S;
                case "h": return SwedishIpaPhoneme.H;
                case "\u0267": return SwedishIpaPhoneme.Sj;                // ɧ (sj音)
                case "\u0255": return SwedishIpaPhoneme.Tj;                // ɕ (tj音)

                // === 鼻音 ===
                case "m": return SwedishIpaPhoneme.M;
                case "n": return SwedishIpaPhoneme.N;
                case "\u014B": return SwedishIpaPhoneme.Ng;                // ŋ

                // === 接近音・ふるえ音 ===
                case "l": return SwedishIpaPhoneme.L;
                case "r": return SwedishIpaPhoneme.R;
                case "j": return SwedishIpaPhoneme.J;

                // === そり舌音 ===
                case "\u0288": return SwedishIpaPhoneme.RetroT;            // ʈ
                case "\u0256": return SwedishIpaPhoneme.RetroD;            // ɖ
                case "\u0273": return SwedishIpaPhoneme.RetroN;            // ɳ
                case "\u026D": return SwedishIpaPhoneme.RetroL;            // ɭ
                case "\u0282": return SwedishIpaPhoneme.RetroS;            // ʂ

                // === 破擦音 ===
                case "t\u0361\u0255": return SwedishIpaPhoneme.TjAffricate;  // t͡ɕ
                case "t\u0255": return SwedishIpaPhoneme.TjAffricate;        // tɕ (tie barなしフォールバック)

                default:
                    throw new InvalidOperationException(
                        "Unknown phoneme token in Swedish exception dictionary: " + token);
            }
        }
    }
}
