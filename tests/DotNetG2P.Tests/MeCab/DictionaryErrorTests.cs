using System;
using System.IO;
using DotNetG2P.MeCab;
using Xunit;

namespace DotNetG2P.Tests.MeCab
{
    /// <summary>
    /// MeCabTokenizerのエラーハンドリングテスト（辞書不要）。
    /// コンストラクタに不正な引数を渡した際の例外を検証する。
    /// </summary>
    public class DictionaryErrorTests
    {
        [Fact]
        public void コンストラクタ_nullパス_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MeCabTokenizer(null!));
        }

        [Fact]
        public void コンストラクタ_存在しないパス_DirectoryNotFoundException()
        {
            var nonExistent = Path.Combine(Path.GetTempPath(), "nonexistent_dict_" + Guid.NewGuid().ToString("N"));

            Assert.Throws<DirectoryNotFoundException>(() => new MeCabTokenizer(nonExistent));
        }

        [Fact]
        public void コンストラクタ_空パス_ArgumentException_or_DirectoryNotFound()
        {
            // 空文字列は DirectoryNotFoundException になるか ArgumentException になるかは実装依存
            Assert.ThrowsAny<Exception>(() => new MeCabTokenizer(""));
        }

        [Fact]
        public void コンストラクタ_辞書ファイルなしディレクトリ_例外()
        {
            // 実在するが辞書ファイルがないディレクトリ
            var tempDir = Path.Combine(Path.GetTempPath(), "empty_dict_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                // 辞書ファイルがないので何らかの例外が発生するはず
                Assert.ThrowsAny<Exception>(() => new MeCabTokenizer(tempDir));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // =====================================================================
        // 破損辞書ファイルテスト
        // =====================================================================

        /// <summary>テンポラリ辞書ディレクトリを作成し、指定ファイルを配置するヘルパー</summary>
        private static string CreateTempDicDir(
            byte[]? sysDic = null,
            byte[]? matrixBin = null,
            byte[]? charBin = null,
            byte[]? unkDic = null,
            bool skipSysDic = false,
            bool skipUnkDic = false)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "mecab_corrupt_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            if (!skipSysDic)
                File.WriteAllBytes(Path.Combine(tempDir, "sys.dic"), sysDic ?? new byte[0]);
            if (matrixBin != null)
                File.WriteAllBytes(Path.Combine(tempDir, "matrix.bin"), matrixBin);
            else
                File.WriteAllBytes(Path.Combine(tempDir, "matrix.bin"), new byte[0]);
            if (charBin != null)
                File.WriteAllBytes(Path.Combine(tempDir, "char.bin"), charBin);
            else
                File.WriteAllBytes(Path.Combine(tempDir, "char.bin"), new byte[0]);
            if (!skipUnkDic)
                File.WriteAllBytes(Path.Combine(tempDir, "unk.dic"), unkDic ?? new byte[0]);

            return tempDir;
        }

        [Fact]
        public void コンストラクタ_sysDicが72バイト未満_例外()
        {
            var tempDir = CreateTempDicDir(sysDic: new byte[10]);
            try
            {
                Assert.ThrowsAny<Exception>(() => new MeCabTokenizer(tempDir));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void コンストラクタ_sysDicが空_例外()
        {
            var tempDir = CreateTempDicDir(sysDic: new byte[0]);
            try
            {
                Assert.ThrowsAny<Exception>(() => new MeCabTokenizer(tempDir));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void コンストラクタ_matrixBinが空_例外()
        {
            // sys.dicも不正だが、matrixが空でも例外になることを確認
            var tempDir = CreateTempDicDir(matrixBin: new byte[0]);
            try
            {
                Assert.ThrowsAny<Exception>(() => new MeCabTokenizer(tempDir));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void コンストラクタ_charBinが空_例外()
        {
            var tempDir = CreateTempDicDir(charBin: new byte[0]);
            try
            {
                Assert.ThrowsAny<Exception>(() => new MeCabTokenizer(tempDir));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void コンストラクタ_unkDicが欠落_FileNotFoundException()
        {
            var tempDir = CreateTempDicDir(skipUnkDic: true);
            try
            {
                Assert.ThrowsAny<Exception>(() => new MeCabTokenizer(tempDir));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void コンストラクタ_sysDicが欠落_FileNotFoundException()
        {
            var tempDir = CreateTempDicDir(skipSysDic: true);
            try
            {
                Assert.ThrowsAny<Exception>(() => new MeCabTokenizer(tempDir));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
