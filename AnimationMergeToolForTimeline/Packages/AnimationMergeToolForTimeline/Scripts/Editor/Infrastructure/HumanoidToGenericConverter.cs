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
