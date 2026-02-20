# Changelog

このパッケージに対するすべての変更はこのファイルに記録されます。

形式は [Keep a Changelog](https://keepachangelog.com/ja/1.0.0/) に基づいており、
このプロジェクトは [Semantic Versioning](https://semver.org/spec/v2.0.0.html) に準拠しています。

## [1.3.0] - 2026-02-20

### Added

- **フレームレートリサンプリング機能**
  - マージ後のキーフレームをTimelineで指定したフレームレート（例: 30fps）の間隔にリサンプリング
  - `AnimationCurve.Evaluate()`を使用してリサンプリング時の値を正確に補間
  - 始端・終端のキーフレーム時間をフレーム境界にスナップ（始端は切り捨て、終端は切り上げ）
  - null/空カーブ/不正なフレームレートは安全にスキップ
  - `CurveResampler`クラスを新規追加

## [1.2.0] - 2026-02-20

### Fixed

- Extrapolation=None設定時に、同一トラック内のクリップ間Gap区間で上位レイヤーの値が反映されない不具合を修正
  - Gap区間で下位レイヤーのクリップ最終フレーム値がHoldされ、上位レイヤーの値が見えなくなる問題
  - トラックの有効範囲を「クリップごとのアクティブ区間リスト」として管理するアプローチで修正
  - CurveOverriderがGap区間では上位レイヤーの値を保持するよう改修

### Added

- `ActiveInterval`構造体を新規追加（トラック内のクリップ有効区間を表現）
- `CurveOverrider.ApplyPartialOverrideWithActiveIntervals()`メソッドを新規追加
- `CurveWithTimeRange`にアクティブ区間対応コンストラクタを追加

## [1.1.3] - 2026-02-10

### Fixed

- FBXエクスポート時にマージ済みクリップのRootT/RootQがHips位置・回転に反映されない不具合を修正
  - マージ済みクリップは`isHumanMotion=false`のため、`SampleAnimation`がRootT/RootQを`Animator.transform`に適用しない問題
  - サンプリングループ内でRootT/RootQカーブを手動評価して`Animator.transform`に設定するよう修正
  - Humanoidクリップの場合は従来通り`SampleAnimation`に委譲し、二重適用を防止
- GenericリグモデルのFBXエクスポート時にルートモーションのPosition（移動）が反映されない不具合を修正
  - `FindRootBonePathFromCurves`がPositionカーブのみを検索していたため、Positionカーブを持たないGenericリグでルートボーンパスが特定できなかった
  - Positionカーブが見つからない場合にRotationカーブからもルートボーンパスを検索するよう修正
- GenericリグモデルでAnimationPlayableAssetのシーンオフセット（Position/Rotation）がFBX出力に反映されない不具合を修正
  - `SceneOffsetApplier.FindRootCurve`がpath=""のカーブのみを検索していたため、ルートボーンが非空パス（例: "fbxJnt_grp/J_C_hip"）にあるGenericリグではオフセットが適用されなかった
  - path=""が見つからない場合に最も浅いパスのカーブにフォールバックするよう修正

## [1.1.2] - 2026-02-09

### Fixed

- FBXエクスポート時にシーンオフセットのPosition値がルートモーションに反映されず、Hipsボーンに不正に合成される不具合を修正
  - HumanoidToGenericConverter.ConvertMuscleCurvesToRotation()のリファレンス行列取得方法を変更
  - SampleAnimation(T=0)後の状態ではなく、Animator原点状態でリファレンスを取得するよう修正
  - これにより、RootTカーブに加算されたシーンオフセットがFBX出力に正しく保持される

## [1.1.1] - 2026-02-09

### Fixed

- AnimatorのGameObjectをHierarchy上で親オブジェクトから移動・回転させた際のオフセットがマージ結果に反映されない不具合を修正
  - AnimatorのTransform（localPosition/localRotation）をルートカーブに適用する処理を追加
  - Transform（m_LocalPosition/m_LocalRotation）とHumanoidルートモーション（RootT/RootQ）の両方に対応

## [1.1.0] - 2026-02-09

### Added

- **シーンオフセット反映機能**
  - AnimationPlayableAssetのPosition/Rotationオフセットをマージ結果に反映
  - Hierarchy上で手動変更したオフセットが書き出しモーションに正しく適用される
  - Transform（m_LocalPosition/m_LocalRotation）とHumanoidルートモーション（RootT/RootQ）の両方に対応
  - SceneOffsetApplierクラスを新規追加

- **ファイル名への親Animator階層反映**
  - 子AnimatorがHierarchy内で親Animatorを持つ場合、ファイル名が「親Animator名_子Animator名」形式になる
  - 親Animatorがない場合は従来通りのAnimator名を使用
  - .anim出力・FBX出力の両方に適用

### Changed

- ClipInfoにSceneOffsetPosition/SceneOffsetRotationプロパティを追加
- ClipMerger.Merge()内で時間オフセット適用後にシーンオフセットを適用するよう変更
- FileNameGeneratorにGetHierarchicalAnimatorName()静的メソッドを追加

## [1.0.0] - 2026-02-01

### Added

- **FBXエクスポート機能**
  - コンテキストメニューから「Export as FBX」で実行可能
  - スケルトン（ボーン階層）をFBXに含めて出力
  - Transformアニメーション（Position/Rotation/Scale）の出力
  - BlendShape（モーフターゲット）アニメーションの出力
  - ルートモーションの出力
  - Humanoidリグのマッスルカーブを自動的にTransform回転カーブに変換して出力
  - Generic形式での出力（他エンジン・DCCツールとの互換性を確保）
  - FBX Exporterパッケージ未インストール時のエラーダイアログ表示
  - 同名FBXファイル存在時の連番付与

- **BlendShapeアニメーション対応**
  - AnimationClip出力・FBXエクスポートの両方でBlendShapeカーブに対応

- **パス補正機能**
  - AnimationClip内のプロパティパスの不一致を自動補正

### Fixed

- ループのExtrapolation処理の不具合を修正
- OverrideTrackのマージ処理の不具合を修正
- FBXにメッシュが含まれない不具合を修正
- Materialの順番が変わる不具合を修正
- タンジェント（接線）のエラーを修正
- 特定条件下でクラッシュする場合がある不具合を修正
- HumanoidモーションのFBX出力時の挙動を修正
- Humanoid時にルートのTransformがおかしくなる不具合を修正
- HumanoidをFBXにした際にHipsの動きが極端に小さくなる不具合を修正

### Changed

- パッケージバージョンを1.0.0に更新
- 依存パッケージにcom.unity.formats.fbx 5.1.1を追加

### 制約事項

- マージ対象のトラック選択機能は未実装
- 時間範囲指定によるマージ機能は未実装
- 保存先はAssetsフォルダ直下に固定
- FBXエクスポートはGeneric形式のみ（Humanoid形式での出力は不可）
- FBXエクスポートにはcom.unity.formats.fbxパッケージが必要

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
