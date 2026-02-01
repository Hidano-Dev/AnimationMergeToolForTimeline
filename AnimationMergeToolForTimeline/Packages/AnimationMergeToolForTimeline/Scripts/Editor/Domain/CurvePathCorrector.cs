using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnimationMergeTool.Editor.Domain
{
    /// <summary>
    /// AnimationClipのカーブパスがAnimatorのヒエラルキーで解決できない場合に
    /// ヒエラルキーを探索して正しいパスに自動補正するクラス
    /// </summary>
    public class CurvePathCorrector
    {
        /// <summary>
        /// パス補正の結果を保持する構造体
        /// </summary>
        public struct CorrectionResult
        {
            /// <summary>
            /// 補正後のカーブバインディングペアリスト
            /// </summary>
            public List<CurveBindingPair> CorrectedPairs;

            /// <summary>
            /// 補正されたパスの件数
            /// </summary>
            public int CorrectedCount;

            /// <summary>
            /// 候補が複数あり補正をスキップした件数
            /// </summary>
            public int AmbiguousCount;

            /// <summary>
            /// 候補が見つからなかった件数
            /// </summary>
            public int NotFoundCount;
        }

        /// <summary>
        /// カーブリストのパスをAnimatorのヒエラルキーに基づいて補正する
        /// </summary>
        /// <param name="pairs">カーブバインディングペアリスト</param>
        /// <param name="animatorTransform">AnimatorのTransform</param>
        /// <returns>補正結果</returns>
        public CorrectionResult CorrectPaths(List<CurveBindingPair> pairs, Transform animatorTransform)
        {
            var result = new CorrectionResult
            {
                CorrectedPairs = new List<CurveBindingPair>(),
                CorrectedCount = 0,
                AmbiguousCount = 0,
                NotFoundCount = 0
            };

            if (pairs == null)
            {
                return result;
            }

            if (animatorTransform == null)
            {
                result.CorrectedPairs = new List<CurveBindingPair>(pairs);
                return result;
            }

            // 同一パスの再計算を回避するためのキャッシュ
            // value が null の場合はパスが既に解決可能であることを意味する
            var pathCorrectionCache = new Dictionary<string, string>();

            foreach (var pair in pairs)
            {
                // 補正対象外のカーブタイプ
                if (!IsTargetCurveType(pair.Binding))
                {
                    result.CorrectedPairs.Add(pair);
                    continue;
                }

                // 空パス（Animatorルート自体）はそのまま
                if (string.IsNullOrEmpty(pair.Binding.path))
                {
                    result.CorrectedPairs.Add(pair);
                    continue;
                }

                var originalPath = pair.Binding.path;

                // キャッシュヒット
                if (pathCorrectionCache.TryGetValue(originalPath, out var cachedPath))
                {
                    if (cachedPath == null)
                    {
                        // パスは既に解決可能
                        result.CorrectedPairs.Add(pair);
                    }
                    else
                    {
                        // キャッシュされた補正パスを適用
                        result.CorrectedPairs.Add(CreateCorrectedPair(pair, cachedPath));
                        result.CorrectedCount++;
                    }
                    continue;
                }

                // パスが既に解決可能か確認
                if (CanResolvePath(animatorTransform, originalPath))
                {
                    pathCorrectionCache[originalPath] = null;
                    result.CorrectedPairs.Add(pair);
                    continue;
                }

                // パスからリーフ名を抽出（最後の "/" 以降）
                var leafName = ExtractLeafName(originalPath);

                // リーフ名でTransformを探索
                var candidates = FindTransformsByLeafName(animatorTransform, leafName);

                if (candidates.Count == 0)
                {
                    Debug.LogWarning(
                        $"[AnimationMergeTool] パス補正: パス \"{originalPath}\" に一致するTransformが見つかりませんでした");
                    pathCorrectionCache[originalPath] = null;
                    result.CorrectedPairs.Add(pair);
                    result.NotFoundCount++;
                }
                else if (candidates.Count == 1)
                {
                    var correctedPath = GetRelativePath(animatorTransform, candidates[0]);
                    Debug.Log($"[AnimationMergeTool] パス補正: \"{originalPath}\" → \"{correctedPath}\"");
                    pathCorrectionCache[originalPath] = correctedPath;
                    result.CorrectedPairs.Add(CreateCorrectedPair(pair, correctedPath));
                    result.CorrectedCount++;
                }
                else
                {
                    // 候補が複数
                    var candidatePaths = new List<string>();
                    foreach (var candidate in candidates)
                    {
                        candidatePaths.Add(GetRelativePath(animatorTransform, candidate));
                    }
                    Debug.LogWarning(
                        $"[AnimationMergeTool] パス補正: パス \"{originalPath}\" に複数の候補が見つかりました [{string.Join(", ", candidatePaths)}]。補正をスキップします");
                    pathCorrectionCache[originalPath] = null;
                    result.CorrectedPairs.Add(pair);
                    result.AmbiguousCount++;
                }
            }

            // 補正が1件以上あった場合のみサマリーログを出力
            if (result.CorrectedCount > 0 || result.AmbiguousCount > 0 || result.NotFoundCount > 0)
            {
                Debug.Log(
                    $"[AnimationMergeTool] パス補正完了: 補正 {result.CorrectedCount}件, スキップ（曖昧） {result.AmbiguousCount}件, 未発見 {result.NotFoundCount}件");
            }

            return result;
        }

        /// <summary>
        /// パスがAnimator配下で解決可能か判定する
        /// </summary>
        /// <param name="animatorTransform">AnimatorのTransform</param>
        /// <param name="path">パス</param>
        /// <returns>解決可能な場合true</returns>
        public bool CanResolvePath(Transform animatorTransform, string path)
        {
            if (animatorTransform == null || string.IsNullOrEmpty(path))
            {
                return false;
            }

            return animatorTransform.Find(path) != null;
        }

        /// <summary>
        /// リーフ名（Transformの名前）でAnimator配下を再帰検索する
        /// </summary>
        /// <param name="root">検索ルートのTransform</param>
        /// <param name="leafName">検索するTransformの名前</param>
        /// <returns>一致するTransformのリスト</returns>
        public List<Transform> FindTransformsByLeafName(Transform root, string leafName)
        {
            var results = new List<Transform>();
            if (root == null || string.IsNullOrEmpty(leafName))
            {
                return results;
            }

            FindTransformsByLeafNameRecursive(root, leafName, results);
            return results;
        }

        /// <summary>
        /// AnimatorルートからTransformへの相対パスを計算する
        /// </summary>
        /// <param name="root">AnimatorルートのTransform</param>
        /// <param name="target">対象のTransform</param>
        /// <returns>相対パス</returns>
        public string GetRelativePath(Transform root, Transform target)
        {
            if (root == null || target == null || root == target)
            {
                return "";
            }

            var path = target.name;
            var current = target.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            // rootに到達できなかった場合はそのまま返す
            return path;
        }

        /// <summary>
        /// 補正対象のカーブタイプか判定する（Transform / BlendShape）
        /// </summary>
        /// <param name="binding">EditorCurveBinding</param>
        /// <returns>補正対象の場合true</returns>
        public bool IsTargetCurveType(EditorCurveBinding binding)
        {
            // Transformカーブは補正対象
            if (binding.type == typeof(Transform))
            {
                return true;
            }

            // SkinnedMeshRendererのBlendShapeカーブは補正対象
            if (binding.type == typeof(SkinnedMeshRenderer) &&
                binding.propertyName.StartsWith("blendShape."))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// パスからリーフ名（最後の "/" 以降）を抽出する
        /// </summary>
        /// <param name="path">パス</param>
        /// <returns>リーフ名</returns>
        private string ExtractLeafName(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            var lastSlashIndex = path.LastIndexOf('/');
            return lastSlashIndex >= 0 ? path.Substring(lastSlashIndex + 1) : path;
        }

        /// <summary>
        /// リーフ名で再帰検索する内部メソッド
        /// </summary>
        /// <param name="current">現在のTransform</param>
        /// <param name="leafName">検索するTransformの名前</param>
        /// <param name="results">結果リスト</param>
        private void FindTransformsByLeafNameRecursive(Transform current, string leafName, List<Transform> results)
        {
            for (var i = 0; i < current.childCount; i++)
            {
                var child = current.GetChild(i);
                if (child.name == leafName)
                {
                    results.Add(child);
                }
                FindTransformsByLeafNameRecursive(child, leafName, results);
            }
        }

        /// <summary>
        /// 補正されたパスで新しいCurveBindingPairを作成する
        /// </summary>
        /// <param name="original">元のCurveBindingPair</param>
        /// <param name="correctedPath">補正後のパス</param>
        /// <returns>新しいCurveBindingPair</returns>
        private CurveBindingPair CreateCorrectedPair(CurveBindingPair original, string correctedPath)
        {
            var correctedBinding = new EditorCurveBinding
            {
                path = correctedPath,
                type = original.Binding.type,
                propertyName = original.Binding.propertyName
            };
            return new CurveBindingPair(correctedBinding, original.Curve);
        }
    }
}
