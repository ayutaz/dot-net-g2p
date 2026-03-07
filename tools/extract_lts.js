#!/usr/bin/env node
/**
 * Flite LTS CARTツリーデータ抽出スクリプト
 * cmu_lts_model.h + cmu_lts_model.c + cmu_lts_rules.c からバイナリデータとC#ソースを生成する
 */

const fs = require('fs');
const path = require('path');

// Windows: Git Bash /tmp maps to %LOCALAPPDATA%\Temp
const FLITE_DIR = process.env.LOCALAPPDATA
    ? require('path').join(process.env.LOCALAPPDATA, 'Temp', 'flite_src', 'lang', 'cmulex')
    : '/tmp/flite_src/lang/cmulex';
const OUTPUT_DIR = path.join(__dirname, '..', 'src', 'DotNetG2P.English', 'LTS');

// === Step 1: Parse cmu_lts_model.h to build macro lookup table ===
function parseModelHeader(filePath) {
    const content = fs.readFileSync(filePath, 'utf-8');
    const macros = {};
    // Format: #define LTS_STATE_a_0 0x00,0x00
    const re = /^#define\s+(LTS_STATE_\w+)\s+(0x[0-9a-f]{2}),(0x[0-9a-f]{2})/gm;
    let m;
    while ((m = re.exec(content)) !== null) {
        macros[m[1]] = [parseInt(m[2], 16), parseInt(m[3], 16)];
    }
    console.log(`Parsed ${Object.keys(macros).length} macro definitions from model header`);
    return macros;
}

// === Step 2: Parse cmu_lts_model.c to extract tree nodes ===
function parseModelData(filePath, macros) {
    const content = fs.readFileSync(filePath, 'utf-8');

    // Find the array body between { and };
    const startIdx = content.indexOf('{');
    const endIdx = content.lastIndexOf('};');
    const body = content.substring(startIdx + 1, endIdx);

    const nodes = [];
    const lines = body.split('\n');

    for (const line of lines) {
        const trimmed = line.trim();
        if (!trimmed || trimmed.startsWith('/**') || trimmed.startsWith('/*') || trimmed.startsWith('*')) {
            continue;
        }

        // Terminal line: 0, 0, 0,0, 0,0
        if (/^\s*0,\s*0,\s*0,0,?\s*0,0\s*$/.test(trimmed)) {
            // Terminator node
            nodes.push([0, 0, 0, 0, 0, 0]);
            continue;
        }

        // Parse line format: feat, 'val', LTS_STATE_x_n , LTS_STATE_x_m ,
        // Or leaf: 255, phoneIdx, 0,0 , 0,0 ,

        // First, try leaf node: 255, N, 0,0 , 0,0 ,
        const leafMatch = trimmed.match(/^\s*255,\s*(\d+),\s*0,0\s*,\s*0,0\s*,?\s*$/);
        if (leafMatch) {
            const phoneIdx = parseInt(leafMatch[1]);
            nodes.push([255, phoneIdx, 0, 0, 0, 0]);
            continue;
        }

        // Branch node: feat, 'val', LTS_STATE_x_n , LTS_STATE_x_m ,
        // val can be a char like 'r', '#', '0' or a number
        const branchMatch = trimmed.match(/^\s*(\d+),\s*'([^'])',\s*(LTS_STATE_\w+)\s*,\s*(LTS_STATE_\w+)\s*,?\s*$/);
        if (branchMatch) {
            const feat = parseInt(branchMatch[1]);
            const val = branchMatch[2].charCodeAt(0);
            const qtrueKey = branchMatch[3];
            const qfalseKey = branchMatch[4];

            if (!macros[qtrueKey]) {
                console.error(`Unknown macro: ${qtrueKey}`);
                continue;
            }
            if (!macros[qfalseKey]) {
                console.error(`Unknown macro: ${qfalseKey}`);
                continue;
            }

            const qtrue = macros[qtrueKey];
            const qfalse = macros[qfalseKey];

            nodes.push([feat, val, qtrue[0], qtrue[1], qfalse[0], qfalse[1]]);
            continue;
        }

        // Some lines might have numeric val instead of char
        const numBranchMatch = trimmed.match(/^\s*(\d+),\s*(\d+),\s*(LTS_STATE_\w+)\s*,\s*(LTS_STATE_\w+)\s*,?\s*$/);
        if (numBranchMatch) {
            const feat = parseInt(numBranchMatch[1]);
            const val = parseInt(numBranchMatch[2]);
            const qtrueKey = numBranchMatch[3];
            const qfalseKey = numBranchMatch[4];

            const qtrue = macros[qtrueKey] || [0, 0];
            const qfalse = macros[qfalseKey] || [0, 0];

            nodes.push([feat, val, qtrue[0], qtrue[1], qfalse[0], qfalse[1]]);
            continue;
        }

        // Leaf with trailing comma variants
        const leafMatch2 = trimmed.match(/^\s*255,\s*(\d+),\s*0,\s*0\s*,\s*0,\s*0\s*,?\s*$/);
        if (leafMatch2) {
            const phoneIdx = parseInt(leafMatch2[1]);
            nodes.push([255, phoneIdx, 0, 0, 0, 0]);
            continue;
        }

        if (trimmed.length > 2 && !trimmed.startsWith('#include')) {
            console.warn(`Unmatched line: ${trimmed}`);
        }
    }

    console.log(`Parsed ${nodes.length} tree nodes`);
    return nodes;
}

