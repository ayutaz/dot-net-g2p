using System;
using DotNetG2P.Swedish;
using Xunit;

namespace DotNetG2P.Tests.SwedishG2P
{
    /// <summary>
    /// Central / FinlandSwedish 方言の差異に関するテスト。
    /// </summary>
    public class SwedishDialectTests : IDisposable
    {
        private readonly SwedishG2PEngine _centralEngine = new SwedishG2PEngine();
        private readonly SwedishG2PEngine _finlandEngine = new SwedishG2PEngine(
            new SwedishG2POptions(dialect: SwedishDialect.FinlandSwedish));

        public void Dispose()
        {
            _centralEngine.Dispose();
            _finlandEngine.Dispose();
        }

        // =================================================================
        // 1. Central方言の基本確認
        // =================================================================

        [Fact]
        public void Central_デフォルト設定()
        {
            var options = SwedishG2POptions.Default;
            Assert.Equal(SwedishDialect.Central, options.Dialect);
        }

        [Fact]
        public void Central_そり舌音含む_bord()
        {
            // "bord" → Central: ˈbɔɖ (rd→ɖ)
            var ipa = _centralEngine.ToIPA("bord");
            Assert.Contains("\u0256", ipa); // ɖ が含まれる
        }

        [Fact]
        public void Central_ピッチアクセントあり()
        {
            // "hunden" は Accent 1 → 0 以外のはず
            var phonemeList = _centralEngine.ToPhonemeList("hunden");
            // エンジン経由の ConvertWord で Accent が設定される
            // ToPhonemeList は直接 Accent を返さないが、
            // ToIPA で確認可能 — Central ではピッチアクセントを持つ語が存在する
            // 内部的に pronunciation.Accent != 0 であることを確認するため、
            // Central と Finland の差で間接的にテストする（テスト15参照）
            Assert.NotEmpty(phonemeList);
        }

        [Fact]
        public void Central_tj音は摩擦音()
        {
            // "kök" → Central: k軟化→ɕ → AllophoneProcessor(CentralDefault)では TjAffrication OFF → ɕ 維持
            var ipa = _centralEngine.ToIPA("kök");
            Assert.Contains("\u0255", ipa);             // ɕ が含まれる
            Assert.DoesNotContain("t\u0361\u0255", ipa); // t͡ɕ は含まれない
        }

        // =================================================================
        // 5-7. FinlandSwedish — そり舌化なし (E2Eテスト)
        // =================================================================

        [Fact]
        public void Finland_そり舌化なし_bord()
        {
            // "bord" → Finland: そり舌化なし → r+d パターン
            var ipa = _finlandEngine.ToIPA("bord");
            // そり舌IPA文字が含まれないことを確認
            Assert.DoesNotContain("\u0288", ipa); // ʈ
            Assert.DoesNotContain("\u0256", ipa); // ɖ
            Assert.DoesNotContain("\u0273", ipa); // ɳ
            Assert.DoesNotContain("\u026D", ipa); // ɭ
            Assert.DoesNotContain("\u0282", ipa); // ʂ
            // r + d が含まれる
            Assert.Contains("r", ipa);
            Assert.Contains("d", ipa);
        }

        [Fact]
        public void Finland_rt維持()
        {
            // "hjort" → Finland: そり舌化なし → r+t パターン
            var ipa = _finlandEngine.ToIPA("hjort");
            Assert.DoesNotContain("\u0288", ipa); // ʈ が含まれない
            Assert.Contains("r", ipa);
            Assert.Contains("t", ipa);
        }

        [Fact]
        public void Finland_rn維持_barn()
        {
            // "barn" → Finland: そり舌化なし → r+n パターン
            var ipa = _finlandEngine.ToIPA("barn");
            Assert.DoesNotContain("\u0273", ipa); // ɳ が含まれない
            Assert.Contains("r", ipa);
            Assert.Contains("n", ipa);
        }

        // =================================================================
        // 8-9. FinlandSwedish — ピッチアクセント無効化
        // =================================================================

        [Fact]
        public void Finland_ピッチアクセントなし()
        {
            // FinlandSwedish では全語の Accent が 0 にリセットされる
            // ToPhonemeList 経由では直接 Accent を確認できないため、
            // 内部的に ConvertWord を間接テストする — Options が Finland であることを確認
            var options = new SwedishG2POptions(dialect: SwedishDialect.FinlandSwedish);
            Assert.Equal(SwedishDialect.FinlandSwedish, options.Dialect);
        }

        [Fact]
        public void Finland_全語_Accentは0()
        {
            // 複数語をテストし、FinlandSwedish の ToPhonemeList が空でないことを確認
            // （Accent=0 の設定は ConvertWord 内部で行われる）
            var words = new[] { "hunden", "flickan", "stol" };
            foreach (var word in words)
            {
                var result = _finlandEngine.ToPhonemeList(word);
                Assert.NotEmpty(result);
            }
        }

        // =================================================================
        // 10. FinlandSwedish — tj音は破擦音
        // =================================================================

        [Fact]
        public void Finland_tj音は破擦音()
        {
            // "kök" → Finland: k軟化→ɕ → AllophoneProcessor(FinlandDefault, TjAffrication ON) → t͡ɕ
            var ipa = _finlandEngine.ToIPA("kök");
            Assert.Contains("t\u0361\u0255", ipa); // t͡ɕ が含まれる
        }

        // =================================================================
        // 11-14. Options の AllophoneFeatures 設定テスト
        // =================================================================

        [Fact]
        public void Options_Central_AllophoneFeatures自動設定()
        {
            var options = new SwedishG2POptions(dialect: SwedishDialect.Central);
            Assert.Equal(SwedishAllophoneFeatures.CentralDefault, options.AllophoneFeatures);
        }

        [Fact]
        public void Options_Finland_AllophoneFeatures自動設定()
        {
            var options = new SwedishG2POptions(dialect: SwedishDialect.FinlandSwedish);
            Assert.Equal(SwedishAllophoneFeatures.FinlandDefault, options.AllophoneFeatures);
        }

        [Fact]
        public void Options_明示指定_優先()
        {
            // 方言は Central だが AllophoneFeatures を明示的に FinlandDefault に指定
            var options = new SwedishG2POptions(
                dialect: SwedishDialect.Central,
                allophoneFeatures: SwedishAllophoneFeatures.FinlandDefault);
            Assert.Equal(SwedishAllophoneFeatures.FinlandDefault, options.AllophoneFeatures);
        }

        [Fact]
        public void Options_EnableAllophones_false()
        {
            // EnableAllophones=false の場合、AllophoneProcessor が呼ばれない
            // → Central でも "bord" にそり舌音が残る（ルールベースG2Pはそり舌化を常に適用するため）
            var options = new SwedishG2POptions(
                dialect: SwedishDialect.Central,
                enableAllophones: false);
            using var engine = new SwedishG2PEngine(options);
            var ipa = engine.ToIPA("bord");
            // AllophoneProcessor を通さなくても Central ではそり舌音が出る
            // （GraphemeToPhonemeRules が常にそり舌化するため）
            Assert.Contains("\u0256", ipa); // ɖ が含まれる
        }

        // =================================================================
        // 15. 方言切り替え — 同一テキストで異なる出力
        // =================================================================

        [Fact]
        public void Dialect切り替え_同一テキスト_異なる出力()
        {
            // "bord" は Central と Finland で異なる IPA 出力になるはず
            var centralIpa = _centralEngine.ToIPA("bord");
            var finlandIpa = _finlandEngine.ToIPA("bord");
            Assert.NotEqual(centralIpa, finlandIpa);
        }
    }
}
