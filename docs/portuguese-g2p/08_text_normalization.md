# ポルトガル語テキスト正規化要件

## 概要

ポルトガル語G2Pパイプラインにおけるテキスト正規化は、数字・通貨・日付・時刻・略語・単位・記号等の非アルファベット表現をポルトガル語の読み上げ形式に展開する処理である。既存のスペイン語・フランス語実装（`SpanishNormalizer.cs`, `FrenchNormalizer.cs`）と同様の構成で実装する。

ポルトガル語固有の注意点として、ブラジルポルトガル語（pt-BR）とヨーロッパポルトガル語（pt-PT）で数詞の綴りや通貨名が異なる部分がある。G2Pの主要ターゲットはブラジルポルトガル語（pt-BR）とし、方言オプションでヨーロッパポルトガル語（pt-PT）にも対応する設計とする。

## 正規化パイプライン（処理順序）

スペイン語・フランス語実装に準拠した段階的展開:

1. NFKC正規化 + 小文字化（互換分解を含む。全角→半角変換、リガチャ分解等。SpanishNormalizerと同じ `NormalizationForm.FormKC` を採用）
2. 略語展開
3. ISO日付展開（YYYY-MM-DD）（スペイン語実装と同様にサポート。フランス語実装では未サポート）
4. 日付展開（DD/MM/YYYY）
5. 時刻展開
6. 通貨展開（パーセントより先に処理し、通貨記号内のピリオドとの干渉を回避）
7. パーセント展開
8. 単位展開
9. 数値範囲展開（"10-20" → "dez a vinte"。スペイン語の `ExpandNumericRanges` と同パターン。接続詞は「a」を使用）
10. 小数展開
11. 独立数値展開
12. 記号展開
13. 空白正規化 + trim

---

## 1. 数字→ポルトガル語読み（NumberToWords）

### 1.1 基数詞（Cardinal Numbers）

#### 基本数詞（0-19）

| 数値 | pt-BR         | pt-PT         |
|------|---------------|---------------|
| 0    | zero          | zero          |
| 1    | um / uma      | um / uma      |
| 2    | dois / duas   | dois / duas   |
| 3    | três          | três          |
| 4    | quatro        | quatro        |
| 5    | cinco         | cinco         |
| 6    | seis          | seis          |
| 7    | sete          | sete          |
| 8    | oito          | oito          |
| 9    | nove          | nove          |
| 10   | dez           | dez           |
| 11   | onze          | onze          |
| 12   | doze          | doze          |
| 13   | treze         | treze         |
| 14   | **quatorze**  | **catorze**   |
| 15   | quinze        | quinze        |
| 16   | **dezesseis** | **dezasseis** |
| 17   | **dezessete** | **dezassete** |
| 18   | **dezoito**   | **dezoito**   |
| 19   | **dezenove**  | **dezanove**  |

**方言差**:
- 16,17,19 でブラジルは「ess」、ヨーロッパは「ass」を使う
- 14はブラジルでは「quatorze」がより一般的、ポルトガルでは「catorze」がより一般的

#### 十の位（20-99）

| 数値 | ポルトガル語   |
|------|---------------|
| 20   | vinte         |
| 30   | trinta        |
| 40   | quarenta      |
| 50   | cinquenta     |
| 60   | sessenta      |
| 70   | setenta       |
| 80   | oitenta       |
| 90   | noventa       |

- 十の位 + 一の位は **「e」** で接続: 21 = "vinte e um", 43 = "quarenta e três"
- フランス語の20進法（70=soixante-dix等）とは異なり、ポルトガル語は完全な10進法

#### 百の位（100-999）

| 数値 | 男性形       | 女性形        |
|------|-------------|---------------|
| 100  | cem / cento | cem / cento   |
| 200  | duzentos    | duzentas      |
| 300  | trezentos   | trezentas     |
| 400  | quatrocentos | quatrocentas |
| 500  | quinhentos  | quinhentas    |
| 600  | seiscentos  | seiscentas    |
| 700  | setecentos  | setecentas    |
| 800  | oitocentos  | oitocentas    |
| 900  | novecentos  | novecentas    |

