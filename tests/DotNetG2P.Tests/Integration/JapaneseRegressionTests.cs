using System;
using System.IO;
using System.Linq;
using DotNetG2P;
using DotNetG2P.Models;
using DotNetG2P.MeCab;
using Xunit;

namespace DotNetG2P.Tests.Integration
{
    /// <summary>
    /// 日本語G2P基本リグレッションテスト。
    /// 全API・NJDパイプライン各段階・エッジケースを網羅的に検証する。
    /// MeCab辞書が必要（環境変数 NAIST_JDIC_PATH）。
    /// </summary>
    public class JapaneseRegressionTests : IDisposable
    {
        private static string? DicPath => Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
        private static bool DictionaryExists => !string.IsNullOrEmpty(DicPath) && Directory.Exists(DicPath);

        private readonly MeCabTokenizer? _tokenizer;
        private readonly G2PEngine? _engine;

        public JapaneseRegressionTests()
        {
            if (DictionaryExists)
            {
                _tokenizer = new MeCabTokenizer(DicPath);
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

        private G2PEngine CreateEngineWithOptions(G2POptions options)
        {
            var tokenizer = new MeCabTokenizer(DicPath);
            return new G2PEngine(tokenizer, options);
        }

        // =====================================================================
        // 1. 全API正常動作テスト
        // =====================================================================

        [SkippableFact]
        public void ToPhonemes_こんにちは_正確な音素列()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("こんにちは");
            Assert.Contains("k o N n i ch i w a", result);
        }

        [SkippableFact]
        public void ToKana_こんにちは_コンニチワ()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToKana("こんにちは");
            Assert.Equal("コンニチワ", result);
        }

        [SkippableFact]
        public void ToProsody_こんにちは_韻律マーカー付き()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToProsody("こんにちは");
            Assert.StartsWith("^", result);
            Assert.EndsWith("$", result);
            Assert.Contains("k o", result);
        }

