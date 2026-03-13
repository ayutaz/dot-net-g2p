namespace DotNetG2P.Benchmarks;

internal static class BenchmarkInputs
{
    public const string EnglishWord = "hello";
    public const string EnglishSentence = "The quick brown fox jumps over the lazy dog.";
    public const string ChineseSentence = "今天天气非常好，我们一起去公园散步吧。";
    public const string ChineseShortText = "你好世界";
    public const string KoreanWord = "안녕하세요";
    public const string KoreanSentence = "오늘 날씨가 좋아서 공원에 산책하러 갑니다.";

    public static readonly string[] EnglishBatch =
    {
        "hello",
        "phoneme conversion",
        "benchmarking makes regressions visible",
        "the quick brown fox jumps over the lazy dog",
        "dot net g2p supports multiple languages"
    };

    public static readonly string[] ChineseBatch =
    {
        "你好世界",
        "学习中文很有意思",
        "中华人民共和国",
        "今天天气非常好",
        "机器学习改变世界"
    };

    public static readonly string[] KoreanBatch =
    {
        "안녕하세요",
        "반갑습니다",
        "오늘 날씨가 좋습니다",
        "한국어 발음을 변환합니다",
        "벤치마크로 성능을 측정합니다"
    };
}
