# P15-001: FBX BlendShapeエクスポート仕様調査メモ

**作成日**: 2026-01-27
**対応要件**: FR-084, ERR-004

---

## 1. 概要

Unity FBX Exporterを使用したBlendShape（モーフターゲット）アニメーションのエクスポート仕様を調査し、本プロジェクトのFBXエクスポート機能にBlendShapeアニメーションを組み込むために必要な情報をまとめる。

---

## 2. BlendShapeの基本概念

### 2.1 UnityにおけるBlendShape

- **BlendShape**（別名: モーフターゲット、ブレンドシェイプ）はメッシュの頂点位置を変形させる機能
- SkinnedMeshRendererコンポーネントが持つMeshのBlendShape情報を参照
- 各BlendShapeは0〜100（Unityでは0.0〜100.0）の重み値で制御
- 主に表情アニメーション、衣服の変形、筋肉の膨らみ等に使用

### 2.2 AnimationClipにおけるBlendShapeカーブ

BlendShapeアニメーションは以下の形式でAnimationClipに格納される：

```
プロパティパス: blendShape.{BlendShapeName}
型: float
値の範囲: 0.0 〜 100.0
```

例：
- `blendShape.smile` - 笑顔のBlendShape
- `blendShape.blink_L` - 左目の瞬き
- `blendShape.blink_R` - 右目の瞬き

---

## 3. FBX ExporterのBlendShapeエクスポート機能

### 3.1 ExportModelSettingsSerializeの設定

P12-001調査で確認したとおり、`ExportModelSettingsSerialize`クラスには以下の設定がある：

```csharp
public bool ExportBlendShapes { get; set; }  // BlendShapeを含める
```

この設定を`true`にすることで、BlendShapeジオメトリがFBXに出力される。

### 3.2 BlendShapeジオメトリのエクスポート

FBX Exporterは`SkinnedMeshRenderer`をエクスポートする際、メッシュに含まれるBlendShapeターゲットを自動的にエクスポートする。

エクスポートされる情報：
1. **ベースメッシュ** - BlendShape適用前の基本形状
2. **各BlendShapeターゲット** - 頂点のオフセット情報
3. **BlendShape名** - Unity上での名前がそのまま使用される

### 3.3 BlendShapeアニメーションのエクスポート

**重要**: FBX ExporterはBlendShapeの**現在の重み値（ポーズ）**をエクスポートできるが、**アニメーションカーブ**の直接エクスポートは標準APIでは限定的。

#### 3.3.1 高レベルAPI（ModelExporter）での制限

`ModelExporter.ExportObject()`を使用する場合：

- BlendShapeターゲットのジオメトリはエクスポートされる
- エクスポート時点のBlendShape重み値は反映される
- しかし、AnimationClip内のBlendShapeカーブは自動的にはFBXアニメーションに変換されない

#### 3.3.2 低レベルAPI（Autodesk.Fbx）でのアプローチ

BlendShapeアニメーションカーブをFBXに書き込むには、`Autodesk.Fbx`名前空間のAPIを使用する必要がある：

```csharp
using Autodesk.Fbx;

// FbxBlendShapeChannel にアニメーションカーブをバインド
FbxAnimCurve curve = FbxAnimCurve.Create(scene, "blendShapeCurve");
FbxProperty property = blendShapeChannel.DeformPercent;
property.ConnectSrcObject(curve);

// キーフレームの追加
for (int i = 0; i < keyframes.Length; i++)
{
    FbxTime time = new FbxTime();
    time.SetSecondDouble(keyframes[i].time);
    int keyIndex = curve.KeyAdd(time);
    curve.KeySet(keyIndex, time, keyframes[i].value);
}
```

---

## 4. 本プロジェクトでの実装方針

### 4.1 採用するアプローチ

**Phase 13で確立したアプローチを拡張**:

1. AnimationModeでアニメーションをサンプリングしながらFBXをエクスポート
2. BlendShapeの重み変化もサンプリングに含める
3. タイムベースでベイクしたアニメーションをFBXに出力

### 4.2 BlendShapeカーブの検出

AnimationClipからBlendShapeカーブを検出する方法：

```csharp
/// <summary>
/// AnimationClipからBlendShapeカーブを検出する
/// </summary>
public static List<EditorCurveBinding> GetBlendShapeBindings(AnimationClip clip)
{
    var bindings = AnimationUtility.GetCurveBindings(clip);
    return bindings
        .Where(b => b.propertyName.StartsWith("blendShape."))
        .ToList();
}
```

### 4.3 BlendShapeカーブの特性

| 項目 | 値 |
|------|-----|
| プロパティプレフィックス | `blendShape.` |
| 型 | `typeof(SkinnedMeshRenderer)` |
| 値の範囲 | 0.0 〜 100.0（通常） |
| パスの形式 | `{SkinnedMeshRendererへのパス}` |

### 4.4 検出ロジックの要件

1. `propertyName`が`blendShape.`で始まるかを判定
2. バインディングの`type`が`SkinnedMeshRenderer`であることを確認
3. 対応するSkinnedMeshRendererがエクスポート対象に含まれることを確認

---

## 5. FBXエクスポート時のBlendShapeアニメーション出力

### 5.1 出力フロー