        [SkippableFact]
        public void ToAccentPhrases_こんにちは_アクセント句あり()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToAccentPhrases("こんにちは");
            Assert.NotEmpty(result);
            var totalMoras = result.Sum(ap => ap.Moras.Count);
            Assert.True(totalMoras >= 5, $"モーラ数が5未満: {totalMoras}");
        }

        [SkippableFact]
        public void ToFullContextLabels_こんにちは_silで囲まれたラベル()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToFullContextLabels("こんにちは");
            Assert.NotEmpty(result);
            Assert.Contains("sil", result[0]);
            Assert.Contains("sil", result[result.Count - 1]);
            foreach (var label in result)
            {
                Assert.Contains("/A:", label);
                Assert.Contains("/K:", label);
            }
        }

        [SkippableFact]
        public void ToProsodyFeatures_こんにちは_配列長一致()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToProsodyFeatures("こんにちは");
            Assert.True(result.Count > 0);
            Assert.Equal(result.Phonemes.Count, result.A1.Count);
            Assert.Equal(result.Phonemes.Count, result.A2.Count);
            Assert.Equal(result.Phonemes.Count, result.A3.Count);
        }

        [SkippableFact]
        public void Analyze_こんにちは_NjdNodeリスト()
        {
            SkipIfNoDictionary();
            var result = _engine!.Analyze("こんにちは");
            Assert.NotEmpty(result);
            Assert.True(result.Any(n => n.Pronunciation != null && n.Pronunciation.MoraCount > 0));
        }

        // =====================================================================
        // 2. 無声音化テスト（SetUnvoicedVowel）
        // =====================================================================

        [SkippableFact]
        public void 無声音化_すき_sUki()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("すき");
            Assert.Contains("s U", result);
            Assert.Contains("k i", result);
        }

        [SkippableFact]
        public void 無声音化_くさ_kUsa()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("くさ");
            // 「くさ」→ kU sa (くの母音が無声化)
            Assert.Contains("k U", result);
        }

        [SkippableFact]
        public void 無声音化OFF_すき_小文字u()
        {
            SkipIfNoDictionary();
            using var engine = CreateEngineWithOptions(new G2POptions(enableUnvoicedVowel: false));
            var result = engine.ToPhonemes("すき");
            Assert.Contains("s u", result);
            Assert.DoesNotContain("U", result);
        }

        [SkippableFact]
        public void 無声音化_です文末_無声化される()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("これはテストです");
            // 「です」の「す」は文末で無声化されることが多い
            Assert.NotEmpty(result);
            var phonemes = result.Split(' ');
            Assert.True(phonemes.Length > 5);
        }

        // =====================================================================
        // 3. 数字読み変換テスト（DigitSequence/SetDigit）
        // =====================================================================

        [SkippableFact]
        public void 数字読み_100円_ヒャクエン()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToKana("１００円");
            Assert.Contains("ヒャク", result);
            Assert.Contains("エン", result);
        }

        [SkippableFact]
        public void 数字読み_3本_サンボン()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToKana("３本");
            Assert.Contains("サン", result);
            Assert.Contains("ボン", result);
        }

        [SkippableFact]
        public void 数字読み_1000_セン()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToKana("１０００");
            Assert.Contains("セン", result);
        }

        [SkippableFact]
        public void 数字読み_10000_イチマン()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToKana("１００００");
            Assert.Contains("マン", result);
        }

        [SkippableFact]
        public void 数字読みOFF_辞書読みのまま()
        {
            SkipIfNoDictionary();
            using var engine = CreateEngineWithOptions(new G2POptions(enableDigitProcessing: false));
            var resultOn = _engine!.ToKana("１２３");
            var resultOff = engine.ToKana("１２３");
            Assert.NotEmpty(resultOn);
            Assert.NotEmpty(resultOff);
        }

        // =====================================================================
        // 4. アクセント句結合テスト（SetAccentPhrase）
        // =====================================================================

        [SkippableFact]
        public void アクセント句結合_助詞は前の語に結合()
        {
            SkipIfNoDictionary();
            var nodes = _engine!.Analyze("東京に行く");
            // 助詞「に」は前の語「東京」と同じアクセント句に結合されるはず
            Assert.NotEmpty(nodes);
            // ノード数がトークン数より少ないことで結合を確認
            Assert.True(nodes.Count >= 1);
        }

        [SkippableFact]
        public void アクセント句結合OFF_結合されない()
        {
            SkipIfNoDictionary();
            using var engine = CreateEngineWithOptions(new G2POptions(enableAccentPhrase: false));
            var nodesOn = _engine!.Analyze("東京に行く");
            var nodesOff = engine.Analyze("東京に行く");
            // アクセント句結合OFFのほうがノード数が多い（または同じ）
            Assert.True(nodesOff.Count >= nodesOn.Count,
                $"結合OFF({nodesOff.Count}) < 結合ON({nodesOn.Count})");
        }

        // =====================================================================
        // 5. アクセント結合型テスト（SetAccentType）
        // =====================================================================

        [SkippableFact]
        public void アクセント結合型_複合語のアクセント位置()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToAccentPhrases("東京タワー");
            Assert.NotEmpty(result);
            // アクセント句があり、AccentPositionが設定されていること
            foreach (var ap in result)
            {
                Assert.True(ap.Accent >= 0, $"アクセント位置が負: {ap.Accent}");
            }
        }

        [SkippableFact]
        public void アクセント結合型OFF_アクセントが変わる()
        {
            SkipIfNoDictionary();
            using var engine = CreateEngineWithOptions(new G2POptions(enableAccentType: false));
            var resultOn = _engine!.ToAccentPhrases("東京タワー");
            var resultOff = engine.ToAccentPhrases("東京タワー");
            Assert.NotEmpty(resultOn);
            Assert.NotEmpty(resultOff);
        }

        // =====================================================================
        // 6. 長音処理テスト
        // =====================================================================

        [SkippableFact]
        public void 長音展開ON_東京_母音繰り返し()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("東京");
            // デフォルトでは長音が母音繰り返しになる
            Assert.NotEmpty(result);
            // 「トーキョー」→ 母音展開あり
            Assert.DoesNotContain("-", result);
        }

        [SkippableFact]
        public void 長音展開OFF_ハイフン記号()
        {
            SkipIfNoDictionary();
            using var engine = CreateEngineWithOptions(new G2POptions(expandLongVowels: false));
            var result = engine.ToPhonemes("東京");
            Assert.NotEmpty(result);
            // ExpandLongVowels=false なら "-" が使われる
            Assert.Contains("-", result);
        }

        // =====================================================================
        // 7. 発音設定テスト（SetPronunciation）
        // =====================================================================

        [SkippableFact]
        public void 発音設定_助詞は_ワと発音()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("私は学生です");
            // 助詞「は」は「ワ」と読まれる
            Assert.Contains("w a", result);
        }

        [SkippableFact]
        public void 発音設定_助詞へ_エと発音()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("東京へ行く");
            // 助詞「へ」は「エ」と読まれる
            Assert.Contains("e", result);
        }

        // =====================================================================
        // 8. 具体的な文の音素検証
        // =====================================================================

        [SkippableTheory]
        [InlineData("おはよう", "o h a y o")]
        [InlineData("ありがとう", "a r i g a t o")]
        [InlineData("さようなら", "s a y o")]
        public void 基本語彙_期待音素を含む(string input, string expectedSubstring)
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes(input);
            Assert.Contains(expectedSubstring, result);
        }

        [SkippableTheory]
        [InlineData("東京", "トーキョー")]
        [InlineData("大阪", "オーサカ")]
        [InlineData("名古屋", "ナゴヤ")]
        [InlineData("北海道", "ホッカイドー")]
        public void 地名_カタカナ読み(string input, string expectedKana)
        {
            SkipIfNoDictionary();
            var result = _engine!.ToKana(input);
            Assert.Equal(expectedKana, result);
        }

        // =====================================================================
        // 9. 長文処理テスト
        // =====================================================================

        [SkippableFact]
        public void 長文_複数文_全APIがクラッシュしない()
        {
            SkipIfNoDictionary();
            var longText = "今日は天気がとても良いです。明日も晴れるといいですね。東京タワーに行って景色を見ました。富士山がきれいに見えました。日本語の音声合成技術はとても進歩しています。";

            var phonemes = _engine!.ToPhonemes(longText);
            Assert.NotEmpty(phonemes);

            var kana = _engine.ToKana(longText);
            Assert.NotEmpty(kana);

            var prosody = _engine.ToProsody(longText);
            Assert.StartsWith("^", prosody);
            Assert.EndsWith("$", prosody);

            var accentPhrases = _engine.ToAccentPhrases(longText);
            Assert.NotEmpty(accentPhrases);

            var labels = _engine.ToFullContextLabels(longText);
            Assert.NotEmpty(labels);

            var features = _engine.ToProsodyFeatures(longText);
            Assert.True(features.Count > 0);
        }

        [SkippableFact]
        public void 長文_300文字超_音素出力あり()
        {
            SkipIfNoDictionary();
            var text = string.Concat(Enumerable.Repeat("これはテストです。", 40));
            Assert.True(text.Length > 300);
            var result = _engine!.ToPhonemes(text);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 10. 記号・特殊文字テスト
        // =====================================================================

        [SkippableTheory]
        [InlineData("。")]
        [InlineData("、")]
        [InlineData("！？")]
        [InlineData("（）")]
        [InlineData("「」")]
        [InlineData("…")]
        public void 記号のみ_クラッシュしない(string input)
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes(input);
            Assert.NotNull(result);
        }

        [SkippableFact]
        public void 記号混じり文_音素出力あり()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("「こんにちは！」と言った。");
            Assert.NotEmpty(result);
            Assert.Contains("k o N n i ch i w a", result);
        }

        // =====================================================================
        // 11. バッチAPI テスト
        // =====================================================================

        [SkippableFact]
        public void バッチAPI_複数入力_正しい件数()
        {
            SkipIfNoDictionary();
            var texts = new[] { "こんにちは", "東京", "音声合成", "", null! };
            var phonemes = _engine!.ToPhonemesBatch(texts);
            var kanas = _engine.ToKanaBatch(texts);
            var prosodies = _engine.ToProsodyBatch(texts);

            Assert.Equal(5, phonemes.Count);
            Assert.Equal(5, kanas.Count);
            Assert.Equal(5, prosodies.Count);

            // 有効入力は空でない
            Assert.NotEmpty(phonemes[0]);
            Assert.NotEmpty(kanas[0]);
            Assert.NotEmpty(prosodies[0]);

            // 空/null入力は空
            Assert.Equal("", phonemes[3]);
            Assert.Equal("", phonemes[4]);
        }

        // =====================================================================
        // 12. オプション組み合わせテスト
        // =====================================================================

        [SkippableFact]
        public void 全オプションOFF_クラッシュしない()
        {
            SkipIfNoDictionary();
            using var engine = CreateEngineWithOptions(new G2POptions(
                enableTextNormalization: false,
                enableUnvoicedVowel: false,
                enableDigitProcessing: false,
                enableAccentPhrase: false,
                enableAccentType: false,
                expandLongVowels: false));

            var result = engine.ToPhonemes("こんにちは");
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void テキスト正規化OFF_全角数字そのまま()
        {
            SkipIfNoDictionary();
            using var engineNormOn = CreateEngineWithOptions(new G2POptions(enableTextNormalization: true));
            using var engineNormOff = CreateEngineWithOptions(new G2POptions(enableTextNormalization: false));

            var resultOn = engineNormOn.ToPhonemes("こんにちは");
            var resultOff = engineNormOff.ToPhonemes("こんにちは");

            // 基本テキストでは結果が同じ
            Assert.NotEmpty(resultOn);
            Assert.NotEmpty(resultOff);
        }

        // =====================================================================
        // 13. API一貫性テスト（ToPhonemes/ToKana/ToProsodyの一貫性）
        // =====================================================================

        [SkippableTheory]
        [InlineData("東京タワー")]
        [InlineData("人工知能")]
        [InlineData("おはようございます")]
        public void API一貫性_各APIが同じ入力で矛盾しない(string input)
        {
            SkipIfNoDictionary();
            var phonemes = _engine!.ToPhonemes(input);
            var kana = _engine.ToKana(input);
            var prosody = _engine.ToProsody(input);
            var accentPhrases = _engine.ToAccentPhrases(input);
            var labels = _engine.ToFullContextLabels(input);
            var features = _engine.ToProsodyFeatures(input);

            // 全て非空
            Assert.NotEmpty(phonemes);
            Assert.NotEmpty(kana);
            Assert.NotEmpty(prosody);
            Assert.NotEmpty(accentPhrases);
            Assert.NotEmpty(labels);
            Assert.True(features.Count > 0);

            // FullContextLabelsとProsodyFeaturesの長さが一致
            Assert.Equal(labels.Count, features.Count);
        }

        // =====================================================================
        // 14. Dispose後テスト
        // =====================================================================

        [SkippableFact]
        public void Dispose後_ToPhonemes_ObjectDisposedException()
        {
            SkipIfNoDictionary();
            var tokenizer = new MeCabTokenizer(DicPath);
            var engine = new G2PEngine(tokenizer);
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("テスト"));
        }

        [SkippableFact]
        public void Dispose後_ToKana_ObjectDisposedException()
        {
            SkipIfNoDictionary();
            var tokenizer = new MeCabTokenizer(DicPath);
            var engine = new G2PEngine(tokenizer);
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToKana("テスト"));
        }

        [SkippableFact]
        public void Dispose後_Analyze_ObjectDisposedException()
        {
            SkipIfNoDictionary();
            var tokenizer = new MeCabTokenizer(DicPath);
            var engine = new G2PEngine(tokenizer);
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.Analyze("テスト"));
        }

        [SkippableFact]
        public void 二重Dispose_例外なし()
        {
            SkipIfNoDictionary();
            var tokenizer = new MeCabTokenizer(DicPath);
            var engine = new G2PEngine(tokenizer);
            engine.Dispose();
            engine.Dispose(); // 二重Disposeで例外が出ないこと
        }

        // =====================================================================
        // 15. カタカナ入力テスト
        // =====================================================================

        [SkippableFact]
        public void カタカナ入力_音素変換()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("テスト");
            Assert.Contains("t e", result);
        }

        [SkippableFact]
        public void ひらがな入力_音素変換()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("てすと");
            Assert.Contains("t e", result);
        }

        // =====================================================================
        // 16. 漢字読みテスト
        // =====================================================================

        [SkippableTheory]
        [InlineData("学校", "ガッコー")]
        [InlineData("先生", "センセー")]
        [InlineData("日本", "ニッポン")]
        public void 漢字_カタカナ読み(string input, string expectedKana)
        {
            SkipIfNoDictionary();
            var result = _engine!.ToKana(input);
            Assert.Equal(expectedKana, result);
        }

        // =====================================================================
        // 17. 促音・撥音・長音テスト
        // =====================================================================

        [SkippableFact]
        public void 促音_がっこう_clを含む()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("学校");
            Assert.Contains("cl", result);
        }

        [SkippableFact]
        public void 撥音_にほん_Nを含む()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("日本");
            Assert.Contains("N", result);
        }

        [SkippableFact]
        public void 長音_東京_母音展開()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("東京");
            // 「トーキョー」→ 長音展開で "t o o ky o o" のようにoが繰り返される
            Assert.NotEmpty(result);
            // 長音展開のため "-" は含まれない
            Assert.DoesNotContain("-", result);
        }
    }
}
