# P14-002: HumanoidToGenericConverterクラス設計メモ

**作成日**: 2026-01-27
**対応要件**: FR-090, FR-091, FR-092
**依存**: P14-001（Humanoidボーンマッピング調査）

---

## 1. 概要

本設計メモでは、HumanoidリグのアニメーションをGeneric形式に変換するための`HumanoidToGenericConverter`クラスを設計する。このクラスはFBXエクスポート時にHumanoidアニメーションをTransformベースのカーブに変換する責務を持つ。

---

## 2. 要件分析

### 2.1 対象要件

| 要件ID | 要件内容 | 本設計での対応 |
|--------|----------|----------------|
| FR-090 | FBX出力は常にGeneric形式とする | HumanoidカーブをTransformカーブに変換 |
| FR-091 | HumanoidボーンをTransformパスに変換してエクスポート | Animator.GetBoneTransform()でパス取得 |
| FR-092 | ルートモーションが含まれる場合は適切にエクスポート | RootT/RootQをlocalPosition/localRotationに変換 |

### 2.2 変換の必要性

Humanoidアニメーションは以下の特徴を持つ：
- マッスルカーブ（正規化された-1.0～1.0の値）で回転を表現
- ボーン名ではなくHumanBodyBones列挙値でボーンを識別
- ルートモーションはRootT/RootQプロパティで表現

FBXエクスポート時にGeneric形式に変換するため、これらをTransformベースのカーブに変換する必要がある。

---

## 3. クラス設計

### 3.1 HumanoidToGenericConverter クラス

```csharp
namespace AnimationMergeTool.Editor.Infrastructure
{
    /// <summary>
    /// HumanoidアニメーションをGeneric形式に変換するクラス
    /// </summary>
    public class HumanoidToGenericConverter
    {
        /// <summary>
        /// HumanoidアニメーションをGeneric形式のカーブに変換する
        /// </summary>
        /// <param name="animator">対象のAnimator（Humanoidリグ）</param>
        /// <param name="humanoidClip">変換元のHumanoidアニメーションクリップ</param>
        /// <returns>変換されたTransformカーブのリスト</returns>
        public List<TransformCurveData> Convert(Animator animator, AnimationClip humanoidClip);

        /// <summary>
        /// HumanBodyBonesからTransformパスを取得する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="bone">HumanBodyBones列挙値</param>
        /// <returns>Animator相対のTransformパス（ボーンが見つからない場合はnull）</returns>
        public string GetTransformPath(Animator animator, HumanBodyBones bone);

        /// <summary>
        /// マッスルカーブをRotationカーブに変換する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="humanoidClip">変換元のAnimationClip</param>
        /// <returns>変換されたRotationカーブのリスト</returns>
        public List<TransformCurveData> ConvertMuscleCurvesToRotation(
            Animator animator,
            AnimationClip humanoidClip);

        /// <summary>
        /// ルートモーションカーブをTransformカーブに変換する
        /// </summary>
        /// <param name="humanoidClip">変換元のAnimationClip</param>
        /// <param name="rootBonePath">ルートボーンのパス</param>
        /// <returns>変換されたルートモーションカーブのリスト</returns>
        public List<TransformCurveData> ConvertRootMotionCurves(
            AnimationClip humanoidClip,
            string rootBonePath);

        /// <summary>
        /// AnimationClipがHumanoid形式かどうかを判定する
        /// </summary>
        /// <param name="clip">判定対象のAnimationClip</param>
        /// <returns>Humanoid形式の場合はtrue</returns>
        public bool IsHumanoidClip(AnimationClip clip);
    }
}
```

### 3.2 ファイル配置

```
Scripts/Editor/Infrastructure/
├── FbxAnimationExporter.cs        (既存)
├── FbxPackageChecker.cs           (既存)
├── SkeletonExtractor.cs           (既存)
└── HumanoidToGenericConverter.cs  (新規 - P14-004で実装)
```

---

## 4. 変換ロジック詳細

### 4.1 メイン変換フロー

```
Convert(animator, humanoidClip)
    │
    ├─ IsHumanoidClip(humanoidClip) ?
    │       No → 既存のTransformカーブをそのまま返す
    │
    ├─ マッスルカーブの変換
    │       └─ ConvertMuscleCurvesToRotation()
    │               │
    │               ├─ 各フレームをサンプリング
    │               │       └─ humanoidClip.SampleAnimation()
    │               │
    │               ├─ 各ボーンの回転を取得
    │               │       └─ boneTransform.localRotation
    │               │
    │               └─ Rotationカーブを生成
    │
    ├─ ルートモーションカーブの変換
    │       └─ ConvertRootMotionCurves()
    │               │
    │               ├─ RootT.x/y/z → localPosition.x/y/z
    │               │
    │               └─ RootQ.x/y/z/w → localRotation.x/y/z/w
    │
    └─ 変換結果をマージして返す
```

### 4.2 ボーン名→Transformパス変換

