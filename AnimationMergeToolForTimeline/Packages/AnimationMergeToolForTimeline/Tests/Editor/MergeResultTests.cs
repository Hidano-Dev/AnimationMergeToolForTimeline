using NUnit.Framework;
using UnityEngine;
using AnimationMergeTool.Editor.Domain.Models;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// MergeResultクラスの単体テスト
    /// </summary>
    public class MergeResultTests
    {
        private GameObject _testGameObject;
        private Animator _testAnimator;
        private AnimationClip _testClip;

        [SetUp]
        public void SetUp()
        {
            // テスト用のGameObjectとAnimatorを作成
            _testGameObject = new GameObject("Test Animator Object");
            _testAnimator = _testGameObject.AddComponent<Animator>();

            // テスト用のAnimationClipを作成
            _testClip = new AnimationClip();
            _testClip.name = "Test Generated Clip";
        }

        [TearDown]
        public void TearDown()
        {
            // テストリソースのクリーンアップ
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
            if (_testClip != null)
            {
                Object.DestroyImmediate(_testClip);
            }
        }

        [Test]
        public void Constructor_Animatorが設定される()
        {
            // Arrange & Act
            var result = new MergeResult(_testAnimator);

            // Assert
            Assert.AreEqual(_testAnimator, result.TargetAnimator);
        }

        [Test]
        public void Constructor_nullのAnimatorを渡しても例外が発生しない()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => new MergeResult(null));
        }

        [Test]
        public void Constructor_Logsが空のリストで初期化される()
        {
            // Arrange & Act
            var result = new MergeResult(_testAnimator);

            // Assert
            Assert.IsNotNull(result.Logs);
            Assert.AreEqual(0, result.Logs.Count);
        }

        [Test]
        public void Constructor_GeneratedClipがnullで初期化される()
        {
            // Arrange & Act
            var result = new MergeResult(_testAnimator);

            // Assert
            Assert.IsNull(result.GeneratedClip);
        }

        [Test]
        public void GeneratedClip_設定と取得ができる()
        {
            // Arrange
            var result = new MergeResult(_testAnimator);

            // Act
            result.GeneratedClip = _testClip;

            // Assert
            Assert.AreEqual(_testClip, result.GeneratedClip);
        }

        [Test]
        public void IsSuccess_GeneratedClipがnullの場合falseを返す()
        {
            // Arrange
            var result = new MergeResult(_testAnimator);

            // Act & Assert
            Assert.IsFalse(result.IsSuccess);
        }

        [Test]
        public void IsSuccess_GeneratedClipが設定されている場合trueを返す()
        {
            // Arrange
            var result = new MergeResult(_testAnimator);
            result.GeneratedClip = _testClip;

            // Act & Assert
            Assert.IsTrue(result.IsSuccess);
        }

        [Test]
        public void AddLog_ログメッセージが追加される()
        {
            // Arrange
            var result = new MergeResult(_testAnimator);

            // Act
            result.AddLog("テストログメッセージ");

            // Assert
            Assert.AreEqual(1, result.Logs.Count);
            Assert.AreEqual("テストログメッセージ", result.Logs[0]);
        }

        [Test]
        public void AddLog_複数のログメッセージを追加できる()
        {
            // Arrange
            var result = new MergeResult(_testAnimator);

            // Act
            result.AddLog("ログ1");
            result.AddLog("ログ2");
            result.AddLog("ログ3");

            // Assert
            Assert.AreEqual(3, result.Logs.Count);
            Assert.AreEqual("ログ1", result.Logs[0]);
            Assert.AreEqual("ログ2", result.Logs[1]);
            Assert.AreEqual("ログ3", result.Logs[2]);
        }

        [Test]
        public void AddLog_nullを渡しても例外が発生しない()
        {
            // Arrange
            var result = new MergeResult(_testAnimator);

            // Act & Assert
            Assert.DoesNotThrow(() => result.AddLog(null));
            Assert.AreEqual(0, result.Logs.Count);
        }

        [Test]
        public void AddLog_空文字を渡しても追加されない()
        {
            // Arrange
            var result = new MergeResult(_testAnimator);

            // Act
            result.AddLog("");

            // Assert
            Assert.AreEqual(0, result.Logs.Count);
        }

        [Test]
        public void AddErrorLog_エラーログが追加される()
        {
            // Arrange
            var result = new MergeResult(_testAnimator);

            // Act
            result.AddErrorLog("エラーメッセージ");

            // Assert
            Assert.AreEqual(1, result.Logs.Count);
            Assert.AreEqual("[Error] エラーメッセージ", result.Logs[0]);
        }

        [Test]
        public void AddErrorLog_複数のエラーログを追加できる()
        {
            // Arrange
            var result = new MergeResult(_testAnimator);

            // Act
            result.AddErrorLog("エラー1");
            result.AddErrorLog("エラー2");

            // Assert
            Assert.AreEqual(2, result.Logs.Count);
            Assert.AreEqual("[Error] エラー1", result.Logs[0]);
            Assert.AreEqual("[Error] エラー2", result.Logs[1]);
        }

        [Test]
        public void AddErrorLog_nullを渡しても例外が発生しない()
        {
            // Arrange
            var result = new MergeResult(_testAnimator);

            // Act & Assert
            Assert.DoesNotThrow(() => result.AddErrorLog(null));
            Assert.AreEqual(0, result.Logs.Count);
        }

        [Test]
        public void AddErrorLog_空文字を渡しても追加されない()
        {
            // Arrange
            var result = new MergeResult(_testAnimator);

            // Act
            result.AddErrorLog("");

            // Assert
            Assert.AreEqual(0, result.Logs.Count);
        }

        [Test]
        public void AddLogとAddErrorLog_混在して追加できる()
        {
            // Arrange
            var result = new MergeResult(_testAnimator);

            // Act
            result.AddLog("処理開始");
            result.AddErrorLog("警告メッセージ");
            result.AddLog("処理終了");

            // Assert
            Assert.AreEqual(3, result.Logs.Count);
            Assert.AreEqual("処理開始", result.Logs[0]);
            Assert.AreEqual("[Error] 警告メッセージ", result.Logs[1]);
            Assert.AreEqual("処理終了", result.Logs[2]);
        }
    }
}
