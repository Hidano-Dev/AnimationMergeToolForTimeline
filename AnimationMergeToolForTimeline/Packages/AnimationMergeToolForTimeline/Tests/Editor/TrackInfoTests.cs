using NUnit.Framework;
using UnityEngine;
using UnityEngine.Timeline;
using AnimationMergeTool.Editor.Domain.Models;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// TrackInfoクラスの単体テスト
    /// </summary>
    public class TrackInfoTests
    {
        private TimelineAsset _timelineAsset;
        private AnimationTrack _animationTrack;
        private GameObject _testGameObject;
        private Animator _testAnimator;

        [SetUp]
        public void SetUp()
        {
            // テスト用のTimelineAssetとAnimationTrackを作成
            _timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
            _animationTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Test Track");

            // テスト用のGameObjectとAnimatorを作成
            _testGameObject = new GameObject("Test Animator Object");
            _testAnimator = _testGameObject.AddComponent<Animator>();
        }

        [TearDown]
        public void TearDown()
        {
            // テストリソースのクリーンアップ
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
            if (_timelineAsset != null)
            {
                Object.DestroyImmediate(_timelineAsset);
            }
        }

        [Test]
        public void Constructor_デフォルト値で初期化される()
        {
            // Arrange & Act
            var trackInfo = new TrackInfo(_animationTrack);

            // Assert
            Assert.AreEqual(_animationTrack, trackInfo.Track);
            Assert.AreEqual(0, trackInfo.Priority);
            Assert.IsNull(trackInfo.BoundAnimator);
            Assert.IsNotNull(trackInfo.OverrideTracks);
            Assert.AreEqual(0, trackInfo.OverrideTracks.Count);
        }

        [Test]
        public void Constructor_指定した値で初期化される()
        {
            // Arrange
            int priority = 5;

            // Act
            var trackInfo = new TrackInfo(_animationTrack, priority, _testAnimator);

            // Assert
            Assert.AreEqual(_animationTrack, trackInfo.Track);
            Assert.AreEqual(priority, trackInfo.Priority);
            Assert.AreEqual(_testAnimator, trackInfo.BoundAnimator);
        }

        [Test]
        public void IsMuted_Trackがnullの場合falseを返す()
        {
            // Arrange
            var trackInfo = new TrackInfo(null);

            // Act & Assert
            Assert.IsFalse(trackInfo.IsMuted);
        }

        [Test]
        public void IsMuted_TrackがMuteされていない場合falseを返す()
        {
            // Arrange
            _animationTrack.muted = false;
            var trackInfo = new TrackInfo(_animationTrack);

            // Act & Assert
            Assert.IsFalse(trackInfo.IsMuted);
        }

        [Test]
        public void IsMuted_TrackがMuteされている場合trueを返す()
        {
            // Arrange
            _animationTrack.muted = true;
            var trackInfo = new TrackInfo(_animationTrack);

            // Act & Assert
            Assert.IsTrue(trackInfo.IsMuted);
        }

        [Test]
        public void AddOverrideTrack_OverrideTrackが追加される()
        {
            // Arrange
            var parentTrackInfo = new TrackInfo(_animationTrack);
            var overrideTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Override Track");
            var overrideTrackInfo = new TrackInfo(overrideTrack, 1);

            // Act
            parentTrackInfo.AddOverrideTrack(overrideTrackInfo);

            // Assert
            Assert.AreEqual(1, parentTrackInfo.OverrideTracks.Count);
            Assert.AreEqual(overrideTrackInfo, parentTrackInfo.OverrideTracks[0]);
        }

        [Test]
        public void AddOverrideTrack_nullを渡しても例外が発生しない()
        {
            // Arrange
            var trackInfo = new TrackInfo(_animationTrack);

            // Act & Assert
            Assert.DoesNotThrow(() => trackInfo.AddOverrideTrack(null));
            Assert.AreEqual(0, trackInfo.OverrideTracks.Count);
        }

        [Test]
        public void AddOverrideTrack_複数のOverrideTrackを追加できる()
        {
            // Arrange
            var parentTrackInfo = new TrackInfo(_animationTrack);
            var overrideTrack1 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Override Track 1");
            var overrideTrack2 = _timelineAsset.CreateTrack<AnimationTrack>(null, "Override Track 2");
            var overrideTrackInfo1 = new TrackInfo(overrideTrack1, 1);
            var overrideTrackInfo2 = new TrackInfo(overrideTrack2, 2);

            // Act
            parentTrackInfo.AddOverrideTrack(overrideTrackInfo1);
            parentTrackInfo.AddOverrideTrack(overrideTrackInfo2);

            // Assert
            Assert.AreEqual(2, parentTrackInfo.OverrideTracks.Count);
        }

        [Test]
        public void IsValid_MuteされておらずAnimatorがバインドされている場合trueを返す()
        {
            // Arrange
            _animationTrack.muted = false;
            var trackInfo = new TrackInfo(_animationTrack, 0, _testAnimator);

            // Act & Assert
            Assert.IsTrue(trackInfo.IsValid);
        }

        [Test]
        public void IsValid_Muteされている場合falseを返す()
        {
            // Arrange
            _animationTrack.muted = true;
            var trackInfo = new TrackInfo(_animationTrack, 0, _testAnimator);

            // Act & Assert
            Assert.IsFalse(trackInfo.IsValid);
        }

        [Test]
        public void IsValid_Animatorがバインドされていない場合falseを返す()
        {
            // Arrange
            _animationTrack.muted = false;
            var trackInfo = new TrackInfo(_animationTrack, 0, null);

            // Act & Assert
            Assert.IsFalse(trackInfo.IsValid);
        }

        [Test]
        public void IsValid_MuteされていてAnimatorもバインドされていない場合falseを返す()
        {
            // Arrange
            _animationTrack.muted = true;
            var trackInfo = new TrackInfo(_animationTrack, 0, null);

            // Act & Assert
            Assert.IsFalse(trackInfo.IsValid);
        }

        [Test]
        public void Priority_設定と取得ができる()
        {
            // Arrange
            var trackInfo = new TrackInfo(_animationTrack);

            // Act
            trackInfo.Priority = 10;

            // Assert
            Assert.AreEqual(10, trackInfo.Priority);
        }

        [Test]
        public void BoundAnimator_設定と取得ができる()
        {
            // Arrange
            var trackInfo = new TrackInfo(_animationTrack);

            // Act
            trackInfo.BoundAnimator = _testAnimator;

            // Assert
            Assert.AreEqual(_testAnimator, trackInfo.BoundAnimator);
        }
    }
}
