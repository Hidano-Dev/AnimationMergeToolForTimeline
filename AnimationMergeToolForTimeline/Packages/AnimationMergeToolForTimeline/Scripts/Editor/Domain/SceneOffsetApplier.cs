using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AnimationMergeTool.Editor.Domain
{
    /// <summary>
    /// AnimationPlayableAssetのシーンオフセット（Position/Rotation）を
    /// アニメーションカーブに適用するクラス
    /// </summary>
    public class SceneOffsetApplier
    {
        // ルートTransformのPositionプロパティ名
        private static readonly string[] PositionPropertyNames =
        {
            "m_LocalPosition.x",
            "m_LocalPosition.y",
            "m_LocalPosition.z"
        };

        // ルートTransformのRotationプロパティ名（Quaternion）
        private static readonly string[] RotationPropertyNames =
        {
            "m_LocalRotation.x",
            "m_LocalRotation.y",
            "m_LocalRotation.z",
            "m_LocalRotation.w"
        };

        // Humanoidルートモーション Positionプロパティ名
        private static readonly string[] RootMotionPositionPropertyNames =
        {
            "RootT.x",
            "RootT.y",
            "RootT.z"
        };

        // Humanoidルートモーション Rotationプロパティ名
        private static readonly string[] RootMotionRotationPropertyNames =
        {
            "RootQ.x",
            "RootQ.y",
            "RootQ.z",
            "RootQ.w"
        };

        /// <summary>
        /// カーブリストにシーンオフセットを適用する
        /// Positionオフセット: ルートTransformのPositionカーブに加算
        /// Rotationオフセット: ルートTransformのRotationカーブにクォータニオン乗算
        /// </summary>
        /// <param name="curveBindingPairs">適用対象のカーブリスト（変更される）</param>
        /// <param name="positionOffset">Positionオフセット</param>
        /// <param name="rotationOffset">Rotationオフセット</param>
        /// <returns>オフセット適用後のカーブリスト</returns>
        public List<CurveBindingPair> Apply(
            List<CurveBindingPair> curveBindingPairs,
            Vector3 positionOffset,
            Quaternion rotationOffset)
        {
            if (curveBindingPairs == null || curveBindingPairs.Count == 0)
            {
                return curveBindingPairs;
            }

            var hasPositionOffset = positionOffset != Vector3.zero;
            var hasRotationOffset = rotationOffset != Quaternion.identity;

            if (!hasPositionOffset && !hasRotationOffset)
            {
                return curveBindingPairs;
            }

            var result = new List<CurveBindingPair>(curveBindingPairs.Count);

            // Positionオフセットの適用
            if (hasPositionOffset)
            {
                ApplyPositionOffset(curveBindingPairs, result, positionOffset);
            }

            // Rotationオフセットの適用
            if (hasRotationOffset)
            {
                ApplyRotationOffset(curveBindingPairs, result, rotationOffset);
            }

            // オフセット適用対象外のカーブをそのまま追加
            foreach (var pair in curveBindingPairs)
            {
                if (!result.Any(r =>
                    r.Binding.path == pair.Binding.path &&
                    r.Binding.propertyName == pair.Binding.propertyName &&
                    r.Binding.type == pair.Binding.type))
                {
                    result.Add(pair);
                }
            }

            return result;
        }

        /// <summary>
        /// Positionオフセットを適用する
        /// </summary>
        private void ApplyPositionOffset(
            List<CurveBindingPair> source,
            List<CurveBindingPair> result,
            Vector3 offset)
        {
            var offsetComponents = new[] { offset.x, offset.y, offset.z };

            // Transform Positionカーブ
            ApplyAdditiveOffset(source, result, PositionPropertyNames, offsetComponents, typeof(Transform));
            // Humanoid RootT カーブ
            ApplyAdditiveOffset(source, result, RootMotionPositionPropertyNames, offsetComponents, typeof(Animator));
        }

        /// <summary>
        /// 加算オフセットをカーブに適用する
        /// </summary>
        private void ApplyAdditiveOffset(
            List<CurveBindingPair> source,
            List<CurveBindingPair> result,
            string[] propertyNames,
            float[] offsets,
            System.Type bindingType)
        {
            for (var i = 0; i < propertyNames.Length && i < offsets.Length; i++)
            {
                if (Mathf.Approximately(offsets[i], 0f))
                {
                    continue;
                }

                var propertyName = propertyNames[i];
                var offsetValue = offsets[i];

                var pair = FindRootCurve(source, propertyName, bindingType);
                if (pair == null)
                {
                    continue;
                }

                var newCurve = new AnimationCurve();
                foreach (var key in pair.Curve.keys)
                {
                    var newKey = new Keyframe(key.time, key.value + offsetValue)
                    {
                        inTangent = key.inTangent,
                        outTangent = key.outTangent,
                        inWeight = key.inWeight,
                        outWeight = key.outWeight,
                        weightedMode = key.weightedMode
                    };
                    newCurve.AddKey(newKey);
                }

                result.Add(new CurveBindingPair(pair.Binding, newCurve));
            }
        }

        /// <summary>
        /// Rotationオフセットを適用する
        /// クォータニオン乗算で適用: newQ = offsetQ * originalQ
        /// </summary>
        private void ApplyRotationOffset(
            List<CurveBindingPair> source,
            List<CurveBindingPair> result,
            Quaternion offset)
        {
            // Transform Rotation カーブ
            ApplyQuaternionOffset(source, result, RotationPropertyNames, offset, typeof(Transform));
            // Humanoid RootQ カーブ
            ApplyQuaternionOffset(source, result, RootMotionRotationPropertyNames, offset, typeof(Animator));
        }

        /// <summary>
        /// クォータニオンオフセットをカーブに適用する
        /// </summary>
        private void ApplyQuaternionOffset(
            List<CurveBindingPair> source,
            List<CurveBindingPair> result,
            string[] propertyNames,
            Quaternion offset,
            System.Type bindingType)
        {
            // 4つのクォータニオンコンポーネントカーブを取得
            var curves = new CurveBindingPair[4];
            var allFound = true;
            for (var i = 0; i < 4; i++)
            {
                curves[i] = FindRootCurve(source, propertyNames[i], bindingType);
                if (curves[i] == null)
                {
                    allFound = false;
                }
            }

            // 4つのカーブがすべて揃っていない場合はスキップ
            if (!allFound)
            {
                return;
            }

            // 既にresultに追加済みのカーブと重複しないよう確認
            // 見つかったカーブのパスで比較する（path=""固定ではなくGenericリグにも対応）
            var foundPath = curves[0].Binding.path;
            if (result.Any(r => r.Binding.path == foundPath && r.Binding.propertyName == propertyNames[0] && r.Binding.type == bindingType))
            {
                return;
            }

            // 全カーブのユニークなキーフレーム時間を収集
            var allTimes = new SortedSet<float>();
            foreach (var curve in curves)
            {
                foreach (var key in curve.Curve.keys)
                {
                    allTimes.Add(key.time);
                }
            }

            // 新しい4つのカーブを作成
            var newCurves = new AnimationCurve[4];
            for (var i = 0; i < 4; i++)
            {
                newCurves[i] = new AnimationCurve();
            }

            // 各時間でクォータニオン乗算を適用
            foreach (var time in allTimes)
            {
                var originalQ = new Quaternion(
                    curves[0].Curve.Evaluate(time),
                    curves[1].Curve.Evaluate(time),
                    curves[2].Curve.Evaluate(time),
                    curves[3].Curve.Evaluate(time)
                );

                var newQ = offset * originalQ;

                newCurves[0].AddKey(new Keyframe(time, newQ.x));
                newCurves[1].AddKey(new Keyframe(time, newQ.y));
                newCurves[2].AddKey(new Keyframe(time, newQ.z));
                newCurves[3].AddKey(new Keyframe(time, newQ.w));
            }

            // 結果に追加
            for (var i = 0; i < 4; i++)
            {
                result.Add(new CurveBindingPair(curves[i].Binding, newCurves[i]));
            }
        }

        /// <summary>
        /// ルートカーブを検索する
        /// まずpath=""のカーブを探し、見つからない場合は最も浅いパスのカーブを返す（Genericリグ対応）
        /// </summary>
        private CurveBindingPair FindRootCurve(
            List<CurveBindingPair> pairs,
            string propertyName,
            System.Type bindingType)
        {
            // まずpath=""のカーブを探す（Humanoidリグ、path=""のTransformカーブ）
            foreach (var pair in pairs)
            {
                if (pair.Binding.path == "" &&
                    pair.Binding.propertyName == propertyName &&
                    pair.Binding.type == bindingType)
                {
                    return pair;
                }
            }

            // path=""が見つからない場合、最も浅いパスのカーブを探す（Genericリグ対応）
            // Genericリグではルートボーンがpath=""ではなく "fbxJnt_grp/J_C_hip" 等のパスにある
            CurveBindingPair shallowest = null;
            int shallowestDepth = int.MaxValue;
            foreach (var pair in pairs)
            {
                if (string.IsNullOrEmpty(pair.Binding.path) ||
                    pair.Binding.propertyName != propertyName ||
                    pair.Binding.type != bindingType)
                {
                    continue;
                }

                int depth = pair.Binding.path.Split('/').Length;
                if (depth < shallowestDepth)
                {
                    shallowestDepth = depth;
                    shallowest = pair;
                }
            }

            return shallowest;
        }
    }
}
