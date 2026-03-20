using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DotNetG2P.Chinese.Conversion;
using DotNetG2P.Internal;
using UnityEngine.Scripting;

namespace DotNetG2P.Chinese
{
    /// <summary>
    /// 中国語（普通話）ピンイン変換エンジン。
    /// 漢字テキストをピンイン（声調記号付き/声調番号/無声調）に変換する。
    /// </summary>
    /// <remarks>
    /// このクラスはスレッドセーフです。辞書はコンストラクタで読み込まれ、以後は読み取り専用です。
    /// </remarks>
    [Preserve]
    public sealed class ChineseG2PEngine : IDisposable
    {
        private readonly PinyinCharDictionary _charDictionary;
        private readonly PinyinPhraseDictionary? _phraseDictionary;
        private readonly ChineseG2POptions _options;
        private int _disposed;

        internal PinyinCharDictionary CharDictionaryInternal => _charDictionary;

        internal PinyinPhraseDictionary? PhraseDictionaryInternal => _phraseDictionary;

        /// <summary>
        /// 埋め込み辞書（単字+フレーズ）とデフォルトオプションでエンジンを初期化する。
        /// </summary>
        public ChineseG2PEngine()
            : this(EmbeddedChineseDictionaryCache.CharDictionary, EmbeddedChineseDictionaryCache.PhraseDictionary, ChineseG2POptions.Default)
        {
        }

        /// <summary>
        /// 埋め込み辞書（単字+フレーズ）とオプションを指定してエンジンを初期化する。
        /// </summary>
        /// <param name="options">処理オプション</param>
        public ChineseG2PEngine(ChineseG2POptions options)
            : this(EmbeddedChineseDictionaryCache.CharDictionary, EmbeddedChineseDictionaryCache.PhraseDictionary, options)
        {
        }

        /// <summary>
        /// 外部単字辞書ファイルを使用してエンジンを初期化する（フレーズ辞書なし）。
        /// </summary>
        /// <param name="charDictPath">単字ピンイン辞書ファイルパス</param>
        public ChineseG2PEngine(string charDictPath)
            : this(PinyinCharDictionary.LoadFromFile(charDictPath), null, ChineseG2POptions.Default)
        {
        }

        /// <summary>
        /// 外部単字辞書ファイルとオプションを指定してエンジンを初期化する（フレーズ辞書なし）。
        /// </summary>
        /// <param name="charDictPath">単字ピンイン辞書ファイルパス</param>
        /// <param name="options">処理オプション</param>
        public ChineseG2PEngine(string charDictPath, ChineseG2POptions options)
            : this(PinyinCharDictionary.LoadFromFile(charDictPath), null, options)
        {
        }

        /// <summary>
        /// 外部辞書ファイル（単字+フレーズ）を使用してエンジンを初期化する。
        /// </summary>
        /// <param name="charDictPath">単字ピンイン辞書ファイルパス</param>
        /// <param name="phraseDictPath">フレーズピンイン辞書ファイルパス</param>
        public ChineseG2PEngine(string charDictPath, string phraseDictPath)
            : this(PinyinCharDictionary.LoadFromFile(charDictPath), PinyinPhraseDictionary.LoadFromFile(phraseDictPath), ChineseG2POptions.Default)
        {
        }

        /// <summary>
        /// 外部辞書ファイル（単字+フレーズ）とオプションを指定してエンジンを初期化する。
        /// </summary>
        /// <param name="charDictPath">単字ピンイン辞書ファイルパス</param>
        /// <param name="phraseDictPath">フレーズピンイン辞書ファイルパス</param>
        /// <param name="options">処理オプション</param>
        public ChineseG2PEngine(string charDictPath, string phraseDictPath, ChineseG2POptions options)
            : this(PinyinCharDictionary.LoadFromFile(charDictPath), PinyinPhraseDictionary.LoadFromFile(phraseDictPath), options)
        {
        }

