using System.Globalization;
using System.Text;
using System.Text.Json;
using DotNetG2P.Swedish;

var options = EvalCliOptions.Parse(args);
var repoRoot = RepositoryPaths.ResolveRepoRoot();
var inputRoot = RepositoryPaths.ResolveAgainstRepo(repoRoot, options.InputRoot ?? Path.Combine("artifacts", "swedish-eval", "corpora"));
var outputRoot = RepositoryPaths.ResolveAgainstRepo(repoRoot, options.OutputRoot ?? Path.Combine("artifacts", "swedish-eval", "reports", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)));
var thresholdsPath = RepositoryPaths.ResolveAgainstRepo(repoRoot, options.ThresholdsPath ?? Path.Combine("tools", "swedish_eval_thresholds.json"));
var exceptionMasterPath = Path.Combine(repoRoot, "src", "DotNetG2P.Swedish", "Data", "swedish_exceptions.master.tsv");

Directory.CreateDirectory(outputRoot);
Directory.CreateDirectory(Path.Combine(outputRoot, "mismatches"));

var datasetDefinitions = DatasetDefinition.CreateSet(options.DatasetSet);
var thresholds = File.Exists(thresholdsPath)
    ? EvalThresholdConfig.Load(thresholdsPath)
    : EvalThresholdConfig.Empty;
var exceptionCategories = File.Exists(exceptionMasterPath)
    ? ExceptionCategoryMap.Load(exceptionMasterPath)
    : ExceptionCategoryMap.Empty;
var profiles = EvalProfile.CreateDefault(options.ProfileNames);

var summaries = new List<EvalSummary>();
var categorySummaries = new List<CategorySummaryRow>();
var failedThresholds = new List<string>();

foreach (var dataset in datasetDefinitions)
{
    var inputPath = Path.Combine(inputRoot, dataset.FileName);
    if (!File.Exists(inputPath))
        throw new FileNotFoundException($"Dataset not found: {inputPath}");

    var rows = File.ReadAllLines(inputPath)
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .Select(ParseRow)
        .ToArray();

    Console.WriteLine($"Evaluating {dataset.Name} ({rows.Length} rows)");

    foreach (var profile in profiles)
    {
        using var engine = new SwedishG2PEngine(new SwedishG2POptions(
            dialect: dataset.Dialect,
            includeStress: false,
            enableAllophones: profile.EnableAllophones,
            enableExceptionDictionary: profile.EnableExceptionDictionary,
            allophoneFeatures: profile.AllophoneFeatures));

        var result = CorpusEvaluator.Evaluate(rows, engine, dataset.SourceKind, dataset.Dialect, exceptionCategories, profile.Name, options.MismatchLimit);
        var threshold = thresholds.TryGetThreshold(dataset.FileName, profile.Name, out var configuredThreshold)
            ? configuredThreshold
            : (double?)null;
        var passed = threshold == null || result.PhonemeErrorRate <= threshold.Value;

        summaries.Add(new EvalSummary(
            dataset.Name,
            dataset.FileName,
            profile.Name,
            dataset.SourceKind.ToString(),
            dataset.Dialect.ToString(),
            result.Cases,
            result.EvaluatedCases,
            result.ExactMatches,
            result.WordErrorRate,
            result.PhonemeErrorRate,
            result.TotalErrors,
            result.TotalReferencePhonemes,
            threshold,
            passed));

        categorySummaries.AddRange(result.CategorySummaries.Select(category => new CategorySummaryRow(
            dataset.Name,
            profile.Name,
            category.Category,
            category.Count,
            category.AverageDistance)));

        WriteMismatchReport(Path.Combine(outputRoot, "mismatches", $"{dataset.Name}__{profile.Name}.tsv"), result.Mismatches);
        Console.WriteLine($"  {profile.Name}: PER={result.PhonemeErrorRate:P2}, WER={result.WordErrorRate:P2}, mismatches={result.Mismatches.Count}, threshold={(threshold?.ToString("P2", CultureInfo.InvariantCulture) ?? "n/a")}");

        if (options.EnforceThresholds && !passed)
            failedThresholds.Add($"{dataset.Name}/{profile.Name}: {result.PhonemeErrorRate:P2} > {threshold:P2}");
    }
}

