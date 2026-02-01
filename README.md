# Animation Merge Tool for Timeline

[![Unity 6000.0.64f1](https://img.shields.io/badge/Unity-6000.0.64f1-blue.svg)](https://unity.com/)
[![Version](https://img.shields.io/badge/Version-1.0.0-green.svg)](./AnimationMergeToolForTimeline/Packages/AnimationMergeToolForTimeline/package.json)

UnityのTimeline上で複数トラックにまたがり構成されたAnimationTrackの内容をマージし、単一のAnimationClipアセットとして出力するエディタ拡張ツールです。

Timelineを再生したときと同じ状態のアニメーションを単一のAnimationClipに統合できます。

---

## 機能

- **複数トラックのマージ**: Timeline上の複数のAnimationTrackを1つのAnimationClipに統合
- **Override動作の再現**: 下のトラックが上のトラックをオーバーライドする動作を正確に再現
- **Extrapolation対応**: None/Hold/Loop/PingPong/Continueの各設定に対応
- **ブレンド処理**: クリップのEaseIn/EaseOut（ブレンド）を正しく処理
- **複数Animator対応**: 1つのTimelineに複数のAnimatorがある場合、それぞれ別のAnimationClipを出力
- **特殊プロパティ対応**: ルートモーション、Humanoidマッスルカーブに対応

---

## 動作環境

- **Unity**: 6000.0.64f1 以降
- **依存パッケージ**: com.unity.timeline 1.8.10 以降

---

## インストール方法

### 方法1: npm レジストリ経由でインストール（推奨）

プロジェクトの `Packages/manifest.json` に以下のScoped Registryを追加し、`dependencies` にパッケージを追加してください:

```json
{
  "scopedRegistries": [
    {
      "name": "Hidano",
      "url": "https://registry.npmjs.com",
      "scopes": [
        "com.hidano"
      ]
    }
  ],
  "dependencies": {
    "com.hidano.animation-merge-tool": "1.0.0"
  }
}
```

### 方法2: Git URL経由でインストール

1. Unity Package Managerを開く（Window > Package Manager）
2. 左上の「+」ボタンをクリック
3. 「Add package from git URL...」を選択
4. 以下のURLを入力:

```
https://github.com/hidano/AnimationMergeToolForTimeline.git?path=AnimationMergeToolForTimeline/Packages/AnimationMergeToolForTimeline
```

### 方法3: パッケージフォルダを直接配置

`Packages/AnimationMergeToolForTimeline` フォルダをプロジェクトの `Packages` ディレクトリに配置してください。

---

## 使用方法

### 起動方法1: Hierarchyビューから実行

1. Hierarchyビューで、PlayableDirectorがアタッチされたGameObjectを選択
2. 右クリックでコンテキストメニューを開く
3. **「Animation Merge Tool」>「Merge Timeline Animations」** を選択

### 起動方法2: Projectビューから実行

1. ProjectビューでTimelineAssetファイル（.playable）を選択
2. 右クリックでコンテキストメニューを開く
3. **「Animation Merge Tool」>「Merge Timeline Animations」** を選択

### 複数選択

複数のPlayableDirectorまたはTimelineAssetを選択した状態で実行すると、選択された各アセットに対して順次処理を実行します。

---

## FBXエクスポート機能

Timeline上のアニメーションをFBX形式でエクスポートし、他のゲームエンジン（Unreal Engine、Godot等）やDCCツール（Blender等）で利用できます。

### 前提条件

FBXエクスポートには **com.unity.formats.fbx** (FBX Exporter) パッケージが必要です。

Package Managerから「Add package by name...」で `com.unity.formats.fbx` を追加してください。

### FBXエクスポートの実行

AnimationClip出力と同様の操作で、コンテキストメニューから **「Animation Merge Tool」>「Export as FBX」** を選択します。

### FBX出力仕様

| 項目 | 内容 |
|------|------|
| ファイル形式 | FBX Binary (.fbx) |
| ファイル名 | `{TimelineAsset名}_{Animator名}_Merged.fbx` |
| 保存先 | Assets フォルダ直下 |
| リグタイプ | Generic形式固定 |

FBXファイルには以下のデータが含まれます：

- スケルトン（AnimatorにバインドされたBone階層）
- Transformアニメーション（Position/Rotation/Scale）
- BlendShapeアニメーション（モーフターゲット）
- ルートモーション

Humanoidリグの場合、マッスルカーブはTransformの回転カーブに自動変換されます。

### 他エンジンへのインポート

**Unreal Engine**: Content Browserで右クリック → Import → FBXファイルを選択 → Import Animationsにチェック

**Godot**: プロジェクトフォルダにFBXファイルを配置 → Importタブで Animation > Import Animations を有効化

**Blender**: File → Import → FBX (.fbx) → Animation チェックを有効化

### AnimationClip出力との違い

| 機能 | AnimationClip出力 | FBXエクスポート |
|-----|------------------|----------------|
| 出力形式 | .anim（Unity専用） | .fbx（汎用） |
| 他エンジン互換 | なし | あり |
| 必須パッケージ | なし | FBX Exporter |
| リグタイプ | 元のまま | Generic固定 |
| メニュー | Merge Timeline Animations | Export as FBX |

---

## 出力仕様（AnimationClip）

### 出力ファイル

| 項目 | 内容 |
|------|------|
| ファイル形式 | AnimationClip (.anim) |
| ファイル名 | `{TimelineAsset名}_{Animator名}_Merged.anim` |
| 保存先 | Assets フォルダ直下 |

### 複数Animator対応

1つのTimelineAssetに複数のAnimatorにバインドされたAnimationTrackが含まれている場合、バインドされているAnimator単位でそれぞれ別のAnimationClipを出力します。

### 重複ファイル名

同名ファイルが既に存在する場合、上書きせず「(1)」「(2)」等の連番を付与した重複しない名前で保存します。

例: `MyTimeline_Character_Merged(1).anim`

---

## 対応機能

### トラック処理

| 機能 | 説明 |
|------|------|
| AnimationTrack | 統合対象として処理 |
| OverrideTrack | 親トラックのバインド情報を継承して処理 |
| Muteトラック | 処理対象から除外 |
| バインドなしトラック | スキップ（エラーログを出力） |

### Override動作

Timeline上で下にあるトラックほど優先順位が高くなります。同一のAnimationPropertyが複数トラックに存在する場合、下の段のAnimationCurveが上の段をOverrideします。

### Extrapolation対応

クリップ範囲外の動作は、各クリップのAnimationExtrapolation設定に従って処理されます。

| 設定 | 動作 |
|------|------|
| None | クリップ範囲外は値を出力しない |
| Hold | 最初/最後のキーフレームの値を維持 |
| Loop | クリップの長さでループ |
| PingPong | クリップの長さで往復 |
| Continue | 最初/最後のキーフレームの接線を延長 |

### ブレンド処理

クリップのEaseIn/EaseOut（ブレンド）設定が反映されます。各クリップのBlendCurvesのIn/Outを使用してブレンド処理を行います。

### 対応プロパティ

- 通常のTransform/Animatorプロパティ
- ルートモーション (RootT, RootQ)
- Humanoidリグのマッスルカーブ

---

## 処理結果の確認

### 進捗表示

処理中はプログレスバーが表示されます。

### Console出力

| 結果 | 出力 |
|------|------|
| 成功 | Debug.Logで出力ファイルパスを表示 |
| 失敗 | Debug.LogErrorでエラー内容を表示 |

---

## 制限事項

- マージ対象のトラックを選択する機能は提供していません（すべての有効なトラックが対象）
- 時間範囲を指定してマージする機能は提供していません
- 保存先フォルダは Assetsフォルダ直下に固定です
- FBXエクスポートはGeneric形式のみ（Humanoid形式での出力は不可）
- FBXエクスポートにはcom.unity.formats.fbxパッケージが必要です

---

## トラブルシューティング

### 「対象となるトラックがありません」エラー

以下を確認してください：
- TimelineAssetにAnimationTrackが存在するか
- トラックにAnimatorがバインドされているか
- すべてのトラックがMuteになっていないか

### 「バインドされていないトラックがあります」警告

Animatorがバインドされていないトラックは処理対象外としてスキップされます。必要な場合は、PlayableDirector上でトラックにAnimatorをバインドしてください。

---

## ドキュメント

- [要件定義書](./AnimationMergeToolForTimeline/Packages/AnimationMergeToolForTimeline/Docs/要件定義書.md) - 正式な要件定義書

---

## ライセンス

MIT License

---

## 更新履歴

| バージョン | 日付 | 変更内容 |
|------------|------|----------|
| 1.0.0 | 2026-02-01 | 正式リリース（FBXエクスポート機能追加、各種バグ修正） |
| 0.1.0-preview.1 | 2026-01-27 | プレビュー版リリース |
