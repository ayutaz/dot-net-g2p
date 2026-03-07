using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DotNetG2P.Chinese
{
    /// <summary>
    /// 中国語（普通話）ピンイン変換エンジン。
    /// 漢字テキストをピンイン（声調記号付き/声調番号/無声調）に変換する。
    /// </summary>
    /// <remarks>
    /// このクラスはスレッドセーフです。辞書はコンストラクタで読み込まれ、以後は読み取り専用です。
    /// </remarks>
    public sealed class ChineseG2PEngine : IDisposable
    {
        private readonly PinyinCharDictionary _charDictionary;
        private readonly PinyinPhraseDictionary? _phraseDictionary;
        private readonly ChineseG2POptions _options;
        private int _disposed;

        /// <summary>
        /// 埋め込み辞書（単字+フレーズ）とデフォルトオプションでエンジンを初期化する。
        /// </summary>
        public ChineseG2PEngine()
            : this(PinyinCharDictionary.LoadEmbedded(), PinyinPhraseDictionary.LoadEmbedded(), ChineseG2POptions.Default)
        {
        }

        /// <summary>
        /// 埋め込み辞書（単字+フレーズ）とオプションを指定してエンジンを初期化する。
        /// </summary>
        /// <param name="options">処理オプション</param>
        public ChineseG2PEngine(ChineseG2POptions options)
            : this(PinyinCharDictionary.LoadEmbedded(), PinyinPhraseDictionary.LoadEmbedded(), options)
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
        /// PinyinCharDictionaryとPinyinPhraseDictionaryを指定してエンジンを初期化する（内部用）。
        /// </summary>
        internal ChineseG2PEngine(PinyinCharDictionary charDictionary, PinyinPhraseDictionary? phraseDictionary, ChineseG2POptions options)
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
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(text))
                return "";

            // Step 1: ピンイン収集
            var entries = CollectPinyins(text);

            // Step 2: 声調変調
            if (_options.EnableToneSandhi)
                ApplyToneSandhiToEntries(entries);

            // Step 3: スタイル変換+出力
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
                    sb.Append(ApplyStyle(entry.Pinyin, style));
                    needsSeparator = true;
                }
                else if (entry.IsUnknownHanzi)
                {
                    // 辞書にない漢字: 区切り付きで出力し、次のピンインの前にも区切りを入れる
                    if (needsSeparator && sb.Length > 0)
                        sb.Append(separator);
                    sb.Append(entry.OriginalChar);
                    needsSeparator = true;
                }
                else if (entry.RawText != null)
                {
                    // ASCII英数字等: そのまま出力
                    sb.Append(entry.RawText);
                    needsSeparator = false;
                }
            }

            return sb.ToString();
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
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(text))
                return Array.Empty<string>();

            // Step 1: ピンイン収集
            var entries = CollectPinyins(text);

            // Step 2: 声調変調
            if (_options.EnableToneSandhi)
                ApplyToneSandhiToEntries(entries);

            // Step 3: スタイル変換+出力
            var result = new List<string>(entries.Count);

            foreach (var entry in entries)
            {
                if (entry.Pinyin != null)
                {
                    result.Add(ApplyStyle(entry.Pinyin, style));
                }
                else
                {
                    // 非漢字（句読点・スペース・ASCII英数字等）はそのまま文字として出力
                    result.Add(entry.RawText ?? entry.OriginalChar.ToString());
                }
            }

            return result.ToArray();
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
        // バッチAPI
        // =====================================================================

        /// <summary>
        /// 複数テキストを一括でピンインに変換する（デフォルトスタイル使用）。
        /// </summary>
        /// <param name="texts">入力テキストの配列</param>
        /// <returns>各テキストに対応するピンイン文字列の配列</returns>
        public string[] ToPinyinBatch(string[] texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new string[texts.Length];
            for (var i = 0; i < texts.Length; i++)
                results[i] = ToPinyin(texts[i]);
            return results;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                _charDictionary.Clear();
                _phraseDictionary?.Clear();
            }
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
