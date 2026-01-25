using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
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
                Assert.AreEqual(animator, results[0].BoundAnimator, "バインドされたAnimatorの結果であるべき");

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