// === Step 3: Parse cmu_lts_rules.c for phone table, letter table, letter index ===
function parseRules(filePath) {
    const content = fs.readFileSync(filePath, 'utf-8');

    // Phone table
    const phoneMatch = content.match(/cmu_lts_phone_table\[\d+\]\s*=\s*\{([^}]+)\}/s);
    const phones = [];
    if (phoneMatch) {
        const entries = phoneMatch[1].match(/"([^"]+)"/g);
        if (entries) {
            for (const e of entries) {
                phones.push(e.replace(/"/g, ''));
            }
        }
    }
    // Add epsilon at index 0 if not already there
    console.log(`Phone table: ${phones.length} entries`);
    console.log(`Phones: ${phones.join(', ')}`);

    // Letter index
    const indexMatch = content.match(/cmu_lts_letter_index\[\d+\]\s*=\s*\{([^}]+)\}/s);
    const letterIndex = [];
    if (indexMatch) {
        const nums = indexMatch[1].match(/\d+/g);
        if (nums) {
            for (const n of nums) {
                letterIndex.push(parseInt(n));
            }
        }
    }
    console.log(`Letter index: ${letterIndex.length} entries (last=${letterIndex[letterIndex.length-1]})`);

    return { phones, letterIndex };
}

// === Step 4: Map Flite phones to ARPAbet ===
function buildPhoneMapping(phones) {
    // Flite phone -> our ArpabetPhoneme enum name + stress
    // Format is like "eh1" -> (EH, Stress=1), "ax0" -> (AH, Stress=0)
    // "epsilon" -> null output
    // dual phones like "w-ey1" -> [W, EY1]

    const mapping = [];
    for (let i = 0; i < phones.length; i++) {
        const phone = phones[i];
        if (phone === 'epsilon') {
            mapping.push({ type: 'epsilon', index: i });
            continue;
        }

        // Check for dual phone (contains '-')
        if (phone.includes('-')) {
            const parts = phone.split('-');
            mapping.push({ type: 'dual', parts: parts, raw: phone, index: i });
        } else {
            mapping.push({ type: 'single', raw: phone, index: i });
        }
    }
    return mapping;
}

