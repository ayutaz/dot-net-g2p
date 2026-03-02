using System;
using System.IO;

namespace DotNetG2P.MeCab.Dictionary
{
    /// <summary>
    /// MeCab辞書ファイル一式 (sys.dic, matrix.bin, char.bin, unk.dic) を集約管理する。
    /// </summary>
    public sealed class DictionaryBundle : IDisposable
    {
        private bool _disposed;

        /// <summary>システム辞書</summary>
        public SystemDictionary SystemDic { get; }

        /// <summary>連接コスト行列</summary>
        public ConnectionMatrix Matrix { get; }

        /// <summary>文字種プロパティ</summary>
        public CharProperty CharProperty { get; }

        /// <summary>未知語辞書</summary>
        public UnknownDictionary UnknownDic { get; }

        private DictionaryBundle(
            SystemDictionary systemDic,
            ConnectionMatrix matrix,
            CharProperty charProperty,
            UnknownDictionary unknownDic)
        {
            SystemDic = systemDic;
            Matrix = matrix;
            CharProperty = charProperty;
            UnknownDic = unknownDic;
        }

        /// <summary>
        /// 辞書ディレクトリから全辞書ファイルを一括読み込みする。
        /// </summary>
        /// <param name="dictionaryDirectoryPath">辞書ディレクトリパス (sys.dic, matrix.bin, char.bin, unk.dic が格納されたディレクトリ)</param>
        public static DictionaryBundle Load(string dictionaryDirectoryPath)
        {
            if (dictionaryDirectoryPath == null)
                throw new ArgumentNullException(nameof(dictionaryDirectoryPath));
            if (!Directory.Exists(dictionaryDirectoryPath))
                throw new DirectoryNotFoundException(
                    $"辞書ディレクトリが見つかりません: {dictionaryDirectoryPath}");

            string sysDicPath = Path.Combine(dictionaryDirectoryPath, "sys.dic");
            string matrixPath = Path.Combine(dictionaryDirectoryPath, "matrix.bin");
            string charBinPath = Path.Combine(dictionaryDirectoryPath, "char.bin");
            string unkDicPath = Path.Combine(dictionaryDirectoryPath, "unk.dic");

            // 読み込み順序: CharPropertyはUnknownDictionaryの前に必要
            var systemDic = SystemDictionary.Load(sysDicPath);
            var matrix = ConnectionMatrix.Load(matrixPath);
            var charProperty = CharProperty.Load(charBinPath);
            var unknownDic = UnknownDictionary.Load(unkDicPath, charProperty);

            return new DictionaryBundle(systemDic, matrix, charProperty, unknownDic);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // 現在はすべてマネージドメモリ (byte[]) なのでDisposeで特別な処理は不要。
            // 将来Memory-Mapped Fileを使う場合にDispose処理を追加する。
            _disposed = true;
        }
    }
}