**重要規則**:
- **cem vs cento**: ぴったり100の場合は「cem」、101以上（100+何か）の場合は「cento」
  - 100 = "cem"
  - 101 = "cento e um"
  - 150 = "cento e cinquenta"
- 200-900は性数一致（男性名詞には -centos、女性名詞には -centas）
- 百の位と下位は **「e」** で接続: 234 = "duzentos e trinta e quatro"

#### 千以上

| 数値          | ポルトガル語                |
|---------------|-----------------------------|
| 1,000         | mil                         |
| 2,000         | dois mil                    |
| 1,000,000     | um milhão                   |
| 2,000,000     | dois milhões                |
| 1,000,000,000 | um bilhão (BR) / mil milhões (PT) |

**重要規則**:
- 「mil」は不変（単複同形）: 2,000 = "dois mil"、複数形にならない
- 「milhão」は複数形で「milhões」: 5,000,000 = "cinco milhões"
- 「bilhão」(BR) vs 「bilião」(PT): ブラジルは短尺法（10^9 = bilhão）、ポルトガルは長尺法（10^9 = mil milhões、10^12 = bilião）。ポルトガルの「bilião」はブラジルの「trilhão」に相当する
- milhão/bilhão の後に名詞が続く場合は「de」を挿入: "um milhão de pessoas"

#### 「e」接続詞の使用規則

ポルトガル語の数詞で最も複雑な規則:

1. **百の位と十の位/一の位の間**: 常に「e」を使用
   - 121 = "cento e vinte e um"
2. **千の位と下位の間**:
   - 下位が1〜99の場合（百の位が0）: 「e」を使用
     - 1,033 = "mil e trinta e três"
     - 2,005 = "dois mil e cinco"
   - 下位が端数百（100, 200, ... 900）の場合: 「e」を使用
     - 1,100 = "mil e cem"
     - 2,500 = "dois mil e quinhentos"
   - 下位が101以上で端数百でない場合（百の位と十/一の位の両方がある）: 「e」を使用**しない**
     - 2,122 = "dois mil cento e vinte e dois"
     - 1,350 = "mil trezentos e cinquenta"

**簡略規則**: 千の位の後で「e」を入れるのは、下位の数値が1〜99（百の位が0）または端数百（100, 200, ...900）の場合。下位が101〜199, 201〜299, ... のように百の位と端数の両方がある場合は「e」を入れない。

### 1.2 序数詞（Ordinal Numbers）

日付の「1日」で使用。G2P正規化では限定的に使用。

| 数値 | 男性形         | 女性形          |
|------|---------------|-----------------|
| 1st  | primeiro      | primeira        |
| 2nd  | segundo       | segunda         |
| 3rd  | terceiro      | terceira        |
| 4th  | quarto        | quarta          |
| 5th  | quinto        | quinta          |
| 6th  | sexto         | sexta           |
| 7th  | sétimo        | sétima          |
| 8th  | oitavo        | oitava          |
| 9th  | nono          | nona            |
| 10th | décimo        | décima          |
| 20th | vigésimo      | vigésima        |
| 30th | trigésimo     | trigésima       |
| 40th | quadragésimo  | quadragésima    |
| 50th | quinquagésimo | quinquagésima   |
| 60th | sexagésimo    | sexagésima      |
| 70th | septuagésimo  | septuagésima    |
| 80th | octogésimo    | octogésima      |
| 90th | nonagésimo    | nonagésima      |
| 100th| centésimo     | centésima       |

- 複合序数詞: 42nd = "quadragésimo segundo"
- 性数一致が必要（4形態: m.sg, f.sg, m.pl, f.pl）
- 高次序数詞（50以上）は日常会話ではあまり使われず、基数詞で代用されることが多い

