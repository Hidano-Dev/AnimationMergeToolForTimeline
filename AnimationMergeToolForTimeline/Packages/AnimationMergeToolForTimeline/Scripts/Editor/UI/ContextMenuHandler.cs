using AnimationMergeTool.Editor.Application;
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
        /// メニューの優先順位
        /// </summary>
        private const int MenuPriority = 100;

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
                return new PlayableDirector[0];
            }

            var directors = new System.Collections.Generic.List<PlayableDirector>();
            foreach (var obj in selectedObjects)
            {
                var director = obj.GetComponent<PlayableDirector>();
                if (director != null)
                {
                    directors.Add(director);
                }
            }

            return directors.ToArray();
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
                return new TimelineAsset[0];
            }

            var timelineAssets = new System.Collections.Generic.List<TimelineAsset>();
            foreach (var obj in selectedObjects)
            {
                if (obj is TimelineAsset timelineAsset)
                {
                    timelineAssets.Add(timelineAsset);
                }
            }

            return timelineAssets.ToArray();
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
    }
}
