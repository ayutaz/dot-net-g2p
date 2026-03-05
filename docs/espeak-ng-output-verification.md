# espeak-ng 実際の出力検証レポート

Docker環境（Ubuntu 24.04 + espeak-ng 1.51）で実際に音素出力を確認した結果。

## 検証環境

- **Docker**: Ubuntu 24.04ベース
- **espeak-ng**: 1.51 (`1.51+dfsg-12build1`)
- **データ**: `/usr/lib/x86_64-linux-gnu/espeak-ng-data`

## 1. 基本出力（IPA）

### アメリカ英語 (en-us)

| 入力 | IPA出力 |
|------|---------|
| hello | həlˈoʊ |
| world | wˈɜːld |
| computer | kəmpjˈuːɾɚ |
| beautiful | bjˈuːɾifəl |
| technology | tɛknˈɑːlədʒi |
| hello world | həlˈoʊ wˈɜːld |

### Kirshenbaum内部表記 (-x オプション)

| 入力 | 内部表記 |
|------|---------|
| hello world | h@l'oU w'3:ld |
| computer | k@mpj'u:t#3 |
| beautiful | bj'u:t#if@L |
| technology | t2Ekn'0l@dZi |
| The quick brown fox... | D@2 kw'Ik br'aUn f'0ks dZ'Vmps ,oUv3 D@2 l'eIzi d'0g |

## 2. 方言比較

### "hello world"

| 方言 | IPA出力 |
|------|---------|
| en-us (American) | həlˈoʊ wˈɜːld |
| en-gb (British) | həlˈəʊ wˈɜːld |
| en-gb-scotland (Scottish) | həlˈoː wˈʌɹld |
| en-gb-x-rp (RP) | həlˈəʊ wˈɜːld |
| en-029 (Caribbean) | həlˈoʊ wˈɜːld |
| en-us-nyc (New York City) | həlˈoʊ wˈəɪld |

注目点: `oʊ` vs `əʊ` vs `oː`（方言による二重母音の違い）、NYCの `wˈəɪld`（/ɜːl/ → /əɪl/ 変化）

### "water bottle"

| 方言 | IPA出力 |
|------|---------|
| en-us | wˈɔːɾɚ bˈɑːɾəl |
| en-gb | wˈɔːtə bˈɒtəl |
| en-gb-scotland | wˈɔːtɜ bˈɒtəl |

注目点: 米語の flapping（t→ɾ）、rhoticity（ɚ vs ə）

## 3. 同綴異音語 (homographs) の判別

| 単語 | 文脈 | IPA出力 | 正しい発音 | 判定 |
|------|------|---------|-----------|------|
| read | "I read books every day" (現在) | ɹˈiːd | /ɹiːd/ | ○ |
| read | "I read a book yesterday" (過去) | ɹˈiːd | /ɹɛd/ | **✗** |
| lead | "I will lead the team" (動詞) | lˈiːd | /liːd/ | ○ |
| lead | "made of lead" (名詞・鉛) | lˈiːd | /lɛd/ | **✗** |
| live | "I live in Tokyo" (動詞) | lˈɪv | /lɪv/ | ○ |
| live | "a live concert" (形容詞) | lˈaɪv | /laɪv/ | ○ |
| tear | "A tear rolled down" (涙) | tˈɪɹ | /tɪɹ/ | ○ |
| tear | "Don't tear the paper" (破る) | tˈɛɹ | /tɛɹ/ | ○ |
| wind | "The wind is blowing" (風) | wˈɪnd | /wɪnd/ | ○ |
| wind | "Wind up the clock" (巻く) | wˈaɪnd | /waɪnd/ | ○ |
| close | "Please close the door" (閉じる) | klˈoʊs | /kloʊz/ | **△**（z→s） |
| close | "Stay close to me" (近い) | klˈoʊs | /kloʊs/ | ○ |
| record | "I will record the song" (動詞) | ɹᵻkˈoːɹd | /ɹɪˈkɔːɹd/ | ○ |
| record | "This is a new record" (名詞) | ɹˈɛkɚd | /ˈɹɛkɚd/ | ○ |
| bow | "take a bow" (お辞儀) | bˈoʊ | /baʊ/ | **✗** |
| bow | "a bow and arrow" (弓) | bˈoʊ | /boʊ/ | ○ |

**結果**: 14テストケース中、正解10、不正解3、微妙1 → **同綴異音語正解率 約71%**
- `read`/`lead`の過去形・名詞形の判別に失敗（文脈情報を使わないため）
- `bow` (お辞儀 /baʊ/) が /boʊ/ になる
- `close` (動詞 /kloʊz/) の語末子音が /s/ になる

## 4. 数字・略語・特殊ケース

### 数字

| 入力 | IPA出力 | 読み |
|------|---------|------|
| 123 | wˈʌnhˈʌndɹɪd twˈɛnti θɹˈiː | one hundred twenty three |
| 3.14 | θɹˈiː pɔɪnt wˈʌn fˈoːɹ | three point one four |
| 100,000 | wˈʌnhˈʌndɹɪd θˈaʊzənd | one hundred thousand |
| $99.99 | nˈaɪn pɔɪnt nˈaɪn nˈaɪn | nine point nine nine（$記号無視） |
| 2026-03-05 | tˈuː θˈaʊzənd twˈɛnti sˈɪks dˈæʃ... | 日付としてではなく数字＋ダッシュとして読む |
| 3:14 PM | θɹˈiː fˈoːɹtiːn pˌiːˈɛm | three fourteen PM |
| 1st 2nd 3rd 4th | fˈɜːst sˈɛkənd θˈɜːd fˈoːɹθ | first second third fourth |

### 略語

