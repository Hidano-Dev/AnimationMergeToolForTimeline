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
        /// マッスルカーブをRotation・Positionカーブに変換する
        /// タスク P14-006: マッスルカーブ→Rotation変換の実装
        /// Hipsボーンの場合はlocalPositionカーブも含む（Body位置）
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="humanoidClip">変換元のAnimationClip</param>
        /// <returns>変換されたRotation・Positionカーブのリスト</returns>
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

            // Hips用のPositionカーブを準備
            PositionCurveSet hipsPositionCurves = null;
            Transform hipsTransformRef = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hipsTransformRef != null)
            {
                hipsPositionCurves = new PositionCurveSet();
            }

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

            // Animatorの元の状態を保存（サンプリング後に復元するため）
            var animatorOriginalPos = animator.transform.localPosition;
            var animatorOriginalRot = animator.transform.localRotation;

            // Hipsの親のワールド座標系を参照として保存
            // SampleAnimationはルートモーション（RootT/RootQ）をAnimator.transformに適用するため、
            // HipsのlocalPositionにはルートモーションが含まれない。
            // Animator.transformを原点にリセットした状態でリファレンスを取得し、
            // 各フレームでHipsのワールド座標をこの参照空間に変換することで、
            // ルートモーション全体（シーンオフセット含む）を含んだ正しいlocalPositionが得られる。
            //
            // 注意: 以前はSampleAnimation(T=0)後にリファレンスを取得していたが、
            // この方法ではRootT(0)に含まれるシーンオフセットがリファレンスに含まれ、
            // 各フレームの結果から差し引かれてオフセットが消失していた。
            var hipsParentRefWorldToLocal = Matrix4x4.identity;
            var hipsParentRefWorldRotInverse = Quaternion.identity;
            if (hipsTransformRef != null)
            {
                Transform hipsParent = hipsTransformRef.parent;
                if (hipsParent != null)
                {
                    // Animatorの位置/回転を原点にリセットしてリファレンスを取得する。
                    // これにより、SampleAnimationで適用されるRootT/RootQ（シーンオフセット含む）が
                    // リファレンスに含まれず、出力にルートモーション全体が保持される。
                    animator.transform.localPosition = Vector3.zero;
                    animator.transform.localRotation = Quaternion.identity;
                    hipsParentRefWorldToLocal = hipsParent.worldToLocalMatrix;
                    hipsParentRefWorldRotInverse = Quaternion.Inverse(hipsParent.rotation);
                }
            }

            // マージ済みクリップの場合、isHumanMotion=falseのため
            // SampleAnimationがRootT/RootQをAnimator.transformに適用しない。
            // RootT/RootQカーブを事前に取得し、サンプリング後に手動で適用する。
            var rootTxCurve = AnimationUtility.GetEditorCurve(humanoidClip,
                EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x"));
            var rootTyCurve = AnimationUtility.GetEditorCurve(humanoidClip,
                EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.y"));
            var rootTzCurve = AnimationUtility.GetEditorCurve(humanoidClip,
                EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.z"));
            var rootQxCurve = AnimationUtility.GetEditorCurve(humanoidClip,
                EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.x"));
            var rootQyCurve = AnimationUtility.GetEditorCurve(humanoidClip,
                EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.y"));
            var rootQzCurve = AnimationUtility.GetEditorCurve(humanoidClip,
                EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.z"));
            var rootQwCurve = AnimationUtility.GetEditorCurve(humanoidClip,
                EditorCurveBinding.FloatCurve("", typeof(Animator), "RootQ.w"));
            bool hasRootTCurves = rootTxCurve != null || rootTyCurve != null || rootTzCurve != null;
            bool hasRootQCurves = rootQxCurve != null || rootQyCurve != null ||
                                  rootQzCurve != null || rootQwCurve != null;
            // Humanoidクリップの場合はSampleAnimationが自動でRootT/RootQを適用するため不要
            bool needsManualRootMotion = !humanoidClip.isHumanMotion &&
                                         (hasRootTCurves || hasRootQCurves);

            // 各フレームをサンプリング
            for (float time = 0; time <= duration + sampleInterval * 0.5f; time += sampleInterval)
            {
                // 最終フレームを超えないようにクランプ
                float sampleTime = Mathf.Min(time, duration);

                // クリップをサンプリング
                humanoidClip.SampleAnimation(animator.gameObject, sampleTime);

                // マージ済みクリップ（isHumanMotion=false）の場合、SampleAnimationが
                // RootT/RootQをAnimator.transformに適用しないため、手動で適用する
                if (needsManualRootMotion)
                {
                    if (hasRootTCurves)
                    {
                        animator.transform.localPosition = new Vector3(
                            rootTxCurve?.Evaluate(sampleTime) ?? 0f,
                            rootTyCurve?.Evaluate(sampleTime) ?? 0f,
                            rootTzCurve?.Evaluate(sampleTime) ?? 0f);
                    }
                    if (hasRootQCurves)
                    {
                        animator.transform.localRotation = new Quaternion(
                            rootQxCurve?.Evaluate(sampleTime) ?? 0f,
                            rootQyCurve?.Evaluate(sampleTime) ?? 0f,
                            rootQzCurve?.Evaluate(sampleTime) ?? 0f,
                            rootQwCurve?.Evaluate(sampleTime) ?? 1f);
                    }
                }

                // Hipsの位置を記録（ルートモーションを含む）
                if (hipsPositionCurves != null && hipsTransformRef != null)
                {
                    // Hipsのワールド座標位置を取得し、T=0時の親のローカル空間に変換する
                    // これによりルートモーション（Animator.transformに適用される）を含んだ
                    // 正しいlocalPositionが得られる
                    Vector3 hipsWorldPos = hipsTransformRef.position;
                    Vector3 hipsLocalPos = hipsParentRefWorldToLocal.MultiplyPoint3x4(hipsWorldPos);
                    hipsPositionCurves.AddKey(sampleTime, hipsLocalPos);
                }

                // 各ボーンの回転を記録
                foreach (var kvp in boneRotationCurves)
                {
                    Transform boneTransform = animator.GetBoneTransform(kvp.Key);
                    if (boneTransform == null)
                    {
                        continue;
                    }

                    Quaternion rotation;
                    if (kvp.Key == HumanBodyBones.Hips)
                    {
                        // Hipsの回転はルートモーション回転を含める必要がある
                        // ワールド回転をT=0時の親空間の回転に変換する
                        rotation = hipsParentRefWorldRotInverse * boneTransform.rotation;
                    }
                    else
                    {
                        rotation = boneTransform.localRotation;
                    }
                    kvp.Value.AddKey(sampleTime, rotation);
                }
            }

            // Animatorの状態を復元
            animator.transform.localPosition = animatorOriginalPos;
            animator.transform.localRotation = animatorOriginalRot;

            // Hips Positionカーブを結果に追加
            if (hipsPositionCurves != null)
            {
                string hipsPath = GetTransformPath(animator, HumanBodyBones.Hips) ?? string.Empty;
                result.AddRange(hipsPositionCurves.ToTransformCurveDataList(hipsPath));
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
        /// Positionカーブ（localPosition.x/y/z）を管理する内部クラス
        /// </summary>
        private class PositionCurveSet
        {
            public AnimationCurve X { get; } = new AnimationCurve();
            public AnimationCurve Y { get; } = new AnimationCurve();
            public AnimationCurve Z { get; } = new AnimationCurve();

            public void AddKey(float time, Vector3 position)
            {
                X.AddKey(time, position.x);
                Y.AddKey(time, position.y);
                Z.AddKey(time, position.z);
            }

            public List<TransformCurveData> ToTransformCurveDataList(string path)
            {
                return new List<TransformCurveData>
                {
                    new TransformCurveData(path, "localPosition.x", X, TransformCurveType.Position),
                    new TransformCurveData(path, "localPosition.y", Y, TransformCurveType.Position),
                    new TransformCurveData(path, "localPosition.z", Z, TransformCurveType.Position)
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
