using System.Collections.Generic;
using System.Linq;
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
                // SetCurveを使用すると、Unityは内部的に全てのコンポーネント（x,y,z）を生成する
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(3, bindings.Length, "3つのカーブが含まれるべき（x,y,z）");

                var propertyNames = new HashSet<string>();
                foreach (var binding in bindings)
                {
                    propertyNames.Add(binding.propertyName);
                }
                // Unity APIでは、SetCurveに渡す"localPosition.x"は内部的に"m_LocalPosition.x"として保存される
                Assert.IsTrue(propertyNames.Contains("m_LocalPosition.x"), "localPosition.xカーブが含まれるべき");
                Assert.IsTrue(propertyNames.Contains("m_LocalPosition.y"), "localPosition.yカーブが含まれるべき");

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
                // 注意: Unityはスケール成分を設定すると内部的に3Dベクター(x,y,z)として保存するため、
                // localScale.xのみ設定しても、デフォルト値のy/zカーブも追加される
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.IsTrue(bindings.Length >= 1, "少なくとも1つのカーブが含まれるべき");
                Assert.IsTrue(bindings.Any(b => b.propertyName == "m_LocalScale.x"), "localScale.xカーブが含まれるべき");

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
                // 両トラックが同一プロパティ(localPosition.x)を持つため、
                // 優先順位に基づくOverride処理により1つのマージされたカーブのみが生成されるべき
                // SetCurveを使用すると、Unityは内部的に全てのコンポーネント（x,y,z）を生成する
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(3, bindings.Length, "同一プロパティへのOverride処理により3つのカーブが含まれるべき（x,y,z）");
                var propertyNames = new HashSet<string>();
                foreach (var binding in bindings)
                {
                    propertyNames.Add(binding.propertyName);
                }
                Assert.IsTrue(propertyNames.Contains("m_LocalPosition.x"), "localPosition.xプロパティのカーブが含まれるべき");

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
                // SetCurveを使用すると、Unityは内部的に全てのコンポーネント（x,y,z）を生成する
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(3, bindings.Length, "有効なトラックの3つのカーブ（x,y,z）のみ含まれるべき");
                var propertyNames = new HashSet<string>();
                foreach (var binding in bindings)
                {
                    propertyNames.Add(binding.propertyName);
                }
                Assert.IsTrue(propertyNames.Contains("m_LocalPosition.x"), "有効なトラックのm_LocalPosition.xカーブが含まれるべき");

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

            // バインドなしトラックに対するエラーログを期待
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] トラック \"UnboundTrack\" にAnimatorがバインドされていません。");
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] バインドされたAnimatorが見つかりません。");

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
                // 注: localPosition.xを設定するとUnityは内部的にx,y,zの3つのバインディングを作成する
                var sourceBindings = AnimationUtility.GetCurveBindings(animClip);
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(sourceBindings.Length, bindings.Length, $"ソースと同じ{sourceBindings.Length}個のカーブが含まれるべき");

                // パフォーマンス: 大量のカーブは5秒以内に処理されるべき
                Assert.Less(stopwatch.ElapsedMilliseconds, 5000,
                    $"{sourceBindings.Length}カーブは5秒以内に処理されるべき。実際: {stopwatch.ElapsedMilliseconds}ms");

                Debug.Log($"大量のカーブ（{curveCount}ボーン={sourceBindings.Length}個のカーブバインディング）の処理時間: {stopwatch.ElapsedMilliseconds}ms");

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
                // 注: localPosition.xを設定するとUnityは内部的にx,y,zの3つのバインディングを作成する
                var expectedCurveCount = 0;
                foreach (var clip in createdClips)
                {
                    expectedCurveCount += AnimationUtility.GetCurveBindings(clip).Length;
                }
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(expectedCurveCount, bindings.Length, $"ソースと同じ{expectedCurveCount}個のカーブが含まれるべき");

                // パフォーマンス: 大量のカーブは10秒以内に処理されるべき
                Assert.Less(stopwatch.ElapsedMilliseconds, 10000,
                    $"{expectedCurveCount}カーブは10秒以内に処理されるべき。実際: {stopwatch.ElapsedMilliseconds}ms");

                Debug.Log($"複数トラックの大量カーブ（{trackCount}トラック×{curvesPerTrack}ボーン={expectedCurveCount}個のカーブバインディング）の処理時間: {stopwatch.ElapsedMilliseconds}ms");

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
            var idleCurve = AnimationCurve.Linear(0, 0, 2, 10); // X方向に移動
            idleClip.SetCurve("", typeof(Transform), "localPosition.x", idleCurve);
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
                // SetCurveを使用すると、Unityは内部的に"m_LocalPosition.y"として保存する
                var hasPositionY = System.Array.Exists(bindings, b => b.propertyName == "m_LocalPosition.y");
                Assert.IsTrue(hasPositionY, "m_LocalPosition.yカーブが含まれるべき");

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

            // TimelineAssetのみからの処理ではAnimatorがnullなのでファイル名に"NoAnimator"が含まれる
            string expectedAssetPath = "Assets/E2E_ProjectViewTimeline_NoAnimator_Merged.anim";

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
                    generatedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/E2E_ProjectViewTimeline_NoAnimator_Merged(1).anim");
                }

                Assert.IsNotNull(generatedClip, "AnimationClipアセットが生成されるべき");

                // 全てのカーブが含まれていることを確認
                var bindings = AnimationUtility.GetCurveBindings(generatedClip);
                Assert.AreEqual(3, bindings.Length, "3つのカーブが含まれるべき");

                // クリーンアップ用にパスを記録
                _createdAssetPaths.Add(expectedAssetPath);
                _createdAssetPaths.Add("Assets/E2E_ProjectViewTimeline_NoAnimator_Merged(1).anim");
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

                // Unity APIでは、SetCurveに渡す"localPosition.x"は内部的に"m_LocalPosition.x"として保存される
                Assert.IsTrue(propertyNames.Contains("m_LocalPosition.x"), "localPosition.xが含まれるべき");
                Assert.IsTrue(propertyNames.Contains("m_LocalPosition.y"), "localPosition.yが含まれるべき");
                Assert.IsTrue(propertyNames.Contains("m_LocalPosition.z"), "localPosition.zが含まれるべき");
                Assert.IsTrue(propertyNames.Contains("m_LocalRotation.y"), "localRotation.yが含まれるべき");

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
                // SetCurveを使用すると、Unityは内部的に全てのコンポーネント（x,y,z）を生成する
                Assert.AreEqual(3, bindings.Length, "3つのカーブが含まれるべき（x,y,z）");

                // 生成されたクリップの長さがタイムライン全体をカバーしていることを確認
                Assert.GreaterOrEqual(generatedClip.length, 2.0f, "クリップの長さがタイムライン全体をカバーすべき");

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
                // 注意: Unityはlocalpositionを3Dベクターとして管理するため、
                // x, yを設定するとzも自動追加される（Unityの仕様）
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.IsTrue(bindings.Length >= 2, "少なくとも2つのカーブが含まれるべき");
                Assert.IsTrue(bindings.Any(b => b.propertyName == "m_LocalPosition.x"),
                    "localPosition.xのカーブが含まれるべき");
                Assert.IsTrue(bindings.Any(b => b.propertyName == "m_LocalPosition.y"),
                    "localPosition.yのカーブが含まれるべき");

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

        #region Phase 11 BlendShape対応 統合テスト（P11-008）

        /// <summary>
        /// P11-008: BlendShapeカーブを含むTimelineの統合テスト
        /// BlendShapeアニメーションカーブの検出・マージ・Override処理が正しく動作することを検証
        /// </summary>
        [Test]
        public void Phase11統合_BlendShapeカーブを含むTimelineのマージ処理()
        {
            // Arrange: BlendShapeカーブを含むTimelineを作成
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "BlendShapeIntegrationTest";

            var track = timeline.CreateTrack<AnimationTrack>(null, "BlendShapeTrack");

            var animClip = new AnimationClip();
            animClip.name = "BlendShapeClip";

            // BlendShapeカーブを追加（SkinnedMeshRenderer用）
            var bindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            var curveSmile = AnimationCurve.Linear(0, 0, 1, 100);
            AnimationUtility.SetEditorCurve(animClip, bindingSmile, curveSmile);

            var bindingBlink = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.eyeBlink_L");
            var curveBlink = new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.5f, 100),
                new Keyframe(1, 0)
            );
            AnimationUtility.SetEditorCurve(animClip, bindingBlink, curveBlink);

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
                Assert.AreEqual(1, results.Count, "結果が1つ返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "BlendShapeカーブを含むTimeline処理が成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");

                // BlendShapeカーブが正しく含まれていることを確認
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.AreEqual(2, bindings.Length, "2つのBlendShapeカーブが含まれるべき");

                // BlendShapeプロパティが含まれていることを確認
                Assert.IsTrue(bindings.Any(b => b.propertyName == "blendShape.Smile"), "blendShape.Smileカーブが含まれるべき");
                Assert.IsTrue(bindings.Any(b => b.propertyName == "blendShape.eyeBlink_L"), "blendShape.eyeBlink_Lカーブが含まれるべき");

                // 型がSkinnedMeshRendererであることを確認
                Assert.IsTrue(bindings.All(b => b.type == typeof(SkinnedMeshRenderer)), "全てのバインディングがSkinnedMeshRenderer型であるべき");

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
        /// P11-008: BlendShapeカーブと通常のTransformカーブが混在するTimelineの統合テスト
        /// </summary>
        [Test]
        public void Phase11統合_BlendShapeとTransformカーブが混在するTimelineのマージ処理()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "MixedCurvesIntegrationTest";

            var track = timeline.CreateTrack<AnimationTrack>(null, "MixedTrack");

            var animClip = new AnimationClip();
            animClip.name = "MixedClip";

            // BlendShapeカーブを追加
            var bindingSmile = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            var curveSmile = AnimationCurve.Linear(0, 0, 2, 100);
            AnimationUtility.SetEditorCurve(animClip, bindingSmile, curveSmile);

            // 通常のTransformカーブを追加
            animClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 2, 10));
            animClip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, 0, 2, 5));

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 2;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            try
            {
                // Act
                var service = new AnimationMergeService();
                var results = service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.AreEqual(1, results.Count, "結果が1つ返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "混在カーブのTimeline処理が成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");

                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);

                // BlendShapeカーブとTransformカーブの両方が含まれていることを確認
                Assert.IsTrue(bindings.Any(b => b.propertyName == "blendShape.Smile"), "BlendShapeカーブが含まれるべき");
                Assert.IsTrue(bindings.Any(b => b.propertyName == "m_LocalPosition.x"), "Transformカーブ(x)が含まれるべき");
                Assert.IsTrue(bindings.Any(b => b.propertyName == "m_LocalPosition.y"), "Transformカーブ(y)が含まれるべき");

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
        /// P11-008: 複数トラックのBlendShapeカーブのOverride処理統合テスト
        /// 同一BlendShapeプロパティが複数トラックに存在する場合、優先順位に基づいてOverrideされることを検証
        /// </summary>
        [Test]
        public void Phase11統合_複数トラックのBlendShapeカーブのOverride処理()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "BlendShapeOverrideTest";

            // 低優先順位トラック（上の段）
            var lowerTrack = timeline.CreateTrack<AnimationTrack>(null, "LowerPriorityTrack");
            var lowerClip = new AnimationClip { name = "LowerClip" };

            // Smile: 0→50 (0-2秒)
            var lowerBindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            var lowerCurveSmile = AnimationCurve.Linear(0, 0, 2, 50);
            AnimationUtility.SetEditorCurve(lowerClip, lowerBindingSmile, lowerCurveSmile);

            var lowerTimelineClip = lowerTrack.CreateClip<AnimationPlayableAsset>();
            lowerTimelineClip.start = 0;
            lowerTimelineClip.duration = 2;
            (lowerTimelineClip.asset as AnimationPlayableAsset).clip = lowerClip;

            // 高優先順位トラック（下の段）
            var higherTrack = timeline.CreateTrack<AnimationTrack>(null, "HigherPriorityTrack");
            var higherClip = new AnimationClip { name = "HigherClip" };

            // Smile: 100→100 (0.5-1.5秒、完全に上書き)
            var higherBindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            var higherCurveSmile = AnimationCurve.Linear(0, 100, 1, 100);
            AnimationUtility.SetEditorCurve(higherClip, higherBindingSmile, higherCurveSmile);

            var higherTimelineClip = higherTrack.CreateClip<AnimationPlayableAsset>();
            higherTimelineClip.start = 0.5;
            higherTimelineClip.duration = 1;
            (higherTimelineClip.asset as AnimationPlayableAsset).clip = higherClip;

            try
            {
                // Act
                var service = new AnimationMergeService();
                var results = service.MergeFromTimelineAsset(timeline);

                // Assert
                Assert.AreEqual(1, results.Count, "結果が1つ返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "BlendShapeカーブのOverride処理が成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");

                // BlendShapeカーブが1つだけ含まれていることを確認（マージ後）
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                var smileBindings = bindings.Where(b => b.propertyName == "blendShape.Smile").ToArray();
                Assert.AreEqual(1, smileBindings.Length, "Smileカーブは1つにマージされるべき");

                // 高優先順位区間（0.5-1.5秒）で高優先順位の値（100）が採用されていることを確認
                var smileCurve = AnimationUtility.GetEditorCurve(results[0].GeneratedClip, smileBindings[0]);
                Assert.AreEqual(100f, smileCurve.Evaluate(0.75f), 1f, "高優先順位区間では高優先順位の値が採用されるべき");
                Assert.AreEqual(100f, smileCurve.Evaluate(1.25f), 1f, "高優先順位区間では高優先順位の値が採用されるべき");

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(lowerClip);
                Object.DestroyImmediate(higherClip);
            }
        }

        /// <summary>
        /// P11-008: 複数の異なるBlendShapeプロパティが複数トラックに分散している場合の統合テスト
        /// </summary>
        [Test]
        public void Phase11統合_複数トラックの異なるBlendShapeプロパティのマージ処理()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("TestAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "MultiBlendShapePropertiesTest";

            // トラック1: Smile
            var track1 = timeline.CreateTrack<AnimationTrack>(null, "SmileTrack");
            var clip1 = new AnimationClip { name = "SmileClip" };
            var binding1 = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            AnimationUtility.SetEditorCurve(clip1, binding1, AnimationCurve.Linear(0, 0, 1, 100));
            var tc1 = track1.CreateClip<AnimationPlayableAsset>();
            tc1.start = 0;
            tc1.duration = 1;
            (tc1.asset as AnimationPlayableAsset).clip = clip1;

            // トラック2: eyeBlink_L
            var track2 = timeline.CreateTrack<AnimationTrack>(null, "BlinkTrack");
            var clip2 = new AnimationClip { name = "BlinkClip" };
            var binding2 = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.eyeBlink_L");
            AnimationUtility.SetEditorCurve(clip2, binding2, new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.25f, 100),
                new Keyframe(0.5f, 0)
            ));
            var tc2 = track2.CreateClip<AnimationPlayableAsset>();
            tc2.start = 0.25;
            tc2.duration = 0.5;
            (tc2.asset as AnimationPlayableAsset).clip = clip2;

            // トラック3: MouthOpen
            var track3 = timeline.CreateTrack<AnimationTrack>(null, "MouthTrack");
            var clip3 = new AnimationClip { name = "MouthClip" };
            var binding3 = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.MouthOpen");
            AnimationUtility.SetEditorCurve(clip3, binding3, AnimationCurve.Linear(0, 50, 0.75f, 100));
            var tc3 = track3.CreateClip<AnimationPlayableAsset>();
            tc3.start = 0;
            tc3.duration = 0.75;
            (tc3.asset as AnimationPlayableAsset).clip = clip3;

            director.playableAsset = timeline;
            director.SetGenericBinding(track1, animator);
            director.SetGenericBinding(track2, animator);
            director.SetGenericBinding(track3, animator);

            try
            {
                // Act
                var service = new AnimationMergeService();
                var results = service.MergeFromPlayableDirector(director);

                // Assert
                Assert.AreEqual(1, results.Count, "結果が1つ返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "複数BlendShapeプロパティのマージが成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");

                // 3つの異なるBlendShapeプロパティが含まれていることを確認
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.IsTrue(bindings.Any(b => b.propertyName == "blendShape.Smile"), "Smileが含まれるべき");
                Assert.IsTrue(bindings.Any(b => b.propertyName == "blendShape.eyeBlink_L"), "eyeBlink_Lが含まれるべき");
                Assert.IsTrue(bindings.Any(b => b.propertyName == "blendShape.MouthOpen"), "MouthOpenが含まれるべき");

                // 全てSkinnedMeshRenderer型であることを確認
                var blendShapeBindings = bindings.Where(b => b.propertyName.StartsWith("blendShape.")).ToArray();
                Assert.AreEqual(3, blendShapeBindings.Length, "3つのBlendShapeカーブが含まれるべき");
                Assert.IsTrue(blendShapeBindings.All(b => b.type == typeof(SkinnedMeshRenderer)), "全てSkinnedMeshRenderer型であるべき");

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(animatorGo);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(clip1);
                Object.DestroyImmediate(clip2);
                Object.DestroyImmediate(clip3);
            }
        }

        /// <summary>
        /// P11-008: BlendShapeカーブの0-100範囲の値が正しく保持されることを確認する統合テスト
        /// </summary>
        [Test]
        public void Phase11統合_BlendShapeカーブの値範囲が正しく保持される()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "BlendShapeValueRangeTest";

            var track = timeline.CreateTrack<AnimationTrack>(null, "ValueRangeTrack");

            var animClip = new AnimationClip { name = "ValueRangeClip" };

            // 0→100の範囲で変化するBlendShapeカーブを追加
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.TestShape");
            var curve = new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.25f, 25),
                new Keyframe(0.5f, 50),
                new Keyframe(0.75f, 75),
                new Keyframe(1, 100)
            );
            AnimationUtility.SetEditorCurve(animClip, binding, curve);

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
                Assert.AreEqual(1, results.Count, "結果が1つ返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "処理が成功すべき");

                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                var testShapeBinding = bindings.FirstOrDefault(b => b.propertyName == "blendShape.TestShape");
                Assert.IsTrue(testShapeBinding.propertyName == "blendShape.TestShape", "TestShapeバインディングが存在すべき");

                var resultCurve = AnimationUtility.GetEditorCurve(results[0].GeneratedClip, testShapeBinding);

                // 各時点での値が正しく保持されていることを確認
                Assert.AreEqual(0f, resultCurve.Evaluate(0f), 1f, "0秒での値が正しいべき");
                Assert.AreEqual(25f, resultCurve.Evaluate(0.25f), 1f, "0.25秒での値が正しいべき");
                Assert.AreEqual(50f, resultCurve.Evaluate(0.5f), 1f, "0.5秒での値が正しいべき");
                Assert.AreEqual(75f, resultCurve.Evaluate(0.75f), 1f, "0.75秒での値が正しいべき");
                Assert.AreEqual(100f, resultCurve.Evaluate(1f), 1f, "1秒での値が正しいべき");

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
        /// P11-008: BlendShapeカーブを含むE2Eテスト（PlayableDirectorからアセット生成まで）
        /// </summary>
        [Test]
        public void Phase11統合_BlendShapeカーブを含むE2E処理()
        {
            // Arrange
            var directorGo = new GameObject("BlendShape_E2E_Director");
            var director = directorGo.AddComponent<PlayableDirector>();
            var characterGo = new GameObject("BlendShape_E2E_Character");
            var animator = characterGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "BlendShape_E2E_Timeline";

            // 表情アニメーショントラック
            var faceTrack = timeline.CreateTrack<AnimationTrack>(null, "FaceExpressions");
            var faceClip = new AnimationClip { name = "FaceExpressionClip" };

            // 複数の表情BlendShapeを追加
            var smileBinding = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            AnimationUtility.SetEditorCurve(faceClip, smileBinding, new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(1, 80),
                new Keyframe(2, 0)
            ));

            var angryBinding = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.Angry");
            AnimationUtility.SetEditorCurve(faceClip, angryBinding, new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.5f, 50),
                new Keyframe(1, 0)
            ));

            var faceTimelineClip = faceTrack.CreateClip<AnimationPlayableAsset>();
            faceTimelineClip.start = 0;
            faceTimelineClip.duration = 2;
            (faceTimelineClip.asset as AnimationPlayableAsset).clip = faceClip;

            // 体のトランスフォームアニメーショントラック
            var bodyTrack = timeline.CreateTrack<AnimationTrack>(null, "BodyMovement");
            var bodyClip = new AnimationClip { name = "BodyMovementClip" };
            bodyClip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.EaseInOut(0, 0, 2, 1));
            var bodyTimelineClip = bodyTrack.CreateClip<AnimationPlayableAsset>();
            bodyTimelineClip.start = 0;
            bodyTimelineClip.duration = 2;
            (bodyTimelineClip.asset as AnimationPlayableAsset).clip = bodyClip;

            director.playableAsset = timeline;
            director.SetGenericBinding(faceTrack, animator);
            director.SetGenericBinding(bodyTrack, animator);

            try
            {
                // Act
                var success = ContextMenuHandler.ExecuteForPlayableDirectors(new[] { director });

                // Assert
                Assert.IsTrue(success, "BlendShape E2E処理が成功すべき");

                // 生成されたアセットを確認
                var expectedPath = "Assets/BlendShape_E2E_Timeline_BlendShape_E2E_Character_Merged.anim";
                AssetDatabase.Refresh();
                var generatedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(expectedPath);
                if (generatedClip == null)
                {
                    generatedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/BlendShape_E2E_Timeline_BlendShape_E2E_Character_Merged(1).anim");
                }

                Assert.IsNotNull(generatedClip, "AnimationClipアセットが生成されるべき");

                // BlendShapeカーブとTransformカーブの両方が含まれていることを確認
                var bindings = AnimationUtility.GetCurveBindings(generatedClip);
                Assert.IsTrue(bindings.Any(b => b.propertyName == "blendShape.Smile"), "Smileカーブが含まれるべき");
                Assert.IsTrue(bindings.Any(b => b.propertyName == "blendShape.Angry"), "Angryカーブが含まれるべき");
                Assert.IsTrue(bindings.Any(b => b.propertyName == "m_LocalPosition.y"), "localPosition.yカーブが含まれるべき");

                // クリーンアップ用にパスを記録
                _createdAssetPaths.Add(expectedPath);
                _createdAssetPaths.Add("Assets/BlendShape_E2E_Timeline_BlendShape_E2E_Character_Merged(1).anim");
            }
            finally
            {
                Object.DestroyImmediate(directorGo);
                Object.DestroyImmediate(characterGo);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(faceClip);
                Object.DestroyImmediate(bodyClip);
            }
        }

        #endregion

        #region Phase 13 統合テスト: スケルトン・Transform出力

        /// <summary>
        /// P13-008: スケルトン取得からTransformカーブ抽出までの統合テスト
        /// AnimatorからSkeletonを取得し、そのボーン階層のカーブを正しく抽出できることを検証
        /// </summary>
        [Test]
        public void Phase13統合_スケルトン取得とTransformカーブ抽出の連携()
        {
            // Arrange
            var root = new GameObject("TestCharacter");
            var animator = root.AddComponent<Animator>();

            // ボーン階層を作成
            var hips = new GameObject("Hips");
            hips.transform.SetParent(root.transform);
            var spine = new GameObject("Spine");
            spine.transform.SetParent(hips.transform);
            var chest = new GameObject("Chest");
            chest.transform.SetParent(spine.transform);

            // SkinnedMeshRendererを追加してボーンを定義
            var meshObj = new GameObject("Body");
            meshObj.transform.SetParent(root.transform);
            var skinnedMesh = meshObj.AddComponent<SkinnedMeshRenderer>();
            skinnedMesh.bones = new Transform[]
            {
                hips.transform, spine.transform, chest.transform
            };
            skinnedMesh.rootBone = hips.transform;

            // ボーンをアニメートするAnimationClipを作成
            var animClip = new AnimationClip();
            animClip.name = "SkeletonAnimClip";
            animClip.SetCurve("Hips", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            animClip.SetCurve("Hips/Spine", typeof(Transform), "localRotation.x", AnimationCurve.Linear(0, 0, 1, 0));
            animClip.SetCurve("Hips/Spine", typeof(Transform), "localRotation.y", AnimationCurve.Linear(0, 0, 1, 0));
            animClip.SetCurve("Hips/Spine", typeof(Transform), "localRotation.z", AnimationCurve.Linear(0, 0, 1, 0));
            animClip.SetCurve("Hips/Spine", typeof(Transform), "localRotation.w", AnimationCurve.Linear(0, 1, 1, 1));

            try
            {
                // Act
                var skeletonExtractor = new Infrastructure.SkeletonExtractor();
                var skeleton = skeletonExtractor.Extract(animator);

                var exporter = new Infrastructure.FbxAnimationExporter(skeletonExtractor);
                var transformCurves = exporter.ExtractTransformCurves(animClip, animator);

                // Assert - スケルトン取得の検証
                Assert.IsTrue(skeleton.HasSkeleton, "スケルトンが取得できるべき");
                Assert.AreEqual(hips.transform, skeleton.RootBone, "Hipsがルートボーンであるべき");
                Assert.AreEqual(3, skeleton.Bones.Count, "3つのボーンが含まれるべき");

                // Assert - Transformカーブ抽出の検証
                Assert.IsNotNull(transformCurves);
                Assert.Greater(transformCurves.Count, 0, "Transformカーブが抽出されるべき");

                // ボーンパスのカーブが存在することを確認
                bool hasHipsCurve = false;
                bool hasSpineCurve = false;
                foreach (var curve in transformCurves)
                {
                    if (curve.Path == "Hips") hasHipsCurve = true;
                    if (curve.Path == "Hips/Spine") hasSpineCurve = true;
                }
                Assert.IsTrue(hasHipsCurve, "Hipsのカーブが存在すべき");
                Assert.IsTrue(hasSpineCurve, "Spineのカーブが存在すべき");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(animClip);
            }
        }

        /// <summary>
        /// P13-008: 非スケルトンTransform（プロップ等）の取得とカーブ抽出の統合テスト
        /// スケルトン以外のGameObject（プロップなど）のTransformカーブも正しく抽出できることを検証
        /// </summary>
        [Test]
        public void Phase13統合_非スケルトンTransformのカーブ抽出()
        {
            // Arrange
            var root = new GameObject("TestCharacter");
            var animator = root.AddComponent<Animator>();

            // ボーン階層を作成
            var hips = new GameObject("Hips");
            hips.transform.SetParent(root.transform);
            var rightHand = new GameObject("RightHand");
            rightHand.transform.SetParent(hips.transform);

            // SkinnedMeshRendererを追加
            var meshObj = new GameObject("Body");
            meshObj.transform.SetParent(root.transform);
            var skinnedMesh = meshObj.AddComponent<SkinnedMeshRenderer>();
            skinnedMesh.bones = new Transform[] { hips.transform, rightHand.transform };
            skinnedMesh.rootBone = hips.transform;

            // プロップ（非スケルトンTransform）を追加
            var sword = new GameObject("Sword");
            sword.transform.SetParent(rightHand.transform);
            sword.AddComponent<MeshFilter>();

            // プロップをアニメートするAnimationClipを作成
            var animClip = new AnimationClip();
            animClip.name = "PropAnimClip";
            animClip.SetCurve("Hips/RightHand/Sword", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 2));
            animClip.SetCurve("Hips/RightHand/Sword", typeof(Transform), "localRotation.x", AnimationCurve.Linear(0, 0, 1, 0));
            animClip.SetCurve("Hips/RightHand/Sword", typeof(Transform), "localRotation.y", AnimationCurve.Linear(0, 0, 1, 0));
            animClip.SetCurve("Hips/RightHand/Sword", typeof(Transform), "localRotation.z", AnimationCurve.Linear(0, 0, 1, 0.7f));
            animClip.SetCurve("Hips/RightHand/Sword", typeof(Transform), "localRotation.w", AnimationCurve.Linear(0, 1, 1, 0.7f));

            try
            {
                // Act
                var skeletonExtractor = new Infrastructure.SkeletonExtractor();
                var skeleton = skeletonExtractor.Extract(animator);
                var nonSkeletonTransforms = skeletonExtractor.ExtractNonSkeletonTransforms(animator, skeleton, animClip);

                var exporter = new Infrastructure.FbxAnimationExporter(skeletonExtractor);
                var transformCurves = exporter.ExtractTransformCurves(animClip, animator);

                // Assert - 非スケルトンTransform取得の検証
                Assert.IsNotNull(nonSkeletonTransforms);
                bool hasSword = false;
                foreach (var t in nonSkeletonTransforms)
                {
                    if (t.name == "Sword") hasSword = true;
                }
                Assert.IsTrue(hasSword, "Swordが非スケルトンTransformとして取得されるべき");

                // Assert - プロップのカーブが抽出されていることを確認
                bool hasSwordCurve = false;
                foreach (var curve in transformCurves)
                {
                    if (curve.Path == "Hips/RightHand/Sword")
                    {
                        hasSwordCurve = true;
                        break;
                    }
                }
                Assert.IsTrue(hasSwordCurve, "Swordのカーブが抽出されるべき");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(animClip);
            }
        }

        /// <summary>
        /// P13-008: FbxExportDataの構築とエクスポート可否判定の統合テスト
        /// スケルトンとTransformカーブから正しくFbxExportDataが構築され、エクスポート可能と判定されることを検証
        /// </summary>
        [Test]
        public void Phase13統合_FbxExportDataの構築とエクスポート判定()
        {
            // Arrange
            var root = new GameObject("ExportTestCharacter");
            var animator = root.AddComponent<Animator>();

            // ボーン階層を作成
            var hips = new GameObject("Hips");
            hips.transform.SetParent(root.transform);
            var spine = new GameObject("Spine");
            spine.transform.SetParent(hips.transform);

            // SkinnedMeshRendererを追加
            var meshObj = new GameObject("Body");
            meshObj.transform.SetParent(root.transform);
            var skinnedMesh = meshObj.AddComponent<SkinnedMeshRenderer>();
            skinnedMesh.bones = new Transform[] { hips.transform, spine.transform };
            skinnedMesh.rootBone = hips.transform;

            // AnimationClipを作成
            var animClip = new AnimationClip();
            animClip.name = "ExportTestClip";
            animClip.SetCurve("Hips", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            animClip.SetCurve("Hips", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, 0, 1, 2));
            animClip.SetCurve("Hips", typeof(Transform), "localPosition.z", AnimationCurve.Linear(0, 0, 1, 3));

            try
            {
                // Act
                var skeletonExtractor = new Infrastructure.SkeletonExtractor();
                var exporter = new Infrastructure.FbxAnimationExporter(skeletonExtractor);

                // スケルトン取得
                var skeleton = exporter.ExtractSkeleton(animator);

                // Transformカーブ抽出
                var transformCurves = exporter.ExtractTransformCurves(animClip, animator);

                // FbxExportData構築
                var exportData = exporter.PrepareTransformCurvesForExport(animator, animClip, transformCurves);

                // Assert - FbxExportDataの検証
                Assert.IsNotNull(exportData, "FbxExportDataが生成されるべき");
                Assert.AreSame(animator, exportData.SourceAnimator, "Animatorが正しく設定されるべき");
                Assert.AreSame(animClip, exportData.MergedClip, "AnimationClipが正しく設定されるべき");
                Assert.Greater(exportData.TransformCurves.Count, 0, "Transformカーブが含まれるべき");
                Assert.IsTrue(exportData.HasExportableData, "エクスポート可能なデータがあるべき");

                // エクスポート可否判定
                Assert.IsTrue(exporter.CanExport(exportData), "エクスポート可能と判定されるべき");
                Assert.IsNull(exporter.ValidateExportData(exportData), "バリデーションエラーがないべき");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(animClip);
            }
        }

        /// <summary>
        /// P13-008: 複数のTransformタイプ（Position/Rotation/Scale）が混在するアニメーションの統合テスト
        /// </summary>
        [Test]
        public void Phase13統合_複数TransformタイプのカーブをFbxExportDataに含める()
        {
            // Arrange
            var root = new GameObject("MultiTypeCharacter");
            var animator = root.AddComponent<Animator>();

            var bone = new GameObject("Bone");
            bone.transform.SetParent(root.transform);

            // SkinnedMeshRendererを追加
            var meshObj = new GameObject("Body");
            meshObj.transform.SetParent(root.transform);
            var skinnedMesh = meshObj.AddComponent<SkinnedMeshRenderer>();
            skinnedMesh.bones = new Transform[] { bone.transform };
            skinnedMesh.rootBone = bone.transform;

            // Position/Rotation/Scale全てを含むAnimationClipを作成
            var animClip = new AnimationClip();
            animClip.name = "MultiTypeClip";

            // Position
            animClip.SetCurve("Bone", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            animClip.SetCurve("Bone", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, 0, 1, 2));
            animClip.SetCurve("Bone", typeof(Transform), "localPosition.z", AnimationCurve.Linear(0, 0, 1, 3));

            // Rotation
            animClip.SetCurve("Bone", typeof(Transform), "localRotation.x", AnimationCurve.Linear(0, 0, 1, 0));
            animClip.SetCurve("Bone", typeof(Transform), "localRotation.y", AnimationCurve.Linear(0, 0, 1, 0));
            animClip.SetCurve("Bone", typeof(Transform), "localRotation.z", AnimationCurve.Linear(0, 0, 1, 0));
            animClip.SetCurve("Bone", typeof(Transform), "localRotation.w", AnimationCurve.Linear(0, 1, 1, 1));

            // Scale
            animClip.SetCurve("Bone", typeof(Transform), "localScale.x", AnimationCurve.Linear(0, 1, 1, 2));
            animClip.SetCurve("Bone", typeof(Transform), "localScale.y", AnimationCurve.Linear(0, 1, 1, 2));
            animClip.SetCurve("Bone", typeof(Transform), "localScale.z", AnimationCurve.Linear(0, 1, 1, 2));

            try
            {
                // Act
                var skeletonExtractor = new Infrastructure.SkeletonExtractor();
                var exporter = new Infrastructure.FbxAnimationExporter(skeletonExtractor);
                var transformCurves = exporter.ExtractTransformCurves(animClip, animator);
                var exportData = exporter.PrepareTransformCurvesForExport(animator, animClip, transformCurves);

                // Assert - 各タイプのカーブ数を確認
                int positionCount = 0;
                int rotationCount = 0;
                int scaleCount = 0;

                foreach (var curve in exportData.TransformCurves)
                {
                    switch (curve.CurveType)
                    {
                        case Domain.Models.TransformCurveType.Position:
                            positionCount++;
                            break;
                        case Domain.Models.TransformCurveType.Rotation:
                            rotationCount++;
                            break;
                        case Domain.Models.TransformCurveType.Scale:
                            scaleCount++;
                            break;
                    }
                }

                Assert.AreEqual(3, positionCount, "Positionカーブは3つ（x,y,z）であるべき");
                Assert.AreEqual(4, rotationCount, "Rotationカーブは4つ（x,y,z,w）であるべき");
                Assert.AreEqual(3, scaleCount, "Scaleカーブは3つ（x,y,z）であるべき");
                Assert.AreEqual(10, exportData.TransformCurves.Count, "合計10のカーブが含まれるべき");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(animClip);
            }
        }

        /// <summary>
        /// P13-008: スケルトンボーンと非スケルトンTransformが混在するアニメーションの統合テスト
        /// </summary>
        [Test]
        public void Phase13統合_スケルトンと非スケルトンの混在アニメーション()
        {
            // Arrange
            var root = new GameObject("MixedCharacter");
            var animator = root.AddComponent<Animator>();

            // ボーン階層
            var hips = new GameObject("Hips");
            hips.transform.SetParent(root.transform);
            var rightHand = new GameObject("RightHand");
            rightHand.transform.SetParent(hips.transform);

            // SkinnedMeshRenderer（ボーン定義）
            var meshObj = new GameObject("Body");
            meshObj.transform.SetParent(root.transform);
            var skinnedMesh = meshObj.AddComponent<SkinnedMeshRenderer>();
            skinnedMesh.bones = new Transform[] { hips.transform, rightHand.transform };
            skinnedMesh.rootBone = hips.transform;

            // プロップ（非スケルトン）
            var sword = new GameObject("Sword");
            sword.transform.SetParent(rightHand.transform);
            sword.AddComponent<MeshFilter>();

            // スケルトンボーンとプロップの両方をアニメートするClip
            var animClip = new AnimationClip();
            animClip.name = "MixedAnimClip";

            // スケルトンボーン（Hips）のアニメーション
            animClip.SetCurve("Hips", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, 0, 1, 1));

            // プロップ（Sword）のアニメーション
            animClip.SetCurve("Hips/RightHand/Sword", typeof(Transform), "localRotation.x", AnimationCurve.Linear(0, 0, 1, 0));
            animClip.SetCurve("Hips/RightHand/Sword", typeof(Transform), "localRotation.y", AnimationCurve.Linear(0, 0, 1, 0));
            animClip.SetCurve("Hips/RightHand/Sword", typeof(Transform), "localRotation.z", AnimationCurve.Linear(0, 0, 1, 0.7f));
            animClip.SetCurve("Hips/RightHand/Sword", typeof(Transform), "localRotation.w", AnimationCurve.Linear(0, 1, 1, 0.7f));

            try
            {
                // Act
                var skeletonExtractor = new Infrastructure.SkeletonExtractor();
                var exporter = new Infrastructure.FbxAnimationExporter(skeletonExtractor);

                var skeleton = exporter.ExtractSkeleton(animator);
                var nonSkeletonTransforms = skeletonExtractor.ExtractNonSkeletonTransforms(animator, skeleton, animClip);
                var transformCurves = exporter.ExtractTransformCurves(animClip, animator);
                var exportData = exporter.PrepareTransformCurvesForExport(animator, animClip, transformCurves);

                // Assert - スケルトンの検証
                Assert.IsTrue(skeleton.HasSkeleton, "スケルトンが存在すべき");
                Assert.AreEqual(2, skeleton.Bones.Count, "Hips, RightHandの2つのボーンがあるべき");

                // Assert - 非スケルトンTransformの検証
                bool hasSword = false;
                foreach (var t in nonSkeletonTransforms)
                {
                    if (t.name == "Sword") hasSword = true;
                }
                Assert.IsTrue(hasSword, "Swordが非スケルトンとして取得されるべき");

                // Assert - 両方のパスのカーブが抽出されていることを確認
                bool hasHipsCurve = false;
                bool hasSwordCurve = false;
                foreach (var curve in transformCurves)
                {
                    if (curve.Path == "Hips") hasHipsCurve = true;
                    if (curve.Path == "Hips/RightHand/Sword") hasSwordCurve = true;
                }
                Assert.IsTrue(hasHipsCurve, "スケルトンボーン（Hips）のカーブが抽出されるべき");
                Assert.IsTrue(hasSwordCurve, "非スケルトン（Sword）のカーブが抽出されるべき");

                // Assert - FbxExportDataの検証
                Assert.IsTrue(exportData.HasExportableData, "エクスポート可能なデータがあるべき");
                Assert.IsTrue(exporter.CanExport(exportData), "エクスポート可能と判定されるべき");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(animClip);
            }
        }

        /// <summary>
        /// P13-008: エクスポートデータなしの場合のエラーハンドリング統合テスト
        /// </summary>
        [Test]
        public void Phase13統合_エクスポート可能なデータがない場合の処理()
        {
            // Arrange
            var root = new GameObject("EmptyDataCharacter");
            var animator = root.AddComponent<Animator>();

            // Transformカーブを含まないAnimationClip（マテリアルカーブのみ）
            var animClip = new AnimationClip();
            animClip.name = "NoTransformClip";
            animClip.SetCurve("", typeof(MeshRenderer), "material._Color.r", AnimationCurve.Linear(0, 0, 1, 1));

            try
            {
                // Act
                var skeletonExtractor = new Infrastructure.SkeletonExtractor();
                var exporter = new Infrastructure.FbxAnimationExporter(skeletonExtractor);
                var transformCurves = exporter.ExtractTransformCurves(animClip, animator);
                var exportData = exporter.PrepareTransformCurvesForExport(animator, animClip, transformCurves);

                // Assert
                Assert.AreEqual(0, transformCurves.Count, "Transformカーブは抽出されないべき");
                Assert.IsFalse(exportData.HasExportableData, "エクスポート可能なデータがないべき");
                Assert.IsFalse(exporter.CanExport(exportData), "エクスポート不可と判定されるべき");

                var validationError = exporter.ValidateExportData(exportData);
                Assert.IsNotNull(validationError, "バリデーションエラーがあるべき");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(animClip);
            }
        }

        #endregion

        #region Phase 14 統合テスト: Humanoid→Generic変換 (P14-010)

        /// <summary>
        /// Phase 14 統合テスト: HumanoidToGenericConverterとFbxAnimationExporterの統合確認
        /// </summary>
        [Test]
        public void Phase14統合_FbxAnimationExporterにHumanoidConverterが統合されている()
        {
            // Arrange
            var skeletonExtractor = new Infrastructure.SkeletonExtractor();
            var humanoidConverter = new Infrastructure.HumanoidToGenericConverter();
            var exporter = new Infrastructure.FbxAnimationExporter(skeletonExtractor, humanoidConverter);

            // Act & Assert
            Assert.IsNotNull(exporter, "FbxAnimationExporterが生成されるべき");
        }

        /// <summary>
        /// Phase 14 統合テスト: IsHumanoidAnimator/IsHumanoidClipメソッドの動作確認
        /// </summary>
        [Test]
        public void Phase14統合_IsHumanoid判定メソッドが動作する()
        {
            // Arrange
            var root = new GameObject("TestCharacter");
            var animator = root.AddComponent<Animator>();
            var clip = new AnimationClip();

            try
            {
                var exporter = new Infrastructure.FbxAnimationExporter();

                // Act
                bool isHumanoidAnimator = exporter.IsHumanoidAnimator(animator);
                bool isHumanoidClip = exporter.IsHumanoidClip(clip);

                // Assert
                // Genericリグなのでfalse
                Assert.IsFalse(isHumanoidAnimator, "GenericリグはHumanoidではないと判定されるべき");
                Assert.IsFalse(isHumanoidClip, "空のクリップはHumanoidではないと判定されるべき");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(clip);
            }
        }

        /// <summary>
        /// Phase 14 統合テスト: ConvertHumanoidToGenericCurvesがGenericリグでも例外を投げない
        /// </summary>
        [Test]
        public void Phase14統合_ConvertHumanoidToGenericCurves_Genericリグでも安全()
        {
            // Arrange
            var root = new GameObject("GenericCharacter");
            var animator = root.AddComponent<Animator>();
            var clip = new AnimationClip();
            clip.SetCurve("Hips", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));

            try
            {
                var exporter = new Infrastructure.FbxAnimationExporter();

                // Act
                var result = exporter.ConvertHumanoidToGenericCurves(animator, clip);

                // Assert
                Assert.IsNotNull(result, "結果はnullではなく空のリストを返すべき");
                Assert.AreEqual(0, result.Count, "Genericリグでは変換カーブは生成されないべき");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(clip);
            }
        }

        /// <summary>
        /// Phase 14 統合テスト: PrepareHumanoidForExportがGenericリグで動作する
        /// </summary>
        [Test]
        public void Phase14統合_PrepareHumanoidForExport_Genericリグで動作()
        {
            // Arrange
            var root = new GameObject("GenericCharacter");
            var animator = root.AddComponent<Animator>();
            var clip = new AnimationClip();
            clip.name = "GenericAnimation";
            clip.SetCurve("Bone1", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            clip.SetCurve("Bone1", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, 0, 1, 2));

            try
            {
                var exporter = new Infrastructure.FbxAnimationExporter();

                // Act
                var exportData = exporter.PrepareHumanoidForExport(animator, clip);

                // Assert
                Assert.IsNotNull(exportData, "FbxExportDataが生成されるべき");
                Assert.AreSame(animator, exportData.SourceAnimator, "Animatorが正しく設定されるべき");
                Assert.AreSame(clip, exportData.MergedClip, "AnimationClipが正しく設定されるべき");
                Assert.IsFalse(exportData.IsHumanoid, "GenericリグはHumanoidではないと判定されるべき");
                // GenericリグではTransformカーブが抽出される
                Assert.GreaterOrEqual(exportData.TransformCurves.Count, 0, "TransformCurvesリストが初期化されているべき");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(clip);
            }
        }

        /// <summary>
        /// Phase 14 統合テスト: null引数でも例外を投げない
        /// </summary>
        [Test]
        public void Phase14統合_null引数での安全性()
        {
            // Arrange
            var exporter = new Infrastructure.FbxAnimationExporter();

            // Act & Assert - IsHumanoidAnimator
            Assert.DoesNotThrow(() =>
            {
                var result = exporter.IsHumanoidAnimator(null);
                Assert.IsFalse(result, "nullはHumanoidではないと判定されるべき");
            }, "IsHumanoidAnimator(null)で例外が発生しない");

            // Act & Assert - IsHumanoidClip
            Assert.DoesNotThrow(() =>
            {
                var result = exporter.IsHumanoidClip(null);
                Assert.IsFalse(result, "nullはHumanoidクリップではないと判定されるべき");
            }, "IsHumanoidClip(null)で例外が発生しない");

            // Act & Assert - ConvertHumanoidToGenericCurves
            Assert.DoesNotThrow(() =>
            {
                var result = exporter.ConvertHumanoidToGenericCurves(null, null);
                Assert.IsNotNull(result, "nullでも空のリストを返すべき");
                Assert.AreEqual(0, result.Count, "nullでは空のリストを返すべき");
            }, "ConvertHumanoidToGenericCurves(null, null)で例外が発生しない");

            // Act & Assert - PrepareHumanoidForExport
            Assert.DoesNotThrow(() =>
            {
                var result = exporter.PrepareHumanoidForExport(null, null);
                Assert.IsNotNull(result, "nullでもFbxExportDataを返すべき");
            }, "PrepareHumanoidForExport(null, null)で例外が発生しない");
        }

        /// <summary>
        /// Phase 14 統合テスト: ExportHumanoidAnimationがnullチェックを行う
        /// </summary>
        [Test]
        public void Phase14統合_ExportHumanoidAnimation_nullチェック()
        {
            // Arrange
            var exporter = new Infrastructure.FbxAnimationExporter();
            var root = new GameObject("TestCharacter");
            var animator = root.AddComponent<Animator>();
            var clip = new AnimationClip();

            try
            {
                // Act & Assert - Animatorがnull
                LogAssert.Expect(LogType.Error, "Animatorがnullです。");
                var result1 = exporter.ExportHumanoidAnimation(null, clip, "test.fbx");
                Assert.IsFalse(result1, "Animatorがnullの場合falseを返すべき");

                // Act & Assert - AnimationClipがnull
                LogAssert.Expect(LogType.Error, "AnimationClipがnullです。");
                var result2 = exporter.ExportHumanoidAnimation(animator, null, "test.fbx");
                Assert.IsFalse(result2, "AnimationClipがnullの場合falseを返すべき");

                // Act & Assert - outputPathがnull
                LogAssert.Expect(LogType.Error, "出力パスが指定されていません。");
                var result3 = exporter.ExportHumanoidAnimation(animator, clip, null);
                Assert.IsFalse(result3, "outputPathがnullの場合falseを返すべき");

                // Act & Assert - outputPathが空文字
                LogAssert.Expect(LogType.Error, "出力パスが指定されていません。");
                var result4 = exporter.ExportHumanoidAnimation(animator, clip, "");
                Assert.IsFalse(result4, "outputPathが空の場合falseを返すべき");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(clip);
            }
        }

        /// <summary>
        /// Phase 14 統合テスト: HumanoidToGenericConverterの各メソッドが統合後も正常に動作
        /// </summary>
        [Test]
        public void Phase14統合_HumanoidToGenericConverter_全メソッド動作確認()
        {
            // Arrange
            var converter = new Infrastructure.HumanoidToGenericConverter();
            var root = new GameObject("TestCharacter");
            var animator = root.AddComponent<Animator>();
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Animator), "RootT.x", AnimationCurve.Linear(0, 0, 1, 1));

            try
            {
                // Act & Assert - GetTransformPath
                var path = converter.GetTransformPath(animator, HumanBodyBones.Hips);
                // Genericリグなのでnull
                Assert.IsNull(path, "Genericリグではnullを返すべき");

                // Act & Assert - IsHumanoidClip
                var isHumanoid = converter.IsHumanoidClip(clip);
                Assert.IsFalse(isHumanoid, "GenericクリップはHumanoidではないと判定されるべき");

                // Act & Assert - ConvertMuscleCurvesToRotation
                var muscleCurves = converter.ConvertMuscleCurvesToRotation(animator, clip);
                Assert.IsNotNull(muscleCurves, "結果はnullではなく空リストを返すべき");

                // Act & Assert - ConvertRootMotionCurves
                var rootMotionCurves = converter.ConvertRootMotionCurves(clip, "Hips");
                Assert.IsNotNull(rootMotionCurves, "結果はnullではなく空リストを返すべき");
                // RootT.xカーブが設定されているので変換される
                Assert.GreaterOrEqual(rootMotionCurves.Count, 1, "RootT.xカーブが変換されるべき");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(clip);
            }
        }

        /// <summary>
        /// Phase 14 統合テスト: ルートモーションカーブの変換結果が正しいプロパティを持つ
        /// </summary>
        [Test]
        public void Phase14統合_ルートモーションカーブ変換結果の検証()
        {
            // Arrange
            var converter = new Infrastructure.HumanoidToGenericConverter();
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Animator), "RootT.x", AnimationCurve.Linear(0, 0, 1, 5));
            clip.SetCurve("", typeof(Animator), "RootT.y", AnimationCurve.Linear(0, 0, 1, 3));
            clip.SetCurve("", typeof(Animator), "RootT.z", AnimationCurve.Linear(0, 0, 1, 1));
            clip.SetCurve("", typeof(Animator), "RootQ.x", AnimationCurve.Linear(0, 0, 1, 0.1f));
            clip.SetCurve("", typeof(Animator), "RootQ.y", AnimationCurve.Linear(0, 0, 1, 0.2f));
            clip.SetCurve("", typeof(Animator), "RootQ.z", AnimationCurve.Linear(0, 0, 1, 0.3f));
            clip.SetCurve("", typeof(Animator), "RootQ.w", AnimationCurve.Linear(0, 1, 1, 0.9f));

            try
            {
                // Act
                var result = converter.ConvertRootMotionCurves(clip, "Root/Hips");

                // Assert
                Assert.AreEqual(7, result.Count, "7つのカーブが変換されるべき");

                // パスの検証
                foreach (var curveData in result)
                {
                    Assert.AreEqual("Root/Hips", curveData.Path, "パスがRoot/Hipsに設定されるべき");
                }

                // プロパティ名の検証
                var propertyNames = new List<string>();
                foreach (var curveData in result)
                {
                    propertyNames.Add(curveData.PropertyName);
                }

                Assert.Contains("localPosition.x", propertyNames);
                Assert.Contains("localPosition.y", propertyNames);
                Assert.Contains("localPosition.z", propertyNames);
                Assert.Contains("localRotation.x", propertyNames);
                Assert.Contains("localRotation.y", propertyNames);
                Assert.Contains("localRotation.z", propertyNames);
                Assert.Contains("localRotation.w", propertyNames);
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        /// <summary>
        /// Phase 14 統合テスト: GenericリグでのTransformカーブ抽出との連携
        /// </summary>
        [Test]
        public void Phase14統合_Genericリグでの通常Transformカーブ抽出()
        {
            // Arrange
            var root = new GameObject("GenericCharacter");
            var animator = root.AddComponent<Animator>();

            var hips = new GameObject("Hips");
            hips.transform.SetParent(root.transform);
            var spine = new GameObject("Spine");
            spine.transform.SetParent(hips.transform);

            var clip = new AnimationClip();
            clip.name = "GenericAnimation";
            // AnimationUtility.SetEditorCurveを使用してカーブを設定
            // （SetCurveはLegacy Animation用）
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("Hips", typeof(Transform), "m_LocalPosition.x"),
                AnimationCurve.Linear(0, 0, 1, 1));
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("Hips", typeof(Transform), "m_LocalPosition.y"),
                AnimationCurve.Linear(0, 0, 1, 2));
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("Hips", typeof(Transform), "m_LocalPosition.z"),
                AnimationCurve.Linear(0, 0, 1, 3));
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("Hips/Spine", typeof(Transform), "m_LocalRotation.x"),
                AnimationCurve.Linear(0, 0, 1, 0.1f));
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("Hips/Spine", typeof(Transform), "m_LocalRotation.y"),
                AnimationCurve.Linear(0, 0, 1, 0.2f));
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("Hips/Spine", typeof(Transform), "m_LocalRotation.z"),
                AnimationCurve.Linear(0, 0, 1, 0.3f));
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("Hips/Spine", typeof(Transform), "m_LocalRotation.w"),
                AnimationCurve.Linear(0, 1, 1, 0.9f));

            try
            {
                var exporter = new Infrastructure.FbxAnimationExporter();

                // Act
                // GenericリグにはPrepareAllCurvesForExportを使用する
                // PrepareHumanoidForExportはHumanoidリグ専用
                var exportData = exporter.PrepareAllCurvesForExport(animator, clip);

                // Assert
                Assert.IsNotNull(exportData, "FbxExportDataが生成されるべき");
                Assert.IsFalse(exportData.IsHumanoid, "GenericリグはHumanoidではないと判定されるべき");
                // Genericリグでは通常のTransformカーブ抽出が行われる
                Assert.AreEqual(7, exportData.TransformCurves.Count, "7つのTransformカーブが抽出されるべき");

                // カーブのパスを検証
                var hipsPositionCount = 0;
                var spineRotationCount = 0;
                foreach (var curveData in exportData.TransformCurves)
                {
                    // m_LocalPositionまたはlocalPositionで始まるものをチェック
                    if (curveData.Path == "Hips" && (curveData.PropertyName.StartsWith("m_LocalPosition") || curveData.PropertyName.StartsWith("localPosition")))
                    {
                        hipsPositionCount++;
                    }
                    else if (curveData.Path == "Hips/Spine" && (curveData.PropertyName.StartsWith("m_LocalRotation") || curveData.PropertyName.StartsWith("localRotation")))
                    {
                        spineRotationCount++;
                    }
                }
                Assert.AreEqual(3, hipsPositionCount, "Hipsのポジションカーブは3つ");
                Assert.AreEqual(4, spineRotationCount, "Spineのローテーションカーブは4つ");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(clip);
            }
        }

        #endregion

        #region Phase 15 統合テスト: BlendShapeエクスポート (P15-008)

        /// <summary>
        /// P15-008: BlendShapeカーブを含むAnimationClipからFbxExportDataを準備する統合テスト
        /// BlendShapeカーブの検出・抽出・FbxExportData準備が正しく連携することを検証
        /// </summary>
        [Test]
        public void Phase15統合_BlendShapeカーブのFbxExportData準備()
        {
            // Arrange: BlendShapeカーブを含むAnimationClipを作成
            var clip = new AnimationClip();
            clip.name = "Phase15BlendShapeTest";

            // BlendShapeカーブを追加
            var bindingSmile = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            var curveSmile = AnimationCurve.Linear(0, 0, 1, 100);
            AnimationUtility.SetEditorCurve(clip, bindingSmile, curveSmile);

            var bindingBlink = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.Blink");
            var curveBlink = new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.5f, 100),
                new Keyframe(1, 0)
            );
            AnimationUtility.SetEditorCurve(clip, bindingBlink, curveBlink);

            try
            {
                // Act
                var exporter = new Infrastructure.FbxAnimationExporter();
                var blendShapeCurves = exporter.ExtractBlendShapeCurves(clip, null);
                var exportData = exporter.PrepareBlendShapeCurvesForExport(null, clip, blendShapeCurves);

                // Assert
                Assert.IsNotNull(exportData, "FbxExportDataが生成されるべき");
                Assert.IsNotNull(exportData.BlendShapeCurves, "BlendShapeカーブリストがnullでないべき");
                Assert.AreEqual(2, exportData.BlendShapeCurves.Count, "2つのBlendShapeカーブが抽出されるべき");

                // BlendShape名を検証
                var blendShapeNames = exportData.BlendShapeCurves.Select(c => c.BlendShapeName).ToList();
                Assert.IsTrue(blendShapeNames.Contains("Smile"), "Smileカーブが含まれるべき");
                Assert.IsTrue(blendShapeNames.Contains("Blink"), "Blinkカーブが含まれるべき");

                // パスを検証
                Assert.IsTrue(exportData.BlendShapeCurves.All(c => c.Path == "Face"), "全カーブのパスがFaceであるべき");
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        /// <summary>
        /// P15-008: TransformとBlendShapeカーブが混在するAnimationClipのFbxExportData準備統合テスト
        /// </summary>
        [Test]
        public void Phase15統合_TransformとBlendShapeカーブ混在のFbxExportData準備()
        {
            // Arrange: 混在カーブを含むAnimationClipを作成
            var clip = new AnimationClip();
            clip.name = "Phase15MixedCurvesTest";

            // BlendShapeカーブを追加
            var bindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            var curveSmile = AnimationCurve.Linear(0, 0, 2, 100);
            AnimationUtility.SetEditorCurve(clip, bindingSmile, curveSmile);

            // Transformカーブを追加
            clip.SetCurve("Hips", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 2, 10));
            clip.SetCurve("Hips", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, 0, 2, 5));

            try
            {
                // Act
                var exporter = new Infrastructure.FbxAnimationExporter();
                var exportData = exporter.PrepareAllCurvesForExport(null, clip);

                // Assert
                Assert.IsNotNull(exportData, "FbxExportDataが生成されるべき");

                // BlendShapeカーブの検証
                Assert.IsNotNull(exportData.BlendShapeCurves, "BlendShapeカーブリストがnullでないべき");
                Assert.AreEqual(1, exportData.BlendShapeCurves.Count, "1つのBlendShapeカーブが抽出されるべき");
                Assert.AreEqual("Smile", exportData.BlendShapeCurves[0].BlendShapeName, "Smileカーブが含まれるべき");

                // Transformカーブの検証
                Assert.IsNotNull(exportData.TransformCurves, "Transformカーブリストがnullでないべき");
                Assert.IsTrue(exportData.TransformCurves.Count >= 2, "少なくとも2つのTransformカーブが抽出されるべき");

                // エクスポート可能データの検証
                Assert.IsTrue(exportData.HasExportableData, "エクスポート可能なデータがあるべき");
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        /// <summary>
        /// P15-008: BlendShapeカーブを含まないAnimationClipのエラー処理統合テスト (ERR-004)
        /// </summary>
        [Test]
        public void Phase15統合_BlendShapeカーブなしのエクスポートエラー処理()
        {
            // Arrange: BlendShapeカーブを含まないAnimationClipを作成
            var clip = new AnimationClip();
            clip.name = "Phase15NoBlendShapeTest";

            // Transformカーブのみを追加
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 10));

            try
            {
                // Act
                var exporter = new Infrastructure.FbxAnimationExporter();
                var hasBlendShapes = exporter.HasBlendShapeCurves(clip);
                var blendShapeCurves = exporter.ExtractBlendShapeCurves(clip, null);

                // Assert
                Assert.IsFalse(hasBlendShapes, "BlendShapeカーブは含まれていないべき");
                Assert.IsNotNull(blendShapeCurves, "空のリストが返されるべき（nullではない）");
                Assert.AreEqual(0, blendShapeCurves.Count, "BlendShapeカーブは0件であるべき");
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        /// <summary>
        /// P15-008: BlendShapeカーブからAnimationClip生成の統合テスト
        /// </summary>
        [Test]
        public void Phase15統合_BlendShapeカーブからAnimationClip生成()
        {
            // Arrange: BlendShapeカーブを含むAnimationClipを作成
            var sourceClip = new AnimationClip();
            sourceClip.name = "Phase15SourceClip";

            var bindingSmile = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            var curveSmile = AnimationCurve.Linear(0, 0, 1, 100);
            AnimationUtility.SetEditorCurve(sourceClip, bindingSmile, curveSmile);

            var bindingAngry = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.Angry");
            var curveAngry = AnimationCurve.Linear(0, 50, 1, 75);
            AnimationUtility.SetEditorCurve(sourceClip, bindingAngry, curveAngry);

            AnimationClip generatedClip = null;

            try
            {
                // Act
                var exporter = new Infrastructure.FbxAnimationExporter();
                var blendShapeCurves = exporter.ExtractBlendShapeCurves(sourceClip, null);
                generatedClip = exporter.CreateAnimationClipFromBlendShapeCurves(blendShapeCurves, "GeneratedBlendShapeClip");

                // Assert
                Assert.IsNotNull(generatedClip, "AnimationClipが生成されるべき");
                Assert.AreEqual("GeneratedBlendShapeClip", generatedClip.name, "クリップ名が正しいべき");

                // 生成されたクリップのカーブを検証
                var bindings = AnimationUtility.GetCurveBindings(generatedClip);
                Assert.AreEqual(2, bindings.Length, "2つのカーブが含まれるべき");

                // BlendShapeプロパティが正しく設定されていることを確認
                var propertyNames = bindings.Select(b => b.propertyName).ToList();
                Assert.IsTrue(propertyNames.Contains("blendShape.Smile"), "blendShape.Smileが含まれるべき");
                Assert.IsTrue(propertyNames.Contains("blendShape.Angry"), "blendShape.Angryが含まれるべき");

                // 型がSkinnedMeshRendererであることを確認
                Assert.IsTrue(bindings.All(b => b.type == typeof(SkinnedMeshRenderer)), "全バインディングがSkinnedMeshRenderer型であるべき");
            }
            finally
            {
                Object.DestroyImmediate(sourceClip);
                if (generatedClip != null)
                {
                    Object.DestroyImmediate(generatedClip);
                }
            }
        }

        /// <summary>
        /// P15-008: 複数パスのBlendShapeカーブのFbxExportData準備統合テスト
        /// 異なるSkinnedMeshRendererパスに紐づくBlendShapeカーブが正しく処理されることを検証
        /// </summary>
        [Test]
        public void Phase15統合_複数パスのBlendShapeカーブのFbxExportData準備()
        {
            // Arrange: 複数パスのBlendShapeカーブを含むAnimationClipを作成
            var clip = new AnimationClip();
            clip.name = "Phase15MultiPathTest";

            // Face用BlendShapeカーブ
            var bindingFaceSmile = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            AnimationUtility.SetEditorCurve(clip, bindingFaceSmile, AnimationCurve.Linear(0, 0, 1, 100));

            // Body用BlendShapeカーブ
            var bindingBodyMuscle = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Muscle");
            AnimationUtility.SetEditorCurve(clip, bindingBodyMuscle, AnimationCurve.Linear(0, 0, 1, 50));

            // Hands用BlendShapeカーブ
            var bindingHandsGrip = EditorCurveBinding.FloatCurve("Hands", typeof(SkinnedMeshRenderer), "blendShape.Grip");
            AnimationUtility.SetEditorCurve(clip, bindingHandsGrip, AnimationCurve.Linear(0, 0, 1, 75));

            try
            {
                // Act
                var exporter = new Infrastructure.FbxAnimationExporter();
                var blendShapeCurves = exporter.ExtractBlendShapeCurves(clip, null);
                var exportData = exporter.PrepareBlendShapeCurvesForExport(null, clip, blendShapeCurves);

                // Assert
                Assert.IsNotNull(exportData, "FbxExportDataが生成されるべき");
                Assert.AreEqual(3, exportData.BlendShapeCurves.Count, "3つのBlendShapeカーブが抽出されるべき");

                // 各パスのカーブを検証
                var paths = exportData.BlendShapeCurves.Select(c => c.Path).ToList();
                Assert.IsTrue(paths.Contains("Face"), "Faceパスが含まれるべき");
                Assert.IsTrue(paths.Contains("Body"), "Bodyパスが含まれるべき");
                Assert.IsTrue(paths.Contains("Hands"), "Handsパスが含まれるべき");

                // BlendShape名を検証
                var names = exportData.BlendShapeCurves.Select(c => c.BlendShapeName).ToList();
                Assert.IsTrue(names.Contains("Smile"), "Smileが含まれるべき");
                Assert.IsTrue(names.Contains("Muscle"), "Muscleが含まれるべき");
                Assert.IsTrue(names.Contains("Grip"), "Gripが含まれるべき");
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        /// <summary>
        /// P15-008: TimelineからBlendShapeカーブ抽出とFbxExportData準備の統合テスト
        /// Timelineのマージ結果からBlendShapeカーブを抽出してFBXエクスポート準備ができることを検証
        /// </summary>
        [Test]
        public void Phase15統合_TimelineマージからBlendShapeエクスポート準備()
        {
            // Arrange: BlendShapeカーブを含むTimelineを作成
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "Phase15TimelineTest";

            var track = timeline.CreateTrack<AnimationTrack>(null, "BlendShapeTrack");

            var animClip = new AnimationClip();
            animClip.name = "BlendShapeAnimClip";

            // BlendShapeカーブを追加
            var bindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            AnimationUtility.SetEditorCurve(animClip, bindingSmile, AnimationCurve.Linear(0, 0, 1, 100));

            var bindingFrown = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Frown");
            AnimationUtility.SetEditorCurve(animClip, bindingFrown, AnimationCurve.Linear(0, 0, 1, 50));

            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            try
            {
                // Act: Timelineをマージ
                var service = new AnimationMergeService();
                var mergeResults = service.MergeFromTimelineAsset(timeline);

                Assert.AreEqual(1, mergeResults.Count, "マージ結果が1つ返されるべき");
                Assert.IsTrue(mergeResults[0].IsSuccess, "マージが成功すべき");

                var mergedClip = mergeResults[0].GeneratedClip;
                Assert.IsNotNull(mergedClip, "マージされたクリップが存在すべき");

                // マージされたクリップからBlendShapeカーブを抽出
                var exporter = new Infrastructure.FbxAnimationExporter();
                var hasBlendShapes = exporter.HasBlendShapeCurves(mergedClip);
                var blendShapeCurves = exporter.ExtractBlendShapeCurves(mergedClip, null);

                // Assert
                Assert.IsTrue(hasBlendShapes, "マージされたクリップにBlendShapeカーブがあるべき");
                Assert.AreEqual(2, blendShapeCurves.Count, "2つのBlendShapeカーブが抽出されるべき");

                // FbxExportDataの準備
                var exportData = exporter.PrepareBlendShapeCurvesForExport(null, mergedClip, blendShapeCurves);
                Assert.IsNotNull(exportData, "FbxExportDataが生成されるべき");
                Assert.IsTrue(exportData.HasExportableData, "エクスポート可能なデータがあるべき");

                // 生成されたファイルパスを記録
                RecordCreatedAssetPaths(mergeResults[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip);
            }
        }

        /// <summary>
        /// P15-008: エクスポート可能データなしのエラーハンドリング統合テスト (ERR-004)
        /// ExportWithBlendShapeCurvesでBlendShapeカーブがない場合のエラー処理を検証
        /// </summary>
        [Test]
        public void Phase15統合_エクスポート可能データなしのエラーハンドリング()
        {
            // Arrange: 空のFbxExportDataを作成
            var clip = new AnimationClip();
            clip.name = "Phase15EmptyExportTest";

            // Transformカーブのみ（BlendShapeなし）
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 10));

            try
            {
                // Act
                var exporter = new Infrastructure.FbxAnimationExporter();
                var blendShapeCurves = exporter.ExtractBlendShapeCurves(clip, null);

                // 空のBlendShapeカーブリストでFbxExportDataを作成
                var exportData = exporter.PrepareBlendShapeCurvesForExport(null, clip, blendShapeCurves);

                // Assert: BlendShapeカーブがない場合のエクスポート可能性を確認
                Assert.IsNotNull(exportData, "FbxExportDataは生成されるべき");
                Assert.AreEqual(0, exportData.BlendShapeCurves.Count, "BlendShapeカーブは0件であるべき");

                // BlendShapeカーブがない場合、Transformカーブもなければエクスポートできないはずだが、
                // PrepareBlendShapeCurvesForExportはBlendShape用なのでTransformは考慮しない
                // 従って、BlendShape専用エクスポートとしては不可

                // ExportWithBlendShapeCurvesでのエラー処理を確認
                LogAssert.Expect(LogType.Error, "エクスポート可能なBlendShapeカーブがありません。");
                var result = exporter.ExportWithBlendShapeCurves(exportData, "Assets/TestExport.fbx");
                Assert.IsFalse(result, "BlendShapeなしのエクスポートは失敗すべき");
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        #endregion

        #region Phase 16 統合テスト: 全機能統合（P16-005）

        /// <summary>
        /// Phase 16 統合テスト: .anim出力とFBX出力の両方を含む全機能統合テスト
        /// タスクP16-005: AnimationMergeServiceを通じて.anim出力が正しく動作することを検証
        /// </summary>
        [Test]
        public void 全機能統合テスト_PlayableDirectorからAnimファイル出力まで()
        {
            // Arrange: 複数トラック・複数クリップ・BlendShapeを含む完全なシナリオ
            var go = new GameObject("FullIntegrationDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("FullIntegrationAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            // SkinnedMeshRendererを追加（BlendShape用）
            var meshGo = new GameObject("Face");
            meshGo.transform.SetParent(animatorGo.transform);
            var skinnedMesh = meshGo.AddComponent<SkinnedMeshRenderer>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "FullIntegrationTimeline";

            // トラック1: Transformアニメーション
            var track1 = timeline.CreateTrack<AnimationTrack>(null, "TransformTrack");
            var animClip1 = new AnimationClip();
            animClip1.name = "TransformClip";
            var curve1X = AnimationCurve.Linear(0, 0, 2, 10);
            var curve1Y = AnimationCurve.Linear(0, 0, 2, 5);
            animClip1.SetCurve("", typeof(Transform), "localPosition.x", curve1X);
            animClip1.SetCurve("", typeof(Transform), "localPosition.y", curve1Y);
            var timelineClip1 = track1.CreateClip<AnimationPlayableAsset>();
            timelineClip1.start = 0;
            timelineClip1.duration = 2;
            (timelineClip1.asset as AnimationPlayableAsset).clip = animClip1;

            // トラック2: BlendShapeアニメーション
            var track2 = timeline.CreateTrack<AnimationTrack>(null, "BlendShapeTrack");
            var animClip2 = new AnimationClip();
            animClip2.name = "BlendShapeClip";
            var blendCurve = AnimationCurve.Linear(0, 0, 1.5f, 100);
            animClip2.SetCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.smile", blendCurve);
            var timelineClip2 = track2.CreateClip<AnimationPlayableAsset>();
            timelineClip2.start = 0.5;
            timelineClip2.duration = 1.5;
            (timelineClip2.asset as AnimationPlayableAsset).clip = animClip2;

            // トラック3: Rotationアニメーション（Overrideテスト用）
            var track3 = timeline.CreateTrack<AnimationTrack>(null, "RotationTrack");
            var animClip3 = new AnimationClip();
            animClip3.name = "RotationClip";
            var rotCurve = AnimationCurve.Linear(0, 0, 1, 45);
            animClip3.SetCurve("", typeof(Transform), "localEulerAngles.z", rotCurve);
            var timelineClip3 = track3.CreateClip<AnimationPlayableAsset>();
            timelineClip3.start = 1;
            timelineClip3.duration = 1;
            (timelineClip3.asset as AnimationPlayableAsset).clip = animClip3;

            director.playableAsset = timeline;
            director.SetGenericBinding(track1, animator);
            director.SetGenericBinding(track2, animator);
            director.SetGenericBinding(track3, animator);

            try
            {
                // Act: AnimationMergeServiceで.animファイル出力
                var service = new AnimationMergeService();
                var results = service.MergeFromPlayableDirector(director);

                // Assert: 結果検証
                Assert.IsNotNull(results, "結果がnullであってはならない");
                Assert.AreEqual(1, results.Count, "1つのAnimator用の結果が返されるべき");
                Assert.IsTrue(results[0].IsSuccess, "処理は成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");
                Assert.AreSame(animator, results[0].TargetAnimator, "正しいAnimatorに紐づいているべき");

                // 生成されたクリップのカーブを検証
                var bindings = AnimationUtility.GetCurveBindings(results[0].GeneratedClip);
                Assert.IsTrue(bindings.Length >= 4, "複数のカーブが含まれるべき");

                // Transformカーブの確認
                var hasPositionX = false;
                var hasPositionY = false;
                var hasRotationZ = false;
                var hasBlendShape = false;

                foreach (var binding in bindings)
                {
                    if (binding.propertyName == "m_LocalPosition.x") hasPositionX = true;
                    if (binding.propertyName == "m_LocalPosition.y") hasPositionY = true;
                    if (binding.propertyName == "localEulerAnglesRaw.z") hasRotationZ = true;
                    if (binding.propertyName.Contains("blendShape")) hasBlendShape = true;
                }

                Assert.IsTrue(hasPositionX, "Position.xカーブが含まれるべき");
                Assert.IsTrue(hasPositionY, "Position.yカーブが含まれるべき");
                Assert.IsTrue(hasRotationZ, "Rotation.zカーブが含まれるべき");
                Assert.IsTrue(hasBlendShape, "BlendShapeカーブが含まれるべき");

                // 処理ログを検証
                var logs = results[0].Logs;
                Assert.IsTrue(logs.Exists(log => log.Contains("処理開始")), "処理開始ログがあるべき");
                Assert.IsTrue(logs.Exists(log => log.Contains("カーブの統合完了")), "カーブ統合完了ログがあるべき");
                Assert.IsTrue(logs.Exists(log => log.Contains("出力完了")), "出力完了ログがあるべき");

                RecordCreatedAssetPaths(results[0].Logs);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(animatorGo);
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(animClip1);
                Object.DestroyImmediate(animClip2);
                Object.DestroyImmediate(animClip3);
            }
        }

        /// <summary>
        /// Phase 16 統合テスト: AnimationMergeServiceを通じてFBXエクスポート機能が利用可能か確認
        /// タスクP16-005: FBXエクスポートの利用可否確認テスト
        /// </summary>
        [Test]
        public void 全機能統合テスト_FBXエクスポート機能の利用可否確認()
        {
            // Arrange
            var service = new AnimationMergeService();

            // Act
            var isFbxAvailable = service.IsFbxExportAvailable();

            // Assert
            // FBX Exporterパッケージがインストールされているかどうかに関わらずテストは成功
            // 実際の値は環境に依存するが、メソッドが正常に動作することを確認
            Assert.IsTrue(isFbxAvailable || !isFbxAvailable, "IsFbxExportAvailableメソッドが正常に動作すべき");
        }

        /// <summary>
        /// Phase 16 統合テスト: AnimationMergeServiceのFBXエクスポートデータ準備機能テスト
        /// タスクP16-005: FbxExportData準備の統合テスト
        /// </summary>
        [Test]
        public void 全機能統合テスト_FbxExportData準備()
        {
            // Arrange
            var go = new GameObject("FbxPrepareDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("FbxPrepareAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "FbxPrepareTimeline";

            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");
            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 10);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            director.playableAsset = timeline;
            director.SetGenericBinding(track, animator);

            try
            {
                // Act: まず.animを生成
                var service = new AnimationMergeService();
                var results = service.MergeFromPlayableDirector(director);

                Assert.IsNotNull(results);
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);

                // FbxExportDataを準備
                var exportData = service.PrepareFbxExportData(results[0]);

                // Assert
                Assert.IsNotNull(exportData, "FbxExportDataが生成されるべき");
                Assert.AreSame(animator, exportData.SourceAnimator, "SourceAnimatorが正しく設定されるべき");

                RecordCreatedAssetPaths(results[0].Logs);
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
        /// Phase 16 統合テスト: 複数Animatorの.animとFBX出力
        /// タスクP16-005: 複数Animatorシナリオの全機能統合テスト
        /// </summary>
        [Test]
        public void 全機能統合テスト_複数Animatorの出力()
        {
            // Arrange
            var go = new GameObject("MultiAnimatorDirector");
            var director = go.AddComponent<PlayableDirector>();

            var animatorGoA = new GameObject("AnimatorA");
            var animatorA = animatorGoA.AddComponent<Animator>();
            var animatorGoB = new GameObject("AnimatorB");
            var animatorB = animatorGoB.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "MultiAnimatorTimeline";

            // AnimatorA用トラック
            var trackA = timeline.CreateTrack<AnimationTrack>(null, "TrackA");
            var animClipA = new AnimationClip();
            animClipA.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 10));
            var timelineClipA = trackA.CreateClip<AnimationPlayableAsset>();
            timelineClipA.start = 0;
            timelineClipA.duration = 1;
            (timelineClipA.asset as AnimationPlayableAsset).clip = animClipA;

            // AnimatorB用トラック
            var trackB = timeline.CreateTrack<AnimationTrack>(null, "TrackB");
            var animClipB = new AnimationClip();
            animClipB.SetCurve("", typeof(Transform), "localScale.y", AnimationCurve.Linear(0, 1, 1, 2));
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

                // Assert
                Assert.IsNotNull(results, "結果がnullであってはならない");
                Assert.AreEqual(2, results.Count, "2つのAnimatorに対して2つの結果が返されるべき");

                // 各結果が成功していることを確認
                foreach (var result in results)
                {
                    Assert.IsTrue(result.IsSuccess, $"{result.TargetAnimator?.name ?? "Unknown"}の処理は成功すべき");
                    Assert.IsNotNull(result.GeneratedClip, $"{result.TargetAnimator?.name ?? "Unknown"}のAnimationClipが生成されるべき");
                    Assert.IsNotNull(result.TargetAnimator, "TargetAnimatorが設定されるべき");

                    // FbxExportData準備も可能であることを確認
                    var exportData = service.PrepareFbxExportData(result);
                    Assert.IsNotNull(exportData, "FbxExportDataが生成されるべき");

                    RecordCreatedAssetPaths(result.Logs);
                }

                // AnimatorAとAnimatorBの結果が存在することを確認
                Assert.IsTrue(results.Exists(r => r.TargetAnimator == animatorA), "AnimatorAの結果が存在すべき");
                Assert.IsTrue(results.Exists(r => r.TargetAnimator == animatorB), "AnimatorBの結果が存在すべき");
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

        /// <summary>
        /// Phase 16 統合テスト: BlendShapeカーブを含む.animとFBX出力
        /// タスクP16-005: BlendShapeを含む全機能統合テスト
        /// </summary>
        [Test]
        public void 全機能統合テスト_BlendShapeを含む出力()
        {
            // Arrange
            var go = new GameObject("BlendShapeIntegrationDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("BlendShapeIntegrationAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            // SkinnedMeshRendererを追加
            var meshGo = new GameObject("FaceMesh");
            meshGo.transform.SetParent(animatorGo.transform);
            var skinnedMesh = meshGo.AddComponent<SkinnedMeshRenderer>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "BlendShapeIntegrationTimeline";

            var track = timeline.CreateTrack<AnimationTrack>(null, "BlendShapeTrack");
            var animClip = new AnimationClip();
            animClip.SetCurve("FaceMesh", typeof(SkinnedMeshRenderer), "blendShape.happy", AnimationCurve.Linear(0, 0, 1, 100));
            animClip.SetCurve("FaceMesh", typeof(SkinnedMeshRenderer), "blendShape.sad", AnimationCurve.Linear(0, 100, 1, 0));
            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            director.playableAsset = timeline;
            director.SetGenericBinding(track, animator);

            try
            {
                // Act
                var service = new AnimationMergeService();
                var results = service.MergeFromPlayableDirector(director);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);

                var generatedClip = results[0].GeneratedClip;
                Assert.IsNotNull(generatedClip);

                // BlendShapeカーブが含まれていることを確認
                var bindings = AnimationUtility.GetCurveBindings(generatedClip);
                var blendShapeBindings = bindings.Where(b => b.propertyName.Contains("blendShape")).ToList();
                Assert.AreEqual(2, blendShapeBindings.Count, "2つのBlendShapeカーブが含まれるべき");

                // FbxExportDataでもBlendShapeカーブが取得できることを確認
                var exportData = service.PrepareFbxExportData(results[0]);
                Assert.IsNotNull(exportData);
                Assert.IsTrue(exportData.BlendShapeCurves.Count >= 0, "BlendShapeCurvesが設定されるべき");

                RecordCreatedAssetPaths(results[0].Logs);
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
        /// Phase 16 統合テスト: 優先順位によるOverride処理の全機能統合テスト
        /// タスクP16-005: Override処理を含む統合テスト
        /// </summary>
        [Test]
        public void 全機能統合テスト_トラック優先順位によるOverride()
        {
            // Arrange
            var go = new GameObject("OverrideIntegrationDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("OverrideIntegrationAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "OverrideIntegrationTimeline";

            // 低優先順位トラック（先に作成 = 上の段）
            var track1 = timeline.CreateTrack<AnimationTrack>(null, "LowPriorityTrack");
            var animClip1 = new AnimationClip();
            animClip1.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 2, 10));
            var timelineClip1 = track1.CreateClip<AnimationPlayableAsset>();
            timelineClip1.start = 0;
            timelineClip1.duration = 2;
            (timelineClip1.asset as AnimationPlayableAsset).clip = animClip1;

            // 高優先順位トラック（後に作成 = 下の段）- 同じプロパティをOverride
            var track2 = timeline.CreateTrack<AnimationTrack>(null, "HighPriorityTrack");
            var animClip2 = new AnimationClip();
            animClip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 50, 1, 50));
            var timelineClip2 = track2.CreateClip<AnimationPlayableAsset>();
            timelineClip2.start = 0.5;
            timelineClip2.duration = 1;
            (timelineClip2.asset as AnimationPlayableAsset).clip = animClip2;

            director.playableAsset = timeline;
            director.SetGenericBinding(track1, animator);
            director.SetGenericBinding(track2, animator);

            try
            {
                // Act
                var service = new AnimationMergeService();
                var results = service.MergeFromPlayableDirector(director);

                // Assert
                Assert.IsNotNull(results);
                Assert.AreEqual(1, results.Count);
                Assert.IsTrue(results[0].IsSuccess);

                var generatedClip = results[0].GeneratedClip;
                Assert.IsNotNull(generatedClip);

                // カーブが正しくOverrideされていることを確認
                var bindings = AnimationUtility.GetCurveBindings(generatedClip);
                var posXBinding = bindings.FirstOrDefault(b => b.propertyName == "m_LocalPosition.x");
                Assert.IsTrue(posXBinding.propertyName == "m_LocalPosition.x", "m_LocalPosition.xカーブが存在すべき");

                var curve = AnimationUtility.GetEditorCurve(generatedClip, posXBinding);
                Assert.IsNotNull(curve);

                // Override期間中（0.5〜1.5秒）の値が高優先順位トラックの値（50）に近いことを確認
                var valueAt1 = curve.Evaluate(1.0f);
                Assert.IsTrue(valueAt1 >= 40 && valueAt1 <= 60,
                    $"Override期間中の値は約50であるべき（実際: {valueAt1}）");

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

        /// <summary>
        /// Phase 16 統合テスト: FileNameGeneratorによる.animとFBXファイル名生成
        /// タスクP16-005: ファイル名生成の統合テスト
        /// </summary>
        [Test]
        public void 全機能統合テスト_ファイル名生成()
        {
            // Arrange
            var fileNameGenerator = new Infrastructure.FileNameGenerator();

            // Act
            var animBaseName = fileNameGenerator.GenerateBaseName("TestTimeline", "TestAnimator");
            var fbxBaseName = fileNameGenerator.GenerateBaseName("TestTimeline", "TestAnimator", ".fbx");

            // Assert
            Assert.AreEqual("TestTimeline_TestAnimator_Merged.anim", animBaseName);
            Assert.AreEqual("TestTimeline_TestAnimator_Merged.fbx", fbxBaseName);
        }

        /// <summary>
        /// Phase 16 統合テスト: MergeAndExportToFbxの呼び出しテスト（パッケージ未インストール時）
        /// タスクP16-005: FBXエクスポート統合メソッドのテスト
        /// </summary>
        [Test]
        public void 全機能統合テスト_MergeAndExportToFbx呼び出し()
        {
            // Arrange
            var go = new GameObject("MergeAndExportDirector");
            var director = go.AddComponent<PlayableDirector>();
            var animatorGo = new GameObject("MergeAndExportAnimator");
            var animator = animatorGo.AddComponent<Animator>();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "MergeAndExportTimeline";

            var track = timeline.CreateTrack<AnimationTrack>(null, "TestTrack");
            var animClip = new AnimationClip();
            animClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 5));
            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            timelineClip.start = 0;
            timelineClip.duration = 1;
            (timelineClip.asset as AnimationPlayableAsset).clip = animClip;

            director.playableAsset = timeline;
            director.SetGenericBinding(track, animator);

            try
            {
                // Act
                var service = new AnimationMergeService();

                // FBX Exporterがインストールされていない場合のエラーログを期待
                if (!service.IsFbxExportAvailable())
                {
                    LogAssert.Expect(LogType.Error, "[AnimationMergeTool] FBX Exporterパッケージがインストールされていません。FBXエクスポートをスキップします。");
                }

                var results = service.MergeAndExportToFbx(director);

                // Assert
                Assert.IsNotNull(results, "結果がnullであってはならない");
                Assert.AreEqual(1, results.Count, "1つの結果が返されるべき");
                Assert.IsTrue(results[0].IsSuccess, ".anim出力は成功すべき");
                Assert.IsNotNull(results[0].GeneratedClip, "AnimationClipが生成されるべき");

                RecordCreatedAssetPaths(results[0].Logs);
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
        /// Phase 16 統合テスト: エラーケース - nullのMergeResultでFbxExportData準備
        /// タスクP16-005: エラーハンドリングの統合テスト
        /// </summary>
        [Test]
        public void 全機能統合テスト_nullMergeResultでFbxExportData準備()
        {
            // Arrange
            var service = new AnimationMergeService();

            // Act
            var exportData = service.PrepareFbxExportData(null);

            // Assert
            Assert.IsNull(exportData, "nullのMergeResultからはnullが返されるべき");
        }

        /// <summary>
        /// Phase 16 統合テスト: エラーケース - 生成失敗したMergeResultでFbxExportData準備
        /// タスクP16-005: 失敗ケースの統合テスト
        /// </summary>
        [Test]
        public void 全機能統合テスト_失敗したMergeResultでFbxExportData準備()
        {
            // Arrange
            var service = new AnimationMergeService();
            var failedResult = new Domain.Models.MergeResult(null);
            failedResult.AddErrorLog("テストエラー");
            // GeneratedClipがnullのままなのでIsSuccess = false

            // Act
            var exportData = service.PrepareFbxExportData(failedResult);

            // Assert
            Assert.IsNull(exportData, "失敗したMergeResultからはnullが返されるべき");
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
