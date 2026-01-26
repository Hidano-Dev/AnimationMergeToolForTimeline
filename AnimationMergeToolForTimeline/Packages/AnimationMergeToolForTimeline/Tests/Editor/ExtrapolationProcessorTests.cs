using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
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

        /// <summary>
        /// TimelineClipのExtrapolationモードを設定するヘルパーメソッド
        /// preExtrapolationMode/postExtrapolationModeは読み取り専用のため、SerializedObjectを使用
        /// </summary>
        private void SetExtrapolationModes(
            TimelineClip clip,
            TimelineClip.ClipExtrapolation? preMode = null,
            TimelineClip.ClipExtrapolation? postMode = null)
        {
            var serializedObject = new SerializedObject(_timelineAsset);
            var tracksProperty = serializedObject.FindProperty("m_Tracks");

            for (int trackIndex = 0; trackIndex < tracksProperty.arraySize; trackIndex++)
            {
                var trackProperty = tracksProperty.GetArrayElementAtIndex(trackIndex);
                var trackObject = new SerializedObject(trackProperty.objectReferenceValue);
                var clipsProperty = trackObject.FindProperty("m_Clips");

                for (int clipIndex = 0; clipIndex < clipsProperty.arraySize; clipIndex++)
                {
                    var clipProperty = clipsProperty.GetArrayElementAtIndex(clipIndex);

                    // クリップのstartとdurationで照合
                    var startProperty = clipProperty.FindPropertyRelative("m_Start");
                    var durationProperty = clipProperty.FindPropertyRelative("m_Duration");

                    if (System.Math.Abs(startProperty.doubleValue - clip.start) < 0.0001 &&
                        System.Math.Abs(durationProperty.doubleValue - clip.duration) < 0.0001)
                    {
                        if (preMode.HasValue)
                        {
                            var preProperty = clipProperty.FindPropertyRelative("m_PreExtrapolationMode");
                            preProperty.intValue = (int)preMode.Value;
                        }
                        if (postMode.HasValue)
                        {
                            var postProperty = clipProperty.FindPropertyRelative("m_PostExtrapolationMode");
                            postProperty.intValue = (int)postMode.Value;
                        }
                        trackObject.ApplyModifiedPropertiesWithoutUndo();
                        return;
                    }
                }
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
            SetExtrapolationModes(timelineClip, preMode: TimelineClip.ClipExtrapolation.None);
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
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.None);
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
            SetExtrapolationModes(timelineClip, preMode: TimelineClip.ClipExtrapolation.None);
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
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.None);
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
            SetExtrapolationModes(timelineClip, TimelineClip.ClipExtrapolation.None, TimelineClip.ClipExtrapolation.None);
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
            SetExtrapolationModes(timelineClip, TimelineClip.ClipExtrapolation.None, TimelineClip.ClipExtrapolation.None);
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
            SetExtrapolationModes(timelineClip, preMode: TimelineClip.ClipExtrapolation.Hold);
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
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Hold);
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
            SetExtrapolationModes(timelineClip, preMode: TimelineClip.ClipExtrapolation.Hold);
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
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Hold);
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
            SetExtrapolationModes(timelineClip, TimelineClip.ClipExtrapolation.Hold, TimelineClip.ClipExtrapolation.Hold);
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
            SetExtrapolationModes(timelineClip, TimelineClip.ClipExtrapolation.Hold, TimelineClip.ClipExtrapolation.Hold);
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
            SetExtrapolationModes(timelineClip, TimelineClip.ClipExtrapolation.Hold, TimelineClip.ClipExtrapolation.Hold);
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
            SetExtrapolationModes(timelineClip, TimelineClip.ClipExtrapolation.Hold, TimelineClip.ClipExtrapolation.Hold);
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

        #region Loop Extrapolation テスト（4.1.4）

        [Test]
        public void TryGetExtrapolatedValue_PreExtrapolationがLoopの場合_クリップ開始前はループした値を返す()
        {
            // Arrange: 0→0, 1→10 の線形カーブ（1秒のクリップ）
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 2.0; // クリップは2秒から開始
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, preMode: TimelineClip.ClipExtrapolation.Loop);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.5秒はクリップ開始前（0.5秒前）
            // ループ: (1.5 - 2.0) = -0.5 → Repeat(-0.5, 1.0) = 0.5 → curve.Evaluate(0.5) = 5
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(5f, value, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_PostExtrapolationがLoopの場合_クリップ終了後はループした値を返す()
        {
            // Arrange: 0→0, 1→10 の線形カーブ（1秒のクリップ）
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0; // クリップは1秒で終了
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Loop);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.5秒はクリップ終了後
            // ループ: (1.5 - 0.0) * 1.0 = 1.5 → Repeat(1.5, 1.0) = 0.5 → curve.Evaluate(0.5) = 5
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(5f, value, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_Loopで2周目の値を正しく計算する()
        {
            // Arrange: 0→0, 1→10 の線形カーブ（1秒のクリップ）
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Loop);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 2.3秒は2周目の0.3秒目
            // ループ: Repeat(2.3, 1.0) = 0.3 → curve.Evaluate(0.3) = 3
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 2.3f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(3f, value, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_LoopとTimeScaleの組み合わせ()
        {
            // Arrange: TimeScale=2で2倍速再生
            var curve = AnimationCurve.Linear(0, 0, 2, 20); // 0→0, 2→20
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0; // Timeline上では1秒
            timelineClip.timeScale = 2.0; // 2倍速で2秒分のアニメーションを再生
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Loop);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.5秒はクリップ終了後
            // ループ: (1.5 - 0.0) * 2.0 = 3.0 → Repeat(3.0, 2.0) = 1.0 → curve.Evaluate(1.0) = 10
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(10f, value, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_LoopとClipInの組み合わせ()
        {
            // Arrange: ClipIn=0.5でトリミング
            var curve = AnimationCurve.Linear(0, 0, 2, 20); // 0→0, 2→20
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            timelineClip.clipIn = 0.5; // ソースクリップの0.5秒からスタート
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Loop);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.5秒はクリップ終了後
            // ループ: (1.5 - 0.0) * 1.0 = 1.5 → Repeat(1.5, 1.0) = 0.5
            // clipIn=0.5なので → curve.Evaluate(0.5 + 0.5) = curve.Evaluate(1.0) = 10
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(10f, value, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_Loopでクリップ範囲内は通常の値を返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 1.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, TimelineClip.ClipExtrapolation.Loop, TimelineClip.ClipExtrapolation.Loop);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.7秒はクリップ範囲内
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.7f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(7f, value, 0.0001f); // 線形補間された値
        }

        [Test]
        public void TryGetExtrapolatedValue_Loop境界での動作確認()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Loop);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act & Assert - ちょうど2秒目（1周完了）
            // ループ: Repeat(2.0, 1.0) = 0.0 → curve.Evaluate(0.0) = 0
            var resultAt2 = _processor.TryGetExtrapolatedValue(curve, clipInfo, 2.0f, out var valueAt2);
            Assert.IsTrue(resultAt2);
            Assert.AreEqual(0f, valueAt2, 0.0001f);

            // Act & Assert - ちょうど3秒目（2周完了）
            var resultAt3 = _processor.TryGetExtrapolatedValue(curve, clipInfo, 3.0f, out var valueAt3);
            Assert.IsTrue(resultAt3);
            Assert.AreEqual(0f, valueAt3, 0.0001f);
        }

        #endregion

        #region PingPong Extrapolation テスト（4.1.5）

        [Test]
        public void TryGetExtrapolatedValue_PreExtrapolationがPingPongの場合_クリップ開始前は往復した値を返す()
        {
            // Arrange: 0→0, 1→10 の線形カーブ（1秒のクリップ）
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 2.0; // クリップは2秒から開始
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, preMode: TimelineClip.ClipExtrapolation.PingPong);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.5秒はクリップ開始前（0.5秒前）
            // PingPong: |(-0.5) * 1.0| = 0.5 → PingPong(0.5, 1.0) = 0.5 → curve.Evaluate(0.5) = 5
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(5f, value, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_PostExtrapolationがPingPongの場合_クリップ終了後は往復した値を返す()
        {
            // Arrange: 0→0, 1→10 の線形カーブ（1秒のクリップ）
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0; // クリップは1秒で終了
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.PingPong);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.5秒はクリップ終了後
            // PingPong: |1.5 * 1.0| = 1.5 → PingPong(1.5, 1.0) = 0.5 → curve.Evaluate(0.5) = 5
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(5f, value, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_PingPongで往路と復路の値を正しく計算する()
        {
            // Arrange: 0→0, 1→10 の線形カーブ（1秒のクリップ）
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.PingPong);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act & Assert - 1.3秒（往路の折り返し後、復路の0.3秒目）
            // PingPong: PingPong(1.3, 1.0) = 0.7 → curve.Evaluate(0.7) = 7
            var result1 = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.3f, out var value1);
            Assert.IsTrue(result1);
            Assert.AreEqual(7f, value1, 0.0001f);

            // Act & Assert - 2.3秒（2往復目の往路0.3秒目）
            // PingPong: PingPong(2.3, 1.0) = 0.3 → curve.Evaluate(0.3) = 3
            var result2 = _processor.TryGetExtrapolatedValue(curve, clipInfo, 2.3f, out var value2);
            Assert.IsTrue(result2);
            Assert.AreEqual(3f, value2, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_PingPongとTimeScaleの組み合わせ()
        {
            // Arrange: TimeScale=2で2倍速再生
            var curve = AnimationCurve.Linear(0, 0, 2, 20); // 0→0, 2→20
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0; // Timeline上では1秒
            timelineClip.timeScale = 2.0; // 2倍速で2秒分のアニメーションを再生
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.PingPong);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.5秒はクリップ終了後
            // PingPong: |1.5 * 2.0| = 3.0 → PingPong(3.0, 2.0) = 1.0 → curve.Evaluate(1.0) = 10
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(10f, value, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_PingPongとClipInの組み合わせ()
        {
            // Arrange: ClipIn=0.5でトリミング
            var curve = AnimationCurve.Linear(0, 0, 2, 20); // 0→0, 2→20
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            timelineClip.clipIn = 0.5; // ソースクリップの0.5秒からスタート
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.PingPong);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.5秒はクリップ終了後
            // PingPong: PingPong(1.5, 1.0) = 0.5
            // clipIn=0.5なので → curve.Evaluate(0.5 + 0.5) = curve.Evaluate(1.0) = 10
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(10f, value, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_PingPongでクリップ範囲内は通常の値を返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 1.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, TimelineClip.ClipExtrapolation.PingPong, TimelineClip.ClipExtrapolation.PingPong);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.7秒はクリップ範囲内
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.7f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(7f, value, 0.0001f); // 線形補間された値
        }

        [Test]
        public void TryGetExtrapolatedValue_PingPong境界での動作確認()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.PingPong);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act & Assert - ちょうど2秒目（1往復完了、折り返し点）
            // PingPong: PingPong(2.0, 1.0) = 0.0 → curve.Evaluate(0.0) = 0
            var resultAt2 = _processor.TryGetExtrapolatedValue(curve, clipInfo, 2.0f, out var valueAt2);
            Assert.IsTrue(resultAt2);
            Assert.AreEqual(0f, valueAt2, 0.0001f);

            // Act & Assert - ちょうど3秒目（復路の終点）
            // PingPong: PingPong(3.0, 1.0) = 1.0 → curve.Evaluate(1.0) = 10
            var resultAt3 = _processor.TryGetExtrapolatedValue(curve, clipInfo, 3.0f, out var valueAt3);
            Assert.IsTrue(resultAt3);
            Assert.AreEqual(10f, valueAt3, 0.0001f);

            // Act & Assert - ちょうど4秒目（2往復完了）
            // PingPong: PingPong(4.0, 1.0) = 0.0 → curve.Evaluate(0.0) = 0
            var resultAt4 = _processor.TryGetExtrapolatedValue(curve, clipInfo, 4.0f, out var valueAt4);
            Assert.IsTrue(resultAt4);
            Assert.AreEqual(0f, valueAt4, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_PingPongとLoopの違いを確認()
        {
            // Arrange: 同じカーブでLoopとPingPongの挙動の違いを確認
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            var timelineClipLoop = _animationTrack.CreateClip(_testClip);
            timelineClipLoop.start = 0.0;
            timelineClipLoop.duration = 1.0;
            SetExtrapolationModes(timelineClipLoop, postMode: TimelineClip.ClipExtrapolation.Loop);
            var clipInfoLoop = new ClipInfo(timelineClipLoop, _testClip);

            var timelineClipPingPong = _animationTrack.CreateClip(_testClip);
            timelineClipPingPong.start = 0.0;
            timelineClipPingPong.duration = 1.0;
            SetExtrapolationModes(timelineClipPingPong, postMode: TimelineClip.ClipExtrapolation.PingPong);
            var clipInfoPingPong = new ClipInfo(timelineClipPingPong, _testClip);

            // Act & Assert - 1.7秒での比較
            // Loop: Repeat(1.7, 1.0) = 0.7 → 7
            // PingPong: PingPong(1.7, 1.0) = 0.3 → 3 (復路なので逆方向)
            _processor.TryGetExtrapolatedValue(curve, clipInfoLoop, 1.7f, out var valueLoop);
            _processor.TryGetExtrapolatedValue(curve, clipInfoPingPong, 1.7f, out var valuePingPong);

            Assert.AreEqual(7f, valueLoop, 0.0001f);
            Assert.AreEqual(3f, valuePingPong, 0.0001f);
            Assert.AreNotEqual(valueLoop, valuePingPong);
        }

        #endregion

        #region Continue Extrapolation テスト（4.1.6）

        [Test]
        public void TryGetExtrapolatedValue_PreExtrapolationがContinueの場合_クリップ開始前は接線延長した値を返す()
        {
            // Arrange: 0→0, 1→10 の線形カーブ（傾き=10）
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 2.0; // クリップは2秒から開始
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, preMode: TimelineClip.ClipExtrapolation.Continue);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.5秒はクリップ開始前（0.5秒前）
            // Continue: firstKeyValue + inTangent * timeDelta = 0 + 10 * (-0.5) = -5
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(-5f, value, 0.0001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_PostExtrapolationがContinueの場合_クリップ終了後は接線延長した値を返す()
        {
            // Arrange: 0→0, 1→10 の線形カーブ（傾き=10）
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0; // クリップは1秒で終了
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Continue);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.5秒はクリップ終了後（0.5秒後）
            // Continue: lastKeyValue + outTangent * timeDelta = 10 + 10 * 0.5 = 15
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(15f, value, 0.01f);
        }

        [Test]
        public void TryGetExtrapolatedValue_Continueで傾きが0のカーブの場合_値が維持される()
        {
            // Arrange: 傾きが0のカーブ（水平）
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0, 5, 0, 0)); // inTangent=0, outTangent=0
            curve.AddKey(new Keyframe(1, 5, 0, 0)); // inTangent=0, outTangent=0
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 1.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, TimelineClip.ClipExtrapolation.Continue, TimelineClip.ClipExtrapolation.Continue);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act & Assert - クリップ開始前
            var resultBefore = _processor.TryGetExtrapolatedValue(curve, clipInfo, 0.5f, out var valueBefore);
            Assert.IsTrue(resultBefore);
            Assert.AreEqual(5f, valueBefore, 0.0001f); // 傾き0なので値は変わらない

            // Act & Assert - クリップ終了後
            var resultAfter = _processor.TryGetExtrapolatedValue(curve, clipInfo, 3.0f, out var valueAfter);
            Assert.IsTrue(resultAfter);
            Assert.AreEqual(5f, valueAfter, 0.0001f); // 傾き0なので値は変わらない
        }

        [Test]
        public void TryGetExtrapolatedValue_Continueで負の傾きのカーブの場合_正しく延長される()
        {
            // Arrange: 負の傾きのカーブ
            var curve = AnimationCurve.Linear(0, 10, 1, 0); // 傾き=-10
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Continue);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 2.0秒はクリップ終了後（1秒後）
            // Continue: lastKeyValue + outTangent * timeDelta = 0 + (-10) * 1.0 = -10
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 2.0f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(-10f, value, 0.01f);
        }

        [Test]
        public void TryGetExtrapolatedValue_ContinueとTimeScaleの組み合わせ()
        {
            // Arrange: TimeScale=2で2倍速再生
            var curve = AnimationCurve.Linear(0, 0, 2, 20); // 傾き=10
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0; // Timeline上では1秒
            timelineClip.timeScale = 2.0; // 2倍速で2秒分のアニメーションを再生
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Continue);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.5秒はクリップ終了後（0.5秒後）
            // lastKeyTime = 0 + 1.0 * 2.0 = 2.0, lastKeyValue = curve.Evaluate(2.0) = 20
            // Continue: lastKeyValue + outTangent * timeDelta = 20 + 10 * 0.5 = 25
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(25f, value, 0.01f);
        }

        [Test]
        public void TryGetExtrapolatedValue_ContinueとClipInの組み合わせ()
        {
            // Arrange: ClipIn=0.5でトリミング
            var curve = AnimationCurve.Linear(0, 0, 2, 20); // 傾き=10
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 1.0;
            timelineClip.duration = 1.0;
            timelineClip.clipIn = 0.5; // ソースクリップの0.5秒からスタート
            SetExtrapolationModes(timelineClip, preMode: TimelineClip.ClipExtrapolation.Continue);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 0.5秒はクリップ開始前（0.5秒前）
            // clipIn=0.5なので、firstKeyValue = curve.Evaluate(0.5) = 5
            // Continue: firstKeyValue + inTangent * timeDelta = 5 + 10 * (-0.5) = 0
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 0.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, value, 0.01f);
        }

        [Test]
        public void TryGetExtrapolatedValue_Continueでクリップ範囲内は通常の値を返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 1.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, TimelineClip.ClipExtrapolation.Continue, TimelineClip.ClipExtrapolation.Continue);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act - 1.7秒はクリップ範囲内
            var result = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.7f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(7f, value, 0.0001f); // 線形補間された値
        }

        [Test]
        public void TryGetExtrapolatedValue_Continue境界での動作確認()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 1.0;
            timelineClip.duration = 1.0; // クリップは1秒〜2秒
            SetExtrapolationModes(timelineClip, TimelineClip.ClipExtrapolation.Continue, TimelineClip.ClipExtrapolation.Continue);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act & Assert - 開始時間（1秒）はクリップ範囲内
            var resultAtStart = _processor.TryGetExtrapolatedValue(curve, clipInfo, 1.0f, out var valueAtStart);
            Assert.IsTrue(resultAtStart);
            Assert.AreEqual(0f, valueAtStart, 0.0001f);

            // Act & Assert - 終了時間（2秒）はクリップ範囲内
            var resultAtEnd = _processor.TryGetExtrapolatedValue(curve, clipInfo, 2.0f, out var valueAtEnd);
            Assert.IsTrue(resultAtEnd);
            Assert.AreEqual(10f, valueAtEnd, 0.0001f);

            // Act & Assert - 開始時間直前
            var resultBeforeStart = _processor.TryGetExtrapolatedValue(curve, clipInfo, 0.999f, out var valueBeforeStart);
            Assert.IsTrue(resultBeforeStart);
            // Continue: 0 + 10 * (-0.001) = -0.01
            Assert.AreEqual(-0.01f, valueBeforeStart, 0.001f);

            // Act & Assert - 終了時間直後
            var resultAfterEnd = _processor.TryGetExtrapolatedValue(curve, clipInfo, 2.001f, out var valueAfterEnd);
            Assert.IsTrue(resultAfterEnd);
            // Continue: 10 + 10 * 0.001 = 10.01
            Assert.AreEqual(10.01f, valueAfterEnd, 0.001f);
        }

        [Test]
        public void TryGetExtrapolatedValue_ContinueとHoldの違いを確認()
        {
            // Arrange: 同じカーブでContinueとHoldの挙動の違いを確認
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            // Hold用のクリップを作成（start=0.0で識別）
            var timelineClipHold = _animationTrack.CreateClip(_testClip);
            timelineClipHold.start = 0.0;
            timelineClipHold.duration = 1.0;
            SetExtrapolationModes(timelineClipHold, postMode: TimelineClip.ClipExtrapolation.Hold);
            var clipInfoHold = new ClipInfo(timelineClipHold, _testClip);

            // Continue用のクリップを作成（start=10.0で識別、Holdと区別するため）
            var timelineClipContinue = _animationTrack.CreateClip(_testClip);
            timelineClipContinue.start = 10.0;
            timelineClipContinue.duration = 1.0;
            SetExtrapolationModes(timelineClipContinue, postMode: TimelineClip.ClipExtrapolation.Continue);
            var clipInfoContinue = new ClipInfo(timelineClipContinue, _testClip);

            // Act & Assert - Hold: 2.0秒での評価、Continue: 12.0秒での評価
            // Hold: 最後の値をそのまま維持 → 10
            // Continue: 接線を延長 → 10 + 10 * 1.0 = 20
            _processor.TryGetExtrapolatedValue(curve, clipInfoHold, 2.0f, out var valueHold);
            _processor.TryGetExtrapolatedValue(curve, clipInfoContinue, 12.0f, out var valueContinue);

            Assert.AreEqual(10f, valueHold, 0.0001f);
            Assert.AreEqual(20f, valueContinue, 0.01f);
            Assert.AreNotEqual(valueHold, valueContinue);
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

        #region Gap検出テスト（4.2.1）

        [Test]
        public void DetectGaps_nullを渡すと空のリストを返す()
        {
            // Act
            var gaps = _processor.DetectGaps(null);

            // Assert
            Assert.IsNotNull(gaps);
            Assert.AreEqual(0, gaps.Count);
        }

        [Test]
        public void DetectGaps_空のリストを渡すと空のリストを返す()
        {
            // Act
            var gaps = _processor.DetectGaps(new System.Collections.Generic.List<ClipInfo>());

            // Assert
            Assert.IsNotNull(gaps);
            Assert.AreEqual(0, gaps.Count);
        }

        [Test]
        public void DetectGaps_クリップが1つの場合は空のリストを返す()
        {
            // Arrange
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo };

            // Act
            var gaps = _processor.DetectGaps(clipInfos);

            // Assert
            Assert.IsNotNull(gaps);
            Assert.AreEqual(0, gaps.Count);
        }

        [Test]
        public void DetectGaps_連続した2つのクリップ間にGapがない場合は空のリストを返す()
        {
            // Arrange: クリップ1 (0-1秒) とクリップ2 (1-2秒) が隙間なく連続
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            var testClip2 = new AnimationClip();
            testClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip2 = _animationTrack.CreateClip(testClip2);
            timelineClip2.start = 1.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, testClip2);

            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo1, clipInfo2 };

            // Act
            var gaps = _processor.DetectGaps(clipInfos);

            // Assert
            Assert.IsNotNull(gaps);
            Assert.AreEqual(0, gaps.Count);

            Object.DestroyImmediate(testClip2);
        }

        [Test]
        public void DetectGaps_2つのクリップ間にGapがある場合は検出する()
        {
            // Arrange: クリップ1 (0-1秒) とクリップ2 (2-3秒) の間に1秒のGap
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            var testClip2 = new AnimationClip();
            testClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip2 = _animationTrack.CreateClip(testClip2);
            timelineClip2.start = 2.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, testClip2);

            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo1, clipInfo2 };

            // Act
            var gaps = _processor.DetectGaps(clipInfos);

            // Assert
            Assert.AreEqual(1, gaps.Count);
            Assert.AreEqual(1.0, gaps[0].StartTime, 0.0001);
            Assert.AreEqual(2.0, gaps[0].EndTime, 0.0001);
            Assert.AreEqual(1.0, gaps[0].Duration, 0.0001);
            Assert.IsTrue(gaps[0].IsValid);
            Assert.AreSame(clipInfo1, gaps[0].PreviousClip);
            Assert.AreSame(clipInfo2, gaps[0].NextClip);

            Object.DestroyImmediate(testClip2);
        }

        [Test]
        public void DetectGaps_ソートされていないクリップリストでも正しくGapを検出する()
        {
            // Arrange: クリップ2 (2-3秒)、クリップ1 (0-1秒) の順で追加（逆順）
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));

            var testClip2 = new AnimationClip();
            testClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip2 = _animationTrack.CreateClip(testClip2);
            timelineClip2.start = 2.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, testClip2);

            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            // 逆順でリストに追加
            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo2, clipInfo1 };

            // Act
            var gaps = _processor.DetectGaps(clipInfos);

            // Assert
            Assert.AreEqual(1, gaps.Count);
            Assert.AreEqual(1.0, gaps[0].StartTime, 0.0001);
            Assert.AreEqual(2.0, gaps[0].EndTime, 0.0001);

            Object.DestroyImmediate(testClip2);
        }

        [Test]
        public void DetectGaps_複数のGapを検出する()
        {
            // Arrange: クリップ1 (0-1秒)、クリップ2 (2-3秒)、クリップ3 (4-5秒)
            // Gap1: 1-2秒、Gap2: 3-4秒
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            var testClip2 = new AnimationClip();
            testClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip2 = _animationTrack.CreateClip(testClip2);
            timelineClip2.start = 2.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, testClip2);

            var testClip3 = new AnimationClip();
            testClip3.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip3 = _animationTrack.CreateClip(testClip3);
            timelineClip3.start = 4.0;
            timelineClip3.duration = 1.0;
            var clipInfo3 = new ClipInfo(timelineClip3, testClip3);

            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo1, clipInfo2, clipInfo3 };

            // Act
            var gaps = _processor.DetectGaps(clipInfos);

            // Assert
            Assert.AreEqual(2, gaps.Count);

            // Gap1: 1-2秒
            Assert.AreEqual(1.0, gaps[0].StartTime, 0.0001);
            Assert.AreEqual(2.0, gaps[0].EndTime, 0.0001);
            Assert.AreSame(clipInfo1, gaps[0].PreviousClip);
            Assert.AreSame(clipInfo2, gaps[0].NextClip);

            // Gap2: 3-4秒
            Assert.AreEqual(3.0, gaps[1].StartTime, 0.0001);
            Assert.AreEqual(4.0, gaps[1].EndTime, 0.0001);
            Assert.AreSame(clipInfo2, gaps[1].PreviousClip);
            Assert.AreSame(clipInfo3, gaps[1].NextClip);

            Object.DestroyImmediate(testClip2);
            Object.DestroyImmediate(testClip3);
        }

        [Test]
        public void DetectGaps_重なりのあるクリップはGapとして検出しない()
        {
            // Arrange: クリップ1 (0-2秒) とクリップ2 (1-3秒) が重なっている
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 2.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            var testClip2 = new AnimationClip();
            testClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip2 = _animationTrack.CreateClip(testClip2);
            timelineClip2.start = 1.0;
            timelineClip2.duration = 2.0;
            var clipInfo2 = new ClipInfo(timelineClip2, testClip2);

            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo1, clipInfo2 };

            // Act
            var gaps = _processor.DetectGaps(clipInfos);

            // Assert
            Assert.AreEqual(0, gaps.Count);

            Object.DestroyImmediate(testClip2);
        }

        [Test]
        public void DetectGaps_nullのClipInfoはスキップする()
        {
            // Arrange: クリップ1、null、クリップ2
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            var testClip2 = new AnimationClip();
            testClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip2 = _animationTrack.CreateClip(testClip2);
            timelineClip2.start = 2.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, testClip2);

            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo1, null, clipInfo2 };

            // Act
            var gaps = _processor.DetectGaps(clipInfos);

            // Assert
            Assert.AreEqual(1, gaps.Count);
            Assert.AreEqual(1.0, gaps[0].StartTime, 0.0001);
            Assert.AreEqual(2.0, gaps[0].EndTime, 0.0001);

            Object.DestroyImmediate(testClip2);
        }

        [Test]
        public void TryGetGapAtTime_nullを渡すとfalseを返す()
        {
            // Act
            var result = _processor.TryGetGapAtTime(null, 1.5, out var gapInfo);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void TryGetGapAtTime_Gap内の時間ではtrueを返しGap情報を取得できる()
        {
            // Arrange: クリップ1 (0-1秒) とクリップ2 (2-3秒) の間に1秒のGap
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            var testClip2 = new AnimationClip();
            testClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip2 = _animationTrack.CreateClip(testClip2);
            timelineClip2.start = 2.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, testClip2);

            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo1, clipInfo2 };
            var gaps = _processor.DetectGaps(clipInfos);

            // Act - Gap内の時間（1.5秒）
            var result = _processor.TryGetGapAtTime(gaps, 1.5, out var gapInfo);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1.0, gapInfo.StartTime, 0.0001);
            Assert.AreEqual(2.0, gapInfo.EndTime, 0.0001);

            Object.DestroyImmediate(testClip2);
        }

        [Test]
        public void TryGetGapAtTime_Gap外の時間ではfalseを返す()
        {
            // Arrange: クリップ1 (0-1秒) とクリップ2 (2-3秒) の間に1秒のGap
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            var testClip2 = new AnimationClip();
            testClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip2 = _animationTrack.CreateClip(testClip2);
            timelineClip2.start = 2.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, testClip2);

            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo1, clipInfo2 };
            var gaps = _processor.DetectGaps(clipInfos);

            // Act - クリップ内の時間（0.5秒）
            var result = _processor.TryGetGapAtTime(gaps, 0.5, out var gapInfo);

            // Assert
            Assert.IsFalse(result);

            Object.DestroyImmediate(testClip2);
        }

        [Test]
        public void TryGetGapAtTime_Gap開始時間ではtrueを返す()
        {
            // Arrange
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            var testClip2 = new AnimationClip();
            testClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip2 = _animationTrack.CreateClip(testClip2);
            timelineClip2.start = 2.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, testClip2);

            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo1, clipInfo2 };
            var gaps = _processor.DetectGaps(clipInfos);

            // Act - Gap開始時間（1.0秒）
            var result = _processor.TryGetGapAtTime(gaps, 1.0, out var gapInfo);

            // Assert
            Assert.IsTrue(result);

            Object.DestroyImmediate(testClip2);
        }

        [Test]
        public void TryGetGapAtTime_Gap終了時間ではfalseを返す()
        {
            // Arrange
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            var testClip2 = new AnimationClip();
            testClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip2 = _animationTrack.CreateClip(testClip2);
            timelineClip2.start = 2.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, testClip2);

            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo1, clipInfo2 };
            var gaps = _processor.DetectGaps(clipInfos);

            // Act - Gap終了時間（2.0秒）は次のクリップの開始時間なのでfalse
            var result = _processor.TryGetGapAtTime(gaps, 2.0, out var gapInfo);

            // Assert
            Assert.IsFalse(result);

            Object.DestroyImmediate(testClip2);
        }

        [Test]
        public void CalculateGapDuration_2つのクリップ間のGap時間を計算する()
        {
            // Arrange: クリップ1 (0-1秒) とクリップ2 (2-3秒)
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            var testClip2 = new AnimationClip();
            testClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip2 = _animationTrack.CreateClip(testClip2);
            timelineClip2.start = 2.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, testClip2);

            // Act
            var gapDuration = _processor.CalculateGapDuration(clipInfo1, clipInfo2);

            // Assert
            Assert.AreEqual(1.0, gapDuration, 0.0001);

            Object.DestroyImmediate(testClip2);
        }

        [Test]
        public void CalculateGapDuration_連続したクリップ間は0を返す()
        {
            // Arrange: クリップ1 (0-1秒) とクリップ2 (1-2秒)
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            var testClip2 = new AnimationClip();
            testClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip2 = _animationTrack.CreateClip(testClip2);
            timelineClip2.start = 1.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, testClip2);

            // Act
            var gapDuration = _processor.CalculateGapDuration(clipInfo1, clipInfo2);

            // Assert
            Assert.AreEqual(0.0, gapDuration, 0.0001);

            Object.DestroyImmediate(testClip2);
        }

        [Test]
        public void CalculateGapDuration_重なりがある場合は負の値を返す()
        {
            // Arrange: クリップ1 (0-2秒) とクリップ2 (1-3秒) が重なっている
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 2.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            var testClip2 = new AnimationClip();
            testClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip2 = _animationTrack.CreateClip(testClip2);
            timelineClip2.start = 1.0;
            timelineClip2.duration = 2.0;
            var clipInfo2 = new ClipInfo(timelineClip2, testClip2);

            // Act
            var gapDuration = _processor.CalculateGapDuration(clipInfo1, clipInfo2);

            // Assert
            Assert.AreEqual(-1.0, gapDuration, 0.0001);

            Object.DestroyImmediate(testClip2);
        }

        [Test]
        public void CalculateGapDuration_nullクリップの場合は0を返す()
        {
            // Arrange
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            // Act & Assert
            Assert.AreEqual(0.0, _processor.CalculateGapDuration(null, clipInfo1), 0.0001);
            Assert.AreEqual(0.0, _processor.CalculateGapDuration(clipInfo1, null), 0.0001);
            Assert.AreEqual(0.0, _processor.CalculateGapDuration(null, null), 0.0001);
        }

        [Test]
        public void GapInfo_プロパティが正しく設定される()
        {
            // Arrange
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip1 = _animationTrack.CreateClip(_testClip);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, _testClip);

            var testClip2 = new AnimationClip();
            testClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip2 = _animationTrack.CreateClip(testClip2);
            timelineClip2.start = 2.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, testClip2);

            // Act
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo1, clipInfo2);

            // Assert
            Assert.AreEqual(1.0, gapInfo.StartTime, 0.0001);
            Assert.AreEqual(2.0, gapInfo.EndTime, 0.0001);
            Assert.AreEqual(1.0, gapInfo.Duration, 0.0001);
            Assert.IsTrue(gapInfo.IsValid);
            Assert.AreSame(clipInfo1, gapInfo.PreviousClip);
            Assert.AreSame(clipInfo2, gapInfo.NextClip);

            Object.DestroyImmediate(testClip2);
        }

        [Test]
        public void GapInfo_Durationが0以下の場合はIsValidがfalseになる()
        {
            // Act & Assert
            var gapInfoZero = new GapInfo(1.0, 1.0, null, null);
            Assert.IsFalse(gapInfoZero.IsValid);
            Assert.AreEqual(0.0, gapInfoZero.Duration, 0.0001);

            var gapInfoNegative = new GapInfo(2.0, 1.0, null, null);
            Assert.IsFalse(gapInfoNegative.IsValid);
            Assert.AreEqual(-1.0, gapInfoNegative.Duration, 0.0001);
        }

        #endregion

        #region Gap区間の補間処理テスト（4.2.2）

        [Test]
        public void FillGapWithExtrapolation_nullカーブを渡すとnullを返す()
        {
            // Arrange
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act
            var result = _processor.FillGapWithExtrapolation(null, gapInfo);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void FillGapWithExtrapolation_空のカーブを渡すとnullを返す()
        {
            // Arrange
            var emptyCurve = new AnimationCurve();
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act
            var result = _processor.FillGapWithExtrapolation(emptyCurve, gapInfo);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void FillGapWithExtrapolation_無効なGapInfoを渡すとnullを返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            var invalidGapInfo = new GapInfo(2.0, 1.0, null, null); // Duration < 0

            // Act
            var result = _processor.FillGapWithExtrapolation(curve, invalidGapInfo);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void FillGapWithExtrapolation_PreviousClipがnullの場合はnullを返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            var gapInfo = new GapInfo(1.0, 2.0, null, null);

            // Act
            var result = _processor.FillGapWithExtrapolation(curve, gapInfo);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void FillGapWithExtrapolation_PostExtrapolationがNoneの場合はnullを返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.None);
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act
            var result = _processor.FillGapWithExtrapolation(curve, gapInfo);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void FillGapWithExtrapolation_PostExtrapolationがHoldの場合はGap区間全体で最後の値を維持する()
        {
            // Arrange: クリップ (0-1秒)、Gap (1-2秒)
            var curve = AnimationCurve.Linear(0, 0, 1, 10); // 0→0, 1→10
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Hold);
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act
            var result = _processor.FillGapWithExtrapolation(curve, gapInfo);

            // Assert
            Assert.IsNotNull(result);
            Assert.Greater(result.keys.Length, 0);

            // Gap区間内の値はすべて10（Hold）
            foreach (var key in result.keys)
            {
                Assert.GreaterOrEqual(key.time, 1.0f);
                Assert.Less(key.time, 2.0f);
                Assert.AreEqual(10f, key.value, 0.0001f);
            }
        }

        [Test]
        public void FillGapWithExtrapolation_PostExtrapolationがLoopの場合はGap区間でループする()
        {
            // Arrange: クリップ (0-1秒)、Gap (1-2秒)
            var curve = AnimationCurve.Linear(0, 0, 1, 10); // 0→0, 1→10
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Loop);
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act
            var result = _processor.FillGapWithExtrapolation(curve, gapInfo);

            // Assert
            Assert.IsNotNull(result);
            Assert.Greater(result.keys.Length, 0);

            // Gap区間内でループが正しく行われることを確認
            // 例: 1.5秒 → Repeat(1.5, 1.0) = 0.5 → curve.Evaluate(0.5) = 5
            var midKey = FindKeyNearTime(result, 1.5f);
            if (midKey.HasValue)
            {
                Assert.AreEqual(5f, midKey.Value.value, 0.5f); // フレームレートによる誤差を許容
            }
        }

        [Test]
        public void FillGapWithExtrapolation_PostExtrapolationがContinueの場合はGap区間で接線延長する()
        {
            // Arrange: クリップ (0-1秒)、Gap (1-2秒)
            var curve = AnimationCurve.Linear(0, 0, 1, 10); // 傾き=10
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Continue);
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act
            var result = _processor.FillGapWithExtrapolation(curve, gapInfo);

            // Assert
            Assert.IsNotNull(result);
            Assert.Greater(result.keys.Length, 0);

            // Gap区間内で接線延長が正しく行われることを確認
            // 例: 1.5秒 → lastKeyValue + outTangent * timeDelta = 10 + 10 * 0.5 = 15
            var midKey = FindKeyNearTime(result, 1.5f);
            if (midKey.HasValue)
            {
                Assert.AreEqual(15f, midKey.Value.value, 0.5f); // フレームレートによる誤差を許容
            }
        }

        [Test]
        public void FillGapWithExtrapolation_フレームレートに基づいてサンプリングされる()
        {
            // Arrange: フレームレート30fps、Gap 1秒 → 約30サンプル
            _processor.SetFrameRate(30f);
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Hold);
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act
            var result = _processor.FillGapWithExtrapolation(curve, gapInfo);

            // Assert
            Assert.IsNotNull(result);
            // 30fpsで1秒のGapなので約30サンプル（最後のキーフレーム追加を含む）
            Assert.GreaterOrEqual(result.keys.Length, 29);
            Assert.LessOrEqual(result.keys.Length, 32);
        }

        [Test]
        public void TryGetGapInterpolatedValue_nullカーブを渡すとfalseを返す()
        {
            // Arrange
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act
            var result = _processor.TryGetGapInterpolatedValue(null, gapInfo, 1.5f, out var value);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0f, value);
        }

        [Test]
        public void TryGetGapInterpolatedValue_無効なGapInfoを渡すとfalseを返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            var invalidGapInfo = new GapInfo(2.0, 1.0, null, null);

            // Act
            var result = _processor.TryGetGapInterpolatedValue(curve, invalidGapInfo, 1.5f, out var value);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0f, value);
        }

        [Test]
        public void TryGetGapInterpolatedValue_Gap区間外の時間ではfalseを返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Hold);
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act & Assert - Gap開始前
            var resultBefore = _processor.TryGetGapInterpolatedValue(curve, gapInfo, 0.5f, out var valueBefore);
            Assert.IsFalse(resultBefore);

            // Act & Assert - Gap終了後（終了時間を含まない）
            var resultAfter = _processor.TryGetGapInterpolatedValue(curve, gapInfo, 2.0f, out var valueAfter);
            Assert.IsFalse(resultAfter);
        }

        [Test]
        public void TryGetGapInterpolatedValue_Gap区間内の時間でHold設定の場合は正しい値を返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Hold);
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act
            var result = _processor.TryGetGapInterpolatedValue(curve, gapInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(10f, value, 0.0001f); // Holdなので最後の値を維持
        }

        [Test]
        public void TryGetGapInterpolatedValue_Gap区間内の時間でLoop設定の場合は正しい値を返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Loop);
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act - 1.5秒 → Repeat(1.5, 1.0) = 0.5 → curve.Evaluate(0.5) = 5
            var result = _processor.TryGetGapInterpolatedValue(curve, gapInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(5f, value, 0.0001f);
        }

        [Test]
        public void TryGetGapInterpolatedValue_Gap区間内の時間でContinue設定の場合は正しい値を返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10); // 傾き=10
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Continue);
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act - 1.5秒 → lastKeyValue + outTangent * timeDelta = 10 + 10 * 0.5 = 15
            var result = _processor.TryGetGapInterpolatedValue(curve, gapInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(15f, value, 0.01f);
        }

        [Test]
        public void TryGetGapInterpolatedValue_Gap区間内の時間でPingPong設定の場合は正しい値を返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.PingPong);
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act - 1.5秒 → PingPong(1.5, 1.0) = 0.5 → curve.Evaluate(0.5) = 5
            var result = _processor.TryGetGapInterpolatedValue(curve, gapInfo, 1.5f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(5f, value, 0.0001f);
        }

        [Test]
        public void TryGetGapInterpolatedValue_Gap区間内の時間でNone設定の場合はfalseを返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.None);
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act
            var result = _processor.TryGetGapInterpolatedValue(curve, gapInfo, 1.5f, out var value);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0f, value);
        }

        [Test]
        public void TryGetGapInterpolatedValue_Gap開始時間ではtrueを返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = _animationTrack.CreateClip(_testClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            SetExtrapolationModes(timelineClip, postMode: TimelineClip.ClipExtrapolation.Hold);
            var clipInfo = new ClipInfo(timelineClip, _testClip);
            var gapInfo = new GapInfo(1.0, 2.0, clipInfo, null);

            // Act - Gap開始時間ぴったり
            var result = _processor.TryGetGapInterpolatedValue(curve, gapInfo, 1.0f, out var value);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(10f, value, 0.0001f);
        }

        /// <summary>
        /// 指定した時間に近いキーフレームを検索するヘルパーメソッド
        /// </summary>
        private Keyframe? FindKeyNearTime(AnimationCurve curve, float time)
        {
            if (curve == null || curve.keys.Length == 0)
            {
                return null;
            }

            const float tolerance = 0.05f; // 50ms以内
            foreach (var key in curve.keys)
            {
                if (Mathf.Abs(key.time - time) < tolerance)
                {
                    return key;
                }
            }
            return null;
        }

        #endregion
    }
}
