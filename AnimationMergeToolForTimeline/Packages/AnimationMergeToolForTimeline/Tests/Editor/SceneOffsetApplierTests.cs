using System.Collections.Generic;
using NUnit.Framework;
using AnimationMergeTool.Editor.Domain;
using UnityEditor;
using UnityEngine;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// SceneOffsetApplierクラスの単体テスト
    /// </summary>
    public class SceneOffsetApplierTests
    {
        private SceneOffsetApplier _applier;

        [SetUp]
        public void SetUp()
        {
            _applier = new SceneOffsetApplier();
        }

        #region Positionオフセットテスト

        [Test]
        public void Apply_Positionオフセットがルートカーブに加算される()
        {
            // Arrange
            var pairs = new List<CurveBindingPair>
            {
                CreateRootPositionCurve("m_LocalPosition.x", new Keyframe(0f, 1f), new Keyframe(1f, 2f)),
                CreateRootPositionCurve("m_LocalPosition.y", new Keyframe(0f, 0f), new Keyframe(1f, 1f)),
                CreateRootPositionCurve("m_LocalPosition.z", new Keyframe(0f, 3f), new Keyframe(1f, 4f))
            };
            var positionOffset = new Vector3(10f, 20f, 30f);

            // Act
            var result = _applier.Apply(pairs, positionOffset, Quaternion.identity);

            // Assert
            var xCurve = FindCurve(result, "m_LocalPosition.x");
            var yCurve = FindCurve(result, "m_LocalPosition.y");
            var zCurve = FindCurve(result, "m_LocalPosition.z");

            Assert.IsNotNull(xCurve);
            Assert.IsNotNull(yCurve);
            Assert.IsNotNull(zCurve);

            Assert.AreEqual(11f, xCurve.keys[0].value, 0.001f);
            Assert.AreEqual(12f, xCurve.keys[1].value, 0.001f);
            Assert.AreEqual(20f, yCurve.keys[0].value, 0.001f);
            Assert.AreEqual(21f, yCurve.keys[1].value, 0.001f);
            Assert.AreEqual(33f, zCurve.keys[0].value, 0.001f);
            Assert.AreEqual(34f, zCurve.keys[1].value, 0.001f);
        }

        [Test]
        public void Apply_Positionオフセットがゼロの場合カーブが変更されない()
        {
            // Arrange
            var pairs = new List<CurveBindingPair>
            {
                CreateRootPositionCurve("m_LocalPosition.x", new Keyframe(0f, 1f)),
                CreateRootPositionCurve("m_LocalPosition.y", new Keyframe(0f, 2f)),
                CreateRootPositionCurve("m_LocalPosition.z", new Keyframe(0f, 3f))
            };

            // Act
            var result = _applier.Apply(pairs, Vector3.zero, Quaternion.identity);

            // Assert - 元のカーブと同じ値
            var xCurve = FindCurve(result, "m_LocalPosition.x");
            Assert.AreEqual(1f, xCurve.keys[0].value, 0.001f);
        }

        [Test]
        public void Apply_ルートカーブが存在する場合に子パスのカーブにはPositionオフセットが適用されない()
        {
            // Arrange - path=""のルートカーブと子パスのカーブの両方がある場合
            var childBinding = EditorCurveBinding.FloatCurve("Child", typeof(Transform), "m_LocalPosition.x");
            var childCurve = new AnimationCurve(new Keyframe(0f, 5f));
            var pairs = new List<CurveBindingPair>
            {
                CreateRootPositionCurve("m_LocalPosition.x", new Keyframe(0f, 1f)),
                new CurveBindingPair(childBinding, childCurve)
            };
            var positionOffset = new Vector3(10f, 0f, 0f);

            // Act
            var result = _applier.Apply(pairs, positionOffset, Quaternion.identity);

            // Assert - ルートのカーブにはオフセットが適用される
            var rootCurve = FindCurve(result, "m_LocalPosition.x");
            Assert.AreEqual(11f, rootCurve.keys[0].value, 0.001f);

            // Assert - 子パスのカーブは変更されない
            CurveBindingPair childPair = null;
            foreach (var pair in result)
            {
                if (pair.Binding.path == "Child")
                {
                    childPair = pair;
                    break;
                }
            }
            Assert.IsNotNull(childPair);
            Assert.AreEqual(5f, childPair.Curve.keys[0].value, 0.001f);
        }

        [Test]
        public void Apply_Genericリグで最浅パスのカーブにPositionオフセットが適用される()
        {
            // Arrange - path=""がなく、非空パスのカーブのみの場合（Genericリグ）
            var rootBoneBinding = EditorCurveBinding.FloatCurve("Armature/Hips", typeof(Transform), "m_LocalPosition.x");
            var rootBoneCurve = new AnimationCurve(new Keyframe(0f, 0.5f));
            var childBinding = EditorCurveBinding.FloatCurve("Armature/Hips/Spine", typeof(Transform), "m_LocalPosition.x");
            var childCurve = new AnimationCurve(new Keyframe(0f, 0.1f));
            var pairs = new List<CurveBindingPair>
            {
                new CurveBindingPair(rootBoneBinding, rootBoneCurve),
                new CurveBindingPair(childBinding, childCurve)
            };
            var positionOffset = new Vector3(10f, 0f, 0f);

            // Act
            var result = _applier.Apply(pairs, positionOffset, Quaternion.identity);

            // Assert - 最浅パス（Armature/Hips）にオフセットが適用される
            CurveBindingPair rootBonePair = null;
            CurveBindingPair childPair = null;
            foreach (var pair in result)
            {
                if (pair.Binding.path == "Armature/Hips" && pair.Binding.propertyName == "m_LocalPosition.x")
                    rootBonePair = pair;
                if (pair.Binding.path == "Armature/Hips/Spine")
                    childPair = pair;
            }
            Assert.IsNotNull(rootBonePair);
            Assert.AreEqual(10.5f, rootBonePair.Curve.keys[0].value, 0.001f);

            // Assert - 子パスはオフセット未適用
            Assert.IsNotNull(childPair);
            Assert.AreEqual(0.1f, childPair.Curve.keys[0].value, 0.001f);
        }

        [Test]
        public void Apply_Positionオフセットでタンジェントが保持される()
        {
            // Arrange
            var key = new Keyframe(0f, 1f)
            {
                inTangent = 0.5f,
                outTangent = 1.5f
            };
            var pairs = new List<CurveBindingPair>
            {
                CreateRootPositionCurve("m_LocalPosition.x", key)
            };
            var positionOffset = new Vector3(10f, 0f, 0f);

            // Act
            var result = _applier.Apply(pairs, positionOffset, Quaternion.identity);

            // Assert
            var xCurve = FindCurve(result, "m_LocalPosition.x");
            Assert.AreEqual(0.5f, xCurve.keys[0].inTangent, 0.001f);
            Assert.AreEqual(1.5f, xCurve.keys[0].outTangent, 0.001f);
        }

        #endregion

        #region Rotationオフセットテスト

        [Test]
        public void Apply_Rotationオフセットがルートカーブに適用される()
        {
            // Arrange - 初期回転はIdentity (0,0,0,1)
            var pairs = new List<CurveBindingPair>
            {
                CreateRootRotationCurve("m_LocalRotation.x", new Keyframe(0f, 0f)),
                CreateRootRotationCurve("m_LocalRotation.y", new Keyframe(0f, 0f)),
                CreateRootRotationCurve("m_LocalRotation.z", new Keyframe(0f, 0f)),
                CreateRootRotationCurve("m_LocalRotation.w", new Keyframe(0f, 1f))
            };

            // Y軸90度回転のオフセット
            var rotationOffset = Quaternion.Euler(0f, 90f, 0f);

            // Act
            var result = _applier.Apply(pairs, Vector3.zero, rotationOffset);

            // Assert - offsetQ * identityQ = offsetQ
            var xCurve = FindCurve(result, "m_LocalRotation.x");
            var yCurve = FindCurve(result, "m_LocalRotation.y");
            var zCurve = FindCurve(result, "m_LocalRotation.z");
            var wCurve = FindCurve(result, "m_LocalRotation.w");

            Assert.IsNotNull(xCurve);
            Assert.IsNotNull(yCurve);
            Assert.IsNotNull(zCurve);
            Assert.IsNotNull(wCurve);

            // offset * identity = offset
            var expectedQ = rotationOffset;
            Assert.AreEqual(expectedQ.x, xCurve.keys[0].value, 0.001f);
            Assert.AreEqual(expectedQ.y, yCurve.keys[0].value, 0.001f);
            Assert.AreEqual(expectedQ.z, zCurve.keys[0].value, 0.001f);
            Assert.AreEqual(expectedQ.w, wCurve.keys[0].value, 0.001f);
        }

        [Test]
        public void Apply_Rotationオフセットがidentityの場合カーブが変更されない()
        {
            // Arrange
            var pairs = new List<CurveBindingPair>
            {
                CreateRootRotationCurve("m_LocalRotation.x", new Keyframe(0f, 0.1f)),
                CreateRootRotationCurve("m_LocalRotation.y", new Keyframe(0f, 0.2f)),
                CreateRootRotationCurve("m_LocalRotation.z", new Keyframe(0f, 0.3f)),
                CreateRootRotationCurve("m_LocalRotation.w", new Keyframe(0f, 0.9f))
            };

            // Act
            var result = _applier.Apply(pairs, Vector3.zero, Quaternion.identity);

            // Assert - 元のカーブと同じ値
            var xCurve = FindCurve(result, "m_LocalRotation.x");
            Assert.AreEqual(0.1f, xCurve.keys[0].value, 0.001f);
        }

        [Test]
        public void Apply_4つのRotationカーブが揃っていない場合スキップされる()
        {
            // Arrange - wカーブがない
            var pairs = new List<CurveBindingPair>
            {
                CreateRootRotationCurve("m_LocalRotation.x", new Keyframe(0f, 0f)),
                CreateRootRotationCurve("m_LocalRotation.y", new Keyframe(0f, 0f)),
                CreateRootRotationCurve("m_LocalRotation.z", new Keyframe(0f, 0f))
            };
            var rotationOffset = Quaternion.Euler(0f, 90f, 0f);

            // Act
            var result = _applier.Apply(pairs, Vector3.zero, rotationOffset);

            // Assert - カーブは変更されずにそのまま
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(0f, result[0].Curve.keys[0].value, 0.001f);
        }

        #endregion

        #region 複合テスト

        [Test]
        public void Apply_PositionとRotation両方のオフセットが同時に適用される()
        {
            // Arrange
            var pairs = new List<CurveBindingPair>
            {
                CreateRootPositionCurve("m_LocalPosition.x", new Keyframe(0f, 1f)),
                CreateRootPositionCurve("m_LocalPosition.y", new Keyframe(0f, 2f)),
                CreateRootPositionCurve("m_LocalPosition.z", new Keyframe(0f, 3f)),
                CreateRootRotationCurve("m_LocalRotation.x", new Keyframe(0f, 0f)),
                CreateRootRotationCurve("m_LocalRotation.y", new Keyframe(0f, 0f)),
                CreateRootRotationCurve("m_LocalRotation.z", new Keyframe(0f, 0f)),
                CreateRootRotationCurve("m_LocalRotation.w", new Keyframe(0f, 1f))
            };
            var positionOffset = new Vector3(5f, 10f, 15f);
            var rotationOffset = Quaternion.Euler(0f, 90f, 0f);

            // Act
            var result = _applier.Apply(pairs, positionOffset, rotationOffset);

            // Assert - Positionが加算されている
            var xCurve = FindCurve(result, "m_LocalPosition.x");
            Assert.AreEqual(6f, xCurve.keys[0].value, 0.001f);

            // Assert - Rotationが適用されている
            var qyCurve = FindCurve(result, "m_LocalRotation.y");
            Assert.AreEqual(rotationOffset.y, qyCurve.keys[0].value, 0.001f);
        }

        [Test]
        public void Apply_nullのカーブリストの場合nullを返す()
        {
            // Act
            var result = _applier.Apply(null, Vector3.one, Quaternion.identity);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void Apply_空のカーブリストの場合空を返す()
        {
            // Act
            var result = _applier.Apply(new List<CurveBindingPair>(), Vector3.one, Quaternion.identity);

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Apply_オフセット対象外のカーブもそのまま残る()
        {
            // Arrange - BlendShapeカーブ（オフセット適用対象外）
            var blendShapeBinding = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.smile");
            var blendShapeCurve = new AnimationCurve(new Keyframe(0f, 50f));
            var pairs = new List<CurveBindingPair>
            {
                CreateRootPositionCurve("m_LocalPosition.x", new Keyframe(0f, 1f)),
                CreateRootPositionCurve("m_LocalPosition.y", new Keyframe(0f, 0f)),
                CreateRootPositionCurve("m_LocalPosition.z", new Keyframe(0f, 0f)),
                new CurveBindingPair(blendShapeBinding, blendShapeCurve)
            };
            var positionOffset = new Vector3(10f, 0f, 0f);

            // Act
            var result = _applier.Apply(pairs, positionOffset, Quaternion.identity);

            // Assert - BlendShapeカーブが残っている
            CurveBindingPair blendShapePair = null;
            foreach (var pair in result)
            {
                if (pair.Binding.propertyName == "blendShape.smile")
                {
                    blendShapePair = pair;
                    break;
                }
            }
            Assert.IsNotNull(blendShapePair);
            Assert.AreEqual(50f, blendShapePair.Curve.keys[0].value, 0.001f);
        }

        #endregion

        #region Humanoidルートモーションカーブテスト

        [Test]
        public void Apply_HumanoidのRootTカーブにPositionオフセットが適用される()
        {
            // Arrange
            var pairs = new List<CurveBindingPair>
            {
                CreateRootMotionPositionCurve("RootT.x", new Keyframe(0f, 1f)),
                CreateRootMotionPositionCurve("RootT.y", new Keyframe(0f, 2f)),
                CreateRootMotionPositionCurve("RootT.z", new Keyframe(0f, 3f))
            };
            var positionOffset = new Vector3(10f, 20f, 30f);

            // Act
            var result = _applier.Apply(pairs, positionOffset, Quaternion.identity);

            // Assert
            var xCurve = FindCurve(result, "RootT.x");
            var yCurve = FindCurve(result, "RootT.y");
            var zCurve = FindCurve(result, "RootT.z");

            Assert.AreEqual(11f, xCurve.keys[0].value, 0.001f);
            Assert.AreEqual(22f, yCurve.keys[0].value, 0.001f);
            Assert.AreEqual(33f, zCurve.keys[0].value, 0.001f);
        }

        [Test]
        public void Apply_HumanoidのRootQカーブにRotationオフセットが適用される()
        {
            // Arrange - identity quaternion
            var pairs = new List<CurveBindingPair>
            {
                CreateRootMotionRotationCurve("RootQ.x", new Keyframe(0f, 0f)),
                CreateRootMotionRotationCurve("RootQ.y", new Keyframe(0f, 0f)),
                CreateRootMotionRotationCurve("RootQ.z", new Keyframe(0f, 0f)),
                CreateRootMotionRotationCurve("RootQ.w", new Keyframe(0f, 1f))
            };
            var rotationOffset = Quaternion.Euler(0f, 90f, 0f);

            // Act
            var result = _applier.Apply(pairs, Vector3.zero, rotationOffset);

            // Assert
            var xCurve = FindCurve(result, "RootQ.x");
            var yCurve = FindCurve(result, "RootQ.y");

            Assert.IsNotNull(xCurve);
            Assert.IsNotNull(yCurve);
            Assert.AreEqual(rotationOffset.y, yCurve.keys[0].value, 0.001f);
        }

        #endregion

        #region ヘルパーメソッド

        private CurveBindingPair CreateRootPositionCurve(string propertyName, params Keyframe[] keys)
        {
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), propertyName);
            var curve = new AnimationCurve(keys);
            return new CurveBindingPair(binding, curve);
        }

        private CurveBindingPair CreateRootRotationCurve(string propertyName, params Keyframe[] keys)
        {
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), propertyName);
            var curve = new AnimationCurve(keys);
            return new CurveBindingPair(binding, curve);
        }

        private CurveBindingPair CreateRootMotionPositionCurve(string propertyName, params Keyframe[] keys)
        {
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), propertyName);
            var curve = new AnimationCurve(keys);
            return new CurveBindingPair(binding, curve);
        }

        private CurveBindingPair CreateRootMotionRotationCurve(string propertyName, params Keyframe[] keys)
        {
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), propertyName);
            var curve = new AnimationCurve(keys);
            return new CurveBindingPair(binding, curve);
        }

        private AnimationCurve FindCurve(List<CurveBindingPair> pairs, string propertyName)
        {
            foreach (var pair in pairs)
            {
                if (pair.Binding.propertyName == propertyName && pair.Binding.path == "")
                {
                    return pair.Curve;
                }
            }
            return null;
        }

        #endregion
    }
}
