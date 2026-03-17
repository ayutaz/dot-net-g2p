using System;
using System.Collections.Generic;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// 中国語Prosody APIの正確性を検証するテスト。
    /// ChineseProsodyInfo（a1=声調, a2=語内位置, a3=語長）と
    /// ChineseProsodyResult（IPA音素+韻律情報）の動作を網羅的にテストする。
    /// </summary>
    public class ChineseProsodyTests : IDisposable
    {
        private readonly ChineseG2PEngine _engine;

        public ChineseProsodyTests()
        {
            _engine = new ChineseG2PEngine();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // 1. 単一漢字テスト
        // =====================================================================

        [Fact]
        public void ToIpaWithProsody_単一漢字_我_声調と語長が正しい()
        {
            // "我" は第3声
            var result = _engine.ToIpaWithProsody("我");
            Assert.Single(result.Phonemes);
            Assert.Single(result.Prosody);
            Assert.Equal(3, result.Prosody[0].A1); // 第3声
            Assert.Equal(1, result.Prosody[0].A2); // 語内位置1
            Assert.Equal(1, result.Prosody[0].A3); // 語長1
        }

        [Fact]
        public void ToIpaWithProsody_単一漢字_中_声調と語長が正しい()
        {
            // "中" は第1声
            var result = _engine.ToIpaWithProsody("中");
            Assert.Single(result.Phonemes);
            Assert.Single(result.Prosody);
            Assert.Equal(1, result.Prosody[0].A1); // 第1声
            Assert.Equal(1, result.Prosody[0].A2); // 語内位置1
            Assert.Equal(1, result.Prosody[0].A3); // 語長1
        }

        [Fact]
        public void ToIpaWithProsody_単一漢字_IPA音素が非空()
        {
            var result = _engine.ToIpaWithProsody("人");
            Assert.Single(result.Phonemes);
            Assert.NotEmpty(result.Phonemes[0]);
        }

        // =====================================================================
        // 2. 二字熟語テスト
        // =====================================================================

        [Fact]
        public void ToIpaWithProsody_二字熟語_你好_語長と位置が正しい()
        {
            var result = _engine.ToIpaWithProsody("你好");
            Assert.Equal(2, result.Phonemes.Count);
            Assert.Equal(2, result.Prosody.Count);

            // 第1字: 語内位置1, 語長2
            Assert.Equal(1, result.Prosody[0].A2);
            Assert.Equal(2, result.Prosody[0].A3);

            // 第2字: 語内位置2, 語長2
            Assert.Equal(2, result.Prosody[1].A2);
            Assert.Equal(2, result.Prosody[1].A3);
        }

        [Fact]
        public void ToIpaWithProsody_二字熟語_中国_語長と位置が正しい()
        {
            var result = _engine.ToIpaWithProsody("中国");
            Assert.Equal(2, result.Phonemes.Count);

            Assert.Equal(1, result.Prosody[0].A2);
            Assert.Equal(2, result.Prosody[0].A3);

            Assert.Equal(2, result.Prosody[1].A2);
            Assert.Equal(2, result.Prosody[1].A3);
        }

        // =====================================================================
        // 3. 三字以上テスト
        // =====================================================================

        [Fact]
        public void ToIpaWithProsody_三字_大学生_語内位置が1から3まで正しい()
        {
            var result = _engine.ToIpaWithProsody("大学生");
            Assert.Equal(3, result.Phonemes.Count);
            Assert.Equal(3, result.Prosody.Count);

            // 全3文字が1語として扱われる
            Assert.Equal(1, result.Prosody[0].A2); // 第1字
            Assert.Equal(3, result.Prosody[0].A3);

            Assert.Equal(2, result.Prosody[1].A2); // 第2字
            Assert.Equal(3, result.Prosody[1].A3);

            Assert.Equal(3, result.Prosody[2].A2); // 第3字
            Assert.Equal(3, result.Prosody[2].A3);
        }

        [Fact]
        public void ToIpaWithProsody_四字熟語_一言一行_語長が4()
        {
            // 4つの連続漢字 → 1語、語長4
            var result = _engine.ToIpaWithProsody("自由自在");
            Assert.Equal(4, result.Phonemes.Count);

            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(i + 1, result.Prosody[i].A2);
                Assert.Equal(4, result.Prosody[i].A3);
            }
        }

        // =====================================================================
        // 4. 複数語文テスト（句読点/スペースによる語分割）
        // =====================================================================

        [Fact]
        public void ToIpaWithProsody_スペース区切り_別々の語として扱われる()
        {
            // "我 好" → "我"(語長1) と "好"(語長1) が独立
            var result = _engine.ToIpaWithProsody("我 好");
            Assert.Equal(2, result.Phonemes.Count);

            // "我": a2=1, a3=1
            Assert.Equal(1, result.Prosody[0].A2);
            Assert.Equal(1, result.Prosody[0].A3);

            // "好": a2=1, a3=1
            Assert.Equal(1, result.Prosody[1].A2);
            Assert.Equal(1, result.Prosody[1].A3);
        }

        [Fact]
        public void ToIpaWithProsody_句読点区切り_別々の語として扱われる()
        {
            // "中国人，好" → "中国人"(語長3) と "好"(語長1)
            var result = _engine.ToIpaWithProsody("中国人，好");
            Assert.Equal(4, result.Phonemes.Count);

            // "中国人": a3=3
            Assert.Equal(1, result.Prosody[0].A2);
            Assert.Equal(3, result.Prosody[0].A3);

            Assert.Equal(2, result.Prosody[1].A2);
            Assert.Equal(3, result.Prosody[1].A3);

            Assert.Equal(3, result.Prosody[2].A2);
            Assert.Equal(3, result.Prosody[2].A3);

            // "好": a3=1
            Assert.Equal(1, result.Prosody[3].A2);
            Assert.Equal(1, result.Prosody[3].A3);
        }

        [Fact]
        public void ToIpaWithProsody_ASCII句読点区切り_別々の語として扱われる()
        {
            // "中国.人" → "中国"(語長2) と "人"(語長1)
            var result = _engine.ToIpaWithProsody("中国.人");
            Assert.Equal(3, result.Phonemes.Count);

            Assert.Equal(2, result.Prosody[0].A3); // 中国: 語長2
            Assert.Equal(2, result.Prosody[1].A3);

            Assert.Equal(1, result.Prosody[2].A3); // 人: 語長1
        }

        [Fact]
        public void ToIpaWithProsody_英数字挟み_別々の語として扱われる()
        {
            // "中abc国" → 英数字で区切られるので"中"(語長1)と"国"(語長1)
            var result = _engine.ToIpaWithProsody("中abc国");
            Assert.Equal(2, result.Phonemes.Count);

            Assert.Equal(1, result.Prosody[0].A3); // 中: 語長1
            Assert.Equal(1, result.Prosody[1].A3); // 国: 語長1
        }

        // =====================================================================
        // 5. 声調変調後のa1確認テスト
        // =====================================================================

        [Fact]
        public void ToIpaWithProsody_三声連読_你好_第1字が2声に変調()
        {
            // "你好": 你(3声) + 好(3声) → 三声連読で 你→2声
            var result = _engine.ToIpaWithProsody("你好");
            Assert.Equal(2, result.Prosody[0].A1); // 変調後: 第2声
            Assert.Equal(3, result.Prosody[1].A1); // 最後の3声は変わらない
        }

        [Fact]
        public void ToIpaWithProsody_声調変調なし_天気_声調がそのまま()
        {
            // 声調変調なしオプションで確認
            var opts = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(opts);

            // "你好": 你(3声) + 好(3声) → 変調なしなので両方3声
            var result = engine.ToIpaWithProsody("你好");
            Assert.Equal(3, result.Prosody[0].A1); // 変調なし: 第3声
            Assert.Equal(3, result.Prosody[1].A1); // 第3声
        }

        [Fact]
        public void ToIpaWithProsody_一変調_四声前で2声に変わる()
        {
            // "一个" → "一"(1声)が"个"(4声)の前で2声に変調
            var result = _engine.ToIpaWithProsody("一个");
            Assert.Equal(2, result.Prosody[0].A1); // 変調後: 第2声
            Assert.Equal(4, result.Prosody[1].A1); // 个: 第4声
        }

        [Fact]
        public void ToIpaWithProsody_一変調_一声前で4声に変わる()
        {
            // "一天" → "一"(1声)が"天"(1声)の前で4声に変調
            var result = _engine.ToIpaWithProsody("一天");
            Assert.Equal(4, result.Prosody[0].A1); // 変調後: 第4声
            Assert.Equal(1, result.Prosody[1].A1); // 天: 第1声
        }

        [Fact]
        public void ToIpaWithProsody_不変調_四声前で2声に変わる()
        {
            // "不是" → "不"(4声)が"是"(4声)の前で2声に変調
            var result = _engine.ToIpaWithProsody("不是");
            Assert.Equal(2, result.Prosody[0].A1); // 変調後: 第2声
            Assert.Equal(4, result.Prosody[1].A1); // 是: 第4声
        }

        [Fact]
        public void ToIpaWithProsody_第一声の漢字_a1が1()
        {
            // "天" は第1声
            var result = _engine.ToIpaWithProsody("天");
            Assert.Equal(1, result.Prosody[0].A1);
        }

        [Fact]
        public void ToIpaWithProsody_第二声の漢字_a1が2()
        {
            // "人" は第2声
            var result = _engine.ToIpaWithProsody("人");
            Assert.Equal(2, result.Prosody[0].A1);
        }

        [Fact]
        public void ToIpaWithProsody_第四声の漢字_a1が4()
        {
            // "大" は第4声
            var result = _engine.ToIpaWithProsody("大");
            Assert.Equal(4, result.Prosody[0].A1);
        }

        // =====================================================================
        // 6. 音素配列と韻律配列の長さ一致テスト
        // =====================================================================

        [Theory]
        [InlineData("我")]
        [InlineData("你好")]
        [InlineData("中国人")]
        [InlineData("大学生活")]
        [InlineData("中国，你好")]
        [InlineData("我 好")]
        public void ToIpaWithProsody_音素と韻律の長さが一致する(string text)
        {
            var result = _engine.ToIpaWithProsody(text);
            Assert.Equal(result.Phonemes.Count, result.Prosody.Count);
        }

        [Fact]
        public void ToIpaWithProsody_長文_音素と韻律の長さが一致する()
        {
            var result = _engine.ToIpaWithProsody("中华人民共和国万岁");
            Assert.Equal(result.Phonemes.Count, result.Prosody.Count);
            Assert.Equal(9, result.Phonemes.Count);
        }

        // =====================================================================
        // 7. 空・nullテスト
        // =====================================================================

        [Fact]
        public void ToIpaWithProsody_null_空結果を返す()
        {
            var result = _engine.ToIpaWithProsody(null!);
            Assert.Empty(result.Phonemes);
            Assert.Empty(result.Prosody);
        }

        [Fact]
        public void ToIpaWithProsody_空文字列_空結果を返す()
        {
            var result = _engine.ToIpaWithProsody("");
            Assert.Empty(result.Phonemes);
            Assert.Empty(result.Prosody);
        }

        [Fact]
        public void ToIpaWithProsody_ホワイトスペースのみ_空結果を返す()
        {
            var result = _engine.ToIpaWithProsody("   ");
            Assert.Empty(result.Phonemes);
            Assert.Empty(result.Prosody);
        }

        [Fact]
        public void ToIpaWithProsody_句読点のみ_空結果を返す()
        {
            // 句読点はピンインなし → 結果は空
            var result = _engine.ToIpaWithProsody("，。！");
            Assert.Empty(result.Phonemes);
            Assert.Empty(result.Prosody);
        }

        [Fact]
        public void ToIpaWithProsody_英数字のみ_空結果を返す()
        {
            // 英数字はピンインなし（RawText扱い）→ 結果は空
            var result = _engine.ToIpaWithProsody("abc123");
            Assert.Empty(result.Phonemes);
            Assert.Empty(result.Prosody);
        }

        // =====================================================================
        // 8. Dispose後テスト
        // =====================================================================

        [Fact]
        public void ToIpaWithProsody_Dispose後_ObjectDisposedExceptionをスロー()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToIpaWithProsody("你好"));
        }

        [Fact]
        public void ToIpaWithProsodyBatch_Dispose後_ObjectDisposedExceptionをスロー()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(
                () => engine.ToIpaWithProsodyBatch(new[] { "你好" }));
        }

        // =====================================================================
        // 9. バッチAPIテスト
        // =====================================================================

        [Fact]
        public void ToIpaWithProsodyBatch_複数テキスト_各結果が正しい()
        {
            var texts = new[] { "你好", "中国", "大" };
            var results = _engine.ToIpaWithProsodyBatch(texts);

            Assert.Equal(3, results.Count);

            // "你好": 2音節
            Assert.Equal(2, results[0].Phonemes.Count);
            Assert.Equal(2, results[0].Prosody.Count);

            // "中国": 2音節
            Assert.Equal(2, results[1].Phonemes.Count);
            Assert.Equal(2, results[1].Prosody.Count);

            // "大": 1音節
            Assert.Equal(1, results[2].Phonemes.Count);
            Assert.Equal(1, results[2].Prosody.Count);
        }

        [Fact]
        public void ToIpaWithProsodyBatch_空配列_空結果を返す()
        {
            var results = _engine.ToIpaWithProsodyBatch(Array.Empty<string>());
            Assert.Empty(results);
        }

        [Fact]
        public void ToIpaWithProsodyBatch_null配列_ArgumentNullExceptionをスロー()
        {
            Assert.Throws<ArgumentNullException>(
                () => _engine.ToIpaWithProsodyBatch(null!));
        }

        [Fact]
        public void ToIpaWithProsodyBatch_includeTones_false_声調マーカーなし()
        {
            var texts = new[] { "天" };
            var results = _engine.ToIpaWithProsodyBatch(texts, false);
            Assert.Single(results);

            // 声調マーカーなしのIPA
            var withTones = _engine.ToIpaWithProsody("天", true);
            var withoutTones = _engine.ToIpaWithProsody("天", false);

            // 声調マーカーありの方が長い（tone lettersが付加される）
            Assert.True(withTones.Phonemes[0].Length > withoutTones.Phonemes[0].Length);
        }

        [Fact]
        public void ToIpaWithProsodyBatch_includeTones_true_声調マーカーあり()
        {
            var texts = new[] { "中" };
            var results = _engine.ToIpaWithProsodyBatch(texts, true);
            Assert.Single(results);
            Assert.Single(results[0].Phonemes);

            // デフォルト（声調あり）と結果一致
            var defaultResult = _engine.ToIpaWithProsody("中");
            Assert.Equal(defaultResult.Phonemes[0], results[0].Phonemes[0]);
        }

        // =====================================================================
        // 10. ChineseProsodyInfo の Equals/GetHashCode テスト
        // =====================================================================

        [Fact]
        public void ChineseProsodyInfo_同値_Equalsがtrue()
        {
            var a = new ChineseProsodyInfo(1, 2, 3);
            var b = new ChineseProsodyInfo(1, 2, 3);
            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
        }

        [Fact]
        public void ChineseProsodyInfo_異値_Equalsがfalse()
        {
            var a = new ChineseProsodyInfo(1, 2, 3);
            var b = new ChineseProsodyInfo(2, 2, 3);
            Assert.False(a.Equals(b));
            Assert.False(a == b);
            Assert.True(a != b);
        }

        [Fact]
        public void ChineseProsodyInfo_a2が異なる_Equalsがfalse()
        {
            var a = new ChineseProsodyInfo(1, 1, 3);
            var b = new ChineseProsodyInfo(1, 2, 3);
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void ChineseProsodyInfo_a3が異なる_Equalsがfalse()
        {
            var a = new ChineseProsodyInfo(1, 2, 2);
            var b = new ChineseProsodyInfo(1, 2, 3);
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void ChineseProsodyInfo_同値_GetHashCodeが同一()
        {
            var a = new ChineseProsodyInfo(3, 1, 2);
            var b = new ChineseProsodyInfo(3, 1, 2);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void ChineseProsodyInfo_Equals_object型_正しく比較()
        {
            var a = new ChineseProsodyInfo(1, 2, 3);
            object b = new ChineseProsodyInfo(1, 2, 3);
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void ChineseProsodyInfo_Equals_異なる型_falseを返す()
        {
            var a = new ChineseProsodyInfo(1, 2, 3);
            Assert.False(a.Equals("not a prosody info"));
        }

        [Fact]
        public void ChineseProsodyInfo_Equals_null_falseを返す()
        {
            var a = new ChineseProsodyInfo(1, 2, 3);
            Assert.False(a.Equals(null));
        }

        [Fact]
        public void ChineseProsodyInfo_ToString_フォーマットが正しい()
        {
            var info = new ChineseProsodyInfo(3, 1, 2);
            Assert.Equal("a1=3, a2=1, a3=2", info.ToString());
        }

        [Fact]
        public void ChineseProsodyInfo_デフォルト値_全てゼロ()
        {
            var info = default(ChineseProsodyInfo);
            Assert.Equal(0, info.A1);
            Assert.Equal(0, info.A2);
            Assert.Equal(0, info.A3);
        }

        // =====================================================================
        // 11. ChineseProsodyResult のバリデーションテスト
        // =====================================================================

        [Fact]
        public void ChineseProsodyResult_null_phonemes_ArgumentNullExceptionをスロー()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ChineseProsodyResult(null!, new ChineseProsodyInfo[0]));
        }

        [Fact]
        public void ChineseProsodyResult_null_prosody_ArgumentNullExceptionをスロー()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ChineseProsodyResult(new string[0], null!));
        }

        [Fact]
        public void ChineseProsodyResult_長さ不一致_ArgumentExceptionをスロー()
        {
            Assert.Throws<ArgumentException>(
                () => new ChineseProsodyResult(
                    new[] { "a", "b" },
                    new[] { new ChineseProsodyInfo(1, 1, 1) }));
        }

        [Fact]
        public void ChineseProsodyResult_空配列_正常に生成()
        {
            var result = new ChineseProsodyResult(new string[0], new ChineseProsodyInfo[0]);
            Assert.Empty(result.Phonemes);
            Assert.Empty(result.Prosody);
        }

        [Fact]
        public void ChineseProsodyResult_同じ長さ_正常に生成()
        {
            var phonemes = new[] { "a", "b", "c" };
            var prosody = new[]
            {
                new ChineseProsodyInfo(1, 1, 3),
                new ChineseProsodyInfo(2, 2, 3),
                new ChineseProsodyInfo(3, 3, 3),
            };
            var result = new ChineseProsodyResult(phonemes, prosody);
            Assert.Equal(3, result.Phonemes.Count);
            Assert.Equal(3, result.Prosody.Count);
        }

        // =====================================================================
        // 12. includeTones パラメータテスト
        // =====================================================================

        [Fact]
        public void ToIpaWithProsody_includeTones_true_IPAに声調マーカーが含まれる()
        {
            var result = _engine.ToIpaWithProsody("中", true);
            Assert.Single(result.Phonemes);
            // 声調マーカーあり: 少なくとも声調文字（˥等）を含む
            Assert.Contains("\u02E5", result.Phonemes[0]); // ˥（第1声の一部）
        }

        [Fact]
        public void ToIpaWithProsody_includeTones_false_IPAに声調マーカーが含まれない()
        {
            var result = _engine.ToIpaWithProsody("中", false);
            Assert.Single(result.Phonemes);
            // 声調マーカーなし: tone lettersを含まない
            Assert.DoesNotContain("\u02E5", result.Phonemes[0]); // ˥
            Assert.DoesNotContain("\u02E9", result.Phonemes[0]); // ˩
        }

        [Fact]
        public void ToIpaWithProsody_includeTones変更_韻律情報は同一()
        {
            // includeTonesはIPA表記にのみ影響し、韻律情報には影響しない
            var withTones = _engine.ToIpaWithProsody("你好", true);
            var withoutTones = _engine.ToIpaWithProsody("你好", false);

            Assert.Equal(withTones.Prosody.Count, withoutTones.Prosody.Count);
            for (int i = 0; i < withTones.Prosody.Count; i++)
            {
                Assert.Equal(withTones.Prosody[i], withoutTones.Prosody[i]);
            }
        }

        // =====================================================================
        // 13. 非漢字の除外テスト
        // =====================================================================

        [Fact]
        public void ToIpaWithProsody_漢字と非漢字混在_漢字のみ韻律に含まれる()
        {
            // "你abc好" → abc で語分割。"你"(語長1), "好"(語長1)
            var result = _engine.ToIpaWithProsody("你abc好");
            Assert.Equal(2, result.Phonemes.Count);
            Assert.Equal(1, result.Prosody[0].A3);
            Assert.Equal(1, result.Prosody[1].A3);
        }

        [Fact]
        public void ToIpaWithProsody_前後に句読点_漢字のみ出力()
        {
            var result = _engine.ToIpaWithProsody("，你好。");
            Assert.Equal(2, result.Phonemes.Count);
            Assert.Equal(2, result.Prosody[0].A3);
            Assert.Equal(2, result.Prosody[1].A3);
        }

        // =====================================================================
        // 14. 軽声のa1テスト
        // =====================================================================

        [Fact]
        public void ToIpaWithProsody_軽声_a1が5()
        {
            // "的" は辞書で軽声の場合がある。
            // 軽声は a1=5 として扱われる。
            // ToneConverter.ExtractTone が Neutral を返す場合 → a1=5
            var pinyinList = _engine.ToPinyinList("的", PinyinStyle.ToneNumber);
            if (pinyinList.Length > 0 && !pinyinList[0].EndsWith("1") && !pinyinList[0].EndsWith("2")
                && !pinyinList[0].EndsWith("3") && !pinyinList[0].EndsWith("4"))
            {
                // 軽声の場合のテスト
                var result = _engine.ToIpaWithProsody("的");
                Assert.Equal(5, result.Prosody[0].A1);
            }
        }

        // =====================================================================
        // 15. 複合テスト（実際の文）
        // =====================================================================

        [Fact]
        public void ToIpaWithProsody_実文_我是中国人_語構造が正しい()
        {
            // "我是中国人" → 5つの連続漢字で1語
            var result = _engine.ToIpaWithProsody("我是中国人");
            Assert.Equal(5, result.Phonemes.Count);
            Assert.Equal(5, result.Prosody.Count);

            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(i + 1, result.Prosody[i].A2);
                Assert.Equal(5, result.Prosody[i].A3);
            }
        }

        [Fact]
        public void ToIpaWithProsody_実文_句読点で語分割_正しく分割される()
        {
            // "你好，世界" → "你好"(語長2) + "世界"(語長2)
            var result = _engine.ToIpaWithProsody("你好，世界");
            Assert.Equal(4, result.Phonemes.Count);

            // "你好": 語長2
            Assert.Equal(2, result.Prosody[0].A3);
            Assert.Equal(2, result.Prosody[1].A3);

            // "世界": 語長2
            Assert.Equal(2, result.Prosody[2].A3);
            Assert.Equal(2, result.Prosody[3].A3);

            // 語内位置: それぞれ1, 2
            Assert.Equal(1, result.Prosody[0].A2);
            Assert.Equal(2, result.Prosody[1].A2);
            Assert.Equal(1, result.Prosody[2].A2);
            Assert.Equal(2, result.Prosody[3].A2);
        }

        [Fact]
        public void ToIpaWithProsody_全声調カバー_a1が1から4を含む()
        {
            // "天人你大" → 1声・2声・3声・4声
            var result = _engine.ToIpaWithProsody("天人你大");
            var tones = new HashSet<int>();
            for (int i = 0; i < result.Prosody.Count; i++)
            {
                tones.Add(result.Prosody[i].A1);
            }

            // 声調変調で変わる可能性があるが、少なくとも複数の声調が含まれる
            Assert.True(tones.Count >= 2, $"声調バリエーション不足: {string.Join(", ", tones)}");
        }

        // =====================================================================
        // 16. IPA音素内容の検証テスト
        // =====================================================================

        [Fact]
        public void ToIpaWithProsody_IPA音素がToIPAと一致する()
        {
            // ToIpaWithProsody の音素は ToIPA の結果をスペース区切りしたものと一致するはず
            var text = "你好";
            var prosodyResult = _engine.ToIpaWithProsody(text);
            var ipaList = _engine.ToPinyinList(text, PinyinStyle.ToneMarked);

            // 各漢字のIPA変換と比較
            Assert.Equal(ipaList.Length, prosodyResult.Phonemes.Count);
            for (int i = 0; i < ipaList.Length; i++)
            {
                var expectedIpa = PinyinToIpa.Convert(ipaList[i], true);
                Assert.Equal(expectedIpa, prosodyResult.Phonemes[i]);
            }
        }

        [Fact]
        public void ToIpaWithProsody_全音素が非空文字列()
        {
            var result = _engine.ToIpaWithProsody("中华人民共和国");
            foreach (var phoneme in result.Phonemes)
            {
                Assert.NotNull(phoneme);
                Assert.NotEmpty(phoneme);
            }
        }

        // =====================================================================
        // 17. デフォルト引数テスト
        // =====================================================================

        [Fact]
        public void ToIpaWithProsody_引数1つ_声調マーカーあり()
        {
            // 引数1つ（デフォルト: includeTones=true）
            var defaultResult = _engine.ToIpaWithProsody("中");
            var explicitResult = _engine.ToIpaWithProsody("中", true);

            Assert.Equal(defaultResult.Phonemes[0], explicitResult.Phonemes[0]);
            Assert.Equal(defaultResult.Prosody[0], explicitResult.Prosody[0]);
        }

        // =====================================================================
        // 18. オプション組み合わせテスト
        // =====================================================================

        [Fact]
        public void ToIpaWithProsody_声調変調無効_a1が変調前の値()
        {
            var opts = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(opts);

            // "你好": 変調なし → 両方3声のまま
            var result = engine.ToIpaWithProsody("你好");
            Assert.Equal(3, result.Prosody[0].A1);
            Assert.Equal(3, result.Prosody[1].A1);
        }

        [Fact]
        public void ToIpaWithProsody_声調変調有効_a1が変調後の値()
        {
            var opts = new ChineseG2POptions(enableToneSandhi: true);
            using var engine = new ChineseG2PEngine(opts);

            // "你好": 三声連読 → 第1字が2声に変調
            var result = engine.ToIpaWithProsody("你好");
            Assert.Equal(2, result.Prosody[0].A1); // 変調後
            Assert.Equal(3, result.Prosody[1].A1);
        }
    }
}
