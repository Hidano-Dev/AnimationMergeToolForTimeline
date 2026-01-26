using System;
using System.Collections.Generic;
using NUnit.Framework;
using AnimationMergeTool.Editor.Infrastructure;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// テスト用のファイル存在確認モック
    /// </summary>
    public class MockFileExistenceChecker : IFileExistenceChecker
    {
        private readonly HashSet<string> _existingFiles = new HashSet<string>();

        public void AddExistingFile(string path)
        {
            _existingFiles.Add(path);
        }

        public bool Exists(string path)
        {
            return _existingFiles.Contains(path);
        }
    }

    /// <summary>
    /// FileNameGeneratorクラスの単体テスト
    /// </summary>
    public class FileNameGeneratorTests
    {
        private FileNameGenerator _fileNameGenerator;

        [SetUp]
        public void SetUp()
        {
            _fileNameGenerator = new FileNameGenerator();
        }

        #region GenerateBaseName テスト

        [Test]
        public void GenerateBaseName_正しい形式でファイル名を生成する()
        {
            // Arrange
            var timelineAssetName = "MyTimeline";
            var animatorName = "MyAnimator";

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName);

            // Assert
            Assert.AreEqual("MyTimeline_MyAnimator_Merged.anim", result);
        }

        [Test]
        public void GenerateBaseName_TimelineAsset名がnullの場合Unknownを使用する()
        {
            // Arrange
            string timelineAssetName = null;
            var animatorName = "MyAnimator";

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName);

            // Assert
            Assert.AreEqual("Unknown_MyAnimator_Merged.anim", result);
        }

        [Test]
        public void GenerateBaseName_TimelineAsset名が空文字の場合Unknownを使用する()
        {
            // Arrange
            var timelineAssetName = "";
            var animatorName = "MyAnimator";

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName);

            // Assert
            Assert.AreEqual("Unknown_MyAnimator_Merged.anim", result);
        }

        [Test]
        public void GenerateBaseName_Animator名がnullの場合Unknownを使用する()
        {
            // Arrange
            var timelineAssetName = "MyTimeline";
            string animatorName = null;

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName);

            // Assert
            Assert.AreEqual("MyTimeline_Unknown_Merged.anim", result);
        }

        [Test]
        public void GenerateBaseName_Animator名が空文字の場合Unknownを使用する()
        {
            // Arrange
            var timelineAssetName = "MyTimeline";
            var animatorName = "";

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName);

            // Assert
            Assert.AreEqual("MyTimeline_Unknown_Merged.anim", result);
        }

        [Test]
        public void GenerateBaseName_両方nullの場合両方Unknownを使用する()
        {
            // Arrange
            string timelineAssetName = null;
            string animatorName = null;

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName);

            // Assert
            Assert.AreEqual("Unknown_Unknown_Merged.anim", result);
        }

        [Test]
        public void GenerateBaseName_日本語名でも正しく生成できる()
        {
            // Arrange
            var timelineAssetName = "タイムライン";
            var animatorName = "アニメーター";

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName);

            // Assert
            Assert.AreEqual("タイムライン_アニメーター_Merged.anim", result);
        }

        [Test]
        public void GenerateBaseName_スペースを含む名前でも正しく生成できる()
        {
            // Arrange
            var timelineAssetName = "My Timeline";
            var animatorName = "My Animator";

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName);

            // Assert
            Assert.AreEqual("My Timeline_My Animator_Merged.anim", result);
        }

        [Test]
        public void GenerateBaseName_拡張子animで終わる()
        {
            // Arrange
            var timelineAssetName = "Timeline";
            var animatorName = "Animator";

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName);

            // Assert
            Assert.IsTrue(result.EndsWith(".anim"));
        }

        [Test]
        public void GenerateBaseName_Mergedサフィックスが含まれる()
        {
            // Arrange
            var timelineAssetName = "Timeline";
            var animatorName = "Animator";

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName);

            // Assert
            Assert.IsTrue(result.Contains("_Merged"));
        }

        #endregion

        #region GenerateUniqueFilePath テスト

        [Test]
        public void GenerateUniqueFilePath_ファイルが存在しない場合基本ファイル名を返す()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets", "MyTimeline", "MyAnimator");

            // Assert
            Assert.AreEqual("Assets/MyTimeline_MyAnimator_Merged.anim", result);
        }

        [Test]
        public void GenerateUniqueFilePath_ファイルが存在する場合連番1を付与する()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            mockChecker.AddExistingFile("Assets/MyTimeline_MyAnimator_Merged.anim");
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets", "MyTimeline", "MyAnimator");

            // Assert
            Assert.AreEqual("Assets/MyTimeline_MyAnimator_Merged(1).anim", result);
        }

        [Test]
        public void GenerateUniqueFilePath_連番1も存在する場合連番2を付与する()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            mockChecker.AddExistingFile("Assets/MyTimeline_MyAnimator_Merged.anim");
            mockChecker.AddExistingFile("Assets/MyTimeline_MyAnimator_Merged(1).anim");
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets", "MyTimeline", "MyAnimator");

            // Assert
            Assert.AreEqual("Assets/MyTimeline_MyAnimator_Merged(2).anim", result);
        }

        [Test]
        public void GenerateUniqueFilePath_連番に途中の欠番がある場合最初の空きを使用する()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            mockChecker.AddExistingFile("Assets/MyTimeline_MyAnimator_Merged.anim");
            // (1)は存在しない
            mockChecker.AddExistingFile("Assets/MyTimeline_MyAnimator_Merged(2).anim");
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets", "MyTimeline", "MyAnimator");

            // Assert
            Assert.AreEqual("Assets/MyTimeline_MyAnimator_Merged(1).anim", result);
        }

        [Test]
        public void GenerateUniqueFilePath_ディレクトリ末尾にスラッシュがあっても正しく処理する()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets/", "MyTimeline", "MyAnimator");

            // Assert
            Assert.AreEqual("Assets/MyTimeline_MyAnimator_Merged.anim", result);
        }

        [Test]
        public void GenerateUniqueFilePath_ディレクトリ末尾にバックスラッシュがあっても正しく処理する()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets\\", "MyTimeline", "MyAnimator");

            // Assert
            Assert.AreEqual("Assets/MyTimeline_MyAnimator_Merged.anim", result);
        }

        [Test]
        public void GenerateUniqueFilePath_FileExistenceCheckerがnullの場合例外を投げる()
        {
            // Arrange
            var generator = new FileNameGenerator(); // デフォルトコンストラクタ

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                generator.GenerateUniqueFilePath("Assets", "MyTimeline", "MyAnimator");
            });
        }

        [Test]
        public void GenerateUniqueFilePath_連番はスペースなしで付与される()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            mockChecker.AddExistingFile("Assets/MyTimeline_MyAnimator_Merged.anim");
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets", "MyTimeline", "MyAnimator");

            // Assert
            // "(1)"の前にスペースがないことを確認
            Assert.IsFalse(result.Contains(" (1)"));
            Assert.IsTrue(result.Contains("_Merged(1).anim"));
        }

        [Test]
        public void GenerateUniqueFilePath_サブディレクトリでも正しく動作する()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            mockChecker.AddExistingFile("Assets/Animations/MyTimeline_MyAnimator_Merged.anim");
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets/Animations", "MyTimeline", "MyAnimator");

            // Assert
            Assert.AreEqual("Assets/Animations/MyTimeline_MyAnimator_Merged(1).anim", result);
        }

        #endregion

        #region FBX対応 GenerateBaseName テスト

        [Test]
        public void GenerateBaseName_FBX拡張子で正しい形式のファイル名を生成する()
        {
            // Arrange
            var timelineAssetName = "MyTimeline";
            var animatorName = "MyAnimator";
            var extension = ".fbx";

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName, extension);

            // Assert
            Assert.AreEqual("MyTimeline_MyAnimator_Merged.fbx", result);
        }

        [Test]
        public void GenerateBaseName_FBX拡張子でTimelineAsset名がnullの場合Unknownを使用する()
        {
            // Arrange
            string timelineAssetName = null;
            var animatorName = "MyAnimator";
            var extension = ".fbx";

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName, extension);

            // Assert
            Assert.AreEqual("Unknown_MyAnimator_Merged.fbx", result);
        }

        [Test]
        public void GenerateBaseName_FBX拡張子でAnimator名がnullの場合Unknownを使用する()
        {
            // Arrange
            var timelineAssetName = "MyTimeline";
            string animatorName = null;
            var extension = ".fbx";

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName, extension);

            // Assert
            Assert.AreEqual("MyTimeline_Unknown_Merged.fbx", result);
        }

        [Test]
        public void GenerateBaseName_FBX拡張子で両方nullの場合両方Unknownを使用する()
        {
            // Arrange
            string timelineAssetName = null;
            string animatorName = null;
            var extension = ".fbx";

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName, extension);

            // Assert
            Assert.AreEqual("Unknown_Unknown_Merged.fbx", result);
        }

        [Test]
        public void GenerateBaseName_拡張子にドットがない場合でも正しく処理する()
        {
            // Arrange
            var timelineAssetName = "MyTimeline";
            var animatorName = "MyAnimator";
            var extension = "fbx"; // ドットなし

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName, extension);

            // Assert
            Assert.AreEqual("MyTimeline_MyAnimator_Merged.fbx", result);
        }

        [Test]
        public void GenerateBaseName_拡張子がnullの場合デフォルトのanim拡張子を使用する()
        {
            // Arrange
            var timelineAssetName = "MyTimeline";
            var animatorName = "MyAnimator";
            string extension = null;

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName, extension);

            // Assert
            Assert.AreEqual("MyTimeline_MyAnimator_Merged.anim", result);
        }

        [Test]
        public void GenerateBaseName_拡張子が空文字の場合デフォルトのanim拡張子を使用する()
        {
            // Arrange
            var timelineAssetName = "MyTimeline";
            var animatorName = "MyAnimator";
            var extension = "";

            // Act
            var result = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName, extension);

            // Assert
            Assert.AreEqual("MyTimeline_MyAnimator_Merged.anim", result);
        }

        #endregion

        #region FBX対応 GenerateUniqueFilePath テスト

        [Test]
        public void GenerateUniqueFilePath_FBX拡張子でファイルが存在しない場合基本ファイル名を返す()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets", "MyTimeline", "MyAnimator", ".fbx");

            // Assert
            Assert.AreEqual("Assets/MyTimeline_MyAnimator_Merged.fbx", result);
        }

        [Test]
        public void GenerateUniqueFilePath_FBX拡張子でファイルが存在する場合連番1を付与する()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            mockChecker.AddExistingFile("Assets/MyTimeline_MyAnimator_Merged.fbx");
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets", "MyTimeline", "MyAnimator", ".fbx");

            // Assert
            Assert.AreEqual("Assets/MyTimeline_MyAnimator_Merged(1).fbx", result);
        }

        [Test]
        public void GenerateUniqueFilePath_FBX拡張子で連番1も存在する場合連番2を付与する()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            mockChecker.AddExistingFile("Assets/MyTimeline_MyAnimator_Merged.fbx");
            mockChecker.AddExistingFile("Assets/MyTimeline_MyAnimator_Merged(1).fbx");
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets", "MyTimeline", "MyAnimator", ".fbx");

            // Assert
            Assert.AreEqual("Assets/MyTimeline_MyAnimator_Merged(2).fbx", result);
        }

        [Test]
        public void GenerateUniqueFilePath_FBX拡張子で連番に途中の欠番がある場合最初の空きを使用する()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            mockChecker.AddExistingFile("Assets/MyTimeline_MyAnimator_Merged.fbx");
            // (1)は存在しない
            mockChecker.AddExistingFile("Assets/MyTimeline_MyAnimator_Merged(2).fbx");
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets", "MyTimeline", "MyAnimator", ".fbx");

            // Assert
            Assert.AreEqual("Assets/MyTimeline_MyAnimator_Merged(1).fbx", result);
        }

        [Test]
        public void GenerateUniqueFilePath_FBX拡張子でサブディレクトリでも正しく動作する()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            mockChecker.AddExistingFile("Assets/Animations/MyTimeline_MyAnimator_Merged.fbx");
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets/Animations", "MyTimeline", "MyAnimator", ".fbx");

            // Assert
            Assert.AreEqual("Assets/Animations/MyTimeline_MyAnimator_Merged(1).fbx", result);
        }

        [Test]
        public void GenerateUniqueFilePath_FBX拡張子でディレクトリ末尾にスラッシュがあっても正しく処理する()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets/", "MyTimeline", "MyAnimator", ".fbx");

            // Assert
            Assert.AreEqual("Assets/MyTimeline_MyAnimator_Merged.fbx", result);
        }

        [Test]
        public void GenerateUniqueFilePath_拡張子がnullの場合デフォルトのanim拡張子を使用する()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            var generator = new FileNameGenerator(mockChecker);

            // Act
            var result = generator.GenerateUniqueFilePath("Assets", "MyTimeline", "MyAnimator", null);

            // Assert
            Assert.AreEqual("Assets/MyTimeline_MyAnimator_Merged.anim", result);
        }

        [Test]
        public void GenerateUniqueFilePath_拡張子パラメータなしの場合anim拡張子を使用する()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            var generator = new FileNameGenerator(mockChecker);

            // Act
            // 既存のオーバーロード（拡張子なし）を使用
            var result = generator.GenerateUniqueFilePath("Assets", "MyTimeline", "MyAnimator");

            // Assert
            Assert.AreEqual("Assets/MyTimeline_MyAnimator_Merged.anim", result);
        }

        #endregion
    }
}