        /// <summary>
        /// 外部から辞書オブジェクトを直接指定してエンジンを初期化する（Unity StreamingAssets対応）。
        /// </summary>
        /// <param name="charDictionary">単字ピンイン辞書</param>
        /// <param name="phraseDictionary">フレーズピンイン辞書（nullの場合はフレーズ辞書なし）</param>
        /// <param name="options">処理オプション</param>
        public ChineseG2PEngine(PinyinCharDictionary charDictionary, PinyinPhraseDictionary? phraseDictionary, ChineseG2POptions options)
        {
            _charDictionary = charDictionary ?? throw new ArgumentNullException(nameof(charDictionary));
            _phraseDictionary = phraseDictionary;
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        // =====================================================================
        // ピンイン収集用内部構造体
        // =====================================================================

        /// <summary>
        /// テキストから収集したピンイン情報を保持する構造体。
        /// </summary>
        private struct PinyinEntry
        {
            /// <summary>声調記号付きピンイン（非漢字の場合はnull）</summary>
            public string? Pinyin;
            /// <summary>元の文字</summary>
            public char OriginalChar;
            /// <summary>句読点/スペースか</summary>
            public bool IsSeparator;
            /// <summary>非漢字のそのまま出力テキスト（ASCII英数字等）</summary>
            public string? RawText;
            /// <summary>辞書にない漢字か（ToPinyinでの区切り動作が異なる）</summary>
            public bool IsUnknownHanzi;
        }

        /// <summary>
        /// テキストをピンイン文字列に変換する（デフォルトスタイル使用）。
        /// CJK統合漢字を辞書検索してピンインに変換し、
        /// 句読点は区切りとして扱い、ASCII英数字はそのまま出力する。
        /// 例: "你好" → "nǐ hǎo"（ToneMarkedスタイル時）
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>ピンイン文字列</returns>
        public string ToPinyin(string text)
        {
            return ToPinyin(text, _options.DefaultStyle);
        }

        /// <summary>
        /// テキストを指定スタイルのピンイン文字列に変換する。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <param name="style">ピンインスタイル</param>
        /// <returns>ピンイン文字列</returns>
        public string ToPinyin(string text, PinyinStyle style)
        {
            return RunPipeline(text, p => ApplyStyle(p, style));
        }

        /// <summary>
        /// テキストを各文字ごとのピンイン配列に変換する（デフォルトスタイル使用）。
        /// CJK統合漢字はピンインに、非漢字はその文字自体の文字列として返す。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>各文字に対応するピンイン文字列の配列</returns>
        public string[] ToPinyinList(string text)
        {
            return ToPinyinList(text, _options.DefaultStyle);
        }

        /// <summary>
        /// テキストを各文字ごとの指定スタイルのピンイン配列に変換する。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <param name="style">ピンインスタイル</param>
        /// <returns>各文字に対応するピンイン文字列の配列</returns>
        public string[] ToPinyinList(string text, PinyinStyle style)
        {
            return RunPipelineList(text, p => ApplyStyle(p, style));
        }

        /// <summary>
        /// 指定した漢字が辞書に登録されているかを返す。
        /// </summary>
        /// <param name="c">検索する漢字</param>
        /// <returns>登録されている場合 true</returns>
        public bool ContainsChar(char c)
        {
            ThrowIfDisposed();
            return _charDictionary.TryLookup((int)c, out _);
        }

        /// <summary>
        /// 指定した漢字の全ピンイン候補を返す。
        /// </summary>
        /// <param name="c">検索する漢字</param>
        /// <returns>ピンイン候補の配列。辞書に未登録の場合は空配列。</returns>
        public string[] LookupChar(char c)
        {
            ThrowIfDisposed();

            if (_charDictionary.TryLookupAll((int)c, out var pinyins))
                return pinyins;

            return Array.Empty<string>();
        }

        // =====================================================================
        // IPA出力
        // =====================================================================

        /// <summary>
        /// テキストをIPA（国際音声記号）表記に変換する（声調マーカー付き）。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>IPA表記文字列</returns>
        public string ToIPA(string text)
        {
            return ToIPA(text, true);
        }

        /// <summary>
        /// テキストをIPA（国際音声記号）表記に変換する。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <param name="includeTones">声調マーカーを含めるかどうか</param>
        /// <returns>IPA表記文字列</returns>
        public string ToIPA(string text, bool includeTones)
        {
            return RunPipeline(text, p => PinyinToIpa.Convert(p, includeTones));
        }

        // =====================================================================
        // 注音出力
        // =====================================================================

        /// <summary>
        /// テキストを注音符号（ボポモフォ）に変換する（声調マーカー付き）。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>注音符号文字列</returns>
        public string ToZhuyin(string text)
        {
            return ToZhuyin(text, true);
        }

        /// <summary>
        /// テキストを注音符号（ボポモフォ）に変換する。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <param name="includeTones">声調マーカーを含めるかどうか</param>
        /// <returns>注音符号文字列</returns>
        public string ToZhuyin(string text, bool includeTones)
        {
            return RunPipeline(text, p => PinyinToZhuyin.Convert(p, includeTones));
        }

        // =====================================================================
        // piper-plus 互換 IPA 出力
        // =====================================================================

        /// <summary>
        /// テキストを piper-plus 互換 IPA 文字列に変換する。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>piper-plus 互換 IPA 文字列</returns>
        public string ToPiperIPA(string text)
        {
            return RunPipeline(text, p => PinyinToPiperIpa.Convert(p));
        }

        /// <summary>
        /// テキストを piper-plus 互換 IPA 音素配列に変換する。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>piper-plus 互換 IPA 音素の配列</returns>
        public string[] ToPiperIpaPhonemes(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            var entries = CollectPinyins(text);

            if (_options.EnableToneSandhi)
                ApplyToneSandhiToEntries(entries);

            var result = new List<string>();
            foreach (var entry in entries)
            {
                if (entry.Pinyin != null)
                {
                    if (PinyinParser.TryParse(entry.Pinyin, out var syllable))
                    {
                        var phonemes = PinyinToPiperIpa.ConvertToPhonemes(syllable);
                        result.AddRange(phonemes);
                    }
                }
            }
            return result.ToArray();
        }

        // =====================================================================
        // PUA 出力
        // =====================================================================

        /// <summary>
        /// テキストを piper-plus 互換 PUA 音素配列に変換する。
        /// 各音節の PUA 音素の末尾にトーン PUA 文字（0xE046-0xE04A）を自動追加する。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>PUA 音素の配列（各音節末尾にトーンPUA含む）</returns>
        public string[] ToPuaPhonemes(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            var entries = CollectPinyins(text);

            if (_options.EnableToneSandhi)
                ApplyToneSandhiToEntries(entries);

            var result = new List<string>();
            foreach (var entry in entries)
            {
                if (entry.Pinyin != null)
                {
                    if (PinyinParser.TryParse(entry.Pinyin, out var syllable))
                    {
                        // 音節の IPA 音素を PUA マッピング
                        var ipaPhonemes = PinyinToPiperIpa.ConvertToPhonemes(syllable);
                        var puaPhonemes = ChinesePuaMapper.ApplyPuaMapping(ipaPhonemes);
                        result.AddRange(puaPhonemes);

                        // 声調番号: Tone enum の int 値（1-4）、Neutral=0→5扱い
                        int toneNumber = (int)syllable.Tone;
                        if (toneNumber == 0)
                            toneNumber = 5;

                        var tonePua = ChinesePuaMapper.ToneToPua(toneNumber);
                        if (tonePua.Length > 0)
                            result.Add(tonePua);
                    }
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// テキストを piper-plus 互換 PUA 文字列に変換する。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>PUA 文字列</returns>
        public string ToPuaString(string text)
        {
            var puaPhonemes = ToPuaPhonemes(text);
            return string.Join(" ", puaPhonemes);
        }

        // =====================================================================
        // Prosody 出力
        // =====================================================================

        /// <summary>
        /// テキストの IPA 音素と韻律情報（声調・語内位置・語長）を返す。
        /// piper-plus の _build_word_info() に準拠し、連続する漢字エントリを「語」としてグループ化する。
        /// 各音節ごとに声調マーカー付き IPA 文字列を1つ返す。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>IPA 音素配列と韻律情報を含む結果</returns>
        public ChineseProsodyResult ToIpaWithProsody(string text)
        {
            return ToIpaWithProsody(text, true);
        }

        /// <summary>
        /// テキストの IPA 音素と韻律情報（声調・語内位置・語長）を返す。
        /// 各音節ごとに IPA 文字列を1つ返す。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <param name="includeTones">IPA 出力に声調マーカーを含めるか</param>
        /// <returns>IPA 音素配列と韻律情報を含む結果</returns>
        public ChineseProsodyResult ToIpaWithProsody(string text, bool includeTones)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(text))
                return new ChineseProsodyResult(Array.Empty<string>(), Array.Empty<ChineseProsodyInfo>());

            var entries = CollectPinyins(text);

            if (_options.EnableToneSandhi)
                ApplyToneSandhiToEntries(entries);

            // 連続する漢字エントリ（Pinyin != null && !IsSeparator）をグループ化して「語」とする
            var allPhonemes = new List<string>();
            var allProsody = new List<ChineseProsodyInfo>();

            int i = 0;
            while (i < entries.Count)
            {
                var entry = entries[i];
                if (entry.Pinyin != null && !entry.IsSeparator)
                {
                    // 語の開始: 連続する漢字エントリを収集
                    var wordEntries = new List<PinyinEntry>();
                    while (i < entries.Count && entries[i].Pinyin != null && !entries[i].IsSeparator)
                    {
                        wordEntries.Add(entries[i]);
                        i++;
                    }

                    int wordLength = wordEntries.Count;

                    for (int w = 0; w < wordEntries.Count; w++)
                    {
                        var we = wordEntries[w];
                        if (PinyinParser.TryParse(we.Pinyin!, out var syllable))
                        {
                            // 音節ごとに1つの IPA 文字列を生成
                            var ipaStr = PinyinToIpa.Convert(we.Pinyin!, includeTones);

                            // 声調番号: Tone enum の int 値（1-4）、Neutral=0→5扱い
                            int toneNumber = (int)syllable.Tone;
                            if (toneNumber == 0)
                                toneNumber = 5;

                            int syllablePosition = w + 1; // 1ベース

                            var prosodyInfo = new ChineseProsodyInfo(toneNumber, syllablePosition, wordLength);

                            allPhonemes.Add(ipaStr);
                            allProsody.Add(prosodyInfo);
                        }
                    }
                }
                else
                {
                    i++;
                }
            }

            return new ChineseProsodyResult(allPhonemes.ToArray(), allProsody.ToArray());
        }

        // =====================================================================
        // バッチAPI
        // =====================================================================

        /// <summary>
        /// 複数テキストを一括でピンインに変換する（デフォルトスタイル使用）。
        /// </summary>
        /// <param name="texts">入力テキストの配列</param>
        /// <returns>各テキストに対応するピンイン文字列のリスト</returns>
        public IReadOnlyList<string> ToPinyinBatch(string[] texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList(texts, ToPinyin);
        }

        /// <summary>
        /// 複数テキストを一括で指定スタイルのピンインに変換する。
        /// </summary>
        /// <param name="texts">入力テキストの配列</param>
        /// <param name="style">ピンインスタイル</param>
        /// <returns>各テキストに対応するピンイン文字列のリスト</returns>
        public IReadOnlyList<string> ToPinyinBatch(string[] texts, PinyinStyle style)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList(
                texts,
                this,
                style,
                ConvertPinyinBatchItem);
        }

        /// <summary>
        /// 複数テキストを一括で各文字ごとのピンイン配列に変換する（デフォルトスタイル使用）。
        /// </summary>
        /// <param name="texts">入力テキストの配列</param>
        /// <returns>各テキストに対応するピンイン配列のリスト</returns>
        public IReadOnlyList<string[]> ToPinyinListBatch(string[] texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList(texts, ToPinyinList);
        }

        /// <summary>
        /// 複数テキストを一括で各文字ごとの指定スタイルのピンイン配列に変換する。
        /// </summary>
        /// <param name="texts">入力テキストの配列</param>
        /// <param name="style">ピンインスタイル</param>
        /// <returns>各テキストに対応するピンイン配列のリスト</returns>
        public IReadOnlyList<string[]> ToPinyinListBatch(string[] texts, PinyinStyle style)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList(
                texts,
                this,
                style,
                ConvertPinyinListBatchItem);
        }

