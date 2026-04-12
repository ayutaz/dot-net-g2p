# Misaki互換 中国語G2P チケット一覧

> 対応Issue: [#56](https://github.com/ayutaz/dot-net-g2p/issues/56)
> マイルストーン計画: [../guides/misaki-milestones.md](../guides/misaki-milestones.md)
> 設計ドキュメント: [../guides/misaki-compatible-chinese.md](../guides/misaki-compatible-chinese.md)

## フェーズ別チケット

### Phase 1 — Mi1: PinyinToMisaki 変換クラス

| ID | タイトル | 状態 | 依存 | 後続 |
|----|----------|------|------|------|
| [T01](T01-misaki-mapping-tables.md) | PinyinToMisaki マッピングテーブル設計・実装 | 未着手 | なし | T02 |
| [T02](T02-misaki-convert-method.md) | PinyinToMisaki Convert メソッド統合 | 未着手 | T01 | T03 |

### Phase 2 — Mi2: API統合 + テスト

| ID | タイトル | 状態 | 依存 | 後続 |
|----|----------|------|------|------|
| [T03](T03-engine-api-integration.md) | ChineseG2PEngine ToMisakiIpa API 追加 | 未着手 | T02 | T04 |
| [T04](T04-misaki-tests.md) | Misaki互換テスト実装 | 未着手 | T03 | T05 |

### Phase 3 — Mi3: ドキュメント・品質保証・リリース準備

| ID | タイトル | 状態 | 依存 | 後続 |
|----|----------|------|------|------|
| [T05](T05-documentation-qa.md) | ドキュメント更新・品質保証 | 未着手 | T04 | T06 |
| [T06](T06-release-followup.md) | Issue#56 フォローアップ・リリース準備 | 未着手 | T05 | なし |

## 依存関係グラフ

```
T01 ──► T02 ──► T03 ──► T04 ──► T05 ──► T06
 [Mi1]    [Mi1]   [Mi2]   [Mi2]   [Mi3]   [Mi3]
```

## フェーズレビュー

各フェーズ完了後、「一から作り直すとしたら」セクションをエージェントチームでレビュー・修正してから次フェーズに進む運用とする。

| フェーズ | レビュー対象チケット | レビュー状態 |
|---------|---------------------|-------------|
| Phase 1 | T01, T02 | **完了** (3エージェント: アーキテクト/マッピング戦略/テスタビリティ) |
| Phase 2 | T03, T04 | **完了** (3エージェント: API設計/テスト戦略/システム統合) |
| Phase 3 | T05, T06 | **完了** (3エージェント: ドキュメント戦略/リリース戦略/全体振り返り) |
