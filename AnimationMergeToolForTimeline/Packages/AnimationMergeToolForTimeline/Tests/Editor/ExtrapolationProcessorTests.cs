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
