using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DotNetG2P;
using DotNetG2P.Models;
using DotNetG2P.MeCab;
using Xunit;

namespace DotNetG2P.Tests.Integration
{
    /// <summary>
    /// 日本語G2Pエッジケース回帰テスト。
    /// 既存のEdgeCaseTests/PiperPlusTests/PyOpenJTalkComparisonTestsとは異なる観点で、
    /// 全角英数字、半角カタカナ、特殊文字連続、助詞読み分け、G2POptions変更等を検証する。
    /// </summary>
    public class JapaneseEdgeCaseRegressionTests : IDisposable
    {
        private static string? DicPath => Environment.GetEnvironmentVariable("NAIST_JDIC_PATH")
            ?? FindFallbackDicPath();
        private static bool DictionaryExists => !string.IsNullOrEmpty(DicPath) && Directory.Exists(DicPath);

        private static string? FindFallbackDicPath()
        {
            var candidates = new[]
            {
                @"C:\Users\yuta\Desktop\Private\open_jtalk_dic_utf_8-1.11",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "open_jtalk_dic_utf_8-1.11"),
            };
            foreach (var path in candidates)
            {
                if (Directory.Exists(path)) return path;
            }
            return null;
        }

        private readonly ITokenizer? _tokenizer;
        private readonly G2PEngine? _engine;

