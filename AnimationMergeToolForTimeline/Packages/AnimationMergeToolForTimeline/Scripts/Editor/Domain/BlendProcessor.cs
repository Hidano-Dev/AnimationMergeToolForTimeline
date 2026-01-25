using AnimationMergeTool.Editor.Domain.Models;
using UnityEngine;

namespace AnimationMergeTool.Editor.Domain
{
    /// <summary>
    /// EaseIn/EaseOutブレンド処理を行うクラス
    /// FR-050, FR-051 対応
    /// </summary>
    public class BlendProcessor
    {
        /// <summary>
        /// デフォルトのフレームレート
        /// </summary>
        private float _frameRate = 60f;

        /// <summary>
        /// フレームレートを設定する
        /// </summary>
        /// <param name="frameRate">フレームレート（fps）</param>
        public void SetFrameRate(float frameRate)
        {
            if (frameRate > 0)
            {
                _frameRate = frameRate;
            }
        }

        /// <summary>
        /// 現在のフレームレートを取得する
        /// </summary>
        /// <returns>フレームレート（fps）</returns>
        public float GetFrameRate()
        {
            return _frameRate;
        }
    }
}
