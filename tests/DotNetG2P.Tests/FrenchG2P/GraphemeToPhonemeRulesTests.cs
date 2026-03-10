using DotNetG2P.French;
using DotNetG2P.French.Rules;

namespace DotNetG2P.Tests.FrenchG2P
{
    /// <summary>
    /// GraphemeToPhonemeRules の単体テスト。
    /// FrenchG2PEngine.ToIPA() 経由で検証する（ConvertWord は internal）。
    /// デフォルト方言は Metropolitan（Ah→A, OeNasal→ENasal 統合）。
    /// </summary>
    public class GraphemeToPhonemeRulesTests : IDisposable
    {
        private readonly FrenchG2PEngine _engine = new FrenchG2PEngine(
            new FrenchG2POptions(enableExceptionDictionary: false));
        private readonly FrenchG2PEngine _conservativeEngine = new FrenchG2PEngine(
            new FrenchG2POptions(dialect: FrenchDialect.Conservative, enableExceptionDictionary: false));

        // ========== 母音規則 (~10件) ==========

        [Theory]
        // 'a' → /a/
        [InlineData("ami", "ami")]
        // 'é' → /e/
        [InlineData("\u00E9t\u00E9", "ete")]
        // 'ê' → /ɛ/
        [InlineData("b\u00EAte", "b\u025Bt")]
        // 'u' → /y/
        [InlineData("lune", "lyn")]
        // 'i' → /i/
        [InlineData("lit", "li")]
        // 'î' → /i/
        [InlineData("\u00EEle", "il")]
        // 'ô' → /o/
        [InlineData("c\u00F4te", "kot")]
        // 'o' 開音節（語末t黙字で開音節扱い）→ /o/
        [InlineData("mot", "mo")]
        // 'à' → /a/
        [InlineData("l\u00E0", "la")]
        // 'æ' → /e/
        [InlineData("\u00E6", "e")]
        public void ToIPA_Vowels_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== 鼻母音規則 (~10件) ==========

        [Theory]
        // "an" + 語末子音 → /ɑ̃/ + 語末cはCaReFuL→/k/
        [InlineData("banc", "b\u0251\u0303k")]
        // "in" + 語末 → /ɛ̃/
        [InlineData("vin", "v\u025B\u0303")]
        // "on" + 語末 → /ɔ̃/
        [InlineData("bon", "b\u0254\u0303")]
        // "bonne" → nn は非鼻母音化 → b + ɔ + n + 語末e黙字
        [InlineData("bonne", "b\u0254n")]
        // "en" + t語末黙字 → /ɑ̃/
        [InlineData("vent", "v\u0251\u0303")]
        // "un" → u+n: Metropolitan → /ɛ̃/
        [InlineData("brun", "b\u0281\u025B\u0303")]
        // "ain" 語末 → /ɛ̃/
        [InlineData("pain", "p\u025B\u0303")]
        // "oin" 語末 → /wɛ̃/
        [InlineData("coin", "kw\u025B\u0303")]
        // "an" + 母音 → 非鼻母音化（a + n）
        [InlineData("cane", "kan")]
        // "on" + i → 非鼻母音化 (o + n + i)
        [InlineData("boni", "boni")]
        public void ToIPA_NasalVowels_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== 子音規則 (~10件) ==========

        [Theory]
        // "ch" → /ʃ/
        [InlineData("chat", "\u0283a")]
        // "c" + e/i → /s/
        [InlineData("cent", "s\u0251\u0303")]
        // "c" + a → /k/
        [InlineData("car", "ka\u0281")]
        // "g" + e → /ʒ/（geste: g→ʒ, e→ə, s→s, t→t, e語末→schwa）
        [InlineData("geste", "\u0292\u0259st")]
        // "g" + a → /ɡ/
        [InlineData("gare", "\u0261a\u0281")]
        // "j" → /ʒ/
        [InlineData("jour", "\u0292u\u0281")]
        // "ph" → /f/
        [InlineData("photo", "foto")]
        // "gn" → /ɲ/
        [InlineData("ligne", "li\u0272")]
        // "ç" → /s/
        [InlineData("gar\u00E7on", "\u0261a\u0281s\u0254\u0303")]
        // "r" → /ʁ/
        [InlineData("riz", "\u0281i")]
        public void ToIPA_Consonants_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== ダイグラフ・トリグラフ (~8件) ==========

