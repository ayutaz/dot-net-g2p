# Swedish G2P Test Data

## データソース

| ファイル | ソース | ライセンス | エントリ数 |
|---------|--------|-----------|----------|
| ipa_dict_sv_se_sample.tsv | ipa-dict (Folkets lexikon) | CC BY-SA 2.5 | 256 |
| wikipron_swe_latn_broad_filtered_sample.tsv | WikiPron (Wiktionary) | Apache-2.0 | 256 |

## 再生成手順

```powershell
# サンプルTSV再生成
pwsh tools/refresh_swedish_eval_data.ps1 -Mode Sample

# フルデータセット生成（Sw4用）
pwsh tools/refresh_swedish_eval_data.ps1 -Mode Full

# サンプル＋フル両方を再生成（デフォルト）
pwsh tools/refresh_swedish_eval_data.ps1
```

## 注意事項

- サンプルTSVは等間隔サンプリングで決定的に生成されています
- TSVはGitにコミットされます（評価再現性のため）
- フルデータセットはサイズが大きいため .gitignore 推奨