### 1.3 小数（Decimal Numbers）

- ポルトガル語では **カンマ（,）** が小数点: 3,14 = "três vírgula quatorze"
- **ピリオド（.）** は桁区切り: 1.000 = mil
- 小数部は数字を一つずつ読む方式と数値として読む方式の両方がある
  - 3,14 → "três vírgula um quatro"（桁読み）または "três vírgula quatorze"（数値読み）
  - TTS向けには数値読み方式を採用（スペイン語実装と同様）

### 1.4 分数

- 基本形: 分子（基数詞）+ 分母（序数詞の変化形）
  - 1/2 = "um meio" / "metade"
  - 1/3 = "um terço"
  - 1/4 = "um quarto"
  - 2/3 = "dois terços"
  - 3/4 = "três quartos"
- 分母が5以上の場合は序数詞ベース: 1/5 = "um quinto"
- G2P正規化での分数展開は優先度低（スペイン語・フランス語でも未実装）

### 1.5 性数一致（Gender Agreement）

スペイン語と同様、一部の数詞で性数一致が必要:

- **um/uma**: 男性名詞 "um quilômetro" / 女性名詞 "uma hora"
- **dois/duas**: 男性名詞 "dois metros" / 女性名詞 "duas horas"
- **200-900**: 男性形 -centos / 女性形 -centas
  - "duzentos metros" / "duzentas horas"

**実装方針**: `PortugueseNumberGender` enum (Masculine, Feminine) で制御。スペイン語の `SpanishNumberGender` と同パターン。単位定義に性別情報を持たせ、`ConvertAttributed()` で性別付き変換を行う。

---

## 2. 通貨記号の展開

### ブラジルレアル（R$）

| パターン      | 展開例                                        |
|--------------|-----------------------------------------------|
| R$ 1         | um real                                       |
| R$ 10        | dez reais                                     |
| R$ 1,50      | um real e cinquenta centavos                  |
| R$ 2.500,00  | dois mil e quinhentos reais                   |
| R$ 0,99      | noventa e nove centavos                       |

- 単数: real / 複数: reais（不規則複数形）
- 補助通貨: centavo / centavos
- 整数部と小数部は「e」で接続: "um real **e** cinquenta centavos"

### ユーロ（EUR）

| パターン | 展開例                                |
|---------|---------------------------------------|
| 1 EUR    | um euro                              |
| 50 EUR   | cinquenta euros                      |
| 1,50 EUR | um euro e cinquenta cêntimos         |

- 単数: euro / 複数: euros
- 補助通貨: cêntimo / cêntimos（ヨーロッパポルトガル語）
- 整数部と小数部は「e」で接続（レアルと同様）

### ドル（$）

| パターン | 展開例                                |
|---------|---------------------------------------|
| $ 1     | um dólar                              |
| $ 100   | cem dólares                           |
| $ 1,50  | um dólar e cinquenta centavos         |

- 単数: dólar / 複数: dólares
- 補助通貨: centavo / centavos
- 入力パターンはピリオド小数点（英語圏書式: "$ 1.50"）とカンマ小数点（ブラジル書式: "$ 1,50"）の両方を受け入れる。スペイン語実装の `TrySplitNumber` と同様にヒューリスティックで処理

### 実装メモ

- 通貨記号の前置・後置の両方を処理（R$は前置、EURは後置が一般的）
- 小数部は2桁に正規化（スペイン語実装の `NormalizeCurrencyMinorUnits` と同方式）
- ブラジルポルトガル語ではカンマが小数点、ピリオドが桁区切り

---

## 3. 日付・時刻フォーマット

### 3.1 日付

#### フォーマット

- DD/MM/YYYY（ブラジル・ポルトガル共通の標準形式）
- DD-MM-YYYY, DD.MM.YYYY（代替形式）
- YYYY-MM-DD（ISO形式）

#### 展開規則