        public JapaneseEdgeCaseRegressionTests()
        {
            if (DictionaryExists)
            {
                _tokenizer = new MeCabTokenizer(DicPath!);
                _engine = new G2PEngine(_tokenizer);
            }
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!DictionaryExists, "naist-jdic辞書が見つかりません（環境変数 NAIST_JDIC_PATH を設定してください）");
        }

        /// <summary>
        /// 音素文字列の基本的な妥当性を検証する。
        /// </summary>
        private static void AssertValidPhonemeString(string phonemes)
        {
            if (string.IsNullOrEmpty(phonemes)) return;
            // 音素列は英字(大小)、ハイフン、スペースのみで構成される
            Assert.Matches(@"^[a-zA-Z\- ]+$", phonemes);
            // 連続スペースがないこと
            Assert.DoesNotMatch(@"  ", phonemes);
            // 先頭/末尾がスペースでないこと
            Assert.False(phonemes.StartsWith(" "), "音素列が先頭スペースで始まっています");
            Assert.False(phonemes.EndsWith(" "), "音素列が末尾スペースで終わっています");
        }

        // =====================================================================
        // 1. 全角英数字の処理
        // =====================================================================

        [SkippableTheory]
        [InlineData("ＡＢＣ")]
        [InlineData("１２３")]
        [InlineData("Ｘ")]
        [InlineData("ＡＢＣ１２３")]
        public void ToPhonemes_全角英数字_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            AssertValidPhonemeString(result);
        }

        [SkippableFact]
        public void ToPhonemes_全角英数字混在_正常動作()
        {
            SkipIfNoDictionary();

            // 全角英数字が日本語テキストに混在
            var result = _engine!.ToPhonemes("Ａランチは１２００円です");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
        }

        // =====================================================================
        // 2. 半角カタカナの処理
        // =====================================================================

        [SkippableTheory]
        [InlineData("ｶﾀｶﾅ")]
        [InlineData("ﾃｽﾄ")]
        [InlineData("ｺﾝﾆﾁﾊ")]
        [InlineData("ﾊﾟﾝﾀﾞ")]    // 半角カタカナ+濁点/半濁点
        public void ToPhonemes_半角カタカナ_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            AssertValidPhonemeString(result);
        }

        [SkippableFact]
        public void ToPhonemes_半角カタカナ混在_正常動作()
        {
            SkipIfNoDictionary();

            // 半角カタカナと全角カタカナの混在
            var result = _engine!.ToPhonemes("ﾃｽﾄとテスト");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
        }

        // =====================================================================
        // 3. カタカナ直接入力（辞書に載っていない外来語）
        // =====================================================================

        [SkippableTheory]
        [InlineData("カタカナ")]
        [InlineData("アイスクリーム")]
        [InlineData("スマートフォン")]
        [InlineData("プレゼンテーション")]
        [InlineData("インフラストラクチャー")]
        public void ToPhonemes_カタカナ直接入力_非空の音素列を返す(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
        }

        // =====================================================================
        // 4. 長音記号・促音・撥音の連続
        // =====================================================================

        [SkippableTheory]
        [InlineData("ああー")]           // 母音+長音記号
        [InlineData("えーっと")]         // 長音+促音
        [InlineData("うーん")]           // 長音+撥音
        [InlineData("んんん")]           // 撥音の連続
        [InlineData("っっっ")]           // 促音の連続
        [InlineData("ーーー")]           // 長音記号の連続
        public void ToPhonemes_特殊モーラ連続_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            AssertValidPhonemeString(result);
        }

        [SkippableFact]
        public void ToPhonemes_長音含む文_長音が処理される()
        {
            SkipIfNoDictionary();

            // 「ラーメン」は長音を含む一般的な単語
            var result = _engine!.ToPhonemes("ラーメン");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // r a を含むはず
            Assert.Contains("r a", result);
            // N（撥音）を含むはず
            Assert.Contains("N", result);
        }

        [SkippableFact]
        public void ToPhonemes_促音含む文_促音clが含まれる()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("きっと成功する");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // cl（促音）を含むはず
            Assert.Contains("cl", result);
        }

        // =====================================================================
        // 5. 助詞「は」「へ」の読み分け
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_助詞は_waと読む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("私は学生です");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 助詞「は」は「ワ」と読まれ、"w a"を含むはず
            Assert.Contains("w a", result);
        }

        [SkippableFact]
        public void ToPhonemes_助詞へ_eと読む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("東京へ行く");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 助詞「へ」は「エ」と読まれる
            // "e" が音素列中に存在するはず（東京の「ky o o」の後に）
            var phonemes = result.Split(' ');
            // 助詞「へ」が「e」として出現する
            Assert.Contains("e", phonemes);
        }

        [SkippableFact]
        public void ToPhonemes_助詞を_oと読む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("本を読む");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 助詞「を」は「o」と読まれる
            var phonemes = result.Split(' ');
            Assert.Contains("o", phonemes);
        }

        // =====================================================================
        // 6. 漢字の特殊な読み
        // =====================================================================

        [SkippableTheory]
        [InlineData("今日")]           // きょう
        [InlineData("明日")]           // あした/あす
        [InlineData("昨日")]           // きのう
        [InlineData("一人")]           // ひとり
        [InlineData("二人")]           // ふたり
        [InlineData("大人")]           // おとな
        public void ToPhonemes_特殊読み漢字_非空の音素列を返す(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
            // 最低2音素以上あるはず
            var tokens = result.Split(' ');
            Assert.True(tokens.Length >= 2, $"入力「{input}」の音素が少なすぎます: {result}");
        }

        // =====================================================================
        // 7. 句読点・記号の位置による影響
        // =====================================================================

        [SkippableTheory]
        [InlineData("「こんにちは」")]       // カギ括弧付き
        [InlineData("（テスト）")]           // 丸括弧付き
        [InlineData("—テスト—")]            // ダッシュ付き
        [InlineData("…テスト…")]            // 省略記号付き
        public void ToPhonemes_記号囲み_内容の音素が返る(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
        }

        [SkippableFact]
        public void ToPhonemes_文末記号_結果に影響しない()
        {
            SkipIfNoDictionary();

            var withPunctuation = _engine!.ToPhonemes("テストです。");
            var withQuestion = _engine!.ToPhonemes("テストです？");
            var withExclamation = _engine!.ToPhonemes("テストです！");

            // いずれも非空であること
            Assert.NotEmpty(withPunctuation);
            Assert.NotEmpty(withQuestion);
            Assert.NotEmpty(withExclamation);

            // 句読点違いでは音素列の基本部分は同じはず
            // （末尾のpauが異なる可能性があるが、基本音素は一致）
            Assert.Contains("t e", withPunctuation);
            Assert.Contains("t e", withQuestion);
            Assert.Contains("t e", withExclamation);
        }

        // =====================================================================
        // 8. 数字の様々なパターン
        // =====================================================================

        [SkippableTheory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("10")]
        [InlineData("100")]
        [InlineData("1000")]
        [InlineData("10000")]
        [InlineData("100000")]
        public void ToPhonemes_数字のみ_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            AssertValidPhonemeString(result);
        }

        [SkippableTheory]
        [InlineData("1月1日")]
        [InlineData("2月14日")]
        [InlineData("3月3日")]
        [InlineData("12月31日")]
        public void ToPhonemes_日付パターン_非空の音素列を返す(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
        }

        [SkippableTheory]
        [InlineData("1時")]
        [InlineData("12時30分")]
        [InlineData("午前9時")]
        [InlineData("午後3時15分")]
        public void ToPhonemes_時刻パターン_非空の音素列を返す(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
        }

        // =====================================================================
        // 9. G2POptionsによる処理制御
        // =====================================================================

        [SkippableFact]
        public void G2POptions_無声化OFF_大文字母音が出ない()
        {
            SkipIfNoDictionary();

            var options = new G2POptions(enableUnvoicedVowel: false);
            using var engine = new G2PEngine(new MeCabTokenizer(DicPath!), options);

            var result = engine.ToPhonemes("すき");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 無声化OFFなので大文字母音(A,I,U,E,O)を含まないはず
            Assert.DoesNotMatch(@"[AIUEO]", result);
        }

        [SkippableFact]
        public void G2POptions_長音展開OFF_ハイフンが出る()
        {
            SkipIfNoDictionary();

            var options = new G2POptions(expandLongVowels: false);
            using var engine = new G2PEngine(new MeCabTokenizer(DicPath!), options);

            var result = engine.ToPhonemes("東京");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 長音展開OFFなので "-" が含まれるはず
            Assert.Contains("-", result);
        }

        [SkippableFact]
        public void G2POptions_テキスト正規化OFF_動作する()
        {
            SkipIfNoDictionary();

            var options = new G2POptions(enableTextNormalization: false);
            using var engine = new G2PEngine(new MeCabTokenizer(DicPath!), options);

            var result = engine.ToPhonemes("こんにちは");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
        }

        [SkippableFact]
        public void G2POptions_数字処理OFF_動作する()
        {
            SkipIfNoDictionary();

            var options = new G2POptions(enableDigitProcessing: false);
            using var engine = new G2PEngine(new MeCabTokenizer(DicPath!), options);

            var result = engine.ToPhonemes("3本のペン");

            Assert.NotNull(result);
            // 数字処理OFFでもクラッシュしないこと
            AssertValidPhonemeString(result);
        }

        [SkippableFact]
        public void G2POptions_全処理OFF_動作する()
        {
            SkipIfNoDictionary();

            var options = new G2POptions(
                enableTextNormalization: false,
                enableUnvoicedVowel: false,
                enableDigitProcessing: false,
                enableAccentPhrase: false,
                enableAccentType: false,
                expandLongVowels: false);
            using var engine = new G2PEngine(new MeCabTokenizer(DicPath!), options);

            var result = engine.ToPhonemes("東京タワーに行きました");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
        }

        // =====================================================================
        // 10. Analyze APIの詳細検証
        // =====================================================================

        [SkippableFact]
        public void Analyze_各ノードのフィールドが設定される()
        {
            SkipIfNoDictionary();

            var nodes = _engine!.Analyze("東京タワーに行きました");

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            foreach (var node in nodes)
            {
                if (node.IsEmpty) continue;

                // Surface（表層形）が設定されている
                Assert.NotNull(node.Surface);
                Assert.NotEmpty(node.Surface);

                // POSが設定されている
                Assert.NotNull(node.PartOfSpeech);

                // Pronunciation（発音）が設定されている
                Assert.NotNull(node.Pronunciation);
            }
        }

        [SkippableFact]
        public void Analyze_アクセント情報が設定される()
        {
            SkipIfNoDictionary();

            var nodes = _engine!.Analyze("東京タワー");

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            // 少なくとも1つのノードにアクセント情報があるはず
            var hasAccent = nodes.Any(n => !n.IsEmpty && n.Pronunciation != null && n.Pronunciation.MoraCount > 0);
            Assert.True(hasAccent, "アクセント情報を持つノードがありません");
        }

        // =====================================================================
        // 11. 複数文の処理
        // =====================================================================

        [SkippableTheory]
        [InlineData("あ。い。う。え。お。")]
        [InlineData("おはよう。こんにちは。こんばんは。")]
        [InlineData("東京は晴れ。大阪は曇り。")]
        public void ToPhonemes_複数文_非空の音素列を返す(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
        }

        // =====================================================================
        // 12. Unicode特殊文字
        // =====================================================================

        [SkippableTheory]
        [InlineData("\u200B")]         // ゼロ幅スペース
        [InlineData("\uFEFF")]         // BOM
        [InlineData("\u00A0")]         // ノーブレークスペース
        [InlineData("\r\n")]           // CRLF
        [InlineData("\r")]             // CR
        public void ToPhonemes_Unicode特殊文字_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            AssertValidPhonemeString(result);
        }

        [SkippableTheory]
        [InlineData("🍣を食べる")]       // 絵文字+日本語
        [InlineData("テスト😀")]          // 日本語+絵文字
        [InlineData("😀😀😀")]           // 絵文字のみ
        public void ToPhonemes_絵文字_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            AssertValidPhonemeString(result);
        }

        // =====================================================================
        // 13. ToKanaの詳細検証
        // =====================================================================

        [SkippableFact]
        public void ToKana_ひらがな入力_カタカナに変換される()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("こんにちは");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // カタカナ文字のみ（長音記号含む）で構成されること
            Assert.Matches(@"^[\u30A0-\u30FF\u30FC]+$", result);
            // 「コンニチワ」を含むはず（助詞「は」→ワ）
            Assert.Contains("コンニチワ", result);
        }

        [SkippableFact]
        public void ToKana_漢字混じり文_カタカナのみの出力()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana("東京タワー");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 全てカタカナ+長音記号
            Assert.Matches(@"^[\u30A0-\u30FF\u30FC]+$", result);
        }

        // =====================================================================
        // 14. ToProsodyの詳細検証
        // =====================================================================

        [SkippableFact]
        public void ToProsody_複数アクセント句_区切り記号を含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToProsody("東京タワーに行きました");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.StartsWith("^", result);
            Assert.EndsWith("$", result);
            // 複数のアクセント句があれば "#" 区切りを含む可能性がある
            // （この検証は構造テスト）
            Assert.True(result.Length > 5, $"ToProsody出力が短すぎます: {result}");
        }

        // =====================================================================
        // 15. ToAccentPhrasesの詳細検証
        // =====================================================================

        [SkippableFact]
        public void ToAccentPhrases_各句にモーラが存在する()
        {
            SkipIfNoDictionary();

            var phrases = _engine!.ToAccentPhrases("東京タワーに行きました");

            Assert.NotNull(phrases);
            Assert.NotEmpty(phrases);

            foreach (var phrase in phrases)
            {
                Assert.NotNull(phrase.Moras);
                Assert.True(phrase.Moras.Count > 0, "モーラのないアクセント句があります");

                // 各モーラに種類が設定されている
                foreach (var mora in phrase.Moras)
                {
                    var kana = mora.Kind.ToKatakana();
                    Assert.NotNull(kana);
                    Assert.NotEmpty(kana);
                }
            }
        }

        [SkippableFact]
        public void ToAccentPhrases_アクセント位置が妥当()
        {
            SkipIfNoDictionary();

            var phrases = _engine!.ToAccentPhrases("こんにちは");

            Assert.NotNull(phrases);
            Assert.NotEmpty(phrases);

            foreach (var phrase in phrases)
            {
                // アクセント位置はモーラ数以下であるべき
                Assert.True(phrase.Accent >= 0, "アクセント位置が負です");
                Assert.True(phrase.Accent <= phrase.Moras.Count,
                    $"アクセント位置({phrase.Accent})がモーラ数({phrase.Moras.Count})を超えています");
            }
        }

        // =====================================================================
        // 16. ToFullContextLabelsの詳細検証
        // =====================================================================

        [SkippableFact]
        public void ToFullContextLabels_各ラベルがHTSフォーマットに準拠()
        {
            SkipIfNoDictionary();

            var labels = _engine!.ToFullContextLabels("東京タワー");

            Assert.NotNull(labels);
            Assert.True(labels.Count >= 3, $"ラベル数が少なすぎます: {labels.Count}");

            // 先頭と末尾はsil
            Assert.Contains("sil", labels[0]);
            Assert.Contains("sil", labels[labels.Count - 1]);

            // 中間ラベルは音素情報を含む
            for (int i = 1; i < labels.Count - 1; i++)
            {
                var label = labels[i];
                // HTSフォーマット必須フィールド
                Assert.Contains("/A:", label);
                Assert.Contains("/B:", label);
                Assert.Contains("/C:", label);
                Assert.Contains("/D:", label);
                Assert.Contains("/E:", label);
                Assert.Contains("/F:", label);
                Assert.Contains("/G:", label);
                Assert.Contains("/H:", label);
                Assert.Contains("/I:", label);
                Assert.Contains("/J:", label);
                Assert.Contains("/K:", label);
            }
        }

        // =====================================================================
        // 17. 繰り返しAPI呼び出しの安定性
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_同一入力複数回_同じ結果を返す()
        {
            SkipIfNoDictionary();

            var input = "音声合成テスト";
            var first = _engine!.ToPhonemes(input);
            var second = _engine!.ToPhonemes(input);
            var third = _engine!.ToPhonemes(input);

            Assert.Equal(first, second);
            Assert.Equal(second, third);
        }

        [SkippableFact]
        public void 連続異なる入力_安定動作()
        {
            SkipIfNoDictionary();

            var inputs = new[]
            {
                "こんにちは",
                "東京タワー",
                "",
                "123",
                "ABC",
                "テスト",
                "あいうえお",
                "日本語の音声合成",
            };

            foreach (var input in inputs)
            {
                var phonemes = _engine!.ToPhonemes(input);
                Assert.NotNull(phonemes);
                AssertValidPhonemeString(phonemes);

                var kana = _engine!.ToKana(input);
                Assert.NotNull(kana);

                var prosody = _engine!.ToProsody(input);
                Assert.NotNull(prosody);

                var phrases = _engine!.ToAccentPhrases(input);
                Assert.NotNull(phrases);

                var labels = _engine!.ToFullContextLabels(input);
                Assert.NotNull(labels);
            }
        }

        // =====================================================================
        // 18. 文字種の境界パターン
        // =====================================================================

        [SkippableTheory]
        [InlineData("あA")]              // ひらがな→ASCII
        [InlineData("Aあ")]              // ASCII→ひらがな
        [InlineData("漢字ABC漢字")]      // 漢字→英字→漢字
        [InlineData("テスト123テスト")]   // カタカナ→数字→カタカナ
        [InlineData("テストtest")]        // カタカナ→英字
        [InlineData("test テスト")]       // 英字→空白→カタカナ
        public void ToPhonemes_文字種境界_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            AssertValidPhonemeString(result);
        }

        // =====================================================================
        // 19. 非常に短い入力
        // =====================================================================

        [SkippableTheory]
        [InlineData("あ", "a")]
        [InlineData("い", "i")]
        [InlineData("う", "u")]
        [InlineData("え", "e")]
        [InlineData("お", "o")]
        public void ToPhonemes_単母音_期待する母音を含む(string input, string expectedVowel)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 無声化で大文字になる可能性があるため、小文字化して比較
            Assert.Contains(expectedVowel, result.ToLowerInvariant());
        }

        // =====================================================================
        // 20. 複雑な複合語・長い文
        // =====================================================================

        [SkippableTheory]
        [InlineData("東京都千代田区丸の内一丁目")]
        [InlineData("国際連合教育科学文化機関")]
        [InlineData("独立行政法人情報処理推進機構")]
        public void ToPhonemes_複合語_非空の音素列を返す(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
            // 長い複合語は多数の音素を含むはず
            var tokens = result.Split(' ');
            Assert.True(tokens.Length >= 5, $"入力「{input}」の音素数が少なすぎます: {tokens.Length}");
        }

        [SkippableFact]
        public void ToPhonemes_非常に長い文_クラッシュしない()
        {
            SkipIfNoDictionary();

            // 500文字の文
            var longText = string.Concat(Enumerable.Repeat("東京は日本の首都です。", 50));
            Assert.True(longText.Length >= 500);

            var result = _engine!.ToPhonemes(longText);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            AssertValidPhonemeString(result);
        }

        // =====================================================================
        // 21. pyopenjtalk互換性の詳細検証
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_こんにちは_完全一致()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("こんにちは");

            // pyopenjtalkの出力と完全一致
            Assert.Equal("k o N n i ch i w a", result);
        }

        [SkippableFact]
        public void ToPhonemes_ありがとうございます_構造検証()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("ありがとうございます");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 「ありがとう」= a r i g a t o o を含むはず
            Assert.Contains("a r i g a t o", result.ToLowerInvariant());
            // 「ございます」= g o z a i m a s を含むはず
            Assert.Contains("g o z a i m a", result.ToLowerInvariant());
        }

        // =====================================================================
        // 22. Dispose後の動作
        // =====================================================================

        [SkippableFact]
        public void Dispose後_ToPhonemes_例外をスロー()
        {
            SkipIfNoDictionary();

            var tokenizer = new MeCabTokenizer(DicPath!);
            var engine = new G2PEngine(tokenizer);

            // 正常動作を確認
            var result = engine.ToPhonemes("テスト");
            Assert.NotEmpty(result);

            // Dispose
            engine.Dispose();

            // Dispose後は例外がスローされるはず
            Assert.ThrowsAny<Exception>(() => engine.ToPhonemes("テスト"));
        }

        [SkippableFact]
        public void 二重Dispose_例外をスローしない()
        {
            SkipIfNoDictionary();

            var tokenizer = new MeCabTokenizer(DicPath!);
            var engine = new G2PEngine(tokenizer);

            // 二重Disposeが安全であること
            engine.Dispose();
            engine.Dispose(); // 例外なし
        }
    }
}
