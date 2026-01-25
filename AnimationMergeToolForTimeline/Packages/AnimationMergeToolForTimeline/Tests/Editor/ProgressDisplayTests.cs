using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AnimationMergeTool.Editor.UI;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// ProgressDisplayクラスの単体テスト
    /// タスク8.3.2: 進捗表示機能のテスト
    /// タスク8.3.3: 処理結果のConsole出力機能のテスト
    /// </summary>
    public class ProgressDisplayTests
    {
        private ProgressDisplay _progressDisplay;

        [SetUp]
        public void SetUp()
        {
            _progressDisplay = new ProgressDisplay();
        }

        [TearDown]
        public void TearDown()
        {
            // 進捗バーが表示されたままにならないようにクリア
            if (_progressDisplay != null && _progressDisplay.IsDisplaying)
            {
                _progressDisplay.End();
            }
            _progressDisplay = null;
        }

        #region コンストラクタ テスト

        [Test]
        public void Constructor_初期状態では表示していない()
        {
            // Assert
            Assert.IsFalse(_progressDisplay.IsDisplaying);
        }

        [Test]
        public void Constructor_初期状態では進捗が0()
        {
            // Assert
            Assert.AreEqual(0f, _progressDisplay.CurrentProgress);
        }

        [Test]
        public void Constructor_初期状態ではメッセージが空文字()
        {
            // Assert
            Assert.AreEqual(string.Empty, _progressDisplay.CurrentMessage);
        }

        #endregion

        #region Begin テスト

        [Test]
        public void Begin_IsDisplayingがtrueになる()
        {
            // Act
            _progressDisplay.Begin("テスト開始");

            // Assert
            Assert.IsTrue(_progressDisplay.IsDisplaying);
        }

        [Test]
        public void Begin_CurrentProgressが0になる()
        {
            // Arrange
            _progressDisplay.Begin("初期化");
            _progressDisplay.Update("進行中", 0.5f);

            // Act
            _progressDisplay.Begin("再開始");

            // Assert
            Assert.AreEqual(0f, _progressDisplay.CurrentProgress);
        }

        [Test]
        public void Begin_CurrentMessageが設定される()
        {
            // Act
            _progressDisplay.Begin("テストメッセージ");

            // Assert
            Assert.AreEqual("テストメッセージ", _progressDisplay.CurrentMessage);
        }

        [Test]
        public void Begin_nullを渡すと空文字になる()
        {
            // Act
            _progressDisplay.Begin(null);

            // Assert
            Assert.AreEqual(string.Empty, _progressDisplay.CurrentMessage);
        }

        #endregion

        #region Update テスト

        [Test]
        public void Update_CurrentMessageが更新される()
        {
            // Arrange
            _progressDisplay.Begin("開始");

            // Act
            _progressDisplay.Update("更新後メッセージ", 0.5f);

            // Assert
            Assert.AreEqual("更新後メッセージ", _progressDisplay.CurrentMessage);
        }

        [Test]
        public void Update_CurrentProgressが更新される()
        {
            // Arrange
            _progressDisplay.Begin("開始");

            // Act
            _progressDisplay.Update("進行中", 0.75f);

            // Assert
            Assert.AreEqual(0.75f, _progressDisplay.CurrentProgress, 0.001f);
        }

        [Test]
        public void Update_進捗が1を超える場合1にクランプされる()
        {
            // Arrange
            _progressDisplay.Begin("開始");

            // Act
            _progressDisplay.Update("完了", 1.5f);

            // Assert
            Assert.AreEqual(1f, _progressDisplay.CurrentProgress, 0.001f);
        }

        [Test]
        public void Update_進捗が0未満の場合0にクランプされる()
        {
            // Arrange
            _progressDisplay.Begin("開始");

            // Act
            _progressDisplay.Update("エラー", -0.5f);

            // Assert
            Assert.AreEqual(0f, _progressDisplay.CurrentProgress, 0.001f);
        }

        [Test]
        public void Update_nullを渡すと空文字になる()
        {
            // Arrange
            _progressDisplay.Begin("開始");

            // Act
            _progressDisplay.Update(null, 0.5f);

            // Assert
            Assert.AreEqual(string.Empty, _progressDisplay.CurrentMessage);
        }

        [Test]
        public void Update_Begin前でも状態は更新される()
        {
            // Act
            _progressDisplay.Update("メッセージ", 0.5f);

            // Assert
            Assert.AreEqual("メッセージ", _progressDisplay.CurrentMessage);
            Assert.AreEqual(0.5f, _progressDisplay.CurrentProgress, 0.001f);
        }

        #endregion

        #region End テスト

        [Test]
        public void End_IsDisplayingがfalseになる()
        {
            // Arrange
            _progressDisplay.Begin("開始");

            // Act
            _progressDisplay.End();

            // Assert
            Assert.IsFalse(_progressDisplay.IsDisplaying);
        }

        [Test]
        public void End_CurrentProgressが0にリセットされる()
        {
            // Arrange
            _progressDisplay.Begin("開始");
            _progressDisplay.Update("進行中", 0.75f);

            // Act
            _progressDisplay.End();

            // Assert
            Assert.AreEqual(0f, _progressDisplay.CurrentProgress);
        }

        [Test]
        public void End_CurrentMessageが空文字にリセットされる()
        {
            // Arrange
            _progressDisplay.Begin("開始");
            _progressDisplay.Update("進行中", 0.75f);

            // Act
            _progressDisplay.End();

            // Assert
            Assert.AreEqual(string.Empty, _progressDisplay.CurrentMessage);
        }

        [Test]
        public void End_Begin前に呼んでもエラーにならない()
        {
            // Act & Assert - 例外が発生しないことを確認
            Assert.DoesNotThrow(() => _progressDisplay.End());
        }

        #endregion

        #region 連続操作 テスト

        [Test]
        public void 連続操作_Begin_Update_End_正常に動作する()
        {
            // Act & Assert - 一連の操作が例外なく完了すること
            Assert.DoesNotThrow(() =>
            {
                _progressDisplay.Begin("処理開始");
                Assert.IsTrue(_progressDisplay.IsDisplaying);

                _progressDisplay.Update("処理中 1/3", 0.33f);
                Assert.AreEqual(0.33f, _progressDisplay.CurrentProgress, 0.01f);

                _progressDisplay.Update("処理中 2/3", 0.66f);
                Assert.AreEqual(0.66f, _progressDisplay.CurrentProgress, 0.01f);

                _progressDisplay.Update("処理中 3/3", 1.0f);
                Assert.AreEqual(1.0f, _progressDisplay.CurrentProgress, 0.01f);

                _progressDisplay.End();
                Assert.IsFalse(_progressDisplay.IsDisplaying);
            });
        }

        [Test]
        public void 連続操作_複数回Beginを呼んでも正常に動作する()
        {
            // Act & Assert
            _progressDisplay.Begin("処理1");
            Assert.IsTrue(_progressDisplay.IsDisplaying);
            Assert.AreEqual("処理1", _progressDisplay.CurrentMessage);

            _progressDisplay.Begin("処理2");
            Assert.IsTrue(_progressDisplay.IsDisplaying);
            Assert.AreEqual("処理2", _progressDisplay.CurrentMessage);
            Assert.AreEqual(0f, _progressDisplay.CurrentProgress);
        }

        #endregion

        #region LogSuccess テスト（タスク8.3.3）

        [Test]
        public void LogSuccess_メッセージがDebugLogに出力される()
        {
            // Act
            _progressDisplay.LogSuccess("処理が正常に完了しました");

            // Assert - LogAssertでログ出力を検証
            LogAssert.Expect(LogType.Log, "[Animation Merge Tool] 処理が正常に完了しました");
        }

        [Test]
        public void LogSuccess_nullを渡すとプレフィックスのみ出力される()
        {
            // Act
            _progressDisplay.LogSuccess(null);

            // Assert
            LogAssert.Expect(LogType.Log, "[Animation Merge Tool] ");
        }

        [Test]
        public void LogSuccess_空文字を渡すとプレフィックスのみ出力される()
        {
            // Act
            _progressDisplay.LogSuccess(string.Empty);

            // Assert
            LogAssert.Expect(LogType.Log, "[Animation Merge Tool] ");
        }

        #endregion

        #region LogError テスト（タスク8.3.3）

        [Test]
        public void LogError_メッセージがDebugLogErrorに出力される()
        {
            // Act
            _progressDisplay.LogError("処理中にエラーが発生しました");

            // Assert - LogAssertでエラーログ出力を検証
            LogAssert.Expect(LogType.Error, "[Animation Merge Tool] 処理中にエラーが発生しました");
        }

        [Test]
        public void LogError_nullを渡すとプレフィックスのみ出力される()
        {
            // Act
            _progressDisplay.LogError(null);

            // Assert
            LogAssert.Expect(LogType.Error, "[Animation Merge Tool] ");
        }

        [Test]
        public void LogError_空文字を渡すとプレフィックスのみ出力される()
        {
            // Act
            _progressDisplay.LogError(string.Empty);

            // Assert
            LogAssert.Expect(LogType.Error, "[Animation Merge Tool] ");
        }

        #endregion

        #region LogWarning テスト（タスク8.3.3）

        [Test]
        public void LogWarning_メッセージがDebugLogWarningに出力される()
        {
            // Act
            _progressDisplay.LogWarning("警告: 一部のトラックがスキップされました");

            // Assert - LogAssertで警告ログ出力を検証
            LogAssert.Expect(LogType.Warning, "[Animation Merge Tool] 警告: 一部のトラックがスキップされました");
        }

        [Test]
        public void LogWarning_nullを渡すとプレフィックスのみ出力される()
        {
            // Act
            _progressDisplay.LogWarning(null);

            // Assert
            LogAssert.Expect(LogType.Warning, "[Animation Merge Tool] ");
        }

        [Test]
        public void LogWarning_空文字を渡すとプレフィックスのみ出力される()
        {
            // Act
            _progressDisplay.LogWarning(string.Empty);

            // Assert
            LogAssert.Expect(LogType.Warning, "[Animation Merge Tool] ");
        }

        #endregion
    }
}
