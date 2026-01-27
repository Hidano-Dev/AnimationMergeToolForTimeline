using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using AnimationMergeTool.Editor.Application;
using AnimationMergeTool.Editor.Domain;
using AnimationMergeTool.Editor.Domain.Models;
using Debug = UnityEngine.Debug;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// パフォーマンス検証テスト
    /// P16-008: 大量カーブ・長時間アニメーションでのパフォーマンス検証
    /// </summary>
    public class PerformanceTests
    {
        private List<string> _createdAssetPaths;
        private ClipMerger _clipMerger;
        private TimelineAsset _timelineAsset;
        private AnimationTrack _animationTrack;
        private List<AnimationClip> _createdClips;

        // パフォーマンス閾値（ミリ秒）
        private const int LargeCurveCountThresholdMs = 5000; // 大量カーブ処理の上限
        private const int LongDurationThresholdMs = 3000;     // 長時間アニメーション処理の上限
        private const int BlendShapeThresholdMs = 3000;       // BlendShape大量処理の上限
        private const int IntegrationThresholdMs = 10000;     // 統合テストの上限

        [SetUp]
        public void SetUp()
        {
            _createdAssetPaths = new List<string>();
            _createdClips = new List<AnimationClip>();
            _clipMerger = new ClipMerger();
            _timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
            _animationTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Test Track");
        }

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

            // 作成したAnimationClipを破棄
            foreach (var clip in _createdClips)
            {
                if (clip != null)
                {
                    Object.DestroyImmediate(clip);
                }
            }
            _createdClips.Clear();

            // TimelineAssetを破棄
            if (_timelineAsset != null)
            {
                Object.DestroyImmediate(_timelineAsset);
            }
        }

        #region 大量カーブ パフォーマンステスト

        [Test]
        public void パフォーマンス_100カーブのAnimationClipマージ処理が閾値内で完了する()
        {
            // Arrange
            const int curveCount = 100;
            var clipInfos = CreateClipInfosWithManyCurves(curveCount, 1.0);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _clipMerger.Merge(clipInfos);

            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(result, "マージ結果がnullであってはならない");

            var bindings = AnimationUtility.GetCurveBindings(result);
            Assert.AreEqual(curveCount, bindings.Length, $"{curveCount}カーブが生成されるべき");

            Debug.Log($"[パフォーマンス] 100カーブのマージ処理: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, LargeCurveCountThresholdMs,
                $"処理時間が閾値({LargeCurveCountThresholdMs}ms)を超過しています: {stopwatch.ElapsedMilliseconds}ms");

            Object.DestroyImmediate(result);
        }

        [Test]
        public void パフォーマンス_500カーブのAnimationClipマージ処理が閾値内で完了する()
        {
            // Arrange
            const int curveCount = 500;
            var clipInfos = CreateClipInfosWithManyCurves(curveCount, 1.0);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _clipMerger.Merge(clipInfos);

            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(result, "マージ結果がnullであってはならない");

            var bindings = AnimationUtility.GetCurveBindings(result);
            Assert.AreEqual(curveCount, bindings.Length, $"{curveCount}カーブが生成されるべき");

            Debug.Log($"[パフォーマンス] 500カーブのマージ処理: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, LargeCurveCountThresholdMs,
                $"処理時間が閾値({LargeCurveCountThresholdMs}ms)を超過しています: {stopwatch.ElapsedMilliseconds}ms");

            Object.DestroyImmediate(result);
        }

        [Test]
        public void パフォーマンス_1000カーブのAnimationClipマージ処理が閾値内で完了する()
        {
            // Arrange
            const int curveCount = 1000;
            var clipInfos = CreateClipInfosWithManyCurves(curveCount, 1.0);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _clipMerger.Merge(clipInfos);

            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(result, "マージ結果がnullであってはならない");

            var bindings = AnimationUtility.GetCurveBindings(result);
            Assert.AreEqual(curveCount, bindings.Length, $"{curveCount}カーブが生成されるべき");

            Debug.Log($"[パフォーマンス] 1000カーブのマージ処理: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, LargeCurveCountThresholdMs,
                $"処理時間が閾値({LargeCurveCountThresholdMs}ms)を超過しています: {stopwatch.ElapsedMilliseconds}ms");

            Object.DestroyImmediate(result);
        }

        #endregion

        #region 長時間アニメーション パフォーマンステスト

        [Test]
        public void パフォーマンス_60秒アニメーションのマージ処理が閾値内で完了する()
        {
            // Arrange
            const double duration = 60.0; // 60秒
            const int curveCount = 10;
            var clipInfos = CreateClipInfosWithManyCurves(curveCount, duration);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _clipMerger.Merge(clipInfos);

            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(result, "マージ結果がnullであってはならない");
            Assert.AreEqual((float)duration, result.length, 0.01f, "クリップの長さが正しくない");

            Debug.Log($"[パフォーマンス] 60秒アニメーションのマージ処理: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, LongDurationThresholdMs,
                $"処理時間が閾値({LongDurationThresholdMs}ms)を超過しています: {stopwatch.ElapsedMilliseconds}ms");

            Object.DestroyImmediate(result);
        }

        [Test]
        public void パフォーマンス_300秒アニメーションのマージ処理が閾値内で完了する()
        {
            // Arrange
            const double duration = 300.0; // 5分
            const int curveCount = 10;
            var clipInfos = CreateClipInfosWithManyCurves(curveCount, duration);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _clipMerger.Merge(clipInfos);

            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(result, "マージ結果がnullであってはならない");
            Assert.AreEqual((float)duration, result.length, 0.01f, "クリップの長さが正しくない");

            Debug.Log($"[パフォーマンス] 300秒（5分）アニメーションのマージ処理: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, LongDurationThresholdMs,
                $"処理時間が閾値({LongDurationThresholdMs}ms)を超過しています: {stopwatch.ElapsedMilliseconds}ms");

            Object.DestroyImmediate(result);
        }

        [Test]
        public void パフォーマンス_600秒アニメーションのマージ処理が閾値内で完了する()
        {
            // Arrange
            const double duration = 600.0; // 10分
            const int curveCount = 10;
            var clipInfos = CreateClipInfosWithManyCurves(curveCount, duration);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _clipMerger.Merge(clipInfos);

            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(result, "マージ結果がnullであってはならない");
            Assert.AreEqual((float)duration, result.length, 0.01f, "クリップの長さが正しくない");

            Debug.Log($"[パフォーマンス] 600秒（10分）アニメーションのマージ処理: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, LongDurationThresholdMs,
                $"処理時間が閾値({LongDurationThresholdMs}ms)を超過しています: {stopwatch.ElapsedMilliseconds}ms");

            Object.DestroyImmediate(result);
        }

        #endregion

        #region BlendShape大量カーブ パフォーマンステスト

        [Test]
        public void パフォーマンス_100BlendShapeカーブのマージ処理が閾値内で完了する()
        {
            // Arrange
            const int blendShapeCount = 100;
            var clipInfos = CreateClipInfosWithManyBlendShapes(blendShapeCount, 1.0);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _clipMerger.Merge(clipInfos);

            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(result, "マージ結果がnullであってはならない");

            var bindings = AnimationUtility.GetCurveBindings(result);
            Assert.AreEqual(blendShapeCount, bindings.Length, $"{blendShapeCount}BlendShapeカーブが生成されるべき");

            Debug.Log($"[パフォーマンス] 100BlendShapeカーブのマージ処理: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, BlendShapeThresholdMs,
                $"処理時間が閾値({BlendShapeThresholdMs}ms)を超過しています: {stopwatch.ElapsedMilliseconds}ms");

            Object.DestroyImmediate(result);
        }

        [Test]
        public void パフォーマンス_500BlendShapeカーブのマージ処理が閾値内で完了する()
        {
            // Arrange
            const int blendShapeCount = 500;
            var clipInfos = CreateClipInfosWithManyBlendShapes(blendShapeCount, 1.0);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _clipMerger.Merge(clipInfos);

            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(result, "マージ結果がnullであってはならない");

            var bindings = AnimationUtility.GetCurveBindings(result);
            Assert.AreEqual(blendShapeCount, bindings.Length, $"{blendShapeCount}BlendShapeカーブが生成されるべき");

            Debug.Log($"[パフォーマンス] 500BlendShapeカーブのマージ処理: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, BlendShapeThresholdMs,
                $"処理時間が閾値({BlendShapeThresholdMs}ms)を超過しています: {stopwatch.ElapsedMilliseconds}ms");

            Object.DestroyImmediate(result);
        }

        #endregion

        #region 複合条件 パフォーマンステスト

        [Test]
        public void パフォーマンス_大量カーブかつ長時間アニメーションのマージ処理が閾値内で完了する()
        {
            // Arrange: 200カーブ x 120秒アニメーション
            const int curveCount = 200;
            const double duration = 120.0;
            var clipInfos = CreateClipInfosWithManyCurves(curveCount, duration);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _clipMerger.Merge(clipInfos);

            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(result, "マージ結果がnullであってはならない");

            var bindings = AnimationUtility.GetCurveBindings(result);
            Assert.AreEqual(curveCount, bindings.Length, $"{curveCount}カーブが生成されるべき");
            Assert.AreEqual((float)duration, result.length, 0.01f, "クリップの長さが正しくない");

            Debug.Log($"[パフォーマンス] 200カーブ x 120秒アニメーションのマージ処理: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, IntegrationThresholdMs,
                $"処理時間が閾値({IntegrationThresholdMs}ms)を超過しています: {stopwatch.ElapsedMilliseconds}ms");

            Object.DestroyImmediate(result);
        }

        [Test]
        public void パフォーマンス_TransformとBlendShape混合カーブのマージ処理が閾値内で完了する()
        {
            // Arrange: 100 Transformカーブ + 100 BlendShapeカーブ
            const int transformCurveCount = 100;
            const int blendShapeCurveCount = 100;
            const double duration = 60.0;

            var clipInfos = CreateMixedClipInfos(transformCurveCount, blendShapeCurveCount, duration);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _clipMerger.Merge(clipInfos);

            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(result, "マージ結果がnullであってはならない");

            var bindings = AnimationUtility.GetCurveBindings(result);
            Assert.AreEqual(transformCurveCount + blendShapeCurveCount, bindings.Length,
                $"{transformCurveCount + blendShapeCurveCount}カーブが生成されるべき");

            Debug.Log($"[パフォーマンス] 混合カーブ({transformCurveCount}Transform + {blendShapeCurveCount}BlendShape) x {duration}秒のマージ処理: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, IntegrationThresholdMs,
                $"処理時間が閾値({IntegrationThresholdMs}ms)を超過しています: {stopwatch.ElapsedMilliseconds}ms");

            Object.DestroyImmediate(result);
        }

        [Test]
        public void パフォーマンス_複数トラックからの統合処理が閾値内で完了する()
        {
            // Arrange: 10トラック x 各50カーブ
            const int trackCount = 10;
            const int curvesPerTrack = 50;
            const double duration = 30.0;

            var clipInfos = CreateMultiTrackClipInfos(trackCount, curvesPerTrack, duration);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = _clipMerger.Merge(clipInfos);

            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(result, "マージ結果がnullであってはならない");

            // 各トラックで異なるプロパティを持つため、総カーブ数はトラック数 x カーブ数
            var bindings = AnimationUtility.GetCurveBindings(result);
            Assert.AreEqual(trackCount * curvesPerTrack, bindings.Length,
                $"{trackCount * curvesPerTrack}カーブが生成されるべき");

            Debug.Log($"[パフォーマンス] {trackCount}トラック x 各{curvesPerTrack}カーブ x {duration}秒のマージ処理: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, IntegrationThresholdMs,
                $"処理時間が閾値({IntegrationThresholdMs}ms)を超過しています: {stopwatch.ElapsedMilliseconds}ms");

            Object.DestroyImmediate(result);
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// 指定カーブ数のClipInfoリストを作成
        /// </summary>
        private List<ClipInfo> CreateClipInfosWithManyCurves(int curveCount, double duration)
        {
            var animClip = new AnimationClip();
            animClip.name = $"PerformanceTest_{curveCount}Curves";
            _createdClips.Add(animClip);

            for (int i = 0; i < curveCount; i++)
            {
                var curve = AnimationCurve.Linear(0, 0, (float)duration, 1);
                var binding = EditorCurveBinding.FloatCurve($"Object_{i}", typeof(Transform), "m_LocalPosition.x");
                AnimationUtility.SetEditorCurve(animClip, binding, curve);
            }

            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 0;
            timelineClip.duration = duration;
            var clipInfo = new ClipInfo(timelineClip, animClip);
            return new List<ClipInfo> { clipInfo };
        }

        /// <summary>
        /// 指定BlendShapeカーブ数のClipInfoリストを作成
        /// </summary>
        private List<ClipInfo> CreateClipInfosWithManyBlendShapes(int blendShapeCount, double duration)
        {
            var animClip = new AnimationClip();
            animClip.name = $"PerformanceTest_{blendShapeCount}BlendShapes";
            _createdClips.Add(animClip);

            for (int i = 0; i < blendShapeCount; i++)
            {
                var curve = AnimationCurve.Linear(0, 0, (float)duration, 100);
                var binding = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), $"blendShape.Shape_{i}");
                AnimationUtility.SetEditorCurve(animClip, binding, curve);
            }

            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 0;
            timelineClip.duration = duration;
            var clipInfo = new ClipInfo(timelineClip, animClip);
            return new List<ClipInfo> { clipInfo };
        }

        /// <summary>
        /// TransformとBlendShape混合のClipInfoリストを作成
        /// </summary>
        private List<ClipInfo> CreateMixedClipInfos(int transformCount, int blendShapeCount, double duration)
        {
            var animClip = new AnimationClip();
            animClip.name = $"PerformanceTest_Mixed_{transformCount}T_{blendShapeCount}BS";
            _createdClips.Add(animClip);

            // Transformカーブを追加
            for (int i = 0; i < transformCount; i++)
            {
                var curve = AnimationCurve.Linear(0, 0, (float)duration, 1);
                var binding = EditorCurveBinding.FloatCurve($"Bone_{i}", typeof(Transform), "m_LocalPosition.x");
                AnimationUtility.SetEditorCurve(animClip, binding, curve);
            }

            // BlendShapeカーブを追加
            for (int i = 0; i < blendShapeCount; i++)
            {
                var curve = AnimationCurve.Linear(0, 0, (float)duration, 100);
                var binding = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), $"blendShape.Shape_{i}");
                AnimationUtility.SetEditorCurve(animClip, binding, curve);
            }

            var timelineClip = _animationTrack.CreateClip(animClip);
            timelineClip.start = 0;
            timelineClip.duration = duration;
            var clipInfo = new ClipInfo(timelineClip, animClip);
            return new List<ClipInfo> { clipInfo };
        }

        /// <summary>
        /// 複数トラックをシミュレートしたClipInfoリストを作成
        /// </summary>
        private List<ClipInfo> CreateMultiTrackClipInfos(int trackCount, int curvesPerTrack, double duration)
        {
            var clipInfos = new List<ClipInfo>();

            for (int t = 0; t < trackCount; t++)
            {
                var animClip = new AnimationClip();
                animClip.name = $"PerformanceTest_Track{t}";
                _createdClips.Add(animClip);

                for (int c = 0; c < curvesPerTrack; c++)
                {
                    var curve = AnimationCurve.Linear(0, 0, (float)duration, 1);
                    // 各トラックで異なるパスを持つカーブを生成（オーバーライドを避ける）
                    var binding = EditorCurveBinding.FloatCurve($"Track{t}_Object_{c}", typeof(Transform), "m_LocalPosition.x");
                    AnimationUtility.SetEditorCurve(animClip, binding, curve);
                }

                var timelineClip = _animationTrack.CreateClip(animClip);
                timelineClip.start = 0;
                timelineClip.duration = duration;
                var clipInfo = new ClipInfo(timelineClip, animClip);
                clipInfos.Add(clipInfo);
            }

            return clipInfos;
        }

        #endregion
    }
}
