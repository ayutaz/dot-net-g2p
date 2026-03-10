using DotNetG2P.Chinese;

namespace DotNetG2P.Tests.Multilingual
{
    public class EmbeddedChineseDictionaryCacheTests
    {
        [Fact]
        public void ChineseG2PEngine_EmbeddedDictionariesAreShared()
        {
            var sharedCharDictionary = EmbeddedChineseDictionaryCache.CharDictionary;
            var sharedPhraseDictionary = EmbeddedChineseDictionaryCache.PhraseDictionary;

            using var engine = new ChineseG2PEngine();

            Assert.Same(sharedCharDictionary, engine.CharDictionaryInternal);
            Assert.Same(sharedPhraseDictionary, engine.PhraseDictionaryInternal);
        }
    }
}
