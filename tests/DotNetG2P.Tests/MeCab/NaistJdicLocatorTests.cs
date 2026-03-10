using System;
using System.IO;
using DotNetG2P.MeCab;

namespace DotNetG2P.Tests.MeCab
{
    public class NaistJdicLocatorTests
    {
        [Fact]
        public void GetDefaultInstallPath_UserProfile配下のnaist_jdic()
        {
            var result = NaistJdicLocator.GetDefaultInstallPath();

            Assert.EndsWith("naist-jdic", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void IsValidDictionaryDirectory_4ファイルあり_true()
        {
            var tempDir = CreateTempDictionaryDirectory();

            try
            {
                Assert.True(NaistJdicLocator.IsValidDictionaryDirectory(tempDir));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void IsValidDictionaryDirectory_ファイル欠落_false()
        {
            var tempDir = CreateTempDictionaryDirectory();
            File.Delete(Path.Combine(tempDir, "unk.dic"));

            try
            {
                Assert.False(NaistJdicLocator.IsValidDictionaryDirectory(tempDir));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        private static string CreateTempDictionaryDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "naist_jdic_locator_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            File.WriteAllBytes(Path.Combine(tempDir, "sys.dic"), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(tempDir, "matrix.bin"), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(tempDir, "char.bin"), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(tempDir, "unk.dic"), new byte[] { 1 });
            return tempDir;
        }
    }
}