- 1日 → "primeiro"（序数詞）、2日以降 → 基数詞
- 月名: janeiro, fevereiro, março, abril, maio, junho, julho, agosto, setembro, outubro, novembro, dezembro
- 接続詞: 「de ... de ...」

| 入力         | 出力                                          |
|-------------|-----------------------------------------------|
| 01/01/2024  | primeiro de janeiro de dois mil e vinte e quatro |
| 15/03/2024  | quinze de março de dois mil e vinte e quatro     |
| 25/12/1999  | vinte e cinco de dezembro de mil novecentos e noventa e nove |

#### バリデーション

- 月: 1-12
- 日: 月ごとの日数チェック（DateTime.DaysInMonth使用）
- 2桁年の4桁展開: < 50 → +2000, >= 50 → +1900

### 3.2 時刻

#### フォーマット

- NNhNN: "14h30" → "quatorze horas e trinta minutos"
- NN:NN: "14:30" → "quatorze horas e trinta minutos"

#### 展開規則

| 入力   | 出力                                    |
|-------|----------------------------------------|
| 1h00  | uma hora                               |
| 1h30  | uma hora e trinta minutos              |
| 12h00 | meio-dia                               |
| 0h00  | meia-noite                             |
| 14h30 | quatorze horas e trinta minutos        |
| 8:15  | oito horas e quinze minutos            |
| 13:00 | treze horas                            |

**重要規則**:
- 1時は「uma hora」（女性形、horaが女性名詞のため）
- 12:00 = "meio-dia"、0:00 = "meia-noite"（特殊形）
- 分が0の場合は分を省略: "treze horas"
- 分が1以上の場合は常に「minutos」を付ける（省略しない方針で統一）
- 24時間表記を使用（ブラジルでは口語で12時間制も使われるが、TTS正規化では24時間制で統一）

---

## 4. 略語・頭字語の展開

### 4.1 敬称略語

| 略語    | 展開            | 備考              |
|---------|----------------|-------------------|
| Sr.     | senhor         | 男性敬称           |
| Sra.    | senhora        | 女性敬称           |
| Srta.   | senhorita      | 未婚女性敬称       |
| D.      | dona / dom     | 女性/男性敬称      |
| Dr.     | doutor         | 男性博士/医師      |
| Dra.    | doutora        | 女性博士/医師      |
| Prof.   | professor      | 男性教授           |
| Profa.  | professora     | 女性教授           |
| Eng.    | engenheiro     | 男性エンジニア     |
| Enga.   | engenheira     | 女性エンジニア     |
| Adv.    | advogado       | 弁護士             |
| Exmo.   | excelentíssimo | 敬称               |
| Ilmo.   | ilustríssimo   | 敬称               |
| V. Ex.ª | Vossa Excelência | 敬称             |

### 4.2 住所・場所略語

| 略語  | 展開       |
|------|-----------|
| Av.  | avenida   |
| R.   | rua       |
| Pça. | praça     |
| Edif.| edifício  |
| Apto.| apartamento |
| And. | andar     |

### 4.3 一般略語

| 略語    | 展開              |
|---------|-------------------|
| etc.    | et cetera         |
| p. ex.  | por exemplo       |
| ex.     | exemplo           |
| n.º     | número (+ 数値前) |
| pág.    | página            |
| págs.   | páginas           |
| tel.    | telefone          |
| vol.    | volume            |
| cap.    | capítulo          |
| art.    | artigo            |
| dept.   | departamento      |
| aprox.  | aproximadamente   |
| ltda.   | limitada          |
| S.A.    | sociedade anônima |
| obs.    | observação        |
| ref.    | referência        |

### 4.4 頭字語の読み方

- **スペルアウト型**（各文字を読む）: ONU, ONG, CPF, CNPJ, PT, MG, SP, RJ
- **単語型**（一語として読む）: FIFA, NASA, FGTS
- 判定ルール: 母音を含み発音可能なら単語型、それ以外はスペルアウト型
- G2P正規化では頭字語をそのまま通過させる（G2Pルール側で処理）

