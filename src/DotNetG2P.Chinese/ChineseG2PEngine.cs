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
        private readonly ChineseG2POptions _options;
        private int _disposed;

        /// <summary>
        /// 埋め込み辞書とデフォルトオプションでエンジンを初期化する。
        /// </summary>
        public ChineseG2PEngine()
            : this(PinyinCharDictionary.LoadEmbedded(), ChineseG2POptions.Default)
        {
        }

        /// <summary>
        /// 埋め込み辞書とオプションを指定してエンジンを初期化する。
        /// </summary>
        /// <param name="options">処理オプション</param>
        public ChineseG2PEngine(ChineseG2POptions options)
            : this(PinyinCharDictionary.LoadEmbedded(), options)
        {
        }

        /// <summary>
        /// 外部辞書ファイルを使用してエンジンを初期化する。
        /// </summary>
        /// <param name="charDictPath">単字ピンイン辞書ファイルパス</param>
        public ChineseG2PEngine(string charDictPath)
            : this(PinyinCharDictionary.LoadFromFile(charDictPath), ChineseG2POptions.Default)
        {
        }

        /// <summary>
        /// 外部辞書ファイルとオプションを指定してエンジンを初期化する。
        /// </summary>
        /// <param name="charDictPath">単字ピンイン辞書ファイルパス</param>
        /// <param name="options">処理オプション</param>
        public ChineseG2PEngine(string charDictPath, ChineseG2POptions options)
            : this(PinyinCharDictionary.LoadFromFile(charDictPath), options)
        {
        }

        /// <summary>
        /// PinyinCharDictionaryインスタンスとオプションを指定してエンジンを初期化する（内部用）。
        /// </summary>
        internal ChineseG2PEngine(PinyinCharDictionary charDictionary, ChineseG2POptions options)
        {
            _charDictionary = charDictionary ?? throw new ArgumentNullException(nameof(charDictionary));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// テキストをピンイン文字列に変換する（デフォルトスタイル使用）。
        /// CJK統合漢字（U+4E00-U+9FFF）を辞書検索してピンインに変換し、
        /// 非漢字はそのまま出力する。
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

            var separator = _options.Separator;
            var sb = new StringBuilder();
            var needsSeparator = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (IsCjkUnifiedIdeograph(c))
                {
                    var codePoint = (int)c;
                    if (_charDictionary.TryLookup(codePoint, out var pinyin))
                    {
                        if (needsSeparator && sb.Length > 0)
                            sb.Append(separator);
                        sb.Append(ApplyStyle(pinyin, style));
                        needsSeparator = true;
                    }
                    else
                    {
                        if (needsSeparator && sb.Length > 0)
                            sb.Append(separator);
                        sb.Append(c);
                        needsSeparator = true;
                    }
                }
                else
                {
                    // 非漢字はそのまま出力
                    sb.Append(c);
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

            var result = new List<string>(text.Length);

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (IsCjkUnifiedIdeograph(c))
                {
                    var codePoint = (int)c;
                    if (_charDictionary.TryLookup(codePoint, out var pinyin))
                    {
                        result.Add(ApplyStyle(pinyin, style));
                    }
                    else
                    {
                        result.Add(c.ToString());
                    }
                }
                else
                {
                    result.Add(c.ToString());
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

            if (_charDictionary.TryLookup((int)c, out var pinyin))
                return new[] { pinyin };

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
        /// CJK統合漢字（U+4E00〜U+9FFF）であるかを判定する。
        /// </summary>
        private static bool IsCjkUnifiedIdeograph(char c)
        {
            return c >= '\u4E00' && c <= '\u9FFF';
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(ChineseG2PEngine));
        }
    }
}