| 入力 | IPA出力 | 読み方 |
|------|---------|--------|
| NASA | nˈæsɐ | 頭字語として1語で発音 |
| API | ˌeɪpˌiːˈaɪ | 1文字ずつ発音 |
| CEO | sˌiːˌiːˈoʊ | 1文字ずつ発音 |
| U.S.A. | jˌuːˌɛsˈeɪ | 1文字ずつ発音 |
| Dr. Smith | dˈɑːktɚ smˈɪθ | Doctor Smith に展開 |

## 5. 固有名詞

| 入力 | IPA出力 (en-us) |
|------|-----------------|
| Tokyo | tˈoʊkɪˌoʊ |
| London | lˈʌndən |
| Microsoft | mˈaɪkɹəsˌɑːft |
| Kubernetes | kjˈuːbɚnˌiːts |
| Schwarzenegger | ʃwˈɔːɹzənˌɛɡɚ |

## 6. 造語・未知語

| 入力 | IPA出力 (en-us) | 評価 |
|------|-----------------|------|
| blurfington | blˈɜːfɪŋtən | 自然な英語風 ○ |
| unmicrowaveable | ʌnmˈaɪkɹoʊwˌeɪvəbəl | 接頭辞・接尾辞を正しく処理 ○ |
| chatgpt | tʃˈætɡpt | 子音連続を維持 △ |
| defenestration | dᵻfˌɛnɪstɹˈeɪʃən | 正しい ○ |

## 7. 外来語

| 入力 | IPA出力 (en-us) | 評価 |
|------|-----------------|------|
| cafe | kˈæfeɪ | ○ |
| naive | naɪˈiːv | ○ |
| resume | ɹᵻzˈuːm | ○（レジュメ）|
| karate | kɚɹˈɑːɾi | ○ |
| tsunami | tsuːnˈɑːmi | ○ |

## 8. 接頭辞・接尾辞（長い単語）

| 入力 | IPA出力 (en-us) |
|------|-----------------|
| unhappiness | ʌnhˈæpɪnəs |
| misunderstanding | mɪsˌʌndɚstˈændɪŋ |
| internationalization | ˌɪntɚnˌæʃənəlᵻzˈeɪʃən |
| antidisestablishmentarianism | ˌæntɪdˌɪsɪstˌæblɪʃməntˈɛɹiənˌɪzəm |

## 9. 文レベル

| 入力 | IPA出力 (en-us) |
|------|-----------------|
| The quick brown fox jumps over the lazy dog. | ðə kwˈɪk bɹˈaʊn fˈɑːks dʒˈʌmps ˌoʊvɚ ðə lˈeɪzi dˈɑːɡ |
| She sells seashells by the seashore. | ʃiː sˈɛlz sˈiːʃɛlz baɪ ðə sˈiːʃoːɹ |
| How much wood would a woodchuck chuck? | hˌaʊ mˈʌtʃ wˈʊd wʊd ɐ wˈʊdtʃʌk tʃˈʌk |
| To be or not to be, that is the question. | təbi ɔːɹ nˌɑːt tə bˈiː / ðæt ɪz ðə kwˈɛstʃən |
| I can't believe it's not butter. | aɪ kˈænt bᵻlˈiːv ɪts nˌɑːt bˈʌɾɚ |
| Peter Piper picked a peck of pickled peppers. | pˈiːɾɚ pˈaɪpɚ pˈɪkt ɐ pˈɛk ʌv pˈɪkəld pˈɛpɚz |

## 10. エッジケース

| 入力 | IPA出力 | 備考 |
|------|---------|------|
| (空文字列) | (出力なし) | エラーなし |
| a | ˈeɪ | 文字名 |
| I | ˈaɪ | 代名詞 |
| ! | ˈɛkskləmˌeɪʃən | 記号名「exclamation」 |
| @ | ˈæt | 記号名「at」 |
| # | hˈæʃ | 記号名「hash」 |

## 11. 利用可能な英語方言

espeak-ng 1.51で利用可能な英語方言:

| コード | 名前 |
|--------|------|
| en-us | English (America) |
| en-gb | English (Great Britain) |
| en-gb-scotland | English (Scotland) |
| en-gb-x-rp | English (Received Pronunciation) |
| en-gb-x-gbclan | English (Lancaster) |
| en-gb-x-gbcwmd | English (West Midlands) |
| en-029 | English (Caribbean) |
| en-us-nyc | English (America, New York City) |

## 12. 総合評価

### 強み
- **基本的な英単語の音素変換は高精度**: 一般的な語彙に対して正確なIPA出力
- **数字・略語処理が充実**: 序数(1st→first)、略語展開(Dr.→Doctor)、頭字語判別(NASA vs API)
- **未知語への対応力が高い**: 造語(blurfington)や複合語(unmicrowaveable)もルールベースで自然に処理
- **方言対応**: 同一ルールセットで米英蘇等の方言差を反映（flapping, rhoticity等）
- **外来語もそれなりに対応**: cafe, karate, tsunami等

### 弱み
- **同綴異音語の文脈判別が弱い**: read(過去形), lead(鉛), bow(お辞儀)等の判別に失敗（約71%正解率）
- **$記号の処理**: $99.99が"dollar"として読まれない
- **日付フォーマット**: 2026-03-05が日付として認識されず数字＋ダッシュとして読まれる
- **close動詞の有声化**: /kloʊz/が/kloʊs/になる

### DotNetG2P実装への示唆
1. CMU辞書ルックアップだけでも、一般テキストの90-95%はカバー可能
2. LTSルールは未知語対応に必須だが、espeak-ngレベル（7,131行）のルールは大きな工数
3. 同綴異音語対応には品詞タグ等の文脈情報が必要（espeak-ngもここが弱点）
4. 数字・略語・記号の前処理は別途テキスト正規化モジュールとして実装すべき