---

## 5. 単位の展開

### 5.1 計量単位

| 記号   | 単数形                  | 複数形                   | 性別 |
|--------|------------------------|--------------------------|------|
| km     | quilômetro (BR) / quilómetro (PT) | quilômetros (BR) / quilómetros (PT) | M |
| m      | metro                  | metros                   | M    |
| cm     | centímetro             | centímetros              | M    |
| mm     | milímetro              | milímetros               | M    |
| kg     | quilograma             | quilogramas              | M    |
| g      | grama                  | gramas                   | M    |
| mg     | miligrama              | miligramas               | M    |
| l      | litro                  | litros                   | M    |
| ml     | mililitro              | mililitros               | M    |
| km/h   | quilômetro por hora (BR) | quilômetros por hora (BR) | M |
| m/s    | metro por segundo      | metros por segundo       | M    |
| m²     | metro quadrado         | metros quadrados         | M    |
| cm²    | centímetro quadrado    | centímetros quadrados    | M    |
| km²    | quilômetro quadrado (BR) | quilômetros quadrados (BR) | M |

### 5.2 時間単位

| 記号 | 単数形    | 複数形     | 性別 |
|------|----------|-----------|------|
| h    | hora     | horas     | F    |
| min  | minuto   | minutos   | M    |
| s    | segundo  | segundos  | M    |
| ms   | milissegundo | milissegundos | M |

### 5.3 温度・周波数・デジタル

| 記号 | 単数形             | 複数形              | 性別 |
|------|-------------------|---------------------|------|
| °C   | grau Celsius      | graus Celsius       | M    |
| °F   | grau Fahrenheit   | graus Fahrenheit    | M    |
| Hz   | hertz             | hertz               | M    |
| kHz  | quilohertz        | quilohertz          | M    |
| MHz  | megahertz         | megahertz           | M    |
| GHz  | gigahertz         | gigahertz           | M    |
| GB   | gigabyte          | gigabytes           | M    |
| MB   | megabyte          | megabytes           | M    |
| KB   | quilobyte         | quilobytes          | M    |

**注**: IT分野では英語由来の「kilobyte」がそのまま使われることが多いが、G2P変換の観点からは「quilobyte」で統一する（発音に大差なし）。

### 5.4 注意事項

- 「grama」の性別: 計量単位としての「grama」は**pt-BR/pt-PT とも男性名詞**（"um grama"）。女性名詞の「a grama」は「芝生/草」の意味であり、計量単位とは別語
- 数値1の場合は性数一致: "um quilômetro" / "uma hora"
- Hertz系は単複同形

---

## 6. 記号→名前変換

| 記号 | ポルトガル語         | 備考                                      |
|------|---------------------|-------------------------------------------|
| @    | arroba              | pt-BR/pt-PT共通                           |
| #    | número              | 「cerquilha」(PT) / 「jogo da velha」(BR口語) もあるが、「número」が無難 |
| &    | e                   | 接続詞                                     |
| %    | por cento           | パーセント展開で処理                       |
| +    | mais                |                                            |
| -    | menos               | 数値の前のみ。ハイフンは別処理             |
| =    | igual               |                                            |
| $    | 通貨として処理       | 通貨展開で処理                             |
| EUR  | 通貨として処理       | 通貨展開で処理                             |

---

## 7. ポルトガル語特有の正規化

### 7.1 前置詞との縮約（Contractions）

ポルトガル語では前置詞と冠詞が縮約する。これはG2P処理ではなくテキスト正規化の前段階で発生する現象であり、正規化処理では**縮約形をそのまま保持**する（分解しない）。

