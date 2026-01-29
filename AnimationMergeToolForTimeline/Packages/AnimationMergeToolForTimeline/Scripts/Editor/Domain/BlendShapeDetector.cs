using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnimationMergeTool.Editor.Domain
{
    /// <summary>
    /// BlendShape（モーフターゲット）カーブの検出を行うクラス
    /// blendShape.プレフィックスを持つSkinnedMeshRendererのカーブを識別する
    /// </summary>
    public class BlendShapeDetector
    {
        /// <summary>
        /// BlendShapeカーブのプロパティ名プレフィックス
        /// </summary>
        public const string BlendShapePrefix = "blendShape.";

        /// <summary>
        /// 指定されたEditorCurveBindingがBlendShapeプロパティかどうかを判定する
        /// </summary>
        /// <param name="binding">判定対象のEditorCurveBinding</param>
        /// <returns>BlendShapeプロパティの場合はtrue</returns>
        public bool IsBlendShapeProperty(EditorCurveBinding binding)
        {
            if (string.IsNullOrEmpty(binding.propertyName))
            {
                return false;
            }

            // typeがSkinnedMeshRendererかチェック
            if (binding.type != typeof(SkinnedMeshRenderer))
            {
                return false;
            }

            // プロパティ名が"blendShape."で始まるかチェック
            return binding.propertyName.StartsWith(BlendShapePrefix);
        }

        /// <summary>
        /// AnimationClipからBlendShapeカーブを検出する
        /// </summary>
        /// <param name="clip">検索対象のAnimationClip</param>
        /// <returns>BlendShapeカーブのバインディングとカーブのペアのリスト</returns>
        public List<CurveBindingPair> DetectBlendShapeCurves(AnimationClip clip)
        {
            var result = new List<CurveBindingPair>();

            if (clip == null)
            {
                return result;
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                if (IsBlendShapeProperty(binding))
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
        /// AnimationClipがBlendShapeカーブを持っているかどうかを判定する
        /// </summary>
        /// <param name="clip">検索対象のAnimationClip</param>
        /// <returns>BlendShapeカーブを持っている場合はtrue</returns>
        public bool HasBlendShapeCurves(AnimationClip clip)
        {
            if (clip == null)
            {
                return false;
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                if (IsBlendShapeProperty(binding))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// EditorCurveBindingからBlendShape名を抽出する
        /// </summary>
        /// <param name="binding">BlendShapeプロパティのEditorCurveBinding</param>
        /// <returns>BlendShape名（"blendShape."プレフィックスを除いた部分）。BlendShapeプロパティでない場合はnull</returns>
        public string GetBlendShapeName(EditorCurveBinding binding)
        {
            if (!IsBlendShapeProperty(binding))
            {
                return null;
            }

            return binding.propertyName.Substring(BlendShapePrefix.Length);
        }
    }
}
