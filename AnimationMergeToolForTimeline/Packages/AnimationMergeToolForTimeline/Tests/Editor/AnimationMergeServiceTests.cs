using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using AnimationMergeTool.Editor.Application;
using AnimationMergeTool.Editor.Domain.Models;
using AnimationMergeTool.Editor.Infrastructure;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// AnimationMergeServiceクラスの単体テスト
    /// タスク8.1.2: 処理オーケストレーション機能のテスト
    /// TrackAnalyzer → ClipMerger → ExtrapolationProcessor → BlendProcessor → CurveOverrider → Exporter
    /// の流れで処理をオーケストレーションすることを確認
    /// </summary>
    public class AnimationMergeServiceTests
    {
        private AnimationMergeService _service;
        private List<string> _createdAssetPaths;

        [SetUp]
        public void SetUp()
        {
            _service = new AnimationMergeService();
            _createdAssetPaths = new List<string>();
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
        }

        #region MergeFromPlayableDirector テスト

        [Test]
        public void MergeFromPlayableDirector_PlayableDirectorがnullの場合空のリストを返す()
        {
            // Act
            var results = _service.MergeFromPlayableDirector(null);

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void MergeFromPlayableDirector_TimelineAssetが未設定の場合空のリストを返す()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            director.playableAsset = null;

            try
            {
                // Act
                var results = _service.MergeFromPlayableDirector(director);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(0, results.Count);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void MergeFromPlayableDirector_バインドされたAnimatorがない場合空のリストを返す()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");
            director.playableAsset = timeline;
            // Animatorをバインドしない

            try
            {
                // Act
                var results = _service.MergeFromPlayableDirector(director);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(0, results.Count);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void MergeFromPlayableDirector_有効な設定でAnimationClipを生成できる()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("TestAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");

            // AnimationClipを作成してトラックに追加
            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            var asset = timelineClip.asset as AnimationPlayableAsset;
            asset.clip = animClip;

            director.playableAsset = timeline;
            director.SetGenericBinding(track, animator);

            try
            {
                // Act
                var results = _service.MergeFromPlayableDirector(director);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);
                Assert.IsNotNull(results[0].GeneratedClip);
                Assert.AreSame(animator, results[0].TargetAnimator);

                // 保存されたパスを記録
                foreach (var log in results[0].Logs)
                {
                    if (log.Contains("出力完了:") || log.Contains("アセットを保存しました"))
                    {
                        var parts = log.Split(':');
                        if (parts.Length > 1)
                        {
                            var path = parts[parts.Length - 1].Trim();
                            if (path.EndsWith(".anim"))
                            {
                                _createdAssetPaths.Add(path);
                            }
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(animatorGo);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        [Test]
        public void MergeFromPlayableDirector_Muteトラックはスキップされる()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("TestAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            // Muteしていないトラック
            var track1 = timeline.CreateTrack<AnimationTrack>(null, "ActiveTrack");
            var animClip1 = new AnimationClip();
            var curve1 = AnimationCurve.Linear(0, 0, 1, 1);
            animClip1.SetCurve("", typeof(Transform), "localPosition.x", curve1);
            var timelineClip1 = track1.CreateClip<AnimationPlayableAsset>();
            timelineClip1.start = 0;
            timelineClip1.duration = 1;
            (timelineClip1.asset as AnimationPlayableAsset).clip = animClip1;

            // Muteしたトラック
            var track2 = timeline.CreateTrack<AnimationTrack>(null, "MutedTrack");
            track2.muted = true;
            var animClip2 = new AnimationClip();
            var curve2 = AnimationCurve.Linear(0, 0, 1, 2);
            animClip2.SetCurve("", typeof(Transform), "localPosition.y", curve2);
            var timelineClip2 = track2.CreateClip<AnimationPlayableAsset>();
            timelineClip2.start = 0;
            timelineClip2.duration = 1;
            (timelineClip2.asset as AnimationPlayableAsset).clip = animClip2;

            director.playableAsset = timeline;
            director.SetGenericBinding(track1, animator);
            director.SetGenericBinding(track2, animator);

            try
            {
                // Act
                var results = _service.MergeFromPlayableDirector(director);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(1, results.Count);

                // Muteされたトラック（localPosition.y）は含まれないことを確認
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(1, bindings.Length);
                Assert.AreEqual("localPosition.x", bindings[0].propertyName);

                // クリーンアップ用にパスを記録
                foreach (var log in results[0].Logs)
                {
                    if (log.Contains(".anim"))
                    {
                        var parts = log.Split(' ');
                        foreach (var part in parts)
                        {
                            if (part.EndsWith(".anim"))
                            {
                                _createdAssetPaths.Add(part);
                            }
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(animatorGo);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip1);
                Object.DestroyImmediate(animClip2);
            }
        }

        #endregion

        #region MergeFromTimelineAsset テスト

        [Test]
        public void MergeFromTimelineAsset_TimelineAssetがnullの場合空のリストを返す()
        {
            // Act
            var results = _service.MergeFromTimelineAsset(null);

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void MergeFromTimelineAsset_AnimationTrackがない場合空のリストを返す()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            try
            {
                // Act
                var results = _service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(0, results.Count);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void MergeFromTimelineAsset_有効なTimelineAssetからAnimationClipを生成できる()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");

            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            var asset = timelineClip.asset as AnimationPlayableAsset;
            asset.clip = animClip;

            try
            {
                // Act
                var results = _service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);
                Assert.IsNotNull(results[0].GeneratedClip);
                Assert.IsNull(results[0].TargetAnimator); // TimelineAssetのみの場合はAnimatorがnull

                // クリーンアップ用にパスを記録
                foreach (var log in results[0].Logs)
                {
                    if (log.Contains(".anim"))
                    {
                        var parts = log.Split(' ');
                        foreach (var part in parts)
                        {
                            if (part.EndsWith(".anim"))
                            {
                                _createdAssetPaths.Add(part);
                            }
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        [Test]
        public void MergeFromTimelineAsset_全てのトラックがMuteの場合空のリストを返す()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var track = timeline.CreateTrack<AnimationTrack>(null, "MutedTrack");
            track.muted = true;

            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            try
            {
                // Act
                var results = _service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(0, results.Count);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        #endregion

        #region 処理オーケストレーション テスト

        [Test]
        public void 処理オーケストレーション_複数トラックの優先順位に基づいてOverride処理が適用される()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            // 低優先順位トラック（上の段、先に作成）
            var track1 = timeline.CreateTrack<AnimationTrack>(null, "LowPriorityTrack");
            var animClip1 = new AnimationClip();
            var curve1 = AnimationCurve.Linear(0, 0, 1, 1); // 0から1へ
            animClip1.SetCurve("", typeof(Transform), "localPosition.x", curve1);
            var timelineClip1 = track1.CreateClip<AnimationPlayableAsset>();
            timelineClip1.start = 0;
            timelineClip1.duration = 2;
            (timelineClip1.asset as AnimationPlayableAsset).clip = animClip1;

            // 高優先順位トラック（下の段、後に作成）
            var track2 = timeline.CreateTrack<AnimationTrack>(null, "HighPriorityTrack");
            var animClip2 = new AnimationClip();
            var curve2 = AnimationCurve.Linear(0, 5, 1, 5); // 常に5
            animClip2.SetCurve("", typeof(Transform), "localPosition.x", curve2);
            var timelineClip2 = track2.CreateClip<AnimationPlayableAsset>();
            timelineClip2.start = 0.5;
            timelineClip2.duration = 1;
            (timelineClip2.asset as AnimationPlayableAsset).clip = animClip2;

            try
            {
                // Act
                var results = _service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);

                // ログに処理フローが記録されていることを確認
                var logs = results[0].Logs;
                Assert.IsTrue(logs.Exists(log => log.Contains("処理開始")));
                Assert.IsTrue(logs.Exists(log => log.Contains("トラック")));
                Assert.IsTrue(logs.Exists(log => log.Contains("カーブの統合完了")));
                Assert.IsTrue(logs.Exists(log => log.Contains("出力完了")));

                // クリーンアップ用にパスを記録
                foreach (var log in logs)
                {
                    if (log.Contains(".anim"))
                    {
                        var parts = log.Split(' ');
                        foreach (var part in parts)
                        {
                            if (part.EndsWith(".anim"))
                            {
                                _createdAssetPaths.Add(part);
                            }
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip1);
                Object.DestroyImmediate(animClip2);
            }
        }

        [Test]
        public void 処理オーケストレーション_クリップがないトラックはスキップされる()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            // クリップがあるトラック
            var track1 = timeline.CreateTrack<AnimationTrack>(null, "TrackWithClip");
            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = track1.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            // クリップがないトラック
            var track2 = timeline.CreateTrack<AnimationTrack>(null, "EmptyTrack");

            try
            {
                // Act
                var results = _service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);

                // 空トラックに関するログが含まれることを確認
                var logs = results[0].Logs;
                Assert.IsTrue(logs.Exists(log => log.Contains("EmptyTrack") && log.Contains("クリップがありません")));

                // クリーンアップ用にパスを記録
                foreach (var log in logs)
                {
                    if (log.Contains(".anim"))
                    {
                        var parts = log.Split(' ');
                        foreach (var part in parts)
                        {
                            if (part.EndsWith(".anim"))
                            {
                                _createdAssetPaths.Add(part);
                            }
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        [Test]
        public void 処理オーケストレーション_フレームレートが正しく設定される()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            // TimelineAssetのフレームレートを30fpsに設定
            var editorSettings = timeline.editorSettings;
            editorSettings.frameRate = 30.0;

            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");
            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            try
            {
                // Act
                var results = _service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);
                Assert.IsNotNull(results[0].GeneratedClip);
                Assert.AreEqual(30f, results[0].GeneratedClip.frameRate);

                // クリーンアップ用にパスを記録
                foreach (var log in results[0].Logs)
                {
                    if (log.Contains(".anim"))
                    {
                        var parts = log.Split(' ');
                        foreach (var part in parts)
                        {
                            if (part.EndsWith(".anim"))
                            {
                                _createdAssetPaths.Add(part);
                            }
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        #endregion

        #region 依存性注入 テスト

        [Test]
        public void コンストラクタ_依存性注入でFileNameGeneratorとExporterを設定できる()
        {
            // Arrange
            var fileNameGenerator = new FileNameGenerator();
            var exporter = new AnimationClipExporter(fileNameGenerator);

            // Act
            var service = new AnimationMergeService(fileNameGenerator, exporter);

            // Assert
            Assert.IsNotNull(service);
        }

        #endregion

        #region タスク8.1.3 Animator単位の処理分割 テスト

        [Test]
        public void MergeFromPlayableDirector_複数Animatorがある場合それぞれ別のAnimationClipを出力する()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();

            // Animator A
            var animatorGoA = new GameObject("AnimatorA");
            var animatorA = animatorGoA.AddComponent<Animator>();

            // Animator B
            var animatorGoB = new GameObject("AnimatorB");
            var animatorB = animatorGoB.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            // Animator Aにバインドするトラック
            var trackA = timeline.CreateTrack<AnimationTrack>(null, "TrackA");
            var animClipA = new AnimationClip();
            var curveA = AnimationCurve.Linear(0, 0, 1, 10);
            animClipA.SetCurve("", typeof(Transform), "localPosition.x", curveA);
            var timelineClipA = trackA.CreateClip<AnimationPlayableAsset>();
            timelineClipA.start = 0;
            timelineClipA.duration = 1;
            (timelineClipA.asset as AnimationPlayableAsset).clip = animClipA;

            // Animator Bにバインドするトラック
            var trackB = timeline.CreateTrack<AnimationTrack>(null, "TrackB");
            var animClipB = new AnimationClip();
            var curveB = AnimationCurve.Linear(0, 0, 1, 20);
            animClipB.SetCurve("", typeof(Transform), "localPosition.y", curveB);
            var timelineClipB = trackB.CreateClip<AnimationPlayableAsset>();
            timelineClipB.start = 0;
            timelineClipB.duration = 1;
            (timelineClipB.asset as AnimationPlayableAsset).clip = animClipB;

            director.playableAsset = timeline;
            director.SetGenericBinding(trackA, animatorA);
            director.SetGenericBinding(trackB, animatorB);

            try
            {
                // Act
                var results = _service.MergeFromPlayableDirector(director);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(2, results.Count, "2つのAnimatorに対して2つのMergeResultが生成されるべき");

                // 各結果が正しいAnimatorに紐づいているか確認
                MergeResult resultA = null;
                MergeResult resultB = null;
                foreach (var result in results)
                {
                    if (result.TargetAnimator == animatorA)
                    {
                        resultA = result;
                    }
                    else if (result.TargetAnimator == animatorB)
                    {
                        resultB = result;
                    }
                }

                Assert.IsNotNull(resultA, "AnimatorAに対する結果が存在すべき");
                Assert.IsNotNull(resultB, "AnimatorBに対する結果が存在すべき");
                Assert.IsTrue(resultA.IsSuccess, "AnimatorAの処理が成功すべき");
                Assert.IsTrue(resultB.IsSuccess, "AnimatorBの処理が成功すべき");
                Assert.IsNotNull(resultA.GeneratedClip, "AnimatorA用のAnimationClipが生成されるべき");
                Assert.IsNotNull(resultB.GeneratedClip, "AnimatorB用のAnimationClipが生成されるべき");

                // 生成されたクリップが別々であることを確認
                Assert.AreNotSame(resultA.GeneratedClip, resultB.GeneratedClip, "2つの別々のAnimationClipが生成されるべき");

                // 各クリップに正しいカーブが含まれていることを確認
                var bindingsA = AnimationUtility.GetCurveBindings(resultA.GeneratedClip);
                var bindingsB = AnimationUtility.GetCurveBindings(resultB.GeneratedClip);

                Assert.AreEqual(1, bindingsA.Length, "AnimatorA用クリップには1つのカーブがあるべき");
                Assert.AreEqual(1, bindingsB.Length, "AnimatorB用クリップには1つのカーブがあるべき");
                Assert.AreEqual("localPosition.x", bindingsA[0].propertyName, "AnimatorA用クリップにはlocalPosition.xカーブがあるべき");
                Assert.AreEqual("localPosition.y", bindingsB[0].propertyName, "AnimatorB用クリップにはlocalPosition.yカーブがあるべき");

                // クリーンアップ用にパスを記録
                foreach (var result in results)
                {
                    foreach (var log in result.Logs)
                    {
                        if (log.Contains("出力完了:") || log.Contains(".anim"))
                        {
                            var parts = log.Split(' ');
                            foreach (var part in parts)
                            {
                                if (part.EndsWith(".anim"))
                                {
                                    _createdAssetPaths.Add(part);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(animatorGoA);
                Object.DestroyImmediate(animatorGoB);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClipA);
                Object.DestroyImmediate(animClipB);
            }
        }

        [Test]
        public void MergeFromPlayableDirector_同一Animatorに複数トラックがバインドされている場合1つのAnimationClipに統合される()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("TestAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            // 同じAnimatorにバインドする2つのトラック
            var track1 = timeline.CreateTrack<AnimationTrack>(null, "Track1");
            var animClip1 = new AnimationClip();
            var curve1 = AnimationCurve.Linear(0, 0, 1, 10);
            animClip1.SetCurve("", typeof(Transform), "localPosition.x", curve1);
            var timelineClip1 = track1.CreateClip<AnimationPlayableAsset>();
            timelineClip1.start = 0;
            timelineClip1.duration = 1;
            (timelineClip1.asset as AnimationPlayableAsset).clip = animClip1;

            var track2 = timeline.CreateTrack<AnimationTrack>(null, "Track2");
            var animClip2 = new AnimationClip();
            var curve2 = AnimationCurve.Linear(0, 0, 1, 20);
            animClip2.SetCurve("", typeof(Transform), "localPosition.y", curve2);
            var timelineClip2 = track2.CreateClip<AnimationPlayableAsset>();
            timelineClip2.start = 0;
            timelineClip2.duration = 1;
            (timelineClip2.asset as AnimationPlayableAsset).clip = animClip2;

            director.playableAsset = timeline;
            director.SetGenericBinding(track1, animator);
            director.SetGenericBinding(track2, animator);

            try
            {
                // Act
                var results = _service.MergeFromPlayableDirector(director);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(1, results.Count, "同一Animatorなので1つのMergeResultのみ");
                Assert.IsTrue(results[0].IsSuccess);
                Assert.AreSame(animator, results[0].TargetAnimator);
                Assert.IsNotNull(results[0].GeneratedClip);

                // 両方のカーブが1つのクリップに含まれていることを確認
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(2, bindings.Length, "両方のカーブが1つのクリップに統合されるべき");

                var propertyNames = new HashSet<string>();
                foreach (var binding in bindings)
                {
                    propertyNames.Add(binding.propertyName);
                }
                Assert.IsTrue(propertyNames.Contains("localPosition.x"), "localPosition.xカーブが含まれるべき");
                Assert.IsTrue(propertyNames.Contains("localPosition.y"), "localPosition.yカーブが含まれるべき");

                // クリーンアップ用にパスを記録
                foreach (var log in results[0].Logs)
                {
                    if (log.Contains("出力完了:") || log.Contains(".anim"))
                    {
                        var parts = log.Split(' ');
                        foreach (var part in parts)
                        {
                            if (part.EndsWith(".anim"))
                            {
                                _createdAssetPaths.Add(part);
                            }
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(animatorGo);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip1);
                Object.DestroyImmediate(animClip2);
            }
        }

        #endregion
    }
}
