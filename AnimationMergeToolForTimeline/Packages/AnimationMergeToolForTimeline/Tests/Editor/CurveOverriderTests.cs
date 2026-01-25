using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using AnimationMergeTool.Editor.Domain;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// CurveOverriderクラスの単体テスト
    /// </summary>
    public class CurveOverriderTests
    {
        private CurveOverrider _curveOverrider;

        [SetUp]
        public void SetUp()
        {
            _curveOverrider = new CurveOverrider();
        }

        #region IsSameProperty テスト

        [Test]
        public void IsSameProperty_同一のバインディングの場合trueを返す()
        {
            // Arrange
            var binding1 = new EditorCurveBinding
            {
                path = "Child/Grandchild",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };
            var binding2 = new EditorCurveBinding
            {
                path = "Child/Grandchild",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };

            // Act
            var result = _curveOverrider.IsSameProperty(binding1, binding2);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsSameProperty_pathが異なる場合falseを返す()
        {
            // Arrange
            var binding1 = new EditorCurveBinding
            {
                path = "Child/Grandchild",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };
            var binding2 = new EditorCurveBinding
            {
                path = "Child/Other",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };

            // Act
            var result = _curveOverrider.IsSameProperty(binding1, binding2);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsSameProperty_typeが異なる場合falseを返す()
        {
            // Arrange
            var binding1 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };
            var binding2 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(MeshRenderer),
                propertyName = "m_LocalPosition.x"
            };

            // Act
            var result = _curveOverrider.IsSameProperty(binding1, binding2);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsSameProperty_propertyNameが異なる場合falseを返す()
        {
            // Arrange
            var binding1 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };
            var binding2 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.y"
            };

            // Act
            var result = _curveOverrider.IsSameProperty(binding1, binding2);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsSameProperty_空のパスでも正しく比較できる()
        {
            // Arrange
            var binding1 = new EditorCurveBinding
            {
                path = "",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };
            var binding2 = new EditorCurveBinding
            {
                path = "",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };

            // Act
            var result = _curveOverrider.IsSameProperty(binding1, binding2);

            // Assert
            Assert.IsTrue(result);
        }

        #endregion

        #region GetBindingKey テスト

        [Test]
        public void GetBindingKey_正しい形式のキーを生成する()
        {
            // Arrange
            var binding = new EditorCurveBinding
            {
                path = "Child/Grandchild",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };

            // Act
            var key = _curveOverrider.GetBindingKey(binding);

            // Assert
            Assert.AreEqual("Child/Grandchild|UnityEngine.Transform|m_LocalPosition.x", key);
        }

        [Test]
        public void GetBindingKey_同一バインディングは同じキーを生成する()
        {
            // Arrange
            var binding1 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };
            var binding2 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };

            // Act
            var key1 = _curveOverrider.GetBindingKey(binding1);
            var key2 = _curveOverrider.GetBindingKey(binding2);

            // Assert
            Assert.AreEqual(key1, key2);
        }

        [Test]
        public void GetBindingKey_異なるバインディングは異なるキーを生成する()
        {
            // Arrange
            var binding1 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };
            var binding2 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.y"
            };

            // Act
            var key1 = _curveOverrider.GetBindingKey(binding1);
            var key2 = _curveOverrider.GetBindingKey(binding2);

            // Assert
            Assert.AreNotEqual(key1, key2);
        }

        [Test]
        public void GetBindingKey_空のパスでもキーを生成できる()
        {
            // Arrange
            var binding = new EditorCurveBinding
            {
                path = "",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };

            // Act
            var key = _curveOverrider.GetBindingKey(binding);

            // Assert
            Assert.AreEqual("|UnityEngine.Transform|m_LocalPosition.x", key);
        }

        #endregion

        #region DetectOverlappingProperties テスト

        [Test]
        public void DetectOverlappingProperties_重複するプロパティを検出できる()
        {
            // Arrange
            var binding1 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };
            var binding2 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.y"
            };
            var curve = AnimationCurve.Linear(0, 0, 1, 1);

            var lowerPriorityPairs = new System.Collections.Generic.List<CurveBindingPair>
            {
                new CurveBindingPair(binding1, curve),
                new CurveBindingPair(binding2, curve)
            };

            var higherPriorityPairs = new System.Collections.Generic.List<CurveBindingPair>
            {
                new CurveBindingPair(binding1, curve)  // binding1と重複
            };

            // Act
            var overlapping = _curveOverrider.DetectOverlappingProperties(lowerPriorityPairs, higherPriorityPairs);

            // Assert
            Assert.AreEqual(1, overlapping.Count);
            Assert.IsTrue(overlapping.Contains(_curveOverrider.GetBindingKey(binding1)));
            Assert.IsFalse(overlapping.Contains(_curveOverrider.GetBindingKey(binding2)));
        }

        [Test]
        public void DetectOverlappingProperties_重複がない場合空のセットを返す()
        {
            // Arrange
            var binding1 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };
            var binding2 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.y"
            };
            var curve = AnimationCurve.Linear(0, 0, 1, 1);

            var lowerPriorityPairs = new System.Collections.Generic.List<CurveBindingPair>
            {
                new CurveBindingPair(binding1, curve)
            };

            var higherPriorityPairs = new System.Collections.Generic.List<CurveBindingPair>
            {
                new CurveBindingPair(binding2, curve)  // 異なるプロパティ
            };

            // Act
            var overlapping = _curveOverrider.DetectOverlappingProperties(lowerPriorityPairs, higherPriorityPairs);

            // Assert
            Assert.AreEqual(0, overlapping.Count);
        }

        [Test]
        public void DetectOverlappingProperties_lowerPriorityPairsがnullの場合空のセットを返す()
        {
            // Arrange
            var binding = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };
            var curve = AnimationCurve.Linear(0, 0, 1, 1);

            var higherPriorityPairs = new System.Collections.Generic.List<CurveBindingPair>
            {
                new CurveBindingPair(binding, curve)
            };

            // Act
            var overlapping = _curveOverrider.DetectOverlappingProperties(null, higherPriorityPairs);

            // Assert
            Assert.AreEqual(0, overlapping.Count);
        }

        [Test]
        public void DetectOverlappingProperties_higherPriorityPairsがnullの場合空のセットを返す()
        {
            // Arrange
            var binding = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };
            var curve = AnimationCurve.Linear(0, 0, 1, 1);

            var lowerPriorityPairs = new System.Collections.Generic.List<CurveBindingPair>
            {
                new CurveBindingPair(binding, curve)
            };

            // Act
            var overlapping = _curveOverrider.DetectOverlappingProperties(lowerPriorityPairs, null);

            // Assert
            Assert.AreEqual(0, overlapping.Count);
        }

        [Test]
        public void DetectOverlappingProperties_複数の重複を検出できる()
        {
            // Arrange
            var binding1 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };
            var binding2 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.y"
            };
            var binding3 = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.z"
            };
            var curve = AnimationCurve.Linear(0, 0, 1, 1);

            var lowerPriorityPairs = new System.Collections.Generic.List<CurveBindingPair>
            {
                new CurveBindingPair(binding1, curve),
                new CurveBindingPair(binding2, curve),
                new CurveBindingPair(binding3, curve)
            };

            var higherPriorityPairs = new System.Collections.Generic.List<CurveBindingPair>
            {
                new CurveBindingPair(binding1, curve),
                new CurveBindingPair(binding3, curve)
            };

            // Act
            var overlapping = _curveOverrider.DetectOverlappingProperties(lowerPriorityPairs, higherPriorityPairs);

            // Assert
            Assert.AreEqual(2, overlapping.Count);
            Assert.IsTrue(overlapping.Contains(_curveOverrider.GetBindingKey(binding1)));
            Assert.IsTrue(overlapping.Contains(_curveOverrider.GetBindingKey(binding3)));
            Assert.IsFalse(overlapping.Contains(_curveOverrider.GetBindingKey(binding2)));
        }

        #endregion

        #region CurveBindingPair テスト

        [Test]
        public void CurveBindingPair_コンストラクタで値が正しく設定される()
        {
            // Arrange
            var binding = new EditorCurveBinding
            {
                path = "Child",
                type = typeof(Transform),
                propertyName = "m_LocalPosition.x"
            };
            var curve = AnimationCurve.Linear(0, 0, 1, 1);

            // Act
            var pair = new CurveBindingPair(binding, curve);

            // Assert
            Assert.AreEqual(binding.path, pair.Binding.path);
            Assert.AreEqual(binding.type, pair.Binding.type);
            Assert.AreEqual(binding.propertyName, pair.Binding.propertyName);
            Assert.AreEqual(curve, pair.Curve);
        }

        #endregion
    }
}