        [Theory]
        // "eau" → /o/
        [InlineData("eau", "o")]
        // "oi" → /wa/
        [InlineData("bois", "bwa")]
        // "au" → /o/
        [InlineData("auto", "oto")]
        // "ou" → /u/
        [InlineData("bout", "bu")]
        // "ai" → /ɛ/
        [InlineData("fait", "f\u025B")]
        // "ei" → /ɛ/（reine: ʁ+ɛ+n, 語末e黙字）
        [InlineData("reine", "\u0281\u025Bn")]
        // "qu" → /k/
        [InlineData("quel", "k\u0259l")]
        // "th" → /t/
        [InlineData("the", "t\u0259")]
        public void ToIPA_Digraphs_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== -tion/-sion/-ill- パターン (~8件) ==========

        [Theory]
        // "-tion" 語末 → /sjɔ̃/（na-tion: n+a+sj+ɔ̃）
        [InlineData("nation", "nasj\u0254\u0303")]
        // "-ille" 語末 → /ij/
        [InlineData("fille", "fij")]
        // "-ille" 語末（母音+ille: famille → f+a+m+ij）
        [InlineData("famille", "famij")]
        // "-aille" 語末 → /aj/
        [InlineData("aille", "aj")]
        // "-eille" 語末 → /ɛj/
        [InlineData("eille", "\u025Bj")]
        // "-ouille" 語末 → /uj/
        [InlineData("fouille", "fuj")]
        // "-ssion" 語末 → /sjɔ̃/（pa-ssion: p+a+sj+ɔ̃）
        [InlineData("passion", "pasj\u0254\u0303")]
        // "-sion" 語末 → /zjɔ̃/（fu-sion: f+y+zj+ɔ̃）
        [InlineData("fusion", "fyzj\u0254\u0303")]
        public void ToIPA_SpecialPatterns_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== 黙字規則 (~8件) ==========

        [Theory]
        // 語末 -t → 黙字
        [InlineData("petit", "p\u0259ti")]
        // 語末 -ez → /e/
        [InlineData("parlez", "pa\u0281le")]
        // 語末子音 → table: t+a+b+l（語末eは先行に母音あるので黙字）
        [InlineData("table", "tabl")]
        // 語末 -d → 黙字
        [InlineData("grand", "\u0261\u0281\u0251\u0303")]
        // 語末 -s → 黙字
        [InlineData("gros", "\u0261\u0281o")]
        // 語末 -x → 黙字
        [InlineData("voix", "vwa")]
        // 語末 -z → 黙字
        [InlineData("nez", "ne")]
        // 'h' → 黙字
        [InlineData("homme", "\u0254m")]
        public void ToIPA_SilentLetters_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== 位置の法則 (~5件) ==========

        [Theory]
        // "eu" 開音節 → /ø/
        [InlineData("feu", "f\u00F8")]
        // "eu" 閉音節 → /œ/
        [InlineData("seul", "s\u0153l")]
        // "o" 閉音節 → /ɔ/
        [InlineData("porte", "p\u0254\u0281t")]
        // 語末 -er → /e/
        [InlineData("parler", "pa\u0281le")]
        // 語末 -et → /ɛ/
        [InlineData("billet", "bil\u025B")]
        public void ToIPA_PositionalRules_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== 半母音化 (~4件) ==========

        [Theory]
        // i + 母音 → /j/ + 母音（半母音化）
        [InlineData("piano", "pjano")]
        // u(/y/) + 母音 → /ɥ/ + 母音
        [InlineData("nuit", "n\u0265i")]
        // ou(/u/) + i → /w/ + /i/
        [InlineData("oui", "wi")]
        // 語頭 y + 母音 → /j/
        [InlineData("yeux", "j\u00F8")]
        public void ToIPA_Semivowelization_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== 方言差異 (~4件) ==========