WriteSummaryReport(Path.Combine(outputRoot, "summary.tsv"), summaries);
WriteCategoryReport(Path.Combine(outputRoot, "category_summary.tsv"), categorySummaries);
File.WriteAllText(Path.Combine(outputRoot, "summary.json"), JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true }));
File.WriteAllText(Path.Combine(outputRoot, "category_summary.json"), JsonSerializer.Serialize(categorySummaries, new JsonSerializerOptions { WriteIndented = true }));

if (failedThresholds.Count > 0)
{
    foreach (var failed in failedThresholds)
        Console.Error.WriteLine(failed);
    return 1;
}

return 0;

static (string Word, string Reference) ParseRow(string line)
{
    var parts = line.Split('\t');
    if (parts.Length < 2)
        throw new InvalidDataException($"Malformed TSV row: {line}");

    return (parts[0].Trim(), parts[1].Trim());
}

static void WriteSummaryReport(string path, IReadOnlyList<EvalSummary> rows)
{
    var lines = new List<string>(rows.Count + 1)
    {
        "dataset\tfile\tprofile\tsource\tdialect\tcases\tevaluated_cases\texact_matches\tword_error_rate\tphoneme_error_rate\ttotal_errors\treference_phonemes\tthreshold\tpassed"
    };

    foreach (var row in rows)
    {
        lines.Add(string.Join('\t', new[]
        {
            row.Dataset,
            row.FileName,
            row.Profile,
            row.Source,
            row.Dialect,
            row.Cases.ToString(CultureInfo.InvariantCulture),
            row.EvaluatedCases.ToString(CultureInfo.InvariantCulture),
            row.ExactMatches.ToString(CultureInfo.InvariantCulture),
            row.WordErrorRate.ToString("F6", CultureInfo.InvariantCulture),
            row.PhonemeErrorRate.ToString("F6", CultureInfo.InvariantCulture),
            row.TotalErrors.ToString(CultureInfo.InvariantCulture),
            row.TotalReferencePhonemes.ToString(CultureInfo.InvariantCulture),
            row.Threshold?.ToString("F6", CultureInfo.InvariantCulture) ?? string.Empty,
            row.Passed ? "true" : "false",
        }));
    }

    File.WriteAllLines(path, lines, new UTF8Encoding(false));
}

static void WriteCategoryReport(string path, IReadOnlyList<CategorySummaryRow> rows)
{
    var lines = new List<string>(rows.Count + 1)
    {
        "dataset\tprofile\tcategory\tcount\taverage_distance"
    };

    foreach (var row in rows.OrderBy(x => x.Dataset).ThenBy(x => x.Profile).ThenByDescending(x => x.Count).ThenBy(x => x.Category))
    {
        lines.Add(string.Join('\t', new[]
        {
            row.Dataset,
            row.Profile,
            row.Category,
            row.Count.ToString(CultureInfo.InvariantCulture),
            row.AverageDistance.ToString("F6", CultureInfo.InvariantCulture),
        }));
    }

    File.WriteAllLines(path, lines, new UTF8Encoding(false));
}

static void WriteMismatchReport(string path, IReadOnlyList<MismatchRow> rows)
{
    var lines = new List<string>(rows.Count + 1)
    {
        "word\tcategory\tdistance\tpredicted\treference"
    };

    foreach (var row in rows)
    {
        lines.Add(string.Join('\t', new[]
        {
            row.Word,
            row.Category,
            row.Distance.ToString(CultureInfo.InvariantCulture),
            row.Predicted,
            row.Reference,
        }));
    }

    File.WriteAllLines(path, lines, new UTF8Encoding(false));
}

