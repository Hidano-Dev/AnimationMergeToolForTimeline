# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

Unity 6 (6000.0.64f1) で構築された、タイムラインシステム向けのアニメーション結合ツール。

## 開発環境

- **Unity バージョン**: 6000.0.64f1
- **C# バージョン**: 9.0
- **.NET Framework**: 4.7.1
- **推奨IDE**: JetBrains Rider（com.unity.ide.rider パッケージ導入済み）

## プロジェクト構造

```
AnimationMergeToolForTimeline/
├── Assets/                    # ゲームアセット・スクリプト
├── Packages/                  # Unity パッケージ依存関係
├── ProjectSettings/           # Unity プロジェクト設定
├── Assembly-CSharp.csproj     # ランタイムアセンブリ
└── Assembly-CSharp-Editor.csproj  # エディタ拡張アセンブリ
```

## ビルドとテスト

### Unity エディタでの操作
- **プロジェクトを開く**: Unity Hub から `AnimationMergeToolForTimeline` フォルダを開く
- **テスト実行**: Unity エディタ → Window → General → Test Runner

### コマンドラインビルド
```bash
# Unity をコマンドラインから実行（パスは環境に応じて調整）
Unity.exe -batchmode -projectPath ./AnimationMergeToolForTimeline -executeMethod BuildScript.Build -quit
```

## 主要依存パッケージ

- **com.unity.timeline**: 1.8.10 - アニメーションタイムライン機能の中核
- **com.unity.test-framework**: 1.6.0 - ユニットテスト

## アセンブリ構成

| アセンブリ | 用途 |
|-----------|------|
| Assembly-CSharp | ランタイムスクリプト（ゲーム実行時に動作） |
| Assembly-CSharp-Editor | エディタ拡張スクリプト（Unityエディタ内でのみ動作） |

## コーディング規約

- エディタ専用コードは `Editor` フォルダ配下に配置
- タイムライン関連の拡張は `Timeline` 名前空間を使用
- ランタイムとエディタのコードを明確に分離
