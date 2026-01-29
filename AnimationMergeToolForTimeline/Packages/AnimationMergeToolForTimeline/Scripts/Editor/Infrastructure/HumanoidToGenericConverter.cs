using System;
using System.Collections.Generic;
using AnimationMergeTool.Editor.Domain.Models;
using UnityEditor;
using UnityEngine;

namespace AnimationMergeTool.Editor.Infrastructure
{
    /// <summary>
    /// HumanoidアニメーションをGeneric形式に変換するクラス
    /// タスク P14-004: ボーン名→Transformパス変換の実装
    /// </summary>
    public class HumanoidToGenericConverter
    {
        /// <summary>
        /// HumanBodyBonesからTransformパスを取得する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="bone">HumanBodyBones列挙値</param>
        /// <returns>Animator相対のTransformパス（ボーンが見つからない場合はnull）</returns>
        public string GetTransformPath(Animator animator, HumanBodyBones bone)
        {
            // nullチェック
            if (animator == null)
            {
                return null;
            }

            // Humanoidリグでない場合はnull
            if (!animator.isHuman)
            {
                return null;
            }

            // LastBoneは無効な値
            if (bone == HumanBodyBones.LastBone)
            {
                return null;
            }

            // HumanBodyBonesからTransformを取得
            Transform boneTransform = animator.GetBoneTransform(bone);
            if (boneTransform == null)
            {
                return null;
            }

            // Animatorからの相対パスを構築
            return BuildRelativePath(animator.transform, boneTransform);
        }

        /// <summary>
        /// AnimationClipがHumanoid形式かどうかを判定する
        /// </summary>
        /// <param name="clip">判定対象のAnimationClip</param>
        /// <returns>Humanoid形式の場合はtrue</returns>
        public bool IsHumanoidClip(AnimationClip clip)
        {
            if (clip == null)
            {
                return false;
            }

            // clip.isHumanMotionプロパティを使用
            return clip.isHumanMotion;
        }

        /// <summary>
        /// マッスルカーブをRotationカーブに変換する
        /// タスク P14-006: マッスルカーブ→Rotation変換の実装
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="humanoidClip">変換元のAnimationClip</param>
        /// <returns>変換されたRotationカーブのリスト</returns>
        public List<TransformCurveData> ConvertMuscleCurvesToRotation(
            Animator animator,
            AnimationClip humanoidClip)
        {
            var result = new List<TransformCurveData>();

            // nullチェック
            if (animator == null || humanoidClip == null)
            {
                return result;
            }

            // Humanoidリグでない場合は空のリストを返す
            if (!animator.isHuman)
            {
                return result;
            }

            // フレームレートを取得（0以下の場合はデフォルト60fps）
            float frameRate = humanoidClip.frameRate;
            if (frameRate <= 0)
            {
                frameRate = 60f;
            }

            float duration = humanoidClip.length;
            float sampleInterval = 1.0f / frameRate;

            // 非常に短いクリップの場合
            if (duration <= 0)
            {
                return result;
            }

            // 各ボーンのカーブを準備
            var boneRotationCurves = new Dictionary<HumanBodyBones, RotationCurveSet>();

            // 有効なボーンを列挙
            foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone)
                {
                    continue;
                }

                Transform boneTransform = animator.GetBoneTransform(bone);
                if (boneTransform == null)
                {
                    continue;
                }

                boneRotationCurves[bone] = new RotationCurveSet();
            }

            // 各フレームをサンプリング
            for (float time = 0; time <= duration + sampleInterval * 0.5f; time += sampleInterval)
            {
                // 最終フレームを超えないようにクランプ
                float sampleTime = Mathf.Min(time, duration);

                // クリップをサンプリング
                humanoidClip.SampleAnimation(animator.gameObject, sampleTime);

                // 各ボーンの回転を記録
                foreach (var kvp in boneRotationCurves)
                {
                    Transform boneTransform = animator.GetBoneTransform(kvp.Key);
                    if (boneTransform == null)
                    {
                        continue;
                    }

                    Quaternion rotation = boneTransform.localRotation;
                    kvp.Value.AddKey(sampleTime, rotation);
                }
            }

            // カーブをTransformCurveDataに変換
            foreach (var kvp in boneRotationCurves)
            {
                string path = GetTransformPath(animator, kvp.Key);
                if (string.IsNullOrEmpty(path) && kvp.Key != HumanBodyBones.Hips)
                {
                    // Hips以外でパスが空の場合はスキップ
                    // Hipsはルート直下の場合があり空パスになり得る
                    continue;
                }

                var curves = kvp.Value.ToTransformCurveDataList(path ?? string.Empty);
                result.AddRange(curves);
            }

