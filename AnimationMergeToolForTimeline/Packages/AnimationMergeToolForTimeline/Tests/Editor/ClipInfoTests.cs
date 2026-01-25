using NUnit.Framework;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using AnimationMergeTool.Editor.Domain.Models;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// ClipInfoクラスの単体テスト
    /// </summary>
    public class ClipInfoTests
    {
        private TimelineAsset _timelineAsset;
        private AnimationTrack _animationTrack;
        private AnimationClip _animationClip;
        private TimelineClip _timelineClip;

        [SetUp]
        public void SetUp()
        {
            // テスト用のTimelineAssetとAnimationTrackを作成
            _timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
            _animationTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Test Track");

            // テスト用のAnimationClipを作成
            _animationClip = new AnimationClip();
            _animationClip.name = "Test Animation";

            // TimelineClipを作成
            _timelineClip = _animationTrack.CreateClip(_animationClip);
            _timelineClip.start = 1.0;
            _timelineClip.duration = 2.0;
        }

        [TearDown]
        public void TearDown()
        {
            // テストリソースのクリーンアップ
            if (_animationClip != null)
            {
                Object.DestroyImmediate(_animationClip);
            }
            if (_timelineAsset != null)
            {
                Object.DestroyImmediate(_timelineAsset);
            }
        }

        [Test]
        public void Constructor_TimelineClipとAnimationClipが設定される()
        {
            // Arrange & Act
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Assert
            Assert.AreEqual(_timelineClip, clipInfo.TimelineClip);
            Assert.AreEqual(_animationClip, clipInfo.AnimationClip);
        }

        [Test]
        public void Constructor_nullを渡しても例外が発生しない()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => new ClipInfo(null, null));
        }

        [Test]
        public void StartTime_TimelineClipの開始時間を返す()
        {
            // Arrange
            _timelineClip.start = 1.5;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act & Assert
            Assert.AreEqual(1.5, clipInfo.StartTime, 0.0001);
        }

        [Test]
        public void StartTime_TimelineClipがnullの場合0を返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(null, _animationClip);

            // Act & Assert
            Assert.AreEqual(0, clipInfo.StartTime);
        }

        [Test]
        public void EndTime_TimelineClipの終了時間を返す()
        {
            // Arrange
            _timelineClip.start = 1.0;
            _timelineClip.duration = 2.0;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act & Assert
            Assert.AreEqual(3.0, clipInfo.EndTime, 0.0001);
        }

        [Test]
        public void EndTime_TimelineClipがnullの場合0を返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(null, _animationClip);

            // Act & Assert
            Assert.AreEqual(0, clipInfo.EndTime);
        }

        [Test]
        public void Duration_TimelineClipの長さを返す()
        {
            // Arrange
            _timelineClip.duration = 3.5;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act & Assert
            Assert.AreEqual(3.5, clipInfo.Duration, 0.0001);
        }

        [Test]
        public void Duration_TimelineClipがnullの場合0を返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(null, _animationClip);

            // Act & Assert
            Assert.AreEqual(0, clipInfo.Duration);
        }

        [Test]
        public void ClipIn_TimelineClipのclipInを返す()
        {
            // Arrange
            _timelineClip.clipIn = 0.5;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act & Assert
            Assert.AreEqual(0.5, clipInfo.ClipIn, 0.0001);
        }

        [Test]
        public void ClipIn_TimelineClipがnullの場合0を返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(null, _animationClip);

            // Act & Assert
            Assert.AreEqual(0, clipInfo.ClipIn);
        }

        [Test]
        public void TimeScale_TimelineClipのtimeScaleを返す()
        {
            // Arrange
            _timelineClip.timeScale = 2.0;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act & Assert
            Assert.AreEqual(2.0, clipInfo.TimeScale, 0.0001);
        }

        [Test]
        public void TimeScale_TimelineClipがnullの場合1を返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(null, _animationClip);

            // Act & Assert
            Assert.AreEqual(1, clipInfo.TimeScale);
        }

        [Test]
        public void PreExtrapolation_TimelineClipのpreExtrapolationModeを返す()
        {
            // Arrange
            _timelineClip.preExtrapolationMode = TimelineClip.ClipExtrapolation.Hold;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act & Assert
            Assert.AreEqual(TimelineClip.ClipExtrapolation.Hold, clipInfo.PreExtrapolation);
        }

        [Test]
        public void PreExtrapolation_TimelineClipがnullの場合Noneを返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(null, _animationClip);

            // Act & Assert
            Assert.AreEqual(TimelineClip.ClipExtrapolation.None, clipInfo.PreExtrapolation);
        }

        [Test]
        public void PostExtrapolation_TimelineClipのpostExtrapolationModeを返す()
        {
            // Arrange
            _timelineClip.postExtrapolationMode = TimelineClip.ClipExtrapolation.Loop;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act & Assert
            Assert.AreEqual(TimelineClip.ClipExtrapolation.Loop, clipInfo.PostExtrapolation);
        }

        [Test]
        public void PostExtrapolation_TimelineClipがnullの場合Noneを返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(null, _animationClip);

            // Act & Assert
            Assert.AreEqual(TimelineClip.ClipExtrapolation.None, clipInfo.PostExtrapolation);
        }

        [Test]
        public void EaseInDuration_TimelineClipのeaseInDurationを返す()
        {
            // Arrange
            _timelineClip.easeInDuration = 0.25;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act & Assert
            Assert.AreEqual(0.25, clipInfo.EaseInDuration, 0.0001);
        }

        [Test]
        public void EaseInDuration_TimelineClipがnullの場合0を返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(null, _animationClip);

            // Act & Assert
            Assert.AreEqual(0, clipInfo.EaseInDuration);
        }

        [Test]
        public void EaseOutDuration_TimelineClipのeaseOutDurationを返す()
        {
            // Arrange
            _timelineClip.easeOutDuration = 0.5;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act & Assert
            Assert.AreEqual(0.5, clipInfo.EaseOutDuration, 0.0001);
        }

        [Test]
        public void EaseOutDuration_TimelineClipがnullの場合0を返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(null, _animationClip);

            // Act & Assert
            Assert.AreEqual(0, clipInfo.EaseOutDuration);
        }

        [Test]
        public void BlendInCurve_TimelineClipのmixInCurveを返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act & Assert
            // mixInCurveはTimelineClipが自動生成するため、nullでないことを確認
            Assert.IsNotNull(clipInfo.BlendInCurve);
        }

        [Test]
        public void BlendInCurve_TimelineClipがnullの場合nullを返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(null, _animationClip);

            // Act & Assert
            Assert.IsNull(clipInfo.BlendInCurve);
        }

        [Test]
        public void BlendOutCurve_TimelineClipのmixOutCurveを返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act & Assert
            // mixOutCurveはTimelineClipが自動生成するため、nullでないことを確認
            Assert.IsNotNull(clipInfo.BlendOutCurve);
        }

        [Test]
        public void BlendOutCurve_TimelineClipがnullの場合nullを返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(null, _animationClip);

            // Act & Assert
            Assert.IsNull(clipInfo.BlendOutCurve);
        }

        [Test]
        public void IsValid_TimelineClipとAnimationClipが両方存在する場合trueを返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act & Assert
            Assert.IsTrue(clipInfo.IsValid);
        }

        [Test]
        public void IsValid_TimelineClipがnullの場合falseを返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(null, _animationClip);

            // Act & Assert
            Assert.IsFalse(clipInfo.IsValid);
        }

        [Test]
        public void IsValid_AnimationClipがnullの場合falseを返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(_timelineClip, null);

            // Act & Assert
            Assert.IsFalse(clipInfo.IsValid);
        }

        [Test]
        public void IsValid_両方nullの場合falseを返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(null, null);

            // Act & Assert
            Assert.IsFalse(clipInfo.IsValid);
        }
    }
}
