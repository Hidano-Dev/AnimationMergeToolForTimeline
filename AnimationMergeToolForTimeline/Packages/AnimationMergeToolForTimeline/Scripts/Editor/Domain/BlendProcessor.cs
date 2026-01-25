using AnimationMergeTool.Editor.Domain.Models;
using UnityEngine;
using UnityEngine.Timeline;

namespace AnimationMergeTool.Editor.Domain
{
    /// <summary>
    /// ブレンド情報を保持する構造体
    /// </summary>
    public struct BlendInfo
    {
        /// <summary>
        /// ブレンドインカーブ
        /// </summary>
        public AnimationCurve BlendInCurve;

        /// <summary>
        /// ブレンドアウトカーブ
        /// </summary>
        public AnimationCurve BlendOutCurve;

        /// <summary>
        /// EaseIn時間（秒）
        /// </summary>
        public double EaseInDuration;

        /// <summary>
        /// EaseOut時間（秒）
        /// </summary>
        public double EaseOutDuration;

        /// <summary>
        /// 有効なブレンド情報かどうか
        /// </summary>
        public bool IsValid => BlendInCurve != null || BlendOutCurve != null;

        /// <summary>
        /// EaseInが有効かどうか
        /// </summary>
        public bool HasEaseIn => EaseInDuration > 0 && BlendInCurve != null;

        /// <summary>
        /// EaseOutが有効かどうか
        /// </summary>
        public bool HasEaseOut => EaseOutDuration > 0 && BlendOutCurve != null;
    }

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

        /// <summary>
        /// TimelineClipからブレンド情報を取得する
        /// </summary>
        /// <param name="timelineClip">TimelineClip</param>
        /// <returns>ブレンド情報</returns>
        public BlendInfo GetBlendInfo(TimelineClip timelineClip)
        {
            if (timelineClip == null)
            {
                return new BlendInfo
                {
                    BlendInCurve = null,
                    BlendOutCurve = null,
                    EaseInDuration = 0,
                    EaseOutDuration = 0
                };
            }

            return new BlendInfo
            {
                BlendInCurve = timelineClip.mixInCurve,
                BlendOutCurve = timelineClip.mixOutCurve,
                EaseInDuration = timelineClip.easeInDuration,
                EaseOutDuration = timelineClip.easeOutDuration
            };
        }

        /// <summary>
        /// ClipInfoからブレンド情報を取得する
        /// </summary>
        /// <param name="clipInfo">ClipInfo</param>
        /// <returns>ブレンド情報</returns>
        public BlendInfo GetBlendInfo(ClipInfo clipInfo)
        {
            if (clipInfo == null || clipInfo.TimelineClip == null)
            {
                return new BlendInfo
                {
                    BlendInCurve = null,
                    BlendOutCurve = null,
                    EaseInDuration = 0,
                    EaseOutDuration = 0
                };
            }

            return new BlendInfo
            {
                BlendInCurve = clipInfo.BlendInCurve,
                BlendOutCurve = clipInfo.BlendOutCurve,
                EaseInDuration = clipInfo.EaseInDuration,
                EaseOutDuration = clipInfo.EaseOutDuration
            };
        }
    }
}
