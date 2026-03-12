# Korean G2P Benchmark Seeds

These TSV files establish the M0 benchmark scaffold for Korean G2P.

Files:

- `g2pk_parity.tsv`
  - starter cases intended to mirror `g2pK` behavior on stable Hangul-only words
- `official_gold.tsv`
  - starter cases seeded from National Institute of Korean Language references curated for this repository
- `weak_rules.tsv`
  - starter cases for rules called out as weak spots in the 2022 comparison paper

Schema:

- header: `input	expected	source	rule_tag	notes`
- `input`: source spelling in Hangul
- `expected`: normalized pronunciation in Hangul. Alternate accepted outputs may be joined by `|`
- `source`: short provenance label such as `g2pK_0.9.4`, `NIKL_QNA_312379`, `Mun2022`
- `rule_tag`: one of
  - `neutralization`
  - `resyllabification`
  - `tensification`
  - `nasalization`
  - `liquidization`
  - `h-deletion`
  - `n-insertion`
  - `ui-variation`
  - `place-assimilation`
- `notes`: short comment for why the row exists

Notes:

- M0 stores `expected` as pronunciation Hangul, not final phoneme inventory. This avoids locking the internal phoneme set too early.
- When a rule has multiple standard-accepted outputs, `expected` may store alternatives such as `검녈|거멸`.
- Current rows are hand-curated starter seeds only. They are intentionally small and must be expanded and source-verified before M3.
- If `g2pK` parity and official gold disagree, future evaluation should prefer official gold.
- M3 benchmark reports are generated to `tests/DotNetG2P.Tests/TestResults/KoreanG2P/` as:
  - `korean-benchmark-summary.json`
  - `korean-benchmark-dataset-summary.tsv`
  - `korean-benchmark-rule-summary.tsv`
  - `korean-benchmark-mismatches.tsv`

External corpus evaluation:

- `external_corpus.template.tsv` shows the schema expected by `KoreanExternalBenchmarkTests`.
- Configure external benchmark files with `DOTNETG2P_KOREAN_EXTERNAL_CORPUS_PATHS`.
  - Use `Path.PathSeparator` delimited paths. On Windows this means `;`.
- Optional gates:
  - `DOTNETG2P_KOREAN_EXTERNAL_MIN_CASES`
  - `DOTNETG2P_KOREAN_EXTERNAL_ACCURACY_THRESHOLD`
- Recommended official source pipelines:
  - `한국어기초사전 Open API` pronunciation fields
  - `표준국어대사전` or `우리말샘` entries curated into the same TSV schema
