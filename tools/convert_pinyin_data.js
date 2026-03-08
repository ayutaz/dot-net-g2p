// convert_pinyin_data.js
// pinyin-data / phrase-pinyin-data を DotNetG2P.Chinese 用に変換する
//
// 使い方:
//   node convert_pinyin_data.js char [input] [output]   - 単字辞書変換（既存）
//   node convert_pinyin_data.js phrase [input] [output]  - フレーズ辞書変換（新規）
//   node convert_pinyin_data.js [input] [output]         - 後方互換（charモード）
//
// charモード:
//   入力形式 (pinyin.txt):   U+4E00: yī  # 一
//   出力形式 (pinyin_char.txt): 4E00 yī
//
// phraseモード:
//   入力形式 (large_pinyin.txt): 上海: shàng hǎi
//   出力形式 (pinyin_phrase.txt): 上海\tshàng hǎi

const fs = require('fs');
const path = require('path');

// コマンドライン引数の解析
let mode, inputPath, outputPath;

const firstArg = process.argv[2];
if (firstArg === 'char' || firstArg === 'phrase') {
    mode = firstArg;
    inputPath = process.argv[3];
    outputPath = process.argv[4];
} else {
    // 後方互換: 引数なし or 直接パス指定の場合はcharモード
    mode = 'char';
    inputPath = process.argv[2];
    outputPath = process.argv[3];
}

if (mode === 'char') {
    convertChar(inputPath, outputPath);
} else {
    convertPhrase(inputPath, outputPath);
}

// 単字辞書変換（既存ロジック）
function convertChar(inPath, outPath) {
    inPath = inPath || path.join(__dirname, 'pinyin_raw.txt');
    outPath = outPath || path.join(__dirname, '..', 'src', 'DotNetG2P.Chinese', 'Dictionary', 'Data', 'pinyin_char.txt');

    const input = fs.readFileSync(inPath, 'utf-8');
    const lines = input.split('\n');
    const outputLines = [];

    for (const line of lines) {
        const trimmed = line.trim();

        // 空行・コメント行をスキップ
        if (trimmed === '' || trimmed.startsWith('#')) {
            continue;
        }

        // "U+XXXX: pinyin  # comment" の形式をパース
        const commentIdx = trimmed.indexOf('#');
        const withoutComment = commentIdx >= 0 ? trimmed.substring(0, commentIdx) : trimmed;
        const cleaned = withoutComment.trim();

        // "U+XXXX: pinyin" を分割
        const colonIdx = cleaned.indexOf(':');
        if (colonIdx < 0) {
            continue;
        }

        const codepoint = cleaned.substring(0, colonIdx).trim();
        const pinyin = cleaned.substring(colonIdx + 1).trim();

        // U+ プレフィックスを除去
        const hex = codepoint.startsWith('U+') ? codepoint.substring(2) : codepoint;

        if (hex === '' || pinyin === '') {
            continue;
        }

        outputLines.push(`${hex} ${pinyin}`);
    }

    ensureDir(outPath);
    fs.writeFileSync(outPath, outputLines.join('\n') + '\n', 'utf-8');

    console.log(`[char] Converted ${outputLines.length} entries`);
    console.log(`Output: ${outPath}`);
}

// フレーズ辞書変換（新規）
function convertPhrase(inPath, outPath) {
    inPath = inPath || path.join(__dirname, 'large_pinyin_raw.txt');
    outPath = outPath || path.join(__dirname, '..', 'src', 'DotNetG2P.Chinese', 'Dictionary', 'Data', 'pinyin_phrase.txt');

    const input = fs.readFileSync(inPath, 'utf-8');
    const lines = input.split('\n');
    const outputLines = [];

    for (const line of lines) {
        const trimmed = line.trim();

        // 空行・コメント行をスキップ
        if (trimmed === '' || trimmed.startsWith('#')) {
            continue;
        }

        // "フレーズ: ピンイン列" の形式をパース
        const colonIdx = trimmed.indexOf(':');
        if (colonIdx < 0) {
            continue;
        }

        const phrase = trimmed.substring(0, colonIdx).trim();
        const pinyin = trimmed.substring(colonIdx + 1).trim();

        if (phrase === '' || pinyin === '') {
            continue;
        }

        outputLines.push(`${phrase}\t${pinyin}`);
    }

    ensureDir(outPath);
    fs.writeFileSync(outPath, outputLines.join('\n') + '\n', 'utf-8');

    console.log(`[phrase] Converted ${outputLines.length} entries`);
    console.log(`Output: ${outPath}`);
}

function ensureDir(filePath) {
    const dir = path.dirname(filePath);
    if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
    }
}