| 前置詞 | + 冠詞    | 縮約形   |
|--------|----------|---------|
| de     | + o      | do      |
| de     | + a      | da      |
| de     | + os     | dos     |
| de     | + as     | das     |
| em     | + o      | no      |
| em     | + a      | na      |
| em     | + os     | nos     |
| em     | + as     | nas     |
| a      | + o      | ao      |
| a      | + a      | à (U+00E0, グレーブアクセント付き) |
| a      | + os     | aos     |
| a      | + as     | às (U+00E0 + s, グレーブアクセント付き) |
| por    | + o      | pelo    |
| por    | + a      | pela    |
| por    | + os     | pelos   |
| por    | + as     | pelas   |

### 7.2 Crase（クラーゼ）の処理

- 「à」（craseアクセント）はグレーブアクセント付きの母音として扱う
- G2Pルール側で処理するため、正規化段階では保持
- NFKC正規化で Unicode 互換分解・再合成を適用するだけでOK

### 7.3 ポルトガル語文字の保持

正規化後の空白正規化（非文字除去）では以下を保持する:
- 基本ラテン文字 (a-z)
- アクセント付き文字: á, é, í, ó, ú, â, ê, ô, ã, õ, ü, ç
- ハイフン（複合語内: "meia-noite"、動詞の接語: "disse-me"）
- アポストロフ（まれだが外来語等で使用）

### 7.4 接語（Clitics）

ポルトガル語では代名詞接語がハイフンで動詞に結合する:
- "disse-me" (he told me)
- "faz-se" (it is done)
- "diga-lhe" (tell him/her)

正規化ではハイフンを含む単語をそのまま保持し、トークン化では1トークンとして扱う。

---

## 8. 実装設計

### 8.1 クラス構成

```
DotNetG2P.Portuguese/
  Normalization/
    PortugueseNormalizer.cs      # メイン正規化パイプライン (internal static)
    NumberToWords.cs             # 数値→ポルトガル語数詞変換 (internal static)
```

### 8.2 PortugueseNormalizer API

フランス語実装 (`FrenchNormalizer.cs`) のパターンに合わせ、`Tokenize()` 内部で `Normalize()` を呼ぶ方式を採用する。二重正規化を防止するため、`TokenizeNormalized()` internal メソッドも用意する。戻り値は `string[]`（フランス語実装と同じ）。

```csharp
internal static class PortugueseNormalizer
{
    // テキスト正規化（NFKC + 小文字化 + 各種展開 + 空白正規化）
    public static string Normalize(string text);

    // トークン化（内部で Normalize() を呼び、空白分割）
    public static string[] Tokenize(string text);

    // 正規化済みテキストのトークン化（二重正規化防止）
    // G2PEngine 側で Normalize() 済みのテキストを渡す場合に使用
    internal static string[] TokenizeNormalized(string normalized);
}
```

**注意**: `Tokenize()` は内部で `Normalize()` を呼ぶため、呼び出し側で事前に正規化を行わないこと。既に正規化済みのテキストをトークン化する場合は `TokenizeNormalized()` を使用する（フランス語G2Pレビューで学んだ教訓）。

### 8.3 NumberToWords API

```csharp
internal enum PortugueseNumberGender : byte
{
    Masculine = 0,
    Feminine = 1,
}

internal static class NumberToWords
{
    // 基本変換
    public static string Convert(long number);
    public static string Convert(string text);

    // 性数一致付き変換（単位展開用）
    public static string ConvertAttributed(long number, PortugueseNumberGender gender);

    // 桁読み（小数部用）
    public static string ConvertDigits(string digits);
}
```

### 8.4 方言対応

`PortugueseDialect` enum により以下を切り替え:

| 項目                | Brazilian (pt-BR) | European (pt-PT) |
|---------------------|-------------------|-------------------|
| 14                  | quatorze          | catorze           |
| 16                  | dezesseis         | dezasseis         |
| 17                  | dezessete         | dezassete         |
| 19                  | dezenove          | dezanove          |
| 10^9                | um bilhão         | mil milhões       |
| デフォルト通貨      | R$ (real/reais)   | EUR (euro/euros)  |
| 通貨補助単位        | centavo/centavos  | cêntimo/cêntimos  |
| quilômetro の綴り   | quilômetro (ô)    | quilómetro (ó)    |
| 小数点              | vírgula (,)       | vírgula (,)       |