        [Fact]
        public void ToIPA_Metropolitan_MergesAhToA()
        {
            // â → Ah → Metropolitan merger → A → "a"
            var result = _engine.ToIPA("\u00E2me");
            Assert.Equal("am", result);
        }

        [Fact]
        public void ToIPA_Conservative_PreservesAh()
        {
            // â → Ah → Conservative: ɑ
            var result = _conservativeEngine.ToIPA("\u00E2me");
            Assert.Equal("\u0251m", result);
        }

        [Fact]
        public void ToIPA_Metropolitan_MergesOeNasalToENasal()
        {
            // "un" → Metropolitan: ENasal (ɛ̃)
            var result = _engine.ToIPA("brun");
            Assert.Equal("b\u0281\u025B\u0303", result);
        }

        [Fact]
        public void ToIPA_Conservative_PreservesOeNasal()
        {
            // "un" → Conservative: OeNasal (œ̃)
            var result = _conservativeEngine.ToIPA("brun");
            Assert.Equal("b\u0281\u0153\u0303", result);
        }

        // ========== 重複子音 (~6件) ==========

        [Theory]
        // "ff" → /f/
        [InlineData("effet", "\u0259f\u025B")]
        // "ss" → /s/（母音間でも /z/ にならない）
        [InlineData("passe", "pas")]
        // "tt" → /t/（cette: s+ə+t, 語末e黙字）
        [InlineData("cette", "s\u0259t")]
        // "nn" → /n/（非鼻母音化、語末e黙字）
        [InlineData("bonne", "b\u0254n")]
        // "mm" → /m/（femme: f+ə+m, 語末e黙字）
        [InlineData("femme", "f\u0259m")]
        // "ll" → /l/（belle: b+ə+l, 語末e黙字）
        [InlineData("belle", "b\u0259l")]
        public void ToIPA_GeminateConsonants_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== x の処理 (~2件) ==========

        [Theory]
        // 語末 -x → 黙字
        [InlineData("deux", "d\u00F8")]
        // "ex" + 母音 → /ɛɡz/
        [InlineData("examen", "\u025B\u0261zam\u0251\u0303")]
        public void ToIPA_XRules_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== 母音間 s (~2件) ==========

        [Theory]
        // 母音間 s → /z/（rose: ʁ+o+z, 語末e黙字）
        [InlineData("rose", "\u0281oz")]
        // 語頭 s → /s/
        [InlineData("sol", "s\u0254l")]
        public void ToIPA_IntervocalicS_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== 'c' の軟音化 (~4件) ==========

        [Theory]
        // c + e → /s/
        [InlineData("ce", "s\u0259")]
        // c + i → /s/（ciel: s+j+ə+l、iの半母音化）
        [InlineData("ciel", "sj\u0259l")]
        // c + a → /k/
        [InlineData("cave", "kav")]
        // 語末 c → /k/（bec: b+ə+k）
        [InlineData("bec", "b\u0259k")]
        public void ToIPA_CSoftening_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== 'g' の軟音化 (~4件) ==========

        [Theory]
        // g + e → /ʒ/（gel: ʒ+ə+l）
        [InlineData("gel", "\u0292\u0259l")]
        // g + i → /ʒ/（gite: ʒ+i+t, 語末e黙字）
        [InlineData("gite", "\u0292it")]
        // g + a → /ɡ/（gant: ɡ+ɑ̃, 語末t黙字）
        [InlineData("gant", "\u0261\u0251\u0303")]
        // 語末 g → 黙字（long: l+ɔ̃）
        [InlineData("long", "l\u0254\u0303")]
        public void ToIPA_GSoftening_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== 'w' の処理 ==========

        [Fact]
        public void ToIPA_W_DefaultIsV()
        {
            // "w" デフォルト → /v/（wagon: v+a+ɡ+ɔ̃）
            var result = _engine.ToIPA("wagon");
            Assert.Equal("va\u0261\u0254\u0303", result);
        }

