// convert_pinyin_data.js
// pinyin-data の pinyin.txt を DotNetG2P.Chinese 用の pinyin_char.txt に変換する
//
// 入力形式 (pinyin.txt):
//   U+4E00: yī  # 一
//   U+4E2D: zhōng,zhòng  # 中
//
// 出力形式 (pinyin_char.txt):
//   4E00 yī
//   4E2D zhōng,zhòng

const fs = require('fs');
const path = require('path');

const inputPath = process.argv[2] || path.join(__dirname, 'pinyin_raw.txt');
const outputPath = process.argv[3] || path.join(__dirname, '..', 'src', 'DotNetG2P.Chinese', 'Dictionary', 'Data', 'pinyin_char.txt');

const input = fs.readFileSync(inputPath, 'utf-8');
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

// 出力ディレクトリの確認
const outputDir = path.dirname(outputPath);
if (!fs.existsSync(outputDir)) {
    fs.mkdirSync(outputDir, { recursive: true });
}

fs.writeFileSync(outputPath, outputLines.join('\n') + '\n', 'utf-8');

console.log(`Converted ${outputLines.length} entries`);
console.log(`Output: ${outputPath}`);
