# P12-002: FbxExportDataクラス設計メモ

**作成日**: 2026-01-27
**対応要件**: FR-080, FR-081, FR-082, FR-083, FR-084, FR-090, FR-091, NFR-004
**依存**: P12-001（FBX Exporterパッケージ調査）

---

## 1. 概要

本設計メモでは、FBXエクスポート機能で使用するデータ転送オブジェクト（DTO）`FbxExportData`クラスの設計を定義する。このクラスは、マージされたAnimationClipから抽出したスケルトン情報とアニメーションカーブ情報を保持し、FBXエクスポーターに渡すためのデータ構造である。

---

## 2. 設計方針

### 2.1 責務の分離

P12-001調査メモのセクション5.1「推奨アプローチ」に基づき、以下の方針を採用する：

1. **FbxExportData**: エクスポートに必要なデータを保持するDTO（本設計）
2. **FbxAnimationExporter**: FbxExportDataを受け取りFBX出力を行うクラス（P12-007で実装）

### 2.2 既存クラスとの関係

```
MergeResult (既存)
    ├── GeneratedClip: AnimationClip
    └── TargetAnimator: Animator
            ↓ 変換
FbxExportData (新規)
    ├── SkeletonData: スケルトン階層情報
    ├── AnimationCurves: カーブ情報リスト
    └── BlendShapeCurves: BlendShapeカーブ情報リスト
```

---

## 3. クラス設計

### 3.1 FbxExportData

FBXエクスポートに必要な全データを保持するメインクラス。

```csharp
namespace AnimationMergeTool.Editor.Domain.Models
{
    /// <summary>
    /// FBXエクスポートに必要なデータを保持するDTO
    /// </summary>
    public class FbxExportData
    {
        /// <summary>
        /// ソースAnimator
        /// </summary>
        public Animator SourceAnimator { get; }

        /// <summary>
        /// マージ済みAnimationClip
        /// </summary>
        public AnimationClip MergedClip { get; }

        /// <summary>
        /// スケルトン（ボーン階層）情報
        /// </summary>
        public SkeletonData Skeleton { get; }

        /// <summary>
        /// Transformアニメーションカーブ情報リスト
        /// </summary>
        public IReadOnlyList<TransformCurveData> TransformCurves { get; }

        /// <summary>
        /// BlendShapeアニメーションカーブ情報リスト
        /// </summary>
        public IReadOnlyList<BlendShapeCurveData> BlendShapeCurves { get; }

        /// <summary>
        /// エクスポート可能なデータが存在するか
        /// </summary>
        public bool HasExportableData { get; }

        /// <summary>
        /// ソースAnimatorがHumanoidリグか
        /// </summary>
        public bool IsHumanoid { get; }
    }
}
```

### 3.2 SkeletonData

スケルトン（ボーン階層）の情報を保持するクラス。

```csharp
namespace AnimationMergeTool.Editor.Domain.Models
{
    /// <summary>
    /// スケルトン（ボーン階層）情報を保持するDTO
    /// </summary>
    public class SkeletonData
    {
        /// <summary>
        /// ルートボーンのTransform
        /// </summary>
        public Transform RootBone { get; }

        /// <summary>
        /// 全ボーンのTransformリスト（階層順）
        /// </summary>
        public IReadOnlyList<Transform> Bones { get; }

        /// <summary>
        /// スケルトンが存在するか
        /// </summary>
        public bool HasSkeleton { get; }
    }
}
```

### 3.3 TransformCurveData

Transformアニメーションカーブの情報を保持するクラス。

```csharp
namespace AnimationMergeTool.Editor.Domain.Models
{
    /// <summary>
    /// Transformアニメーションカーブ情報を保持するDTO
    /// </summary>
    public class TransformCurveData
    {
        /// <summary>
        /// 対象TransformのAnimator相対パス
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// プロパティ名（localPosition.x, localRotation.x等）
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// アニメーションカーブ
        /// </summary>
        public AnimationCurve Curve { get; }

        /// <summary>
        /// カーブの種類
        /// </summary>
        public TransformCurveType CurveType { get; }
    }

    /// <summary>
    /// Transformカーブの種類
    /// </summary>
    public enum TransformCurveType
    {
        Position,
        Rotation,
        Scale,
        EulerAngles
    }
}
```

### 3.4 BlendShapeCurveData

BlendShapeアニメーションカーブの情報を保持するクラス。

```csharp
namespace AnimationMergeTool.Editor.Domain.Models
{
    /// <summary>
    /// BlendShapeアニメーションカーブ情報を保持するDTO
    /// </summary>
    public class BlendShapeCurveData
    {
        /// <summary>
        /// 対象SkinnedMeshRendererのAnimator相対パス
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// BlendShape名（blendShape.を除いた名前）
        /// </summary>
        public string BlendShapeName { get; }

        /// <summary>
        /// アニメーションカーブ
        /// </summary>
        public AnimationCurve Curve { get; }
    }
}
```

---

## 4. ファイル構成

既存のModelsフォルダ構造に従い、以下のファイルを作成する：

```
Scripts/Editor/Domain/Models/
├── TrackInfo.cs          (既存)
├── ClipInfo.cs           (既存)
├── MergeResult.cs        (既存)
├── FbxExportData.cs      (新規 - P12-003で実装)
├── SkeletonData.cs       (新規 - P12-003で実装)
├── TransformCurveData.cs (新規 - P12-003で実装)
└── BlendShapeCurveData.cs(新規 - P12-003で実装)
```

**注**: 実装の簡素化のため、FbxExportData.cs内に全クラスを含めることも可とする。

---

## 5. 要件対応マッピング

| 要件ID | 要件内容 | 対応クラス/プロパティ |
|--------|----------|----------------------|
| FR-080 | FBX形式で出力 | FbxExportData全体 |
| FR-081 | スケルトン含める | SkeletonData |
| FR-082 | スケルトン以外のTransformアニメーション | TransformCurveData |
| FR-083 | マージされたアニメーションカーブ | FbxExportData.MergedClip |
| FR-084 | BlendShapeアニメーション | BlendShapeCurveData |
| FR-090 | Generic形式でエクスポート | FbxExportData.IsHumanoid（変換判定用） |
| FR-091 | Humanoidボーン名→Transformパス変換 | TransformCurveData.Path（変換後の値を格納） |
| NFR-004 | 既存ロジック再利用 | FbxExportData.MergedClip |

---

## 6. ERR-004対応

`FbxExportData.HasExportableData`プロパティでエクスポート可能なデータの存在を判定する：

```csharp
public bool HasExportableData =>
    (Skeleton != null && Skeleton.HasSkeleton) ||
    (TransformCurves != null && TransformCurves.Count > 0) ||
    (BlendShapeCurves != null && BlendShapeCurves.Count > 0);
```

---

## 7. 次のステップ

1. **P12-003**: 本設計に基づきFbxExportData関連クラスを実装
2. **P13-002**: SkeletonData取得のテスト作成
3. **P15-002**: BlendShapeCurveData検出（FBX用）のテスト作成

---

## 更新履歴

| バージョン | 日付 | 変更内容 |
|------------|------|----------|
| 1.0 | 2026-01-27 | 初版作成 |
