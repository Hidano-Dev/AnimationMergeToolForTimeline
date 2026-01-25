using System.Collections.Generic;
using UnityEngine;

namespace AnimationMergeTool.Editor.Domain.Models
{
    /// <summary>
    /// アニメーション結合処理の結果を保持するデータモデル
    /// </summary>
    public class MergeResult
    {
        /// <summary>
        /// 生成されたAnimationClipデータ
        /// </summary>
        public AnimationClip GeneratedClip { get; set; }

        /// <summary>
        /// バインド先Animator情報
        /// </summary>
        public Animator TargetAnimator { get; }

        /// <summary>
        /// 処理ログ
        /// </summary>
        public List<string> Logs { get; }

        /// <summary>
        /// 処理が成功したかどうか
        /// </summary>
        public bool IsSuccess => GeneratedClip != null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="targetAnimator">バインド先Animator</param>
        public MergeResult(Animator targetAnimator)
        {
            TargetAnimator = targetAnimator;
            Logs = new List<string>();
        }

        /// <summary>
        /// ログを追加する
        /// </summary>
        /// <param name="message">ログメッセージ</param>
        public void AddLog(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Logs.Add(message);
            }
        }

        /// <summary>
        /// エラーログを追加する
        /// </summary>
        /// <param name="errorMessage">エラーメッセージ</param>
        public void AddErrorLog(string errorMessage)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Logs.Add($"[Error] {errorMessage}");
            }
        }
    }
}
