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
    }
}
