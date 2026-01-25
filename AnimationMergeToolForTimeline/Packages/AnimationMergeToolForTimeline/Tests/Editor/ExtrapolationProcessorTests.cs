using NUnit.Framework;
using UnityEngine;
using UnityEngine.Timeline;
using AnimationMergeTool.Editor.Domain;
using AnimationMergeTool.Editor.Domain.Models;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// ExtrapolationProcessorクラスの単体テスト
    /// </summary>
    public class ExtrapolationProcessorTests
    {
        private ExtrapolationProcessor _processor;
        private TimelineAsset _timelineAsset;
        private AnimationTrack _animationTrack;
        private AnimationClip _testClip;

        [SetUp]
        public void SetUp()
        {
            _processor = new ExtrapolationProcessor();
            _timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
            _animationTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Test Track");
            _testClip = new AnimationClip();
            _testClip.name = "Test Animation";
        }

        [TearDown]
        public void TearDown()
        {
            if (_testClip != null)
            {
                Object.DestroyImmediate(_testClip);
            }
            if (_timelineAsset != null)
            {
                Object.DestroyImmediate(_timelineAsset);
            }
        }

        #region 基本テスト

        [Test]
        public void TryGetExtrapolatedValue_nullカーブを渡すとfalseを返す()
        {
            // Arrange
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act
            var result = _processor.TryGetExtrapolatedValue(null, clipInfo, 0.5f, out var value);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0f, value);
        }

        [Test]
        public void TryGetExtrapolatedValue_nullClipInfoを渡すとfalseを返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);

            // Act
            var result = _processor.TryGetExtrapolatedValue(curve, null, 0.5f, out var value);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0f, value);
        }

        [Test]
        public void TryGetExtrapolatedValue_空のカーブを渡すとfalseを返す()
        {
            // Arrange
            var curve = new AnimationCurve();
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 0.5f, out var value);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0f, value);
        }

        [Test]
        public void TryGetExtrapolatedValue_クリップ範囲内の時間は正しい値を返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 0.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0.5f, value, 0.0001f);
        }

        #endregion

        #region None Extrapolation テスト（4.1.2）

        [Test]
        public void TryGetExtrapolatedValue_PreExtrapolationがNoneの場合_クリップ開始前はfalseを返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 1.0; // クリップは1秒から開始
            timelineClip.duration = 1.0;
            timelineClip.preExtrapolationMode = TimelineClip.ClipExtrapolation.None;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 0.5秒はクリップ開始前
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 0.5f, out var value);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0f, value);
        }

        [Test]
        public void TryGetExtrapolatedValue_PostExtrapolationがNoneの場合_クリップ終了後はfalseを返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0; // クリップは1秒で終了
            timelineClip.postExtrapolationMode = TimelineClip.ClipExtrapolation.None;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.5秒はクリップ終了後
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.5f, out var value);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0f, value);
        }

        [Test]
        public void TryGetExtrapolatedValue_PreExtrapolationがNoneでも_クリップ範囲内はtrueを返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 1.0;
            timelineClip.duration = 1.0;
            timelineClip.preExtrapolationMode = TimelineClip.ClipExtrapolation.None;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.5秒はクリップ範囲内
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0.5f, value, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_PostExtrapolationがNoneでも_クリップ範囲内はtrueを返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            timelineClip.postExtrapolationMode = TimelineClip.ClipExtrapolation.None;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 0.5秒はクリップ範囲内
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 0.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0.5f, value, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_PreとPostが両方Noneの場合_範囲外は両方falseを返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 1.0;
            timelineClip.duration = 1.0; // クリップは1秒〜2秒
            timelineClip.preExtrapolationMode = TimelineClip.ClipExtrapolation.None;
            timelineClip.postExtrapolationMode = TimelineClip.ClipExtrapolation.None;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act & Assert - 開始前
            var resultBefore = _processor.TryGetExtrapolatedValue(curve, clipInfo, 0.5f, out var valueBefore);
            Assert.IsFalse(resultBefore);
            Assert.AreEqual(0f, valueBefore);

            // Act & Assert - 終了後
            var resultAfter = _processor.TryGetExtrapolatedValue(curve, clipInfo, 2.5f, out var valueAfter);
            Assert.IsFalse(resultAfter);
            Assert.AreEqual(0f, valueAfter);
        }

        [Test]
        public void TryGetExtrapolatedValue_None設定のクリップ境界での動作確認()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 1.0;
            timelineClip.duration = 1.0; // クリップは1秒〜2秒
            timelineClip.preExtrapolationMode = TimelineClip.ClipExtrapolation.None;
            timelineClip.postExtrapolationMode = TimelineClip.ClipExtrapolation.None;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act & Assert - 開始時間（1秒）はクリップ範囲内
            var resultAtStart = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.0f, out var valueAtStart);
            Assert.IsTrue(resultAtStart);
            Assert.AreEqual(0f, valueAtStart, 0.0001f);

            // Act & Assert - 終了時間（2秒）はクリップ範囲内
            var resultAtEnd = _processor.TryGetExtrapolatedValue(curve, clipInfo, 2.0f, out var valueAtEnd);
            Assert.IsTrue(resultAtEnd);
            Assert.AreEqual(1f, valueAtEnd, 0.0001f);
        }

        #endregion

        #region Hold Extrapolation テスト（4.1.3）

        [Test]
        public void TryGetExtrapolatedValue_PreExtrapolationがHoldの場合_クリップ開始前は最初のキーの値を返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 10, 1, 20); // 0→10, 1→20
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 2.0; // クリップは2秒から開始
            timelineClip.duration = 1.0;
            timelineClip.preExtrapolationMode = TimelineClip.ClipExtrapolation.Hold;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 0.5秒はクリップ開始前
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 0.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(10f, value, 0.0001f); // 最初のキーの値（10）を維持
        }

        [Test]
        public void TryGetExtrapolatedValue_PostExtrapolationがHoldの場合_クリップ終了後は最後のキーの値を返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 10, 1, 20); // 0→10, 1→20
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0; // クリップは1秒で終了
            timelineClip.postExtrapolationMode = TimelineClip.ClipExtrapolation.Hold;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 2.5秒はクリップ終了後
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 2.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(20f, value, 0.0001f); // 最後のキーの値（20）を維持
        }

        [Test]
        public void TryGetExtrapolatedValue_PreExtrapolationがHoldでも_クリップ範囲内は補間された値を返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 1.0;
            timelineClip.duration = 1.0;
            timelineClip.preExtrapolationMode = TimelineClip.ClipExtrapolation.Hold;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.5秒はクリップ範囲内
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(5f, value, 0.0001f); // 線形補間された値
        }

        [Test]
        public void TryGetExtrapolatedValue_PostExtrapolationがHoldでも_クリップ範囲内は補間された値を返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            timelineClip.postExtrapolationMode = TimelineClip.ClipExtrapolation.Hold;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 0.3秒はクリップ範囲内
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 0.3f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(3f, value, 0.0001f); // 線形補間された値
        }

        [Test]
        public void TryGetExtrapolatedValue_PreとPostが両方Holdの場合_範囲外は適切な値を返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 5, 1, 15);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 2.0;
            timelineClip.duration = 1.0; // クリップは2秒〜3秒
            timelineClip.preExtrapolationMode = TimelineClip.ClipExtrapolation.Hold;
            timelineClip.postExtrapolationMode = TimelineClip.ClipExtrapolation.Hold;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act & Assert - 開始前
            var resultBefore = _processor.TryGetExtrapolatedValue(curve, clipInfo, 0.5f, out var valueBefore);
            Assert.IsTrue(resultBefore);
            Assert.AreEqual(5f, valueBefore, 0.0001f); // 最初のキーの値

            // Act & Assert - 終了後
            var resultAfter = _processor.TryGetExtrapolatedValue(curve, clipInfo, 5.0f, out var valueAfter);
            Assert.IsTrue(resultAfter);
            Assert.AreEqual(15f, valueAfter, 0.0001f); // 最後のキーの値
        }

        [Test]
        public void TryGetExtrapolatedValue_Hold設定のクリップ境界での動作確認()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 100, 1, 200);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 1.0;
            timelineClip.duration = 1.0; // クリップは1秒〜2秒
            timelineClip.preExtrapolationMode = TimelineClip.ClipExtrapolation.Hold;
            timelineClip.postExtrapolationMode = TimelineClip.ClipExtrapolation.Hold;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act & Assert - 開始時間（1秒）はクリップ範囲内
            var resultAtStart = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.0f, out var valueAtStart);
            Assert.IsTrue(resultAtStart);
            Assert.AreEqual(100f, valueAtStart, 0.0001f);

            // Act & Assert - 終了時間（2秒）はクリップ範囲内
            var resultAtEnd = _processor.TryGetExtrapolatedValue(curve, clipInfo, 2.0f, out var valueAtEnd);
            Assert.IsTrue(resultAtEnd);
            Assert.AreEqual(200f, valueAtEnd, 0.0001f);

            // Act & Assert - 開始時間直前
            var resultBeforeStart = _processor.TryGetExtrapolatedValue(curve, clipInfo, 0.999f, out var valueBeforeStart);
            Assert.IsTrue(resultBeforeStart);
            Assert.AreEqual(100f, valueBeforeStart, 0.0001f); // Hold: 最初のキーの値

            // Act & Assert - 終了時間直後
            var resultAfterEnd = _processor.TryGetExtrapolatedValue(curve, clipInfo, 2.001f, out var valueAfterEnd);
            Assert.IsTrue(resultAfterEnd);
            Assert.AreEqual(200f, valueAfterEnd, 0.0001f); // Hold: 最後のキーの値
        }

        [Test]
        public void TryGetExtrapolatedValue_HoldとClipInの組み合わせ()
        {
            // Arrange: ClipInで最初の0.5秒をトリミング
            var curve = AnimationCurve.Linear(0, 0, 2, 20); // 0→0, 2→20
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 1.0;
            timelineClip.duration = 1.0;
            timelineClip.clipIn = 0.5; // ソースクリップの0.5秒からスタート
            timelineClip.preExtrapolationMode = TimelineClip.ClipExtrapolation.Hold;
            timelineClip.postExtrapolationMode = TimelineClip.ClipExtrapolation.Hold;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act & Assert - クリップ開始前（PreExtrapolation Hold）
            // ClipIn=0.5なので、最初のキーの値はcurve.Evaluate(0.5) = 5
            var resultBefore = _processor.TryGetExtrapolatedValue(curve, clipInfo, 0.5f, out var valueBefore);
            Assert.IsTrue(resultBefore);
            Assert.AreEqual(5f, valueBefore, 0.0001f);

            // Act & Assert - クリップ終了後（PostExtrapolation Hold）
            // duration=1.0, timeScale=1.0なので、最後のキーの値はcurve.Evaluate(0.5 + 1.0) = curve.Evaluate(1.5) = 15
            var resultAfter = _processor.TryGetExtrapolatedValue(curve, clipInfo, 3.0f, out var valueAfter);
            Assert.IsTrue(resultAfter);
            Assert.AreEqual(15f, valueAfter, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_HoldとTimeScaleの組み合わせ()
        {
            // Arrange: TimeScale=2でアニメーションを2倍速再生
            var curve = AnimationCurve.Linear(0, 0, 2, 20); // 0→0, 2→20
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 1.0;
            timelineClip.duration = 1.0;
            timelineClip.timeScale = 2.0; // 2倍速
            timelineClip.preExtrapolationMode = TimelineClip.ClipExtrapolation.Hold;
            timelineClip.postExtrapolationMode = TimelineClip.ClipExtrapolation.Hold;
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act & Assert - クリップ開始前（PreExtrapolation Hold）
            // 最初のキーの値はcurve.Evaluate(0) = 0
            var resultBefore = _processor.TryGetExtrapolatedValue(curve, clipInfo, 0.5f, out var valueBefore);
            Assert.IsTrue(resultBefore);
            Assert.AreEqual(0f, valueBefore, 0.0001f);

            // Act & Assert - クリップ終了後（PostExtrapolation Hold）
            // TimeScale=2.0なので、duration=1.0のクリップで2秒分のアニメーションが再生される
            // 最後のキーの値はcurve.Evaluate(0 + 1.0 * 2.0) = curve.Evaluate(2.0) = 20
            var resultAfter = _processor.TryGetExtrapolatedValue(curve, clipInfo, 3.0f, out var valueAfter);
            Assert.IsTrue(resultAfter);
            Assert.AreEqual(20f, valueAfter, 0.0001f);
        }

        #endregion

        #region HasValue テスト

        [Test]
        public void HasValue_NoneはFalseを返す()
        {
            // Act & Assert
            Assert.IsFalse(ExtrapolationProcessor.HasValue(TimelineClip.ClipExtrapolation.None));
        }

        [Test]
        public void HasValue_HoldはTrueを返す()
        {
            // Act & Assert
            Assert.IsTrue(ExtrapolationProcessor.HasValue(TimelineClip.ClipExtrapolation.Hold));
        }

        [Test]
        public void HasValue_LoopはTrueを返す()
        {
            // Act & Assert
            Assert.IsTrue(ExtrapolationProcessor.HasValue(TimelineClip.ClipExtrapolation.Loop));
        }

        [Test]
        public void HasValue_PingPongはTrueを返す()
        {
            // Act & Assert
            Assert.IsTrue(ExtrapolationProcessor.HasValue(TimelineClip.ClipExtrapolation.PingPong));
        }

        [Test]
        public void HasValue_ContinueはTrueを返す()
        {
            // Act & Assert
            Assert.IsTrue(ExtrapolationProcessor.HasValue(TimelineClip.ClipExtrapolation.Continue));
        }

        #endregion

        #region フレームレート設定テスト

        [Test]
        public void SetFrameRate_正の値を設定できる()
        {
            // Act
            _processor.SetFrameRate(30f);

            // Assert
            Assert.AreEqual(30f, _processor.GetFrameRate(), 0.0001f);
        }

        [Test]
        public void SetFrameRate_0以下の値は無視される()
        {
            // Arrange
            _processor.SetFrameRate(60f); // 初期値を設定

            // Act
            _processor.SetFrameRate(0f);
            _processor.SetFrameRate(-1f);

            // Assert
            Assert.AreEqual(60f, _processor.GetFrameRate(), 0.0001f);
        }

        [Test]
        public void GetFrameRate_デフォルト値は60()
        {
            // Assert
            Assert.AreEqual(60f, _processor.GetFrameRate(), 0.0001f);
        }

        #endregion
    }
}
