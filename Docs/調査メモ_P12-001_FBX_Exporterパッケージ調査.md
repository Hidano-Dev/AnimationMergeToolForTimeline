# P12-001: FBX Exporterパッケージ調査メモ

**作成日**: 2026-01-27
**対応要件**: FR-080, FR-089, NFR-003, NFR-004, ERR-003

---

## 1. 概要

Unity公式のFBX Exporterパッケージ（com.unity.formats.fbx）のAPI仕様を調査し、本プロジェクトのFBXエクスポート機能実装に必要な情報をまとめる。

---

## 2. パッケージ情報

### 2.1 基本情報

- **パッケージ名**: com.unity.formats.fbx
- **推奨バージョン**: 5.x（開発計画書より）
- **ドキュメント**: https://docs.unity3d.com/Packages/com.unity.formats.fbx@5.0/manual/index.html
- **Unity最小バージョン**: Unity 2020.3以降（5.x系）

### 2.2 主要な名前空間

- `UnityEditor.Formats.Fbx.Exporter` - エクスポート機能の主要API

---

## 3. 主要API

### 3.1 ModelExporter クラス

FBXエクスポートの中心となるクラス。

```csharp
namespace UnityEditor.Formats.Fbx.Exporter
{
    public static class ModelExporter
    {
        /// <summary>
        /// GameObjectをFBXファイルとしてエクスポートする
        /// </summary>
        /// <param name="filePath">出力先ファイルパス（.fbx拡張子）</param>
        /// <param name="singleObject">エクスポート対象のGameObject</param>
        /// <returns>エクスポートされたFBXのAssetPath、失敗時はnull</returns>
        public static string ExportObject(string filePath, UnityEngine.Object singleObject);

        /// <summary>
        /// 複数のGameObjectをFBXファイルとしてエクスポートする
        /// </summary>
        /// <param name="filePath">出力先ファイルパス</param>
        /// <param name="objects">エクスポート対象のオブジェクト配列</param>
        /// <returns>エクスポートされたFBXのAssetPath、失敗時はnull</returns>
        public static string ExportObjects(string filePath, UnityEngine.Object[] objects);

        /// <summary>
        /// エクスポート設定を指定してエクスポートする
        /// </summary>
        /// <param name="filePath">出力先ファイルパス</param>
        /// <param name="singleObject">エクスポート対象のGameObject</param>
        /// <param name="exportOptions">エクスポート設定</param>
        /// <returns>エクスポートされたFBXのAssetPath、失敗時はnull</returns>
        public static string ExportObject(
            string filePath,
            UnityEngine.Object singleObject,
            ExportModelSettingsSerialize exportOptions);
    }
}
```

### 3.2 ExportModelSettingsSerialize クラス

エクスポート設定を保持するクラス。

```csharp
namespace UnityEditor.Formats.Fbx.Exporter
{
    [Serializable]
    public class ExportModelSettingsSerialize
    {
        // ファイル形式
        public ExportFormat ExportFormat { get; set; }  // Binary/ASCII

        // 座標系
        public bool PreserveImportSettings { get; set; }

        // モデル設定
        public bool ExportUnrendered { get; set; }  // レンダラーのないオブジェクトを含める
        public LODExportType LODExportType { get; set; }

        // アニメーション設定
        public bool AnimateSkinnedMesh { get; set; }  // スキンメッシュアニメーションを含める
        public bool AnimationSource { get; set; }  // アニメーションソースを含める
        public bool AnimationDest { get; set; }  // アニメーションの宛先設定

        // BlendShape設定
        public bool ExportBlendShapes { get; set; }  // BlendShapeを含める
    }
}
```

### 3.3 ExportFormat 列挙型

```csharp
public enum ExportFormat
{
    Binary,  // バイナリFBX（推奨、ファイルサイズ小）
    ASCII    // ASCII FBX（デバッグ用、可読性あり）
}
```

---

## 4. エクスポート対象

### 4.1 自動的にエクスポートされるもの

FBX Exporterは以下を自動的にエクスポートする：

1. **メッシュデータ**
   - MeshFilter + MeshRenderer
   - SkinnedMeshRenderer（ボーン含む）

2. **スケルトン（ボーン階層）**
   - Animatorコンポーネントを持つGameObjectの子階層
   - SkinnedMeshRendererのボーン参照

3. **Transform**
   - Position, Rotation, Scale

4. **BlendShape**
   - SkinnedMeshRendererのBlendShape（モーフターゲット）

### 4.2 アニメーションのエクスポート

**重要**: AnimationClipを直接FBXにエクスポートする機能は限定的。

FBX ExporterはGameObjectをエクスポートする際、以下の方法でアニメーションを含められる：

1. **Animation/Animatorコンポーネント経由**
   - GameObjectにAnimatorまたはAnimationコンポーネントがある場合
   - アタッチされたAnimationClipがエクスポートされる

2. **再生中のアニメーション**
   - エディタでアニメーションを再生中にエクスポートすると、現在のポーズが含まれる

### 4.3 アニメーションカーブの直接エクスポート

AnimationClipのカーブを直接FBXにベイクする方法：

```csharp
// 低レベルAPI（Autodesk.Fbx名前空間）を使用
// FBX SDKのラッパー
using Autodesk.Fbx;

// FbxManager, FbxScene, FbxAnimStack, FbxAnimLayer, FbxAnimCurve 等
```

---

## 5. 実装方針

### 5.1 推奨アプローチ

本プロジェクトでは以下のアプローチを採用する：

1. **一時的なGameObject作成**
   - Animatorを持つGameObjectを一時的にインスタンス化
   - マージしたAnimationClipをAnimatorControllerに設定

