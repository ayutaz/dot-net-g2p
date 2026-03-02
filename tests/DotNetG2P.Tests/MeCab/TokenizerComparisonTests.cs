using System;
using System.Collections.Generic;
using System.IO;
using DotNetG2P;
using DotNetG2P.MeCab;
using DotNetG2P.NMeCab;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.MeCab
{
    /// <summary>
    /// NMeCabTokenizerとMeCabTokenizerの出力を完全比較するテスト（最重要テスト）。
    /// 100文以上のテストケースで、トークン数・Surface・Features[0-10]の一致を検証する。
    /// </summary>
    public class TokenizerComparisonTests : IDisposable
    {
        private static string? DicPath => Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
        private static bool DictionaryExists => !string.IsNullOrEmpty(DicPath) && Directory.Exists(DicPath);

        private readonly ITestOutputHelper _output;
        private readonly NMeCabTokenizer? _nmecab;
        private readonly MeCabTokenizer? _mecab;

        public TokenizerComparisonTests(ITestOutputHelper output)
        {
            _output = output;
            if (DictionaryExists)
            {
                _nmecab = new NMeCabTokenizer(DicPath!);
                _mecab = new MeCabTokenizer(DicPath!);
            }
        }

        public void Dispose()
        {
            _nmecab?.Dispose();
            _mecab?.Dispose();
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!DictionaryExists, "naist-jdic辞書が見つかりません（環境変数 NAIST_JDIC_PATH を設定してください）");
        }

        /// <summary>
        /// 100文以上のテストケース。
        /// PiperPlus/pyopenjtalk/EdgeCase既存テストの入力テキスト + 追加文。
        /// </summary>
        private static readonly string[] TestSentences = new[]
        {
            // --- 基本 (10) ---
            "こんにちは",
            "東京タワー",
            "今日は天気がいいですね",
            "私の名前は田中です",
            "音声合成の研究",
            "人工知能",
            "自然言語処理",
            "機械学習",
            "深層学習",
            "テスト",

            // --- カタカナ (10) ---
            "アメリカ",
            "カメラ",
            "コーヒー",
            "ラーメン",
            "プログラミング",
            "コンピュータ",
            "インターネット",
            "スマートフォン",
            "エンジニア",
            "テクノロジー",

            // --- 長音・促音・撥音 (10) ---
            "カード",
            "キーボード",
            "クーラー",
            "ケーキ",
            "がっこう",
            "ハッピー",
            "ロック",
            "散歩",
            "新聞",
            "案内",

            // --- 文章 (20) ---
            "東京から大阪まで新幹線で行きます",
            "美味しいラーメンを食べました",
            "明日の天気予報を教えてください",
            "おはようございます",
            "お疲れ様でした",
            "東京スカイツリー",
            "ありがとう",
            "すみません",
            "よろしくお願いします",
            "日本語",
            "私は東京に住んでいます",
            "今日はいい天気ですね",
            "彼は学生です",
            "図書館で本を読みます",
            "映画を見に行きませんか",
            "この花はとても綺麗です",
            "駅前のレストランで食事をしました",
            "来週の月曜日に会議があります",
            "子供たちが公園で遊んでいる",
            "新しいパソコンを買いました",

            // --- 数字・日付 (10) ---
            "３本のペンがあります",
            "12月25日はクリスマスです",
            "3本",
            "5個",
            "100円",
            "2025年",
            "12月",
            "25日",
            "1000人",
            "42番",

            // --- 記号・英字 (10) ---
            "。、！？",
            "ABC",
            "hello",
            "AI",
            "x86",
            "MP3",
            "Hello こんにちは World",
            "C#プログラミング入門",
            "Python3で始めるAI",
            "Docker入門",

            // --- 短い入力 (10) ---
            "あ",
            "テストケース",
            "すき",
            "です",
            "猫",
            "山",
            "川",
            "花",
            "空",
            "雨",

            // --- PiperPlusテストから (20) ---
            "客",
            "写真",
            "お茶",
            "女性",
            "ガラス",
            "サクラ",
            "タイヤ",
            "ナマエ",
            "ハナ",
            "バナナ",
            "パンダ",
            "マルイ",
            "ヤマ",
            "銀行",
            "天国",
            "本",
            "恋愛",
            "東京タワーに行きました",
            "美味しいケーキを食べたい",

            // --- 追加文 (20) ---
            "富士山は日本一高い山です",
            "桜の花が咲きました",
            "電車に乗って出勤します",
            "スーパーで野菜を買いました",
            "日曜日は家でゆっくりします",
            "友達と映画を見ました",
            "大学で経済学を学んでいます",
            "北海道は冬になると雪が降ります",
            "彼女はピアノが上手です",
            "毎朝ジョギングをしています",
            "レポートを書かなければなりません",
            "先生に質問しました",
            "タクシーで空港に向かいました",
            "お弁当を持ってピクニックに行きます",
            "最近とても忙しいです",
            "この問題は難しいですね",
            "彼は英語がとても流暢です",
            "病院に行かなくてはいけません",
            "プレゼントを渡しました",
            "今夜は星がきれいに見えます",

            // --- ひらがな文 (5) ---
            "わたしはねこがすきです",
            "きょうはあめがふっています",
            "おなかがすきました",
            "いっしょにあそびましょう",
            "とてもたのしかったです",

            // --- 漢字多め (5) ---
            "東京都新宿区",
            "国際連合",
            "経済成長率",
            "環境問題対策",
            "情報通信技術",
        };

        public static IEnumerable<object[]> GetTestSentences()
        {
            foreach (var s in TestSentences)
            {
                yield return new object[] { s };
            }
        }

        [SkippableTheory]
        [MemberData(nameof(GetTestSentences))]
        public void Tokenize_NMeCabと同一出力(string text)
        {
            SkipIfNoDictionary();

            var nmecabTokens = _nmecab!.Tokenize(text);
            var mecabTokens = _mecab!.Tokenize(text);

            _output.WriteLine($"入力: \"{text}\"");
            _output.WriteLine($"NMeCab トークン数: {nmecabTokens.Count}, MeCab トークン数: {mecabTokens.Count}");

            // トークン数の一致
            Assert.Equal(nmecabTokens.Count, mecabTokens.Count);

            for (int i = 0; i < nmecabTokens.Count; i++)
            {
                var expected = nmecabTokens[i];
                var actual = mecabTokens[i];

                // Surface の一致
                Assert.Equal(expected.Surface, actual.Surface);

                _output.WriteLine($"  [{i}] Surface=\"{expected.Surface}\" POS={expected.POS}");

                // Features[0-10] の一致（POS, POSGroup1-3, CType, CForm, Lemma, Reading, Pronunciation, AccentInfo, ChainRule）
                for (int f = 0; f <= 10; f++)
                {
                    Assert.Equal(
                        expected.Features[f],
                        actual.Features[f]);
                }
            }
        }

        [SkippableTheory]
        [MemberData(nameof(GetTestSentences))]
        public void Tokenize_全15フィールド一致(string text)
        {
            SkipIfNoDictionary();

            var nmecabTokens = _nmecab!.Tokenize(text);
            var mecabTokens = _mecab!.Tokenize(text);

            Assert.Equal(nmecabTokens.Count, mecabTokens.Count);

            for (int i = 0; i < nmecabTokens.Count; i++)
            {
                var expected = nmecabTokens[i];
                var actual = mecabTokens[i];

                for (int f = 0; f < 15; f++)
                {
                    Assert.Equal(
                        expected.Features[f],
                        actual.Features[f]);
                }
            }
        }

        [SkippableTheory]
        [MemberData(nameof(GetTestSentences))]
        public void Tokenize_名前付きプロパティ一致(string text)
        {
            SkipIfNoDictionary();

            var nmecabTokens = _nmecab!.Tokenize(text);
            var mecabTokens = _mecab!.Tokenize(text);

            Assert.Equal(nmecabTokens.Count, mecabTokens.Count);

            for (int i = 0; i < nmecabTokens.Count; i++)
            {
                var e = nmecabTokens[i];
                var a = mecabTokens[i];

                Assert.Equal(e.Surface, a.Surface);
                Assert.Equal(e.POS, a.POS);
                Assert.Equal(e.POSGroup1, a.POSGroup1);
                Assert.Equal(e.POSGroup2, a.POSGroup2);
                Assert.Equal(e.POSGroup3, a.POSGroup3);
                Assert.Equal(e.ConjugationType, a.ConjugationType);
                Assert.Equal(e.ConjugationForm, a.ConjugationForm);
                Assert.Equal(e.OriginalForm, a.OriginalForm);
                Assert.Equal(e.Reading, a.Reading);
                Assert.Equal(e.Pronunciation, a.Pronunciation);
                Assert.Equal(e.AccentInfo, a.AccentInfo);
                Assert.Equal(e.ChainRule, a.ChainRule);
            }
        }
    }
}
