# FBXエクスポート機能 使用方法

**文書バージョン**: 1.0
**作成日**: 2026-01-27
**対象ツールバージョン**: Animation Merge Tool for Timeline

---

## 概要

本ドキュメントでは、Animation Merge Tool for TimelineのFBXエクスポート機能の使用方法について説明します。

FBXエクスポート機能は、Timeline上で構成された複数のAnimationTrackをマージし、他のゲームエンジン（Unreal Engine、Godot等）で利用可能なFBX形式で出力する機能です。

---

## 前提条件

### 必須パッケージ

FBXエクスポート機能を使用するには、以下のUnity公式パッケージが必要です：

- **com.unity.formats.fbx** (FBX Exporter) バージョン5.x以降

### パッケージのインストール方法

1. Unity EditorでWindow → Package Managerを開く
2. 左上の「+」ボタンをクリック
3. 「Add package by name...」を選択
4. パッケージ名に `com.unity.formats.fbx` を入力
5. 「Add」をクリック

※パッケージがインストールされていない状態でFBXエクスポートを実行すると、エラーダイアログが表示されます。

---

## 使用方法

### 方法1: Hierarchyビューから実行

1. Hierarchyビューで**PlayableDirector**コンポーネントを持つGameObjectを選択
2. 右クリックしてコンテキストメニューを開く
3. **「Animation Merge Tool」** → **「Export as FBX」** を選択

### 方法2: Projectビューから実行

1. Projectビューで**TimelineAsset**（.playableファイル）を選択
2. 右クリックしてコンテキストメニューを開く
3. **「Animation Merge Tool」** → **「Export as FBX」** を選択

### 複数選択

複数のPlayableDirectorまたはTimelineAssetを選択して実行することで、一括でFBXエクスポートを行うことができます。

---

## 出力仕様

### ファイル名

出力されるFBXファイルの命名規則：

```
{TimelineAsset名}_{Animator名}_Merged.fbx
```

例: `CutsceneTimeline_Character_Merged.fbx`

### 保存先

- FBXファイルは**Assets**フォルダ直下に保存されます
- 同名ファイルが存在する場合、`(1)`、`(2)`等の連番が付与されます
  - 例: `CutsceneTimeline_Character_Merged(1).fbx`

### 出力内容

FBXファイルには以下のデータが含まれます：

| データ種別 | 説明 |
|-----------|------|
| スケルトン | AnimatorにバインドされたBone階層 |
| Transformアニメーション | Position/Rotation/Scaleのカーブ |
| BlendShapeアニメーション | モーフターゲットのカーブ |
| ルートモーション | ルートボーンのアニメーション |

### リグタイプ

- FBX出力は常に**Generic形式**で出力されます
- Humanoidリグの場合、マッスルカーブはTransformの回転カーブに変換されます

---

## エクスポート対象

### 対象となるアニメーション

- Timeline上の全AnimationTrackのクリップがマージされます
- Muteに設定されているトラックは除外されます
- Override処理（下のトラックほど優先）が適用されます
- EaseIn/EaseOutのブレンドが反映されます
- AnimationExtrapolation設定に従った補間が行われます

### 対象とならないもの

- バインドされていないトラック（Animator未設定）
- Muteに設定されているトラック
- エクスポート可能なデータ（スケルトン、BlendShape、Transformアニメーション）がないAnimator

---

## Humanoidアニメーションの変換

Humanoidリグを使用しているAnimatorの場合、以下の変換が行われます：

| 変換前 | 変換後 |
|-------|-------|
| マッスルカーブ | Transformの回転カーブ |
| Humanoidボーン名 | 実際のTransformパス |
| ルートモーション | Hipsボーンの位置/回転カーブ |

これにより、他のゲームエンジンでも正しくアニメーションを再生できます。

---

## エラーと対処法

### ERR-003: FBX Exporterパッケージ未インストール

**症状**: 「FBX Exporterパッケージがインストールされていません」というエラーダイアログが表示される

**対処法**: Package Managerから `com.unity.formats.fbx` パッケージをインストールしてください

### ERR-004: エクスポート可能なデータがない

**症状**: 「エクスポート可能なデータがありません」というエラーログが出力される

**対処法**:
- Animatorが正しくバインドされているか確認
- AnimationClipにTransformまたはBlendShapeのカーブが含まれているか確認

### その他のエラー

**バインドされていないトラックが存在する場合**:
- エラーログが出力され、該当トラックはスキップされます
- 他のトラックの処理は継続されます

---

## 制約事項

| 項目 | 内容 |
|-----|------|
| 保存先 | Assetsフォルダ直下固定（変更不可） |
| リグタイプ | Generic形式のみ（Humanoid出力は不可） |
| 対象 | スケルトン、BlendShape、またはTransformアニメーションのいずれかを持つAnimatorのみ |

---

## 他ゲームエンジンへのインポート

### Unreal Engine

1. Content Browserで右クリック → Import
2. FBXファイルを選択
3. Import Animationsにチェックを入れる
4. Importをクリック

### Godot

1. プロジェクトフォルダにFBXファイルを配置
2. Importタブでファイルを選択
3. Animation → Import Animationsを有効化
4. Reimportをクリック

---

## AnimationClip出力との違い

| 機能 | AnimationClip出力 | FBXエクスポート |
|-----|------------------|----------------|
| 出力形式 | .anim（Unity専用） | .fbx（汎用） |
| 他エンジン互換 | なし | あり |
| 必須パッケージ | なし | FBX Exporter |
| リグタイプ | 元のまま | Generic固定 |
| メニュー | Merge Timeline Animations | Export as FBX |

---

## 更新履歴

| バージョン | 日付 | 変更内容 |
|------------|------|----------|
| 1.0 | 2026-01-27 | 初版作成 |