**注**: デフォルト通貨は方言に応じた初期値であり、どちらの方言でもR$・EUR・ドル等すべての通貨記号を処理可能。

### 8.5 既存実装との差異

| 項目                    | スペイン語            | フランス語              | ポルトガル語            |
|------------------------|----------------------|------------------------|------------------------|
| 小数点記号              | coma (,)             | virgule (,)            | vírgula (,)            |
| 十の位の特殊規則        | なし                 | 70/80/90が20進法        | なし（完全10進法）     |
| 100の変形              | なし                 | なし                   | cem/cento              |
| 性数一致する数詞        | 1, 21               | なし（un固定）         | 1, 2, 200-900          |
| 「e」接続詞            | y                    | et（限定的）            | e（複雑な規則あり）    |
| 通貨の不規則複数形     | なし                 | なし                   | real→reais            |
| 日付の序数詞           | 1日=primero          | 1日=premier            | 1日=primeiro           |
| 時刻の特殊形           | "en punto"           | "minuit"/"midi"        | "meia-noite"/"meio-dia"|
| 200の複数形規則        | -                    | deux cents/deux cent un | -（性数一致のみ）     |

---

## 9. テストケース設計

### 9.1 NumberToWords テスト

- 基本数詞: 0-20、端数（21, 99, 100, 101, 999, 1000, 1001等）
- cem/cento 規則: 100 vs 101-199
- 「e」挿入規則: 1100 ("mil e cem") vs 1200 ("mil e duzentos") vs 2005 ("dois mil e cinco") vs 2122 ("dois mil cento e vinte e dois") vs 2500 ("dois mil e quinhentos")
- 性数一致: um/uma, dois/duas, duzentos/duzentas
- 大きな数値: milhão, milhões, bilhão (BR), mil milhões (PT)
- 方言差: dezesseis vs dezasseis, quatorze vs catorze

### 9.2 正規化パイプラインテスト

- 日付: "01/01/2024" → "primeiro de janeiro de dois mil e vinte e quatro"
- 時刻: "14h30" → "quatorze horas e trinta minutos"
- 通貨: "R$ 1,50" → "um real e cinquenta centavos"
- 単位: "100 km" → "cem quilômetros"
- パーセント: "50%" → "cinquenta por cento"
- 略語: "Sr." → "senhor"
- 記号: "@" → "arroba"
- 複合: "Dr. Silva tem R$ 1.000,50 na conta" → "doutor silva tem mil reais e cinquenta centavos na conta"

---

## 参考資料

- SpanishNormalizer.cs（スペイン語正規化実装）
- FrenchNormalizer.cs（フランス語正規化実装）
- French NumberToWords.cs（フランス語数詞変換）
- [FluentU - Portuguese Numbers Guide](https://www.fluentu.com/blog/portuguese/portuguese-numbers/)
- [Practice Portuguese - Cardinal Numbers](https://www.practiceportuguese.com/learning-notes/cardinal-numbers/)
- [Omniglot - Numbers in Portuguese](https://www.omniglot.com/language/numbers/portuguese.htm)
- [Practice Portuguese - Hours and Telling Time](https://www.practiceportuguese.com/learning-notes/hours-and-telling-time/)
- [Rio & Learn - Ordinal Numbers](https://rioandlearn.com/ordinal-numbers-portuguese/)
- [Portuguesepedia - Portuguese Prepositions](https://portuguesepedia.com/basic-portuguese-prepositions/)
- [FluentU - Portuguese Abbreviations](https://www.fluentu.com/blog/portuguese/portuguese-abbreviations/)
- [Wikipedia - Brazilian Real](https://en.wikipedia.org/wiki/Brazilian_real)