            return result;
        }

        /// <summary>
        /// Quaternionカーブを管理する内部クラス
        /// </summary>
        private class RotationCurveSet
        {
            public AnimationCurve X { get; } = new AnimationCurve();
            public AnimationCurve Y { get; } = new AnimationCurve();
            public AnimationCurve Z { get; } = new AnimationCurve();
            public AnimationCurve W { get; } = new AnimationCurve();

            public void AddKey(float time, Quaternion rotation)
            {
                X.AddKey(time, rotation.x);
                Y.AddKey(time, rotation.y);
                Z.AddKey(time, rotation.z);
                W.AddKey(time, rotation.w);
            }

            public List<TransformCurveData> ToTransformCurveDataList(string path)
            {
                return new List<TransformCurveData>
                {
                    new TransformCurveData(path, "localRotation.x", X, TransformCurveType.Rotation),
                    new TransformCurveData(path, "localRotation.y", Y, TransformCurveType.Rotation),
                    new TransformCurveData(path, "localRotation.z", Z, TransformCurveType.Rotation),
                    new TransformCurveData(path, "localRotation.w", W, TransformCurveType.Rotation)
                };
            }
        }

        /// <summary>
        /// ルートモーションカーブをTransformカーブに変換する
        /// タスク P14-007: ルートモーション変換のテスト作成 / P14-008: ルートモーション変換の実装
        /// </summary>
        /// <param name="humanoidClip">変換元のAnimationClip</param>
        /// <param name="rootBonePath">ルートボーンのパス</param>
        /// <returns>変換されたルートモーションカーブのリスト</returns>
        public List<TransformCurveData> ConvertRootMotionCurves(
            AnimationClip humanoidClip,
            string rootBonePath)
        {
            var result = new List<TransformCurveData>();

            // nullチェック
            if (humanoidClip == null || rootBonePath == null)
            {
                return result;
            }

            var bindings = AnimationUtility.GetCurveBindings(humanoidClip);

            foreach (var binding in bindings)
            {
                // ルートモーションのパスは空文字
                if (!string.IsNullOrEmpty(binding.path))
                {
                    continue;
                }

                var curve = AnimationUtility.GetEditorCurve(humanoidClip, binding);
                if (curve == null)
                {
                    continue;
                }

                // プロパティ名の変換
                string newPropertyName = ConvertRootMotionPropertyName(binding.propertyName);
                if (newPropertyName == null)
                {
                    continue;
                }

                var curveType = newPropertyName.StartsWith("localPosition")
                    ? TransformCurveType.Position
                    : TransformCurveType.Rotation;

                result.Add(new TransformCurveData(
                    rootBonePath,
                    newPropertyName,
                    curve,
                    curveType));
            }

            return result;
        }

        /// <summary>
        /// ルートモーションプロパティ名をTransformプロパティ名に変換する
        /// </summary>
        /// <param name="propertyName">ルートモーションプロパティ名</param>
        /// <returns>変換後のプロパティ名（変換できない場合はnull）</returns>
        private string ConvertRootMotionPropertyName(string propertyName)
        {
            switch (propertyName)
            {
                case "RootT.x": return "localPosition.x";
                case "RootT.y": return "localPosition.y";
                case "RootT.z": return "localPosition.z";
                case "RootQ.x": return "localRotation.x";
                case "RootQ.y": return "localRotation.y";
                case "RootQ.z": return "localRotation.z";
                case "RootQ.w": return "localRotation.w";
                default: return null;
            }
        }

        /// <summary>
        /// ルートからターゲットまでの相対パスを構築する
        /// </summary>
        /// <param name="root">基準となるTransform</param>
        /// <param name="target">パスを取得するTransform</param>
        /// <returns>相対パス（ターゲットがルート自身の場合は空文字、ルートの子孫でない場合はnull）</returns>
        private string BuildRelativePath(Transform root, Transform target)
        {
            if (target == root)
            {
                return string.Empty;
            }

            var pathParts = new System.Collections.Generic.List<string>();
            Transform current = target;

            while (current != null && current != root)
            {
                pathParts.Insert(0, current.name);
                current = current.parent;
            }

            // rootの子でなければnullを返す
            if (current == null)
            {
                return null;
            }

            return string.Join("/", pathParts);
        }
    }
}
