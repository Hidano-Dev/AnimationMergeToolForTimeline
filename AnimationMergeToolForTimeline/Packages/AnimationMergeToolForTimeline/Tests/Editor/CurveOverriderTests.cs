using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using AnimationMergeTool.Editor.Domain;
using AnimationMergeTool.Editor.Domain.Models;

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

        #region ApplyPartialOverrideWithExtrapolation テスト

        /// <summary>
        /// TimelineClipのExtrapolationモードを設定するヘルパーメソッド
        /// preExtrapolationMode/postExtrapolationModeは読み取り専用のため、SerializedObjectを使用
        /// </summary>
        private void SetExtrapolationModesInternal(
            TimelineAsset timelineAsset,
            TimelineClip clip,
            TimelineClip.ClipExtrapolation preMode,
            TimelineClip.ClipExtrapolation postMode)
        {
            var serializedObject = new SerializedObject(timelineAsset);
            var tracksProperty = serializedObject.FindProperty("m_Tracks");

            for (int trackIndex = 0; trackIndex < tracksProperty.arraySize; trackIndex++)
            {
                var trackProperty = tracksProperty.GetArrayElementAtIndex(trackIndex);
                var trackObject = new SerializedObject(trackProperty.objectReferenceValue);
                var clipsProperty = trackObject.FindProperty("m_Clips");

                for (int clipIndex = 0; clipIndex < clipsProperty.arraySize; clipIndex++)
                {
                    var clipProperty = clipsProperty.GetArrayElementAtIndex(clipIndex);

                    var startProperty = clipProperty.FindPropertyRelative("m_Start");
                    var durationProperty = clipProperty.FindPropertyRelative("m_Duration");

                    if (System.Math.Abs(startProperty.doubleValue - clip.start) < 0.0001 &&
                        System.Math.Abs(durationProperty.doubleValue - clip.duration) < 0.0001)
                    {
                        var preProperty = clipProperty.FindPropertyRelative("m_PreExtrapolationMode");
                        preProperty.intValue = (int)preMode;
                        var postProperty = clipProperty.FindPropertyRelative("m_PostExtrapolationMode");
                        postProperty.intValue = (int)postMode;
                        trackObject.ApplyModifiedPropertiesWithoutUndo();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// テスト用のClipInfoを作成するヘルパーメソッド
        /// </summary>
        private ClipInfo CreateTestClipInfo(
            double startTime,
            double duration,
            TimelineClip.ClipExtrapolation preExtrapolation = TimelineClip.ClipExtrapolation.None,
            TimelineClip.ClipExtrapolation postExtrapolation = TimelineClip.ClipExtrapolation.None)
        {
            // TimelineAssetとTrackを作成
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var track = timeline.CreateTrack<AnimationTrack>(null, "Test Track");

            // AnimationClipを作成
            var animClip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, (float)duration, (float)duration);
            animClip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            // TimelineClipを作成
            var timelineClip = track.CreateClip(animClip);
            timelineClip.start = startTime;
            timelineClip.duration = duration;

            // preExtrapolationMode/postExtrapolationModeは読み取り専用のため、SerializedObjectを使用
            SetExtrapolationModesInternal(timeline, timelineClip, preExtrapolation, postExtrapolation);

            return new ClipInfo(timelineClip, animClip);
        }

        [Test]
        public void ApplyPartialOverrideWithExtrapolation_高優先順位がnullの場合_低優先順位カーブを返す()
        {
            // Arrange
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(4f, 4f);

            var lowerClipInfo = CreateTestClipInfo(0, 4);

            // Act
            var result = _curveOverrider.ApplyPartialOverrideWithExtrapolation(
                lowerCurve, lowerClipInfo, null, null, null);

            // Assert
            Assert.AreEqual(2, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].value);
            Assert.AreEqual(4f, result.keys[1].value);
        }

        [Test]
        public void ApplyPartialOverrideWithExtrapolation_低優先順位がnullの場合_高優先順位カーブを返す()
        {
            // Arrange
            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(1f, 10f);
            higherCurve.AddKey(2f, 20f);

            var higherClipInfo = CreateTestClipInfo(1, 1);

            // Act
            var result = _curveOverrider.ApplyPartialOverrideWithExtrapolation(
                null, null, higherCurve, higherClipInfo, null);

            // Assert
            Assert.AreEqual(2, result.keys.Length);
            Assert.AreEqual(10f, result.keys[0].value);
            Assert.AreEqual(20f, result.keys[1].value);
        }

        [Test]
        public void ApplyPartialOverrideWithExtrapolation_ExtrapolationがNoneの場合_重なり区間外は低優先順位カーブを使用()
        {
            // Arrange
            // 低優先順位: 0秒から4秒まで
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(4f, 4f);

            var lowerClipInfo = CreateTestClipInfo(0, 4,
                TimelineClip.ClipExtrapolation.None, TimelineClip.ClipExtrapolation.None);

            // 高優先順位: 1秒から2秒まで（Extrapolation = None）
            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(1f, 100f);
            higherCurve.AddKey(2f, 200f);

            var higherClipInfo = CreateTestClipInfo(1, 1,
                TimelineClip.ClipExtrapolation.None, TimelineClip.ClipExtrapolation.None);

            var processor = new ExtrapolationProcessor();

            // Act
            var result = _curveOverrider.ApplyPartialOverrideWithExtrapolation(
                lowerCurve, lowerClipInfo, higherCurve, higherClipInfo, processor);

            // Assert
            // ExtrapolationがNoneなので、重なり区間（1-2秒）は高優先順位、
            // それ以外（0秒、4秒）は低優先順位のキーが残る
            Assert.IsTrue(result.keys.Length >= 4);

            // 0秒のキーは低優先順位の値
            Assert.AreEqual(0f, result.Evaluate(0f), 0.1f);

            // 1-2秒の区間は高優先順位の値
            Assert.AreEqual(100f, result.Evaluate(1f), 0.1f);
            Assert.AreEqual(200f, result.Evaluate(2f), 0.1f);

            // 4秒のキーは低優先順位の値
            Assert.AreEqual(4f, result.Evaluate(4f), 0.1f);
        }

        [Test]
        public void ApplyPartialOverrideWithExtrapolation_PostExtrapolationがHoldの場合_終了後は高優先順位の値を維持()
        {
            // Arrange
            // 低優先順位: 0秒から4秒まで
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(4f, 4f);

            var lowerClipInfo = CreateTestClipInfo(0, 4,
                TimelineClip.ClipExtrapolation.None, TimelineClip.ClipExtrapolation.None);

            // 高優先順位: 1秒から2秒まで（PostExtrapolation = Hold）
            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(1f, 100f);
            higherCurve.AddKey(2f, 200f);

            var higherClipInfo = CreateTestClipInfo(1, 1,
                TimelineClip.ClipExtrapolation.None, TimelineClip.ClipExtrapolation.Hold);

            var processor = new ExtrapolationProcessor();
            processor.SetFrameRate(10f); // テスト用に低フレームレートを使用

            // Act
            var result = _curveOverrider.ApplyPartialOverrideWithExtrapolation(
                lowerCurve, lowerClipInfo, higherCurve, higherClipInfo, processor);

            // Assert
            // 2秒以降は高優先順位の最終値（200）をHoldする
            Assert.AreEqual(200f, result.Evaluate(3f), 0.1f);
            Assert.AreEqual(200f, result.Evaluate(4f), 0.1f);
        }

        [Test]
        public void ApplyPartialOverrideWithExtrapolation_PreExtrapolationがHoldの場合_開始前は高優先順位の値を維持()
        {
            // Arrange
            // 低優先順位: 0秒から4秒まで
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(4f, 4f);

            var lowerClipInfo = CreateTestClipInfo(0, 4,
                TimelineClip.ClipExtrapolation.None, TimelineClip.ClipExtrapolation.None);

            // 高優先順位: 2秒から3秒まで（PreExtrapolation = Hold）
            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(2f, 100f);
            higherCurve.AddKey(3f, 200f);

            var higherClipInfo = CreateTestClipInfo(2, 1,
                TimelineClip.ClipExtrapolation.Hold, TimelineClip.ClipExtrapolation.None);

            var processor = new ExtrapolationProcessor();
            processor.SetFrameRate(10f);

            // Act
            var result = _curveOverrider.ApplyPartialOverrideWithExtrapolation(
                lowerCurve, lowerClipInfo, higherCurve, higherClipInfo, processor);

            // Assert
            // 2秒より前は高優先順位の最初の値（100）をHoldする
            Assert.AreEqual(100f, result.Evaluate(0f), 0.1f);
            Assert.AreEqual(100f, result.Evaluate(1f), 0.1f);
        }

        [Test]
        public void ApplyPartialOverrideWithExtrapolation_両方のExtrapolationがHoldの場合_全区間で高優先順位が有効()
        {
            // Arrange
            // 低優先順位: 0秒から5秒まで
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(5f, 5f);

            var lowerClipInfo = CreateTestClipInfo(0, 5,
                TimelineClip.ClipExtrapolation.None, TimelineClip.ClipExtrapolation.None);

            // 高優先順位: 2秒から3秒まで（両方Hold）
            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(2f, 100f);
            higherCurve.AddKey(3f, 200f);

            var higherClipInfo = CreateTestClipInfo(2, 1,
                TimelineClip.ClipExtrapolation.Hold, TimelineClip.ClipExtrapolation.Hold);

            var processor = new ExtrapolationProcessor();
            processor.SetFrameRate(10f);

            // Act
            var result = _curveOverrider.ApplyPartialOverrideWithExtrapolation(
                lowerCurve, lowerClipInfo, higherCurve, higherClipInfo, processor);

            // Assert
            // 0-2秒はPreExtrapolation(Hold)により100
            Assert.AreEqual(100f, result.Evaluate(0f), 0.1f);
            Assert.AreEqual(100f, result.Evaluate(1f), 0.1f);

            // 2-3秒はクリップ内の値
            Assert.AreEqual(100f, result.Evaluate(2f), 0.1f);
            Assert.AreEqual(200f, result.Evaluate(3f), 0.1f);

            // 3-5秒はPostExtrapolation(Hold)により200
            Assert.AreEqual(200f, result.Evaluate(4f), 0.1f);
            Assert.AreEqual(200f, result.Evaluate(5f), 0.1f);
        }

        [Test]
        public void ApplyPartialOverrideWithExtrapolation_元のカーブを変更しない()
        {
            // Arrange
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(4f, 4f);

            var lowerClipInfo = CreateTestClipInfo(0, 4);

            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(1f, 10f);
            higherCurve.AddKey(2f, 20f);

            var higherClipInfo = CreateTestClipInfo(1, 1,
                TimelineClip.ClipExtrapolation.None, TimelineClip.ClipExtrapolation.Hold);

            var originalLowerKeyCount = lowerCurve.keys.Length;
            var originalHigherKeyCount = higherCurve.keys.Length;
            var processor = new ExtrapolationProcessor();

            // Act
            var result = _curveOverrider.ApplyPartialOverrideWithExtrapolation(
                lowerCurve, lowerClipInfo, higherCurve, higherClipInfo, processor);

            // Assert
            Assert.AreEqual(originalLowerKeyCount, lowerCurve.keys.Length);
            Assert.AreEqual(originalHigherKeyCount, higherCurve.keys.Length);
            Assert.AreNotSame(lowerCurve, result);
            Assert.AreNotSame(higherCurve, result);
        }

        #endregion

        #region MergeMultipleTracks テスト

        [Test]
        public void MergeMultipleTracks_空のリストの場合_空のカーブを返す()
        {
            // Arrange
            var curvesWithTimeRanges = new System.Collections.Generic.List<CurveWithTimeRange>();

            // Act
            var result = _curveOverrider.MergeMultipleTracks(curvesWithTimeRanges);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.keys.Length);
        }

        [Test]
        public void MergeMultipleTracks_nullの場合_空のカーブを返す()
        {
            // Act
            var result = _curveOverrider.MergeMultipleTracks(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.keys.Length);
        }

        [Test]
        public void MergeMultipleTracks_単一カーブの場合_そのカーブのコピーを返す()
        {
            // Arrange
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0f);
            curve.AddKey(1f, 10f);

            var curvesWithTimeRanges = new System.Collections.Generic.List<CurveWithTimeRange>
            {
                new CurveWithTimeRange(curve, 0f, 1f)
            };

            // Act
            var result = _curveOverrider.MergeMultipleTracks(curvesWithTimeRanges);

            // Assert
            Assert.AreEqual(2, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].time);
            Assert.AreEqual(0f, result.keys[0].value);
            Assert.AreEqual(1f, result.keys[1].time);
            Assert.AreEqual(10f, result.keys[1].value);
            // 元のカーブとは別のインスタンス
            Assert.AreNotSame(curve, result);
        }

        [Test]
        public void MergeMultipleTracks_2つのトラックで重なりがない場合_両方のキーが含まれる()
        {
            // Arrange
            // 低優先順位（上の段）: 0-1秒
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(1f, 10f);

            // 高優先順位（下の段）: 2-3秒
            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(2f, 100f);
            higherCurve.AddKey(3f, 200f);

            var curvesWithTimeRanges = new System.Collections.Generic.List<CurveWithTimeRange>
            {
                new CurveWithTimeRange(lowerCurve, 0f, 1f),
                new CurveWithTimeRange(higherCurve, 2f, 3f)
            };

            // Act
            var result = _curveOverrider.MergeMultipleTracks(curvesWithTimeRanges);

            // Assert
            Assert.AreEqual(4, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].time);
            Assert.AreEqual(0f, result.keys[0].value);
            Assert.AreEqual(1f, result.keys[1].time);
            Assert.AreEqual(10f, result.keys[1].value);
            Assert.AreEqual(2f, result.keys[2].time);
            Assert.AreEqual(100f, result.keys[2].value);
            Assert.AreEqual(3f, result.keys[3].time);
            Assert.AreEqual(200f, result.keys[3].value);
        }

        [Test]
        public void MergeMultipleTracks_2つのトラックで完全重なりの場合_高優先順位のカーブで上書きされる()
        {
            // Arrange
            // 低優先順位（上の段）: 0-2秒
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(1f, 5f);
            lowerCurve.AddKey(2f, 10f);

            // 高優先順位（下の段）: 0-2秒（完全重なり）
            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(0f, 100f);
            higherCurve.AddKey(1f, 150f);
            higherCurve.AddKey(2f, 200f);

            var curvesWithTimeRanges = new System.Collections.Generic.List<CurveWithTimeRange>
            {
                new CurveWithTimeRange(lowerCurve, 0f, 2f),
                new CurveWithTimeRange(higherCurve, 0f, 2f)
            };

            // Act
            var result = _curveOverrider.MergeMultipleTracks(curvesWithTimeRanges);

            // Assert
            // 完全重なりの場合、高優先順位のキーのみ
            Assert.AreEqual(3, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].time);
            Assert.AreEqual(100f, result.keys[0].value);
            Assert.AreEqual(1f, result.keys[1].time);
            Assert.AreEqual(150f, result.keys[1].value);
            Assert.AreEqual(2f, result.keys[2].time);
            Assert.AreEqual(200f, result.keys[2].value);
        }

        [Test]
        public void MergeMultipleTracks_2つのトラックで部分重なりの場合_重なり区間は高優先順位で上書きされる()
        {
            // Arrange
            // 低優先順位（上の段）: 0-3秒
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(3f, 30f);

            // 高優先順位（下の段）: 1-2秒（部分的重なり）
            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(1f, 100f);
            higherCurve.AddKey(2f, 200f);

            var curvesWithTimeRanges = new System.Collections.Generic.List<CurveWithTimeRange>
            {
                new CurveWithTimeRange(lowerCurve, 0f, 3f),
                new CurveWithTimeRange(higherCurve, 1f, 2f)
            };

            // Act
            var result = _curveOverrider.MergeMultipleTracks(curvesWithTimeRanges);

            // Assert
            // 0秒: 低優先順位の値
            // 1-2秒: 高優先順位の値
            // 3秒: 低優先順位の値
            Assert.AreEqual(4, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].time);
            Assert.AreEqual(0f, result.keys[0].value);     // 低優先順位
            Assert.AreEqual(1f, result.keys[1].time);
            Assert.AreEqual(100f, result.keys[1].value);   // 高優先順位
            Assert.AreEqual(2f, result.keys[2].time);
            Assert.AreEqual(200f, result.keys[2].value);   // 高優先順位
            Assert.AreEqual(3f, result.keys[3].time);
            Assert.AreEqual(30f, result.keys[3].value);    // 低優先順位
        }

        [Test]
        public void MergeMultipleTracks_3つのトラックを優先順位順に統合できる()
        {
            // Arrange
            // 最低優先順位（最上段）: 0-4秒
            var lowestCurve = new AnimationCurve();
            lowestCurve.AddKey(0f, 0f);
            lowestCurve.AddKey(4f, 40f);

            // 中間優先順位（中段）: 1-3秒
            var middleCurve = new AnimationCurve();
            middleCurve.AddKey(1f, 100f);
            middleCurve.AddKey(3f, 300f);

            // 最高優先順位（最下段）: 1.5-2.5秒
            var highestCurve = new AnimationCurve();
            highestCurve.AddKey(1.5f, 1000f);
            highestCurve.AddKey(2.5f, 2500f);

            var curvesWithTimeRanges = new System.Collections.Generic.List<CurveWithTimeRange>
            {
                new CurveWithTimeRange(lowestCurve, 0f, 4f),
                new CurveWithTimeRange(middleCurve, 1f, 3f),
                new CurveWithTimeRange(highestCurve, 1.5f, 2.5f)
            };

            // Act
            var result = _curveOverrider.MergeMultipleTracks(curvesWithTimeRanges);

            // Assert
            // 期待されるキー:
            // 0秒: 最低優先順位の値
            // 1秒: 中間優先順位の値
            // 1.5秒: 最高優先順位の値
            // 2.5秒: 最高優先順位の値
            // 3秒: 中間優先順位の値
            // 4秒: 最低優先順位の値
            Assert.AreEqual(6, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].time);
            Assert.AreEqual(0f, result.keys[0].value);
            Assert.AreEqual(1f, result.keys[1].time);
            Assert.AreEqual(100f, result.keys[1].value);
            Assert.AreEqual(1.5f, result.keys[2].time);
            Assert.AreEqual(1000f, result.keys[2].value);
            Assert.AreEqual(2.5f, result.keys[3].time);
            Assert.AreEqual(2500f, result.keys[3].value);
            Assert.AreEqual(3f, result.keys[4].time);
            Assert.AreEqual(300f, result.keys[4].value);
            Assert.AreEqual(4f, result.keys[5].time);
            Assert.AreEqual(40f, result.keys[5].value);
        }

        [Test]
        public void MergeMultipleTracks_途中のカーブがnullの場合_スキップして処理する()
        {
            // Arrange
            // 低優先順位: 0-1秒
            var lowerCurve = new AnimationCurve();
            lowerCurve.AddKey(0f, 0f);
            lowerCurve.AddKey(1f, 10f);

            // 高優先順位: 2-3秒
            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(2f, 100f);
            higherCurve.AddKey(3f, 200f);

            var curvesWithTimeRanges = new System.Collections.Generic.List<CurveWithTimeRange>
            {
                new CurveWithTimeRange(lowerCurve, 0f, 1f),
                new CurveWithTimeRange(null, 1f, 2f),  // nullのカーブ
                new CurveWithTimeRange(higherCurve, 2f, 3f)
            };

            // Act
            var result = _curveOverrider.MergeMultipleTracks(curvesWithTimeRanges);

            // Assert
            // nullカーブはスキップされ、有効なカーブのキーが含まれる
            Assert.AreEqual(4, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].time);
            Assert.AreEqual(0f, result.keys[0].value);
            Assert.AreEqual(1f, result.keys[1].time);
            Assert.AreEqual(10f, result.keys[1].value);
            Assert.AreEqual(2f, result.keys[2].time);
            Assert.AreEqual(100f, result.keys[2].value);
            Assert.AreEqual(3f, result.keys[3].time);
            Assert.AreEqual(200f, result.keys[3].value);
        }

        [Test]
        public void MergeMultipleTracks_最初のカーブがnullの場合_空のカーブから開始して処理する()
        {
            // Arrange
            var higherCurve = new AnimationCurve();
            higherCurve.AddKey(0f, 100f);
            higherCurve.AddKey(1f, 200f);

            var curvesWithTimeRanges = new System.Collections.Generic.List<CurveWithTimeRange>
            {
                new CurveWithTimeRange(null, 0f, 1f),  // 最初がnull
                new CurveWithTimeRange(higherCurve, 0f, 1f)
            };

            // Act
            var result = _curveOverrider.MergeMultipleTracks(curvesWithTimeRanges);

            // Assert
            // 最初がnullでも、後続のカーブが適用される
            Assert.AreEqual(2, result.keys.Length);
            Assert.AreEqual(0f, result.keys[0].time);
            Assert.AreEqual(100f, result.keys[0].value);
            Assert.AreEqual(1f, result.keys[1].time);
            Assert.AreEqual(200f, result.keys[1].value);
        }

        [Test]
        public void MergeMultipleTracks_元のカーブを変更しない()
        {
            // Arrange
            var curve1 = new AnimationCurve();
            curve1.AddKey(0f, 0f);
            curve1.AddKey(1f, 10f);

            var curve2 = new AnimationCurve();
            curve2.AddKey(0.5f, 50f);
            curve2.AddKey(1.5f, 150f);

            var originalCurve1KeyCount = curve1.keys.Length;
            var originalCurve2KeyCount = curve2.keys.Length;

            var curvesWithTimeRanges = new System.Collections.Generic.List<CurveWithTimeRange>
            {
                new CurveWithTimeRange(curve1, 0f, 1f),
                new CurveWithTimeRange(curve2, 0.5f, 1.5f)
            };

            // Act
            var result = _curveOverrider.MergeMultipleTracks(curvesWithTimeRanges);

            // Assert
            Assert.AreEqual(originalCurve1KeyCount, curve1.keys.Length);
            Assert.AreEqual(originalCurve2KeyCount, curve2.keys.Length);
            Assert.AreNotSame(curve1, result);
            Assert.AreNotSame(curve2, result);
        }

        #endregion

        #region CurveWithTimeRange テスト

        [Test]
        public void CurveWithTimeRange_コンストラクタで値が正しく設定される()
        {
            // Arrange
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            var startTime = 1.5f;
            var endTime = 3.5f;

            // Act
            var curveWithTimeRange = new CurveWithTimeRange(curve, startTime, endTime);

            // Assert
            Assert.AreEqual(curve, curveWithTimeRange.Curve);
            Assert.AreEqual(startTime, curveWithTimeRange.StartTime);
            Assert.AreEqual(endTime, curveWithTimeRange.EndTime);
        }

        #endregion
    }
}
