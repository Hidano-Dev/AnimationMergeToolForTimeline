using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using AnimationMergeTool.Editor.Domain;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// ClipMergerクラスの単体テスト
    /// </summary>
    public class ClipMergerTests
    {
        private ClipMerger _clipMerger;
        private AnimationClip _testClip;

        [SetUp]
        public void SetUp()
        {
            _clipMerger = new ClipMerger();
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

        #region GetAnimationCurves テスト

        [Test]
        public void GetAnimationCurves_nullを渡すと空のリストを返す()
        {
            // Arrange & Act
            var result = _clipMerger.GetAnimationCurves(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAnimationCurves_カーブのないクリップは空のリストを返す()
        {
            // Arrange & Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAnimationCurves_単一のカーブを持つクリップから取得できる()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            // Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("localPosition.x", result[0].Binding.propertyName);
            Assert.AreEqual(typeof(Transform), result[0].Binding.type);
            Assert.IsNotNull(result[0].Curve);
        }

        [Test]
        public void GetAnimationCurves_複数のカーブを持つクリップから全て取得できる()
        {
            // Arrange
            var curveX = AnimationCurve.Linear(0, 0, 1, 1);
            var curveY = AnimationCurve.Linear(0, 0, 1, 2);
            var curveZ = AnimationCurve.Linear(0, 0, 1, 3);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curveX);
            _testClip.SetCurve("", typeof(Transform), "localPosition.y", curveY);
            _testClip.SetCurve("", typeof(Transform), "localPosition.z", curveZ);

            // Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void GetAnimationCurves_異なるパスのカーブを取得できる()
        {
            // Arrange
            var curveRoot = AnimationCurve.Linear(0, 0, 1, 1);
            var curveChild = AnimationCurve.Linear(0, 0, 1, 2);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curveRoot);
            _testClip.SetCurve("Child/GrandChild", typeof(Transform), "localPosition.x", curveChild);

            // Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.AreEqual(2, result.Count);
            // パスが異なることを確認
            var paths = new System.Collections.Generic.HashSet<string>();
            foreach (var pair in result)
            {
                paths.Add(pair.Binding.path);
            }
            Assert.IsTrue(paths.Contains(""));
            Assert.IsTrue(paths.Contains("Child/GrandChild"));
        }

        [Test]
        public void GetAnimationCurves_EditorCurveBinding情報が正しく取得できる()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("TestPath", typeof(Transform), "localScale.x", curve);

            // Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("TestPath", result[0].Binding.path);
            Assert.AreEqual("localScale.x", result[0].Binding.propertyName);
            Assert.AreEqual(typeof(Transform), result[0].Binding.type);
        }

        [Test]
        public void GetAnimationCurves_カーブのキーフレーム情報が保持される()
        {
            // Arrange
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0f);
            curve.AddKey(0.5f, 1f);
            curve.AddKey(1f, 0.5f);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            // Act
            var result = _clipMerger.GetAnimationCurves(_testClip);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(3, result[0].Curve.keys.Length);
            Assert.AreEqual(0f, result[0].Curve.keys[0].time, 0.0001f);
            Assert.AreEqual(0.5f, result[0].Curve.keys[1].time, 0.0001f);
            Assert.AreEqual(1f, result[0].Curve.keys[2].time, 0.0001f);
        }

        #endregion

        #region GetAnimationCurve テスト

        [Test]
        public void GetAnimationCurve_nullクリップを渡すとnullを返す()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "localPosition.x");

            // Act
            var result = _clipMerger.GetAnimationCurve(null, binding);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetAnimationCurve_存在しないバインディングはnullを返す()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "localPosition.x");

            // Act
            var result = _clipMerger.GetAnimationCurve(_testClip, binding);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetAnimationCurve_存在するバインディングのカーブを取得できる()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "localPosition.x");

            // Act
            var result = _clipMerger.GetAnimationCurve(_testClip, binding);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.keys.Length);
        }

        [Test]
        public void GetAnimationCurve_正しいパスのバインディングでのみ取得できる()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            _testClip.SetCurve("CorrectPath", typeof(Transform), "localPosition.x", curve);
            var correctBinding = EditorCurveBinding.FloatCurve("CorrectPath", typeof(Transform), "localPosition.x");
            var wrongBinding = EditorCurveBinding.FloatCurve("WrongPath", typeof(Transform), "localPosition.x");

            // Act
            var correctResult = _clipMerger.GetAnimationCurve(_testClip, correctBinding);
            var wrongResult = _clipMerger.GetAnimationCurve(_testClip, wrongBinding);

            // Assert
            Assert.IsNotNull(correctResult);
            Assert.IsNull(wrongResult);
        }

        #endregion

        #region CurveBindingPair テスト

        [Test]
        public void CurveBindingPair_コンストラクタでバインディングとカーブが設定される()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("TestPath", typeof(Transform), "localPosition.x");
            var curve = AnimationCurve.Linear(0, 0, 1, 1);

            // Act
            var pair = new CurveBindingPair(binding, curve);

            // Assert
            Assert.AreEqual("TestPath", pair.Binding.path);
            Assert.AreEqual("localPosition.x", pair.Binding.propertyName);
            Assert.AreEqual(typeof(Transform), pair.Binding.type);
            Assert.IsNotNull(pair.Curve);
        }

        #endregion
    }
}
