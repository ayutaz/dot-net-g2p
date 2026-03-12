# Korean G2P Benchmark Seeds

These TSV files establish the M0 benchmark scaffold for Korean G2P.

Files:

- `g2pk_parity.tsv`
  - starter cases intended to mirror `g2pK` behavior on stable Hangul-only words
- `official_gold.tsv`
  - starter cases seeded from examples cited in National Institute of Korean Language references already listed in `docs/korean-g2p-research.md`
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