```csharp
public string GetTransformPath(Animator animator, HumanBodyBones bone)
{
    if (animator == null || !animator.isHuman)
    {
        return null;
    }

    // HumanBodyBonesからTransformを取得
    Transform boneTransform = animator.GetBoneTransform(bone);
    if (boneTransform == null)
    {
        return null;
    }

    // Animatorからの相対パスを構築
    return BuildRelativePath(animator.transform, boneTransform);
}

private string BuildRelativePath(Transform root, Transform target)
{
    if (target == root)
    {
        return string.Empty;
    }

    var pathParts = new List<string>();
    Transform current = target;

    while (current != null && current != root)
    {
        pathParts.Insert(0, current.name);
        current = current.parent;
    }

    // rootの子でなければnullを返す
    if (current == null)
    {
        return null;
    }

    return string.Join("/", pathParts);
}
```

### 4.3 マッスルカーブ→Rotationカーブ変換

```csharp
public List<TransformCurveData> ConvertMuscleCurvesToRotation(
    Animator animator,
    AnimationClip humanoidClip)
{
    var result = new List<TransformCurveData>();

    if (animator == null || humanoidClip == null || !animator.isHuman)
    {
        return result;
    }

    float frameRate = humanoidClip.frameRate;
    float duration = humanoidClip.length;
    float sampleInterval = 1.0f / frameRate;

    // 各ボーンのカーブを準備
    var boneRotationCurves = new Dictionary<HumanBodyBones, RotationCurveSet>();

    // 有効なボーンを列挙
    foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
    {
        if (bone == HumanBodyBones.LastBone) continue;

        Transform boneTransform = animator.GetBoneTransform(bone);
        if (boneTransform == null) continue;

        boneRotationCurves[bone] = new RotationCurveSet();
    }

    // 各フレームをサンプリング
    for (float time = 0; time <= duration; time += sampleInterval)
    {
        // クリップをサンプリング
        humanoidClip.SampleAnimation(animator.gameObject, time);

        // 各ボーンの回転を記録
        foreach (var kvp in boneRotationCurves)
        {
            Transform boneTransform = animator.GetBoneTransform(kvp.Key);
            if (boneTransform == null) continue;

            Quaternion rotation = boneTransform.localRotation;
            kvp.Value.AddKey(time, rotation);
        }
    }

    // カーブをTransformCurveDataに変換
    foreach (var kvp in boneRotationCurves)
    {
        string path = GetTransformPath(animator, kvp.Key);
        if (string.IsNullOrEmpty(path)) continue;

        var curves = kvp.Value.ToTransformCurveDataList(path);
        result.AddRange(curves);
    }

    return result;
}

/// <summary>
/// Quaternionカーブを管理する内部クラス
/// </summary>
private class RotationCurveSet
{
    public AnimationCurve X { get; } = new AnimationCurve();
    public AnimationCurve Y { get; } = new AnimationCurve();
    public AnimationCurve Z { get; } = new AnimationCurve();
    public AnimationCurve W { get; } = new AnimationCurve();

    public void AddKey(float time, Quaternion rotation)
    {
        X.AddKey(time, rotation.x);
        Y.AddKey(time, rotation.y);
        Z.AddKey(time, rotation.z);
        W.AddKey(time, rotation.w);
    }

    public List<TransformCurveData> ToTransformCurveDataList(string path)
    {
        return new List<TransformCurveData>
        {
            new TransformCurveData(path, "localRotation.x", X, TransformCurveType.Rotation),
            new TransformCurveData(path, "localRotation.y", Y, TransformCurveType.Rotation),
            new TransformCurveData(path, "localRotation.z", Z, TransformCurveType.Rotation),
            new TransformCurveData(path, "localRotation.w", W, TransformCurveType.Rotation)
        };
    }
}
```

### 4.4 ルートモーションカーブ変換

```csharp
public List<TransformCurveData> ConvertRootMotionCurves(
    AnimationClip humanoidClip,
    string rootBonePath)
{
    var result = new List<TransformCurveData>();

    if (humanoidClip == null)
    {
        return result;
    }

    var bindings = AnimationUtility.GetCurveBindings(humanoidClip);

    foreach (var binding in bindings)
    {
        // ルートモーションのパスは空文字
        if (!string.IsNullOrEmpty(binding.path))
        {
            continue;
        }

        var curve = AnimationUtility.GetEditorCurve(humanoidClip, binding);
        if (curve == null)
        {
            continue;
        }

        // プロパティ名の変換
        string newPropertyName = ConvertRootMotionPropertyName(binding.propertyName);
        if (newPropertyName == null)
        {
            continue;
        }

        var curveType = newPropertyName.StartsWith("localPosition")
            ? TransformCurveType.Position
            : TransformCurveType.Rotation;

        result.Add(new TransformCurveData(
            rootBonePath,
            newPropertyName,
            curve,
            curveType));
    }

    return result;
}

private string ConvertRootMotionPropertyName(string propertyName)
{
    switch (propertyName)
    {
        case "RootT.x": return "localPosition.x";
        case "RootT.y": return "localPosition.y";
        case "RootT.z": return "localPosition.z";
        case "RootQ.x": return "localRotation.x";
        case "RootQ.y": return "localRotation.y";
        case "RootQ.z": return "localRotation.z";
        case "RootQ.w": return "localRotation.w";
        default: return null;
    }
}
```

