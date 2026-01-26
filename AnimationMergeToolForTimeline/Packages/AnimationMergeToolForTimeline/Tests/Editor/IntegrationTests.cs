using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using AnimationMergeTool.Editor.Application;
using AnimationMergeTool.Editor.UI;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// 統合テスト
    /// タスク8.4.2: Phase 8の全コンポーネントが連携して動作することを検証
    /// AnimationMergeService、ContextMenuHandler、ProgressDisplayの統合テスト
    /// </summary>
    public class IntegrationTests
    {
        private List<string> _createdAssetPaths;

        [SetUp]
        public void SetUp()
        {
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

            // Selectionをリセット
            Selection.objects = new Object[0];
        }

        #region エンドツーエンド統合テスト

        [Test]
        public void 統合テスト_PlayableDirectorからAnimationClip生成までの全フロー()
        {
            // Arrange: 完全なシナリオをセットアップ
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("TestAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "IntegrationTestTimeline";

            // 複数のトラックを作成
            var track1 = timeline.CreateTrack<AnimationTrack>(null, "Track1");
            var animClip1 = new AnimationClip();
            animClip1.name = "Clip1";
            var curve1 = AnimationCurve.Linear(0, 0, 1, 10);
            animClip1.SetCurve("", typeof(Transform), "localPosition.x", curve1);
            var timelineClip1 = track1.CreateClip<AnimationPlayableAsset>();
            timelineClip1.start = 0;
            timelineClip1.duration = 1;
            (timelineClip1.asset as AnimationPlayableAsset).clip = animClip1;

            var track2 = timeline.CreateTrack<AnimationTrack>(null, "Track2");
            var animClip2 = new AnimationClip();
            animClip2.name = "Clip2";
            var curve2 = AnimationCurve.Linear(0, 0, 1, 20);
            animClip2.SetCurve("", typeof(Transform), "localPosition.y", curve2);
            var timelineClip2 = track2.CreateClip<AnimationPlayableAsset>();
            timelineClip2.start = 0.5;
            timelineClip2.duration = 1;
            (timelineClip2.asset as AnimationPlayableAsset).clip = animClip2;

            director.playableAsset = timeline;
            director.SetGenericBinding(track1, animator);
            director.SetGenericBinding(track2, animator);

            try
            {
                // Act: AnimationMergeServiceを通じてマージ実行
                var service = new AnimationMergeService();
                var results = service.MergeFromPlayableDirector(director);

                // Assert: 結果を検証
                Assert.IsNotNull(results, "結果がnullであってはならない");
                Assert.AreEqual(1, results.Count, "1つのAnimator用の結果が返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "処理は成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");
                Assert.AreSame(animator, results[0].TargetAnimator, "正しいAnimatorに紐づいているべき");

                // 生成されたクリップのカーブを検証
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(2, bindings.Length, "2つのカーブが含まれるべき");

                var propertyNames = new HashSet<string>();
                foreach (var binding in bindings)
                {
                    propertyNames.Add(binding.propertyName);
                }
                Assert.IsTrue(propertyNames.Contains("localPosition.x"), "localPosition.xカーブが含まれるべき");
                Assert.IsTrue(propertyNames.Contains("localPosition.y"), "localPosition.yカーブが含まれるべき");

                // 処理ログを検証
                var logs = results[0].Logs;
                Assert.IsTrue(logs.Exists(log => log.Contains("処理開始")), "処理開始ログがあるべき");
                Assert.IsTrue(logs.Exists(log => log.Contains("カーブの統合完了")), "カーブ統合完了ログがあるべき");
                Assert.IsTrue(logs.Exists(log => log.Contains("出力完了")), "出力完了ログがあるべき");

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
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

        [Test]
        public void 統合テスト_ContextMenuHandlerからの実行フロー()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("TestAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "ContextMenuTestTimeline";

            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");
            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 5);
            animClip.SetCurve("", typeof(Transform), "localPosition.z", curve);
            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            director.playableAsset = timeline;
            director.SetGenericBinding(track, animator);

            try
            {
                // Act: ContextMenuHandler経由で実行
                var success = ContextMenuHandler.ExecuteForPlayableDirectors(new[] { director });

                // Assert
                Assert.IsTrue(success, "ContextMenuHandlerからの実行が成功すべき");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(animatorGo);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);

                // 生成された可能性のあるアセットをクリーンアップ
                var generatedPath = "Assets/ContextMenuTestTimeline_TestAnimator_Merged.anim";
                if (AssetDatabase.LoadAssetAtPath<Object>(generatedPath) != null)
                {
                    AssetDatabase.DeleteAsset(generatedPath);
                }
            }
        }

        [Test]
        public void 統合テスト_TimelineAssetからの実行フロー()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "TimelineAssetTestTimeline";

            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");
            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 2, 100);
            animClip.SetCurve("", typeof(Transform), "localScale.x", curve);
            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 2;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            try
            {
                // Act: TimelineAssetから直接マージ
                var service = new AnimationMergeService();
                var results = service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);
                Assert.IsNotNull(results[0].GeneratedClip);
                Assert.IsNull(results[0].TargetAnimator, "TimelineAssetのみからの処理ではAnimatorはnull");

                // カーブを検証
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(1, bindings.Length);
                Assert.AreEqual("localScale.x", bindings[0].propertyName);

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        [Test]
        public void 統合テスト_複数Animatorへの分割出力()
        {
            // Arrange: 2つの異なるAnimatorにバインドされたトラック
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();

            var animatorGoA = new GameObject("AnimatorA");
            var animatorA = animatorGoA.AddComponent<Animator>();

            var animatorGoB = new GameObject("AnimatorB");
            var animatorB = animatorGoB.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "MultiAnimatorTestTimeline";

            // AnimatorA用トラック
            var trackA = timeline.CreateTrack<AnimationTrack>(null, "TrackForAnimatorA");
            var animClipA = new AnimationClip();
            var curveA = AnimationCurve.Linear(0, 0, 1, 1);
            animClipA.SetCurve("", typeof(Transform), "localPosition.x", curveA);
            var timelineClipA = trackA.CreateClip<AnimationPlayableAsset>();
            timelineClipA.start = 0;
            timelineClipA.duration = 1;
            (timelineClipA.asset as AnimationPlayableAsset).clip = animClipA;

            // AnimatorB用トラック
            var trackB = timeline.CreateTrack<AnimationTrack>(null, "TrackForAnimatorB");
            var animClipB = new AnimationClip();
            var curveB = AnimationCurve.Linear(0, 0, 1, 2);
            animClipB.SetCurve("", typeof(Transform), "localRotation.x", curveB);
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
                var service = new AnimationMergeService();
                var results = service.MergeFromPlayableDirector(director);

                // Assert: 2つの結果が返される
                Assert.AreEqual(2, results.Count, "2つのAnimator用に2つの結果が返されるべき");

                // 各Animatorに対応する結果を検証
                var resultForA = results.Find(r => r.TargetAnimator == animatorA);
                var resultForB = results.Find(r => r.TargetAnimator == animatorB);

                Assert.IsNotNull(resultForA, "AnimatorAの結果が存在すべき");
                Assert.IsNotNull(resultForB, "AnimatorBの結果が存在すべき");
                Assert.IsTrue(resultForA.IsSuccess);
                Assert.IsTrue(resultForB.IsSuccess);
                Assert.IsNotNull(resultForA.GeneratedClip);
                Assert.IsNotNull(resultForB.GeneratedClip);
                Assert.AreNotSame(resultForA.GeneratedClip, resultForB.GeneratedClip, "別々のクリップが生成されるべき");

                // 生成されたファイルパスを記録
                foreach (var result in results)
                {
                    RecordCreatedAssetPaths(result.Logs);
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
        public void 統合テスト_優先順位に基づくOverride処理()
        {
            // Arrange: 同一プロパティを持つ2つのトラック（重複部分あり）
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("TestAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "OverrideTestTimeline";

            // 低優先順位トラック（上の段、広い範囲）
            var lowPriorityTrack = timeline.CreateTrack<AnimationTrack>(null, "LowPriorityTrack");
            var lowPriorityClip = new AnimationClip();
            var lowPriorityCurve = AnimationCurve.Linear(0, 0, 2, 20);
            lowPriorityClip.SetCurve("", typeof(Transform), "localPosition.x", lowPriorityCurve);
            var timelineClipLow = lowPriorityTrack.CreateClip<AnimationPlayableAsset>();
            timelineClipLow.start = 0;
            timelineClipLow.duration = 2;
            (timelineClipLow.asset as AnimationPlayableAsset).clip = lowPriorityClip;

            // 高優先順位トラック（下の段、狭い範囲で上書き）
            var highPriorityTrack = timeline.CreateTrack<AnimationTrack>(null, "HighPriorityTrack");
            var highPriorityClip = new AnimationClip();
            var highPriorityCurve = AnimationCurve.Linear(0, 100, 1, 100); // 常に100
            highPriorityClip.SetCurve("", typeof(Transform), "localPosition.x", highPriorityCurve);
            var timelineClipHigh = highPriorityTrack.CreateClip<AnimationPlayableAsset>();
            timelineClipHigh.start = 0.5;
            timelineClipHigh.duration = 1;
            (timelineClipHigh.asset as AnimationPlayableAsset).clip = highPriorityClip;

            director.playableAsset = timeline;
            director.SetGenericBinding(lowPriorityTrack, animator);
            director.SetGenericBinding(highPriorityTrack, animator);

            try
            {
                // Act
                var service = new AnimationMergeService();
                var results = service.MergeFromPlayableDirector(director);

                // Assert
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);
                Assert.IsNotNull(results[0].GeneratedClip);

                // localPosition.xのカーブが存在することを確認
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(1, bindings.Length);
                Assert.AreEqual("localPosition.x", bindings[0].propertyName);

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(animatorGo);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(lowPriorityClip);
                Object.DestroyImmediate(highPriorityClip);
            }
        }

        [Test]
        public void 統合テスト_Muteトラックのフィルタリング()
        {
            // Arrange: 1つはMute、1つは有効なトラック
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("TestAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "MuteFilterTestTimeline";

            // 有効なトラック
            var activeTrack = timeline.CreateTrack<AnimationTrack>(null, "ActiveTrack");
            var activeClip = new AnimationClip();
            var activeCurve = AnimationCurve.Linear(0, 0, 1, 1);
            activeClip.SetCurve("", typeof(Transform), "localPosition.x", activeCurve);
            var activeTimelineClip = activeTrack.CreateClip<AnimationPlayableAsset>();
            activeTimelineClip.start = 0;
            activeTimelineClip.duration = 1;
            (activeTimelineClip.asset as AnimationPlayableAsset).clip = activeClip;

            // Muteされたトラック
            var mutedTrack = timeline.CreateTrack<AnimationTrack>(null, "MutedTrack");
            mutedTrack.muted = true;
            var mutedClip = new AnimationClip();
            var mutedCurve = AnimationCurve.Linear(0, 0, 1, 100);
            mutedClip.SetCurve("", typeof(Transform), "localPosition.y", mutedCurve);
            var mutedTimelineClip = mutedTrack.CreateClip<AnimationPlayableAsset>();
            mutedTimelineClip.start = 0;
            mutedTimelineClip.duration = 1;
            (mutedTimelineClip.asset as AnimationPlayableAsset).clip = mutedClip;

            director.playableAsset = timeline;
            director.SetGenericBinding(activeTrack, animator);
            director.SetGenericBinding(mutedTrack, animator);

            try
            {
                // Act
                var service = new AnimationMergeService();
                var results = service.MergeFromPlayableDirector(director);

                // Assert
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);

                // Muteトラックのカーブは含まれないことを確認
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(1, bindings.Length, "Muteトラックのカーブは含まれないべき");
                Assert.AreEqual("localPosition.x", bindings[0].propertyName, "有効なトラックのカーブのみ含まれるべき");

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(animatorGo);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(activeClip);
                Object.DestroyImmediate(mutedClip);
            }
        }

        [Test]
        public void 統合テスト_フレームレート設定の継承()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "FrameRateTestTimeline";

            // TimelineAssetのフレームレートを24fpsに設定
            var editorSettings = timeline.editorSettings;
            editorSettings.frameRate = 24.0;

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
                var service = new AnimationMergeService();
                var results = service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);
                Assert.IsNotNull(results[0].GeneratedClip);
                Assert.AreEqual(24f, results[0].GeneratedClip.frameRate, "フレームレートが継承されるべき");

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        [Test]
        public void 統合テスト_複数選択からの順次処理()
        {
            // Arrange: 2つの独立したPlayableDirector
            var go1 = new GameObject("TestDirector1");
            var director1 = go1.AddComponent<PlayableDirector>();
            var animatorGo1 = new GameObject("TestAnimator1");
            var animator1 = animatorGo1.AddComponent<Animator>();

            var go2 = new GameObject("TestDirector2");
            var director2 = go2.AddComponent<PlayableDirector>();
            var animatorGo2 = new GameObject("TestAnimator2");
            var animator2 = animatorGo2.AddComponent<Animator>();

            var timeline1 = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline1.name = "MultiSelect1";
            var track1 = timeline1.CreateTrack<AnimationTrack>(null, "Track1");
            var animClip1 = new AnimationClip();
            var curve1 = AnimationCurve.Linear(0, 0, 1, 1);
            animClip1.SetCurve("", typeof(Transform), "localPosition.x", curve1);
            var timelineClip1 = track1.CreateClip<AnimationPlayableAsset>();
            timelineClip1.start = 0;
            timelineClip1.duration = 1;
            (timelineClip1.asset as AnimationPlayableAsset).clip = animClip1;

            var timeline2 = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline2.name = "MultiSelect2";
            var track2 = timeline2.CreateTrack<AnimationTrack>(null, "Track2");
            var animClip2 = new AnimationClip();
            var curve2 = AnimationCurve.Linear(0, 0, 1, 2);
            animClip2.SetCurve("", typeof(Transform), "localPosition.y", curve2);
            var timelineClip2 = track2.CreateClip<AnimationPlayableAsset>();
            timelineClip2.start = 0;
            timelineClip2.duration = 1;
            (timelineClip2.asset as AnimationPlayableAsset).clip = animClip2;

            director1.playableAsset = timeline1;
            director1.SetGenericBinding(track1, animator1);
            director2.playableAsset = timeline2;
            director2.SetGenericBinding(track2, animator2);

            try
            {
                // Act: 複数のPlayableDirectorを順次処理
                var success = ContextMenuHandler.ExecuteForPlayableDirectors(new[] { director1, director2 });

                // Assert
                Assert.IsTrue(success, "複数選択からの処理が成功すべき");

                // 両方のアセットが生成されていることを確認
                var path1 = "Assets/MultiSelect1_TestAnimator1_Merged.anim";
                var path2 = "Assets/MultiSelect2_TestAnimator2_Merged.anim";

                _createdAssetPaths.Add(path1);
                _createdAssetPaths.Add(path2);
            }
            finally
            {
                Object.DestroyImmediate(go1);
                Object.DestroyImmediate(go2);
                Object.DestroyImmediate(animatorGo1);
                Object.DestroyImmediate(animatorGo2);
                Object.DestroyImmediate(timeline1);
                Object.DestroyImmediate(timeline2);
                Object.DestroyImmediate(animClip1);
                Object.DestroyImmediate(animClip2);
            }
        }

        #endregion

        #region エラーケース統合テスト

        [Test]
        public void 統合テスト_バインドなしトラックのエラー処理()
        {
            // Arrange: Animatorがバインドされていないトラック
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "UnboundTestTimeline";
            var track = timeline.CreateTrack<AnimationTrack>(null, "UnboundTrack");
            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            director.playableAsset = timeline;
            // Animatorをバインドしない

            try
            {
                // Act
                var service = new AnimationMergeService();
                var results = service.MergeFromPlayableDirector(director);

                // Assert: バインドがないので結果は空
                Assert.IsNotNull(results);
                Assert.AreEqual(0, results.Count, "バインドなしの場合は結果が空であるべき");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        /// <summary>
        /// ERR-001対応: バインドなしトラックをスキップして処理継続するテスト
        /// 要件定義書 ERR-001: バインドされていないトラック（Animator未設定）が存在する場合、
        /// エラーログを出力し、該当トラックをスキップして処理を継続
        /// </summary>
        [Test]
        public void ERR001_バインドなしトラックがある場合スキップして処理を継続する()
        {
            // Arrange: バインドされたトラックとバインドされていないトラックが混在
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();

            var animatorGo = new GameObject("TestAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "ERR001TestTimeline";

            // トラック1: Animatorにバインドされる有効なトラック
            var boundTrack = timeline.CreateTrack<AnimationTrack>(null, "BoundTrack");
            var animClip1 = new AnimationClip();
            var curve1 = AnimationCurve.Linear(0, 0, 1, 1);
            animClip1.SetCurve("", typeof(Transform), "localPosition.x", curve1);
            var timelineClip1 = boundTrack.CreateClip<AnimationPlayableAsset>();
            timelineClip1.start = 0;
            timelineClip1.duration = 1;
            (timelineClip1.asset as AnimationPlayableAsset).clip = animClip1;

            // トラック2: Animatorにバインドされない無効なトラック
            var unboundTrack = timeline.CreateTrack<AnimationTrack>(null, "UnboundTrack");
            var animClip2 = new AnimationClip();
            var curve2 = AnimationCurve.Linear(0, 0, 1, 2);
            animClip2.SetCurve("", typeof(Transform), "localPosition.y", curve2);
            var timelineClip2 = unboundTrack.CreateClip<AnimationPlayableAsset>();
            timelineClip2.start = 0;
            timelineClip2.duration = 1;
            (timelineClip2.asset as AnimationPlayableAsset).clip = animClip2;

            director.playableAsset = timeline;
            // boundTrackのみにAnimatorをバインド、unboundTrackにはバインドしない
            director.SetGenericBinding(boundTrack, animator);
            // unboundTrackはバインドしない

            // バインドなしトラックに対するエラーログを期待
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] トラック \"UnboundTrack\" にAnimatorがバインドされていません。");

            try
            {
                // Act
                var service = new AnimationMergeService();
                var results = service.MergeFromPlayableDirector(director);

                // Assert: バインドされたトラックのみ処理され、結果は1つ（バインドなしトラックはスキップ）
                Assert.IsNotNull(results, "結果がnullであってはならない");
                Assert.AreEqual(1, results.Count, "バインドされたAnimatorごとに結果が1つ生成されるべき");
                Assert.IsTrue(results[0].IsSuccess, "処理は成功すべき（バインドなしトラックはスキップして継続）");

                // 結果がバインドされたAnimatorに対するものであることを確認
                Assert.AreEqual(animator, results[0].TargetAnimator, "バインドされたAnimatorの結果であるべき");

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
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

        [Test]
        public void 統合テスト_空のTimelineAssetのエラー処理()
        {
            // Arrange: トラックのないTimelineAsset
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "EmptyTimeline";

            // エラーログを期待
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] 有効なAnimationTrackが見つかりません。");

            try
            {
                // Act
                var service = new AnimationMergeService();
                var results = service.MergeFromTimelineAsset(timeline);

                // Assert: 空のTimelineなので結果は空
                Assert.IsNotNull(results);
                Assert.AreEqual(0, results.Count, "空のTimelineの場合は結果が空であるべき");
            }
            finally
            {
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void 統合テスト_クリップなしトラックの処理()
        {
            // Arrange: クリップのないトラックと有効なトラック
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "EmptyTrackTestTimeline";

            // クリップのないトラック
            var emptyTrack = timeline.CreateTrack<AnimationTrack>(null, "EmptyTrack");

            // 有効なトラック
            var validTrack = timeline.CreateTrack<AnimationTrack>(null, "ValidTrack");
            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = validTrack.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            try
            {
                // Act
                var service = new AnimationMergeService();
                var results = service.MergeFromTimelineAsset(timeline);

                // Assert: 有効なトラックのみ処理される
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);

                // 空トラックに関するログが含まれることを確認
                var logs = results[0].Logs;
                Assert.IsTrue(logs.Exists(log => log.Contains("EmptyTrack") && log.Contains("クリップがありません")),
                    "空トラックに関するログがあるべき");

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        #endregion

        #region パフォーマンステスト

        /// <summary>
        /// タスク10.2.3: 非常に長いタイムラインの処理パフォーマンス確認
        /// 長時間（100秒以上）のタイムラインでも正常に処理できることを検証
        /// </summary>
        [Test]
        public void パフォーマンス_非常に長いタイムラインの処理()
        {
            // Arrange: 100秒を超える長いタイムラインを作成
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "LongTimelineTest";

            var track = timeline.CreateTrack<AnimationTrack>(null, "LongTrack");

            // 100秒以上のアニメーションクリップを作成
            var animClip = new AnimationClip();
            var longDuration = 120.0f; // 120秒（2分）のアニメーション

            // 複数のカーブを追加して負荷を増やす
            var curveX = AnimationCurve.Linear(0, 0, longDuration, 100);
            var curveY = AnimationCurve.Linear(0, 0, longDuration, 200);
            var curveZ = AnimationCurve.Linear(0, 0, longDuration, 300);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curveX);
            animClip.SetCurve("", typeof(Transform), "localPosition.y", curveY);
            animClip.SetCurve("", typeof(Transform), "localPosition.z", curveZ);

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = longDuration;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            try
            {
                // Act: 処理時間を計測
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var service = new AnimationMergeService();
                var results = service.MergeFromTimelineAsset(timeline);
                stopwatch.Stop();

                // Assert
                Assert.AreEqual(1, results.Count, "結果が1つ返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "処理は成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");

                // 生成されたクリップの長さを検証
                var generatedClip = results[0].GeneratedClip;
                Assert.GreaterOrEqual(generatedClip.length, longDuration - 0.1f, "生成されたクリップの長さが正しいべき");

                // カーブが正しく処理されていることを確認
                var bindings = AnimationUtility.GetCurveBindings(generatedClip);
                Assert.AreEqual(3, bindings.Length, "3つのカーブが含まれるべき");

                // パフォーマンス: 120秒のタイムラインは10秒以内に処理されるべき
                Assert.Less(stopwatch.ElapsedMilliseconds, 10000,
                    $"120秒のタイムラインは10秒以内に処理されるべき。実際: {stopwatch.ElapsedMilliseconds}ms");

                Debug.Log($"長いタイムライン（{longDuration}秒）の処理時間: {stopwatch.ElapsedMilliseconds}ms");

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        /// <summary>
        /// タスク10.2.3: 非常に長いタイムライン + 複数クリップの処理
        /// 長いタイムライン上に複数のクリップが配置された場合のパフォーマンス確認
        /// </summary>
        [Test]
        public void パフォーマンス_長いタイムラインに複数クリップ()
        {
            // Arrange: 長いタイムラインに多数のクリップを配置
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "LongTimelineMultiClipTest";

            var track = timeline.CreateTrack<AnimationTrack>(null, "MultiClipTrack");

            var createdClips = new List<AnimationClip>();
            var clipCount = 20;
            var clipDuration = 5.0;  // 各クリップ5秒
            var totalDuration = clipCount * clipDuration; // 合計100秒

            for (int i = 0; i < clipCount; i++)
            {
                var animClip = new AnimationClip();
                animClip.name = $"Clip_{i}";
                var curve = AnimationCurve.Linear(0, i * 10, (float)clipDuration, (i + 1) * 10);
                animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
                createdClips.Add(animClip);

                var timelineClip = track.CreateClip<AnimationPlayableAsset>();
                timelineClip.start = i * clipDuration;
                timelineClip.duration = clipDuration;
                (timelineClip.asset as AnimationPlayableAsset).clip = animClip;
            }

            try
            {
                // Act
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var service = new AnimationMergeService();
                var results = service.MergeFromTimelineAsset(timeline);
                stopwatch.Stop();

                // Assert
                Assert.AreEqual(1, results.Count, "結果が1つ返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "処理は成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");

                // 生成されたクリップの長さを検証
                var generatedClip = results[0].GeneratedClip;
                Assert.GreaterOrEqual(generatedClip.length, totalDuration - 0.1, "タイムライン全体の長さが反映されるべき");

                // パフォーマンス: 100秒のタイムラインは10秒以内に処理されるべき
                Assert.Less(stopwatch.ElapsedMilliseconds, 10000,
                    $"100秒・{clipCount}クリップのタイムラインは10秒以内に処理されるべき。実際: {stopwatch.ElapsedMilliseconds}ms");

                Debug.Log($"長いタイムライン（{totalDuration}秒・{clipCount}クリップ）の処理時間: {stopwatch.ElapsedMilliseconds}ms");

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                foreach (var clip in createdClips)
                {
                    Object.DestroyImmediate(clip);
                }
            }
        }

        /// <summary>
        /// タスク10.2.3: 極端に長いタイムラインの処理
        /// 600秒（10分）のタイムラインでも処理できることを検証
        /// </summary>
        [Test]
        public void パフォーマンス_極端に長いタイムラインの処理()
        {
            // Arrange: 600秒（10分）のタイムラインを作成
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "VeryLongTimelineTest";

            var track = timeline.CreateTrack<AnimationTrack>(null, "VeryLongTrack");

            var animClip = new AnimationClip();
            var veryLongDuration = 600.0f; // 600秒（10分）

            // シンプルなカーブを追加
            var curve = AnimationCurve.Linear(0, 0, veryLongDuration, 1000);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = veryLongDuration;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            try
            {
                // Act
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var service = new AnimationMergeService();
                var results = service.MergeFromTimelineAsset(timeline);
                stopwatch.Stop();

                // Assert
                Assert.AreEqual(1, results.Count, "結果が1つ返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "処理は成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");

                // 生成されたクリップの長さを検証
                var generatedClip = results[0].GeneratedClip;
                Assert.GreaterOrEqual(generatedClip.length, veryLongDuration - 0.1f, "生成されたクリップの長さが正しいべき");

                // パフォーマンス: 600秒のタイムラインは30秒以内に処理されるべき
                Assert.Less(stopwatch.ElapsedMilliseconds, 30000,
                    $"600秒のタイムラインは30秒以内に処理されるべき。実際: {stopwatch.ElapsedMilliseconds}ms");

                Debug.Log($"極端に長いタイムライン（{veryLongDuration}秒）の処理時間: {stopwatch.ElapsedMilliseconds}ms");

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        /// <summary>
        /// タスク10.2.4: 大量のカーブを持つAnimationClipの処理
        /// 多数のカーブを持つアニメーションでも正常に処理できることを検証
        /// </summary>
        [Test]
        public void パフォーマンス_大量のカーブを持つAnimationClipの処理()
        {
            // Arrange: 100個以上のカーブを持つAnimationClipを作成
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "ManyCurvesTest";

            var track = timeline.CreateTrack<AnimationTrack>(null, "ManyCurvesTrack");

            var animClip = new AnimationClip();
            var curveCount = 100; // 100個のカーブ
            var duration = 2.0f;

            // 様々なプロパティに対するカーブを追加
            for (int i = 0; i < curveCount; i++)
            {
                var curve = AnimationCurve.Linear(0, i, duration, i + 100);
                animClip.SetCurve($"Bone_{i}", typeof(Transform), "localPosition.x", curve);
            }

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = duration;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            try
            {
                // Act: 処理時間を計測
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var service = new AnimationMergeService();
                var results = service.MergeFromTimelineAsset(timeline);
                stopwatch.Stop();

                // Assert
                Assert.AreEqual(1, results.Count, "結果が1つ返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "処理は成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");

                // 全てのカーブが正しく処理されていることを確認
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(curveCount, bindings.Length, $"{curveCount}個のカーブが含まれるべき");

                // パフォーマンス: 100カーブは5秒以内に処理されるべき
                Assert.Less(stopwatch.ElapsedMilliseconds, 5000,
                    $"{curveCount}カーブは5秒以内に処理されるべき。実際: {stopwatch.ElapsedMilliseconds}ms");

                Debug.Log($"大量のカーブ（{curveCount}個）の処理時間: {stopwatch.ElapsedMilliseconds}ms");

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        /// <summary>
        /// タスク10.2.4: 複数トラックにまたがる大量のカーブの処理
        /// 複数のトラックに分散された大量のカーブを持つタイムラインでも正常に処理できることを検証
        /// </summary>
        [Test]
        public void パフォーマンス_複数トラックにまたがる大量のカーブ()
        {
            // Arrange: 5トラック × 50カーブ = 250カーブ
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "MultiTrackManyCurvesTest";

            var trackCount = 5;
            var curvesPerTrack = 50;
            var duration = 2.0f;

            var createdClips = new List<AnimationClip>();

            for (int t = 0; t < trackCount; t++)
            {
                var track = timeline.CreateTrack<AnimationTrack>(null, $"Track_{t}");
                var animClip = new AnimationClip();
                animClip.name = $"Clip_{t}";
                createdClips.Add(animClip);

                for (int c = 0; c < curvesPerTrack; c++)
                {
                    var curve = AnimationCurve.Linear(0, c, duration, c + 50);
                    animClip.SetCurve($"Bone_T{t}_C{c}", typeof(Transform), "localPosition.x", curve);
                }

                var timelineClip = track.CreateClip<AnimationPlayableAsset>();
                timelineClip.start = 0;
                timelineClip.duration = duration;
                (timelineClip.asset as AnimationPlayableAsset).clip = animClip;
            }

            try
            {
                // Act
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var service = new AnimationMergeService();
                var results = service.MergeFromTimelineAsset(timeline);
                stopwatch.Stop();

                // Assert
                Assert.AreEqual(1, results.Count, "結果が1つ返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "処理は成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");

                // 全てのカーブが正しく処理されていることを確認
                var totalCurves = trackCount * curvesPerTrack;
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(totalCurves, bindings.Length, $"{totalCurves}個のカーブが含まれるべき");

                // パフォーマンス: 250カーブは10秒以内に処理されるべき
                Assert.Less(stopwatch.ElapsedMilliseconds, 10000,
                    $"{totalCurves}カーブは10秒以内に処理されるべき。実際: {stopwatch.ElapsedMilliseconds}ms");

                Debug.Log($"複数トラックの大量カーブ（{trackCount}トラック×{curvesPerTrack}カーブ={totalCurves}個）の処理時間: {stopwatch.ElapsedMilliseconds}ms");

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                foreach (var clip in createdClips)
                {
                    Object.DestroyImmediate(clip);
                }
            }
        }

        /// <summary>
        /// タスク10.2.4: 極端に多いカーブを持つAnimationClipの処理
        /// 500個のカーブを持つアニメーションでも処理できることを検証
        /// </summary>
        [Test]
        public void パフォーマンス_極端に多いカーブを持つAnimationClipの処理()
        {
            // Arrange: 500個のカーブを持つAnimationClipを作成（Humanoidアバターの全ボーン×複数プロパティを想定）
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "VeryManyCurvesTest";

            var track = timeline.CreateTrack<AnimationTrack>(null, "VeryManyCurvesTrack");

            var animClip = new AnimationClip();
            var curveCount = 500; // 500個のカーブ
            var duration = 3.0f;

            // 様々なプロパティに対するカーブを追加（位置、回転、スケール）
            for (int i = 0; i < curveCount / 3; i++)
            {
                var curveX = AnimationCurve.Linear(0, i, duration, i + 10);
                var curveY = AnimationCurve.Linear(0, i * 2, duration, i * 2 + 10);
                var curveZ = AnimationCurve.Linear(0, i * 3, duration, i * 3 + 10);
                animClip.SetCurve($"Bone_{i}", typeof(Transform), "localPosition.x", curveX);
                animClip.SetCurve($"Bone_{i}", typeof(Transform), "localPosition.y", curveY);
                animClip.SetCurve($"Bone_{i}", typeof(Transform), "localPosition.z", curveZ);
            }

            // 残りのカーブを追加
            var remainingCurves = curveCount - (curveCount / 3) * 3;
            for (int i = 0; i < remainingCurves; i++)
            {
                var curve = AnimationCurve.Linear(0, i, duration, i + 5);
                animClip.SetCurve($"Extra_{i}", typeof(Transform), "localScale.x", curve);
            }

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = duration;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            try
            {
                // Act
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var service = new AnimationMergeService();
                var results = service.MergeFromTimelineAsset(timeline);
                stopwatch.Stop();

                // Assert
                Assert.AreEqual(1, results.Count, "結果が1つ返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "処理は成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");

                // カーブが正しく処理されていることを確認
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.GreaterOrEqual(bindings.Length, curveCount - 10, "ほぼ全てのカーブが含まれるべき");

                // パフォーマンス: 500カーブは15秒以内に処理されるべき
                Assert.Less(stopwatch.ElapsedMilliseconds, 15000,
                    $"{curveCount}カーブは15秒以内に処理されるべき。実際: {stopwatch.ElapsedMilliseconds}ms");

                Debug.Log($"極端に多いカーブ（{bindings.Length}個）の処理時間: {stopwatch.ElapsedMilliseconds}ms");

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        #endregion

        #region E2Eテスト（タスク10.3.3）

        /// <summary>
        /// E2Eテスト: 実際のTimelineAssetを使用した完全なワークフロー
        /// PlayableDirector設定からアセット生成・保存までの全フローを検証
        /// </summary>
        [Test]
        public void E2E_PlayableDirectorから完全なワークフローでAnimationClipが生成保存される()
        {
            // Arrange: 実際のシナリオに近い構成を作成
            var directorGo = new GameObject("E2E_TestDirector");
            var director = directorGo.AddComponent<PlayableDirector>();

            var characterGo = new GameObject("E2E_Character");
            var animator = characterGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "E2E_TestTimeline";

            // 複数のAnimationTrackを持つTimelineを構築
            var idleTrack = timeline.CreateTrack<AnimationTrack>(null, "IdleLayer");
            var idleClip = new AnimationClip { name = "E2E_IdleAnimation" };
            var idleCurve = AnimationCurve.Linear(0, 0, 2, 0); // 静止状態
            idleClip.SetCurve("", typeof(Transform), "localPosition.y", idleCurve);
            var idleTimelineClip = idleTrack.CreateClip<AnimationPlayableAsset>();
            idleTimelineClip.start = 0;
            idleTimelineClip.duration = 3;
            (idleTimelineClip.asset as AnimationPlayableAsset).clip = idleClip;

            var actionTrack = timeline.CreateTrack<AnimationTrack>(null, "ActionLayer");
            var jumpClip = new AnimationClip { name = "E2E_JumpAnimation" };
            var jumpCurve = new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.5f, 2),
                new Keyframe(1, 0)
            );
            jumpClip.SetCurve("", typeof(Transform), "localPosition.y", jumpCurve);
            var jumpTimelineClip = actionTrack.CreateClip<AnimationPlayableAsset>();
            jumpTimelineClip.start = 1;
            jumpTimelineClip.duration = 1;
            (jumpTimelineClip.asset as AnimationPlayableAsset).clip = jumpClip;

            director.playableAsset = timeline;
            director.SetGenericBinding(idleTrack, animator);
            director.SetGenericBinding(actionTrack, animator);

            string expectedAssetPath = "Assets/E2E_TestTimeline_E2E_Character_Merged.anim";

            try
            {
                // Act: コンテキストメニュー経由で実行をシミュレート
                var success = ContextMenuHandler.ExecuteForPlayableDirectors(new[] { director });

                // Assert
                Assert.IsTrue(success, "E2E処理が成功すべき");

                // 生成されたアセットがAssets直下に存在することを確認
                var generatedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(expectedAssetPath);
                if (generatedClip == null)
                {
                    // 連番付きパスを確認
                    generatedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/E2E_TestTimeline_E2E_Character_Merged(1).anim");
                }

                Assert.IsNotNull(generatedClip, "AnimationClipアセットが生成されるべき");

                // 生成されたクリップの内容を検証
                var bindings = AnimationUtility.GetCurveBindings(generatedClip);
                Assert.GreaterOrEqual(bindings.Length, 1, "少なくとも1つのカーブが含まれるべき");

                // localPosition.yカーブが存在することを確認
                var hasPositionY = System.Array.Exists(bindings, b => b.propertyName == "localPosition.y");
                Assert.IsTrue(hasPositionY, "localPosition.yカーブが含まれるべき");

                // クリーンアップ用にパスを記録
                _createdAssetPaths.Add(expectedAssetPath);
                _createdAssetPaths.Add("Assets/E2E_TestTimeline_E2E_Character_Merged(1).anim");
            }
            finally
            {
                Object.DestroyImmediate(directorGo);
                Object.DestroyImmediate(characterGo);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(jumpClip);
            }
        }

        /// <summary>
        /// E2Eテスト: TimelineAsset選択からの実行フロー
        /// ProjectビューからTimelineAssetを選択して実行するシナリオを検証
        /// </summary>
        [Test]
        public void E2E_TimelineAsset選択からの実行でAnimationClipが生成保存される()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "E2E_ProjectViewTimeline";

            var track = timeline.CreateTrack<AnimationTrack>(null, "MainTrack");
            var animClip = new AnimationClip { name = "E2E_WalkAnimation" };

            // 複数のプロパティにカーブを設定（より現実的なシナリオ）
            var posXCurve = AnimationCurve.Linear(0, 0, 2, 5);
            var posYCurve = AnimationCurve.Linear(0, 0, 2, 0);
            var posZCurve = AnimationCurve.Linear(0, 0, 2, 10);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", posXCurve);
            animClip.SetCurve("", typeof(Transform), "localPosition.y", posYCurve);
            animClip.SetCurve("", typeof(Transform), "localPosition.z", posZCurve);

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 2;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            string expectedAssetPath = "Assets/E2E_ProjectViewTimeline_Merged.anim";

            try
            {
                // Act: TimelineAssetからの実行
                var success = ContextMenuHandler.ExecuteForTimelineAssets(new[] { timeline });

                // Assert
                Assert.IsTrue(success, "TimelineAssetからの処理が成功すべき");

                // 生成されたアセットが存在することを確認
                AssetDatabase.Refresh();
                var generatedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(expectedAssetPath);
                if (generatedClip == null)
                {
                    generatedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/E2E_ProjectViewTimeline_Merged(1).anim");
                }

                Assert.IsNotNull(generatedClip, "AnimationClipアセットが生成されるべき");

                // 全てのカーブが含まれていることを確認
                var bindings = AnimationUtility.GetCurveBindings(generatedClip);
                Assert.AreEqual(3, bindings.Length, "3つのカーブが含まれるべき");

                // クリーンアップ用にパスを記録
                _createdAssetPaths.Add(expectedAssetPath);
                _createdAssetPaths.Add("Assets/E2E_ProjectViewTimeline_Merged(1).anim");
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        /// <summary>
        /// E2Eテスト: Selection APIを使用したHierarchyビューからのコンテキストメニュー実行
        /// FR-001の完全なテスト
        /// </summary>
        [Test]
        public void E2E_HierarchyビューSelection経由でのコンテキストメニュー実行()
        {
            // Arrange
            var directorGo = new GameObject("E2E_HierarchyTest_Director");
            var director = directorGo.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("E2E_HierarchyTest_Animator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "E2E_HierarchyTestTimeline";

            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");
            var animClip = new AnimationClip { name = "E2E_HierarchyClip" };
            var curve = AnimationCurve.EaseInOut(0, 0, 1, 100);
            animClip.SetCurve("", typeof(Transform), "localScale.x", curve);
            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            director.playableAsset = timeline;
            director.SetGenericBinding(track, animator);

            // Selection APIを使用してGameObjectを選択状態にする
            var originalSelection = Selection.objects;
            Selection.activeGameObject = directorGo;

            try
            {
                // Act: Selection.gameObjectsからPlayableDirectorを取得して実行
                var directors = ContextMenuHandler.GetSelectedPlayableDirectors();
                Assert.AreEqual(1, directors.Length, "選択されたPlayableDirectorが1つであるべき");

                // メニューの有効状態を確認
                Assert.IsTrue(ContextMenuHandler.CanExecuteFromHierarchy(), "Hierarchyメニューが有効であるべき");

                // 実行
                var success = ContextMenuHandler.ExecuteForPlayableDirectors(directors);

                // Assert
                Assert.IsTrue(success, "Hierarchy経由の処理が成功すべき");

                // クリーンアップ用にパスを記録
                _createdAssetPaths.Add("Assets/E2E_HierarchyTestTimeline_E2E_HierarchyTest_Animator_Merged.anim");
                _createdAssetPaths.Add("Assets/E2E_HierarchyTestTimeline_E2E_HierarchyTest_Animator_Merged(1).anim");
            }
            finally
            {
                Selection.objects = originalSelection;
                Object.DestroyImmediate(directorGo);
                Object.DestroyImmediate(animatorGo);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        /// <summary>
        /// E2Eテスト: 複雑なTimeline構造（複数トラック、ブレンド、オーバーライド）の処理
        /// 実際のプロダクション環境に近い複雑なシナリオを検証
        /// </summary>
        [Test]
        public void E2E_複雑なTimeline構造の完全な処理フロー()
        {
            // Arrange: 複雑なTimeline構造を作成
            var directorGo = new GameObject("E2E_ComplexDirector");
            var director = directorGo.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("E2E_ComplexAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "E2E_ComplexTimeline";

            // ベースレイヤー（長いクリップ）
            var baseTrack = timeline.CreateTrack<AnimationTrack>(null, "BaseLayer");
            var baseClip = new AnimationClip { name = "E2E_BaseAnim" };
            baseClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 5, 50));
            baseClip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, 0, 5, 0));
            baseClip.SetCurve("", typeof(Transform), "localPosition.z", AnimationCurve.Linear(0, 0, 5, 50));
            var baseTimelineClip = baseTrack.CreateClip<AnimationPlayableAsset>();
            baseTimelineClip.start = 0;
            baseTimelineClip.duration = 5;
            (baseTimelineClip.asset as AnimationPlayableAsset).clip = baseClip;

            // オーバーライドレイヤー（部分的に上書き）
            var overrideTrack = timeline.CreateTrack<AnimationTrack>(null, "OverrideLayer");
            var overrideClip = new AnimationClip { name = "E2E_OverrideAnim" };
            overrideClip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.EaseInOut(0, 0, 2, 10));
            var overrideTimelineClip = overrideTrack.CreateClip<AnimationPlayableAsset>();
            overrideTimelineClip.start = 1.5;
            overrideTimelineClip.duration = 2;
            (overrideTimelineClip.asset as AnimationPlayableAsset).clip = overrideClip;

            // 追加レイヤー（異なるプロパティ）
            var additionalTrack = timeline.CreateTrack<AnimationTrack>(null, "AdditionalLayer");
            var additionalClip = new AnimationClip { name = "E2E_AdditionalAnim" };
            additionalClip.SetCurve("", typeof(Transform), "localRotation.y", AnimationCurve.Linear(0, 0, 3, 180));
            var additionalTimelineClip = additionalTrack.CreateClip<AnimationPlayableAsset>();
            additionalTimelineClip.start = 0.5;
            additionalTimelineClip.duration = 3;
            (additionalTimelineClip.asset as AnimationPlayableAsset).clip = additionalClip;

            director.playableAsset = timeline;
            director.SetGenericBinding(baseTrack, animator);
            director.SetGenericBinding(overrideTrack, animator);
            director.SetGenericBinding(additionalTrack, animator);

            try
            {
                // Act
                var service = new AnimationMergeService();
                var results = service.MergeFromPlayableDirector(director);

                // Assert
                Assert.AreEqual(1, results.Count, "1つのAnimator用の結果が返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "複雑なTimeline構造の処理が成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");

                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);

                // 複数のプロパティが含まれていることを確認
                var propertyNames = new HashSet<string>();
                foreach (var binding in bindings)
                {
                    propertyNames.Add(binding.propertyName);
                }

                Assert.IsTrue(propertyNames.Contains("localPosition.x"), "localPosition.xが含まれるべき");
                Assert.IsTrue(propertyNames.Contains("localPosition.y"), "localPosition.yが含まれるべき");
                Assert.IsTrue(propertyNames.Contains("localPosition.z"), "localPosition.zが含まれるべき");
                Assert.IsTrue(propertyNames.Contains("localRotation.y"), "localRotation.yが含まれるべき");

                // 生成されたクリップの長さが適切であることを確認
                Assert.GreaterOrEqual(results[0].GeneratedClip.length, 4.5f, "クリップの長さが十分であるべき");

                // ログが適切に記録されていることを確認
                Assert.IsTrue(results[0].Logs.Count > 0, "処理ログが記録されるべき");
                Assert.IsTrue(results[0].Logs.Exists(log => log.Contains("処理開始")), "処理開始ログがあるべき");
                Assert.IsTrue(results[0].Logs.Exists(log => log.Contains("出力完了")), "出力完了ログがあるべき");

                // クリーンアップ用にパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(directorGo);
                Object.DestroyImmediate(animatorGo);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(baseClip);
                Object.DestroyImmediate(overrideClip);
                Object.DestroyImmediate(additionalClip);
            }
        }

        /// <summary>
        /// E2Eテスト: EaseIn/EaseOutブレンドを含むTimelineの処理
        /// </summary>
        [Test]
        public void E2E_ブレンド設定を含むTimelineの処理()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "E2E_BlendTimeline";

            var track = timeline.CreateTrack<AnimationTrack>(null, "BlendTrack");

            // 最初のクリップ（EaseOutあり）
            var clip1 = new AnimationClip { name = "E2E_BlendClip1" };
            clip1.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 10));
            var timelineClip1 = track.CreateClip<AnimationPlayableAsset>();
            timelineClip1.start = 0;
            timelineClip1.duration = 1.5;
            timelineClip1.easeOutDuration = 0.3;
            (timelineClip1.asset as AnimationPlayableAsset).clip = clip1;

            // 2番目のクリップ（EaseInあり、重複区間でブレンド）
            var clip2 = new AnimationClip { name = "E2E_BlendClip2" };
            clip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 20, 1, 30));
            var timelineClip2 = track.CreateClip<AnimationPlayableAsset>();
            timelineClip2.start = 1.2; // 重複区間を作成
            timelineClip2.duration = 1.5;
            timelineClip2.easeInDuration = 0.3;
            (timelineClip2.asset as AnimationPlayableAsset).clip = clip2;

            try
            {
                // Act
                var service = new AnimationMergeService();
                var results = service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.AreEqual(1, results.Count, "結果が1つ返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "ブレンド処理が成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");

                var generatedClip = results[0].GeneratedClip;
                var bindings = AnimationUtility.GetCurveBindings(generatedClip);
                Assert.AreEqual(1, bindings.Length, "1つのカーブが含まれるべき");

                // 生成されたクリップの長さがタイムライン全体をカバーしていることを確認
                Assert.GreaterOrEqual(generatedClip.length, 2.5f, "クリップの長さがタイムライン全体をカバーすべき");

                // クリーンアップ用にパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(clip1);
                Object.DestroyImmediate(clip2);
            }
        }

        /// <summary>
        /// E2Eテスト: 複数選択からの順次処理（FR-003準拠）
        /// </summary>
        [Test]
        public void E2E_複数PlayableDirector選択からの順次処理()
        {
            // Arrange: 2つの独立したセットアップ
            var director1Go = new GameObject("E2E_MultiSelect_Director1");
            var director1 = director1Go.AddComponent<PlayableDirector>();
            var animator1Go = new GameObject("E2E_MultiSelect_Animator1");
            var animator1 = animator1Go.AddComponent<Animator>();

            var director2Go = new GameObject("E2E_MultiSelect_Director2");
            var director2 = director2Go.AddComponent<PlayableDirector>();
            var animator2Go = new GameObject("E2E_MultiSelect_Animator2");
            var animator2 = animator2Go.AddComponent<Animator>();

            var timeline1 = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline1.name = "E2E_MultiSelect1";
            var track1 = timeline1.CreateTrack<AnimationTrack>(null, "Track1");
            var clip1 = new AnimationClip { name = "E2E_Clip1" };
            clip1.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 10));
            var tc1 = track1.CreateClip<AnimationPlayableAsset>();
            tc1.start = 0;
            tc1.duration = 1;
            (tc1.asset as AnimationPlayableAsset).clip = clip1;

            var timeline2 = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline2.name = "E2E_MultiSelect2";
            var track2 = timeline2.CreateTrack<AnimationTrack>(null, "Track2");
            var clip2 = new AnimationClip { name = "E2E_Clip2" };
            clip2.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, 0, 1, 20));
            var tc2 = track2.CreateClip<AnimationPlayableAsset>();
            tc2.start = 0;
            tc2.duration = 1;
            (tc2.asset as AnimationPlayableAsset).clip = clip2;

            director1.playableAsset = timeline1;
            director1.SetGenericBinding(track1, animator1);
            director2.playableAsset = timeline2;
            director2.SetGenericBinding(track2, animator2);

            try
            {
                // Act: 複数のPlayableDirectorを順次処理
                var success = ContextMenuHandler.ExecuteForPlayableDirectors(new[] { director1, director2 });

                // Assert
                Assert.IsTrue(success, "複数選択からの処理が成功すべき");

                // 両方のアセットが生成されていることを確認
                AssetDatabase.Refresh();

                // クリーンアップ用にパスを記録
                _createdAssetPaths.Add("Assets/E2E_MultiSelect1_E2E_MultiSelect_Animator1_Merged.anim");
                _createdAssetPaths.Add("Assets/E2E_MultiSelect1_E2E_MultiSelect_Animator1_Merged(1).anim");
                _createdAssetPaths.Add("Assets/E2E_MultiSelect2_E2E_MultiSelect_Animator2_Merged.anim");
                _createdAssetPaths.Add("Assets/E2E_MultiSelect2_E2E_MultiSelect_Animator2_Merged(1).anim");
            }
            finally
            {
                Object.DestroyImmediate(director1Go);
                Object.DestroyImmediate(director2Go);
                Object.DestroyImmediate(animator1Go);
                Object.DestroyImmediate(animator2Go);
                Object.DestroyImmediate(timeline1);
                Object.DestroyImmediate(timeline2);
                Object.DestroyImmediate(clip1);
                Object.DestroyImmediate(clip2);
            }
        }

        /// <summary>
        /// E2Eテスト: GroupTrackを含む階層構造の処理
        /// </summary>
        [Test]
        public void E2E_GroupTrackを含む階層構造のTimeline処理()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "E2E_GroupedTimeline";

            // GroupTrackを作成
            var group = timeline.CreateTrack<GroupTrack>(null, "AnimationGroup");

            // グループ内にAnimationTrackを作成
            var track1 = timeline.CreateTrack<AnimationTrack>(group, "GroupedTrack1");
            var clip1 = new AnimationClip { name = "E2E_GroupedClip1" };
            clip1.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 5));
            var tc1 = track1.CreateClip<AnimationPlayableAsset>();
            tc1.start = 0;
            tc1.duration = 1;
            (tc1.asset as AnimationPlayableAsset).clip = clip1;

            var track2 = timeline.CreateTrack<AnimationTrack>(group, "GroupedTrack2");
            var clip2 = new AnimationClip { name = "E2E_GroupedClip2" };
            clip2.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, 0, 1, 10));
            var tc2 = track2.CreateClip<AnimationPlayableAsset>();
            tc2.start = 0.5;
            tc2.duration = 1;
            (tc2.asset as AnimationPlayableAsset).clip = clip2;

            try
            {
                // Act
                var service = new AnimationMergeService();
                var results = service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.AreEqual(1, results.Count, "結果が1つ返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "GroupTrackを含むTimeline処理が成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");

                // 両方のトラックからのカーブが含まれていることを確認
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(2, bindings.Length, "2つのカーブが含まれるべき");

                // クリーンアップ用にパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(clip1);
                Object.DestroyImmediate(clip2);
            }
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// ログから生成されたアセットパスを抽出して記録する
        /// </summary>
        private void RecordCreatedAssetPaths(List<string> logs)
        {
            foreach (var log in logs)
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

                    // コロン区切りでパスを探す
                    var colonParts = log.Split(':');
                    if (colonParts.Length > 1)
                    {
                        var path = colonParts[colonParts.Length - 1].Trim();
                        if (path.EndsWith(".anim"))
                        {
                            _createdAssetPaths.Add(path);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
