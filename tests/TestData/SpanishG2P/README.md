# Spanish Evaluation Samples

These TSV files are deterministic samples derived from:

- `ipa-dict` (MIT): https://github.com/open-dict-data/ipa-dict
- `WikiPron` (Apache-2.0): https://github.com/CUNY-CL/wikipron

They are used for offline regression tests only. Refresh them with:

```powershell
powershell -ExecutionPolicy Bypass -File tools/refresh_spanish_eval_data.ps1 -Mode Sample
```

Generate the full corpora used by the evaluation toolchain with:

```powershell
powershell -ExecutionPolicy Bypass -File tools/refresh_spanish_eval_data.ps1 -Mode Full
```

Or refresh both sample and full corpora in one run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/refresh_spanish_eval_data.ps1 -Mode Both
```

Current generation policy:

- keep single-token alphabetic words only
- require at least one lowercase Spanish letter
- keep words with length `3..16`
- keep only entries whose IPA symbols match the current test normalizer
- deduplicate by lowercase surface form
- select a deterministic evenly spaced sample of 256 entries per upstream corpus