        // ========== ConvertGraphemes 直接テスト (internal) ==========

        [Fact]
        public void ConvertGraphemes_EmptyString_ReturnsEmpty()
        {
            var result = GraphemeToPhonemeRules.ConvertGraphemes("", FrenchDialect.Metropolitan);
            Assert.Empty(result);
        }

        [Fact]
        public void ConvertGraphemes_UnknownChars_AreSkipped()
        {
            // 数字はスキップされる
            var result = GraphemeToPhonemeRules.ConvertGraphemes("123", FrenchDialect.Metropolitan);
            Assert.Empty(result);
        }

        // ========== ApplySemivowelization 直接テスト (~3件) ==========

        [Fact]
        public void ApplySemivowelization_IBeforeVowel_BecomesJ()
        {
            var phonemes = new System.Collections.Generic.List<FrenchIpaPhoneme>
            {
                FrenchIpaPhoneme.I,
                FrenchIpaPhoneme.A,
            };
            GraphemeToPhonemeRules.ApplySemivowelization(phonemes);
            Assert.Equal(FrenchIpaPhoneme.J, phonemes[0]);
            Assert.Equal(FrenchIpaPhoneme.A, phonemes[1]);
        }

        [Fact]
        public void ApplySemivowelization_YBeforeVowel_BecomesUj()
        {
            var phonemes = new System.Collections.Generic.List<FrenchIpaPhoneme>
            {
                FrenchIpaPhoneme.Y,
                FrenchIpaPhoneme.I,
            };
            GraphemeToPhonemeRules.ApplySemivowelization(phonemes);
            Assert.Equal(FrenchIpaPhoneme.Uj, phonemes[0]);
            Assert.Equal(FrenchIpaPhoneme.I, phonemes[1]);
        }

        [Fact]
        public void ApplySemivowelization_UBeforeVowel_BecomesW()
        {
            var phonemes = new System.Collections.Generic.List<FrenchIpaPhoneme>
            {
                FrenchIpaPhoneme.U,
                FrenchIpaPhoneme.A,
            };
            GraphemeToPhonemeRules.ApplySemivowelization(phonemes);
            Assert.Equal(FrenchIpaPhoneme.W, phonemes[0]);
        }

        // ========== ApplyDialectMerger 直接テスト (~3件) ==========

        [Fact]
        public void ApplyDialectMerger_Metropolitan_MergesAh()
        {
            var phonemes = new System.Collections.Generic.List<FrenchIpaPhoneme>
            {
                FrenchIpaPhoneme.Ah,
                FrenchIpaPhoneme.M,
            };
            GraphemeToPhonemeRules.ApplyDialectMerger(phonemes, FrenchDialect.Metropolitan);
            Assert.Equal(FrenchIpaPhoneme.A, phonemes[0]);
        }

        [Fact]
        public void ApplyDialectMerger_Conservative_PreservesAh()
        {
            var phonemes = new System.Collections.Generic.List<FrenchIpaPhoneme>
            {
                FrenchIpaPhoneme.Ah,
                FrenchIpaPhoneme.M,
            };
            GraphemeToPhonemeRules.ApplyDialectMerger(phonemes, FrenchDialect.Conservative);
            Assert.Equal(FrenchIpaPhoneme.Ah, phonemes[0]);
        }

        [Fact]
        public void ApplyDialectMerger_Metropolitan_MergesOeNasal()
        {
            var phonemes = new System.Collections.Generic.List<FrenchIpaPhoneme>
            {
                FrenchIpaPhoneme.OeNasal,
            };
            GraphemeToPhonemeRules.ApplyDialectMerger(phonemes, FrenchDialect.Metropolitan);
            Assert.Equal(FrenchIpaPhoneme.ENasal, phonemes[0]);
        }

        // ========== sc + 前舌母音 (~2件) ==========

