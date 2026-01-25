using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using AnimationMergeTool.Editor.Domain;
using AnimationMergeTool.Editor.Domain.Models;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// ClipMergerクラスの単体テスト
    /// </summary>
    public class ClipMergerTests
    {
        private ClipMerger _clipMerger;
        private AnimationClip _testClip;

        [SetUp]
        public void SetUp()
        {
            _clipMerger = new ClipMerger();
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
        }

        #region GetAnimationCurves テスト

        [Test]
        public void GetAnimationCurves_nullを渡すと空のリストを返す()
        {
            // Arrange & Act
            var result = _clipMerger.GetAnimationCurves(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAnimationCurves_カーブのないクリップは空のリストを返す()
        {
            // Arrange & Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAnimationCurves_単一のカーブを持つクリップから取得できる()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            // Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("localPosition.x", result[0].Binding.propertyName);
            Assert.AreEqual(typeof(Transform), result[0].Binding.type);
            Assert.IsNotNull(result[0].Curve);
        }

        [Test]
        public void GetAnimationCurves_複数のカーブを持つクリップから全て取得できる()
        {
            // Arrange
            var curveX = AnimationCurve.Linear(0, 0, 1, 1);
            var curveY = AnimationCurve.Linear(0, 0, 1, 2);
            var curveZ = AnimationCurve.Linear(0, 0, 1, 3);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curveX);
            _testClip.SetCurve("", typeof(Transform), "localPosition.y", curveY);
            _testClip.SetCurve("", typeof(Transform), "localPosition.z", curveZ);

            // Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void GetAnimationCurves_異なるパスのカーブを取得できる()
        {
            // Arrange
            var curveRoot = AnimationCurve.Linear(0, 0, 1, 1);
            var curveChild = AnimationCurve.Linear(0, 0, 1, 2);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curveRoot);
            _testClip.SetCurve("Child/GrandChild", typeof(Transform), "localPosition.x", curveChild);

            // Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.AreEqual(2, result.Count);
            // パスが異なることを確認
            var paths = new System.Collections.Generic.HashSet<string>();
            foreach (var pair in result)
            {
                paths.Add(pair.Binding.path);
            }
            Assert.IsTrue(paths.Contains(""));
            Assert.IsTrue(paths.Contains("Child/GrandChild"));
        }

        [Test]
        public void GetAnimationCurves_EditorCurveBinding情報が正しく取得できる()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("TestPath", typeof(Transform), "localScale.x", curve);

            // Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("TestPath", result[0].Binding.path);
            Assert.AreEqual("localScale.x", result[0].Binding.propertyName);
            Assert.AreEqual(typeof(Transform), result[0].Binding.type);
        }

        [Test]
        public void GetAnimationCurves_カーブのキーフレーム情報が保持される()
        {
            // Arrange
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0f);
            curve.AddKey(0.5f, 1f);
            curve.AddKey(1f, 0.5f);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            // Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(3, result[0].Curve.keys.Length);
            Assert.AreEqual(0f, result[0].Curve.keys[0].time, 0.0001f);
            Assert.AreEqual(0.5f, result[0].Curve.keys[1].time, 0.0001f);
            Assert.AreEqual(1f, result[0].Curve.keys[2].time, 0.0001f);
        }

        #endregion

        #region GetAnimationCurve テスト

        [Test]
        public void GetAnimationCurve_nullクリップを渡すとnullを返す()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "localPosition.x");

            // Act
            var result = _clipMerger.GetAnimationCurve(null, binding);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetAnimationCurve_存在しないバインディングはnullを返す()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "localPosition.x");

            // Act
            var result = _clipMerger.GetAnimationCurve(_testClip, binding);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetAnimationCurve_存在するバインディングのカーブを取得できる()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "localPosition.x");

            // Act
            var result = _clipMerger.GetAnimationCurve(_testClip, binding);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.keys.Length);
        }

        [Test]
        public void GetAnimationCurve_正しいパスのバインディングでのみ取得できる()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("CorrectPath", typeof(Transform), "localPosition.x", curve);
            var correctBinding = EditorCurveBinding.FloatCurve("CorrectPath", typeof(Transform), "localPosition.x");
            var wrongBinding = EditorCurveBinding.FloatCurve("WrongPath", typeof(Transform), "localPosition.x");

            // Act
            var correctResult = _clipMerger.GetAnimationCurve(_testClip, correctBinding);
            var wrongResult = _clipMerger.GetAnimationCurve(_testClip, wrongBinding);

            // Assert
            Assert.IsNotNull(correctResult);
            Assert.IsNull(wrongResult);
        }

        #endregion

        #region CurveBindingPair テスト

        [Test]
        public void CurveBindingPair_コンストラクタでバインディングとカーブが設定される()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("TestPath", typeof(Transform), "localPosition.x");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);

            // Act
            var pair = new CurveBindingPair(binding, curve);

            // Assert
            Assert.AreEqual("TestPath", pair.Binding.path);
            Assert.AreEqual("localPosition.x", pair.Binding.propertyName);
            Assert.AreEqual(typeof(Transform), pair.Binding.type);
            Assert.IsNotNull(pair.Curve);
        }

        #endregion

        #region ApplyTimeOffset テスト

        private TimelineAsset _timelineAsset;
        private AnimationTrack _animationTrack;

        private void SetUpTimelineForTimeOffsetTests()
        {
            _timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
            _animationTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Test Track");
        }

        private void TearDownTimelineForTimeOffsetTests()
        {
            if (_timelineAsset != null)
            {
                Object.DestroyImmediate(_timelineAsset);
            }
        }

        [Test]
        public void ApplyTimeOffset_nullカーブを渡すとnullを返す()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var timelineClip = _animationTrack.CreateClip(_testClip);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act
            var result = _clipMerger.ApplyTimeOffset(null, clipInfo);

            // Assert
            Assert.IsNull(result);

            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void ApplyTimeOffset_nullClipInfoを渡すとnullを返す()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);

            // Act
            var result = _clipMerger.ApplyTimeOffset(curve, null);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void ApplyTimeOffset_空のカーブを渡すと空のカーブを返す()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var curve = new AnimationCurve();
            var timelineClip = _animationTrack.CreateClip(_testClip);
            var clipInfo = new ClipInfo(timelineClip, _testClip);

            // Act
            var result = _clipMerger.ApplyTimeOffset(curve, clipInfo);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.keys.Length);

            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void ApplyTimeOffset_開始時間オフセットが正しく適用される()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0f);
            curve.AddKey(1f, 1f);

            // 開始時間を2秒に設定
            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 2.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, animClip);

            // Act
            var result = _clipMerger.ApplyTimeOffset(curve, clipInfo);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.keys.Length);
            Assert.AreEqual(2f, result.keys[0].time, 0.0001f); // 0 + 2 = 2
            Assert.AreEqual(3f, result.keys[1].time, 0.0001f); // 1 + 2 = 3

            Object.DestroyImmediate(animClip);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void ApplyTimeOffset_ClipInが正しく適用される()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0f);
            curve.AddKey(0.5f, 0.5f);
            curve.AddKey(1f, 1f);
            curve.AddKey(1.5f, 1.5f);
            curve.AddKey(2f, 2f);

            // ClipInを0.5秒に設定（最初の0.5秒をトリミング）
            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 2, 2));
            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 0.0;
            timelineClip.clipIn = 0.5;
            timelineClip.duration = 1.5;
            var clipInfo = new ClipInfo(timelineClip, animClip);

            // Act
            var result = _clipMerger.ApplyTimeOffset(curve, clipInfo);

            // Assert
            Assert.IsNotNull(result);
            // ClipIn=0.5以降のキー（0.5, 1.0, 1.5, 2.0）のうち、duration=1.5以内のもの
            // 0.5 -> (0.5-0.5)/1 = 0, duration内なのでOK
            // 1.0 -> (1.0-0.5)/1 = 0.5, duration内なのでOK
            // 1.5 -> (1.5-0.5)/1 = 1.0, duration内なのでOK
            // 2.0 -> (2.0-0.5)/1 = 1.5, duration内なのでOK
            Assert.AreEqual(4, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].time, 0.0001f);   // (0.5 - 0.5) = 0
            Assert.AreEqual(0.5f, result.keys[1].time, 0.0001f); // (1.0 - 0.5) = 0.5
            Assert.AreEqual(1f, result.keys[2].time, 0.0001f);   // (1.5 - 0.5) = 1.0
            Assert.AreEqual(1.5f, result.keys[3].time, 0.0001f); // (2.0 - 0.5) = 1.5

            Object.DestroyImmediate(animClip);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void ApplyTimeOffset_TimeScaleが正しく適用される()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0f);
            curve.AddKey(1f, 1f);
            curve.AddKey(2f, 2f);

            // TimeScaleを2に設定（2倍速再生）
            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 2, 2));
            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 0.0;
            timelineClip.timeScale = 2.0;
            timelineClip.duration = 1.0; // 2倍速なので元の2秒のアニメが1秒で再生される
            var clipInfo = new ClipInfo(timelineClip, animClip);

            // Act
            var result = _clipMerger.ApplyTimeOffset(curve, clipInfo);

            // Assert
            Assert.IsNotNull(result);
            // TimeScale=2なので、元のtime / 2 がTimeline上の時間になる
            // 0 -> 0/2 = 0, duration内なのでOK
            // 1 -> 1/2 = 0.5, duration内なのでOK
            // 2 -> 2/2 = 1.0, duration内なのでOK
            Assert.AreEqual(3, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].time, 0.0001f);
            Assert.AreEqual(0.5f, result.keys[1].time, 0.0001f);
            Assert.AreEqual(1f, result.keys[2].time, 0.0001f);

            Object.DestroyImmediate(animClip);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void ApplyTimeOffset_ClipInとTimeScaleと開始時間が複合的に適用される()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0f);
            curve.AddKey(1f, 1f);
            curve.AddKey(2f, 2f);
            curve.AddKey(3f, 3f);
            curve.AddKey(4f, 4f);

            // 開始時間=5, ClipIn=1, TimeScale=2
            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 4, 4));
            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 5.0;
            timelineClip.clipIn = 1.0;
            timelineClip.timeScale = 2.0;
            timelineClip.duration = 1.5; // 2倍速で1.5秒 = 元の3秒分
            var clipInfo = new ClipInfo(timelineClip, animClip);

            // Act
            var result = _clipMerger.ApplyTimeOffset(curve, clipInfo);

            // Assert
            Assert.IsNotNull(result);
            // ClipIn=1以降のキー（1, 2, 3, 4）
            // 1 -> (1-1)/2 = 0, 0 <= 1.5なのでOK -> 5 + 0 = 5
            // 2 -> (2-1)/2 = 0.5, 0.5 <= 1.5なのでOK -> 5 + 0.5 = 5.5
            // 3 -> (3-1)/2 = 1.0, 1.0 <= 1.5なのでOK -> 5 + 1.0 = 6.0
            // 4 -> (4-1)/2 = 1.5, 1.5 <= 1.5なのでOK -> 5 + 1.5 = 6.5
            Assert.AreEqual(4, result.keys.Length);
            Assert.AreEqual(5f, result.keys[0].time, 0.0001f);
            Assert.AreEqual(5.5f, result.keys[1].time, 0.0001f);
            Assert.AreEqual(6f, result.keys[2].time, 0.0001f);
            Assert.AreEqual(6.5f, result.keys[3].time, 0.0001f);

            Object.DestroyImmediate(animClip);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void ApplyTimeOffset_キーフレームの値は変更されない()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var curve = new AnimationCurve();
            curve.AddKey(0f, 10f);
            curve.AddKey(1f, 20f);
            curve.AddKey(2f, 30f);

            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 2, 2));
            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 3.0;
            timelineClip.duration = 2.0;
            var clipInfo = new ClipInfo(timelineClip, animClip);

            // Act
            var result = _clipMerger.ApplyTimeOffset(curve, clipInfo);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.keys.Length);
            Assert.AreEqual(10f, result.keys[0].value, 0.0001f);
            Assert.AreEqual(20f, result.keys[1].value, 0.0001f);
            Assert.AreEqual(30f, result.keys[2].value, 0.0001f);

            Object.DestroyImmediate(animClip);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void ApplyTimeOffset_タンジェントがTimeScaleに応じて調整される()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var curve = new AnimationCurve();
            var key = new Keyframe(0f, 0f)
            {
                inTangent = 1f,
                outTangent = 2f
            };
            curve.AddKey(key);

            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 0.0;
            timelineClip.timeScale = 2.0;
            timelineClip.duration = 0.5;
            var clipInfo = new ClipInfo(timelineClip, animClip);

            // Act
            var result = _clipMerger.ApplyTimeOffset(curve, clipInfo);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.keys.Length);
            // TimeScale=2なので、タンジェントは2倍になる
            Assert.AreEqual(2f, result.keys[0].inTangent, 0.0001f);
            Assert.AreEqual(4f, result.keys[0].outTangent, 0.0001f);

            Object.DestroyImmediate(animClip);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void ApplyTimeOffset_Durationを超えるキーは除外される()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0f);
            curve.AddKey(1f, 1f);
            curve.AddKey(2f, 2f);
            curve.AddKey(3f, 3f);

            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 3, 3));
            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.5; // 1.5秒までのキーのみ
            var clipInfo = new ClipInfo(timelineClip, animClip);

            // Act
            var result = _clipMerger.ApplyTimeOffset(curve, clipInfo);

            // Assert
            Assert.IsNotNull(result);
            // 0, 1のみ（2, 3はduration=1.5を超えるので除外）
            Assert.AreEqual(2, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].time, 0.0001f);
            Assert.AreEqual(1f, result.keys[1].time, 0.0001f);

            Object.DestroyImmediate(animClip);
            TearDownTimelineForTimeOffsetTests();
        }

        #endregion

        #region Merge テスト（カーブ統合機能）

        [Test]
        public void Merge_nullを渡すとnullを返す()
        {
            // Act
            var result = _clipMerger.Merge(null);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void Merge_空のリストを渡すとnullを返す()
        {
            // Act
            var result = _clipMerger.Merge(new System.Collections.Generic.List<ClipInfo>());

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void Merge_単一のClipInfoからカーブを統合できる()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, animClip);
            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo };

            // Act
            var result = _clipMerger.Merge(clipInfos);

            // Assert
            Assert.IsNotNull(result);
            var resultCurves = _clipMerger.GetAnimationCurves(result);
            Assert.AreEqual(1, resultCurves.Count);
            Assert.AreEqual("localPosition.x", resultCurves[0].Binding.propertyName);

            Object.DestroyImmediate(animClip);
            Object.DestroyImmediate(result);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void Merge_複数のClipInfoから異なるプロパティのカーブを統合できる()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var animClip1 = new AnimationClip();
            animClip1.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var animClip2 = new AnimationClip();
            animClip2.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, 0, 1, 2));

            var timelineClip1 = _animationTrack.CreateClip(animClip1);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, animClip1);

            var timelineClip2 = _animationTrack.CreateClip(animClip2);
            timelineClip2.start = 1.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, animClip2);

            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo1, clipInfo2 };

            // Act
            var result = _clipMerger.Merge(clipInfos);

            // Assert
            Assert.IsNotNull(result);
            var resultCurves = _clipMerger.GetAnimationCurves(result);
            Assert.AreEqual(2, resultCurves.Count);

            Object.DestroyImmediate(animClip1);
            Object.DestroyImmediate(animClip2);
            Object.DestroyImmediate(result);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void Merge_同じプロパティのカーブは時間軸上で統合される()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var animClip1 = new AnimationClip();
            animClip1.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var animClip2 = new AnimationClip();
            animClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 2, 1, 3));

            var timelineClip1 = _animationTrack.CreateClip(animClip1);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, animClip1);

            var timelineClip2 = _animationTrack.CreateClip(animClip2);
            timelineClip2.start = 2.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, animClip2);

            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo1, clipInfo2 };

            // Act
            var result = _clipMerger.Merge(clipInfos);

            // Assert
            Assert.IsNotNull(result);
            var resultCurves = _clipMerger.GetAnimationCurves(result);
            Assert.AreEqual(1, resultCurves.Count); // 同じプロパティは1つに統合
            // 2つのクリップから各2キー = 4キー
            Assert.AreEqual(4, resultCurves[0].Curve.keys.Length);

            Object.DestroyImmediate(animClip1);
            Object.DestroyImmediate(animClip2);
            Object.DestroyImmediate(result);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void Merge_時間オフセットが正しく適用される()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));

            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 5.0; // 5秒から開始
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, animClip);
            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo };

            // Act
            var result = _clipMerger.Merge(clipInfos);

            // Assert
            Assert.IsNotNull(result);
            var resultCurves = _clipMerger.GetAnimationCurves(result);
            Assert.AreEqual(1, resultCurves.Count);
            // キーは5秒と6秒に配置される
            Assert.AreEqual(5f, resultCurves[0].Curve.keys[0].time, 0.0001f);
            Assert.AreEqual(6f, resultCurves[0].Curve.keys[1].time, 0.0001f);

            Object.DestroyImmediate(animClip);
            Object.DestroyImmediate(result);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void Merge_フレームレートが設定される()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            _clipMerger.SetFrameRate(30f);
            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));

            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, animClip);
            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo };

            // Act
            var result = _clipMerger.Merge(clipInfos);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(30f, result.frameRate, 0.0001f);

            Object.DestroyImmediate(animClip);
            Object.DestroyImmediate(result);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void Merge_nullのClipInfoは無視される()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));

            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, animClip);
            var clipInfos = new System.Collections.Generic.List<ClipInfo> { null, clipInfo, null };

            // Act
            var result = _clipMerger.Merge(clipInfos);

            // Assert
            Assert.IsNotNull(result);
            var resultCurves = _clipMerger.GetAnimationCurves(result);
            Assert.AreEqual(1, resultCurves.Count);

            Object.DestroyImmediate(animClip);
            Object.DestroyImmediate(result);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void Merge_異なるパスのカーブは別々に統合される()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var animClip1 = new AnimationClip();
            animClip1.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            var animClip2 = new AnimationClip();
            animClip2.SetCurve("Child", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 2));

            var timelineClip1 = _animationTrack.CreateClip(animClip1);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, animClip1);

            var timelineClip2 = _animationTrack.CreateClip(animClip2);
            timelineClip2.start = 0.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, animClip2);

            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo1, clipInfo2 };

            // Act
            var result = _clipMerger.Merge(clipInfos);

            // Assert
            Assert.IsNotNull(result);
            var resultCurves = _clipMerger.GetAnimationCurves(result);
            Assert.AreEqual(2, resultCurves.Count); // パスが違うので別々のカーブ

            Object.DestroyImmediate(animClip1);
            Object.DestroyImmediate(animClip2);
            Object.DestroyImmediate(result);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void Merge_同じ時間のキーフレームは後のClipInfoのものが優先される()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var animClip1 = new AnimationClip();
            var curve1 = new AnimationCurve();
            curve1.AddKey(0f, 10f);
            animClip1.SetCurve("", typeof(Transform), "localPosition.x", curve1);

            var animClip2 = new AnimationClip();
            var curve2 = new AnimationCurve();
            curve2.AddKey(0f, 20f); // 同じ時間（0秒）に異なる値
            animClip2.SetCurve("", typeof(Transform), "localPosition.x", curve2);

            var timelineClip1 = _animationTrack.CreateClip(animClip1);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, animClip1);

            var timelineClip2 = _animationTrack.CreateClip(animClip2);
            timelineClip2.start = 0.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, animClip2);

            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo1, clipInfo2 };

            // Act
            var result = _clipMerger.Merge(clipInfos);

            // Assert
            Assert.IsNotNull(result);
            var resultCurves = _clipMerger.GetAnimationCurves(result);
            Assert.AreEqual(1, resultCurves.Count);
            Assert.AreEqual(1, resultCurves[0].Curve.keys.Length);
            // 後から処理されたclipInfo2の値が優先される
            Assert.AreEqual(20f, resultCurves[0].Curve.keys[0].value, 0.0001f);

            Object.DestroyImmediate(animClip1);
            Object.DestroyImmediate(animClip2);
            Object.DestroyImmediate(result);
            TearDownTimelineForTimeOffsetTests();
        }

        #endregion

        #region フレームレート設定機能テスト

        [Test]
        public void GetFrameRateFromTimeline_nullを渡すと0を返す()
        {
            // Act
            var result = ClipMerger.GetFrameRateFromTimeline(null);

            // Assert
            Assert.AreEqual(0f, result, 0.0001f);
        }

        [Test]
        public void GetFrameRateFromTimeline_TimelineAssetからフレームレートを取得できる()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();

            // Act
            var result = ClipMerger.GetFrameRateFromTimeline(_timelineAsset);

            // Assert
            // TimelineAssetのデフォルトフレームレートは60fps
            Assert.IsTrue(result > 0f);

            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void SetFrameRateFromTimeline_nullを渡しても例外が発生しない()
        {
            // Arrange
            _clipMerger.SetFrameRate(30f);

            // Act & Assert (例外が発生しないことを確認)
            Assert.DoesNotThrow(() => _clipMerger.SetFrameRateFromTimeline(null));
            // フレームレートは変更されない
            Assert.AreEqual(30f, _clipMerger.GetFrameRate(), 0.0001f);
        }

        [Test]
        public void SetFrameRateFromTimeline_TimelineAssetからフレームレートが設定される()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            _clipMerger.SetFrameRate(30f); // 初期値を設定

            // Act
            _clipMerger.SetFrameRateFromTimeline(_timelineAsset);

            // Assert
            // TimelineAssetのデフォルトフレームレートに変更される
            var expectedFrameRate = (float)_timelineAsset.editorSettings.frameRate;
            Assert.AreEqual(expectedFrameRate, _clipMerger.GetFrameRate(), 0.0001f);

            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void Merge_TimelineAssetのフレームレートが適用されたAnimationClipを生成できる()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            _clipMerger.SetFrameRateFromTimeline(_timelineAsset);

            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));

            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, animClip);
            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo };

            // Act
            var result = _clipMerger.Merge(clipInfos);

            // Assert
            Assert.IsNotNull(result);
            var expectedFrameRate = (float)_timelineAsset.editorSettings.frameRate;
            Assert.AreEqual(expectedFrameRate, result.frameRate, 0.0001f);

            Object.DestroyImmediate(animClip);
            Object.DestroyImmediate(result);
            TearDownTimelineForTimeOffsetTests();
        }

        #endregion

        #region ルートモーションカーブ統合テスト

        [Test]
        public void GetAnimationCurves_RootTカーブを取得できる()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, binding, curve);

            // Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("RootT.x", result[0].Binding.propertyName);
            Assert.AreEqual(typeof(Animator), result[0].Binding.type);
        }

        [Test]
        public void GetAnimationCurves_RootQカーブを取得できる()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.w");
            var curve = AnimationCurve.Linear(0, 1, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, binding, curve);

            // Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("RootQ.w", result[0].Binding.propertyName);
            Assert.AreEqual(typeof(Animator), result[0].Binding.type);
        }

        [Test]
        public void GetAnimationCurves_ルートモーションカーブと通常カーブを同時に取得できる()
        {
            // Arrange
            var bindingRootT = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");
            var bindingRootQ = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.x");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, bindingRootT, curve);
            AnimationUtility.SetEditorCurve(_testClip, bindingRootQ, curve);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            // Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void Merge_ルートモーションカーブを統合できる()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var animClip = new AnimationClip();
            var bindingTx = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");
            var bindingTy = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.y");
            var bindingTz = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.z");
            AnimationUtility.SetEditorCurve(animClip, bindingTx, AnimationCurve.Linear(0, 0, 1, 1));
            AnimationUtility.SetEditorCurve(animClip, bindingTy, AnimationCurve.Linear(0, 0, 1, 0.5f));
            AnimationUtility.SetEditorCurve(animClip, bindingTz, AnimationCurve.Linear(0, 0, 1, 2));

            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, animClip);
            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo };

            // Act
            var result = _clipMerger.Merge(clipInfos);

            // Assert
            Assert.IsNotNull(result);
            var resultCurves = _clipMerger.GetAnimationCurves(result);
            Assert.AreEqual(3, resultCurves.Count);

            Object.DestroyImmediate(animClip);
            Object.DestroyImmediate(result);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void Merge_ルートモーション回転カーブを統合できる()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var animClip = new AnimationClip();
            var bindingQx = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.x");
            var bindingQy = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.y");
            var bindingQz = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.z");
            var bindingQw = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.w");
            AnimationUtility.SetEditorCurve(animClip, bindingQx, AnimationCurve.Linear(0, 0, 1, 0));
            AnimationUtility.SetEditorCurve(animClip, bindingQy, AnimationCurve.Linear(0, 0, 1, 0.707f));
            AnimationUtility.SetEditorCurve(animClip, bindingQz, AnimationCurve.Linear(0, 0, 1, 0));
            AnimationUtility.SetEditorCurve(animClip, bindingQw, AnimationCurve.Linear(0, 1, 1, 0.707f));

            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, animClip);
            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo };

            // Act
            var result = _clipMerger.Merge(clipInfos);

            // Assert
            Assert.IsNotNull(result);
            var resultCurves = _clipMerger.GetAnimationCurves(result);
            Assert.AreEqual(4, resultCurves.Count);

            Object.DestroyImmediate(animClip);
            Object.DestroyImmediate(result);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void Merge_複数クリップのルートモーションカーブを時間軸上で統合できる()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var animClip1 = new AnimationClip();
            var binding1 = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");
            AnimationUtility.SetEditorCurve(animClip1, binding1, AnimationCurve.Linear(0, 0, 1, 1));

            var animClip2 = new AnimationClip();
            var binding2 = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");
            AnimationUtility.SetEditorCurve(animClip2, binding2, AnimationCurve.Linear(0, 1, 1, 2));

            var timelineClip1 = _animationTrack.CreateClip(animClip1);
            timelineClip1.start = 0.0;
            timelineClip1.duration = 1.0;
            var clipInfo1 = new ClipInfo(timelineClip1, animClip1);

            var timelineClip2 = _animationTrack.CreateClip(animClip2);
            timelineClip2.start = 2.0;
            timelineClip2.duration = 1.0;
            var clipInfo2 = new ClipInfo(timelineClip2, animClip2);

            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo1, clipInfo2 };

            // Act
            var result = _clipMerger.Merge(clipInfos);

            // Assert
            Assert.IsNotNull(result);
            var resultCurves = _clipMerger.GetAnimationCurves(result);
            Assert.AreEqual(1, resultCurves.Count);
            // 2つのクリップから各2キー = 4キー
            Assert.AreEqual(4, resultCurves[0].Curve.keys.Length);
            // 時間が正しくオフセットされている
            Assert.AreEqual(0f, resultCurves[0].Curve.keys[0].time, 0.0001f);
            Assert.AreEqual(1f, resultCurves[0].Curve.keys[1].time, 0.0001f);
            Assert.AreEqual(2f, resultCurves[0].Curve.keys[2].time, 0.0001f);
            Assert.AreEqual(3f, resultCurves[0].Curve.keys[3].time, 0.0001f);

            Object.DestroyImmediate(animClip1);
            Object.DestroyImmediate(animClip2);
            Object.DestroyImmediate(result);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void Merge_ルートモーションカーブと通常カーブを同時に統合できる()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var animClip = new AnimationClip();
            var bindingRootT = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");
            AnimationUtility.SetEditorCurve(animClip, bindingRootT, AnimationCurve.Linear(0, 0, 1, 1));
            animClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 2));

            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 0.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, animClip);
            var clipInfos = new System.Collections.Generic.List<ClipInfo> { clipInfo };

            // Act
            var result = _clipMerger.Merge(clipInfos);

            // Assert
            Assert.IsNotNull(result);
            var resultCurves = _clipMerger.GetAnimationCurves(result);
            Assert.AreEqual(2, resultCurves.Count);

            Object.DestroyImmediate(animClip);
            Object.DestroyImmediate(result);
            TearDownTimelineForTimeOffsetTests();
        }

        [Test]
        public void ApplyTimeOffset_ルートモーションカーブに時間オフセットが正しく適用される()
        {
            // Arrange
            SetUpTimelineForTimeOffsetTests();
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0f);
            curve.AddKey(1f, 1f);

            var animClip = new AnimationClip();
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");
            AnimationUtility.SetEditorCurve(animClip, binding, AnimationCurve.Linear(0, 0, 1, 1));

            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 3.0;
            timelineClip.duration = 1.0;
            var clipInfo = new ClipInfo(timelineClip, animClip);

            // Act
            var result = _clipMerger.ApplyTimeOffset(curve, clipInfo);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.keys.Length);
            Assert.AreEqual(3f, result.keys[0].time, 0.0001f);
            Assert.AreEqual(4f, result.keys[1].time, 0.0001f);

            Object.DestroyImmediate(animClip);
            TearDownTimelineForTimeOffsetTests();
        }

        #endregion
    }
}
