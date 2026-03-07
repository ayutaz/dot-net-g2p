using System;
using System.Collections.Generic;

namespace DotNetG2P.English.Homograph
{
    /// <summary>
    /// CMU辞書で複数発音バリアントを持つ同綴異音語のデータベース。
    /// 品詞に応じて適切なバリアントインデックスを返す。
    /// </summary>
    internal static class HomographDatabase
    {
        private static readonly Dictionary<string, HomographEntry> _entries;

        static HomographDatabase()
        {
            _entries = new Dictionary<string, HomographEntry>(80, StringComparer.OrdinalIgnoreCase);

            // ================================================================
            // カテゴリ1: 母音変化型
            // ================================================================

            // abuse: [0]=AH0 B Y UW1 S (名詞), [1]=AH0 B Y UW1 Z (動詞)
            Add("abuse", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // bass: [0]=B AE1 S (魚), [1]=B EY1 S (低音/音楽)
            // デフォルトは低音（より一般的）
            Add("bass", 1,
                new HomographRule(PosTag.Adjective, 1));

            // bow: [0]=B AW1 (お辞儀/動詞), [1]=B OW1 (弓/名詞)
            // 文脈ルール: "take"が前方3単語以内 → お辞儀(0), 後続が"down" → お辞儀(0)
            AddWithContext("bow", 0,
                new ContextRule[]
                {
                    new ContextRule(0, precedingWords: new[] { "take", "takes", "took", "taken", "taking" }),
                    new ContextRule(0, followingWords: new[] { "down" }),
                },
                new HomographRule(PosTag.Verb, 0),
                new HomographRule(PosTag.Noun, 1));

            // close: [0]=K L OW1 S (形容詞:近い), [1]=K L OW1 Z (動詞:閉じる)
            Add("close", 1,
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Adverb, 0),
                new HomographRule(PosTag.Verb, 1),
                new HomographRule(PosTag.Noun, 1));

            // excuse: [0]=IH0 K S K Y UW1 S (名詞), [1]=IH0 K S K Y UW1 Z (動詞)
            Add("excuse", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // lead: [0]=L EH1 D (名詞:鉛), [1]=L IY1 D (動詞:導く)
            Add("lead", 1,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // live: [0]=L AY1 V (形容詞:生の), [1]=L IH1 V (動詞:生きる)
            Add("live", 1,
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Adverb, 0),
                new HomographRule(PosTag.Verb, 1));

            // minute: [0]=M IH1 N AH0 T (名詞:分), [1]=M AY0 N UW1 T (形容詞:微小)
            Add("minute", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Adjective, 1));

            // read: [0]=R EH1 D (過去形), [1]=R IY1 D (現在形)
            // デフォルトは現在形（より一般的な文脈）
            // 文脈ルール: have/has/had/havingが前方3単語以内 → 過去分詞(0)
            //            yesterday/ago/already/last/earlier/previouslyが文中に含まれる → 過去形(0)
            AddWithContext("read", 1,
                new ContextRule[]
                {
                    new ContextRule(0, precedingWords: new[] { "have", "has", "had", "having" }),
                    new ContextRule(0, containsAny: new[] { "yesterday", "ago", "already", "last", "earlier", "previously" }),
                },
                new HomographRule(PosTag.Verb, 1),
                new HomographRule(PosTag.Noun, 1));

            // resume: [0]=R IH0 Z UW1 M (動詞:再開), [2]=R EH1 Z AH0 M EY2 (名詞:履歴書)
            Add("resume", 0,
                new HomographRule(PosTag.Verb, 0),
                new HomographRule(PosTag.Noun, 2));

            // tear: [0]=T EH1 R (動詞:裂く), [1]=T IH1 R (名詞:涙)
            Add("tear", 0,
                new HomographRule(PosTag.Verb, 0),
                new HomographRule(PosTag.Noun, 1));

            // use: [0]=Y UW1 S (名詞), [1]=Y UW1 Z (動詞)
            Add("use", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // wind: [0]=W AY1 N D (動詞:巻く), [1]=W IH1 N D (名詞:風)
            Add("wind", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // wound: [0]=W AW1 N D (windの過去形), [1]=W UW1 N D (名詞:傷)
            Add("wound", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // dove: [0]=D AH1 V (diveの過去形), [1]=D OW1 V (鳩)
            Add("dove", 1,
                new HomographRule(PosTag.Verb, 0),
                new HomographRule(PosTag.Noun, 1));

            // ================================================================
            // カテゴリ2: ストレスシフト型 (名詞=第1音節、動詞=第2音節)
            // ================================================================

            // abstract: [0]=AE0 B S T R AE1 K T (動詞), [1]=AE1 B S T R AE2 K T (名詞/形容詞)
            Add("abstract", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Adjective, 1),
                new HomographRule(PosTag.Verb, 0));

            // accent: [0]=AH0 K S EH1 N T (動詞), [1]=AE1 K S EH2 N T (名詞)
            Add("accent", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // address: [0]=AE1 D R EH2 S (名詞), [1]=AH0 D R EH1 S (動詞)
            Add("address", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // compact: [0]=K AA1 M P AE0 K T (名詞), [1]=K AH0 M P AE1 K T (動詞/形容詞)
            Add("compact", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Adjective, 1),
                new HomographRule(PosTag.Verb, 1));

            // compound: [0]=K AA1 M P AW0 N D (名詞), [1]=K AH0 M P AW1 N D (動詞)
            Add("compound", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Verb, 1));

            // conduct: [0]=K AA1 N D AH0 K T (名詞), [1]=K AA0 N D AH1 K T (動詞)
            Add("conduct", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // conflict: [0]=K AA1 N F L IH0 K T (名詞), [1]=K AH0 N F L IH1 K T (動詞)
            Add("conflict", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // console: [0]=K AA1 N S OW0 L (名詞), [1]=K AH0 N S OW1 L (動詞)
            Add("console", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // content: [0]=K AA1 N T EH0 N T (名詞), [1]=K AH0 N T EH1 N T (形容詞/動詞)
            Add("content", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Adjective, 1),
                new HomographRule(PosTag.Verb, 1));

            // contest: [0]=K AA1 N T EH0 S T (名詞), [1]=K AH0 N T EH1 S T (動詞)
            Add("contest", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // contract: [0]=K AA1 N T R AE2 K T (名詞), [1]=K AH0 N T R AE1 K T (動詞)
            Add("contract", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // convert: [0]=K AA1 N V ER0 T (名詞), [1]=K AH0 N V ER1 T (動詞)
            Add("convert", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // convict: [0]=K AA1 N V IH0 K T (名詞), [1]=K AH0 N V IH1 K T (動詞)
            Add("convict", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // decrease: [0]=D IH0 K R IY1 S (動詞), [1]=D IY1 K R IY2 S (名詞)
            Add("decrease", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // defect: [0]=D IY1 F EH0 K T (名詞), [1]=D IH0 F EH1 K T (動詞)
            Add("defect", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // desert: [0]=D EH1 Z ER0 T (名詞), [1]=D IH0 Z ER1 T (動詞)
            Add("desert", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // digest: [0]=D AY0 JH EH1 S T (動詞), [1]=D AY1 JH EH0 S T (名詞)
            Add("digest", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // extract: [0]=EH1 K S T R AE2 K T (名詞), [1]=IH0 K S T R AE1 K T (動詞)
            Add("extract", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // impact: [0]=IH2 M P AE1 K T (動詞), [1]=IH1 M P AE0 K T (名詞)
            Add("impact", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // increase: [0]=IH2 N K R IY1 S (動詞), [1]=IH1 N K R IY2 S (名詞)
            Add("increase", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // insert: [0]=IH2 N S ER1 T (動詞), [1]=IH1 N S ER2 T (名詞)
            Add("insert", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // object: [0]=AA1 B JH EH0 K T (名詞), [1]=AH0 B JH EH1 K T (動詞)
            Add("object", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // permit: [0]=P ER0 M IH1 T (動詞), [1]=P ER1 M IH2 T (名詞)
            Add("permit", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // present: [0]=P R EH1 Z AH0 N T (名詞/形容詞), [1]=P R IY0 Z EH1 N T (動詞)
            Add("present", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Verb, 1));

            // produce: [0]=P R AH0 D UW1 S (動詞), [1]=P R OW1 D UW0 S (名詞)
            Add("produce", 0,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // progress: [0]=P R AA1 G R EH2 S (名詞), [1]=P R AH0 G R EH1 S (動詞)
            Add("progress", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // project: [0]=P R AA1 JH EH0 K T (名詞), [1]=P R AH0 JH EH1 K T (動詞)
            Add("project", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // protest: [0]=P R OW1 T EH2 S T (名詞), [1]=P R AH0 T EH1 S T (動詞)
            Add("protest", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // rebel: [0]=R EH1 B AH0 L (名詞), [1]=R IH0 B EH1 L (動詞)
            Add("rebel", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Verb, 1));

            // record: [0]=R AH0 K AO1 R D (動詞), [1]=R EH1 K ER0 D (名詞)
            Add("record", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // refuse: [0]=R AH0 F Y UW1 Z (動詞), [1]=R EH1 F Y UW2 Z (名詞)
            Add("refuse", 0,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // subject: [0]=S AH0 B JH EH1 K T (動詞), [1]=S AH1 B JH IH0 K T (名詞)
            Add("subject", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // survey: [0]=S ER0 V EY1 (動詞), [1]=S ER1 V EY2 (名詞)
            Add("survey", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // suspect: [0]=S AH0 S P EH1 K T (動詞), [1]=S AH1 S P EH2 K T (名詞/形容詞)
            Add("suspect", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Adjective, 1),
                new HomographRule(PosTag.Verb, 0));

            // transport: [0]=T R AE0 N S P AO1 R T (動詞), [1]=T R AE1 N S P AO0 R T (名詞)
            Add("transport", 1,
                new HomographRule(PosTag.Noun, 1),
                new HomographRule(PosTag.Verb, 0));

            // ================================================================
            // カテゴリ3: -ate 語尾変化型 (動詞=-EY T、形容詞/名詞=-AH T)
            // ================================================================

            // aggregate: [0]=AE1 G R AH0 G AH0 T (名詞/形容詞), [2]=AE1 G R AH0 G EY0 T (動詞)
            Add("aggregate", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Verb, 2));

            // alternate: [0]=AO1 L T ER0 N AH0 T (名詞/形容詞), [1]=AO1 L T ER0 N EY2 T (動詞)
            Add("alternate", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Verb, 1));

            // approximate: [0]=AH0 P R AA1 K S AH0 M AH0 T (形容詞), [1]=AH0 P R AA1 K S AH0 M EY2 T (動詞)
            Add("approximate", 0,
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Verb, 1));

            // associate: [0]=AH0 S OW1 S IY0 AH0 T (名詞/形容詞), [1]=AH0 S OW1 S IY0 EY2 T (動詞)
            Add("associate", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Verb, 1));

            // deliberate: [0]=D IH0 L IH1 B ER0 AH0 T (形容詞), [1]=D IH0 L IH1 B ER0 EY2 T (動詞)
            Add("deliberate", 0,
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Verb, 1));

            // duplicate: [0]=D UW1 P L AH0 K AH0 T (名詞/形容詞), [1]=D UW1 P L AH0 K EY2 T (動詞)
            Add("duplicate", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Verb, 1));

            // elaborate: [0]=IH0 L AE1 B R AH0 T (形容詞), [1]=IH0 L AE1 B ER0 EY2 T (動詞)
            Add("elaborate", 0,
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Verb, 1));

            // estimate: [0]=EH1 S T AH0 M AH0 T (名詞), [1]=EH1 S T AH0 M EY2 T (動詞)
            Add("estimate", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // graduate: [0]=G R AE1 JH AH0 W AH0 T (名詞), [1]=G R AE1 JH AH0 W EY2 T (動詞)
            Add("graduate", 0,
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Verb, 1));

            // intimate: [0]=IH1 N T AH0 M AH0 T (形容詞/名詞), [1]=IH1 N T AH0 M EY2 T (動詞)
            Add("intimate", 0,
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // moderate: [0]=M AA1 D ER0 AH0 T (形容詞/名詞), [1]=M AA1 D ER0 EY2 T (動詞)
            Add("moderate", 0,
                new HomographRule(PosTag.Adjective, 0),
                new HomographRule(PosTag.Noun, 0),
                new HomographRule(PosTag.Verb, 1));

            // separate: [0]=S EH1 P ER0 EY2 T (動詞), [2]=S EH1 P R AH0 T (形容詞)
            Add("separate", 0,
                new HomographRule(PosTag.Verb, 0),
                new HomographRule(PosTag.Adjective, 2));
        }

        /// <summary>
        /// 指定された単語の同綴異音語エントリを取得する。
        /// </summary>
        /// <param name="word">検索する単語</param>
        /// <param name="entry">見つかったエントリ</param>
        /// <returns>エントリが見つかった場合はtrue</returns>
        public static bool TryGetEntry(string word, out HomographEntry entry)
        {
            if (string.IsNullOrEmpty(word))
            {
                entry = null!;
                return false;
            }
            return _entries.TryGetValue(word, out entry!);
        }

        /// <summary>登録済みエントリ数を返す（テスト用）。</summary>
        internal static int Count => _entries.Count;

        private static void Add(string word, int defaultVariant, params HomographRule[] rules)
        {
            _entries[word.ToUpperInvariant()] = new HomographEntry(word, defaultVariant, rules);
        }

        private static void AddWithContext(string word, int defaultVariant, ContextRule[] contextRules, params HomographRule[] rules)
        {
            _entries[word.ToUpperInvariant()] = new HomographEntry(word, defaultVariant, contextRules, rules);
        }
    }
}
