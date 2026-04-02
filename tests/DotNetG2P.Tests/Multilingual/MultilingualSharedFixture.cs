using System;
using System.IO;
using DotNetG2P;
using DotNetG2P.Chinese;
using DotNetG2P.English;
using DotNetG2P.MeCab;
using DotNetG2P.Multilingual;
using DotNetG2P.French;
using DotNetG2P.Korean;
using DotNetG2P.Portuguese;
using DotNetG2P.Spanish;
using DotNetG2P.Swedish;

namespace DotNetG2P.Tests.Multilingual
{
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class MultilingualSharedCollection : ICollectionFixture<MultilingualSharedFixture>
    {
        public const string Name = "MultilingualShared";
    }

    public sealed class MultilingualSharedFixture : IDisposable
    {
        public string? DictPath { get; }

        public bool HasDictionary => DictPath != null;

        public MultilingualG2PEngine? DefaultEngine { get; }

        public MultilingualG2PEngine? SpanishDefaultEngine { get; }

        public MultilingualG2PEngine? ChineseDefaultEngine { get; }

        public MultilingualG2PEngine? ChineseSpanishDefaultEngine { get; }

        public G2PEngine? JapaneseEngine { get; }

        public EnglishG2PEngine EnglishEngine { get; } = new EnglishG2PEngine();

        public ChineseG2PEngine? ChineseEngine { get; }

        public KoreanG2PEngine KoreanEngine { get; } = new KoreanG2PEngine();

        public SpanishG2PEngine SpanishEngine { get; } = new SpanishG2PEngine();

        public FrenchG2PEngine FrenchEngine { get; } = new FrenchG2PEngine();

        public PortugueseG2PEngine PortugueseEngine { get; } = new PortugueseG2PEngine();

        public SwedishG2PEngine SwedishEngine { get; } = new SwedishG2PEngine();

        public MultilingualG2PEngine? FrenchDefaultEngine { get; }

        public MultilingualG2PEngine? PortugueseDefaultEngine { get; }

        public MultilingualG2PEngine? SwedishDefaultEngine { get; }

        public MultilingualSharedFixture()
        {
            DictPath = FindDictPath();
            if (DictPath == null)
                return;

            DefaultEngine = new MultilingualG2PEngine(DictPath);
            SpanishDefaultEngine = new MultilingualG2PEngine(
                DictPath,
                new MultilingualG2POptions(defaultLatinLanguage: Language.Spanish));
            ChineseDefaultEngine = new MultilingualG2PEngine(
                DictPath,
                new MultilingualG2POptions(defaultCjkLanguage: Language.Chinese));
            ChineseSpanishDefaultEngine = new MultilingualG2PEngine(
                DictPath,
                new MultilingualG2POptions(defaultCjkLanguage: Language.Chinese, defaultLatinLanguage: Language.Spanish));
            JapaneseEngine = new G2PEngine(new MeCabTokenizer(DictPath), G2POptions.Default);
            ChineseEngine = new ChineseG2PEngine();
            FrenchDefaultEngine = new MultilingualG2PEngine(
                DictPath,
                new MultilingualG2POptions(defaultLatinLanguage: Language.French));
            PortugueseDefaultEngine = new MultilingualG2PEngine(
                DictPath,
                new MultilingualG2POptions(defaultLatinLanguage: Language.Portuguese));
            SwedishDefaultEngine = new MultilingualG2PEngine(
                DictPath,
                new MultilingualG2POptions(defaultLatinLanguage: Language.Swedish));
        }

        public void Dispose()
        {
            DefaultEngine?.Dispose();
            SpanishDefaultEngine?.Dispose();
            ChineseDefaultEngine?.Dispose();
            ChineseSpanishDefaultEngine?.Dispose();
            JapaneseEngine?.Dispose();
            EnglishEngine.Dispose();
            ChineseEngine?.Dispose();
            KoreanEngine.Dispose();
            SpanishEngine.Dispose();
            FrenchDefaultEngine?.Dispose();
            FrenchEngine.Dispose();
            PortugueseDefaultEngine?.Dispose();
            PortugueseEngine.Dispose();
            SwedishDefaultEngine?.Dispose();
            SwedishEngine.Dispose();
        }

        private static string? FindDictPath()
        {
            var envPath = Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
                return envPath;

            var candidates = new[]
            {
                @"C:\Users\yuta\Desktop\Private\open_jtalk_dic_utf_8-1.11",
                @"C:\naist-jdic",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "naist-jdic"),
                "/usr/local/share/naist-jdic",
                "/usr/share/naist-jdic",
            };

            foreach (var path in candidates)
            {
                if (Directory.Exists(path))
                    return path;
            }

            return null;
        }
    }
}
