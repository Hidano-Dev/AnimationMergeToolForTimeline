# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 言語設定

このプロジェクトでは**日本語で応答してください**。コード内のコメントも日本語で記述します。

## ドキュメント

プロジェクトの詳細な仕様は以下のドキュメントを参照してください：

- `Docs/要件定義書.md` - 正式な要件定義書
- `Docs/開発計画書.md` - 開発計画書

**重要**: 開発を開始する前に、必ず要件定義書を確認してください。

## プロジェクト概要

Unity 6 (6000.0.64f1) で構築された、タイムラインシステム向けのアニメーション結合ツール。Timeline上の複数のAnimationTrackを単一のAnimationClipに結合し、Timelineの再生動作を再現するエディタ拡張ツール。

## 開発環境

- **Unity バージョン**: 6000.0.64f1
- **C# バージョン**: 9.0
- **.NET Framework**: 4.7.1
- **推奨IDE**: JetBrains Rider（com.unity.ide.rider パッケージ導入済み）

## プロジェクト構造

```
AnimationMergeToolForTimeline/
├── Assets/                    # ゲームアセット・スクリプト
│   ├── Editor/               # エディタ専用スクリプト（予定）
│   └── Scripts/              # ランタイムスクリプト（予定）
├── Packages/
│   ├── manifest.json         # パッケージ依存関係
│   └── AnimationMergeToolForTimeline/
│       └── Docs/             # 要件定義書・開発計画書
├── ProjectSettings/          # Unity プロジェクト設定
├── Assembly-CSharp.csproj    # ランタイムアセンブリ
└── Assembly-CSharp-Editor.csproj  # エディタ拡張アセンブリ
```

## ビルドとテスト

### Unity エディタでの操作
- **プロジェクトを開く**: Unity Hub から `AnimationMergeToolForTimeline` フォルダを開く
- **テスト実行**: Unity エディタ → Window → General → Test Runner

### コマンドラインテスト
```bash
# EditMode テストの実行
Unity.exe -batchmode -projectPath ./AnimationMergeToolForTimeline -runTests -testPlatform EditMode -testResults ./TestResults.xml

# PlayMode テストの実行
Unity.exe -batchmode -projectPath ./AnimationMergeToolForTimeline -runTests -testPlatform PlayMode -testResults ./TestResults.xml
```

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
| Assembly-CSharp | ランタイムスクリプト（クリップ結合ロジック） |
| Assembly-CSharp-Editor | エディタ拡張スクリプト（コンテキストメニュー、UI、アセット保存） |

## コーディング規約

- エディタ専用コードは `Editor` フォルダ配下に配置
- タイムライン関連の拡張は `Timeline` 名前空間を使用
- ランタイムとエディタのコードを明確に分離
- クリップデータ生成ロジックとアセットファイル作成を分離（要件定義書 NF-002参照）

## テスト方針

### テスト生成物のクリーンアップ

テストでAnimationClipを生成する場合、**必ずTearDownで削除すること**。

```csharp
[TearDown]
public void TearDown()
{
    // テストで作成したアセットをクリーンアップ
    foreach (var path in _createdAssetPaths)
    {
        if (!string.IsNullOrEmpty(path) && AssetDatabase.LoadAssetAtPath<Object>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
    }
    _createdAssetPaths.Clear();
}
```

**理由**: テスト実行時にAssets直下に `*_Merged.anim` ファイルが大量に残り、プロジェクトを汚染するため。

### LogAssertの使用

`Debug.LogError` を出力するメソッドをテストする場合、`LogAssert.Expect` で事前に宣言すること。

```csharp
using UnityEngine.TestTools;

[Test]
public void SomeTest()
{
    // エラーログを期待
    LogAssert.Expect(LogType.Error, "期待されるエラーメッセージ");

    // エラーログを出力するメソッドを呼び出す
    var result = SomeMethod(null);
    Assert.IsNull(result);
}
```

**理由**: Unity Test Frameworkでは、未宣言のエラーログがあるとテストが失敗するため。

## 作業時の一時ファイルのクリーンアップ

タスクがエラー無く正常に完了した場合、以下の一時ファイルを削除すること：

- **ログファイル**: `*.log`, `unity_test_log*.txt` など
- **テスト結果ファイル**: `TestResults*.xml`, `TestLog*.log` など
- **一時バッチファイル**: `*.bat`（作業用に作成したもの）
- **その他の一時ファイル**: 作業中に生成した中間ファイル

**理由**: リポジトリのルートディレクトリに一時ファイルが残ると、プロジェクトが汚染され、git statusが見づらくなるため。

**注意**: エラーが発生した場合は、デバッグのためにログファイルを残しておくこと。