2. **アニメーション適用**
   - AnimationMode.StartAnimationMode()でアニメーション編集モードに入る
   - AnimationMode.SampleAnimationClip()でアニメーションをサンプリング

3. **FBXエクスポート**
   - ModelExporter.ExportObject()でエクスポート
   - ExportModelSettingsSerializeで設定をカスタマイズ

4. **クリーンアップ**
   - 一時オブジェクトの削除
   - AnimationMode.StopAnimationMode()

### 5.2 代替アプローチ（低レベルAPI）

より細かい制御が必要な場合：

1. **Autodesk.Fbx名前空間の使用**
   - FbxManager.Create()でマネージャー作成
   - FbxScene.Create()でシーン作成
   - FbxNode, FbxSkeleton, FbxMeshでジオメトリ構築
   - FbxAnimStackでアニメーションスタック作成
   - FbxAnimCurveでカーブを個別に設定

この方法は複雑だが、AnimationClipのカーブを直接FBXに書き込める。

### 5.3 本プロジェクトの採用方針

**Phase 12-13**: ModelExporter APIを使用する高レベルアプローチを採用
- 実装の簡素化
- メンテナンス性の向上
- FBX Exporterの更新への追従が容易

**必要に応じて**: 低レベルAPIへの移行を検討
- アニメーションカーブの精密な制御が必要な場合
- BlendShapeアニメーションの出力に問題がある場合

---

## 6. パッケージ存在チェック

### 6.1 パッケージのインストール確認

```csharp
using System.Linq;
using UnityEditor.PackageManager;

public static class FbxPackageChecker
{
    private const string FbxExporterPackageId = "com.unity.formats.fbx";

    /// <summary>
    /// FBX Exporterパッケージがインストールされているかチェック
    /// </summary>
    public static bool IsPackageInstalled()
    {
        // #if UNITY_FORMATS_FBX コンパイルシンボルでのチェック
        #if UNITY_FORMATS_FBX
        return true;
        #else
        return false;
        #endif
    }

    /// <summary>
    /// PackageManager APIを使用した動的チェック（非同期）
    /// </summary>
    public static void CheckPackageAsync(System.Action<bool> callback)
    {
        var request = Client.List(true);
        EditorApplication.update += () =>
        {
            if (request.IsCompleted)
            {
                EditorApplication.update -= CheckComplete;
                var isInstalled = request.Result.Any(p => p.name == FbxExporterPackageId);
                callback?.Invoke(isInstalled);
            }
        };
        void CheckComplete() { }
    }
}
```

### 6.2 コンパイルシンボルの利用

FBX Exporterパッケージがインストールされると、`UNITY_FORMATS_FBX`シンボルが定義される。
これを使用して条件付きコンパイルを行う：

```csharp
#if UNITY_FORMATS_FBX
using UnityEditor.Formats.Fbx.Exporter;
#endif

public class FbxAnimationExporter
{
    public bool Export(string path, GameObject target)
    {
        #if UNITY_FORMATS_FBX
        return ModelExporter.ExportObject(path, target) != null;
        #else
        Debug.LogError("FBX Exporter package is not installed.");
        return false;
        #endif
    }
}
```

---

## 7. エラーハンドリング

### 7.1 ERR-003対応

```csharp
/// <summary>
/// FBX Exporterパッケージ未インストール時のエラーダイアログ表示
/// </summary>
public static void ShowPackageNotInstalledError()
{
    EditorUtility.DisplayDialog(
        "FBX Exporter Required",
        "FBX出力機能を使用するには、FBX Exporterパッケージ（com.unity.formats.fbx）の" +
        "インストールが必要です。\n\n" +
        "Package Managerから「FBX Exporter」を検索してインストールしてください。",
        "OK"
    );
}
```

---

## 8. 制限事項・注意点

### 8.1 既知の制限

1. **Humanoidリグのエクスポート**
   - HumanoidリグはGeneric形式に変換される
   - マッスルカーブは直接エクスポートできない（Phase 14で対応）

2. **BlendShapeアニメーション**
   - メッシュ情報と共にエクスポートが必要
   - カーブのみの出力は不可

3. **FBXフォーマットバージョン**
   - デフォルトはFBX 2019形式
   - 古いエンジンとの互換性に注意

### 8.2 他エンジンとの互換性

- **Unreal Engine**: FBX 2019形式をサポート、インポート時にスケール調整が必要な場合あり
- **Godot**: FBX形式のサポートは限定的（GLTFを推奨する場合あり）

---

## 9. 参考資料

### 9.1 公式ドキュメント

- [Unity FBX Exporter Manual](https://docs.unity3d.com/Packages/com.unity.formats.fbx@5.0/manual/index.html)
- [FBX Exporter API Reference](https://docs.unity3d.com/Packages/com.unity.formats.fbx@5.0/api/index.html)
- [Autodesk FBX SDK](https://www.autodesk.com/developer-network/platform-technologies/fbx-sdk-2020-0)

### 9.2 関連するUnity API

- [AnimationMode](https://docs.unity3d.com/ScriptReference/AnimationMode.html)
- [AnimationUtility](https://docs.unity3d.com/ScriptReference/AnimationUtility.html)
- [PackageManager.Client](https://docs.unity3d.com/ScriptReference/PackageManager.Client.html)

---

## 10. 次のステップ

1. **P12-002**: FbxExportDataクラス設計
   - 本調査結果を元に、エクスポートに必要なデータ構造を設計

2. **P12-004**: パッケージ存在チェック機能のテスト作成
   - セクション6の内容を元にテストを作成

3. **P12-006**: FbxAnimationExporter基本実装のテスト作成
   - セクション3-5の内容を元にテストを作成

---

## 更新履歴

| バージョン | 日付 | 変更内容 |
|------------|------|----------|
| 1.0 | 2026-01-27 | 初版作成 |
