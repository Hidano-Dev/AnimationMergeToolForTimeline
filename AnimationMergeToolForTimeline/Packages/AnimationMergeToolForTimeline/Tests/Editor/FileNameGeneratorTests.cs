using NUnit.Framework;
using AnimationMergeTool.Editor.Infrastructure;

namespace AnimationMergeTool.Editor.Tests
{
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
    }
}
