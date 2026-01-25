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

        #region CalculateBlendWeight(ClipInfo, double) テスト

        [Test]
        public void CalculateBlendWeight_ClipInfo_nullの場合0を返す()
        {
            // Act
            var weight = _blendProcessor.CalculateBlendWeight((ClipInfo)null, 1.0);

            // Assert
            Assert.AreEqual(0f, weight);
        }

        [Test]
        public void CalculateBlendWeight_ClipInfo_クリップ範囲外の場合0を返す()
        {
            // Arrange
            _timelineClip.start = 1.0;
            _timelineClip.duration = 2.0;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act & Assert
            Assert.AreEqual(0f, _blendProcessor.CalculateBlendWeight(clipInfo, 0.5)); // 開始前
            Assert.AreEqual(0f, _blendProcessor.CalculateBlendWeight(clipInfo, 3.5)); // 終了後
        }

        [Test]
        public void CalculateBlendWeight_ClipInfo_EaseInなしEaseOutなしの場合1を返す()
        {
            // Arrange
            _timelineClip.start = 0;
            _timelineClip.duration = 2.0;
            _timelineClip.easeInDuration = 0;
            _timelineClip.easeOutDuration = 0;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act
            var weight = _blendProcessor.CalculateBlendWeight(clipInfo, 1.0);

            // Assert
            Assert.AreEqual(1f, weight);
        }

        [Test]
        public void CalculateBlendWeight_ClipInfo_EaseIn区間内で0から1に変化する()
        {
            // Arrange
            _timelineClip.start = 0;
            _timelineClip.duration = 2.0;
            _timelineClip.easeInDuration = 0.5;
            _timelineClip.easeOutDuration = 0;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act
            var weightAtStart = _blendProcessor.CalculateBlendWeight(clipInfo, 0.0);
            var weightAtMiddle = _blendProcessor.CalculateBlendWeight(clipInfo, 0.25);
            var weightAtEnd = _blendProcessor.CalculateBlendWeight(clipInfo, 0.5);

            // Assert
            // EaseIn開始時は低いウェイト、終了時は高いウェイト
            Assert.That(weightAtStart, Is.LessThanOrEqualTo(weightAtMiddle));
            Assert.That(weightAtMiddle, Is.LessThanOrEqualTo(weightAtEnd));
            // EaseIn終了後は1に近い値
            Assert.That(weightAtEnd, Is.GreaterThanOrEqualTo(0.9f));
        }

        [Test]
        public void CalculateBlendWeight_ClipInfo_EaseOut区間内で1から0に変化する()
        {
            // Arrange
            _timelineClip.start = 0;
            _timelineClip.duration = 2.0;
            _timelineClip.easeInDuration = 0;
            _timelineClip.easeOutDuration = 0.5;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act
            var weightAtStart = _blendProcessor.CalculateBlendWeight(clipInfo, 1.5);  // EaseOut開始時
            var weightAtMiddle = _blendProcessor.CalculateBlendWeight(clipInfo, 1.75);
            var weightAtEnd = _blendProcessor.CalculateBlendWeight(clipInfo, 2.0);

            // Assert
            // EaseOut開始時は高いウェイト、終了時は低いウェイト
            Assert.That(weightAtStart, Is.GreaterThanOrEqualTo(weightAtMiddle));
            Assert.That(weightAtMiddle, Is.GreaterThanOrEqualTo(weightAtEnd));
        }

        [Test]
        public void CalculateBlendWeight_ClipInfo_EaseIn区間とEaseOut区間が重なる場合両方のウェイトが乗算される()
        {
            // Arrange
            _timelineClip.start = 0;
            _timelineClip.duration = 1.0;
            _timelineClip.easeInDuration = 0.6;  // 重なり区間あり
            _timelineClip.easeOutDuration = 0.6;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act
            var weightAtCenter = _blendProcessor.CalculateBlendWeight(clipInfo, 0.5);

            // Assert
            // 重なり区間では両方のウェイトが乗算されるため1未満になる
            Assert.That(weightAtCenter, Is.LessThan(1f));
            Assert.That(weightAtCenter, Is.GreaterThan(0f));
        }

        [Test]
        public void CalculateBlendWeight_ClipInfo_中間区間では1を返す()
        {
            // Arrange
            _timelineClip.start = 0;
            _timelineClip.duration = 3.0;
            _timelineClip.easeInDuration = 0.5;
            _timelineClip.easeOutDuration = 0.5;
            var clipInfo = new ClipInfo(_timelineClip, _animationClip);

            // Act
            var weight = _blendProcessor.CalculateBlendWeight(clipInfo, 1.5);  // 中間

            // Assert
            Assert.AreEqual(1f, weight);
        }

        #endregion

        #region CalculateBlendWeight(BlendInfo, double, double) テスト

        [Test]
        public void CalculateBlendWeight_BlendInfo_clipDurationが0以下の場合0を返す()
        {
            // Arrange
            var blendInfo = new BlendInfo();

            // Act & Assert
            Assert.AreEqual(0f, _blendProcessor.CalculateBlendWeight(blendInfo, 0.5, 0));
            Assert.AreEqual(0f, _blendProcessor.CalculateBlendWeight(blendInfo, 0.5, -1));
        }

        [Test]
        public void CalculateBlendWeight_BlendInfo_ローカル時間が範囲外の場合0を返す()
        {
            // Arrange
            var blendInfo = new BlendInfo();

            // Act & Assert
            Assert.AreEqual(0f, _blendProcessor.CalculateBlendWeight(blendInfo, -0.1, 2.0));
            Assert.AreEqual(0f, _blendProcessor.CalculateBlendWeight(blendInfo, 2.1, 2.0));
        }

        [Test]
        public void CalculateBlendWeight_BlendInfo_EaseInなしEaseOutなしの場合1を返す()
        {
            // Arrange
            var blendInfo = new BlendInfo
            {
                BlendInCurve = null,
                BlendOutCurve = null,
                EaseInDuration = 0,
                EaseOutDuration = 0
            };

            // Act
            var weight = _blendProcessor.CalculateBlendWeight(blendInfo, 1.0, 2.0);

            // Assert
            Assert.AreEqual(1f, weight);
        }

        [Test]
        public void CalculateBlendWeight_BlendInfo_EaseIn区間で正しく計算する()
        {
            // Arrange
            _timelineClip.easeInDuration = 0.5;
            _timelineClip.easeOutDuration = 0;
            var blendInfo = _blendProcessor.GetBlendInfo(_timelineClip);

            // Act
            var weightAtStart = _blendProcessor.CalculateBlendWeight(blendInfo, 0.0, 2.0);
            var weightAtMiddle = _blendProcessor.CalculateBlendWeight(blendInfo, 0.25, 2.0);
            var weightAfterEaseIn = _blendProcessor.CalculateBlendWeight(blendInfo, 1.0, 2.0);

            // Assert
            Assert.That(weightAtStart, Is.LessThanOrEqualTo(weightAtMiddle));
            Assert.AreEqual(1f, weightAfterEaseIn);
        }

        [Test]
        public void CalculateBlendWeight_BlendInfo_EaseOut区間で正しく計算する()
        {
            // Arrange
            _timelineClip.easeInDuration = 0;
            _timelineClip.easeOutDuration = 0.5;
            var blendInfo = _blendProcessor.GetBlendInfo(_timelineClip);

            // Act
            var weightBeforeEaseOut = _blendProcessor.CalculateBlendWeight(blendInfo, 1.0, 2.0);
            var weightAtEaseOutStart = _blendProcessor.CalculateBlendWeight(blendInfo, 1.5, 2.0);
            var weightAtEnd = _blendProcessor.CalculateBlendWeight(blendInfo, 2.0, 2.0);

            // Assert
            Assert.AreEqual(1f, weightBeforeEaseOut);
            Assert.That(weightAtEaseOutStart, Is.GreaterThanOrEqualTo(weightAtEnd));
        }

        #endregion
    }
}
