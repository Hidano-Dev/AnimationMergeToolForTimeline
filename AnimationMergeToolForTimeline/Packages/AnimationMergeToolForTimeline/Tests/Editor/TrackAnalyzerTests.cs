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
    }
}
