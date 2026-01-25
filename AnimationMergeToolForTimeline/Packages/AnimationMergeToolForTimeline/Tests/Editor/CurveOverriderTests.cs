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

        #region ApplyFullOverride テスト

        [Test]
        public void ApplyFullOverride_高優先順位カーブで完全にカバーする場合_高優先順位カーブを返す()
        {
            // Arrange
            // 低優先順位: 0秒から2秒まで
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(2f, 2f);

            // 高優先順位: 0秒から3秒まで（低優先順位を完全にカバー）
            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(0f, 10f);
            higherCurve.AddKey(3f, 30f);

            // Act
            var result = _curveOverrider.ApplyFullOverride(lowerCurve, higherCurve, 0f, 3f);

            // Assert
            Assert.AreEqual(2, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].time);
            Assert.AreEqual(10f, result.keys[0].value);
            Assert.AreEqual(3f, result.keys[1].time);
            Assert.AreEqual(30f, result.keys[1].value);
        }

        [Test]
        public void ApplyFullOverride_高優先順位カーブがnullの場合_低優先順位カーブを返す()
        {
            // Arrange
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(1f, 1f);

            // Act
            var result = _curveOverrider.ApplyFullOverride(lowerCurve, null, 0f, 1f);

            // Assert
            Assert.AreEqual(2, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].value);
            Assert.AreEqual(1f, result.keys[1].value);
        }

        [Test]
        public void ApplyFullOverride_低優先順位カーブがnullの場合_高優先順位カーブを返す()
        {
            // Arrange
            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(0f, 10f);
            higherCurve.AddKey(1f, 20f);

            // Act
            var result = _curveOverrider.ApplyFullOverride(null, higherCurve, 0f, 1f);

            // Assert
            Assert.AreEqual(2, result.keys.Length);
            Assert.AreEqual(10f, result.keys[0].value);
            Assert.AreEqual(20f, result.keys[1].value);
        }

        [Test]
        public void ApplyFullOverride_両方のカーブがnullの場合_空のカーブを返す()
        {
            // Act
            var result = _curveOverrider.ApplyFullOverride(null, null, 0f, 1f);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.keys.Length);
        }

        [Test]
        public void ApplyFullOverride_部分的重なりの場合_ApplyPartialOverrideが呼ばれる()
        {
            // Arrange
            // 低優先順位: 0秒から3秒まで
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(3f, 3f);

            // 高優先順位: 1秒から2秒まで（部分的にのみカバー）
            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(1f, 10f);
            higherCurve.AddKey(2f, 20f);

            // Act
            var result = _curveOverrider.ApplyFullOverride(lowerCurve, higherCurve, 1f, 2f);

            // Assert
            // 部分的重なりの場合、低優先順位の範囲外キー + 高優先順位のキーが含まれる
            Assert.AreEqual(4, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].time);  // 低優先順位から
            Assert.AreEqual(0f, result.keys[0].value);
            Assert.AreEqual(1f, result.keys[1].time);  // 高優先順位から
            Assert.AreEqual(10f, result.keys[1].value);
            Assert.AreEqual(2f, result.keys[2].time);  // 高優先順位から
            Assert.AreEqual(20f, result.keys[2].value);
            Assert.AreEqual(3f, result.keys[3].time);  // 低優先順位から
            Assert.AreEqual(3f, result.keys[3].value);
        }

        [Test]
        public void ApplyFullOverride_低優先順位カーブが空の場合_高優先順位カーブを返す()
        {
            // Arrange
            var lowerCurve = new AnimationCurve();  // キーなし

            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(0f, 10f);
            higherCurve.AddKey(1f, 20f);

            // Act
            var result = _curveOverrider.ApplyFullOverride(lowerCurve, higherCurve, 0f, 1f);

            // Assert
            Assert.AreEqual(2, result.keys.Length);
            Assert.AreEqual(10f, result.keys[0].value);
            Assert.AreEqual(20f, result.keys[1].value);
        }

        [Test]
        public void ApplyFullOverride_高優先順位が前半のみカバーする場合_部分的Overrideが適用される()
        {
            // Arrange
            // 低優先順位: 0秒から4秒まで
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(4f, 4f);

            // 高優先順位: 0秒から2秒まで（前半のみカバー）
            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(0f, 100f);
            higherCurve.AddKey(2f, 200f);

            // Act
            var result = _curveOverrider.ApplyFullOverride(lowerCurve, higherCurve, 0f, 2f);

            // Assert
            // 高優先順位区間（0-2秒）外の4秒のキー + 高優先順位のキー
            Assert.AreEqual(3, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].time);
            Assert.AreEqual(100f, result.keys[0].value);  // 高優先順位
            Assert.AreEqual(2f, result.keys[1].time);
            Assert.AreEqual(200f, result.keys[1].value);  // 高優先順位
            Assert.AreEqual(4f, result.keys[2].time);
            Assert.AreEqual(4f, result.keys[2].value);    // 低優先順位から残る
        }

        [Test]
        public void ApplyFullOverride_元のカーブを変更しない()
        {
            // Arrange
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(1f, 1f);

            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(0f, 10f);
            higherCurve.AddKey(1f, 20f);

            var originalLowerKeyCount = lowerCurve.keys.Length;
            var originalHigherKeyCount = higherCurve.keys.Length;

            // Act
            var result = _curveOverrider.ApplyFullOverride(lowerCurve, higherCurve, 0f, 1f);

            // Assert
            // 元のカーブは変更されていないことを確認
            Assert.AreEqual(originalLowerKeyCount, lowerCurve.keys.Length);
            Assert.AreEqual(originalHigherKeyCount, higherCurve.keys.Length);
            // 結果は別のインスタンス
            Assert.AreNotSame(lowerCurve, result);
            Assert.AreNotSame(higherCurve, result);
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