internal sealed record EvalCliOptions
{
    public string? InputRoot { get; private init; }
    public string? OutputRoot { get; private init; }
    public string? ThresholdsPath { get; private init; }
    public string DatasetSet { get; private init; } = "full";
    public bool EnforceThresholds { get; private init; }
    public int MismatchLimit { get; private init; } = 250;
    public string[] ProfileNames { get; private init; } = Array.Empty<string>();

    public static EvalCliOptions Parse(string[] args)
    {
        var options = new EvalCliOptions();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--input-root":
                    options = options with { InputRoot = args[++i] };
                    break;
                case "--output-root":
                    options = options with { OutputRoot = args[++i] };
                    break;
                case "--thresholds":
                    options = options with { ThresholdsPath = args[++i] };
                    break;
                case "--dataset-set":
                    options = options with { DatasetSet = args[++i].ToLowerInvariant() };
                    break;
                case "--mismatch-limit":
                    options = options with { MismatchLimit = int.Parse(args[++i], CultureInfo.InvariantCulture) };
                    break;
                case "--profiles":
                    options = options with { ProfileNames = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) };
                    break;
                case "--enforce-thresholds":
                    options = options with { EnforceThresholds = true };
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }

        return options;
    }
}

internal static class RepositoryPaths
{
    public static string ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "DotNetG2P.slnx")))
                return current.FullName;
            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    public static string ResolveAgainstRepo(string repoRoot, string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(repoRoot, path));
    }
}

internal enum SourceKind : byte
{
    WikiPron = 0,
    IpaDict = 1,
}

internal sealed record DatasetDefinition(string Name, string FileName, SourceKind SourceKind, SwedishDialect Dialect)
{
    public static IReadOnlyList<DatasetDefinition> CreateSet(string setName)
    {
        return setName switch
        {
            "sample" => new[]
            {
                new DatasetDefinition("ipa_dict_sv_se", "ipa_dict_sv_se_sample.tsv", SourceKind.IpaDict, SwedishDialect.Central),
                new DatasetDefinition("wikipron_swe_latn_broad", "wikipron_swe_latn_broad_filtered_sample.tsv", SourceKind.WikiPron, SwedishDialect.Central),
            },
            "full" => new[]
            {
                new DatasetDefinition("ipa_dict_sv_se", "ipa_dict_sv_se_full.tsv", SourceKind.IpaDict, SwedishDialect.Central),
                new DatasetDefinition("wikipron_swe_latn_broad", "wikipron_swe_latn_broad_filtered_full.tsv", SourceKind.WikiPron, SwedishDialect.Central),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(setName), setName, "dataset-set must be 'sample' or 'full'."),
        };
    }
}

internal sealed record EvalProfile(string Name, bool EnableAllophones, SwedishAllophoneFeatures AllophoneFeatures, bool EnableExceptionDictionary)
{
    public static IReadOnlyList<EvalProfile> CreateDefault(string[] requestedNames)
    {
        var profiles = new Dictionary<string, EvalProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["base"] = new EvalProfile("base", false, SwedishAllophoneFeatures.CentralDefault, true),
            ["allophones"] = new EvalProfile("allophones", true, SwedishAllophoneFeatures.CentralDefault, true),
            ["no_exceptions"] = new EvalProfile("no_exceptions", false, SwedishAllophoneFeatures.CentralDefault, false),
        };

        if (requestedNames.Length == 0)
            return profiles.Values.ToArray();

        return requestedNames.Select(name => profiles.TryGetValue(name, out var profile)
            ? profile
            : throw new ArgumentException($"Unknown profile: {name}")).ToArray();
    }
}

