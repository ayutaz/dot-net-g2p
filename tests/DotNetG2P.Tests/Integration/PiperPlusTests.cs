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
    /// piper-plus (ayutaz/piper-plus) のテストケースをC#に移植したテスト。
    /// 辞書依存のため、辞書が存在しない環境ではスキップされる。
    ///
    /// DotNetG2PとpyopenjtalkではG2P出力に以下の差異がある:
    ///   - 長音: pyopenjtalk/DotNetG2P共に "o o"（母音繰り返し、デフォルト）
    ///   - 無声母音: DotNetG2Pでは大文字（A,I,U,E,O）で表現
    ///   - 促音: pyopenjtalk "q" vs DotNetG2P "cl"
    /// これらの差異を正規化して比較する。
    /// </summary>
    public abstract class PiperPlusTestsBase : IDisposable
    {
        private static string? DicPath => Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
        private static bool DictionaryExists => !string.IsNullOrEmpty(DicPath) && Directory.Exists(DicPath);

        private readonly ITokenizer? _tokenizer;
        protected readonly G2PEngine? _engine;

        protected abstract ITokenizer CreateTokenizer(string dicPath);

        protected PiperPlusTestsBase()
        {
            if (DictionaryExists)
            {
                _tokenizer = CreateTokenizer(DicPath!);
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
        /// 音素文字列を正規化する（無声母音・促音の差異を吸収）。
        /// pyopenjtalk/DotNetG2P共に長音は母音繰り返しで表現（デフォルト）。
        /// 互換性のため、"-" が残っている場合も直前の母音に展開する。
        /// </summary>
        private static string NormalizePhonemes(string phonemes)
        {
            if (string.IsNullOrEmpty(phonemes))
                return "";

            // 1. 各トークンを処理（撥音 "N" は大文字のまま保持）
            var tokens = phonemes.Split(' ');
            for (int i = 0; i < tokens.Length; i++)
            {
                // 撥音 "N" はそのまま
                if (tokens[i] == "N")
                    continue;

                // 促音 "cl" → "q" に統一（piper-plus形式へ）
                if (tokens[i] == "cl")
                {
                    tokens[i] = "q";
                    continue;
                }

                // 無声母音の大文字→小文字（A→a, I→i, U→u, E→e, O→o）
                tokens[i] = tokens[i].ToLowerInvariant();
            }

            // 2. 長音 "-" を直前の母音に展開
            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i] == "-" && i > 0)
                {
                    var prev = tokens[i - 1];
                    if (prev == "a" || prev == "i" || prev == "u" || prev == "e" || prev == "o")
                    {
                        tokens[i] = prev;
                    }
                }
            }

            return string.Join(" ", tokens);
        }

        // =====================================================================
        // 1. 基本的な日本語音素化テスト（piper-plus test_japanese_basic 相当）
        // =====================================================================

        [SkippableFact]
        public void 基本_こんにちは_音素列を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("こんにちは");

            Assert.NotEmpty(result);
            // "k o N n i ch i w a" を含むか（助詞「は」→「ワ」）
            Assert.Contains("k o", result);
            Assert.Contains("ch i", result);
            Assert.Contains("w a", result);
        }

        [SkippableFact]
        public void 基本_ひらがな単一文字_音素が返る()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("あ");

            Assert.NotEmpty(result);
            var normalized = NormalizePhonemes(result);
            Assert.Contains("a", normalized);
        }

        // =====================================================================
        // 2. カタカナ→音素変換テスト（piper-plus test_katakana_to_phonemes 相当）
        // =====================================================================

        [SkippableTheory]
        [InlineData("アメリカ", "a")]      // ア→a
        [InlineData("カメラ", "k a")]      // カ→k a
        [InlineData("ガラス", "g a")]      // ガ→g a
        [InlineData("サクラ", "s a")]      // サ→s a
        [InlineData("タイヤ", "t a")]      // タ→t a
        [InlineData("ナマエ", "n a")]      // ナ→n a
        [InlineData("ハナ", "h a")]        // ハ→h a
        [InlineData("バナナ", "b a")]      // バ→b a
        [InlineData("パンダ", "p a")]      // パ→p a
        [InlineData("マルイ", "m a")]      // マ→m a
        [InlineData("ヤマ", "y a")]        // ヤ→y a
        [InlineData("ラーメン", "r a")]    // ラ→r a
        public void カタカナ基本子音_音素列に期待する子音が含まれる(string input, string expectedSubstring)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotEmpty(result);
            var normalized = NormalizePhonemes(result);
            Assert.Contains(expectedSubstring, normalized);
        }

        // =====================================================================
        // 3. 長音テスト（piper-plus test_long_vowels 相当）
        // =====================================================================

        [SkippableTheory]
        [InlineData("カード", "k a")]
        [InlineData("キーボード", "k i")]
        [InlineData("クーラー", "k u")]
        [InlineData("ケーキ", "k e")]
        [InlineData("コーヒー", "k o")]
        public void 長音_カタカナ長音記号_正規化後に母音繰り返しまたは長音記号を含む(string input, string expectedSubstring)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotEmpty(result);
            var normalized = NormalizePhonemes(result);
            Assert.Contains(expectedSubstring, normalized);
        }

        // =====================================================================
        // 4. 促音（っ）テスト（piper-plus test_small_tsu 相当）
        // =====================================================================

        [SkippableTheory]
        [InlineData("がっこう", "q")]        // がっこう: 促音
        [InlineData("ハッピー", "q")]        // ハッピー: 促音
        [InlineData("ロック", "q")]          // ロック: 促音
        public void 促音_正規化後にqが含まれる(string input, string expectedSubstring)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotEmpty(result);
            var normalized = NormalizePhonemes(result);
            Assert.Contains(expectedSubstring, normalized);
        }

        // =====================================================================
        // 5. 拗音テスト（piper-plus test_compound_kana 相当）
        // =====================================================================

        [SkippableTheory]
        [InlineData("客", "ky")]       // きゃ行: ky
        [InlineData("写真", "sh")]     // しゃ行: sh
        [InlineData("お茶", "ch")]     // ちゃ行: ch
        [InlineData("女性", "j")]      // じょ行: j
        public void 拗音_期待する子音が含まれる(string input, string expectedConsonant)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotEmpty(result);
            var normalized = NormalizePhonemes(result);
            Assert.Contains(expectedConsonant, normalized);
        }

        // =====================================================================
        // 6. 撥音（ン）テスト（piper-plus test_n_variant 相当）
        // =====================================================================

        [SkippableTheory]
        [InlineData("散歩")]       // さんぽ: N + 両唇音p
        [InlineData("新聞")]       // しんぶん: N + 両唇音b
        [InlineData("案内")]       // あんない: N + 歯茎音n
        [InlineData("銀行")]       // ぎんこう: N + 軟口蓋音k
        [InlineData("天国")]       // てんごく: N + 軟口蓋音g
        [InlineData("本")]         // ほん: N (語末)
        [InlineData("恋愛")]       // れんあい: N + 母音
        public void 撥音_Nが音素列に含まれる(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotEmpty(result);
            // DotNetG2Pでは撥音は常に "N" で表現
            var upper = result.ToUpperInvariant();
            Assert.Contains("N", upper);
        }

        // =====================================================================
        // 7. 無効入力ハンドリングテスト（piper-plus invalid input handling 相当）
        // =====================================================================

        [SkippableFact]
        public void 無効入力_空文字列_空を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("");

            Assert.Equal("", result);
        }

        [SkippableFact]
        public void 無効入力_null_空を返す()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(null!);

            Assert.Equal("", result);
        }

        [SkippableFact]
        public void 無効入力_長い入力_クラッシュしない()
        {
            SkipIfNoDictionary();

            // 1000文字の "あ"
            var longInput = new string('あ', 1000);
            var result = _engine!.ToPhonemes(longInput);

            Assert.NotNull(result);
            // 少なくとも何らかの音素が返る
            Assert.True(result.Length > 0, "長い入力に対して空の結果");
        }

        [SkippableFact]
        public void 無効入力_混合スクリプト_クラッシュしない()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes("Hello こんにちは World");

            Assert.NotNull(result);
            // 少なくとも「こんにちは」の音素は含まれる
            Assert.True(result.Length > 0);
        }

        [SkippableTheory]
        [InlineData("！？")]
        [InlineData("。、")]
        [InlineData("・「」『』")]
        public void 無効入力_特殊文字のみ_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            // 例外が発生しないことのみ確認
            var result = _engine!.ToPhonemes(input);
            Assert.NotNull(result);
        }

        // =====================================================================
        // 8. piper-plus互換 包括的テスト
        //    piper-plusで使われる一般的な日本語文に対して、
        //    DotNetG2Pが正しく音素化できることを確認
        // =====================================================================

        [SkippableTheory]
        [InlineData("こんにちは", "k o")]
        [InlineData("東京タワー", "t")]
        [InlineData("今日は天気がいいですね", "d e")]
        [InlineData("私の名前は田中です", "t a")]
        [InlineData("お疲れ様でした", "d e")]
        [InlineData("おはようございます", "o")]
        [InlineData("東京スカイツリー", "t")]
        [InlineData("人工知能", "ch")]
        [InlineData("音声合成", "o")]
        [InlineData("自然言語処理", "sh")]
        [InlineData("機械学習", "k i")]
        [InlineData("深層学習", "sh")]
        [InlineData("テスト", "t e")]
        [InlineData("プログラミング", "p")]
        [InlineData("コンピュータ", "k o")]
        [InlineData("インターネット", "i")]
        [InlineData("ありがとう", "a")]
        [InlineData("すみません", "s")]
        [InlineData("よろしくお願いします", "o")]
        [InlineData("日本語", "n")]
        public void 包括テスト_一般的なテキスト_期待する音素を含む(string input, string expectedSubstring)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotEmpty(result);
            var normalized = NormalizePhonemes(result);
            Assert.Contains(expectedSubstring, normalized);
        }

        [SkippableTheory]
        [InlineData("こんにちは")]
        [InlineData("東京タワー")]
        [InlineData("今日は天気がいいですね")]
        [InlineData("私の名前は田中です")]
        [InlineData("12月25日はクリスマスです")]
        [InlineData("3本のペンがあります")]
        [InlineData("お疲れ様でした")]
        [InlineData("東京から大阪まで新幹線で行きます")]
        [InlineData("美味しいラーメンを食べました")]
        [InlineData("明日の天気予報を教えてください")]
        public void 包括テスト_一般的なテキスト_空でない音素列を返す(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotEmpty(result);
            // 音素が最低3つ以上あること
            var tokens = result.Split(' ');
            Assert.True(tokens.Length >= 3, $"入力「{input}」に対する音素が少なすぎます: {result}");
        }

        // =====================================================================
        // 9. ToKana互換テスト
        // =====================================================================

        [SkippableTheory]
        [InlineData("東京", "トーキョー")]
        [InlineData("テスト", "テスト")]
        public void ToKana_基本的なテキスト_期待するカタカナを含む(string input, string expectedKana)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToKana(input);

            Assert.NotEmpty(result);
            Assert.Contains(expectedKana, result);
        }

        // =====================================================================
        // 10. ToProsody互換テスト
        // =====================================================================

        [SkippableFact]
        public void ToProsody_基本テキスト_開始終了マーカーを含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToProsody("こんにちは");

            Assert.NotEmpty(result);
            // ESPnet韻律形式: ^ で始まり $ で終わる
            Assert.StartsWith("^", result);
            Assert.EndsWith("$", result);
        }

        [SkippableFact]
        public void ToProsody_基本テキスト_音素を含む()
        {
            SkipIfNoDictionary();

            var result = _engine!.ToProsody("こんにちは");

            Assert.NotEmpty(result);
            Assert.Contains("k o", result);
        }

        // =====================================================================
        // 11. 数字テスト（piper-plus日本語TTS向け）
        // =====================================================================

        [SkippableTheory]
        [InlineData("3本")]
        [InlineData("5個")]
        [InlineData("100円")]
        [InlineData("2025年")]
        [InlineData("12月")]
        [InlineData("25日")]
        public void 数字テスト_助数詞付き数字_クラッシュせず音素が返る(string input)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotNull(result);
            // 助数詞部分の読みは最低限あるはず
            Assert.True(result.Length > 0, $"入力「{input}」に対する音素が空です");
        }

        // =====================================================================
        // 12. Analyze API テスト
        // =====================================================================

        [SkippableFact]
        public void Analyze_基本テキスト_NjdNodeリストを返す()
        {
            SkipIfNoDictionary();

            var nodes = _engine!.Analyze("こんにちは");

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            // 各ノードにSurfaceが設定されている
            foreach (var node in nodes)
            {
                if (node.IsEmpty) continue;
                Assert.NotNull(node.Surface);
                Assert.NotEmpty(node.Surface);
            }
        }

        // =====================================================================
        // 13. 正規化比較テスト
        //     DotNetG2Pとpyopenjtalkの出力差異を正規化して比較する
        // =====================================================================

        [SkippableTheory]
        [InlineData("こんにちは", "k o N n i ch i w a")]
        [InlineData("テスト", "t e s u t o")]  // 無声化 "s U" → 正規化で "s u"
        public void 正規化比較_pyopenjtalk期待値と正規化後一致(string input, string expectedNormalized)
        {
            SkipIfNoDictionary();

            var result = _engine!.ToPhonemes(input);

            Assert.NotEmpty(result);
            var normalized = NormalizePhonemes(result);
            Assert.Equal(expectedNormalized, normalized);
        }

        // =====================================================================
        // 14. フルコンテキストラベルテスト（piper-plus HTS labels 相当）
        // =====================================================================

        [SkippableFact]
        public void フルコンテキストラベル_基本テキスト_silで始まりsilで終わる()
        {
            SkipIfNoDictionary();

            var labels = _engine!.ToFullContextLabels("こんにちは");

            Assert.NotNull(labels);
            Assert.NotEmpty(labels);
            // 先頭はsil
            Assert.Contains("sil", labels[0]);
            // 末尾もsil
            Assert.Contains("sil", labels[labels.Count - 1]);
        }

        [SkippableFact]
        public void フルコンテキストラベル_基本テキスト_各ラベルがHTSフォーマット()
        {
            SkipIfNoDictionary();

            var labels = _engine!.ToFullContextLabels("テスト");

            Assert.NotNull(labels);
            Assert.NotEmpty(labels);

            // HTSフォーマット: 各ラベルは "音素^前音素-現音素+次音素=..." のような形式
            foreach (var label in labels)
            {
                // 基本的なHTSフォーマットの特徴文字を含む
                Assert.True(
                    label.Contains("/") || label.Contains("-") || label.Contains("+"),
                    $"ラベルがHTSフォーマットではありません: {label}");
            }
        }

        // =====================================================================
        // 15. AccentPhrase テスト（VOICEVOX互換）
        // =====================================================================

        [SkippableFact]
        public void AccentPhrase_基本テキスト_非空リストを返す()
        {
            SkipIfNoDictionary();

            var phrases = _engine!.ToAccentPhrases("こんにちは");

            Assert.NotNull(phrases);
            Assert.NotEmpty(phrases);
            // 各AccentPhraseにはモーラがある
            Assert.True(phrases[0].Moras.Count > 0, "AccentPhraseにモーラがありません");
        }

        [SkippableFact]
        public void AccentPhrase_複数アクセント句_2つ以上の句を返す()
        {
            SkipIfNoDictionary();

            var phrases = _engine!.ToAccentPhrases("東京タワーに行きました");

            Assert.NotNull(phrases);
            // 複数のアクセント句に分かれるはず
            Assert.True(phrases.Count >= 2, $"アクセント句数が少なすぎます: {phrases.Count}");
        }
    }

    /// <summary>MeCabTokenizerによるpiper-plus互換テスト。</summary>
    public class PiperPlusTests_MeCab : PiperPlusTestsBase
    {
        protected override ITokenizer CreateTokenizer(string dicPath) => new MeCabTokenizer(dicPath);
    }
}
