using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Timeline;
using AnimationMergeTool.Editor.Domain;
using AnimationMergeTool.Editor.Domain.Models;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// TrackAnalyzerクラスの単体テスト
    /// </summary>
    public class TrackAnalyzerTests
    {
        private TimelineAsset _timelineAsset;

        [SetUp]
        public void SetUp()
        {
            _timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_timelineAsset != null)
            {
                Object.DestroyImmediate(_timelineAsset);
            }
        }

        #region GetAllAnimationTracks テスト

        [Test]
        public void GetAllAnimationTracks_TimelineAssetがnullの場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(null);

            // Act
            var result = analyzer.GetAllAnimationTracks();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAllAnimationTracks_トラックがない場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAllAnimationTracks();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAllAnimationTracks_AnimationTrackが1つある場合1つのTrackInfoを返す()
        {
            // Arrange
            var animationTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Test Animation Track");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAllAnimationTracks();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(animationTrack, result[0].Track);
        }

        [Test]
        public void GetAllAnimationTracks_複数のAnimationTrackがある場合全てのTrackInfoを返す()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var track2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 2");
            var track3 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 3");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAllAnimationTracks();

            // Assert
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void GetAllAnimationTracks_非AnimationTrackは除外される()
        {
            // Arrange
            var animationTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Animation Track");
            var groupTrack = _timelineAsset.CreateTrack<GroupTrack>(null, "Group Track");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAllAnimationTracks();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(animationTrack, result[0].Track);
        }

        [Test]
        public void GetAllAnimationTracks_返されるTrackInfoのTrackプロパティが正しく設定される()
        {
            // Arrange
            var track = _timelineAsset.CreateTrack<AnimationTrack>(null, "Test Track");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAllAnimationTracks();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result[0].Track);
            Assert.AreEqual(track.name, result[0].Track.name);
        }

        #endregion

        #region GetOverrideTracks テスト

        [Test]
        public void GetOverrideTracks_parentTrackがnullの場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetOverrideTracks(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetOverrideTracks_子トラックがない場合空のリストを返す()
        {
            // Arrange
            var parentTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Parent Track");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetOverrideTracks(parentTrack);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetOverrideTracks_OverrideTrackが1つある場合1つのTrackInfoを返す()
        {
            // Arrange
            var parentTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Parent Track");
            var overrideTrack = _timelineAsset.CreateTrack<AnimationTrack>(parentTrack, "Override Track");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetOverrideTracks(parentTrack);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(overrideTrack, result[0].Track);
        }

        [Test]
        public void GetOverrideTracks_複数のOverrideTrackがある場合全てのTrackInfoを返す()
        {
            // Arrange
            var parentTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Parent Track");
            var override1 = _timelineAsset.CreateTrack<AnimationTrack>(parentTrack, "Override 1");
            var override2 = _timelineAsset.CreateTrack<AnimationTrack>(parentTrack, "Override 2");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetOverrideTracks(parentTrack);

            // Assert
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void GetOverrideTracks_親トラックのバインド情報とは独立したTrackInfoが返される()
        {
            // Arrange
            var parentTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Parent Track");
            var overrideTrack = _timelineAsset.CreateTrack<AnimationTrack>(parentTrack, "Override Track");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetOverrideTracks(parentTrack);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result[0]);
            Assert.AreEqual(overrideTrack.name, result[0].Track.name);
        }

        #endregion

        #region FilterNonMutedTracks テスト

        [Test]
        public void FilterNonMutedTracks_tracksがnullの場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.FilterNonMutedTracks(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void FilterNonMutedTracks_空のリストの場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);
            var tracks = new List<TrackInfo>();

            // Act
            var result = analyzer.FilterNonMutedTracks(tracks);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void FilterNonMutedTracks_Muteされていないトラックのみを返す()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var track2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 2");
            track2.muted = true; // Mute状態に設定
            var track3 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 3");

            var tracks = new List<TrackInfo>
            {
                new TrackInfo(track1),
                new TrackInfo(track2),
                new TrackInfo(track3)
            };

            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.FilterNonMutedTracks(tracks);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(track1, result[0].Track);
            Assert.AreEqual(track3, result[1].Track);
        }

        [Test]
        public void FilterNonMutedTracks_全てMuteされている場合空のリストを返す()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            track1.muted = true;
            var track2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 2");
            track2.muted = true;

            var tracks = new List<TrackInfo>
            {
                new TrackInfo(track1),
                new TrackInfo(track2)
            };

            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.FilterNonMutedTracks(tracks);

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void FilterNonMutedTracks_全てMuteされていない場合全てのトラックを返す()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var track2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 2");

            var tracks = new List<TrackInfo>
            {
                new TrackInfo(track1),
                new TrackInfo(track2)
            };

            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.FilterNonMutedTracks(tracks);

            // Assert
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void FilterNonMutedTracks_nullのTrackInfoは除外される()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");

            var tracks = new List<TrackInfo>
            {
                new TrackInfo(track1),
                null,
                new TrackInfo(track1)
            };

            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.FilterNonMutedTracks(tracks);

            // Assert
            Assert.AreEqual(2, result.Count);
        }

        #endregion

        #region DetectUnboundTracks テスト

        [Test]
        public void DetectUnboundTracks_tracksがnullの場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.DetectUnboundTracks(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void DetectUnboundTracks_空のリストの場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);
            var tracks = new List<TrackInfo>();

            // Act
            var result = analyzer.DetectUnboundTracks(tracks);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void DetectUnboundTracks_BoundAnimatorがnullのトラックを検出する()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var track2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 2");

            var tracks = new List<TrackInfo>
            {
                new TrackInfo(track1), // BoundAnimator = null
                new TrackInfo(track2)  // BoundAnimator = null
            };

            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.DetectUnboundTracks(tracks);

            // Assert
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void DetectUnboundTracks_BoundAnimatorが設定されているトラックは含まない()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var track2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 2");

            // ダミーのAnimatorを作成
            var gameObject = new GameObject("Test Animator");
            var animator = gameObject.AddComponent<Animator>();

            var tracks = new List<TrackInfo>
            {
                new TrackInfo(track1, 0, animator), // BoundAnimator = animator
                new TrackInfo(track2)               // BoundAnimator = null
            };

            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.DetectUnboundTracks(tracks);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(track2, result[0].Track);

            // クリーンアップ
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void DetectUnboundTracks_全てバインドされている場合空のリストを返す()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var track2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 2");

            // ダミーのAnimatorを作成
            var gameObject = new GameObject("Test Animator");
            var animator = gameObject.AddComponent<Animator>();

            var tracks = new List<TrackInfo>
            {
                new TrackInfo(track1, 0, animator),
                new TrackInfo(track2, 0, animator)
            };

            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.DetectUnboundTracks(tracks);

            // Assert
            Assert.AreEqual(0, result.Count);

            // クリーンアップ
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void DetectUnboundTracks_nullのTrackInfoは除外される()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");

            var tracks = new List<TrackInfo>
            {
                new TrackInfo(track1), // BoundAnimator = null
                null
            };

            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.DetectUnboundTracks(tracks);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(track1, result[0].Track);
        }

        [Test]
        public void DetectUnboundTracks_エラーログが出力される()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Unbound Track");

            var tracks = new List<TrackInfo>
            {
                new TrackInfo(track1)
            };

            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            // Debug.LogErrorの呼び出しを確認（ログを観察するテスト）
            // Unity Test Frameworkでは LogAssert.Expect でログを検証できる
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
                "[AnimationMergeTool] トラック \"Unbound Track\" にAnimatorがバインドされていません。");

            var result = analyzer.DetectUnboundTracks(tracks);

            // Assert
            Assert.AreEqual(1, result.Count);
        }

        #endregion

        #region GetTrackIndex テスト

        [Test]
        public void GetTrackIndex_TimelineAssetがnullの場合マイナス1を返す()
        {
            // Arrange
            var track = _timelineAsset.CreateTrack<AnimationTrack>(null, "Test Track");
            var analyzer = new TrackAnalyzer(null);

            // Act
            var result = analyzer.GetTrackIndex(track);

            // Assert
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void GetTrackIndex_trackがnullの場合マイナス1を返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetTrackIndex(null);

            // Assert
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void GetTrackIndex_トラックが存在する場合正しいインデックスを返す()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var track2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 2");
            var track3 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 3");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act & Assert
            Assert.AreEqual(0, analyzer.GetTrackIndex(track1));
            Assert.AreEqual(1, analyzer.GetTrackIndex(track2));
            Assert.AreEqual(2, analyzer.GetTrackIndex(track3));
        }

        [Test]
        public void GetTrackIndex_トラックが存在しない場合マイナス1を返す()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var otherTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var otherTrack = otherTimeline.CreateTrack<AnimationTrack>(null, "Other Track");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetTrackIndex(otherTrack);

            // Assert
            Assert.AreEqual(-1, result);

            // クリーンアップ
            Object.DestroyImmediate(otherTimeline);
        }

        #endregion

        #region GetOutputTracksInOrder テスト

        [Test]
        public void GetOutputTracksInOrder_TimelineAssetがnullの場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(null);

            // Act
            var result = analyzer.GetOutputTracksInOrder();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetOutputTracksInOrder_トラックがない場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetOutputTracksInOrder();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetOutputTracksInOrder_トラックが作成順に返される()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var track2 = _timelineAsset.CreateTrack<GroupTrack>(null, "Group");
            var track3 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 3");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetOutputTracksInOrder();

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(track1, result[0]);
            Assert.AreEqual(track2, result[1]);
            Assert.AreEqual(track3, result[2]);
        }

        #endregion

        #region GetAnimationTracksWithIndex テスト

        [Test]
        public void GetAnimationTracksWithIndex_TimelineAssetがnullの場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(null);

            // Act
            var result = analyzer.GetAnimationTracksWithIndex();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAnimationTracksWithIndex_AnimationTrackのみがインデックス付きで返される()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Animation 1");
            var groupTrack = _timelineAsset.CreateTrack<GroupTrack>(null, "Group");
            var track2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Animation 2");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAnimationTracksWithIndex();

            // Assert
            Assert.AreEqual(2, result.Count);
            // track1 はインデックス0
            Assert.AreEqual(0, result[0].index);
            Assert.AreEqual(track1, result[0].trackInfo.Track);
            // track2 はインデックス2（GroupTrackがインデックス1）
            Assert.AreEqual(2, result[1].index);
            Assert.AreEqual(track2, result[1].trackInfo.Track);
        }

        [Test]
        public void GetAnimationTracksWithIndex_インデックスはTimeline上の全トラック位置を反映する()
        {
            // Arrange
            var groupTrack1 = _timelineAsset.CreateTrack<GroupTrack>(null, "Group 1");
            var animTrack1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Animation 1");
            var groupTrack2 = _timelineAsset.CreateTrack<GroupTrack>(null, "Group 2");
            var animTrack2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Animation 2");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAnimationTracksWithIndex();

            // Assert
            Assert.AreEqual(2, result.Count);
            // animTrack1 はインデックス1（GroupTrack1がインデックス0）
            Assert.AreEqual(1, result[0].index);
            Assert.AreEqual(animTrack1, result[0].trackInfo.Track);
            // animTrack2 はインデックス3（GroupTrack2がインデックス2）
            Assert.AreEqual(3, result[1].index);
            Assert.AreEqual(animTrack2, result[1].trackInfo.Track);
        }

        #endregion

        #region GetAnimationTracksWithPriority テスト

        [Test]
        public void GetAnimationTracksWithPriority_TimelineAssetがnullの場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(null);

            // Act
            var result = analyzer.GetAnimationTracksWithPriority();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAnimationTracksWithPriority_トラックがない場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAnimationTracksWithPriority();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAnimationTracksWithPriority_下にあるトラックほど高い優先順位を持つ()
        {
            // Arrange
            // Timeline上の順序: Track1(上) -> Track2(中) -> Track3(下)
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var track2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 2");
            var track3 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 3");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAnimationTracksWithPriority();

            // Assert
            Assert.AreEqual(3, result.Count);
            // Track1 はインデックス0 なので Priority = 0
            Assert.AreEqual(track1, result[0].Track);
            Assert.AreEqual(0, result[0].Priority);
            // Track2 はインデックス1 なので Priority = 1
            Assert.AreEqual(track2, result[1].Track);
            Assert.AreEqual(1, result[1].Priority);
            // Track3 はインデックス2 なので Priority = 2（最も高い）
            Assert.AreEqual(track3, result[2].Track);
            Assert.AreEqual(2, result[2].Priority);
        }

        [Test]
        public void GetAnimationTracksWithPriority_GroupTrackを挟んでも正しい優先順位が割り当てられる()
        {
            // Arrange
            // Timeline上の順序: Track1(0) -> GroupTrack(1) -> Track2(2)
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var groupTrack = _timelineAsset.CreateTrack<GroupTrack>(null, "Group");
            var track2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 2");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAnimationTracksWithPriority();

            // Assert
            Assert.AreEqual(2, result.Count);
            // Track1 はインデックス0 なので Priority = 0
            Assert.AreEqual(track1, result[0].Track);
            Assert.AreEqual(0, result[0].Priority);
            // Track2 はインデックス2 なので Priority = 2（GroupTrackがインデックス1）
            Assert.AreEqual(track2, result[1].Track);
            Assert.AreEqual(2, result[1].Priority);
        }

        [Test]
        public void GetAnimationTracksWithPriority_優先順位の大小関係が正しい()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var track2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 2");
            var track3 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 3");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAnimationTracksWithPriority();

            // Assert
            // 下にあるトラックほど高い優先順位 → Priority値が大きい
            Assert.IsTrue(result[0].Priority < result[1].Priority);
            Assert.IsTrue(result[1].Priority < result[2].Priority);
        }

        #endregion

        #region AssignPriorities テスト

        [Test]
        public void AssignPriorities_tracksがnullの場合例外を投げない()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act & Assert
            Assert.DoesNotThrow(() => analyzer.AssignPriorities(null));
        }

        [Test]
        public void AssignPriorities_TimelineAssetがnullの場合何もしない()
        {
            // Arrange
            var track = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track");
            var trackInfo = new TrackInfo(track);
            var tracks = new List<TrackInfo> { trackInfo };
            var analyzer = new TrackAnalyzer(null);

            // Act
            analyzer.AssignPriorities(tracks);

            // Assert
            Assert.AreEqual(0, trackInfo.Priority); // 変更されていない
        }

        [Test]
        public void AssignPriorities_トラックに正しい優先順位を割り当てる()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var track2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 2");
            var track3 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 3");

            var trackInfo1 = new TrackInfo(track1);
            var trackInfo2 = new TrackInfo(track2);
            var trackInfo3 = new TrackInfo(track3);
            var tracks = new List<TrackInfo> { trackInfo1, trackInfo2, trackInfo3 };

            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            analyzer.AssignPriorities(tracks);

            // Assert
            Assert.AreEqual(0, trackInfo1.Priority);
            Assert.AreEqual(1, trackInfo2.Priority);
            Assert.AreEqual(2, trackInfo3.Priority);
        }

        [Test]
        public void AssignPriorities_nullのTrackInfoはスキップされる()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var trackInfo1 = new TrackInfo(track1);
            var tracks = new List<TrackInfo> { trackInfo1, null };

            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act & Assert
            Assert.DoesNotThrow(() => analyzer.AssignPriorities(tracks));
            Assert.AreEqual(0, trackInfo1.Priority);
        }

        [Test]
        public void AssignPriorities_Timelineに存在しないトラックは優先順位が変更されない()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var otherTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var otherTrack = otherTimeline.CreateTrack<AnimationTrack>(null, "Other Track");

            var trackInfo1 = new TrackInfo(track1);
            var trackInfoOther = new TrackInfo(otherTrack);
            trackInfoOther.Priority = 999; // 初期値を設定

            var tracks = new List<TrackInfo> { trackInfo1, trackInfoOther };
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            analyzer.AssignPriorities(tracks);

            // Assert
            Assert.AreEqual(0, trackInfo1.Priority);
            Assert.AreEqual(999, trackInfoOther.Priority); // 変更されていない

            // クリーンアップ
            Object.DestroyImmediate(otherTimeline);
        }

        [Test]
        public void AssignPriorities_リスト内の順序に関係なく正しい優先順位が割り当てられる()
        {
            // Arrange
            var track1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 1");
            var track2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 2");
            var track3 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Track 3");

            // リストの順序を逆にする
            var trackInfo1 = new TrackInfo(track1);
            var trackInfo2 = new TrackInfo(track2);
            var trackInfo3 = new TrackInfo(track3);
            var tracks = new List<TrackInfo> { trackInfo3, trackInfo1, trackInfo2 };

            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            analyzer.AssignPriorities(tracks);

            // Assert
            // リスト順序に関係なく、Timelineの位置に基づいて優先順位が設定される
            Assert.AreEqual(0, trackInfo1.Priority);
            Assert.AreEqual(1, trackInfo2.Priority);
            Assert.AreEqual(2, trackInfo3.Priority);
        }

        #endregion

        #region GetParentGroup テスト

        [Test]
        public void GetParentGroup_trackがnullの場合nullを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetParentGroup(null);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetParentGroup_ルートトラックの場合nullを返す()
        {
            // Arrange
            var track = _timelineAsset.CreateTrack<AnimationTrack>(null, "Root Track");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetParentGroup(track);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetParentGroup_GroupTrack内のトラックの場合親GroupTrackを返す()
        {
            // Arrange
            var groupTrack = _timelineAsset.CreateTrack<GroupTrack>(null, "Group");
            var childTrack = _timelineAsset.CreateTrack<AnimationTrack>(groupTrack, "Child Track");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetParentGroup(childTrack);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(groupTrack, result);
        }

        #endregion

        #region GetChildTracksInGroup テスト

        [Test]
        public void GetChildTracksInGroup_groupTrackがnullの場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetChildTracksInGroup(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetChildTracksInGroup_子トラックがない場合空のリストを返す()
        {
            // Arrange
            var groupTrack = _timelineAsset.CreateTrack<GroupTrack>(null, "Empty Group");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetChildTracksInGroup(groupTrack);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetChildTracksInGroup_子トラックがある場合全ての子トラックを返す()
        {
            // Arrange
            var groupTrack = _timelineAsset.CreateTrack<GroupTrack>(null, "Group");
            var child1 = _timelineAsset.CreateTrack<AnimationTrack>(groupTrack, "Child 1");
            var child2 = _timelineAsset.CreateTrack<AnimationTrack>(groupTrack, "Child 2");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetChildTracksInGroup(groupTrack);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(child1));
            Assert.IsTrue(result.Contains(child2));
        }

        #endregion

        #region GetAnimationTracksInGroup テスト

        [Test]
        public void GetAnimationTracksInGroup_groupTrackがnullの場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAnimationTracksInGroup(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAnimationTracksInGroup_AnimationTrackのみを返す()
        {
            // Arrange
            var groupTrack = _timelineAsset.CreateTrack<GroupTrack>(null, "Group");
            var animTrack = _timelineAsset.CreateTrack<AnimationTrack>(groupTrack, "Animation");
            var nestedGroup = _timelineAsset.CreateTrack<GroupTrack>(groupTrack, "Nested Group");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAnimationTracksInGroup(groupTrack);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(animTrack, result[0].Track);
        }

        #endregion

        #region GetAllGroupTracks テスト

        [Test]
        public void GetAllGroupTracks_TimelineAssetがnullの場合空のリストを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(null);

            // Act
            var result = analyzer.GetAllGroupTracks();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAllGroupTracks_GroupTrackがない場合空のリストを返す()
        {
            // Arrange
            var animTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Animation");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAllGroupTracks();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAllGroupTracks_ルートレベルのGroupTrackを返す()
        {
            // Arrange
            var group1 = _timelineAsset.CreateTrack<GroupTrack>(null, "Group 1");
            var group2 = _timelineAsset.CreateTrack<GroupTrack>(null, "Group 2");
            var animTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Animation");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetAllGroupTracks();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(group1));
            Assert.IsTrue(result.Contains(group2));
        }

        #endregion

        #region IsTrackInGroup テスト

        [Test]
        public void IsTrackInGroup_trackがnullの場合falseを返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.IsTrackInGroup(null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsTrackInGroup_ルートトラックの場合falseを返す()
        {
            // Arrange
            var track = _timelineAsset.CreateTrack<AnimationTrack>(null, "Root Track");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.IsTrackInGroup(track);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsTrackInGroup_GroupTrack内のトラックの場合trueを返す()
        {
            // Arrange
            var groupTrack = _timelineAsset.CreateTrack<GroupTrack>(null, "Group");
            var childTrack = _timelineAsset.CreateTrack<AnimationTrack>(groupTrack, "Child Track");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.IsTrackInGroup(childTrack);

            // Assert
            Assert.IsTrue(result);
        }

        #endregion

        #region GetTrackDepth テスト

        [Test]
        public void GetTrackDepth_trackがnullの場合マイナス1を返す()
        {
            // Arrange
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetTrackDepth(null);

            // Assert
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void GetTrackDepth_ルートトラックの場合0を返す()
        {
            // Arrange
            var track = _timelineAsset.CreateTrack<AnimationTrack>(null, "Root Track");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetTrackDepth(track);

            // Assert
            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetTrackDepth_GroupTrack内のトラックの場合1を返す()
        {
            // Arrange
            var groupTrack = _timelineAsset.CreateTrack<GroupTrack>(null, "Group");
            var childTrack = _timelineAsset.CreateTrack<AnimationTrack>(groupTrack, "Child Track");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetTrackDepth(childTrack);

            // Assert
            Assert.AreEqual(1, result);
        }

        [Test]
        public void GetTrackDepth_ネストされたGroupTrack内のトラックの場合2を返す()
        {
            // Arrange
            var group1 = _timelineAsset.CreateTrack<GroupTrack>(null, "Group 1");
            var group2 = _timelineAsset.CreateTrack<GroupTrack>(group1, "Group 2");
            var childTrack = _timelineAsset.CreateTrack<AnimationTrack>(group2, "Nested Child");
            var analyzer = new TrackAnalyzer(_timelineAsset);

            // Act
            var result = analyzer.GetTrackDepth(childTrack);

            // Assert
            Assert.AreEqual(2, result);
        }

        #endregion
    }
}
