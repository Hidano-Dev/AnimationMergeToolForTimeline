using UnityEditor;

namespace AnimationMergeTool.Editor.UI
{
    /// <summary>
    /// 進捗表示を管理するクラス
    /// UX-001: 処理中の進捗状況を表示
    /// UX-002: 処理結果をConsoleに出力
    /// </summary>
    public class ProgressDisplay
    {
        /// <summary>
        /// 進捗バーのタイトル
        /// </summary>
        private const string ProgressBarTitle = "Animation Merge Tool";

        /// <summary>
        /// 現在の進捗状態を表示しているかどうか
        /// </summary>
        public bool IsDisplaying { get; private set; }

        /// <summary>
        /// 現在の進捗値（0.0〜1.0）
        /// </summary>
        public float CurrentProgress { get; private set; }

        /// <summary>
        /// 現在の進捗メッセージ
        /// </summary>
        public string CurrentMessage { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ProgressDisplay()
        {
            IsDisplaying = false;
            CurrentProgress = 0f;
            CurrentMessage = string.Empty;
        }

        /// <summary>
        /// 進捗表示を開始する
        /// </summary>
        /// <param name="message">表示するメッセージ</param>
        public void Begin(string message)
        {
            IsDisplaying = true;
            CurrentProgress = 0f;
            CurrentMessage = message ?? string.Empty;
            EditorUtility.DisplayProgressBar(ProgressBarTitle, CurrentMessage, CurrentProgress);
        }

        /// <summary>
        /// 進捗を更新する
        /// </summary>
        /// <param name="message">表示するメッセージ</param>
        /// <param name="progress">進捗値（0.0〜1.0）</param>
        public void Update(string message, float progress)
        {
            CurrentMessage = message ?? string.Empty;
            CurrentProgress = UnityEngine.Mathf.Clamp01(progress);

            if (IsDisplaying)
            {
                EditorUtility.DisplayProgressBar(ProgressBarTitle, CurrentMessage, CurrentProgress);
            }
        }

        /// <summary>
        /// 進捗表示を終了する
        /// </summary>
        public void End()
        {
            IsDisplaying = false;
            CurrentProgress = 0f;
            CurrentMessage = string.Empty;
            EditorUtility.ClearProgressBar();
        }
    }
}