internal static class CorpusEvaluator
{
    public static EvalResult Evaluate(
        IReadOnlyList<(string Word, string Reference)> rows,
        SwedishG2PEngine engine,
        SourceKind sourceKind,
        SwedishDialect dialect,
        ExceptionCategoryMap exceptionCategories,
        string profile,
        int mismatchLimit)
    {
        var totalErrors = 0;
        var totalReferencePhonemes = 0;
        var exactMatches = 0;
        var evaluatedCases = 0;
        var mismatches = new List<MismatchRow>();

        foreach (var row in rows)
        {
            var predicted = NormalizePredicted(engine.ToPhonemeList(row.Word), dialect);
            var reference = NormalizeReference(row.Reference, sourceKind, dialect);
            if (reference.Length == 0 || predicted.Length == 0)
                continue;

            evaluatedCases++;
            var distance = LevenshteinDistance(predicted, reference);
            totalErrors += distance;
            totalReferencePhonemes += reference.Length;

            if (distance == 0)
            {
                exactMatches++;
                continue;
            }

            mismatches.Add(new MismatchRow(
                row.Word,
                exceptionCategories.Classify(row.Word, predicted, reference),
                distance,
                string.Join(" ", predicted),
                string.Join(" ", reference)));
        }

        var per = totalReferencePhonemes == 0 ? 0d : (double)totalErrors / totalReferencePhonemes;
        var wer = evaluatedCases == 0 ? 0d : (double)(evaluatedCases - exactMatches) / evaluatedCases;
        var categorySummaries = mismatches
            .GroupBy(x => x.Category, StringComparer.Ordinal)
            .Select(group => new CategorySummary(group.Key, group.Count(), group.Average(x => x.Distance)))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Category, StringComparer.Ordinal)
            .ToArray();

        var orderedMismatches = mismatches
            .OrderByDescending(x => x.Distance)
            .ThenBy(x => x.Category, StringComparer.Ordinal)
            .ThenBy(x => x.Word, StringComparer.Ordinal)
            .Take(mismatchLimit)
            .ToArray();

