using System.Collections.Generic;
using AnimationMergeTool.Editor.Domain.Models;
using UnityEngine;

namespace AnimationMergeTool.Editor.Domain
{
    /// <summary>
    /// AnimationClipの統合処理を行うクラス
    /// 複数のClipInfoから単一のAnimationClipを生成する
    /// </summary>
    public class ClipMerger
    {
        /// <summary>
        /// 出力AnimationClipのフレームレート
        /// </summary>
        private float _frameRate = 60f;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ClipMerger()
        {
        }

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

        /// <summary>
        /// 複数のClipInfoを統合して単一のAnimationClipを生成する
        /// </summary>
        /// <param name="clipInfos">統合対象のClipInfoリスト</param>
        /// <returns>統合されたAnimationClip</returns>
        public AnimationClip Merge(List<ClipInfo> clipInfos)
        {
            if (clipInfos == null || clipInfos.Count == 0)
            {
                return null;
            }

            var resultClip = new AnimationClip
            {
                frameRate = _frameRate
            };

            // TODO: 3.2.2〜3.2.4で実装予定
            // - AnimationCurve取得機能
            // - 時間オフセット適用機能
            // - カーブ統合機能

            return resultClip;
        }
    }
}