        /// <summary>
        /// 複数テキストを一括でIPA表記に変換する（声調マーカー付き）。
        /// </summary>
        /// <param name="texts">入力テキストの配列</param>
        /// <returns>各テキストに対応するIPA文字列のリスト</returns>
        public IReadOnlyList<string> ToIPABatch(string[] texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList(texts, ToIPA);
        }

        /// <summary>
        /// 複数テキストを一括でIPA表記に変換する。
        /// </summary>
        /// <param name="texts">入力テキストの配列</param>
        /// <param name="includeTones">声調マーカーを含めるかどうか</param>
        /// <returns>各テキストに対応するIPA文字列のリスト</returns>
        public IReadOnlyList<string> ToIPABatch(string[] texts, bool includeTones)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList(
                texts,
                this,
                includeTones,
                ConvertIpaBatchItem);
        }

        /// <summary>
        /// 複数テキストを一括で注音符号に変換する（声調マーカー付き）。
        /// </summary>
        /// <param name="texts">入力テキストの配列</param>
        /// <returns>各テキストに対応する注音符号文字列のリスト</returns>
        public IReadOnlyList<string> ToZhuyinBatch(string[] texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList(texts, ToZhuyin);
        }

        /// <summary>
        /// 複数テキストを一括で注音符号に変換する。
        /// </summary>
        /// <param name="texts">入力テキストの配列</param>
        /// <param name="includeTones">声調マーカーを含めるかどうか</param>
        /// <returns>各テキストに対応する注音符号文字列のリスト</returns>
        public IReadOnlyList<string> ToZhuyinBatch(string[] texts, bool includeTones)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList(
                texts,
                this,
                includeTones,
                ConvertZhuyinBatchItem);
        }

        /// <summary>
        /// 複数テキストを一括で piper-plus 互換 IPA に変換する。
        /// </summary>
        /// <param name="texts">入力テキストの配列</param>
        /// <returns>各テキストに対応する piper-plus 互換 IPA 文字列のリスト</returns>
        public IReadOnlyList<string> ToPiperIPABatch(string[] texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList(texts, ToPiperIPA);
        }

        /// <summary>
        /// 複数テキストを一括で piper-plus 互換 PUA 文字列に変換する。
        /// </summary>
        /// <param name="texts">入力テキストの配列</param>
        /// <returns>各テキストに対応する PUA 文字列のリスト</returns>
        public IReadOnlyList<string> ToPuaStringBatch(string[] texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList(texts, ToPuaString);
        }

        /// <summary>
        /// 複数テキストを一括で IPA 音素と韻律情報に変換する。
        /// </summary>
        /// <param name="texts">入力テキストの配列</param>
        /// <returns>各テキストに対応する韻律情報付き IPA 音素結果のリスト</returns>
        public IReadOnlyList<ChineseProsodyResult> ToIpaWithProsodyBatch(string[] texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList(texts, ToIpaWithProsody);
        }

        /// <summary>
        /// 複数テキストを一括で IPA 音素と韻律情報に変換する。
        /// </summary>
        /// <param name="texts">入力テキストの配列</param>
        /// <param name="includeTones">IPA 出力に声調マーカーを含めるか</param>
        /// <returns>各テキストに対応する韻律情報付き IPA 音素結果のリスト</returns>
        public IReadOnlyList<ChineseProsodyResult> ToIpaWithProsodyBatch(string[] texts, bool includeTones)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList(
                texts,
                this,
                includeTones,
                ConvertIpaWithProsodyBatchItem);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Interlocked.CompareExchange(ref _disposed, 1, 0);
        }

        // =====================================================================
        // 内部ヘルパー: パイプライン共通処理
        // =====================================================================

        /// <summary>
        /// 共通パイプライン: 入力テキストに対してピンイン収集→声調変調→文字列出力を行う。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <param name="converter">各ピンインに適用する変換関数</param>
        /// <returns>変換結果文字列</returns>
        private string RunPipeline(string text, Func<string, string> converter)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(text))
                return "";

            var entries = CollectPinyins(text);

            if (_options.EnableToneSandhi)
                ApplyToneSandhiToEntries(entries);

            return FormatOutput(entries, converter);
        }

        /// <summary>
        /// 共通パイプライン: 入力テキストに対してピンイン収集→声調変調→配列出力を行う。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <param name="converter">各ピンインに適用する変換関数</param>
        /// <returns>各文字に対応する変換結果文字列の配列</returns>
        private string[] RunPipelineList(string text, Func<string, string> converter)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            var entries = CollectPinyins(text);

            if (_options.EnableToneSandhi)
                ApplyToneSandhiToEntries(entries);

            var result = new List<string>(entries.Count);
            foreach (var entry in entries)
            {
                if (entry.Pinyin != null)
                    result.Add(converter(entry.Pinyin));
                else
                    result.Add(entry.RawText ?? entry.OriginalChar.ToString());
            }
            return result.ToArray();
        }

        /// <summary>
        /// ピンインエントリリストを文字列に整形する。
        /// </summary>
        /// <param name="entries">ピンインエントリリスト</param>
        /// <param name="converter">各ピンインに適用する変換関数</param>
        /// <returns>整形された出力文字列</returns>
        private string FormatOutput(List<PinyinEntry> entries, Func<string, string> converter)
        {
            var separator = _options.Separator;
            var sb = new StringBuilder();
            var needsSeparator = false;

            foreach (var entry in entries)
            {
                if (entry.IsSeparator)
                {
                    needsSeparator = false;
                }
                else if (entry.Pinyin != null)
                {
                    if (needsSeparator && sb.Length > 0)
                        sb.Append(separator);
                    sb.Append(converter(entry.Pinyin));
                    needsSeparator = true;
                }
                else if (entry.IsUnknownHanzi)
                {
                    if (needsSeparator && sb.Length > 0)
                        sb.Append(separator);
                    sb.Append(entry.OriginalChar);
                    needsSeparator = true;
                }
                else if (entry.RawText != null)
                {
                    sb.Append(entry.RawText);
                    needsSeparator = false;
                }
            }

            return sb.ToString();
        }

        // =====================================================================
        // 内部ヘルパー: ピンイン収集・声調変調適用
        // =====================================================================

        /// <summary>
        /// テキストからピンイン列と対応する元の文字情報を収集する。
        /// </summary>
        private List<PinyinEntry> CollectPinyins(string text)
        {
            var entries = new List<PinyinEntry>(text.Length);

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                // サロゲートペアの検出（絵文字、CJK拡張B以降等）
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    entries.Add(new PinyinEntry
                    {
                        Pinyin = null,
                        OriginalChar = c,
                        IsSeparator = false,
                        RawText = text.Substring(i, 2)
                    });
                    i++; // ローサロゲートをスキップ
                    continue;
                }

                if (IsCjkUnifiedIdeograph(c))
                {
                    // フレーズ辞書で最長一致検索
                    if (_phraseDictionary != null && _options.HandleHeteronyms)
                    {
                        var matchLen = _phraseDictionary.FindLongestMatch(text, i, out var phrasePinyins);
                        if (matchLen > 0)
                        {
                            for (int k = 0; k < phrasePinyins.Length; k++)
                            {
                                entries.Add(new PinyinEntry
                                {
                                    Pinyin = phrasePinyins[k],
                                    OriginalChar = text[i + k],
                                    IsSeparator = false,
                                    RawText = null
                                });
                            }
                            i += matchLen - 1; // ループincrement分を考慮
                            continue;
                        }
                    }

                    // 単字辞書フォールバック
                    var codePoint = (int)c;
                    if (_charDictionary.TryLookup(codePoint, out var singlePinyin))
                    {
                        entries.Add(new PinyinEntry
                        {
                            Pinyin = singlePinyin,
                            OriginalChar = c,
                            IsSeparator = false,
                            RawText = null
                        });
                    }
                    else
                    {
                        // 辞書にない漢字はそのまま出力
                        entries.Add(new PinyinEntry
                        {
                            Pinyin = null,
                            OriginalChar = c,
                            IsSeparator = false,
                            RawText = c.ToString(),
                            IsUnknownHanzi = true
                        });
                    }
                }
                else if (IsCjkPunctuation(c) || IsAsciiPunctuation(c))
                {
                    // 句読点は区切りマーカー（元の文字は保持）
                    entries.Add(new PinyinEntry
                    {
                        Pinyin = null,
                        OriginalChar = c,
                        IsSeparator = true,
                        RawText = null
                    });
                }
                else if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                {
                    // スペース・改行は区切りマーカー（元の文字は保持）
                    entries.Add(new PinyinEntry
                    {
                        Pinyin = null,
                        OriginalChar = c,
                        IsSeparator = true,
                        RawText = null
                    });
                }
                else
                {
                    // ASCII英数字等はそのまま出力
                    entries.Add(new PinyinEntry
                    {
                        Pinyin = null,
                        OriginalChar = c,
                        IsSeparator = false,
                        RawText = c.ToString()
                    });
                }
            }

            return entries;
        }

        /// <summary>
        /// 収集済みエントリに対して声調変調を適用する。
        /// 漢字位置のピンインと元の文字を抽出してToneSandhiProcessorに渡し、結果を書き戻す。
        /// </summary>
        private static void ApplyToneSandhiToEntries(List<PinyinEntry> entries)
        {
            // 漢字（ピンインあり）スロットのインデックスを収集
            var hanziIndices = new List<int>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Pinyin != null)
                    hanziIndices.Add(i);
            }

            if (hanziIndices.Count == 0)
                return;

            // ToneSandhiProcessor用の配列を構築
            var pinyins = new string[hanziIndices.Count];
            var originalChars = new char[hanziIndices.Count];

            for (int i = 0; i < hanziIndices.Count; i++)
            {
                var entry = entries[hanziIndices[i]];
                pinyins[i] = entry.Pinyin!;
                originalChars[i] = entry.OriginalChar;
            }

            // 声調変調を適用（in-place mutation）
            ToneSandhiProcessor.Apply(pinyins, originalChars);

            // 結果をエントリに書き戻す
            for (int i = 0; i < hanziIndices.Count; i++)
            {
                var entry = entries[hanziIndices[i]];
                entry.Pinyin = pinyins[i];
                entries[hanziIndices[i]] = entry;
            }
        }

        /// <summary>
        /// ピンインにスタイルを適用する。
        /// </summary>
        private static string ApplyStyle(string pinyin, PinyinStyle style)
        {
            switch (style)
            {
                case PinyinStyle.ToneMarked:
                    return pinyin;
                case PinyinStyle.ToneNumber:
                    return ToneConverter.ToToneNumber(pinyin);
                case PinyinStyle.Normal:
                    return ToneConverter.RemoveTone(pinyin);
                default:
                    return pinyin;
            }
        }

        private static string ConvertPinyinBatchItem(ChineseG2PEngine engine, string text, PinyinStyle style)
        {
            return engine.ToPinyin(text, style);
        }

        private static string[] ConvertPinyinListBatchItem(ChineseG2PEngine engine, string text, PinyinStyle style)
        {
            return engine.ToPinyinList(text, style);
        }

        private static string ConvertIpaBatchItem(ChineseG2PEngine engine, string text, bool includeTones)
        {
            return engine.ToIPA(text, includeTones);
        }

        private static string ConvertZhuyinBatchItem(ChineseG2PEngine engine, string text, bool includeTones)
        {
            return engine.ToZhuyin(text, includeTones);
        }

        private static ChineseProsodyResult ConvertIpaWithProsodyBatchItem(ChineseG2PEngine engine, string text, bool includeTones)
        {
            return engine.ToIpaWithProsody(text, includeTones);
        }

        /// <summary>
        /// CJK統合漢字であるかを判定する。
        /// CJK Unified Ideographs (U+4E00-U+9FFF)、Extension A (U+3400-U+4DBF)、
        /// Compatibility Ideographs (U+F900-U+FAFF) を含む。
        /// </summary>
        private static bool IsCjkUnifiedIdeograph(char c)
        {
            return (c >= '\u4E00' && c <= '\u9FFF')    // CJK Unified Ideographs
                || (c >= '\u3400' && c <= '\u4DBF')    // CJK Extension A
                || (c >= '\uF900' && c <= '\uFAFF');   // CJK Compatibility
        }

        /// <summary>
        /// CJK句読点であるかを判定する。
        /// </summary>
        private static bool IsCjkPunctuation(char c)
        {
            switch (c)
            {
                case '\u3002': // 。
                case '\uFF0C': // ，
                case '\uFF01': // ！
                case '\uFF1F': // ？
                case '\u3001': // 、
                case '\uFF1B': // ；
                case '\uFF1A': // ：
                case '\uFF08': // （
                case '\uFF09': // ）
                case '\u300A': // 《
                case '\u300B': // 》
                case '\u300C': // 「
                case '\u300D': // 」
                case '\u3010': // 【
                case '\u3011': // 】
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// ASCII句読点であるかを判定する。
        /// </summary>
        private static bool IsAsciiPunctuation(char c)
        {
            switch (c)
            {
                case '.':
                case ',':
                case '!':
                case '?':
                case ';':
                case ':':
                case '(':
                case ')':
                case '[':
                case ']':
                case '{':
                case '}':
                case '"':
                case '\'':
                    return true;
                default:
                    return false;
            }
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(ChineseG2PEngine));
        }
    }
}
