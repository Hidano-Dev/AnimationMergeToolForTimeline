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
    }
}