// === Step 5: Generate binary data ===
function generateBinaryData(nodes) {
    // Each node is 6 bytes: feat(1) val(1) qtrue_lo(1) qtrue_hi(1) qfalse_lo(1) qfalse_hi(1)
    const buf = Buffer.alloc(nodes.length * 6);
    for (let i = 0; i < nodes.length; i++) {
        const offset = i * 6;
        buf[offset + 0] = nodes[i][0]; // feat
        buf[offset + 1] = nodes[i][1]; // val
        buf[offset + 2] = nodes[i][2]; // qtrue lo
        buf[offset + 3] = nodes[i][3]; // qtrue hi
        buf[offset + 4] = nodes[i][4]; // qfalse lo
        buf[offset + 5] = nodes[i][5]; // qfalse hi
    }
    return buf;
}

// === Step 6: Generate C# source files ===
function generateCSharpLtsData(phones, letterIndex, binarySize) {
    const lines = [];
    lines.push('// 自動生成ファイル - 手動で編集しないでください');
    lines.push('// Flite (https://github.com/festvox/flite) の cmu_lts_model/cmu_lts_rules から抽出');
    lines.push('// Flite License: MIT相当 (Carnegie Mellon University)');
    lines.push('');
    lines.push('using System;');
    lines.push('using System.IO;');
    lines.push('using System.Reflection;');
    lines.push('');
    lines.push('namespace DotNetG2P.English.LTS');
    lines.push('{');
    lines.push('    /// <summary>');
    lines.push('    /// Flite LTS CARTツリーデータ。CMU英語辞書のLetter-to-Sound規則。');
    lines.push('    /// </summary>');
    lines.push('    internal static class LtsData');
    lines.push('    {');
    lines.push('        /// <summary>コンテキスト窓サイズ（前後の文字数）。</summary>');
    lines.push('        internal const int ContextWindowSize = 4;');
    lines.push('');
    lines.push('        /// <summary>追加特徴数。</summary>');
    lines.push('        internal const int ContextExtraFeats = 1;');
    lines.push('');
    lines.push('        /// <summary>ツリー終端マーカー。</summary>');
    lines.push('        internal const byte EndOfRule = 255;');
    lines.push('');
    lines.push('        /// <summary>ノードサイズ（バイト）。</summary>');
    lines.push('        internal const int NodeSize = 6;');
    lines.push('');

    // Phone table
    lines.push('        /// <summary>');
    lines.push('        /// 音素テーブル。インデックス→音素文字列のマッピング。');
    lines.push('        /// ツリーのリーフノードのval値がこのテーブルのインデックスに対応する。');
    lines.push('        /// </summary>');
    lines.push('        internal static readonly string[] PhoneTable = new string[]');
    lines.push('        {');
    for (let i = 0; i < phones.length; i++) {
        const comma = i < phones.length - 1 ? ',' : '';
        lines.push(`            "${phones[i]}"${comma} // ${i}`);
    }
    lines.push('        };');
    lines.push('');

    // Letter index
    lines.push('        /// <summary>');
    lines.push('        /// 各文字(a-z)のツリー開始ノードインデックス。');
    lines.push('        /// index 0=a, 1=b, ..., 25=z。');
    lines.push('        /// </summary>');
    lines.push('        internal static readonly ushort[] LetterIndex = new ushort[]');
    lines.push('        {');
    const letters = 'abcdefghijklmnopqrstuvwxyz';
    for (let i = 0; i < 26; i++) {
        const val = i < letterIndex.length ? letterIndex[i] : 0;
        const comma = i < 25 ? ',' : '';
        lines.push(`            ${val}${comma} // ${letters[i]}`);
    }
    lines.push('        };');
    lines.push('');

    // Model data - loaded from embedded resource
    lines.push('        /// <summary>');
    lines.push('        /// CARTツリーモデルバイナリデータを埋め込みリソースから読み込む。');
    lines.push('        /// 各ノードは6バイト: feat(1), val(1), qtrue(2, LE), qfalse(2, LE)。');
    lines.push('        /// </summary>');
    lines.push('        internal static byte[] LoadModelData()');
    lines.push('        {');
    lines.push('            var assembly = typeof(LtsData).Assembly;');
    lines.push('            using (var stream = assembly.GetManifestResourceStream("DotNetG2P.English.LTS.cmu_lts_model.bin"))');
    lines.push('            {');
    lines.push('                if (stream == null)');
    lines.push('                    throw new InvalidOperationException("埋め込みリソース cmu_lts_model.bin が見つかりません。");');
    lines.push('                var data = new byte[stream.Length];');
    lines.push('                stream.Read(data, 0, data.Length);');
    lines.push('                return data;');
    lines.push('            }');
    lines.push('        }');
    lines.push('    }');
    lines.push('}');

    return lines.join('\n') + '\n';
}

