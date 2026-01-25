using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using AnimationMergeTool.Editor.Domain;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// RootMotionDetectorクラスの単体テスト
    /// </summary>
    public class RootMotionDetectorTests
    {
        private RootMotionDetector _detector;
        private AnimationClip _testClip;

        [SetUp]
        public void SetUp()
        {
            _detector = new RootMotionDetector();
            _testClip = new AnimationClip();
            _testClip.name = "Test Animation";
        }

        [TearDown]
        public void TearDown()
        {
            if (_testClip != null)
            {
                Object.DestroyImmediate(_testClip);
            }
        }

        #region IsRootMotionProperty テスト

        [Test]
        public void IsRootMotionProperty_RootTxはルートモーションプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");

            // Act
            var result = _detector.IsRootMotionProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsRootMotionProperty_RootTyはルートモーションプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.y");

            // Act
            var result = _detector.IsRootMotionProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsRootMotionProperty_RootTzはルートモーションプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.z");

            // Act
            var result = _detector.IsRootMotionProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsRootMotionProperty_RootQxはルートモーションプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.x");

            // Act
            var result = _detector.IsRootMotionProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsRootMotionProperty_RootQyはルートモーションプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.y");

            // Act
            var result = _detector.IsRootMotionProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsRootMotionProperty_RootQzはルートモーションプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.z");

            // Act
            var result = _detector.IsRootMotionProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsRootMotionProperty_RootQwはルートモーションプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.w");

            // Act
            var result = _detector.IsRootMotionProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsRootMotionProperty_localPositionはルートモーションプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "localPosition.x");

            // Act
            var result = _detector.IsRootMotionProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsRootMotionProperty_localRotationはルートモーションプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "localRotation.x");

            // Act
            var result = _detector.IsRootMotionProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsRootMotionProperty_子オブジェクトのRootTはルートモーションプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Child", typeof(Animator), "RootT.x");

            // Act
            var result = _detector.IsRootMotionProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsRootMotionProperty_空のプロパティ名はルートモーションプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "");

            // Act
            var result = _detector.IsRootMotionProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region IsRootPositionProperty テスト

        [Test]
        public void IsRootPositionProperty_RootTxは位置プロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");

            // Act
            var result = _detector.IsRootPositionProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsRootPositionProperty_RootQxは位置プロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.x");

            // Act
            var result = _detector.IsRootPositionProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region IsRootRotationProperty テスト

        [Test]
        public void IsRootRotationProperty_RootQxは回転プロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.x");

            // Act
            var result = _detector.IsRootRotationProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsRootRotationProperty_RootTxは回転プロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");

            // Act
            var result = _detector.IsRootRotationProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region DetectRootMotionCurves テスト

        [Test]
        public void DetectRootMotionCurves_nullを渡すと空のリストを返す()
        {
            // Act
            var result = _detector.DetectRootMotionCurves(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void DetectRootMotionCurves_ルートモーションカーブのないクリップは空のリストを返す()
        {
            // Arrange
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));

            // Act
            var result = _detector.DetectRootMotionCurves(_testClip);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void DetectRootMotionCurves_RootTカーブを検出できる()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, binding, curve);

            // Act
            var result = _detector.DetectRootMotionCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("RootT.x", result[0].Binding.propertyName);
        }

        [Test]
        public void DetectRootMotionCurves_RootQカーブを検出できる()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.x");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, binding, curve);

            // Act
            var result = _detector.DetectRootMotionCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("RootQ.x", result[0].Binding.propertyName);
        }

        [Test]
        public void DetectRootMotionCurves_複数のルートモーションカーブを検出できる()
        {
            // Arrange
            var bindingTx = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");
            var bindingTy = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.y");
            var bindingQx = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.x");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, bindingTx, curve);
            AnimationUtility.SetEditorCurve(_testClip, bindingTy, curve);
            AnimationUtility.SetEditorCurve(_testClip, bindingQx, curve);

            // Act
            var result = _detector.DetectRootMotionCurves(_testClip);

            // Assert
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void DetectRootMotionCurves_ルートモーションと通常のカーブが混在する場合ルートモーションのみ検出()
        {
            // Arrange
            var bindingRootT = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, bindingRootT, curve);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            _testClip.SetCurve("Child", typeof(Transform), "localPosition.y", curve);

            // Act
            var result = _detector.DetectRootMotionCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("RootT.x", result[0].Binding.propertyName);
        }

        #endregion

        #region HasRootMotionCurves テスト

        [Test]
        public void HasRootMotionCurves_nullを渡すとfalseを返す()
        {
            // Act
            var result = _detector.HasRootMotionCurves(null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void HasRootMotionCurves_ルートモーションカーブがない場合falseを返す()
        {
            // Arrange
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));

            // Act
            var result = _detector.HasRootMotionCurves(_testClip);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void HasRootMotionCurves_ルートモーションカーブがある場合trueを返す()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, binding, curve);

            // Act
            var result = _detector.HasRootMotionCurves(_testClip);

            // Assert
            Assert.IsTrue(result);
        }

        #endregion

        #region DetectRootPositionCurves テスト

        [Test]
        public void DetectRootPositionCurves_nullを渡すと空のリストを返す()
        {
            // Act
            var result = _detector.DetectRootPositionCurves(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void DetectRootPositionCurves_RootTカーブのみ検出する()
        {
            // Arrange
            var bindingT = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");
            var bindingQ = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.x");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, bindingT, curve);
            AnimationUtility.SetEditorCurve(_testClip, bindingQ, curve);

            // Act
            var result = _detector.DetectRootPositionCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("RootT.x", result[0].Binding.propertyName);
        }

        #endregion

        #region DetectRootRotationCurves テスト

        [Test]
        public void DetectRootRotationCurves_nullを渡すと空のリストを返す()
        {
            // Act
            var result = _detector.DetectRootRotationCurves(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void DetectRootRotationCurves_RootQカーブのみ検出する()
        {
            // Arrange
            var bindingT = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");
            var bindingQ = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.x");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, bindingT, curve);
            AnimationUtility.SetEditorCurve(_testClip, bindingQ, curve);

            // Act
            var result = _detector.DetectRootRotationCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("RootQ.x", result[0].Binding.propertyName);
        }

        #endregion
    }
}
