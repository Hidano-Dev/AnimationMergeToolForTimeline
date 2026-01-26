using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using AnimationMergeTool.Editor.Domain;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// BlendShapeDetectorクラスの単体テスト
    /// </summary>
    public class BlendShapeDetectorTests
    {
        private BlendShapeDetector _detector;
        private AnimationClip _testClip;

        [SetUp]
        public void SetUp()
        {
            _detector = new BlendShapeDetector();
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

        #region IsBlendShapeProperty テスト

        [Test]
        public void IsBlendShapeProperty_BlendShapeSmileはBlendShapeプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");

            // Act
            var result = _detector.IsBlendShapeProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsBlendShapeProperty_BlendShapeEyeBlinkLはBlendShapeプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Character/Face", typeof(SkinnedMeshRenderer), "blendShape.eyeBlink_L");

            // Act
            var result = _detector.IsBlendShapeProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsBlendShapeProperty_ルートオブジェクトのBlendShapeはBlendShapeプロパティ()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(SkinnedMeshRenderer), "blendShape.MouthOpen");

            // Act
            var result = _detector.IsBlendShapeProperty(binding);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsBlendShapeProperty_Transform型はBlendShapeプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(Transform), "blendShape.Smile");

            // Act
            var result = _detector.IsBlendShapeProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsBlendShapeProperty_Animator型はBlendShapeプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "blendShape.Smile");

            // Act
            var result = _detector.IsBlendShapeProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsBlendShapeProperty_BlendShapeプレフィックスなしはBlendShapeプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "m_LocalBounds.m_Center.x");

            // Act
            var result = _detector.IsBlendShapeProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsBlendShapeProperty_空のプロパティ名はBlendShapeプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "");

            // Act
            var result = _detector.IsBlendShapeProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsBlendShapeProperty_大文字小文字が異なるプレフィックスはBlendShapeプロパティではない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "BlendShape.Smile");

            // Act
            var result = _detector.IsBlendShapeProperty(binding);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region DetectBlendShapeCurves テスト

        [Test]
        public void DetectBlendShapeCurves_nullを渡すと空のリストを返す()
        {
            // Act
            var result = _detector.DetectBlendShapeCurves(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void DetectBlendShapeCurves_BlendShapeカーブのないクリップは空のリストを返す()
        {
            // Arrange
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));

            // Act
            var result = _detector.DetectBlendShapeCurves(_testClip);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void DetectBlendShapeCurves_BlendShapeカーブを検出できる()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            var curve = AnimationCurve.Linear(0, 0, 1, 100);
            AnimationUtility.SetEditorCurve(_testClip, binding, curve);

            // Act
            var result = _detector.DetectBlendShapeCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("blendShape.Smile", result[0].Binding.propertyName);
        }

        [Test]
        public void DetectBlendShapeCurves_複数のBlendShapeカーブを検出できる()
        {
            // Arrange
            var bindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            var bindingBlink = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.eyeBlink_L");
            var bindingMouth = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.MouthOpen");
            var curve = AnimationCurve.Linear(0, 0, 1, 100);
            AnimationUtility.SetEditorCurve(_testClip, bindingSmile, curve);
            AnimationUtility.SetEditorCurve(_testClip, bindingBlink, curve);
            AnimationUtility.SetEditorCurve(_testClip, bindingMouth, curve);

            // Act
            var result = _detector.DetectBlendShapeCurves(_testClip);

            // Assert
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void DetectBlendShapeCurves_BlendShapeと通常のカーブが混在する場合BlendShapeのみ検出()
        {
            // Arrange
            var bindingBlendShape = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            var curve = AnimationCurve.Linear(0, 0, 1, 100);
            AnimationUtility.SetEditorCurve(_testClip, bindingBlendShape, curve);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            _testClip.SetCurve("Child", typeof(Transform), "localPosition.y", curve);

            // Act
            var result = _detector.DetectBlendShapeCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("blendShape.Smile", result[0].Binding.propertyName);
        }

        [Test]
        public void DetectBlendShapeCurves_BlendShapeとマッスルカーブが混在する場合BlendShapeのみ検出()
        {
            // Arrange
            var bindingBlendShape = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            var bindingMuscle = EditorCurveBinding.FloatCurve("", typeof(Animator), "Spine Front-Back");
            var curve = AnimationCurve.Linear(0, 0, 1, 100);
            AnimationUtility.SetEditorCurve(_testClip, bindingBlendShape, curve);
            AnimationUtility.SetEditorCurve(_testClip, bindingMuscle, curve);

            // Act
            var result = _detector.DetectBlendShapeCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("blendShape.Smile", result[0].Binding.propertyName);
        }

        #endregion

        #region HasBlendShapeCurves テスト

        [Test]
        public void HasBlendShapeCurves_nullを渡すとfalseを返す()
        {
            // Act
            var result = _detector.HasBlendShapeCurves(null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void HasBlendShapeCurves_BlendShapeカーブがない場合falseを返す()
        {
            // Arrange
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));

            // Act
            var result = _detector.HasBlendShapeCurves(_testClip);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void HasBlendShapeCurves_BlendShapeカーブがある場合trueを返す()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            var curve = AnimationCurve.Linear(0, 0, 1, 100);
            AnimationUtility.SetEditorCurve(_testClip, binding, curve);

            // Act
            var result = _detector.HasBlendShapeCurves(_testClip);

            // Assert
            Assert.IsTrue(result);
        }

        #endregion

        #region GetBlendShapeName テスト

        [Test]
        public void GetBlendShapeName_BlendShapeプロパティから名前を取得できる()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");

            // Act
            var result = _detector.GetBlendShapeName(binding);

            // Assert
            Assert.AreEqual("Smile", result);
        }

        [Test]
        public void GetBlendShapeName_アンダースコアを含むBlendShape名を取得できる()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.eyeBlink_L");

            // Act
            var result = _detector.GetBlendShapeName(binding);

            // Assert
            Assert.AreEqual("eyeBlink_L", result);
        }

        [Test]
        public void GetBlendShapeName_BlendShapeプロパティでない場合nullを返す()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "localPosition.x");

            // Act
            var result = _detector.GetBlendShapeName(binding);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetBlendShapeName_型が異なる場合nullを返す()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(Animator), "blendShape.Smile");

            // Act
            var result = _detector.GetBlendShapeName(binding);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region 定数テスト

        [Test]
        public void BlendShapePrefix_正しい値が定義されている()
        {
            // Assert
            Assert.AreEqual("blendShape.", BlendShapeDetector.BlendShapePrefix);
        }

        #endregion
    }
}