### 4.5 Humanoidクリップ判定

```csharp
public bool IsHumanoidClip(AnimationClip clip)
{
    if (clip == null)
    {
        return false;
    }

    // clip.isHumanMotionプロパティを使用
    return clip.isHumanMotion;
}
```

---

## 5. FbxAnimationExporterとの統合

### 5.1 呼び出し箇所

`FbxAnimationExporter`クラスでHumanoid→Generic変換を行う：

```csharp
public class FbxAnimationExporter
{
    private readonly SkeletonExtractor _skeletonExtractor;
    private readonly HumanoidToGenericConverter _humanoidConverter;

    public FbxAnimationExporter()
    {
        _skeletonExtractor = new SkeletonExtractor();
        _humanoidConverter = new HumanoidToGenericConverter();
    }

    /// <summary>
    /// FbxExportDataを生成する
    /// </summary>
    public FbxExportData CreateExportData(Animator animator, AnimationClip mergedClip)
    {
        // スケルトン取得
        SkeletonData skeleton = _skeletonExtractor.Extract(animator);

        // Humanoidの場合は変換
        List<TransformCurveData> transformCurves;
        bool isHumanoid = animator != null && animator.isHuman;

        if (isHumanoid && _humanoidConverter.IsHumanoidClip(mergedClip))
        {
            // Humanoid→Generic変換
            transformCurves = _humanoidConverter.Convert(animator, mergedClip);
        }
        else
        {
            // 既存のTransformカーブを抽出
            transformCurves = ExtractTransformCurves(mergedClip, animator);
        }

        // BlendShapeカーブの抽出
        var blendShapeCurves = ExtractBlendShapeCurves(mergedClip);

        return new FbxExportData(
            animator,
            mergedClip,
            skeleton,
            transformCurves,
            blendShapeCurves,
            isHumanoid
        );
    }
}
```

---

## 6. エッジケース対応

### 6.1 考慮すべきケース

| ケース | 対応 |
|--------|------|
| Animatorがnull | 空のリストを返す |
| AnimatorがHumanoidでない | 既存カーブをそのまま返す |
| AnimationClipがnull | 空のリストを返す |
| AnimationClipがHumanoidでない | 既存カーブをそのまま返す |
| オプションボーンが未設定 | スキップして処理続行 |
| マッスルカーブがない | Transform既存カーブのみ出力 |
| ルートモーションがない | 警告なしでスキップ |
| フレームレートが0 | デフォルト値（60fps）を使用 |

### 6.2 パフォーマンス考慮事項

- 全フレームサンプリングは処理コストが高い
- 長時間アニメーション（5分以上）では進捗表示を推奨
- 必要に応じてサンプリング間隔を調整可能にする（将来の拡張）

---

## 7. テスト方針（P14-003で作成）

### 7.1 テストケース

1. **ボーン名→Transformパス変換**
   - 全HumanBodyBonesに対するパス変換テスト
   - オプションボーン未設定時のnull返却テスト
   - Animator自身へのパスは空文字テスト

2. **マッスルカーブ→Rotation変換（P14-005で作成）**
   - サンプリング精度のテスト
   - 各ボーンの回転値が正しく取得されることを確認
   - フレームレートが正しく維持されることを確認

3. **ルートモーション変換（P14-007で作成）**
   - RootT → localPosition変換テスト
   - RootQ → localRotation変換テスト
   - ルートモーションがないクリップのテスト

4. **Humanoidクリップ判定**
   - Humanoidクリップでtrueを返すテスト
   - 非Humanoidクリップでfalseを返すテスト

---

## 8. 依存関係

### 8.1 使用するUnity API

| クラス/構造体 | 用途 |
|---------------|------|
| `Animator` | ボーンTransformの取得 |
| `HumanBodyBones` | Humanoidボーンの列挙 |
| `AnimationClip` | アニメーションデータの取得・サンプリング |
| `AnimationUtility` | カーブバインディングの取得 |
| `Transform` | ボーン位置・回転の取得 |

### 8.2 プロジェクト内依存

| クラス | 用途 |
|--------|------|
| `TransformCurveData` | 変換結果の格納 |
| `TransformCurveType` | カーブタイプの識別 |
| `FbxExportData` | エクスポートデータへの統合 |
| `SkeletonExtractor` | スケルトン情報の取得（FbxAnimationExporter経由） |

---

## 9. 次のステップ

1. **P14-003**: ボーン名→Transformパス変換のテスト作成
2. **P14-004**: ボーン名→Transformパス変換の実装
3. **P14-005**: マッスルカーブ→Rotation変換のテスト作成
4. **P14-006**: マッスルカーブ→Rotation変換の実装
5. **P14-007**: ルートモーション変換のテスト作成
6. **P14-008**: ルートモーション変換の実装
7. **P14-009**: FbxAnimationExporterとの統合

---

## 更新履歴

| バージョン | 日付 | 変更内容 |
|------------|------|----------|
| 1.0 | 2026-01-27 | 初版作成 |