        return new EvalResult(rows.Count, evaluatedCases, exactMatches, wer, per, totalErrors, totalReferencePhonemes, orderedMismatches, categorySummaries);
    }

    /// <summary>
    /// エンジン出力の音素列を正規化する。
    /// 長母音はIPA基底母音+ː（長音記号）に分解し、baseプロファイル（ストレスなし）で比較する。
    /// </summary>
    private static string[] NormalizePredicted(IReadOnlyList<SwedishPhoneme> phonemes, SwedishDialect dialect)
    {
        var result = new List<string>(phonemes.Count);
        for (var i = 0; i < phonemes.Count; i++)
        {
            var symbol = NormalizePredictedPhoneme(phonemes[i].Phoneme, dialect);
            // 長母音は基底母音+ːに分解されるため、TokenizeIpaで再トークン化される参照側と一致する
            result.Add(symbol);
        }
        return result.ToArray();
    }

    private static string NormalizePredictedPhoneme(SwedishIpaPhoneme phoneme, SwedishDialect dialect)
    {
        return phoneme switch
        {
            // 長母音 — 基底母音のみ（ːは参照正規化で除去されるため付与しない）
            SwedishIpaPhoneme.LongI => "i",
            SwedishIpaPhoneme.LongY => "y",
            SwedishIpaPhoneme.LongUCentral => "\u0289",      // ʉ
            SwedishIpaPhoneme.LongU => "u",
            SwedishIpaPhoneme.LongE => "e",
            SwedishIpaPhoneme.LongOe => "\u00F8",             // ø
            SwedishIpaPhoneme.LongEh => "\u025B",             // ɛ
            SwedishIpaPhoneme.LongO => "o",
            SwedishIpaPhoneme.LongA => "\u0251",              // ɑ

            // 短母音
            SwedishIpaPhoneme.ShortI => "\u026A",             // ɪ
            SwedishIpaPhoneme.ShortY => "\u028F",             // ʏ → 参照側にない場合は y に置換される可能性あり
            SwedishIpaPhoneme.ShortUCentral => "\u0275",      // ɵ
            SwedishIpaPhoneme.ShortU => "\u028A",             // ʊ
            SwedishIpaPhoneme.ShortE => "\u025B",             // ɛ
            SwedishIpaPhoneme.ShortOe => "\u0153",            // œ
            SwedishIpaPhoneme.ShortO => "\u0254",             // ɔ
            SwedishIpaPhoneme.ShortA => "a",
            SwedishIpaPhoneme.Schwa => "\u0259",              // ə

            // 破裂音
            SwedishIpaPhoneme.P => "p",
            SwedishIpaPhoneme.B => "b",
            SwedishIpaPhoneme.T => "t",
            SwedishIpaPhoneme.D => "d",
            SwedishIpaPhoneme.K => "k",
            SwedishIpaPhoneme.G => "\u0261",                  // ɡ

            // 摩擦音
            SwedishIpaPhoneme.F => "f",
            SwedishIpaPhoneme.V => "v",
            SwedishIpaPhoneme.S => "s",
            SwedishIpaPhoneme.H => "h",
            SwedishIpaPhoneme.Sj => "\u0267",                 // ɧ
            SwedishIpaPhoneme.Tj => "\u0255",                 // ɕ

            // 鼻音
            SwedishIpaPhoneme.M => "m",
            SwedishIpaPhoneme.N => "n",
            SwedishIpaPhoneme.Ng => "\u014B",                 // ŋ

            // 接近音・ふるえ音
            SwedishIpaPhoneme.L => "l",
            SwedishIpaPhoneme.R => "r",
            SwedishIpaPhoneme.J => "j",

            // そり舌音
            SwedishIpaPhoneme.RetroT => "\u0288",             // ʈ
            SwedishIpaPhoneme.RetroD => "\u0256",             // ɖ
            SwedishIpaPhoneme.RetroN => "\u0273",             // ɳ
            SwedishIpaPhoneme.RetroL => "\u026D",             // ɭ
            SwedishIpaPhoneme.RetroS => "\u0282",             // ʂ

            // 破擦音
            SwedishIpaPhoneme.TjAffricate => "t\u0361\u0255", // t͡ɕ

            _ => throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null),
        };
    }

    /// <summary>
    /// 参照IPA転写を正規化する。
    /// ipa-dict: スラッシュ・ストレスマーク・声調アクセント（²）・長音記号(ː)・ピリオドを除去し、トークン化。
    /// WikiPron: パイプをスペースに置換し、スペース区切り。ストレスマーク・長音記号は除去。
    /// </summary>
    private static string[] NormalizeReference(string transcription, SourceKind sourceKind, SwedishDialect dialect)
    {
        var raw = sourceKind == SourceKind.WikiPron
            ? transcription.Replace("|", " ").Trim()
            : transcription.Replace("/", string.Empty)
                .Replace("\u02C8", string.Empty)   // ˈ ストレスマーク除去
                .Replace("\u02CC", string.Empty)   // ˌ 副ストレスマーク除去
                .Replace("\u00B2", string.Empty)    // ² 声調アクセント2除去
                .Replace(".", string.Empty)
                .Replace(" ", string.Empty)
                .Trim();

        // WikiPronでもストレスマーク・長音記号を除去
        if (sourceKind == SourceKind.WikiPron)
        {
            raw = raw
                .Replace("\u02C8", string.Empty)   // ˈ
                .Replace("\u02CC", string.Empty);   // ˌ
        }

        var tokens = sourceKind == SourceKind.WikiPron
            ? raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            : TokenizeIpa(raw);

        for (var i = 0; i < tokens.Length; i++)
            tokens[i] = NormalizeReferenceToken(tokens[i], dialect);

        return tokens.Where(token => token.Length > 0).ToArray();
    }

    /// <summary>
    /// 参照側トークンの正規化。
    /// g(ASCII)/ɡ(IPA)の統合、長音記号(ː)の除去など。
    /// </summary>
    private static string NormalizeReferenceToken(string token, SwedishDialect dialect)
    {
        return token switch
        {
            // g (ASCII) / ɡ (IPA) の統合
            "g" or "\u0261" => "\u0261",          // ɡ
            // 長音記号の除去
            "\u02D0" => string.Empty,             // ː
            // ʏ → 参照側にない場合の統合は行わない（そのまま保持）
            _ => token,
        };
    }

    /// <summary>
    /// IPA連続文字列をトークンに分割する。
    /// 長音記号(ː)、結合記号、マルチ文字音素を適切に処理する。
    /// </summary>
    private static string[] TokenizeIpa(string text)
    {
        var tokens = new List<string>();
        for (var i = 0; i < text.Length;)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                i++;
                continue;
            }

            // 長音記号をスキップ（baseプロファイルでは長短を区別しない）
            if (text[i] == '\u02D0') // ː
            {
                i++;
                continue;
            }

            // 結合分音記号(̈)を先行文字に結合
            if (i + 1 < text.Length && text[i + 1] == '\u0308')
            {
                tokens.Add(text.Substring(i, 2));
                i += 2;
                continue;
            }

            // 結合ティルダ(̃)を先行文字に結合
            if (i + 1 < text.Length && text[i + 1] == '\u0303')
            {
                tokens.Add(text.Substring(i, 2));
                i += 2;
                continue;
            }

            // 音節主音マーカー(̩)を先行文字に結合
            if (i + 1 < text.Length && text[i + 1] == '\u0329')
            {
                tokens.Add(text.Substring(i, 2));
                i += 2;
                continue;
            }

            // 非音節マーカー(̯)を先行文字に結合
            if (i + 1 < text.Length && text[i + 1] == '\u032F')
            {
                tokens.Add(text.Substring(i, 2));
                i += 2;
                continue;
            }

            // マルチ文字音素の検出: t͡ɕ (破擦音)
            if (TryMatch(text, i, "t\u0361\u0255", out var consumed))   // t͡ɕ
            {
                tokens.Add(text.Substring(i, consumed));
                i += consumed;
                continue;
            }

            tokens.Add(text[i].ToString());
            i++;
        }

        return tokens.ToArray();
    }

    private static bool TryMatch(string text, int start, string match, out int consumed)
    {
        if (start + match.Length <= text.Length && string.CompareOrdinal(text, start, match, 0, match.Length) == 0)
        {
            consumed = match.Length;
            return true;
        }

        consumed = 0;
        return false;
    }

    private static int LevenshteinDistance<T>(IReadOnlyList<T> source, IReadOnlyList<T> target)
    {
        if (source.Count == 0)
            return target.Count;
        if (target.Count == 0)
            return source.Count;

        var previous = new int[target.Count + 1];
        var current = new int[target.Count + 1];

        for (var j = 0; j <= target.Count; j++)
            previous[j] = j;

        for (var i = 1; i <= source.Count; i++)
        {
            current[0] = i;
            for (var j = 1; j <= target.Count; j++)
            {
                var cost = EqualityComparer<T>.Default.Equals(source[i - 1], target[j - 1]) ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[target.Count];
    }
}

internal sealed class ExceptionCategoryMap
{
    public static readonly ExceptionCategoryMap Empty = new ExceptionCategoryMap(new Dictionary<string, string>(StringComparer.Ordinal));

    private readonly Dictionary<string, string> _categories;

    private ExceptionCategoryMap(Dictionary<string, string> categories)
    {
        _categories = categories;
    }

    public static ExceptionCategoryMap Load(string path)
    {
        var categories = File.ReadAllLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#", StringComparison.Ordinal) && !line.StartsWith("surface\t", StringComparison.Ordinal))
            .Select(line => line.Split('\t'))
            .Where(parts => parts.Length >= 3)
            .GroupBy(parts => parts[0], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First()[2], StringComparer.Ordinal);

        return new ExceptionCategoryMap(categories);
    }

    /// <summary>
    /// スウェーデン語固有のエラーカテゴリ分類。
    /// </summary>
    public string Classify(string word, string[] predicted, string[] reference)
    {
        if (_categories.TryGetValue(word, out var category))
            return category;

        var predJoined = string.Join(" ", predicted);
        var refJoined = string.Join(" ", reference);

        // 母音長の誤り（長短の混同）
        if (IsVowelLengthMismatch(predicted, reference))
            return "vowel_length";

        // 母音質の誤り（類似母音の混同: ɪ↔i, ɛ↔e, ɔ↔o, ʊ↔u, ɵ↔ʉ 等）
        if (IsVowelQualityMismatch(predicted, reference))
            return "vowel_quality";

        // そり舌音関連: ʈ↔rt, ɖ↔rd, ɳ↔rn, ɭ↔rl, ʂ↔rs
        if (IsRetroflexMismatch(predicted, reference))
            return "retroflex";

        // sj音/tj音の誤り: ɧ↔ɕ, ɕ↔ʃ 等
        if (IsFricativeMismatch(predicted, reference))
            return "sj_tj";

        // 音素数の差（黙字・挿入音）
        if (Math.Abs(predicted.Length - reference.Length) >= 1)
            return "insertion_deletion";

        // 外来語候補
        if (LooksLikeForeignWord(word))
            return "foreign_word";

        // 子音の誤り
        if (HasConsonantMismatch(predicted, reference))
            return "consonant";

        return "other";
    }

    private static readonly HashSet<string> s_longVowelBases = new(StringComparer.Ordinal)
    {
        "i", "y", "\u0289", "u", "e", "\u00F8", "\u025B", "o", "\u0251",
    };

    private static readonly HashSet<(string, string)> s_longShortPairs = new()
    {
        // 長母音基底 ↔ 短母音
        ("i", "\u026A"), ("\u026A", "i"),           // i ↔ ɪ
        ("y", "\u028F"), ("\u028F", "y"),           // y ↔ ʏ
        ("\u0289", "\u0275"), ("\u0275", "\u0289"), // ʉ ↔ ɵ
        ("u", "\u028A"), ("\u028A", "u"),           // u ↔ ʊ
        ("e", "\u025B"), ("\u025B", "e"),           // e ↔ ɛ
        ("\u00F8", "\u0153"), ("\u0153", "\u00F8"), // ø ↔ œ
        ("o", "\u0254"), ("\u0254", "o"),           // o ↔ ɔ
        ("\u0251", "a"), ("a", "\u0251"),           // ɑ ↔ a
    };

    private static bool IsVowelLengthMismatch(string[] predicted, string[] reference)
    {
        var minLen = Math.Min(predicted.Length, reference.Length);
        for (var i = 0; i < minLen; i++)
        {
            if (predicted[i] != reference[i] && s_longShortPairs.Contains((predicted[i], reference[i])))
                return true;
        }
        return false;
    }

    private static readonly HashSet<string> s_vowels = new(StringComparer.Ordinal)
    {
        "i", "y", "\u0289", "u", "e", "\u00F8", "\u025B", "o", "\u0251", "a",
        "\u026A", "\u028F", "\u0275", "\u028A", "\u0153", "\u0254", "\u0259",
        "\u025C", "\u0258",
    };

    private static bool IsVowelQualityMismatch(string[] predicted, string[] reference)
    {
        var minLen = Math.Min(predicted.Length, reference.Length);
        for (var i = 0; i < minLen; i++)
        {
            if (predicted[i] != reference[i]
                && s_vowels.Contains(predicted[i]) && s_vowels.Contains(reference[i]))
                return true;
        }
        return false;
    }

    private static readonly HashSet<string> s_retroflexes = new(StringComparer.Ordinal)
    {
        "\u0288", "\u0256", "\u0273", "\u026D", "\u0282",  // ʈ ɖ ɳ ɭ ʂ
        "r", "t", "d", "n", "l", "s",
    };

    private static bool IsRetroflexMismatch(string[] predicted, string[] reference)
    {
        var minLen = Math.Min(predicted.Length, reference.Length);
        for (var i = 0; i < minLen; i++)
        {
            if (predicted[i] != reference[i])
            {
                var predIsRetroflex = predicted[i] == "\u0288" || predicted[i] == "\u0256"
                    || predicted[i] == "\u0273" || predicted[i] == "\u026D" || predicted[i] == "\u0282";
                var refIsRetroflex = reference[i] == "\u0288" || reference[i] == "\u0256"
                    || reference[i] == "\u0273" || reference[i] == "\u026D" || reference[i] == "\u0282";
                if (predIsRetroflex || refIsRetroflex)
                    return true;
            }
        }
        return false;
    }

    private static bool IsFricativeMismatch(string[] predicted, string[] reference)
    {
        var fricatives = new HashSet<string>(StringComparer.Ordinal) { "\u0267", "\u0255", "\u0283", "\u0292" }; // ɧ ɕ ʃ ʒ
        var minLen = Math.Min(predicted.Length, reference.Length);
        for (var i = 0; i < minLen; i++)
        {
            if (predicted[i] != reference[i]
                && (fricatives.Contains(predicted[i]) || fricatives.Contains(reference[i])))
                return true;
        }
        return false;
    }

    private static bool LooksLikeForeignWord(string word)
    {
        return word.Contains('q')
            || word.Contains('z')
            || word.Contains('w')
            || word.Contains("ph", StringComparison.Ordinal)
            || word.Contains("ck", StringComparison.Ordinal)
            || word.Contains("th", StringComparison.Ordinal)
            || word.EndsWith("ing", StringComparison.OrdinalIgnoreCase)
            || word.EndsWith("tion", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly HashSet<string> s_consonants = new(StringComparer.Ordinal)
    {
        "p", "b", "t", "d", "k", "\u0261", "f", "v", "s", "h",
        "\u0267", "\u0255", "m", "n", "\u014B", "l", "r", "j",
        "\u0288", "\u0256", "\u0273", "\u026D", "\u0282",
    };

    private static bool HasConsonantMismatch(string[] predicted, string[] reference)
    {
        var minLen = Math.Min(predicted.Length, reference.Length);
        for (var i = 0; i < minLen; i++)
        {
            if (predicted[i] != reference[i]
                && (s_consonants.Contains(predicted[i]) || s_consonants.Contains(reference[i])))
                return true;
        }
        return false;
    }
}

internal sealed class EvalThresholdConfig
{
    public static readonly EvalThresholdConfig Empty = new EvalThresholdConfig(new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal));

    private readonly Dictionary<string, Dictionary<string, double>> _thresholds;

    private EvalThresholdConfig(Dictionary<string, Dictionary<string, double>> thresholds)
    {
        _thresholds = thresholds;
    }

    public static EvalThresholdConfig Load(string path)
    {
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var thresholds = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);
        if (!document.RootElement.TryGetProperty("datasets", out var datasetsElement))
            return Empty;

        foreach (var datasetProperty in datasetsElement.EnumerateObject())
        {
            var perProfile = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var profileProperty in datasetProperty.Value.EnumerateObject())
                perProfile[profileProperty.Name] = profileProperty.Value.GetDouble();
            thresholds[datasetProperty.Name] = perProfile;
        }

        return new EvalThresholdConfig(thresholds);
    }

    public bool TryGetThreshold(string fileName, string profile, out double threshold)
    {
        threshold = 0d;
        return _thresholds.TryGetValue(fileName, out var perProfile)
            && perProfile.TryGetValue(profile, out threshold);
    }
}

internal sealed record EvalResult(
    int Cases,
    int EvaluatedCases,
    int ExactMatches,
    double WordErrorRate,
    double PhonemeErrorRate,
    int TotalErrors,
    int TotalReferencePhonemes,
    IReadOnlyList<MismatchRow> Mismatches,
    IReadOnlyList<CategorySummary> CategorySummaries);

internal sealed record EvalSummary(
    string Dataset,
    string FileName,
    string Profile,
    string Source,
    string Dialect,
    int Cases,
    int EvaluatedCases,
    int ExactMatches,
    double WordErrorRate,
    double PhonemeErrorRate,
    int TotalErrors,
    int TotalReferencePhonemes,
    double? Threshold,
    bool Passed);

internal sealed record MismatchRow(string Word, string Category, int Distance, string Predicted, string Reference);

internal sealed record CategorySummary(string Category, int Count, double AverageDistance);

internal sealed record CategorySummaryRow(string Dataset, string Profile, string Category, int Count, double AverageDistance);