function generatePhoneMappingCs(phones) {
    const lines = [];
    lines.push('// 自動生成ファイル - 手動で編集しないでください');
    lines.push('// Flite LTS音素テーブルとARPAbet enumのマッピング');
    lines.push('');
    lines.push('using System;');
    lines.push('using System.Collections.Generic;');
    lines.push('');
    lines.push('namespace DotNetG2P.English.LTS');
    lines.push('{');
    lines.push('    /// <summary>');
    lines.push('    /// Flite LTS出力音素をARPAbet EnglishPhonemeに変換するマッピング。');
    lines.push('    /// </summary>');
    lines.push('    internal static class LtsPhoneMapping');
    lines.push('    {');
    lines.push('        /// <summary>');
    lines.push('        /// LTS音素テーブルインデックスからEnglishPhoneme配列へのマッピング。');
    lines.push('        /// epsilon（インデックス0）はnull、二重音素は2要素配列。');
    lines.push('        /// </summary>');
    lines.push('        internal static readonly EnglishPhoneme[]?[] PhoneToArpabet = BuildMapping();');
    lines.push('');
    lines.push('        private static EnglishPhoneme[]?[] BuildMapping()');
    lines.push('        {');
    lines.push(`            var map = new EnglishPhoneme[]?[${phones.length}];`);
    lines.push('');

    // Build the mapping entries
    for (let i = 0; i < phones.length; i++) {
        const phone = phones[i];
        if (phone === 'epsilon') {
            lines.push(`            map[${i}] = null; // epsilon`);
            continue;
        }

        const phonemes = parseFlitePhone(phone);
        if (phonemes.length === 1) {
            lines.push(`            map[${i}] = new[] { ${phonemes[0]} }; // ${phone}`);
        } else {
            lines.push(`            map[${i}] = new[] { ${phonemes.join(', ')} }; // ${phone}`);
        }
    }

    lines.push('');
    lines.push('            return map;');
    lines.push('        }');
    lines.push('    }');
    lines.push('}');

    return lines.join('\n') + '\n';
}

function parseFlitePhone(phone) {
    // Split dual phones
    const parts = phone.includes('-') ? phone.split('-') : [phone];
    return parts.map(p => flitePhoneToEnglishPhoneme(p));
}

function flitePhoneToEnglishPhoneme(p) {
    // Extract base phoneme and stress
    // Format: "eh1", "aa0", "ax0", "b", "ch", etc.
    const stressMatch = p.match(/^([a-z]+)([012])$/);

    if (stressMatch) {
        const base = stressMatch[1];
        const stress = parseInt(stressMatch[2]);
        const arpabet = fliteBaseToArpabet(base);
        // Stress enum: 0 → NoStress (母音の無ストレス), 1 → Primary, 2 → Secondary
        const stressEnum = stress === 0 ? 'Stress.NoStress' : stress === 1 ? 'Stress.Primary' : 'Stress.Secondary';
        return `new EnglishPhoneme(ArpabetPhoneme.${arpabet}, ${stressEnum})`;
    } else {
        // Consonant (no stress)
        const arpabet = fliteBaseToArpabet(p);
        return `new EnglishPhoneme(ArpabetPhoneme.${arpabet}, Stress.None)`;
    }
}

