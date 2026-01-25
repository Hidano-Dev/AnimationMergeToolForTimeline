using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using AnimationMergeTool.Editor.Domain;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// MuscleDetectorクラスの単体テスト
    /// </summary>
    public class MuscleDetectorTests
    {
        private MuscleDetector _detector;
        private AnimationClip _testClip;

        [SetUp]
        public void SetUp()
        {
            _detector = new MuscleDetector();
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

        #region IsMuscleProperty テスト

        [Test]
        public void IsMuscleProperty_SpineFrontBackはマッスルプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "Spine Front-Back");

            // Act
            var result = _detector.IsMuscleProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsMuscleProperty_ChestLeftRightはマッスルプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "Chest Left-Right");

            // Act
            var result = _detector.IsMuscleProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsMuscleProperty_HeadNodDownUpはマッスルプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "Head Nod Down-Up");

            // Act
            var result = _detector.IsMuscleProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsMuscleProperty_LeftArmDownUpはマッスルプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "Left Arm Down-Up");

            // Act
            var result = _detector.IsMuscleProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsMuscleProperty_RightArmDownUpはマッスルプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "Right Arm Down-Up");

            // Act
            var result = _detector.IsMuscleProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsMuscleProperty_LeftUpperLegFrontBackはマッスルプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "Left Upper Leg Front-Back");

            // Act
            var result = _detector.IsMuscleProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsMuscleProperty_LeftHandThumb1Stretchedはマッスルプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "LeftHand.Thumb.1 Stretched");

            // Act
            var result = _detector.IsMuscleProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsMuscleProperty_RightHandIndexSpreadはマッスルプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RightHand.Index.Spread");

            // Act
            var result = _detector.IsMuscleProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsMuscleProperty_localPositionはマッスルプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "localPosition.x");

            // Act
            var result = _detector.IsMuscleProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsMuscleProperty_RootTはマッスルプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");

            // Act
            var result = _detector.IsMuscleProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsMuscleProperty_RootQはマッスルプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.x");

            // Act
            var result = _detector.IsMuscleProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsMuscleProperty_子オブジェクトのマッスルプロパティはマッスルプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Child", typeof(Animator), "Spine Front-Back");

            // Act
            var result = _detector.IsMuscleProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsMuscleProperty_空のプロパティ名はマッスルプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "");

            // Act
            var result = _detector.IsMuscleProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsMuscleProperty_存在しないプロパティ名はマッスルプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "Unknown Muscle Property");

            // Act
            var result = _detector.IsMuscleProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region DetectMuscleCurves テスト

        [Test]
        public void DetectMuscleCurves_nullを渡すと空のリストを返す()
        {
            // Act
            var result = _detector.DetectMuscleCurves(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void DetectMuscleCurves_マッスルカーブのないクリップは空のリストを返す()
        {
            // Arrange
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));

            // Act
            var result = _detector.DetectMuscleCurves(_testClip);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void DetectMuscleCurves_マッスルカーブを検出できる()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "Spine Front-Back");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, binding, curve);

            // Act
            var result = _detector.DetectMuscleCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Spine Front-Back", result[0].Binding.propertyName);
        }

        [Test]
        public void DetectMuscleCurves_複数のマッスルカーブを検出できる()
        {
            // Arrange
            var bindingSpine = EditorCurveBinding.FloatCurve("", typeof(Animator), "Spine Front-Back");
            var bindingChest = EditorCurveBinding.FloatCurve("", typeof(Animator), "Chest Front-Back");
            var bindingHead = EditorCurveBinding.FloatCurve("", typeof(Animator), "Head Nod Down-Up");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, bindingSpine, curve);
            AnimationUtility.SetEditorCurve(_testClip, bindingChest, curve);
            AnimationUtility.SetEditorCurve(_testClip, bindingHead, curve);

            // Act
            var result = _detector.DetectMuscleCurves(_testClip);

            // Assert
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void DetectMuscleCurves_マッスルと通常のカーブが混在する場合マッスルのみ検出()
        {
            // Arrange
            var bindingMuscle = EditorCurveBinding.FloatCurve("", typeof(Animator), "Spine Front-Back");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, bindingMuscle, curve);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            _testClip.SetCurve("Child", typeof(Transform), "localPosition.y", curve);

            // Act
            var result = _detector.DetectMuscleCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Spine Front-Back", result[0].Binding.propertyName);
        }

        [Test]
        public void DetectMuscleCurves_マッスルとルートモーションが混在する場合マッスルのみ検出()
        {
            // Arrange
            var bindingMuscle = EditorCurveBinding.FloatCurve("", typeof(Animator), "Left Arm Down-Up");
            var bindingRootT = EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, bindingMuscle, curve);
            AnimationUtility.SetEditorCurve(_testClip, bindingRootT, curve);

            // Act
            var result = _detector.DetectMuscleCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Left Arm Down-Up", result[0].Binding.propertyName);
        }

        #endregion

        #region HasMuscleCurves テスト

        [Test]
        public void HasMuscleCurves_nullを渡すとfalseを返す()
        {
            // Act
            var result = _detector.HasMuscleCurves(null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void HasMuscleCurves_マッスルカーブがない場合falseを返す()
        {
            // Arrange
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));

            // Act
            var result = _detector.HasMuscleCurves(_testClip);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void HasMuscleCurves_マッスルカーブがある場合trueを返す()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "Spine Front-Back");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            AnimationUtility.SetEditorCurve(_testClip, binding, curve);

            // Act
            var result = _detector.HasMuscleCurves(_testClip);

            // Assert
            Assert.IsTrue(result);
        }

        #endregion

        #region GetAllMusclePropertyNames テスト

        [Test]
        public void GetAllMusclePropertyNames_マッスルプロパティ名の配列を返す()
        {
            // Act
            var result = MuscleDetector.GetAllMusclePropertyNames();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [Test]
        public void GetAllMusclePropertyNames_配列には主要なマッスルプロパティが含まれる()
        {
            // Act
            var result = MuscleDetector.GetAllMusclePropertyNames();

            // Assert
            Assert.Contains("Spine Front-Back", result);
            Assert.Contains("Chest Front-Back", result);
            Assert.Contains("Head Nod Down-Up", result);
            Assert.Contains("Left Arm Down-Up", result);
            Assert.Contains("Right Arm Down-Up", result);
        }

        #endregion
    }
}
