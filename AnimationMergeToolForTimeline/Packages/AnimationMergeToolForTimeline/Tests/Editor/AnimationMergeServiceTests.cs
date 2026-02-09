using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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
            // Arrange
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] PlayableDirectorがnullです。");

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
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] PlayableDirectorにTimelineAssetが設定されていません。");

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
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] トラック \"TestTrack\" にAnimatorがバインドされていません。");
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] バインドされたAnimatorが見つかりません。");

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
                // SetCurveを使用すると、Unityは内部的に全てのコンポーネント（x,y,z）を生成する
                // m_LocalPosition.x, m_LocalPosition.y, m_LocalPosition.z の3カーブ
                Assert.AreEqual(3, bindings.Length);
                // Muteされていないトラック（localPosition.x）のカーブのみが含まれる
                var propertyNames = new HashSet<string>();
                foreach (var b in bindings)
                {
                    propertyNames.Add(b.propertyName);
                }
                Assert.IsTrue(propertyNames.Contains("m_LocalPosition.x"), "m_LocalPosition.xカーブが含まれるべき");

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
            // Arrange
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] TimelineAssetがnullです。");

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
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] 有効なAnimationTrackが見つかりません。");

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

            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] 有効なAnimationTrackが見つかりません。");

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

                // SetCurveを使用すると、Unityは内部的に全てのコンポーネント（x,y,z）を生成する
                Assert.AreEqual(3, bindingsA.Length, "AnimatorA用クリップには3つのカーブがあるべき（x,y,z）");
                Assert.AreEqual(3, bindingsB.Length, "AnimatorB用クリップには3つのカーブがあるべき（x,y,z）");

                var propertyNamesA = new HashSet<string>();
                var propertyNamesB = new HashSet<string>();
                foreach (var b in bindingsA) propertyNamesA.Add(b.propertyName);
                foreach (var b in bindingsB) propertyNamesB.Add(b.propertyName);
                Assert.IsTrue(propertyNamesA.Contains("m_LocalPosition.x"), "AnimatorA用クリップにはm_LocalPosition.xカーブがあるべき");
                Assert.IsTrue(propertyNamesB.Contains("m_LocalPosition.y"), "AnimatorB用クリップにはm_LocalPosition.yカーブがあるべき");

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
                // SetCurveを使用すると、Unityは内部的に全てのコンポーネント（x,y,z）を生成する
                // track1: localPosition.x → m_LocalPosition.x/y/z の3カーブ
                // track2: localPosition.y → 既存のm_LocalPosition.x/y/zに統合されるため合計3カーブ
                Assert.AreEqual(3, bindings.Length, "両方のトラックからのカーブが統合されるべき");

                var propertyNames = new HashSet<string>();
                foreach (var binding in bindings)
                {
                    propertyNames.Add(binding.propertyName);
                }
                // Unity APIでは、SetCurveに渡す"localPosition.x"は内部的に"m_LocalPosition.x"として保存される
                Assert.IsTrue(propertyNames.Contains("m_LocalPosition.x"), "localPosition.xカーブが含まれるべき");
                Assert.IsTrue(propertyNames.Contains("m_LocalPosition.y"), "localPosition.yカーブが含まれるべき");

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

        #region タスク10.1.2 ERR-002 対象トラック0件のエラー処理 テスト

        [Test]
        public void MergeFromPlayableDirector_対象トラック0件の場合エラーログを出力して処理終了する()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            // AnimationTrackを作成するがAnimatorをバインドしない
            var track = timeline.CreateTrack<AnimationTrack>(null, "UnboundTrack");
            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            director.playableAsset = timeline;
            // Animatorをバインドしない → 有効なトラックが0件になる

            // エラーログを期待
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] トラック \"UnboundTrack\" にAnimatorがバインドされていません。");
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] バインドされたAnimatorが見つかりません。");

            try
            {
                // Act
                // ログをキャプチャするためにログハンドラを設定
                var capturedLogs = new List<string>();
                var originalLogHandler = Debug.unityLogger.logHandler;
                var testLogHandler = new TestLogHandler(originalLogHandler, capturedLogs);
                Debug.unityLogger.logHandler = testLogHandler;

                try
                {
                    var results = _service.MergeFromPlayableDirector(director);

                    // Assert
                    Assert.IsNotNull(results);
                    Assert.AreEqual(0, results.Count, "対象トラックが0件の場合、空のリストを返すべき");

                    // エラーログが出力されたことを確認
                    Assert.IsTrue(
                        capturedLogs.Exists(log =>
                            log.Contains("[AnimationMergeTool]") &&
                            (log.Contains("バインドされたAnimatorが見つかりません") || log.Contains("バインドされていません"))),
                        "対象トラック0件のエラーログが出力されるべき");
                }
                finally
                {
                    Debug.unityLogger.logHandler = originalLogHandler;
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        [Test]
        public void MergeFromTimelineAsset_有効なトラック0件の場合エラーログを出力して処理終了する()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            // 全てのトラックをMuteにして有効なトラックを0件にする
            var track = timeline.CreateTrack<AnimationTrack>(null, "MutedTrack");
            track.muted = true;
            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            // エラーログを期待
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] 有効なAnimationTrackが見つかりません。");

            try
            {
                // Act
                // ログをキャプチャするためにログハンドラを設定
                var capturedLogs = new List<string>();
                var originalLogHandler = Debug.unityLogger.logHandler;
                var testLogHandler = new TestLogHandler(originalLogHandler, capturedLogs);
                Debug.unityLogger.logHandler = testLogHandler;

                try
                {
                    var results = _service.MergeFromTimelineAsset(timeline);

                    // Assert
                    Assert.IsNotNull(results);
                    Assert.AreEqual(0, results.Count, "有効なトラックが0件の場合、空のリストを返すべき");

                    // エラーログが出力されたことを確認
                    Assert.IsTrue(
                        capturedLogs.Exists(log =>
                            log.Contains("[AnimationMergeTool]") &&
                            log.Contains("有効なAnimationTrackが見つかりません")),
                        "有効なトラック0件のエラーログが出力されるべき");
                }
                finally
                {
                    Debug.unityLogger.logHandler = originalLogHandler;
                }
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        [Test]
        public void MergeFromTimelineAsset_AnimationTrackがない場合エラーログを出力して処理終了する()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            // AnimationTrackを追加しない（空のTimeline）

            // エラーログを期待
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] 有効なAnimationTrackが見つかりません。");

            try
            {
                // Act
                // ログをキャプチャするためにログハンドラを設定
                var capturedLogs = new List<string>();
                var originalLogHandler = Debug.unityLogger.logHandler;
                var testLogHandler = new TestLogHandler(originalLogHandler, capturedLogs);
                Debug.unityLogger.logHandler = testLogHandler;

                try
                {
                    var results = _service.MergeFromTimelineAsset(timeline);

                    // Assert
                    Assert.IsNotNull(results);
                    Assert.AreEqual(0, results.Count, "AnimationTrackがない場合、空のリストを返すべき");

                    // エラーログが出力されたことを確認
                    Assert.IsTrue(
                        capturedLogs.Exists(log =>
                            log.Contains("[AnimationMergeTool]") &&
                            log.Contains("有効なAnimationTrackが見つかりません")),
                        "AnimationTrack0件のエラーログが出力されるべき");
                }
                finally
                {
                    Debug.unityLogger.logHandler = originalLogHandler;
                }
            }
            finally
            {
                Object.DestroyImmediate(timeline);
            }
        }

        /// <summary>
        /// テスト用のログハンドラ
        /// ログメッセージをキャプチャするために使用
        /// </summary>
        private class TestLogHandler : ILogHandler
        {
            private readonly ILogHandler _originalHandler;
            private readonly List<string> _capturedLogs;

            public TestLogHandler(ILogHandler originalHandler, List<string> capturedLogs)
            {
                _originalHandler = originalHandler;
                _capturedLogs = capturedLogs;
            }

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                var message = string.Format(format, args);
                _capturedLogs.Add(message);
                _originalHandler.LogFormat(logType, context, format, args);
            }

            public void LogException(System.Exception exception, Object context)
            {
                _capturedLogs.Add(exception.Message);
                _originalHandler.LogException(exception, context);
            }
        }

        #endregion

        #region タスク10.2.1 空のTimelineAssetの処理（エッジケース）テスト

        [Test]
        public void MergeFromTimelineAsset_完全に空のTimelineAssetの場合エラーログを出力して空のリストを返す()
        {
            // Arrange
            // トラックが一切ないTimelineAsset
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "CompletelyEmptyTimeline";

            // エラーログを期待
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] 有効なAnimationTrackが見つかりません。");

            try
            {
                // Act
                var capturedLogs = new List<string>();
                var originalLogHandler = Debug.unityLogger.logHandler;
                var testLogHandler = new TestLogHandler(originalLogHandler, capturedLogs);
                Debug.unityLogger.logHandler = testLogHandler;

                try
                {
                    var results = _service.MergeFromTimelineAsset(timeline);

                    // Assert
                    Assert.IsNotNull(results, "結果がnullであってはならない");
                    Assert.AreEqual(0, results.Count, "空のTimelineAssetの場合、空のリストを返すべき");

                    // エラーログが出力されたことを確認
                    Assert.IsTrue(
                        capturedLogs.Exists(log =>
                            log.Contains("[AnimationMergeTool]") &&
                            log.Contains("有効なAnimationTrackが見つかりません")),
                        "空のTimelineAssetに対するエラーログが出力されるべき");
                }
                finally
                {
                    Debug.unityLogger.logHandler = originalLogHandler;
                }
            }
            finally
            {
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void MergeFromPlayableDirector_空のTimelineAssetがバインドされている場合エラーログを出力して空のリストを返す()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "EmptyTimelineForDirector";
            director.playableAsset = timeline;
            // トラックが一切ないTimelineAssetをバインド

            // エラーログを期待
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] バインドされたAnimatorが見つかりません。");

            try
            {
                // Act
                var capturedLogs = new List<string>();
                var originalLogHandler = Debug.unityLogger.logHandler;
                var testLogHandler = new TestLogHandler(originalLogHandler, capturedLogs);
                Debug.unityLogger.logHandler = testLogHandler;

                try
                {
                    var results = _service.MergeFromPlayableDirector(director);

                    // Assert
                    Assert.IsNotNull(results, "結果がnullであってはならない");
                    Assert.AreEqual(0, results.Count, "空のTimelineAssetの場合、空のリストを返すべき");

                    // エラーログが出力されたことを確認
                    Assert.IsTrue(
                        capturedLogs.Exists(log =>
                            log.Contains("[AnimationMergeTool]") &&
                            log.Contains("バインドされたAnimatorが見つかりません")),
                        "空のTimelineAssetに対するエラーログが出力されるべき");
                }
                finally
                {
                    Debug.unityLogger.logHandler = originalLogHandler;
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void MergeFromTimelineAsset_AnimationTrack以外のトラックのみのTimelineAssetの場合空のリストを返す()
        {
            // Arrange
            // AnimationTrack以外のトラック（例: AudioTrack、SignalTrack等）のみを含むTimeline
            // UnityのTimelineにはAudioTrackなど他のトラックタイプもあるが、
            // テストではAnimationTrack以外が含まれないケースを想定
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "NonAnimationTrackTimeline";

            // GroupTrackを追加（AnimationTrackではない）
            timeline.CreateTrack<GroupTrack>(null, "EmptyGroup");

            // エラーログを期待
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] 有効なAnimationTrackが見つかりません。");

            try
            {
                // Act
                var capturedLogs = new List<string>();
                var originalLogHandler = Debug.unityLogger.logHandler;
                var testLogHandler = new TestLogHandler(originalLogHandler, capturedLogs);
                Debug.unityLogger.logHandler = testLogHandler;

                try
                {
                    var results = _service.MergeFromTimelineAsset(timeline);

                    // Assert
                    Assert.IsNotNull(results, "結果がnullであってはならない");
                    Assert.AreEqual(0, results.Count, "AnimationTrackがない場合、空のリストを返すべき");

                    // エラーログが出力されたことを確認
                    Assert.IsTrue(
                        capturedLogs.Exists(log =>
                            log.Contains("[AnimationMergeTool]") &&
                            log.Contains("有効なAnimationTrackが見つかりません")),
                        "AnimationTrackがない場合のエラーログが出力されるべき");
                }
                finally
                {
                    Debug.unityLogger.logHandler = originalLogHandler;
                }
            }
            finally
            {
                Object.DestroyImmediate(timeline);
            }
        }

        #endregion

        #region タスク10.2.2 クリップが1つもないトラックの処理（エッジケース）テスト

        [Test]
        public void MergeFromTimelineAsset_全てのトラックにクリップがない場合エラーログを出力して処理結果を返す()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "AllEmptyTracksTimeline";

            // クリップがないトラックを複数作成
            var track1 = timeline.CreateTrack<AnimationTrack>(null, "EmptyTrack1");
            var track2 = timeline.CreateTrack<AnimationTrack>(null, "EmptyTrack2");
            // クリップを追加しない

            try
            {
                // Act
                var capturedLogs = new List<string>();
                var originalLogHandler = Debug.unityLogger.logHandler;
                var testLogHandler = new TestLogHandler(originalLogHandler, capturedLogs);
                Debug.unityLogger.logHandler = testLogHandler;

                try
                {
                    var results = _service.MergeFromTimelineAsset(timeline);

                    // Assert
                    Assert.IsNotNull(results, "結果がnullであってはならない");
                    Assert.AreEqual(1, results.Count, "処理結果が1つ返されるべき（トラックはあるが有効なカーブがない）");

                    // 処理結果が失敗していることを確認
                    var result = results[0];
                    Assert.IsFalse(result.IsSuccess, "有効なカーブがない場合は処理失敗となるべき");

                    // ログに空トラックの情報が含まれていることを確認
                    Assert.IsTrue(
                        result.Logs.Exists(log => log.Contains("EmptyTrack1") && log.Contains("クリップがありません")),
                        "EmptyTrack1に対するログが出力されるべき");
                    Assert.IsTrue(
                        result.Logs.Exists(log => log.Contains("EmptyTrack2") && log.Contains("クリップがありません")),
                        "EmptyTrack2に対するログが出力されるべき");

                    // エラーログに「有効なカーブデータがありません」が含まれていることを確認
                    Assert.IsTrue(
                        result.Logs.Exists(log => log.Contains("有効なカーブデータがありません")),
                        "有効なカーブデータがないことを示すエラーログが出力されるべき");
                }
                finally
                {
                    Debug.unityLogger.logHandler = originalLogHandler;
                }
            }
            finally
            {
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void MergeFromPlayableDirector_全てのトラックにクリップがない場合エラーログを出力して処理結果を返す()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("TestAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "AllEmptyTracksTimeline";

            // クリップがないトラックを複数作成
            var track1 = timeline.CreateTrack<AnimationTrack>(null, "EmptyTrack1");
            var track2 = timeline.CreateTrack<AnimationTrack>(null, "EmptyTrack2");
            // クリップを追加しない

            director.playableAsset = timeline;
            director.SetGenericBinding(track1, animator);
            director.SetGenericBinding(track2, animator);

            try
            {
                // Act
                var results = _service.MergeFromPlayableDirector(director);

                // Assert
                Assert.IsNotNull(results, "結果がnullであってはならない");
                Assert.AreEqual(1, results.Count, "処理結果が1つ返されるべき（トラックはあるが有効なカーブがない）");

                // 処理結果が失敗していることを確認
                var result = results[0];
                Assert.IsFalse(result.IsSuccess, "有効なカーブがない場合は処理失敗となるべき");
                Assert.AreSame(animator, result.TargetAnimator, "ターゲットAnimatorが正しく設定されるべき");

                // ログに空トラックの情報が含まれていることを確認
                Assert.IsTrue(
                    result.Logs.Exists(log => log.Contains("EmptyTrack1") && log.Contains("クリップがありません")),
                    "EmptyTrack1に対するログが出力されるべき");
                Assert.IsTrue(
                    result.Logs.Exists(log => log.Contains("EmptyTrack2") && log.Contains("クリップがありません")),
                    "EmptyTrack2に対するログが出力されるべき");

                // エラーログに「有効なカーブデータがありません」が含まれていることを確認
                Assert.IsTrue(
                    result.Logs.Exists(log => log.Contains("有効なカーブデータがありません")),
                    "有効なカーブデータがないことを示すエラーログが出力されるべき");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(animatorGo);
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void MergeFromTimelineAsset_一部のトラックにクリップがない場合クリップありトラックのみ処理される()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "MixedTracksTimeline";

            // クリップがあるトラック
            var trackWithClip = timeline.CreateTrack<AnimationTrack>(null, "TrackWithClip");
            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = trackWithClip.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            // クリップがないトラック
            var emptyTrack = timeline.CreateTrack<AnimationTrack>(null, "EmptyTrack");
            // クリップを追加しない

            try
            {
                // Act
                var results = _service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.IsNotNull(results, "結果がnullであってはならない");
                Assert.AreEqual(1, results.Count, "処理結果が1つ返されるべき");

                var result = results[0];
                Assert.IsTrue(result.IsSuccess, "クリップありトラックがある場合は処理成功となるべき");
                Assert.IsNotNull(result.GeneratedClip, "AnimationClipが生成されるべき");

                // 生成されたクリップに正しいカーブが含まれていることを確認
                var bindings = AnimationUtility.GetCurveBindings(result.GeneratedClip);
                // SetCurveを使用すると、Unityは内部的に全てのコンポーネント（x,y,z）を生成する
                // また、プロパティ名は内部形式（m_LocalPosition.x）に変換される
                Assert.AreEqual(3, bindings.Length, "3つのカーブが含まれるべき（x,y,z）");
                Assert.IsTrue(bindings.Any(b => b.propertyName == "m_LocalPosition.x"), "m_LocalPosition.xカーブが含まれるべき");

                // ログに空トラックの情報が含まれていることを確認
                Assert.IsTrue(
                    result.Logs.Exists(log => log.Contains("EmptyTrack") && log.Contains("クリップがありません")),
                    "EmptyTrackに対するログが出力されるべき");

                // クリップありトラックの処理ログが含まれていることを確認
                Assert.IsTrue(
                    result.Logs.Exists(log => log.Contains("TrackWithClip") && log.Contains("処理しました")),
                    "TrackWithClipの処理ログが出力されるべき");

                // クリーンアップ用にパスを記録
                foreach (var log in result.Logs)
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
        public void MergeFromTimelineAsset_クリップがないトラックが複数ある場合それぞれログが出力される()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "MultipleEmptyTracksTimeline";

            // 複数のクリップがないトラックを作成
            var track1 = timeline.CreateTrack<AnimationTrack>(null, "EmptyTrackAlpha");
            var track2 = timeline.CreateTrack<AnimationTrack>(null, "EmptyTrackBeta");
            var track3 = timeline.CreateTrack<AnimationTrack>(null, "EmptyTrackGamma");
            // クリップを追加しない

            try
            {
                // Act
                var results = _service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.IsNotNull(results, "結果がnullであってはならない");
                Assert.AreEqual(1, results.Count, "処理結果が1つ返されるべき");

                var result = results[0];
                Assert.IsFalse(result.IsSuccess, "全てのトラックにクリップがない場合は処理失敗となるべき");

                // 各トラックに対するログが出力されていることを確認
                Assert.IsTrue(
                    result.Logs.Exists(log => log.Contains("EmptyTrackAlpha") && log.Contains("クリップがありません")),
                    "EmptyTrackAlphaに対するログが出力されるべき");
                Assert.IsTrue(
                    result.Logs.Exists(log => log.Contains("EmptyTrackBeta") && log.Contains("クリップがありません")),
                    "EmptyTrackBetaに対するログが出力されるべき");
                Assert.IsTrue(
                    result.Logs.Exists(log => log.Contains("EmptyTrackGamma") && log.Contains("クリップがありません")),
                    "EmptyTrackGammaに対するログが出力されるべき");
            }
            finally
            {
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void MergeFromTimelineAsset_AnimationPlayableAssetのclipがnullのクリップは無視される()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "NullClipTimeline";

            var track = timeline.CreateTrack<AnimationTrack>(null, "TrackWithNullClip");
            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            // AnimationPlayableAssetのclipをnullのままにする
            var asset = timelineClip.asset as AnimationPlayableAsset;
            // asset.clip = null; （デフォルトでnull）

            try
            {
                // Act
                var results = _service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.IsNotNull(results, "結果がnullであってはならない");
                Assert.AreEqual(1, results.Count, "処理結果が1つ返されるべき");

                var result = results[0];
                Assert.IsFalse(result.IsSuccess, "有効なカーブがない場合は処理失敗となるべき");

                // クリップがないというログが出力されることを確認
                Assert.IsTrue(
                    result.Logs.Exists(log => log.Contains("TrackWithNullClip") && log.Contains("クリップがありません")),
                    "clipがnullのTimelineClipはクリップなしとして扱われるべき");
            }
            finally
            {
                Object.DestroyImmediate(timeline);
            }
        }

        #endregion

        #region P16-003 AnimationMergeService FBX統合テスト

        [Test]
        public void MergeFromPlayableDirectorToFbx_FBXエクスポートオプションでマージ結果にFbxExportDataが含まれる()
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

                // MergeResultからFbxExportData用のカーブが取得可能であることを確認
                var generatedClip = results[0].GeneratedClip;
                Assert.IsNotNull(generatedClip);

                // FbxAnimationExporterでエクスポートデータを準備できることを確認
                var fbxExporter = new FbxAnimationExporter();
                var exportData = fbxExporter.PrepareAllCurvesForExport(animator, generatedClip);

                Assert.IsNotNull(exportData);
                Assert.IsTrue(exportData.HasExportableData || exportData.TransformCurves.Count > 0,
                    "エクスポート可能なデータまたはTransformカーブが存在すべき");

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
                Object.DestroyImmediate(animClip);
            }
        }

        [Test]
        public void FBX統合_AnimationMergeServiceの結果からFbxExportDataを生成できる()
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
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            try
            {
                // Act
                var results = _service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);

                var generatedClip = results[0].GeneratedClip;
                Assert.IsNotNull(generatedClip);

                // FbxExportData生成テスト（Animatorなしでも動作するか）
                var fbxExporter = new FbxAnimationExporter();
                var exportData = fbxExporter.PrepareAllCurvesForExport(null, generatedClip);

                Assert.IsNotNull(exportData);
                // Animatorがなくても、AnimationClipからTransformカーブは抽出可能
                // カーブが存在すればHasExportableDataはtrue
                Assert.IsTrue(exportData.HasExportableData,
                    "AnimationClipにTransformカーブがあればエクスポート可能");

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
        public void FBX統合_複数Animatorの結果から各Animator用のFbxExportDataを生成できる()
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

                var fbxExporter = new FbxAnimationExporter();

                // 各結果からFbxExportDataを生成
                foreach (var result in results)
                {
                    Assert.IsTrue(result.IsSuccess);
                    Assert.IsNotNull(result.GeneratedClip);
                    Assert.IsNotNull(result.TargetAnimator);

                    var exportData = fbxExporter.PrepareAllCurvesForExport(
                        result.TargetAnimator,
                        result.GeneratedClip);

                    Assert.IsNotNull(exportData);
                    Assert.AreSame(result.TargetAnimator, exportData.SourceAnimator,
                        "FbxExportDataのSourceAnimatorがMergeResultのTargetAnimatorと一致すべき");

                    // クリーンアップ用にパスを記録
                    foreach (var log in result.Logs)
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
        public void FBX統合_FileNameGeneratorでFBXファイル名を生成できる()
        {
            // Arrange
            var fileNameGenerator = new FileNameGenerator();

            // Act
            var animFileName = fileNameGenerator.GenerateBaseName("TestTimeline", "TestAnimator");
            var fbxFileName = fileNameGenerator.GenerateBaseName("TestTimeline", "TestAnimator", ".fbx");

            // Assert
            Assert.AreEqual("TestTimeline_TestAnimator_Merged.anim", animFileName);
            Assert.AreEqual("TestTimeline_TestAnimator_Merged.fbx", fbxFileName);
        }

        [Test]
        public void FBX統合_FileNameGeneratorで重複回避のFBXファイルパスを生成できる()
        {
            // Arrange
            var mockChecker = new MockFileExistenceChecker();
            mockChecker.AddExistingFile("Assets/TestTimeline_TestAnimator_Merged.fbx");
            var fileNameGenerator = new FileNameGenerator(mockChecker);

            // Act
            var uniquePath = fileNameGenerator.GenerateUniqueFilePath(
                "Assets",
                "TestTimeline",
                "TestAnimator",
                ".fbx");

            // Assert
            Assert.AreEqual("Assets/TestTimeline_TestAnimator_Merged(1).fbx", uniquePath);
        }

        [Test]
        public void FBX統合_MergeResultにFbxExportDataを格納できることを確認()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("TestAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");

            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

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

                // MergeResultの情報からFBXエクスポートワークフローが構築可能であることを確認
                var mergeResult = results[0];
                Assert.IsNotNull(mergeResult.TargetAnimator, "TargetAnimatorが設定されているべき");
                Assert.IsNotNull(mergeResult.GeneratedClip, "GeneratedClipが設定されているべき");

                // FBXエクスポート用のデータ準備が可能であることを確認
                var fbxExporter = new FbxAnimationExporter();
                var skeleton = fbxExporter.ExtractSkeleton(mergeResult.TargetAnimator);
                Assert.IsNotNull(skeleton, "スケルトン情報を抽出できるべき");

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
                Object.DestroyImmediate(animClip);
            }
        }

        [Test]
        public void FBX統合_BlendShapeカーブを含むマージ結果からFbxExportDataを生成できる()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("TestAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            // SkinnedMeshRendererを追加（BlendShape用）
            var meshGo = new GameObject("Face");
            meshGo.transform.SetParent(animatorGo.transform);
            var skinnedMesh = meshGo.AddComponent<SkinnedMeshRenderer>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");

            // BlendShapeカーブを含むAnimationClipを作成
            var animClip = new AnimationClip();
            var blendShapeCurve = AnimationCurve.Linear(0, 0, 1, 100);
            animClip.SetCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.smile", blendShapeCurve);

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

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

                var generatedClip = results[0].GeneratedClip;
                Assert.IsNotNull(generatedClip);

                // FbxExportDataにBlendShapeカーブが含まれるか確認
                var fbxExporter = new FbxAnimationExporter();
                var hasBlendShapes = fbxExporter.HasBlendShapeCurves(generatedClip);
                Assert.IsTrue(hasBlendShapes, "BlendShapeカーブが含まれるべき");

                var blendShapeCurves = fbxExporter.ExtractBlendShapeCurves(generatedClip, animator);
                Assert.IsNotNull(blendShapeCurves);
                Assert.AreEqual(1, blendShapeCurves.Count, "1つのBlendShapeカーブが抽出されるべき");
                Assert.AreEqual("smile", blendShapeCurves[0].BlendShapeName);

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
                Object.DestroyImmediate(animClip);
            }
        }

        /// <summary>
        /// テスト用のモックファイル存在確認クラス
        /// </summary>
        private class MockFileExistenceChecker : IFileExistenceChecker
        {
            private readonly HashSet<string> _existingFiles = new HashSet<string>();

            public void AddExistingFile(string path)
            {
                _existingFiles.Add(path);
            }

            public bool Exists(string path)
            {
                return _existingFiles.Contains(path);
            }
        }

        #endregion

        #region AnimatorのTransformオフセット反映テスト

        [Test]
        public void MergeFromPlayableDirector_AnimatorのlocalPositionがルートカーブに反映される()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();

            // 親オブジェクトの下にAnimatorを配置し、位置をオフセット
            var parentGo = new GameObject("Parent");
            var animatorGo = new GameObject("TestAnimator");
            animatorGo.transform.SetParent(parentGo.transform);
            animatorGo.transform.localPosition = new Vector3(5f, 0f, 0f);
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");

            // ルートPositionカーブを持つAnimationClipを作成（値は0）
            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "m_LocalPosition.x",
                new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0)));
            animClip.SetCurve("", typeof(Transform), "m_LocalPosition.y",
                new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0)));
            animClip.SetCurve("", typeof(Transform), "m_LocalPosition.z",
                new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0)));

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

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

                var generatedClip = results[0].GeneratedClip;
                Assert.IsNotNull(generatedClip);

                // ルートPositionのXカーブを取得して、Animatorのオフセット(5)が加算されていることを確認
                var bindings = AnimationUtility.GetCurveBindings(generatedClip);
                EditorCurveBinding? posXBinding = null;
                foreach (var b in bindings)
                {
                    if (b.path == "" && b.propertyName == "m_LocalPosition.x" && b.type == typeof(Transform))
                    {
                        posXBinding = b;
                        break;
                    }
                }

                Assert.IsTrue(posXBinding.HasValue, "m_LocalPosition.xのルートカーブが存在すべき");
                var posXCurve = AnimationUtility.GetEditorCurve(generatedClip, posXBinding.Value);
                Assert.IsNotNull(posXCurve);

                // 元の値(0) + Animatorオフセット(5) = 5
                Assert.AreEqual(5f, posXCurve.Evaluate(0f), 0.01f,
                    "AnimatorのlocalPosition.x(5)がルートカーブに加算されるべき");
                Assert.AreEqual(5f, posXCurve.Evaluate(1f), 0.01f,
                    "AnimatorのlocalPosition.x(5)がルートカーブに加算されるべき");

                // ログにTransformオフセット適用のメッセージが含まれることを確認
                Assert.IsTrue(
                    results[0].Logs.Exists(log => log.Contains("AnimatorのTransformオフセットを適用")),
                    "Transformオフセット適用ログが出力されるべき");

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
                Object.DestroyImmediate(parentGo);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        [Test]
        public void MergeFromPlayableDirector_Animatorの位置が原点の場合オフセットは適用されない()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("TestAnimator");
            // localPosition = (0,0,0) のまま
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");

            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "m_LocalPosition.x",
                new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 2)));

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

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

                var generatedClip = results[0].GeneratedClip;
                var bindings = AnimationUtility.GetCurveBindings(generatedClip);
                EditorCurveBinding? posXBinding = null;
                foreach (var b in bindings)
                {
                    if (b.path == "" && b.propertyName == "m_LocalPosition.x" && b.type == typeof(Transform))
                    {
                        posXBinding = b;
                        break;
                    }
                }

                Assert.IsTrue(posXBinding.HasValue);
                var posXCurve = AnimationUtility.GetEditorCurve(generatedClip, posXBinding.Value);

                // Animatorが原点にあるので元の値がそのまま
                Assert.AreEqual(1f, posXCurve.Evaluate(0f), 0.01f,
                    "Animatorが原点の場合、カーブ値は変更されないべき");
                Assert.AreEqual(2f, posXCurve.Evaluate(1f), 0.01f,
                    "Animatorが原点の場合、カーブ値は変更されないべき");

                // Transformオフセット適用ログが出力されないことを確認
                Assert.IsFalse(
                    results[0].Logs.Exists(log => log.Contains("AnimatorのTransformオフセットを適用")),
                    "原点の場合はTransformオフセット適用ログが出力されないべき");

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
                Object.DestroyImmediate(animClip);
            }
        }

        [Test]
        public void MergeFromPlayableDirector_AnimatorのlocalRotationがルートカーブに反映される()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();

            var parentGo = new GameObject("Parent");
            var animatorGo = new GameObject("TestAnimator");
            animatorGo.transform.SetParent(parentGo.transform);
            // Y軸90度回転
            animatorGo.transform.localRotation = Quaternion.Euler(0, 90, 0);
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");

            // ルートRotationカーブを持つAnimationClip（identity回転）
            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "m_LocalRotation.x",
                new AnimationCurve(new Keyframe(0, 0)));
            animClip.SetCurve("", typeof(Transform), "m_LocalRotation.y",
                new AnimationCurve(new Keyframe(0, 0)));
            animClip.SetCurve("", typeof(Transform), "m_LocalRotation.z",
                new AnimationCurve(new Keyframe(0, 0)));
            animClip.SetCurve("", typeof(Transform), "m_LocalRotation.w",
                new AnimationCurve(new Keyframe(0, 1)));

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

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

                var generatedClip = results[0].GeneratedClip;
                Assert.IsNotNull(generatedClip);

                // Rotationカーブを取得
                var bindings = AnimationUtility.GetCurveBindings(generatedClip);
                AnimationCurve rotYCurve = null;
                foreach (var b in bindings)
                {
                    if (b.path == "" && b.propertyName == "m_LocalRotation.y" && b.type == typeof(Transform))
                    {
                        rotYCurve = AnimationUtility.GetEditorCurve(generatedClip, b);
                        break;
                    }
                }

                Assert.IsNotNull(rotYCurve, "m_LocalRotation.yのルートカーブが存在すべき");

                // Y軸90度回転のクォータニオンy成分 ≈ 0.7071
                var expectedQ = Quaternion.Euler(0, 90, 0);
                Assert.AreEqual(expectedQ.y, rotYCurve.Evaluate(0f), 0.01f,
                    "AnimatorのlocalRotation(Y=90度)がルートカーブに反映されるべき");

                // ログにTransformオフセット適用のメッセージが含まれることを確認
                Assert.IsTrue(
                    results[0].Logs.Exists(log => log.Contains("AnimatorのTransformオフセットを適用")),
                    "Transformオフセット適用ログが出力されるべき");

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
                Object.DestroyImmediate(parentGo);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        #endregion

        #region P16-004 PrepareFbxExportData Humanoid判定テスト

        [Test]
        public void PrepareFbxExportData_nullのMergeResult_nullを返す()
        {
            // Act
            var result = _service.PrepareFbxExportData(null);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void PrepareFbxExportData_GeneratedClipがnull_nullを返す()
        {
            // Arrange
            var animatorGo = new GameObject("TestAnimator");
            var animator = animatorGo.AddComponent<Animator>();
            var mergeResult = new MergeResult(animator);
            // GeneratedClipを設定しない（null）

            try
            {
                // Act
                var result = _service.PrepareFbxExportData(mergeResult);

                // Assert
                Assert.IsNull(result);
            }
            finally
            {
                Object.DestroyImmediate(animatorGo);
            }
        }

        [Test]
        public void PrepareFbxExportData_GenericAnimatorの場合_PrepareAllCurvesForExportが使用される()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("TestAnimator");
            var animator = animatorGo.AddComponent<Animator>();
            // avatarを設定しない = Generic扱い（isHuman == false）

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");

            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            director.playableAsset = timeline;
            director.SetGenericBinding(track, animator);

            try
            {
                // MergeResultを取得
                var results = _service.MergeFromPlayableDirector(director);
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);
                Assert.IsFalse(animator.isHuman, "テスト前提: AnimatorはGenericであるべき");

                // Act
                var exportData = _service.PrepareFbxExportData(results[0]);

                // Assert
                Assert.IsNotNull(exportData);
                // GenericリグなのでIsHumanoidはfalse
                Assert.IsFalse(exportData.IsHumanoid,
                    "GenericリグではPrepareAllCurvesForExportが使用され、IsHumanoidはfalseであるべき");

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
                Object.DestroyImmediate(animClip);
            }
        }

        [Test]
        public void PrepareFbxExportData_Animatorがnullの場合_PrepareAllCurvesForExportが使用される()
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
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            try
            {
                // MergeResultを取得（Animatorなし）
                var results = _service.MergeFromTimelineAsset(timeline);
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);
                Assert.IsNull(results[0].TargetAnimator, "テスト前提: TargetAnimatorはnullであるべき");

                // Act
                var exportData = _service.PrepareFbxExportData(results[0]);

                // Assert
                Assert.IsNotNull(exportData);
                // Animatorがnullの場合、Humanoid分岐に入らずPrepareAllCurvesForExportが使用される
                Assert.IsFalse(exportData.IsHumanoid,
                    "Animatorがnullの場合はPrepareAllCurvesForExportが使用されるべき");

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
    }
}
