using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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

        #region BlendCurveValues テスト

        [Test]
        public void BlendCurveValues_ウェイト0の場合value1を返す()
        {
            // Arrange
            float value1 = 10f;
            float value2 = 20f;

            // Act
            var result = _blendProcessor.BlendCurveValues(value1, value2, 0f);

            // Assert
            Assert.AreEqual(10f, result, 0.0001f);
        }

        [Test]
        public void BlendCurveValues_ウェイト1の場合value2を返す()
        {
            // Arrange
            float value1 = 10f;
            float value2 = 20f;

            // Act
            var result = _blendProcessor.BlendCurveValues(value1, value2, 1f);

            // Assert
            Assert.AreEqual(20f, result, 0.0001f);
        }

        [Test]
        public void BlendCurveValues_ウェイト05の場合中間値を返す()
        {
            // Arrange
            float value1 = 10f;
            float value2 = 20f;

            // Act
            var result = _blendProcessor.BlendCurveValues(value1, value2, 0.5f);

            // Assert
            Assert.AreEqual(15f, result, 0.0001f);
        }

        [Test]
        public void BlendCurveValues_ウェイト025の場合正しい補間値を返す()
        {
            // Arrange
            float value1 = 0f;
            float value2 = 100f;

            // Act
            var result = _blendProcessor.BlendCurveValues(value1, value2, 0.25f);

            // Assert
            Assert.AreEqual(25f, result, 0.0001f);
        }

        [Test]
        public void BlendCurveValues_負のウェイトは0にクランプされる()
        {
            // Arrange
            float value1 = 10f;
            float value2 = 20f;

            // Act
            var result = _blendProcessor.BlendCurveValues(value1, value2, -0.5f);

            // Assert
            Assert.AreEqual(10f, result, 0.0001f);
        }

        [Test]
        public void BlendCurveValues_1より大きいウェイトは1にクランプされる()
        {
            // Arrange
            float value1 = 10f;
            float value2 = 20f;

            // Act
            var result = _blendProcessor.BlendCurveValues(value1, value2, 1.5f);

            // Assert
            Assert.AreEqual(20f, result, 0.0001f);
        }

        [Test]
        public void BlendCurveValues_負の値でも正しく補間する()
        {
            // Arrange
            float value1 = -10f;
            float value2 = 10f;

            // Act
            var result = _blendProcessor.BlendCurveValues(value1, value2, 0.5f);

            // Assert
            Assert.AreEqual(0f, result, 0.0001f);
        }

        #endregion

        #region BlendCurveValuesAtTime テスト

        [Test]
        public void BlendCurveValuesAtTime_両方nullの場合0を返す()
        {
            // Act
            var result = _blendProcessor.BlendCurveValuesAtTime(null, null, 0.5f, 0.5f);

            // Assert
            Assert.AreEqual(0f, result, 0.0001f);
        }

        [Test]
        public void BlendCurveValuesAtTime_curve1がnullの場合curve2の値を返す()
        {
            // Arrange
            var curve2 = AnimationCurve.Linear(0, 10, 1, 20);

            // Act
            var result = _blendProcessor.BlendCurveValuesAtTime(null, curve2, 0.5f, 0.5f);

            // Assert
            Assert.AreEqual(15f, result, 0.0001f);
        }

        [Test]
        public void BlendCurveValuesAtTime_curve2がnullの場合curve1の値を返す()
        {
            // Arrange
            var curve1 = AnimationCurve.Linear(0, 10, 1, 20);

            // Act
            var result = _blendProcessor.BlendCurveValuesAtTime(curve1, null, 0.5f, 0.5f);

            // Assert
            Assert.AreEqual(15f, result, 0.0001f);
        }

        [Test]
        public void BlendCurveValuesAtTime_2つのカーブを正しく補間する()
        {
            // Arrange
            var curve1 = AnimationCurve.Constant(0, 1, 10f);  // 常に10
            var curve2 = AnimationCurve.Constant(0, 1, 20f);  // 常に20

            // Act
            var result = _blendProcessor.BlendCurveValuesAtTime(curve1, curve2, 0.5f, 0.5f);

            // Assert
            Assert.AreEqual(15f, result, 0.0001f);
        }

        [Test]
        public void BlendCurveValuesAtTime_異なる時間での評価が正しい()
        {
            // Arrange
            var curve1 = AnimationCurve.Linear(0, 0, 1, 100);  // 0〜100
            var curve2 = AnimationCurve.Linear(0, 100, 1, 0);  // 100〜0

            // Act - time=0.5でweight=0.5
            var result = _blendProcessor.BlendCurveValuesAtTime(curve1, curve2, 0.5f, 0.5f);

            // Assert
            // curve1(0.5) = 50, curve2(0.5) = 50, 補間結果 = 50
            Assert.AreEqual(50f, result, 0.0001f);
        }

        [Test]
        public void BlendCurveValuesAtTime_ウェイト0でcurve1の値を返す()
        {
            // Arrange
            var curve1 = AnimationCurve.Constant(0, 1, 10f);
            var curve2 = AnimationCurve.Constant(0, 1, 20f);

            // Act
            var result = _blendProcessor.BlendCurveValuesAtTime(curve1, curve2, 0.5f, 0f);

            // Assert
            Assert.AreEqual(10f, result, 0.0001f);
        }

        [Test]
        public void BlendCurveValuesAtTime_ウェイト1でcurve2の値を返す()
        {
            // Arrange
            var curve1 = AnimationCurve.Constant(0, 1, 10f);
            var curve2 = AnimationCurve.Constant(0, 1, 20f);

            // Act
            var result = _blendProcessor.BlendCurveValuesAtTime(curve1, curve2, 0.5f, 1f);

            // Assert
            Assert.AreEqual(20f, result, 0.0001f);
        }

        #endregion

        #region BlendVector3Values テスト

        [Test]
        public void BlendVector3Values_ウェイト0の場合value1を返す()
        {
            // Arrange
            var value1 = new Vector3(0, 0, 0);
            var value2 = new Vector3(10, 20, 30);

            // Act
            var result = _blendProcessor.BlendVector3Values(value1, value2, 0f);

            // Assert
            Assert.AreEqual(value1, result);
        }

        [Test]
        public void BlendVector3Values_ウェイト1の場合value2を返す()
        {
            // Arrange
            var value1 = new Vector3(0, 0, 0);
            var value2 = new Vector3(10, 20, 30);

            // Act
            var result = _blendProcessor.BlendVector3Values(value1, value2, 1f);

            // Assert
            Assert.AreEqual(value2, result);
        }

        [Test]
        public void BlendVector3Values_ウェイト05の場合中間値を返す()
        {
            // Arrange
            var value1 = new Vector3(0, 0, 0);
            var value2 = new Vector3(10, 20, 30);

            // Act
            var result = _blendProcessor.BlendVector3Values(value1, value2, 0.5f);

            // Assert
            Assert.AreEqual(new Vector3(5, 10, 15), result);
        }

        #endregion

        #region BlendQuaternionValues テスト

        [Test]
        public void BlendQuaternionValues_ウェイト0の場合value1を返す()
        {
            // Arrange
            var value1 = Quaternion.identity;
            var value2 = Quaternion.Euler(0, 90, 0);

            // Act
            var result = _blendProcessor.BlendQuaternionValues(value1, value2, 0f);

            // Assert
            Assert.AreEqual(value1.x, result.x, 0.0001f);
            Assert.AreEqual(value1.y, result.y, 0.0001f);
            Assert.AreEqual(value1.z, result.z, 0.0001f);
            Assert.AreEqual(value1.w, result.w, 0.0001f);
        }

        [Test]
        public void BlendQuaternionValues_ウェイト1の場合value2を返す()
        {
            // Arrange
            var value1 = Quaternion.identity;
            var value2 = Quaternion.Euler(0, 90, 0);

            // Act
            var result = _blendProcessor.BlendQuaternionValues(value1, value2, 1f);

            // Assert
            Assert.AreEqual(value2.x, result.x, 0.0001f);
            Assert.AreEqual(value2.y, result.y, 0.0001f);
            Assert.AreEqual(value2.z, result.z, 0.0001f);
            Assert.AreEqual(value2.w, result.w, 0.0001f);
        }

        [Test]
        public void BlendQuaternionValues_ウェイト05の場合中間の回転を返す()
        {
            // Arrange
            var value1 = Quaternion.identity;
            var value2 = Quaternion.Euler(0, 90, 0);

            // Act
            var result = _blendProcessor.BlendQuaternionValues(value1, value2, 0.5f);

            // Assert
            var expected = Quaternion.Euler(0, 45, 0);
            Assert.AreEqual(expected.x, result.x, 0.001f);
            Assert.AreEqual(expected.y, result.y, 0.001f);
            Assert.AreEqual(expected.z, result.z, 0.001f);
            Assert.AreEqual(expected.w, result.w, 0.001f);
        }

        #endregion

        #region DetectConsecutiveBlend テスト

        [Test]
        public void DetectConsecutiveBlend_両方nullの場合無効なブレンド情報を返す()
        {
            // Act
            var result = _blendProcessor.DetectConsecutiveBlend(null, null);

            // Assert
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void DetectConsecutiveBlend_前のクリップがnullの場合無効なブレンド情報を返す()
        {
            // Arrange
            var nextClip = new ClipInfo(_timelineClip, _animationClip);

            // Act
            var result = _blendProcessor.DetectConsecutiveBlend(null, nextClip);

            // Assert
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void DetectConsecutiveBlend_次のクリップがnullの場合無効なブレンド情報を返す()
        {
            // Arrange
            var previousClip = new ClipInfo(_timelineClip, _animationClip);

            // Act
            var result = _blendProcessor.DetectConsecutiveBlend(previousClip, null);

            // Assert
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void DetectConsecutiveBlend_クリップが重なっていない場合無効なブレンド情報を返す()
        {
            // Arrange
            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var timelineClip1 = _animationTrack.CreateClip(clip1);
            timelineClip1.start = 0;
            timelineClip1.duration = 1.0;
            timelineClip1.easeOutDuration = 0.2;

            var timelineClip2 = _animationTrack.CreateClip(clip2);
            timelineClip2.start = 2.0;  // Gap あり
            timelineClip2.duration = 1.0;
            timelineClip2.easeInDuration = 0.2;

            var previousClip = new ClipInfo(timelineClip1, clip1);
            var nextClip = new ClipInfo(timelineClip2, clip2);

            // Act
            var result = _blendProcessor.DetectConsecutiveBlend(previousClip, nextClip);

            // Assert
            Assert.IsFalse(result.IsValid);

            // Cleanup
            Object.DestroyImmediate(clip1);
            Object.DestroyImmediate(clip2);
        }

        [Test]
        public void DetectConsecutiveBlend_クリップが重なっている場合有効なブレンド情報を返す()
        {
            // Arrange
            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var timelineClip1 = _animationTrack.CreateClip(clip1);
            timelineClip1.start = 0;
            timelineClip1.duration = 2.0;
            timelineClip1.easeOutDuration = 0.5;

            var timelineClip2 = _animationTrack.CreateClip(clip2);
            timelineClip2.start = 1.5;  // 0.5秒の重なり
            timelineClip2.duration = 2.0;
            timelineClip2.easeInDuration = 0.5;

            var previousClip = new ClipInfo(timelineClip1, clip1);
            var nextClip = new ClipInfo(timelineClip2, clip2);

            // Act
            var result = _blendProcessor.DetectConsecutiveBlend(previousClip, nextClip);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(1.5, result.BlendStartTime, 0.0001);  // 次のクリップの開始時間
            Assert.AreEqual(2.0, result.BlendEndTime, 0.0001);    // 次のクリップのEaseIn終了 or 前のクリップの終了の早い方

            // Cleanup
            Object.DestroyImmediate(clip1);
            Object.DestroyImmediate(clip2);
        }

        [Test]
        public void DetectConsecutiveBlend_ブレンドカーブ情報を正しく取得する()
        {
            // Arrange
            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var timelineClip1 = _animationTrack.CreateClip(clip1);
            timelineClip1.start = 0;
            timelineClip1.duration = 2.0;
            timelineClip1.easeOutDuration = 0.5;

            var timelineClip2 = _animationTrack.CreateClip(clip2);
            timelineClip2.start = 1.5;
            timelineClip2.duration = 2.0;
            timelineClip2.easeInDuration = 0.5;

            var previousClip = new ClipInfo(timelineClip1, clip1);
            var nextClip = new ClipInfo(timelineClip2, clip2);

            // Act
            var result = _blendProcessor.DetectConsecutiveBlend(previousClip, nextClip);

            // Assert
            Assert.IsNotNull(result.PreviousClipEaseOutCurve);
            Assert.IsNotNull(result.NextClipEaseInCurve);
            Assert.AreEqual(previousClip, result.PreviousClip);
            Assert.AreEqual(nextClip, result.NextClip);

            // Cleanup
            Object.DestroyImmediate(clip1);
            Object.DestroyImmediate(clip2);
        }

        #endregion

        #region CalculateConsecutiveBlendWeights テスト

        [Test]
        public void CalculateConsecutiveBlendWeights_無効なブレンド情報の場合両方0を返す()
        {
            // Arrange
            var blendInfo = new BlendProcessor.ConsecutiveBlendInfo();

            // Act
            _blendProcessor.CalculateConsecutiveBlendWeights(blendInfo, 1.0, out float prevWeight, out float nextWeight);

            // Assert
            Assert.AreEqual(0f, prevWeight);
            Assert.AreEqual(0f, nextWeight);
        }

        [Test]
        public void CalculateConsecutiveBlendWeights_ブレンド区間外の場合両方0を返す()
        {
            // Arrange
            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var timelineClip1 = _animationTrack.CreateClip(clip1);
            timelineClip1.start = 0;
            timelineClip1.duration = 2.0;
            timelineClip1.easeOutDuration = 0.5;

            var timelineClip2 = _animationTrack.CreateClip(clip2);
            timelineClip2.start = 1.5;
            timelineClip2.duration = 2.0;
            timelineClip2.easeInDuration = 0.5;

            var previousClip = new ClipInfo(timelineClip1, clip1);
            var nextClip = new ClipInfo(timelineClip2, clip2);
            var blendInfo = _blendProcessor.DetectConsecutiveBlend(previousClip, nextClip);

            // Act - ブレンド区間より前
            _blendProcessor.CalculateConsecutiveBlendWeights(blendInfo, 0.5, out float prevWeight1, out float nextWeight1);

            // Act - ブレンド区間より後
            _blendProcessor.CalculateConsecutiveBlendWeights(blendInfo, 3.0, out float prevWeight2, out float nextWeight2);

            // Assert
            Assert.AreEqual(0f, prevWeight1);
            Assert.AreEqual(0f, nextWeight1);
            Assert.AreEqual(0f, prevWeight2);
            Assert.AreEqual(0f, nextWeight2);

            // Cleanup
            Object.DestroyImmediate(clip1);
            Object.DestroyImmediate(clip2);
        }

        [Test]
        public void CalculateConsecutiveBlendWeights_ブレンド区間内でウェイトの合計が1になる()
        {
            // Arrange
            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var timelineClip1 = _animationTrack.CreateClip(clip1);
            timelineClip1.start = 0;
            timelineClip1.duration = 2.0;
            timelineClip1.easeOutDuration = 0.5;

            var timelineClip2 = _animationTrack.CreateClip(clip2);
            timelineClip2.start = 1.5;
            timelineClip2.duration = 2.0;
            timelineClip2.easeInDuration = 0.5;

            var previousClip = new ClipInfo(timelineClip1, clip1);
            var nextClip = new ClipInfo(timelineClip2, clip2);
            var blendInfo = _blendProcessor.DetectConsecutiveBlend(previousClip, nextClip);

            // Act - ブレンド区間の中央
            _blendProcessor.CalculateConsecutiveBlendWeights(blendInfo, 1.75, out float prevWeight, out float nextWeight);

            // Assert
            Assert.AreEqual(1f, prevWeight + nextWeight, 0.0001f);
            Assert.That(prevWeight, Is.GreaterThan(0f));
            Assert.That(nextWeight, Is.GreaterThan(0f));

            // Cleanup
            Object.DestroyImmediate(clip1);
            Object.DestroyImmediate(clip2);
        }

        [Test]
        public void CalculateConsecutiveBlendWeights_ブレンド開始時は前クリップのウェイトが高い()
        {
            // Arrange
            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var timelineClip1 = _animationTrack.CreateClip(clip1);
            timelineClip1.start = 0;
            timelineClip1.duration = 2.0;
            timelineClip1.easeOutDuration = 0.5;

            var timelineClip2 = _animationTrack.CreateClip(clip2);
            timelineClip2.start = 1.5;
            timelineClip2.duration = 2.0;
            timelineClip2.easeInDuration = 0.5;

            var previousClip = new ClipInfo(timelineClip1, clip1);
            var nextClip = new ClipInfo(timelineClip2, clip2);
            var blendInfo = _blendProcessor.DetectConsecutiveBlend(previousClip, nextClip);

            // Act - ブレンド開始直後
            _blendProcessor.CalculateConsecutiveBlendWeights(blendInfo, 1.55, out float prevWeight, out float nextWeight);

            // Assert
            Assert.That(prevWeight, Is.GreaterThan(nextWeight));

            // Cleanup
            Object.DestroyImmediate(clip1);
            Object.DestroyImmediate(clip2);
        }

        [Test]
        public void CalculateConsecutiveBlendWeights_ブレンド終了時は次クリップのウェイトが高い()
        {
            // Arrange
            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var timelineClip1 = _animationTrack.CreateClip(clip1);
            timelineClip1.start = 0;
            timelineClip1.duration = 2.0;
            timelineClip1.easeOutDuration = 0.5;

            var timelineClip2 = _animationTrack.CreateClip(clip2);
            timelineClip2.start = 1.5;
            timelineClip2.duration = 2.0;
            timelineClip2.easeInDuration = 0.5;

            var previousClip = new ClipInfo(timelineClip1, clip1);
            var nextClip = new ClipInfo(timelineClip2, clip2);
            var blendInfo = _blendProcessor.DetectConsecutiveBlend(previousClip, nextClip);

            // Act - ブレンド終了直前
            _blendProcessor.CalculateConsecutiveBlendWeights(blendInfo, 1.95, out float prevWeight, out float nextWeight);

            // Assert
            Assert.That(nextWeight, Is.GreaterThan(prevWeight));

            // Cleanup
            Object.DestroyImmediate(clip1);
            Object.DestroyImmediate(clip2);
        }

        #endregion

        #region BlendConsecutiveClipValues テスト

        [Test]
        public void BlendConsecutiveClipValues_ブレンド区間内で正しく補間する()
        {
            // Arrange
            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var timelineClip1 = _animationTrack.CreateClip(clip1);
            timelineClip1.start = 0;
            timelineClip1.duration = 2.0;
            timelineClip1.easeOutDuration = 0.5;

            var timelineClip2 = _animationTrack.CreateClip(clip2);
            timelineClip2.start = 1.5;
            timelineClip2.duration = 2.0;
            timelineClip2.easeInDuration = 0.5;

            var previousClip = new ClipInfo(timelineClip1, clip1);
            var nextClip = new ClipInfo(timelineClip2, clip2);
            var blendInfo = _blendProcessor.DetectConsecutiveBlend(previousClip, nextClip);

            // Act
            var result = _blendProcessor.BlendConsecutiveClipValues(blendInfo, 1.75, 10f, 20f);

            // Assert - 補間値は10〜20の間にある
            Assert.That(result, Is.GreaterThan(10f));
            Assert.That(result, Is.LessThan(20f));

            // Cleanup
            Object.DestroyImmediate(clip1);
            Object.DestroyImmediate(clip2);
        }

        [Test]
        public void BlendConsecutiveClipValues_ブレンド区間前では前クリップの値を返す()
        {
            // Arrange
            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var timelineClip1 = _animationTrack.CreateClip(clip1);
            timelineClip1.start = 0;
            timelineClip1.duration = 2.0;
            timelineClip1.easeOutDuration = 0.5;

            var timelineClip2 = _animationTrack.CreateClip(clip2);
            timelineClip2.start = 1.5;
            timelineClip2.duration = 2.0;
            timelineClip2.easeInDuration = 0.5;

            var previousClip = new ClipInfo(timelineClip1, clip1);
            var nextClip = new ClipInfo(timelineClip2, clip2);
            var blendInfo = _blendProcessor.DetectConsecutiveBlend(previousClip, nextClip);

            // Act - ブレンド区間より前
            var result = _blendProcessor.BlendConsecutiveClipValues(blendInfo, 0.5, 10f, 20f);

            // Assert
            Assert.AreEqual(10f, result);

            // Cleanup
            Object.DestroyImmediate(clip1);
            Object.DestroyImmediate(clip2);
        }

        [Test]
        public void BlendConsecutiveClipValues_ブレンド区間後では次クリップの値を返す()
        {
            // Arrange
            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var timelineClip1 = _animationTrack.CreateClip(clip1);
            timelineClip1.start = 0;
            timelineClip1.duration = 2.0;
            timelineClip1.easeOutDuration = 0.5;

            var timelineClip2 = _animationTrack.CreateClip(clip2);
            timelineClip2.start = 1.5;
            timelineClip2.duration = 2.0;
            timelineClip2.easeInDuration = 0.5;

            var previousClip = new ClipInfo(timelineClip1, clip1);
            var nextClip = new ClipInfo(timelineClip2, clip2);
            var blendInfo = _blendProcessor.DetectConsecutiveBlend(previousClip, nextClip);

            // Act - ブレンド区間より後
            var result = _blendProcessor.BlendConsecutiveClipValues(blendInfo, 3.0, 10f, 20f);

            // Assert
            Assert.AreEqual(20f, result);

            // Cleanup
            Object.DestroyImmediate(clip1);
            Object.DestroyImmediate(clip2);
        }

        #endregion

        #region BlendConsecutiveClipVector3Values テスト

        [Test]
        public void BlendConsecutiveClipVector3Values_ブレンド区間内で正しく補間する()
        {
            // Arrange
            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var timelineClip1 = _animationTrack.CreateClip(clip1);
            timelineClip1.start = 0;
            timelineClip1.duration = 2.0;
            timelineClip1.easeOutDuration = 0.5;

            var timelineClip2 = _animationTrack.CreateClip(clip2);
            timelineClip2.start = 1.5;
            timelineClip2.duration = 2.0;
            timelineClip2.easeInDuration = 0.5;

            var previousClip = new ClipInfo(timelineClip1, clip1);
            var nextClip = new ClipInfo(timelineClip2, clip2);
            var blendInfo = _blendProcessor.DetectConsecutiveBlend(previousClip, nextClip);

            var prevValue = new Vector3(0, 0, 0);
            var nextValue = new Vector3(10, 20, 30);

            // Act
            var result = _blendProcessor.BlendConsecutiveClipVector3Values(blendInfo, 1.75, prevValue, nextValue);

            // Assert - 補間値は両値の間にある
            Assert.That(result.x, Is.GreaterThan(0f));
            Assert.That(result.x, Is.LessThan(10f));
            Assert.That(result.y, Is.GreaterThan(0f));
            Assert.That(result.y, Is.LessThan(20f));
            Assert.That(result.z, Is.GreaterThan(0f));
            Assert.That(result.z, Is.LessThan(30f));

            // Cleanup
            Object.DestroyImmediate(clip1);
            Object.DestroyImmediate(clip2);
        }

        #endregion

        #region BlendConsecutiveClipQuaternionValues テスト

        [Test]
        public void BlendConsecutiveClipQuaternionValues_ブレンド区間内で正しく補間する()
        {
            // Arrange
            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var timelineClip1 = _animationTrack.CreateClip(clip1);
            timelineClip1.start = 0;
            timelineClip1.duration = 2.0;
            timelineClip1.easeOutDuration = 0.5;

            var timelineClip2 = _animationTrack.CreateClip(clip2);
            timelineClip2.start = 1.5;
            timelineClip2.duration = 2.0;
            timelineClip2.easeInDuration = 0.5;

            var previousClip = new ClipInfo(timelineClip1, clip1);
            var nextClip = new ClipInfo(timelineClip2, clip2);
            var blendInfo = _blendProcessor.DetectConsecutiveBlend(previousClip, nextClip);

            var prevValue = Quaternion.identity;
            var nextValue = Quaternion.Euler(0, 90, 0);

            // Act
            var result = _blendProcessor.BlendConsecutiveClipQuaternionValues(blendInfo, 1.75, prevValue, nextValue);

            // Assert - 補間値は両値の間にある（0〜90度）
            var eulerResult = result.eulerAngles;
            Assert.That(eulerResult.y, Is.GreaterThan(0f).Or.EqualTo(0f).Within(0.1f));
            Assert.That(eulerResult.y, Is.LessThan(90f));

            // Cleanup
            Object.DestroyImmediate(clip1);
            Object.DestroyImmediate(clip2);
        }

        [Test]
        public void BlendConsecutiveClipQuaternionValues_ブレンド区間前では前クリップの値を返す()
        {
            // Arrange
            var clip1 = new AnimationClip { name = "Clip1" };
            var clip2 = new AnimationClip { name = "Clip2" };

            var timelineClip1 = _animationTrack.CreateClip(clip1);
            timelineClip1.start = 0;
            timelineClip1.duration = 2.0;
            timelineClip1.easeOutDuration = 0.5;

            var timelineClip2 = _animationTrack.CreateClip(clip2);
            timelineClip2.start = 1.5;
            timelineClip2.duration = 2.0;
            timelineClip2.easeInDuration = 0.5;

            var previousClip = new ClipInfo(timelineClip1, clip1);
            var nextClip = new ClipInfo(timelineClip2, clip2);
            var blendInfo = _blendProcessor.DetectConsecutiveBlend(previousClip, nextClip);

            var prevValue = Quaternion.identity;
            var nextValue = Quaternion.Euler(0, 90, 0);

            // Act
            var result = _blendProcessor.BlendConsecutiveClipQuaternionValues(blendInfo, 0.5, prevValue, nextValue);

            // Assert
            Assert.AreEqual(prevValue.x, result.x, 0.0001f);
            Assert.AreEqual(prevValue.y, result.y, 0.0001f);
            Assert.AreEqual(prevValue.z, result.z, 0.0001f);
            Assert.AreEqual(prevValue.w, result.w, 0.0001f);

            // Cleanup
            Object.DestroyImmediate(clip1);
            Object.DestroyImmediate(clip2);
        }

        #endregion
    }
}
