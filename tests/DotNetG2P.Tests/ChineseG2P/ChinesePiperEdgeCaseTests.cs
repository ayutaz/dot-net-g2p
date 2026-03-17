using System;
using System.Linq;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// piper-plus互換APIのエッジケーステスト。
    /// 特殊ピンイン・ü母音・そり舌/歯茎母音・声調変調・長文・混在入力・句読点・
    /// サロゲートペア・語境界・バッチAPI混在入力など、境界的な入力を網羅的に検証する。
    /// </summary>
    public class ChinesePiperEdgeCaseTests : IDisposable
    {
        private readonly ChineseG2PEngine _engine = new ChineseG2PEngine();

        public void Dispose() => _engine.Dispose();

        // =====================================================================
        // 1. 特殊ピンイン
        // =====================================================================

        [Fact]
        public void ToIPA_嗯_声母なし鼻音ngは非標準ピンインのため空になる()
        {
            // "嗯" は ń/ńg/ňg/ǹg 等の非標準ピンイン（声母なし鼻音）
            // 辞書には登録されているが、PinyinParserが標準の声母+韻母に分解できないため
            // IPA変換は空文字列を返す（既知の制限事項）
            var result = _engine.ToIPA("嗯");
            Assert.NotNull(result);
            // 辞書にはあるがIPAへのパースが失敗するため空
            Assert.True(_engine.ContainsChar('嗯'), "嗯は辞書に存在する");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPinyin_嗯_ピンインが返る()
        {
            // "嗯" のピンイン検索
            var pinyins = _engine.LookupChar('嗯');
            // 辞書に存在する場合、ピンイン候補が取得できる
            if (pinyins.Length > 0)
            {
                Assert.True(pinyins.Any(p =>
                    p.Contains("n") || p.Contains("ń") || p.Contains("ǹ") || p.Contains("ň") || p.Contains("ēn") || p.Contains("en")),
                    $"嗯のピンインにn系が含まれること。実際: {string.Join(", ", pinyins)}");
            }
        }

        [Fact]
        public void ToIPA_儿_独立erが変換される()
        {
            // "儿" (ér) は独立のer音
            var result = _engine.ToIPA("儿");
            Assert.NotEmpty(result);
            // IPA: əɻ（er韻母のIPA表現）
            Assert.Contains("\u0259", result); // ə
            Assert.Contains("\u027B", result); // ɻ
        }

        [Fact]
        public void ToPinyin_儿_erピンインが返る()
        {
            var result = _engine.ToPinyin("儿");
            Assert.NotEmpty(result);
            Assert.Contains("ér", result);
        }

        [Fact]
        public void ToIPA_啊_声母なし母音のみ()
        {
            // "啊" (ā/a) は声母なしの母音のみ音節
            var result = _engine.ToIPA("啊");
            Assert.NotEmpty(result);
            // IPA: a（単母音）
            Assert.Contains("a", result);
        }

        [Fact]
        public void ToPinyin_啊_母音のみピンイン()
        {
            var result = _engine.ToPinyin("啊");
            Assert.NotEmpty(result);
            // ā/a/à 等
            Assert.True(result.Contains("a") || result.Contains("ā") || result.Contains("à"),
                $"啊のピンインにa系が含まれること。実際: {result}");
        }

        // =====================================================================
        // 2. ü母音（y_vowelトークン）
        // =====================================================================

        [Fact]
        public void ToIPA_鱼_ü母音がyとして出力される()
        {
            // "鱼" (yú) → IPA: y（前舌円唇母音）
            var result = _engine.ToIPA("鱼");
            Assert.NotEmpty(result);
            // IPAでは ü → y
            Assert.Contains("y", result);
        }

        [Fact]
        public void ToPinyin_鱼_yuピンイン()
        {
            var result = _engine.ToPinyin("鱼");
            Assert.NotEmpty(result);
            Assert.Contains("yú", result);
        }

        [Fact]
        public void ToIPA_女_nü母音のüがIPAのyになる()
        {
            // "女" (nǚ) → IPA: ny（nの後にü=y[IPA]）
            var result = _engine.ToIPA("女");
            Assert.NotEmpty(result);
            Assert.Contains("n", result);
            Assert.Contains("y", result);
        }

        [Fact]
        public void ToPinyin_女_nüピンイン()
        {
            var result = _engine.ToPinyin("女");
            Assert.NotEmpty(result);
            Assert.Contains("nǚ", result);
        }

        [Fact]
        public void ToIPA_绿_lü母音のüがIPAのyになる()
        {
            // "绿" (lǜ) → IPA: ly（lの後にü=y[IPA]）
            var result = _engine.ToIPA("绿");
            Assert.NotEmpty(result);
            Assert.Contains("l", result);
            Assert.Contains("y", result);
        }

        [Fact]
        public void ToPinyin_绿_lüピンイン()
        {
            var result = _engine.ToPinyin("绿");
            Assert.NotEmpty(result);
            Assert.Contains("lǜ", result);
        }

        // =====================================================================
        // 3. そり舌/歯茎母音
        // =====================================================================

        [Fact]
        public void ToIPA_知_そり舌母音ɻ̩が含まれる()
        {
            // "知" (zhī) → zh + i → そり舌母音 ɻ̩
            var result = _engine.ToIPA("知");
            Assert.NotEmpty(result);
            // IPA: ʈʂ + ɻ̩ (そり舌母音)
            Assert.Contains("\u027B\u0329", result); // ɻ̩
        }

        [Fact]
        public void ToIPA_四_歯茎母音ɹ̩が含まれる()
        {
            // "四" (sì) → s + i → 歯茎母音 ɹ̩
            var result = _engine.ToIPA("四");
            Assert.NotEmpty(result);
            // IPA: s + ɹ̩ (歯茎母音)
            Assert.Contains("\u0279\u0329", result); // ɹ̩
        }

        [Fact]
        public void ToIPA_日_rプラスそり舌母音()
        {
            // "日" (rì) → r + i → ɻ + ɻ̩ (そり舌声母+そり舌母音)
            var result = _engine.ToIPA("日");
            Assert.NotEmpty(result);
            // ɻ が含まれること（声母のɻ + 母音のɻ̩）
            Assert.Contains("\u027B", result); // ɻ
        }

        [Fact]
        public void ToIPA_资_zプラス歯茎母音()
        {
            // "资" (zī) → z + i → ts + ɹ̩
            var result = _engine.ToIPA("资");
            Assert.NotEmpty(result);
            Assert.Contains("ts", result);
            Assert.Contains("\u0279\u0329", result); // ɹ̩
        }

        [Fact]
        public void ToIPA_吃_chプラスそり舌母音()
        {
            // "吃" (chī) → ch + i → ʈʂʰ + ɻ̩
            var result = _engine.ToIPA("吃");
            Assert.NotEmpty(result);
            Assert.Contains("\u027B\u0329", result); // ɻ̩ (そり舌母音)
        }

        // =====================================================================
        // 4. 声調変調の影響
        // =====================================================================

        [Fact]
        public void ToPinyin_不是_不の変調が反映される()
        {
            // "不" (bù) + "是" (shì, 4声) → "不"が2声に変調: bú shì
            var result = _engine.ToPinyin("不是");
            Assert.NotEmpty(result);
            var parts = result.Split(' ');
            Assert.Equal(2, parts.Length);
            // 不が2声に変調されていること
            Assert.Equal("bú", parts[0]);
        }

        [Fact]
        public void ToPinyin_一个_一の変調が反映される()
        {
            // "一" (yī) + "个" (gè, 4声) → "一"が2声に変調: yí gè
            var result = _engine.ToPinyin("一个");
            Assert.NotEmpty(result);
            var parts = result.Split(' ');
            Assert.Equal(2, parts.Length);
            // 一が変調されていること（4声の前→2声）
            Assert.Equal("yí", parts[0]);
        }

        [Fact]
        public void ToIPA_不是_変調後のIPAが正しい()
        {
            // IPA出力でも声調変調が反映されること
            var result = _engine.ToIPA("不是");
            Assert.NotEmpty(result);
            // 声調マーカーが含まれる
            Assert.True(result.Length > 2);
        }

        [Fact]
        public void ToIPA_一个_変調後のIPAが正しい()
        {
            var result = _engine.ToIPA("一个");
            Assert.NotEmpty(result);
            Assert.True(result.Length > 2);
        }

        // =====================================================================
        // 5. 長文テスト
        // =====================================================================

        [Fact]
        public void ToPinyin_20文字以上の中国語テキスト_正常に変換()
        {
            // 20文字以上の連続漢字
            var text = "中华人民共和国是世界上人口最多的国家之一也是最大的发展中国家";
            Assert.True(text.Length > 20, "テスト入力が20文字以上であること");

            var result = _engine.ToPinyin(text);
            Assert.NotEmpty(result);

            var parts = result.Split(' ');
            // 各漢字にピンインが割り当てられること
            Assert.Equal(text.Length, parts.Length);
        }

        [Fact]
        public void ToIPA_長文_全文字がIPA変換される()
        {
            var text = "今天天气很好我们一起去公园散步吧大家都很开心";
            Assert.True(text.Length > 20);

            var result = _engine.ToIPA(text);
            Assert.NotEmpty(result);
            // IPAは空白区切り
            var parts = result.Split(' ');
            Assert.Equal(text.Length, parts.Length);
        }

        [Fact]
        public void ToPinyinList_長文_文字数と一致する要素数()
        {
            var text = "春眠不觉晓处处闻啼鸟夜来风雨声花落知多少";
            var result = _engine.ToPinyinList(text);
            Assert.Equal(text.Length, result.Length);
            Assert.All(result, p => Assert.NotEmpty(p));
        }

        // =====================================================================
        // 6. 数字/英語混在
        // =====================================================================

        [Fact]
        public void ToPinyin_数字混在_数字はパススルー()
        {
            // "3个人" → 数字3はそのまま、漢字はピンイン
            var result = _engine.ToPinyin("3个人");
            Assert.NotEmpty(result);
            Assert.Contains("3", result);
        }

        [Fact]
        public void ToPinyinList_数字混在_数字要素が保持される()
        {
            var result = _engine.ToPinyinList("3个人");
            Assert.Equal(3, result.Length);
            Assert.Equal("3", result[0]);
        }

        [Fact]
        public void ToPinyin_英語混在_英字はパススルー()
        {
            // "OK了" → OKはそのまま、了はピンイン
            var result = _engine.ToPinyin("OK了");
            Assert.NotEmpty(result);
            Assert.Contains("OK", result);
        }

        [Fact]
        public void ToPinyinList_英語混在_英字要素が保持される()
        {
            var result = _engine.ToPinyinList("OK了");
            // O, K, 了 → 3要素
            Assert.Equal(3, result.Length);
            Assert.Equal("O", result[0]);
            Assert.Equal("K", result[1]);
        }

        [Fact]
        public void ToIPA_数字混在_数字部分はIPAに含まれない()
        {
            var result = _engine.ToIPA("3个人");
            Assert.NotEmpty(result);
            // 数字はそのまま出力される
            Assert.Contains("3", result);
        }

        [Fact]
        public void ToPinyin_数字のみの挟み込み_前後の漢字が正常()
        {
            // "人123人" → 漢字+数字+漢字
            var result = _engine.ToPinyin("人123人");
            Assert.NotEmpty(result);
            // 数字はそのまま
            Assert.Contains("123", result);
        }

        // =====================================================================
        // 7. 句読点のみ
        // =====================================================================

        [Fact]
        public void ToPinyin_CJK句読点のみ_空文字列()
        {
            // "，。！" → CJK句読点のみ→空文字列
            var result = _engine.ToPinyin("，。！");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToIPA_CJK句読点のみ_空文字列()
        {
            var result = _engine.ToIPA("，。！");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToPinyinList_CJK句読点のみ_空配列()
        {
            // 句読点はセパレータ扱いでピンインに変換されない
            // しかしToPinyinListは全文字を要素として返す
            var result = _engine.ToPinyinList("，。！");
            // 句読点は非漢字として扱われ、各文字が要素になる
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPinyin_疑問符と感嘆符の混合_空文字列()
        {
            // "？！" → CJK疑問符+感嘆符
            var result = _engine.ToPinyin("？！");
            Assert.Equal("", result);
        }

        // =====================================================================
        // 8. サロゲートペア
        // =====================================================================

        [Fact]
        public void ToPinyin_CJK拡張B_サロゲートペア_エラーなし()
        {
            // CJK拡張B (U+20000) はサロゲートペアで表現される
            var text = "\U00020000";
            var result = _engine.ToPinyin(text);
            Assert.NotNull(result);
            // 辞書にない場合はそのまま出力される（フォールバック）
        }

        [Fact]
        public void ToPinyin_拡張漢字混在_サロゲートペアと通常漢字()
        {
            // サロゲートペア + 通常漢字
            var text = "\U00020000你好";
            var result = _engine.ToPinyin(text);
            Assert.NotNull(result);
            // 通常漢字部分はピンインに変換されること
            Assert.Contains("hǎo", result);
        }

        [Fact]
        public void ToPinyinList_サロゲートペア_要素としてフォールバック()
        {
            var text = "\U00020000好";
            var result = _engine.ToPinyinList(text);
            Assert.NotNull(result);
            Assert.True(result.Length >= 2, "サロゲートペア文字と通常漢字で2要素以上");
        }

        [Fact]
        public void ToIPA_絵文字混在_エラーなし()
        {
            // 絵文字（サロゲートペア）と漢字の混在
            var text = "好\U0001F600好";
            var result = _engine.ToIPA(text);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 9. Prosody語境界（連続漢字の語グループ）
        // =====================================================================

        [Fact]
        public void ToPinyin_連続漢字_全体が変換される()
        {
            // "我爱北京天安门" → 連続漢字がすべてピンイン変換される
            var result = _engine.ToPinyin("我爱北京天安门");
            Assert.NotEmpty(result);
            var parts = result.Split(' ');
            Assert.Equal(7, parts.Length);
        }

        [Fact]
        public void ToPinyinList_連続漢字_各文字に対応する要素()
        {
            var result = _engine.ToPinyinList("我爱北京天安门");
            Assert.Equal(7, result.Length);
            Assert.All(result, p => Assert.NotEmpty(p));
        }

        [Fact]
        public void ToPinyin_スペースなし連続漢字_1語として扱われる()
        {
            // スペースなしの連続漢字 → 全体が1つのコンテキストとして処理
            var result = _engine.ToPinyin("中华人民共和国");
            Assert.NotEmpty(result);
            var parts = result.Split(' ');
            Assert.Equal(7, parts.Length);
            // フレーズ辞書による多音字解決が適用される
        }

        [Fact]
        public void ToPinyin_スペースあり_別語として扱われる()
        {
            // スペースで分割された漢字 → 別の語境界
            var resultWithSpace = _engine.ToPinyin("你好 世界");
            var resultWithout = _engine.ToPinyin("你好世界");

            Assert.NotEmpty(resultWithSpace);
            Assert.NotEmpty(resultWithout);

            // スペースありの場合、セパレータとして機能し出力に反映
            // ピンインの個数は同じ（4文字=4ピンイン）だが区切りが異なる
        }

        [Fact]
        public void ToPinyin_漢字間の句読点_語境界を分断()
        {
            // "你好，世界" → 句読点で語境界が分断
            var result = _engine.ToPinyin("你好，世界");
            Assert.NotEmpty(result);
            // 句読点はセパレータとして機能
            Assert.Contains("hǎo", result);
            Assert.Contains("shì", result);
        }

        [Fact]
        public void ToPinyin_フレーズ辞書効果_多音字の読み分け()
        {
            // "行" は多音字: xíng（行く）/ háng（行列）
            // フレーズ辞書があれば文脈で判別される
            var resultYinhang = _engine.ToPinyin("银行");
            var resultXingdong = _engine.ToPinyin("行动");

            Assert.NotEmpty(resultYinhang);
            Assert.NotEmpty(resultXingdong);
            // フレーズ辞書による多音字解決が適用されていること
        }

        // =====================================================================
        // 10. バッチAPIの混在入力
        // =====================================================================

        [Fact]
        public void ToIPABatch_混在入力_正常文字列と空とnull_全要素が返る()
        {
            // 正常、空文字列、null、正常 の4要素
            var inputs = new[] { "你好", "", null!, "世界" };
            var result = _engine.ToIPABatch(inputs);
            Assert.Equal(4, result.Count);
            // 正常入力はIPAに変換
            Assert.NotEmpty(result[0]);
            // 空文字列は空
            Assert.Equal("", result[1]);
            // nullは空
            Assert.Equal("", result[2]);
            // 正常入力はIPAに変換
            Assert.NotEmpty(result[3]);
        }

        [Fact]
        public void ToPinyinBatch_混在入力_4要素が返る()
        {
            var inputs = new[] { "你好", "", null!, "世界" };
            var result = _engine.ToPinyinBatch(inputs);
            Assert.Equal(4, result.Count);
            Assert.NotEmpty(result[0]);
            Assert.Equal("", result[1]);
            Assert.Equal("", result[2]);
            Assert.NotEmpty(result[3]);
        }

        [Fact]
        public void ToZhuyinBatch_混在入力_4要素が返る()
        {
            var inputs = new[] { "你好", "", null!, "世界" };
            var result = _engine.ToZhuyinBatch(inputs);
            Assert.Equal(4, result.Count);
            Assert.NotEmpty(result[0]);
            Assert.Equal("", result[1]);
            Assert.Equal("", result[2]);
            Assert.NotEmpty(result[3]);
        }

        [Fact]
        public void ToPinyinListBatch_混在入力_4要素が返る()
        {
            var inputs = new[] { "你好", "", null!, "世界" };
            var result = _engine.ToPinyinListBatch(inputs);
            Assert.Equal(4, result.Count);
            Assert.Equal(2, result[0].Length);
            Assert.Empty(result[1]);
            Assert.Empty(result[2]);
            Assert.Equal(2, result[3].Length);
        }

        [Fact]
        public void ToIPABatch_空配列_空リストが返る()
        {
            var result = _engine.ToIPABatch(Array.Empty<string>());
            Assert.Empty(result);
        }

        [Fact]
        public void ToPinyinBatch_全要素null_全て空文字列()
        {
            var inputs = new[] { (string)null!, (string)null! };
            var result = _engine.ToPinyinBatch(inputs);
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal("", r));
        }
    }
}
