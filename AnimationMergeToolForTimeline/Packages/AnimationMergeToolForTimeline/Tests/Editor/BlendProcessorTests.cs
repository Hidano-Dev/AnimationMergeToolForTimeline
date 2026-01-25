using NUnit.Framework;
using UnityEngine;
using UnityEngine.Timeline;
using AnimationMergeTool.Editor.Domain;
using AnimationMergeTool.Editor.Domain.Models;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// BlendProcessorクラスの単体テスト
    /// </summary>
    public class BlendProcessorTests
    {
        private BlendProcessor _blendProcessor;
        private TimelineAsset _timelineAsset;
        private AnimationTrack _animationTrack;
        private AnimationClip _animationClip;
        private TimelineClip _timelineClip;

        [SetUp]
        public void SetUp()
        {
            _blendProcessor = new BlendProcessor();

            // テスト用のTimelineAssetとAnimationTrackを作成
            _timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
            _animationTrack = _timelineAsset.CreateTrack<AnimationTrack>(null, "Test Track");

            // テスト用のAnimationClipを作成
            _animationClip = new AnimationClip();
            _animationClip.name = "Test Animation";

            // TimelineClipを作成
            _timelineClip = _animationTrack.CreateClip(_animationClip);
            _timelineClip.start = 0;
            _timelineClip.duration = 2.0;
        }

        [TearDown]
        public void TearDown()
        {
            if (_animationClip != null)
            {
                Object.DestroyImmediate(_animationClip);
            }
            if (_timelineAsset != null)
            {
                Object.DestroyImmediate(_timelineAsset);
            }
        }

        #region フレームレート関連テスト

        [Test]
        public void GetFrameRate_デフォルトで60を返す()
        {
            // Act & Assert
            Assert.AreEqual(60f, _blendProcessor.GetFrameRate());
        }

        [Test]
        public void SetFrameRate_正の値を設定できる()
        {
            // Act
            _blendProcessor.SetFrameRate(30f);

            // Assert
            Assert.AreEqual(30f, _blendProcessor.GetFrameRate());
        }

        [Test]
        public void SetFrameRate_0以下の値は無視される()
        {
            // Arrange
            _blendProcessor.SetFrameRate(30f);

            // Act
            _blendProcessor.SetFrameRate(0f);
            _blendProcessor.SetFrameRate(-10f);

            // Assert
            Assert.AreEqual(30f, _blendProcessor.GetFrameRate());
        }

        #endregion

        #region GetBlendInfo(TimelineClip) テスト

        [Test]
        public void GetBlendInfo_TimelineClip_nullの場合デフォルト値を返す()
        {
            // Act
            var blendInfo = _blendProcessor.GetBlendInfo((TimelineClip)null);

            // Assert
            Assert.IsNull(blendInfo.BlendInCurve);
            Assert.IsNull(blendInfo.BlendOutCurve);
            Assert.AreEqual(0, blendInfo.EaseInDuration);
            Assert.AreEqual(0, blendInfo.EaseOutDuration);
            Assert.IsFalse(blendInfo.IsValid);
        }

        [Test]
        public void GetBlendInfo_TimelineClip_BlendInCurveを取得できる()
        {
            // Act
            var blendInfo = _blendProcessor.GetBlendInfo(_timelineClip);

            // Assert
            // TimelineClipはデフォルトでmixInCurveを持っている
            Assert.IsNotNull(blendInfo.BlendInCurve);
        }

        [Test]
        public void GetBlendInfo_TimelineClip_BlendOutCurveを取得できる()
        {
            // Act
            var blendInfo = _blendProcessor.GetBlendInfo(_timelineClip);

            // Assert
            // TimelineClipはデフォルトでmixOutCurveを持っている
            Assert.IsNotNull(blendInfo.BlendOutCurve);
        }

        [Test]
        public void GetBlendInfo_TimelineClip_EaseInDurationを取得できる()
        {
            // Arrange
            _timelineClip.easeInDuration = 0.5;

            // Act
            var blendInfo = _blendProcessor.GetBlendInfo(_timelineClip);

            // Assert
            Assert.AreEqual(0.5, blendInfo.EaseInDuration, 0.0001);
        }

        [Test]
        public void GetBlendInfo_TimelineClip_EaseOutDurationを取得できる()
        {
            // Arrange
            _timelineClip.easeOutDuration = 0.3;

            // Act
            var blendInfo = _blendProcessor.GetBlendInfo(_timelineClip);

            // Assert
            Assert.AreEqual(0.3, blendInfo.EaseOutDuration, 0.0001);
        }

        [Test]
        public void GetBlendInfo_TimelineClip_EaseInとEaseOut両方取得できる()
        {
            // Arrange
            _timelineClip.easeInDuration = 0.25;
            _timelineClip.easeOutDuration = 0.4;

            // Act
            var blendInfo = _blendProcessor.GetBlendInfo(_timelineClip);

            // Assert
            Assert.AreEqual(0.25, blendInfo.EaseInDuration, 0.0001);
            Assert.AreEqual(0.4, blendInfo.EaseOutDuration, 0.0001);
            Assert.IsNotNull(blendInfo.BlendInCurve);
            Assert.IsNotNull(blendInfo.BlendOutCurve);
        }

        #endregion

        #region GetBlendInfo(ClipInfo) テスト

        [Test]
        public void GetBlendInfo_ClipInfo_nullの場合デフォルト値を返す()
        {
            // Act
            var blendInfo = _blendProcessor.GetBlendInfo((ClipInfo)null);

            // Assert
            Assert.IsNull(blendInfo.BlendInCurve);
            Assert.IsNull(blendInfo.BlendOutCurve);
            Assert.AreEqual(0, blendInfo.EaseInDuration);
            Assert.AreEqual(0, blendInfo.EaseOutDuration);
            Assert.IsFalse(blendInfo.IsValid);
        }

        [Test]
        public void GetBlendInfo_ClipInfo_TimelineClipがnullの場合デフォルト値を返す()
        {
            // Arrange
            var clipInfo = new ClipInfo(null, _animationClip);

            // Act
            var blendInfo = _blendProcessor.GetBlendInfo(clipInfo);

            // Assert
            Assert.IsNull(blendInfo.BlendInCurve);
            Assert.IsNull(blendInfo.BlendOutCurve);
            Assert.AreEqual(0, blendInfo.EaseInDuration);
            Assert.AreEqual(0, blendInfo.EaseOutDuration);
        }

        [Test]
        public void GetBlendInfo_ClipInfo_ブレンド情報を取得できる()
        {
            // Arrange
            _timelineClip.easeInDuration = 0.5;
            _timelineClip.easeOutDuration = 0.3;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act
            var blendInfo = _blendProcessor.GetBlendInfo(clipInfo);

            // Assert
            Assert.IsNotNull(blendInfo.BlendInCurve);
            Assert.IsNotNull(blendInfo.BlendOutCurve);
            Assert.AreEqual(0.5, blendInfo.EaseInDuration, 0.0001);
            Assert.AreEqual(0.3, blendInfo.EaseOutDuration, 0.0001);
        }

        #endregion

        #region BlendInfo構造体テスト

        [Test]
        public void BlendInfo_IsValid_カーブがある場合trueを返す()
        {
            // Arrange
            var blendInfo = _blendProcessor.GetBlendInfo(_timelineClip);

            // Assert
            Assert.IsTrue(blendInfo.IsValid);
        }

        [Test]
        public void BlendInfo_IsValid_カーブがない場合falseを返す()
        {
            // Arrange
            var blendInfo = new BlendInfo
            {
                BlendInCurve = null,
                BlendOutCurve = null,
                EaseInDuration = 0,
                EaseOutDuration = 0
            };

            // Assert
            Assert.IsFalse(blendInfo.IsValid);
        }

        [Test]
        public void BlendInfo_HasEaseIn_EaseInDurationが正でカーブがある場合trueを返す()
        {
            // Arrange
            _timelineClip.easeInDuration = 0.5;
            var blendInfo = _blendProcessor.GetBlendInfo(_timelineClip);

            // Assert
            Assert.IsTrue(blendInfo.HasEaseIn);
        }

        [Test]
        public void BlendInfo_HasEaseIn_EaseInDurationが0の場合falseを返す()
        {
            // Arrange
            _timelineClip.easeInDuration = 0;
            var blendInfo = _blendProcessor.GetBlendInfo(_timelineClip);

            // Assert
            Assert.IsFalse(blendInfo.HasEaseIn);
        }

        [Test]
        public void BlendInfo_HasEaseOut_EaseOutDurationが正でカーブがある場合trueを返す()
        {
            // Arrange
            _timelineClip.easeOutDuration = 0.3;
            var blendInfo = _blendProcessor.GetBlendInfo(_timelineClip);

            // Assert
            Assert.IsTrue(blendInfo.HasEaseOut);
        }

        [Test]
        public void BlendInfo_HasEaseOut_EaseOutDurationが0の場合falseを返す()
        {
            // Arrange
            _timelineClip.easeOutDuration = 0;
            var blendInfo = _blendProcessor.GetBlendInfo(_timelineClip);

            // Assert
            Assert.IsFalse(blendInfo.HasEaseOut);
        }

        #endregion
    }
}