        [Theory]
        // "sc" + e/i → /s/ (science: s→sc=/s/, i→半母音j, en→ANasal, ce→/s/)
        [InlineData("science", "sj\u0251\u0303s")]
        // "sc" + è → /s/ (scène: sc=/s/, è=/ɛ/, n→/n/, 語末e黙字)
        [InlineData("sc\u00E8ne", "s\u025Bn")]
        public void ToIPA_ScBeforeFrontVowel_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== gu + 前舌母音 (~2件) ==========

        [Theory]
        // "gu" + i → /g/ (guide: gu=/ɡ/, i→/i/, d語末黙字→黙字, 語末e黙字)
        [InlineData("guide", "\u0261id")]
        // "gu" + e → /g/ (guerre: gu=/ɡ/, e→/ə/, rr→/ʁ/, 語末e黙字)
        [InlineData("guerre", "\u0261\u0259\u0281")]
        public void ToIPA_GuBeforeFrontVowel_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== cc + 前舌母音/非前舌母音 (~3件) ==========

        [Theory]
        // "cc" + 前舌母音(e) → /ks/ (accent: a + cc→/ks/ + en→ANasal + t語末黙字)
        [InlineData("accent", "akss\u0251\u0303")]
        // "cc" + 前舌母音(i) → /ks/ (accident: a + cc→/ks/ + i + d + en→ANasal + t語末黙字)
        [InlineData("accident", "akssid\u0251\u0303")]
        // "cc" + 非前舌母音(o) → /k/ consumed=2 (accord: a + cc→/k/ + o + r + d語末黙字)
        [InlineData("accord", "ak\u0254\u0281")]
        public void ToIPA_CcRules_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== "eaux" 4文字マルチグラフ ==========

        [Fact]
        public void ToIPA_Eaux_FourCharMultigraph_ReturnsO()
        {
            // "beaux" → b + eaux=/o/
            Assert.Equal("bo", _engine.ToIPA("beaux"));
        }

        // ========== "-euille" 語末パターン ==========

        [Fact]
        public void ToIPA_EuilleSuffix_ReturnsOehJ()
        {
            // "feuille" → f + euille=/œj/
            Assert.Equal("f\u0153j", _engine.ToIPA("feuille"));
        }

        // ========== "ein" → /ɛ̃/ ==========

        [Fact]
        public void ToIPA_Ein_ReturnsENasal()
        {
            // "plein" → p + l + ein=/ɛ̃/
            Assert.Equal("pl\u025B\u0303", _engine.ToIPA("plein"));
        }

        // ========== "sch" → /ʃ/ ==========

        [Fact]
        public void ToIPA_Sch_ReturnsSh()
        {
            // "schéma" → sch=/ʃ/ + é=/e/ + m + a
            Assert.Equal("\u0283ema", _engine.ToIPA("sch\u00E9ma"));
        }

        // ========== トレマによるダイグラフ抑制 ==========

        [Fact]
        public void ToIPA_Trema_PreventsDigraph()
        {
            // "naïf" → n + a + ï=/i/ + f (aï はダイグラフ /ɛ/ にならない)
            Assert.Equal("naif", _engine.ToIPA("na\u00EFf"));
        }

        // ========== "ill" 語中（子音先行後の母音+ill）==========

        [Fact]
        public void ToIPA_IllInWord_ReturnsCorrect()
        {
            // "briller" → b + r + ill + er
            Assert.Equal("b\u0281ile", _engine.ToIPA("briller"));
        }

        // ========== x + 子音 → /ks/ ==========

        [Fact]
        public void ToIPA_XBeforeConsonant_ReturnsKs()
        {
            // "extra" → e→/ə/ + x=/ks/ + t + r + a
            Assert.Equal("\u0259kst\u0281a", _engine.ToIPA("extra"));
        }

        // ========== "ennui" nn非鼻母音化確認 ==========

        [Fact]
        public void ToIPA_Ennui_NnNotNasalized()
        {
            // "ennui" → e→/ə/ + nn→/n/ + u→/y/→半母音/ɥ/ + i
            Assert.Equal("\u0259n\u0265i", _engine.ToIPA("ennui"));
        }

        public void Dispose()
        {
            _engine.Dispose();
            _conservativeEngine.Dispose();
        }
    }
}
