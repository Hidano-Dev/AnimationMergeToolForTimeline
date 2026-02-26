# HANDOVER.md

## 今回やったこと

- OverrideTrackのクリップ終了時にベースレイヤーへフォールバックする際、1Fのブレンドが入る問題を調査
- 根本原因を特定（修正実装は未着手）

## 決定事項

- 修正対象は `CurveOverrider.ApplyPartialOverrideWithActiveIntervals` のStep 3（ギャップ境界遷移キー追加処理）のみ
- 最初のアクティブ区間の直前・最後のアクティブ区間の直後にもベースカーブの遷移キーを追加する方針

## 捨てた選択肢と理由

- 特になし（調査フェーズのため）

## ハマりどころ

- 特になし

## 学び

- `ApplyPartialOverrideWithActiveIntervals` のStep 3ループ（L251）は `activeIntervals.Count - 1` 回しか回らないため、Overrideクリップが1つだけの典型的ケースではループが0回実行される → 遷移キーが一切追加されない
- AnimationCurveのデフォルトスムーズ補間により、Overrideの最終キーと次のベースキー間が勝手に補間される。CurveResamplerがフレーム境界でEvaluateした際、この補間値が1Fのブレンドとして出力される

## 次にやること

### 最優先: Override→ベースフォールバック時の1Fブレンド修正

**問題の詳細:**

`CurveOverrider.ApplyPartialOverrideWithActiveIntervals` (CurveOverrider.cs:201-280) のStep 3ギャップ境界処理:

```csharp
// CurveOverrider.cs:251
for (var i = 0; i < activeIntervals.Count - 1; i++)
```

このループはアクティブ区間**間**のギャップのみ処理。以下が未処理:
1. **最後のアクティブ区間の終端 → ベースへのフォールバック**（最も典型的）
2. **最初のアクティブ区間の始端 ← ベースからの遷移**

**再現シナリオ:**
- ベーストラック: 0s〜10sのクリップ
- Overrideトラック: 2s〜5sのクリップ（PostExtrapolation=None）
- activeIntervalsは [{2.0, 5.0}] の1要素 → ループ0回実行 → 遷移キーなし
- 結果カーブ: 5.0sにOverride値のキー、次のベースキーまでスムーズ補間 → 1Fブレンド発生

**修正方針:**

`ApplyPartialOverrideWithActiveIntervals` のStep 3の後に追加処理:

```
// 擬似コード
// 最初のアクティブ区間の直前に遷移キーを追加
if (最初のアクティブ区間の前にベースカーブのキーが存在する) {
    nearFirstStart = activeIntervals[0].StartTime - boundaryOffset;
    resultCurve.AddKey(nearFirstStart, lowerPriorityCurve.Evaluate(nearFirstStart));
}

// 最後のアクティブ区間の直後に遷移キーを追加
if (最後のアクティブ区間の後にベースカーブのキーが存在する) {
    nearLastEnd = activeIntervals[last].EndTime + boundaryOffset;
    resultCurve.AddKey(nearLastEnd, lowerPriorityCurve.Evaluate(nearLastEnd));
}
```

`boundaryOffset = 0.001f`（既存の定数と同じ値を使用）。

**影響範囲:**
- 変更ファイル: `CurveOverrider.cs` のみ（`ApplyPartialOverrideWithActiveIntervals`メソッド内）
- 既存テスト: `CurveOverriderTests.cs` にActiveIntervals系テスト8件あり → 境界値のアサーション確認が必要
- 新規テスト追加: 「Overrideクリップ1つのみ」のケースでフォールバック境界の値をアサートするテストを追加すべき

### その他（優先度低）
- GenericリグのシーンオフセットFBX反映問題（未解決、memory/generic-rig-rootmotion-investigation.md参照）

## 関連ファイル

- `AnimationMergeToolForTimeline/Packages/AnimationMergeToolForTimeline/Scripts/Editor/Domain/CurveOverrider.cs` — 修正対象（L201-280: ApplyPartialOverrideWithActiveIntervals）
- `AnimationMergeToolForTimeline/Packages/AnimationMergeToolForTimeline/Scripts/Editor/Domain/CurveResampler.cs` — 後段処理（Evaluate時にブレンド値が出力される原因）
- `AnimationMergeToolForTimeline/Packages/AnimationMergeToolForTimeline/Scripts/Editor/Application/AnimationMergeService.cs` — ComputeActiveIntervals（L459-491）
- `AnimationMergeToolForTimeline/Packages/AnimationMergeToolForTimeline/Tests/Editor/CurveOverriderTests.cs` — 既存テスト確認・新規テスト追加先
