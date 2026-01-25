using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnimationMergeTool.Editor.Domain
{
    /// <summary>
    /// Humanoidマッスルカーブの検出を行うクラス
    /// Humanoid Muscleプロパティを識別する
    /// </summary>
    public class MuscleDetector
    {
        /// <summary>
        /// Humanoidマッスルカーブのプロパティ名一覧
        /// Unity内部で使用されるマッスル名を網羅
        /// </summary>
        private static readonly string[] MusclePropertyNames = new[]
        {
            // 体幹
            "Spine Front-Back",
            "Spine Left-Right",
            "Spine Twist Left-Right",
            "Chest Front-Back",
            "Chest Left-Right",
            "Chest Twist Left-Right",
            "UpperChest Front-Back",
            "UpperChest Left-Right",
            "UpperChest Twist Left-Right",

            // 首・頭
            "Neck Nod Down-Up",
            "Neck Tilt Left-Right",
            "Neck Turn Left-Right",
            "Head Nod Down-Up",
            "Head Tilt Left-Right",
            "Head Turn Left-Right",

            // 目
            "Left Eye Down-Up",
            "Left Eye In-Out",
            "Right Eye Down-Up",
            "Right Eye In-Out",

            // 顎
            "Jaw Close",
            "Jaw Left-Right",

            // 左腕
            "Left Shoulder Down-Up",
            "Left Shoulder Front-Back",
            "Left Arm Down-Up",
            "Left Arm Front-Back",
            "Left Arm Twist In-Out",
            "Left Forearm Stretch",
            "Left Forearm Twist In-Out",
            "Left Hand Down-Up",
            "Left Hand In-Out",

            // 右腕
            "Right Shoulder Down-Up",
            "Right Shoulder Front-Back",
            "Right Arm Down-Up",
            "Right Arm Front-Back",
            "Right Arm Twist In-Out",
            "Right Forearm Stretch",
            "Right Forearm Twist In-Out",
            "Right Hand Down-Up",
            "Right Hand In-Out",

            // 左脚
            "Left Upper Leg Front-Back",
            "Left Upper Leg In-Out",
            "Left Upper Leg Twist In-Out",
            "Left Lower Leg Stretch",
            "Left Lower Leg Twist In-Out",
            "Left Foot Up-Down",
            "Left Foot Twist In-Out",
            "Left Toes Up-Down",

            // 右脚
            "Right Upper Leg Front-Back",
            "Right Upper Leg In-Out",
            "Right Upper Leg Twist In-Out",
            "Right Lower Leg Stretch",
            "Right Lower Leg Twist In-Out",
            "Right Foot Up-Down",
            "Right Foot Twist In-Out",
            "Right Toes Up-Down",

            // 左手指
            "LeftHand.Thumb.1 Stretched",
            "LeftHand.Thumb.Spread",
            "LeftHand.Thumb.2 Stretched",
            "LeftHand.Thumb.3 Stretched",
            "LeftHand.Index.1 Stretched",
            "LeftHand.Index.Spread",
            "LeftHand.Index.2 Stretched",
            "LeftHand.Index.3 Stretched",
            "LeftHand.Middle.1 Stretched",
            "LeftHand.Middle.Spread",
            "LeftHand.Middle.2 Stretched",
            "LeftHand.Middle.3 Stretched",
            "LeftHand.Ring.1 Stretched",
            "LeftHand.Ring.Spread",
            "LeftHand.Ring.2 Stretched",
            "LeftHand.Ring.3 Stretched",
            "LeftHand.Little.1 Stretched",
            "LeftHand.Little.Spread",
            "LeftHand.Little.2 Stretched",
            "LeftHand.Little.3 Stretched",

            // 右手指
            "RightHand.Thumb.1 Stretched",
            "RightHand.Thumb.Spread",
            "RightHand.Thumb.2 Stretched",
            "RightHand.Thumb.3 Stretched",
            "RightHand.Index.1 Stretched",
            "RightHand.Index.Spread",
            "RightHand.Index.2 Stretched",
            "RightHand.Index.3 Stretched",
            "RightHand.Middle.1 Stretched",
            "RightHand.Middle.Spread",
            "RightHand.Middle.2 Stretched",
            "RightHand.Middle.3 Stretched",
            "RightHand.Ring.1 Stretched",
            "RightHand.Ring.Spread",
            "RightHand.Ring.2 Stretched",
            "RightHand.Ring.3 Stretched",
            "RightHand.Little.1 Stretched",
            "RightHand.Little.Spread",
            "RightHand.Little.2 Stretched",
            "RightHand.Little.3 Stretched"
        };

        /// <summary>
        /// マッスルプロパティ名の高速検索用HashSet
        /// </summary>
        private static readonly HashSet<string> MusclePropertyNameSet =
            new HashSet<string>(MusclePropertyNames);

        /// <summary>
        /// 指定されたEditorCurveBindingがHumanoidマッスルプロパティかどうかを判定する
        /// </summary>
        /// <param name="binding">判定対象のEditorCurveBinding</param>
        /// <returns>Humanoidマッスルプロパティの場合はtrue</returns>
        public bool IsMuscleProperty(EditorCurveBinding binding)
        {
            if (string.IsNullOrEmpty(binding.propertyName))
            {
                return false;
            }

            // マッスルカーブはルートオブジェクト（パスが空）でAnimator型に対して定義される
            if (!string.IsNullOrEmpty(binding.path))
            {
                return false;
            }

            return MusclePropertyNameSet.Contains(binding.propertyName);
        }

        /// <summary>
        /// AnimationClipからHumanoidマッスルカーブを検出する
        /// </summary>
        /// <param name="clip">検索対象のAnimationClip</param>
        /// <returns>マッスルカーブのバインディングとカーブのペアのリスト</returns>
        public List<CurveBindingPair> DetectMuscleCurves(AnimationClip clip)
        {
            var result = new List<CurveBindingPair>();

            if (clip == null)
            {
                return result;
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                if (IsMuscleProperty(binding))
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve != null)
                    {
                        result.Add(new CurveBindingPair(binding, curve));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// AnimationClipがHumanoidマッスルカーブを持っているかどうかを判定する
        /// </summary>
        /// <param name="clip">検索対象のAnimationClip</param>
        /// <returns>マッスルカーブを持っている場合はtrue</returns>
        public bool HasMuscleCurves(AnimationClip clip)
        {
            if (clip == null)
            {
                return false;
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                if (IsMuscleProperty(binding))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 全てのマッスルプロパティ名を取得する
        /// </summary>
        /// <returns>マッスルプロパティ名の配列</returns>
        public static string[] GetAllMusclePropertyNames()
        {
            return (string[])MusclePropertyNames.Clone();
        }
    }
}
