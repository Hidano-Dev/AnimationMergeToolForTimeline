using System.Collections.Generic;
using UnityEngine;
using AnimationMergeTool.Editor.Domain.Models;

namespace AnimationMergeTool.Editor.Infrastructure
{
    /// <summary>
    /// Animatorからスケルトン（ボーン階層）を抽出するクラス
    /// タスク P13-003で実装予定
    /// </summary>
    public class SkeletonExtractor
    {
        /// <summary>
        /// Animatorからスケルトン情報を取得する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <returns>スケルトン情報（取得できない場合は空のSkeletonData）</returns>
        public SkeletonData Extract(Animator animator)
        {
            // P13-003で実装予定
            // 現在は空のSkeletonDataを返すスタブ実装
            if (animator == null)
            {
                return new SkeletonData(null, new List<Transform>());
            }

            var bones = new List<Transform>();
            Transform rootBone = null;

            // SkinnedMeshRendererからボーンを取得
            var skinnedMeshRenderers = animator.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var smr in skinnedMeshRenderers)
            {
                if (smr.bones != null)
                {
                    foreach (var bone in smr.bones)
                    {
                        if (bone != null && !bones.Contains(bone))
                        {
                            bones.Add(bone);
                        }
                    }
                }

                // ルートボーンを設定
                if (rootBone == null && smr.rootBone != null)
                {
                    rootBone = smr.rootBone;
                }
            }

            // ボーンがない場合はTransform階層から取得
            if (bones.Count == 0)
            {
                CollectBonesRecursive(animator.transform, bones);
            }

            // ルートボーンが未設定の場合、最も浅い階層のボーンを探す
            if (rootBone == null && bones.Count > 0)
            {
                rootBone = FindShallowestBone(bones, animator.transform);
            }

            // ボーンを階層順にソート
            if (rootBone != null && bones.Count > 0)
            {
                SortBonesByHierarchy(bones, rootBone);
            }

            return new SkeletonData(rootBone, bones);
        }

        /// <summary>
        /// ボーンのAnimator相対パスを取得する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="bone">パスを取得するボーン</param>
        /// <returns>Animator相対パス（Animator自身の場合は空文字）</returns>
        public string GetBonePath(Animator animator, Transform bone)
        {
            if (animator == null || bone == null)
            {
                return null;
            }

            if (bone == animator.transform)
            {
                return string.Empty;
            }

            var path = new List<string>();
            var current = bone;

            while (current != null && current != animator.transform)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }

            return string.Join("/", path);
        }

        /// <summary>
        /// Transform階層を再帰的に探索してボーンを収集する
        /// </summary>
        private void CollectBonesRecursive(Transform parent, List<Transform> bones)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);

                // MeshFilterを持たないTransformをボーン候補とする
                if (child.GetComponent<MeshFilter>() == null &&
                    child.GetComponent<SkinnedMeshRenderer>() == null)
                {
                    if (!bones.Contains(child))
                    {
                        bones.Add(child);
                    }
                }

                // 再帰的に子を探索
                CollectBonesRecursive(child, bones);
            }
        }

        /// <summary>
        /// 最も浅い階層のボーンを探す
        /// </summary>
        private Transform FindShallowestBone(List<Transform> bones, Transform root)
        {
            Transform shallowest = null;
            int minDepth = int.MaxValue;

            foreach (var bone in bones)
            {
                int depth = GetDepth(bone, root);
                if (depth < minDepth)
                {
                    minDepth = depth;
                    shallowest = bone;
                }
            }

            return shallowest;
        }

        /// <summary>
        /// Transformの深さを取得する
        /// </summary>
        private int GetDepth(Transform transform, Transform root)
        {
            int depth = 0;
            var current = transform;

            while (current != null && current != root)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        /// <summary>
        /// ボーンを階層順（幅優先）にソートする
        /// </summary>
        private void SortBonesByHierarchy(List<Transform> bones, Transform rootBone)
        {
            if (rootBone == null || bones.Count == 0)
            {
                return;
            }

            var sorted = new List<Transform>();
            var queue = new Queue<Transform>();
            queue.Enqueue(rootBone);

            // 幅優先探索で階層順に追加
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (bones.Contains(current) && !sorted.Contains(current))
                {
                    sorted.Add(current);
                }

                // 子を追加
                for (int i = 0; i < current.childCount; i++)
                {
                    var child = current.GetChild(i);
                    if (bones.Contains(child) || IsAncestorOfAnyBone(child, bones))
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            // 元のリストを更新
            bones.Clear();
            bones.AddRange(sorted);
        }

        /// <summary>
        /// 指定されたTransformがボーンリスト内のいずれかのボーンの祖先かどうかを判定する
        /// </summary>
        private bool IsAncestorOfAnyBone(Transform transform, List<Transform> bones)
        {
            foreach (var bone in bones)
            {
                var current = bone.parent;
                while (current != null)
                {
                    if (current == transform)
                    {
                        return true;
                    }
                    current = current.parent;
                }
            }
            return false;
        }
    }
}
