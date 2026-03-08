using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DotNetG2P.MeCab.Dictionary
{
    /// <summary>
    /// MeCab辞書ファイル一式 (sys.dic, matrix.bin, char.bin, unk.dic) を集約管理する。
    /// 同一パスの辞書はWeakReferenceキャッシュにより複数インスタンス間で共有される。
    /// </summary>
    /// <remarks>
    /// このクラスの <see cref="Load"/> および <see cref="Dispose"/> メソッドはスレッドセーフです。
    /// 辞書データ（<see cref="SystemDic"/>, <see cref="Matrix"/> 等）は読み取り専用のため、
    /// 複数の <see cref="MeCabTokenizer"/> インスタンスで安全に共有できます。
    /// </remarks>
    public sealed class DictionaryBundle : IDisposable
    {
        // 静的キャッシュ: 正規化パス → WeakReference<DictionaryBundle>
        private static readonly Dictionary<string, WeakReference<DictionaryBundle>> _cache
            = new Dictionary<string, WeakReference<DictionaryBundle>>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _cacheLock = new object();

        private int _refCount;
        private readonly string? _path;

        /// <summary>システム辞書</summary>
        public SystemDictionary SystemDic { get; }

        /// <summary>連接コスト行列</summary>
        public ConnectionMatrix Matrix { get; }

        /// <summary>文字種プロパティ</summary>
        public CharProperty CharProperty { get; }

        /// <summary>未知語辞書</summary>
        public UnknownDictionary UnknownDic { get; }

        private DictionaryBundle(
            string? path,
            SystemDictionary systemDic,
            ConnectionMatrix matrix,
            CharProperty charProperty,
            UnknownDictionary unknownDic)
        {
            _path = path;
            _refCount = 1;
            SystemDic = systemDic;
            Matrix = matrix;
            CharProperty = charProperty;
            UnknownDic = unknownDic;
        }

        /// <summary>
        /// 辞書ディレクトリから全辞書ファイルを一括読み込みする。
        /// 同一パスの辞書が既にキャッシュに存在する場合は共有インスタンスを返す。
        /// </summary>
        /// <param name="dictionaryDirectoryPath">辞書ディレクトリパス (sys.dic, matrix.bin, char.bin, unk.dic が格納されたディレクトリ)</param>
        /// <remarks>
        /// 参照カウント方式の WeakReference キャッシュにより、同一辞書パスの辞書データをプロセス内で共有します。
        /// </remarks>
        public static DictionaryBundle Load(string dictionaryDirectoryPath)
        {
            if (dictionaryDirectoryPath == null)
                throw new ArgumentNullException(nameof(dictionaryDirectoryPath));
            if (!Directory.Exists(dictionaryDirectoryPath))
                throw new DirectoryNotFoundException(
                    $"辞書ディレクトリが見つかりません: {dictionaryDirectoryPath}");

            string fullPath = Path.GetFullPath(dictionaryDirectoryPath);

            lock (_cacheLock)
            {
                if (_cache.TryGetValue(fullPath, out var weakRef))
                {
                    if (weakRef.TryGetTarget(out var cached))
                    {
                        // キャッシュヒット: 参照カウント増加
                        Interlocked.Increment(ref cached._refCount);
                        return cached;
                    }
                    else
                    {
                        // GCによりターゲットがクリアされたゾンビエントリを除去
                        _cache.Remove(fullPath);
                    }
                }

                // キャッシュミス: 新規読み込み
                var bundle = LoadInternal(fullPath);
                _cache[fullPath] = new WeakReference<DictionaryBundle>(bundle);
                return bundle;
            }
        }

        /// <summary>
        /// バイト配列から辞書データを一括読み込みする。
        /// WebGL等ファイルシステムが使えない環境向け。キャッシュは使用しない。
        /// </summary>
        /// <param name="sysDic">sys.dic のバイト配列</param>
        /// <param name="matrix">matrix.bin のバイト配列</param>
        /// <param name="charBin">char.bin のバイト配列</param>
        /// <param name="unkDic">unk.dic のバイト配列</param>
        public static DictionaryBundle Load(byte[] sysDic, byte[] matrix, byte[] charBin, byte[] unkDic)
        {
            if (sysDic == null) throw new ArgumentNullException(nameof(sysDic));
            if (matrix == null) throw new ArgumentNullException(nameof(matrix));
            if (charBin == null) throw new ArgumentNullException(nameof(charBin));
            if (unkDic == null) throw new ArgumentNullException(nameof(unkDic));

            // 読み込み順序: CharPropertyはUnknownDictionaryの前に必要
            var systemDic = SystemDictionary.Load(sysDic);
            var matrixObj = ConnectionMatrix.Load(matrix);
            var charProperty = CharProperty.Load(charBin);
            var unknownDic = UnknownDictionary.Load(unkDic, charProperty);

            return new DictionaryBundle(null, systemDic, matrixObj, charProperty, unknownDic);
        }

        private static DictionaryBundle LoadInternal(string fullPath)
        {
            string sysDicPath = Path.Combine(fullPath, "sys.dic");
            string matrixPath = Path.Combine(fullPath, "matrix.bin");
            string charBinPath = Path.Combine(fullPath, "char.bin");
            string unkDicPath = Path.Combine(fullPath, "unk.dic");

            // 読み込み順序: CharPropertyはUnknownDictionaryの前に必要
            var systemDic = SystemDictionary.Load(sysDicPath);
            var matrix = ConnectionMatrix.Load(matrixPath);
            var charProperty = CharProperty.Load(charBinPath);
            var unknownDic = UnknownDictionary.Load(unkDicPath, charProperty);

            return new DictionaryBundle(fullPath, systemDic, matrix, charProperty, unknownDic);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            int remaining = Interlocked.Decrement(ref _refCount);
            if (remaining < 0)
            {
                // 既にrefCount==0だった（二重Dispose）→元に戻す
                Interlocked.Increment(ref _refCount);
                return;
            }
            if (remaining == 0 && _path != null)
            {
                // 最後の参照者 → キャッシュから除去してリソース解放
                lock (_cacheLock)
                {
                    _cache.Remove(_path);
                }
            }
        }
    }
}
