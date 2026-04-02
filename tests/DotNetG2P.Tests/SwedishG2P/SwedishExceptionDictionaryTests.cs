using System;
using DotNetG2P.Swedish;
using Xunit;

namespace DotNetG2P.Tests.SwedishG2P
{
    /// <summary>
    /// スウェーデン語例外辞書テスト。
    /// 例外辞書が有効な場合のエンジン出力を検証する。
    /// </summary>
    public class SwedishExceptionDictionaryTests : IDisposable
    {
        private readonly SwedishG2PEngine _engine;

        public SwedishExceptionDictionaryTests()
        {
            _engine = new SwedishG2PEngine(new SwedishG2POptions(
                enableExceptionDictionary: true,
                enableTextNormalization: false,
                includeStress: false));
        }

        public void Dispose() => _engine.Dispose();

        // =================================================================
        // 機能語テスト（完全一致）
        // =================================================================

        [Fact]
        public void ToIPA_Och_ExactMatch()
        {
            // "och" は接続詞、ch黙字 → 弱形 /ɔ/
            var result = _engine.ToIPA("och");
            Assert.Equal("\u0254", result); // ɔ
        }

        [Fact]
        public void ToIPA_Det_ExactMatch()
        {
            // "det" は冠詞/代名詞、弱形 /dɛ/
            var result = _engine.ToIPA("det");
            Assert.Equal("d\u025B", result); // dɛ
        }

        [Fact]
        public void ToIPA_De_ExactMatch()
        {
            // "de" は三人称複数主格、不規則 → /dɔm/
            var result = _engine.ToIPA("de");
            Assert.Equal("d\u0254m", result); // dɔm
        }

        [Fact]
        public void ToIPA_Mig_ExactMatch()
        {
            // "mig" は目的格、-ig→-ej → /mɛj/
            var result = _engine.ToIPA("mig");
            Assert.Equal("m\u025Bj", result); // mɛj
        }

        [Fact]
        public void ToIPA_Jag_ExactMatch()
        {
            // "jag" は主語代名詞、g脱落 → /jɑː/
            var result = _engine.ToIPA("jag");
            Assert.Equal("j\u0251\u02D0", result); // jɑː
        }

        // =================================================================
        // フランス語借用語テスト（完全一致）
        // =================================================================

        [Fact]
        public void ToIPA_Chef_ExactMatch()
        {
            // "chef" はフランス語借用語、ch→sj音 → /ɧeːf/
            var result = _engine.ToIPA("chef");
            Assert.Equal("\u0267e\u02D0f", result); // ɧeːf
        }

        // =================================================================
        // 英語借用語テスト
        // =================================================================

        [Fact]
        public void ToIPA_Show_ExactMatch()
        {
            // "show" は英語借用語 → /ɧoː/
            var result = _engine.ToIPA("show");
            Assert.Equal("\u0267o\u02D0", result); // ɧoː
        }

        // =================================================================
        // sj例外テスト（完全一致）
        // =================================================================

        [Fact]
        public void ToIPA_Station_ExactMatch()
        {
            // "station" は -tion→sj音 → /staɧuːn/（ストレスなし設定）
            var result = _engine.ToIPA("station");
            Assert.Equal("sta\u0267u\u02D0n", result); // staɧuːn
        }

        [Fact]
        public void ToIPA_Mission_ExactMatch()
        {
            // "mission" は -sion→sj音 → /mɪɧuːn/（ストレスなし設定）
            var result = _engine.ToIPA("mission");
            Assert.Equal("m\u026A\u0267u\u02D0n", result); // mɪɧuːn
        }

        // =================================================================
        // 軟音化例外テスト（完全一致）
        // =================================================================

        [Fact]
        public void ToIPA_Kille_ExactMatch()
        {
            // "kille" は軟音化例外、k+前舌母音でも硬音維持 → /kɪlɛ/
            var result = _engine.ToIPA("kille");
            Assert.Equal("k\u026Al\u025B", result); // kɪlɛ
        }

        // =================================================================
        // フォールバックテスト
        // =================================================================

        [Fact]
        public void ToIPA_NonexistentWord_FallsBackToRules()
        {
            // 辞書に存在しない語 → ルールベースG2Pにフォールバック
            var result = _engine.ToIPA("abcdef");
            Assert.False(string.IsNullOrEmpty(result), "存在しない語でもルールベースG2Pで出力されるべき");
        }

        // =================================================================
        // 辞書無効化テスト
        // =================================================================

        [Fact]
        public void ToIPA_DictionaryDisabled_OchNotHandledByDictionary()
        {
            // EnableExceptionDictionary=false の場合、"och" は辞書ではなくルールベースG2Pで処理される
            using var noDict = new SwedishG2PEngine(new SwedishG2POptions(
                enableExceptionDictionary: false,
                enableTextNormalization: false,
                includeStress: false));

            var withDict = _engine.ToIPA("och");
            var withoutDict = noDict.ToIPA("och");

            // 辞書有効時と無効時で結果が異なることを確認（辞書が実際に効いている）
            Assert.NotEqual(withDict, withoutDict);
        }

        // =================================================================
        // 方言フィルタテスト
        // =================================================================

        [Fact]
        public void ToIPA_DialectWildcard_WorksForBothDialects()
        {
            // dialect=* のエントリはCentral/FinlandSwedish両方で使える
            using var central = new SwedishG2PEngine(new SwedishG2POptions(
                dialect: SwedishDialect.Central,
                enableExceptionDictionary: true,
                enableTextNormalization: false,
                includeStress: false));
            using var finland = new SwedishG2PEngine(new SwedishG2POptions(
                dialect: SwedishDialect.FinlandSwedish,
                enableExceptionDictionary: true,
                enableTextNormalization: false,
                includeStress: false));

            // "och" はdialect=* → 両方で同じ結果
            var centralResult = central.ToIPA("och");
            var finlandResult = finland.ToIPA("och");
            Assert.Equal(centralResult, finlandResult);
            Assert.Equal("\u0254", centralResult); // ɔ
        }
    }
}
