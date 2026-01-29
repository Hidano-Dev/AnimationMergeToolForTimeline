# P11-001: BlendShapeカーブ検出機能の設計メモ

**作成日**: 2026-01-27
**対応要件**: FR-072

---

## 1. 概要

BlendShape（モーフターゲット）アニメーションカーブを検出する機能を設計する。
既存の `RootMotionDetector` および `MuscleDetector` クラスのパターンを踏襲し、`BlendShapeDetector` クラスを新規作成する。

---

## 2. BlendShapeカーブの識別方法

### 2.1 プロパティパスの構造

UnityにおけるBlendShapeアニメーションカーブは、以下の形式でEditorCurveBindingに格納される：

- **path**: SkinnedMeshRendererコンポーネントを持つGameObjectへのパス（例: `"Body"`, `"Character/Face"`）
- **type**: `typeof(SkinnedMeshRenderer)`
- **propertyName**: `"blendShape.{BlendShape名}"` の形式（例: `"blendShape.Smile"`, `"blendShape.eyeBlink_L"`）

### 2.2 識別条件

BlendShapeカーブを識別するための条件：

1. `binding.propertyName` が `"blendShape."` プレフィックスで始まる
2. `binding.type` が `SkinnedMeshRenderer` 型である

**注意**: pathは空でない場合が多い（SkinnedMeshRendererはルートオブジェクト以外に配置されることが多いため）。

---

## 3. クラス設計

### 3.1 クラス名

`BlendShapeDetector`

### 3.2 配置場所

`Scripts/Editor/Domain/BlendShapeDetector.cs`

### 3.3 名前空間

`AnimationMergeTool.Editor.Domain`

### 3.4 パブリックAPI

```csharp
public class BlendShapeDetector
{
    /// <summary>
    /// BlendShapeカーブのプロパティ名プレフィックス
    /// </summary>
    public const string BlendShapePrefix = "blendShape.";

    /// <summary>
    /// 指定されたEditorCurveBindingがBlendShapeプロパティかどうかを判定する
    /// </summary>
    /// <param name="binding">判定対象のEditorCurveBinding</param>
    /// <returns>BlendShapeプロパティの場合はtrue</returns>
    public bool IsBlendShapeProperty(EditorCurveBinding binding);

    /// <summary>
    /// AnimationClipからBlendShapeカーブを検出する
    /// </summary>
    /// <param name="clip">検索対象のAnimationClip</param>
    /// <returns>BlendShapeカーブのバインディングとカーブのペアのリスト</returns>
    public List<CurveBindingPair> DetectBlendShapeCurves(AnimationClip clip);

    /// <summary>
    /// AnimationClipがBlendShapeカーブを持っているかどうかを判定する
    /// </summary>
    /// <param name="clip">検索対象のAnimationClip</param>
    /// <returns>BlendShapeカーブを持っている場合はtrue</returns>
    public bool HasBlendShapeCurves(AnimationClip clip);

    /// <summary>
    /// EditorCurveBindingからBlendShape名を抽出する
    /// </summary>
    /// <param name="binding">BlendShapeプロパティのEditorCurveBinding</param>
    /// <returns>BlendShape名（"blendShape."プレフィックスを除いた部分）。BlendShapeプロパティでない場合はnull</returns>
    public string GetBlendShapeName(EditorCurveBinding binding);
}
```

---

## 4. 実装詳細

### 4.1 IsBlendShapeProperty メソッド

```csharp
public bool IsBlendShapeProperty(EditorCurveBinding binding)
{
    if (string.IsNullOrEmpty(binding.propertyName))
    {
        return false;
    }

    // typeがSkinnedMeshRendererかチェック
    if (binding.type != typeof(SkinnedMeshRenderer))
    {
        return false;
    }

    // プロパティ名が"blendShape."で始まるかチェック
    return binding.propertyName.StartsWith(BlendShapePrefix);
}
```

### 4.2 DetectBlendShapeCurves メソッド

`RootMotionDetector.DetectRootMotionCurves` と同様のパターンで実装：

```csharp
public List<CurveBindingPair> DetectBlendShapeCurves(AnimationClip clip)
{
    var result = new List<CurveBindingPair>();

    if (clip == null)
    {
        return result;
    }

    var bindings = AnimationUtility.GetCurveBindings(clip);
    foreach (var binding in bindings)
    {
        if (IsBlendShapeProperty(binding))
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve != null)
            {
                result.Add(new CurveBindingPair(binding, curve));
            }
        }
    }

    return result;
}
```

### 4.3 GetBlendShapeName メソッド

```csharp
public string GetBlendShapeName(EditorCurveBinding binding)
{
    if (!IsBlendShapeProperty(binding))
    {
        return null;
    }

    return binding.propertyName.Substring(BlendShapePrefix.Length);
}
```

---

## 5. 依存関係

### 5.1 使用する既存クラス

- `CurveBindingPair` (ClipMerger.cs内で定義済み)

### 5.2 必要な名前空間

- `System.Collections.Generic`
- `UnityEditor`
- `UnityEngine`

---

## 6. テスト観点

P11-002で作成するテストの観点：

1. **基本検出テスト**
   - BlendShapeカーブを含むAnimationClipから正しく検出できること
   - BlendShapeカーブを含まないAnimationClipでは空リストを返すこと

2. **プロパティ判定テスト**
   - `IsBlendShapeProperty`が正しくtrue/falseを返すこと
   - `SkinnedMeshRenderer`以外の型ではfalseを返すこと
   - `blendShape.`以外のプレフィックスではfalseを返すこと

3. **BlendShape名抽出テスト**
   - `GetBlendShapeName`が正しくBlendShape名を返すこと
   - BlendShapeプロパティでない場合はnullを返すこと

4. **エッジケース**
   - nullのAnimationClipを渡した場合
   - 空のAnimationClipを渡した場合
   - 複数のBlendShapeカーブを含むAnimationClip

---

## 7. 既存コードへの影響

### 7.1 ClipMerger

現状、ClipMergerはすべてのカーブタイプを同一に処理しているため、BlendShapeカーブも自動的にマージされる。追加の修正は不要。

### 7.2 CurveOverrider

P11-006/P11-007でBlendShapeカーブのOverride処理を追加予定。現時点では修正不要。

---

## 8. 参考資料

- Unity公式ドキュメント: [SkinnedMeshRenderer.SetBlendShapeWeight](https://docs.unity3d.com/ScriptReference/SkinnedMeshRenderer.SetBlendShapeWeight.html)
- Unity公式ドキュメント: [AnimationUtility.GetCurveBindings](https://docs.unity3d.com/ScriptReference/AnimationUtility.GetCurveBindings.html)

---

## 更新履歴

| バージョン | 日付 | 変更内容 |
|------------|------|----------|
| 1.0 | 2026-01-27 | 初版作成 |