```
1. MergedAnimationClipからBlendShapeカーブを抽出
2. 対象のSkinnedMeshRendererを特定
3. AnimationMode.SampleAnimationClip()でBlendShape重みを適用
4. FbxAnimCurveを作成してキーフレームを設定
5. FBXファイルに書き込み
```

### 5.2 SkinnedMeshRendererとの関連付け

BlendShapeカーブはSkinnedMeshRendererにバインドされるため、エクスポート時には：

1. カーブのパスからSkinnedMeshRendererを解決
2. SkinnedMeshRendererが持つBlendShape名と照合
3. 対応するFbxBlendShapeChannelにカーブをバインド

### 5.3 値の正規化

- **Unity**: BlendShape重みは0〜100の範囲
- **FBX**: 0〜100の範囲（同一、変換不要）

---

## 6. エラーハンドリング

### 6.1 ERR-004: エクスポート可能なデータがない場合

以下の条件でエラーダイアログを表示（要件ERR-004）：

1. マージ結果のAnimationClipにカーブが存在しない
2. Transform/BlendShapeのいずれのカーブもない
3. エクスポート対象のGameObjectにSkinnedMeshRenderer/Animator等がない

```csharp
/// <summary>
/// エクスポート可能なデータがない場合のエラーダイアログ表示
/// </summary>
public static void ShowNoExportableDataError()
{
    EditorUtility.DisplayDialog(
        "Export Error",
        "エクスポート可能なアニメーションデータがありません。\n\n" +
        "以下を確認してください：\n" +
        "・選択したトラックにAnimationClipが含まれている\n" +
        "・AnimationClipにTransformまたはBlendShapeカーブが存在する\n" +
        "・バインドターゲットに対応するコンポーネントが存在する",
        "OK"
    );
}
```

### 6.2 BlendShapeターゲットが見つからない場合

AnimationClipのBlendShapeカーブが参照するターゲットが、エクスポート対象に存在しない場合：

- 警告ログを出力
- 該当カーブをスキップしてエクスポートを継続
- ユーザーに結果を通知

---

## 7. テスト要件

### 7.1 BlendShapeカーブ検出テスト

```csharp
[Test]
public void DetectBlendShapeCurves_WithBlendShapeAnimation_ReturnsCorrectBindings()
{
    // Arrange: BlendShapeカーブを含むAnimationClipを準備

    // Act: 検出メソッドを実行

    // Assert: blendShape.プレフィックスを持つバインディングが返される
}
```

### 7.2 BlendShapeカーブFBX出力テスト

```csharp
[Test]
public void ExportToFbx_WithBlendShapeAnimation_IncludesBlendShapeCurves()
{
    // Arrange: BlendShapeアニメーションを持つクリップを準備

    // Act: FBXエクスポートを実行

    // Assert: 出力されたFBXにBlendShapeアニメーションが含まれる
}
```

---

## 8. 実装上の注意点

### 8.1 パフォーマンス考慮

- BlendShapeカーブは頂点数が多いメッシュで大量になる可能性がある
- 表情アニメーションでは数十〜数百のBlendShapeを持つことがある
- カーブ数が多い場合、エクスポート時間に影響

### 8.2 互換性

- **Maya/3ds Max**: FBX BlendShapeアニメーションを正しくインポート可能
- **Unreal Engine**: MorphTargetとして認識される
- **Blender**: Shape Keysとしてインポートされる

### 8.3 命名規則

- Unity上のBlendShape名がそのままFBXに出力される
- 特殊文字（スペース、日本語等）は問題になる可能性がある
- ASCII文字での命名を推奨

---

## 9. 関連するAPI・クラス

### 9.1 Unity API

| クラス/メソッド | 用途 |
|----------------|------|
| `AnimationUtility.GetCurveBindings()` | AnimationClipからカーブバインディング取得 |
| `SkinnedMeshRenderer.GetBlendShapeIndex()` | BlendShape名からインデックス取得 |
| `SkinnedMeshRenderer.SetBlendShapeWeight()` | BlendShape重み設定 |
| `Mesh.blendShapeCount` | BlendShape数取得 |
| `Mesh.GetBlendShapeName()` | BlendShape名取得 |

### 9.2 FBX SDK API（Autodesk.Fbx）

| クラス | 用途 |
|--------|------|
| `FbxBlendShape` | BlendShapeデフォーマー |
| `FbxBlendShapeChannel` | 個別のBlendShapeチャンネル |
| `FbxShape` | BlendShapeターゲット形状 |
| `FbxAnimCurve` | アニメーションカーブ |

---

## 10. 次のステップ

1. **P15-002**: BlendShapeカーブ検出（FBX用）のテスト作成
   - 本調査のセクション4, 7を元にテストを作成

2. **P15-003**: BlendShapeカーブ検出（FBX用）の実装
   - FbxAnimationExporterにBlendShapeカーブ検出機能を追加

3. **P15-004**: BlendShapeカーブFBX出力のテスト作成
   - セクション5の出力フローに基づくテスト

4. **P15-005**: BlendShapeカーブFBX出力の実装
   - BlendShapeアニメーションをFBXに出力する機能の実装

---

## 更新履歴

| バージョン | 日付 | 変更内容 |
|------------|------|----------|
| 1.0 | 2026-01-27 | 初版作成 |
