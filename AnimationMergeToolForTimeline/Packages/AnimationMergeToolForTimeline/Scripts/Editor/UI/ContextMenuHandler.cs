using System;
using System.Linq;
using AnimationMergeTool.Editor.Application;
using AnimationMergeTool.Editor.Domain.Models;
using AnimationMergeTool.Editor.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnimationMergeTool.Editor.UI
{
    /// <summary>
    /// コンテキストメニューからの実行を処理するハンドラクラス
    /// FR-001: HierarchyビューでPlayableDirectorを右クリックした際のコンテキストメニューから実行可能
    /// FR-002: ProjectビューでTimelineAssetを右クリックした際のコンテキストメニューから実行可能
    /// FR-003: 複数選択された場合、選択された各アセットに対して順次処理を実行
    /// </summary>
    public static class ContextMenuHandler
    {
        /// <summary>
        /// メニュー項目のパス（Hierarchyビュー）
        /// </summary>
        public const string HierarchyMenuPath = "GameObject/Animation Merge Tool/Merge Timeline Animations";

        /// <summary>
        /// メニュー項目のパス（Projectビュー）
        /// </summary>
        public const string AssetsMenuPath = "Assets/Animation Merge Tool/Merge Timeline Animations";

        /// <summary>
        /// FBXエクスポートメニュー項目のパス（Hierarchyビュー）
        /// </summary>
        public const string HierarchyFbxMenuPath = "GameObject/Animation Merge Tool/Export as FBX";

        /// <summary>
        /// FBXエクスポートメニュー項目のパス（Projectビュー）
        /// </summary>
        public const string AssetsFbxMenuPath = "Assets/Animation Merge Tool/Export as FBX";

        /// <summary>
        /// メニューの優先順位
        /// </summary>
        private const int MenuPriority = 100;

        /// <summary>
        /// FBXメニューの優先順位
        /// </summary>
        private const int FbxMenuPriority = 101;

        /// <summary>
        /// AnimationMergeServiceインスタンス（テスト時に差し替え可能）
        /// </summary>
        internal static AnimationMergeService ServiceInstance { get; set; }

        /// <summary>
        /// サービスインスタンスを取得または作成する
        /// </summary>
        private static AnimationMergeService GetService()
        {
            return ServiceInstance ?? new AnimationMergeService();
        }

        /// <summary>
        /// 選択されたPlayableDirectorの配列を取得する
        /// </summary>
        /// <returns>選択されたPlayableDirectorの配列</returns>
        public static PlayableDirector[] GetSelectedPlayableDirectors()
        {
            var selectedObjects = Selection.gameObjects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                return Array.Empty<PlayableDirector>();
            }

            return selectedObjects
                .Select(obj => obj.GetComponent<PlayableDirector>())
                .Where(director => director != null)
                .ToArray();
        }

        /// <summary>
        /// 選択されたTimelineAssetの配列を取得する
        /// </summary>
        /// <returns>選択されたTimelineAssetの配列</returns>
        public static TimelineAsset[] GetSelectedTimelineAssets()
        {
            var selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                return Array.Empty<TimelineAsset>();
            }

            return selectedObjects
                .OfType<TimelineAsset>()
                .ToArray();
        }

        /// <summary>
        /// Hierarchyビューメニューの有効状態を判定する
        /// </summary>
        /// <returns>PlayableDirectorが選択されている場合はtrue</returns>
        public static bool CanExecuteFromHierarchy()
        {
            return GetSelectedPlayableDirectors().Length > 0;
        }

        /// <summary>
        /// Projectビューメニューの有効状態を判定する
        /// </summary>
        /// <returns>TimelineAssetが選択されている場合はtrue</returns>
        public static bool CanExecuteFromProject()
        {
            return GetSelectedTimelineAssets().Length > 0;
        }

        /// <summary>
        /// 選択されたPlayableDirectorに対してマージ処理を実行する
        /// </summary>
        /// <param name="directors">処理対象のPlayableDirector配列</param>
        /// <returns>処理に成功した場合はtrue</returns>
        public static bool ExecuteForPlayableDirectors(PlayableDirector[] directors)
        {
            if (directors == null || directors.Length == 0)
            {
                Debug.LogError("[AnimationMergeTool] 処理対象のPlayableDirectorがありません。");
                return false;
            }

            var service = GetService();
            var successCount = 0;

            foreach (var director in directors)
            {
                if (director == null)
                {
                    continue;
                }

                var results = service.MergeFromPlayableDirector(director);
                if (results != null && results.Count > 0)
                {
                    successCount++;
                }
            }

            return successCount > 0;
        }

        /// <summary>
        /// Hierarchyビューのコンテキストメニューからマージ処理を実行する
        /// FR-001: HierarchyビューでPlayableDirectorを右クリックした際のコンテキストメニューから実行可能
        /// </summary>
        [MenuItem(HierarchyMenuPath, false, MenuPriority)]
        private static void ExecuteFromHierarchyMenu()
        {
            var directors = GetSelectedPlayableDirectors();
            ExecuteForPlayableDirectors(directors);
        }

        /// <summary>
        /// Hierarchyビューのコンテキストメニューの有効状態を判定する
        /// PlayableDirectorが選択されている場合のみ有効
        /// </summary>
        /// <returns>PlayableDirectorが選択されている場合はtrue</returns>
        [MenuItem(HierarchyMenuPath, true)]
        private static bool ValidateExecuteFromHierarchyMenu()
        {
            return CanExecuteFromHierarchy();
        }

        /// <summary>
        /// Projectビューのコンテキストメニューからマージ処理を実行する
        /// FR-002: ProjectビューでTimelineAssetを右クリックした際のコンテキストメニューから実行可能
        /// </summary>
        [MenuItem(AssetsMenuPath, false, MenuPriority)]
        private static void ExecuteFromProjectMenu()
        {
            var timelineAssets = GetSelectedTimelineAssets();
            ExecuteForTimelineAssets(timelineAssets);
        }

        /// <summary>
        /// Projectビューのコンテキストメニューの有効状態を判定する
        /// TimelineAssetが選択されている場合のみ有効
        /// </summary>
        /// <returns>TimelineAssetが選択されている場合はtrue</returns>
        [MenuItem(AssetsMenuPath, true)]
        private static bool ValidateExecuteFromProjectMenu()
        {
            return CanExecuteFromProject();
        }

        /// <summary>
        /// 選択されたTimelineAssetに対してマージ処理を実行する
        /// </summary>
        /// <param name="timelineAssets">処理対象のTimelineAsset配列</param>
        /// <returns>処理に成功した場合はtrue</returns>
        public static bool ExecuteForTimelineAssets(TimelineAsset[] timelineAssets)
        {
            if (timelineAssets == null || timelineAssets.Length == 0)
            {
                Debug.LogError("[AnimationMergeTool] 処理対象のTimelineAssetがありません。");
                return false;
            }

            var service = GetService();
            var successCount = 0;

            foreach (var timelineAsset in timelineAssets)
            {
                if (timelineAsset == null)
                {
                    continue;
                }

                var results = service.MergeFromTimelineAsset(timelineAsset);
                if (results != null && results.Count > 0)
                {
                    successCount++;
                }
            }

            return successCount > 0;
        }

        #region FBXエクスポートメニュー

        /// <summary>
        /// HierarchyビューのFBXエクスポートメニューの有効状態を判定する
        /// </summary>
        /// <returns>PlayableDirectorが選択されている場合はtrue</returns>
        public static bool CanExportFbxFromHierarchy()
        {
            return GetSelectedPlayableDirectors().Length > 0;
        }

        /// <summary>
        /// ProjectビューのFBXエクスポートメニューの有効状態を判定する
        /// </summary>
        /// <returns>TimelineAssetが選択されている場合はtrue</returns>
        public static bool CanExportFbxFromProject()
        {
            return GetSelectedTimelineAssets().Length > 0;
        }

        /// <summary>
        /// 選択されたPlayableDirectorに対してFBXエクスポート処理を実行する
        /// </summary>
        /// <param name="directors">処理対象のPlayableDirector配列</param>
        /// <returns>処理に成功した場合はtrue</returns>
        public static bool ExportFbxForPlayableDirectors(PlayableDirector[] directors)
        {
            if (directors == null || directors.Length == 0)
            {
                Debug.LogError("[AnimationMergeTool] FBXエクスポート対象のPlayableDirectorがありません。");
                return false;
            }

            // FBX Exporterパッケージのチェック
            if (!FbxPackageChecker.CheckPackageAndShowDialogIfMissing())
            {
                return false;
            }

            var service = GetService();
            var exporter = new FbxAnimationExporter();
            var successCount = 0;

            foreach (var director in directors)
            {
                if (director == null)
                {
                    continue;
                }

                // マージ処理を実行
                var mergeResults = service.MergeFromPlayableDirector(director);
                if (mergeResults == null || mergeResults.Count == 0)
                {
                    continue;
                }

                // 各マージ結果をFBXとしてエクスポート
                foreach (var result in mergeResults)
                {
                    if (result.GeneratedClip == null)
                    {
                        continue;
                    }

                    // FbxExportDataを作成
                    var exportData = CreateFbxExportData(result);
                    if (exportData == null || !exporter.CanExport(exportData))
                    {
                        continue;
                    }

                    // 出力パスを生成
                    var outputPath = GenerateFbxOutputPath(director.name, result.TargetAnimator);
                    if (string.IsNullOrEmpty(outputPath))
                    {
                        continue;
                    }

                    // FBXエクスポート実行
                    if (exporter.Export(exportData, outputPath))
                    {
                        successCount++;
                    }
                }
            }

            return successCount > 0;
        }

        /// <summary>
        /// 選択されたTimelineAssetに対してFBXエクスポート処理を実行する
        /// </summary>
        /// <param name="timelineAssets">処理対象のTimelineAsset配列</param>
        /// <returns>処理に成功した場合はtrue</returns>
        public static bool ExportFbxForTimelineAssets(TimelineAsset[] timelineAssets)
        {
            if (timelineAssets == null || timelineAssets.Length == 0)
            {
                Debug.LogError("[AnimationMergeTool] FBXエクスポート対象のTimelineAssetがありません。");
                return false;
            }

            // FBX Exporterパッケージのチェック
            if (!FbxPackageChecker.CheckPackageAndShowDialogIfMissing())
            {
                return false;
            }

            var service = GetService();
            var exporter = new FbxAnimationExporter();
            var successCount = 0;

            foreach (var timelineAsset in timelineAssets)
            {
                if (timelineAsset == null)
                {
                    continue;
                }

                // マージ処理を実行
                var mergeResults = service.MergeFromTimelineAsset(timelineAsset);
                if (mergeResults == null || mergeResults.Count == 0)
                {
                    continue;
                }

                // 各マージ結果をFBXとしてエクスポート
                foreach (var result in mergeResults)
                {
                    if (result.GeneratedClip == null)
                    {
                        continue;
                    }

                    // FbxExportDataを作成
                    var exportData = CreateFbxExportData(result);
                    if (exportData == null || !exporter.CanExport(exportData))
                    {
                        continue;
                    }

                    // 出力パスを生成
                    var outputPath = GenerateFbxOutputPath(timelineAsset.name, result.TargetAnimator);
                    if (string.IsNullOrEmpty(outputPath))
                    {
                        continue;
                    }

                    // FBXエクスポート実行
                    if (exporter.Export(exportData, outputPath))
                    {
                        successCount++;
                    }
                }
            }

            return successCount > 0;
        }

        /// <summary>
        /// HierarchyビューのコンテキストメニューからFBXエクスポート処理を実行する
        /// </summary>
        [MenuItem(HierarchyFbxMenuPath, false, FbxMenuPriority)]
        private static void ExecuteFbxExportFromHierarchyMenu()
        {
            var directors = GetSelectedPlayableDirectors();
            ExportFbxForPlayableDirectors(directors);
        }

        /// <summary>
        /// HierarchyビューのFBXエクスポートメニューの有効状態を判定する
        /// PlayableDirectorが選択されている場合のみ有効
        /// </summary>
        /// <returns>PlayableDirectorが選択されている場合はtrue</returns>
        [MenuItem(HierarchyFbxMenuPath, true)]
        private static bool ValidateExecuteFbxExportFromHierarchyMenu()
        {
            return CanExportFbxFromHierarchy();
        }

        /// <summary>
        /// ProjectビューのコンテキストメニューからFBXエクスポート処理を実行する
        /// </summary>
        [MenuItem(AssetsFbxMenuPath, false, FbxMenuPriority)]
        private static void ExecuteFbxExportFromProjectMenu()
        {
            var timelineAssets = GetSelectedTimelineAssets();
            ExportFbxForTimelineAssets(timelineAssets);
        }

        /// <summary>
        /// ProjectビューのFBXエクスポートメニューの有効状態を判定する
        /// TimelineAssetが選択されている場合のみ有効
        /// </summary>
        /// <returns>TimelineAssetが選択されている場合はtrue</returns>
        [MenuItem(AssetsFbxMenuPath, true)]
        private static bool ValidateExecuteFbxExportFromProjectMenu()
        {
            return CanExportFbxFromProject();
        }

        /// <summary>
        /// MergeResultからFbxExportDataを作成する
        /// </summary>
        /// <param name="result">マージ結果</param>
        /// <returns>FBXエクスポートデータ</returns>
        private static FbxExportData CreateFbxExportData(MergeResult result)
        {
            if (result == null || result.GeneratedClip == null)
            {
                return null;
            }

            var animator = result.TargetAnimator;
            var isHumanoid = animator != null && animator.isHuman;

            // スケルトン情報を取得（Phase 13で詳細実装予定）
            SkeletonData skeleton = null;
            if (animator != null)
            {
                var rootBone = animator.transform;
                var bones = new System.Collections.Generic.List<Transform> { rootBone };
                skeleton = new SkeletonData(rootBone, bones);
            }

            // Transformカーブ情報（Phase 13で詳細実装予定）
            var transformCurves = new System.Collections.Generic.List<TransformCurveData>();

            // BlendShapeカーブ情報（Phase 15で詳細実装予定）
            var blendShapeCurves = new System.Collections.Generic.List<BlendShapeCurveData>();

            return new FbxExportData(
                animator,
                result.GeneratedClip,
                skeleton,
                transformCurves,
                blendShapeCurves,
                isHumanoid
            );
        }

        /// <summary>
        /// FBX出力パスを生成する
        /// </summary>
        /// <param name="baseName">基本名</param>
        /// <param name="animator">対象Animator</param>
        /// <returns>出力パス</returns>
        private static string GenerateFbxOutputPath(string baseName, Animator animator)
        {
            var animatorName = animator != null ? animator.name : "NoAnimator";
            var fileName = $"{baseName}_{animatorName}_Merged.fbx";
            return $"Assets/{fileName}";
        }

        #endregion
    }
}