function fliteBaseToArpabet(base) {
    const map = {
        'aa': 'AA', 'ae': 'AE', 'ah': 'AH', 'ao': 'AO',
        'aw': 'AW', 'ax': 'AH', // ax maps to AH (schwa)
        'ay': 'AY', 'b': 'B',
        'ch': 'CH', 'd': 'D', 'dh': 'DH',
        'eh': 'EH', 'er': 'ER', 'ey': 'EY',
        'f': 'F', 'g': 'G', 'hh': 'HH',
        'ih': 'IH', 'iy': 'IY',
        'jh': 'JH', 'k': 'K', 'l': 'L',
        'm': 'M', 'n': 'N', 'ng': 'NG',
        'ow': 'OW', 'oy': 'OY',
        'p': 'P', 'r': 'R', 's': 'S', 'sh': 'SH',
        't': 'T', 'th': 'TH',
        'uh': 'UH', 'uw': 'UW',
        'v': 'V', 'w': 'W', 'y': 'Y',
        'z': 'Z', 'zh': 'ZH',
    };

    if (!map[base]) {
        console.error(`Unknown Flite phone base: "${base}"`);
        return base.toUpperCase();
    }
    return map[base];
}

// === Main ===
function main() {
    console.log('=== Flite LTS Data Extraction ===');

    // Parse
    const macros = parseModelHeader(path.join(FLITE_DIR, 'cmu_lts_model.h'));
    const nodes = parseModelData(path.join(FLITE_DIR, 'cmu_lts_model.c'), macros);
    const { phones, letterIndex } = parseRules(path.join(FLITE_DIR, 'cmu_lts_rules.c'));

    // Validate
    console.log('\n=== Validation ===');
    const lastLetterEnd = letterIndex[26]; // sentinel
    console.log(`Total nodes: ${nodes.length}`);
    console.log(`Letter index range: ${letterIndex[0]} - ${letterIndex[25]} (z), sentinel: ${lastLetterEnd}`);

    // Check phone mapping
    const phoneMapping = buildPhoneMapping(phones);
    console.log(`\nPhone mapping (${phoneMapping.length} entries):`);
    for (const pm of phoneMapping) {
        if (pm.type === 'dual') {
            console.log(`  [${pm.index}] ${pm.raw} -> ${pm.parts.join(' + ')}`);
        }
    }

    // Verify all phones map to known ARPAbet
    console.log('\n=== ARPAbet Mapping Verification ===');
    let allMapped = true;
    for (const pm of phoneMapping) {
        if (pm.type === 'epsilon') continue;
        try {
            parseFlitePhone(pm.raw || pm.parts?.join('-'));
        } catch (e) {
            console.error(`Failed to map: ${pm.raw} - ${e.message}`);
            allMapped = false;
        }
    }
    console.log(allMapped ? 'All phones mapped successfully!' : 'Some phones failed to map!');

    // Generate output
    console.log('\n=== Generating Output ===');

    // Create output directory
    fs.mkdirSync(OUTPUT_DIR, { recursive: true });

    // Binary model data
    const binaryData = generateBinaryData(nodes);
    const binPath = path.join(OUTPUT_DIR, 'cmu_lts_model.bin');
    fs.writeFileSync(binPath, binaryData);
    console.log(`Binary model: ${binPath} (${binaryData.length} bytes, ${nodes.length} nodes)`);

    // C# LtsData.cs
    const ltsDataCs = generateCSharpLtsData(phones, letterIndex, binaryData.length);
    const ltsDataPath = path.join(OUTPUT_DIR, 'LtsData.cs');
    fs.writeFileSync(ltsDataPath, ltsDataCs);
    console.log(`C# data: ${ltsDataPath}`);

    // C# LtsPhoneMapping.cs
    const mappingCs = generatePhoneMappingCs(phones);
    const mappingPath = path.join(OUTPUT_DIR, 'LtsPhoneMapping.cs');
    fs.writeFileSync(mappingPath, mappingCs);
    console.log(`C# mapping: ${mappingPath}`);

    console.log('\n=== Done ===');
}

main();
