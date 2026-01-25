using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnimationMergeTool.Editor.Domain
{
    /// <summary>
    /// ルートモーションプロパティの検出を行うクラス
    /// RootT（位置）、RootQ（回転）プロパティを識別する
    /// </summary>
    public class RootMotionDetector
    {
        /// <summary>
        /// ルートモーション位置のプロパティ名プレフィックス
        /// </summary>
        private const string RootPositionPrefix = "RootT";

        /// <summary>
        /// ルートモーション回転のプロパティ名プレフィックス
        /// </summary>
        private const string RootRotationPrefix = "RootQ";

        /// <summary>
        /// ルートモーション位置のプロパティ名（コンポーネント付き）
        /// </summary>
        private static readonly string[] RootPositionProperties = new[]
        {
            "RootT.x",
            "RootT.y",
            "RootT.z"
        };

        /// <summary>
        /// ルートモーション回転のプロパティ名（コンポーネント付き）
        /// </summary>
        private static readonly string[] RootRotationProperties = new[]
        {
            "RootQ.x",
            "RootQ.y",
            "RootQ.z",
            "RootQ.w"
        };

        /// <summary>
        /// 指定されたEditorCurveBindingがルートモーションプロパティかどうかを判定する
        /// </summary>
        /// <param name="binding">判定対象のEditorCurveBinding</param>
        /// <returns>ルートモーションプロパティの場合はtrue</returns>
        public bool IsRootMotionProperty(EditorCurveBinding binding)
        {
            if (string.IsNullOrEmpty(binding.propertyName))
            {
                return false;
            }

            return IsRootPositionProperty(binding) || IsRootRotationProperty(binding);
        }

        /// <summary>
        /// 指定されたEditorCurveBindingがルートモーション位置プロパティかどうかを判定する
        /// </summary>
        /// <param name="binding">判定対象のEditorCurveBinding</param>
        /// <returns>ルートモーション位置プロパティの場合はtrue</returns>
        public bool IsRootPositionProperty(EditorCurveBinding binding)
        {
            if (string.IsNullOrEmpty(binding.propertyName))
            {
                return false;
            }

            // パスが空（ルートオブジェクト）でプロパティ名がRootTで始まる場合
            if (!string.IsNullOrEmpty(binding.path))
            {
                return false;
            }

            return binding.propertyName.StartsWith(RootPositionPrefix);
        }

        /// <summary>
        /// 指定されたEditorCurveBindingがルートモーション回転プロパティかどうかを判定する
        /// </summary>
        /// <param name="binding">判定対象のEditorCurveBinding</param>
        /// <returns>ルートモーション回転プロパティの場合はtrue</returns>
        public bool IsRootRotationProperty(EditorCurveBinding binding)
        {
            if (string.IsNullOrEmpty(binding.propertyName))
            {
                return false;
            }

            // パスが空（ルートオブジェクト）でプロパティ名がRootQで始まる場合
            if (!string.IsNullOrEmpty(binding.path))
            {
                return false;
            }

            return binding.propertyName.StartsWith(RootRotationPrefix);
        }

        /// <summary>
        /// AnimationClipからルートモーションカーブを検出する
        /// </summary>
        /// <param name="clip">検索対象のAnimationClip</param>
        /// <returns>ルートモーションカーブのバインディングとカーブのペアのリスト</returns>
        public List<CurveBindingPair> DetectRootMotionCurves(AnimationClip clip)
        {
            var result = new List<CurveBindingPair>();

            if (clip == null)
            {
                return result;
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                if (IsRootMotionProperty(binding))
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
        /// AnimationClipがルートモーションカーブを持っているかどうかを判定する
        /// </summary>
        /// <param name="clip">検索対象のAnimationClip</param>
        /// <returns>ルートモーションカーブを持っている場合はtrue</returns>
        public bool HasRootMotionCurves(AnimationClip clip)
        {
            if (clip == null)
            {
                return false;
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                if (IsRootMotionProperty(binding))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// AnimationClipからルートモーション位置カーブのみを検出する
        /// </summary>
        /// <param name="clip">検索対象のAnimationClip</param>
        /// <returns>ルートモーション位置カーブのバインディングとカーブのペアのリスト</returns>
        public List<CurveBindingPair> DetectRootPositionCurves(AnimationClip clip)
        {
            var result = new List<CurveBindingPair>();

            if (clip == null)
            {
                return result;
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                if (IsRootPositionProperty(binding))
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
        /// AnimationClipからルートモーション回転カーブのみを検出する
        /// </summary>
        /// <param name="clip">検索対象のAnimationClip</param>
        /// <returns>ルートモーション回転カーブのバインディングとカーブのペアのリスト</returns>
        public List<CurveBindingPair> DetectRootRotationCurves(AnimationClip clip)
        {
            var result = new List<CurveBindingPair>();

            if (clip == null)
            {
                return result;
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                if (IsRootRotationProperty(binding))
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
    }
}
