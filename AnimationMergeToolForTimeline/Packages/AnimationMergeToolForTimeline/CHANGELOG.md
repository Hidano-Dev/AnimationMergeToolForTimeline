# Changelog

このパッケージに対するすべての変更はこのファイルに記録されます。

形式は [Keep a Changelog](https://keepachangelog.com/ja/1.0.0/) に基づいており、
このプロジェクトは [Semantic Versioning](https://semver.org/spec/v2.0.0.html) に準拠しています。

## [0.1.0-preview.1] - 2026-01-26

### Added

- **コンテキストメニューからの実行機能**
  - HierarchyビューでPlayableDirectorを右クリックして実行可能
  - ProjectビューでTimelineAssetを右クリックして実行可能
  - 複数選択に対応

- **トラック検出機能**
  - AnimationTrackおよびOverrideTrackの検出
  - Muteトラックの自動除外
  - バインドなしトラックのスキップとエラーログ出力

- **優先順位決定機能**
  - Timeline上の位置に基づく優先順位計算
  - TrackGroup内の階層構造対応

- **クリップ統合処理**
  - 複数AnimationClipの時間オフセット適用
  - ClipIn（トリミング）対応
  - TimeScale対応
  - フレームレート設定（Timeline設定に準拠）

- **Extrapolation処理**
  - None、Hold、Loop、PingPong、Continue対応
  - クリップ間Gap区間の補間処理

- **ブレンド処理**
  - EaseIn/EaseOut対応
  - BlendCurvesによるブレンド処理
  - 連続クリップ間のブレンド処理

- **Override処理**
  - 同一プロパティの検出とOverride
  - 完全重なり・部分的重なりの両方に対応

- **出力機能**
  - Animator単位で別々のAnimationClipを出力
  - ファイル名形式: `{TimelineAsset名}_{Animator名}_Merged.anim`
  - 重複ファイル名の自動連番付与

- **特殊プロパティ対応**
  - ルートモーションプロパティ対応
  - Humanoidマッスルカーブ対応

- **UI/UX**
  - 処理中のプログレス表示
  - 処理結果のConsole出力

### 制約事項

- マージ対象のトラック選択機能は未実装
- 時間範囲指定によるマージ機能は未実装
- 保存先はAssetsフォルダ直下に固定
