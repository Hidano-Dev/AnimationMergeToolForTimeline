using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace AnimationMergeTool.Editor.Domain.Models
{
    /// <summary>
    /// AnimationTrackの情報を保持するデータモデル
    /// </summary>
    public class TrackInfo
    {
        /// <summary>
        /// AnimationTrack参照
        /// </summary>
        public AnimationTrack Track { get; }

        /// <summary>
        /// 優先順位（数値が大きいほど高優先）
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// バインドされたAnimator
        /// </summary>
        public Animator BoundAnimator { get; set; }

        /// <summary>
        /// Mute状態
        /// </summary>
        public bool IsMuted => Track != null && Track.muted;

        /// <summary>
        /// 子トラック（OverrideTrack）のリスト
        /// </summary>
        public List<TrackInfo> OverrideTracks { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="track">AnimationTrack参照</param>
        /// <param name="priority">優先順位（デフォルト: 0）</param>
        /// <param name="boundAnimator">バインドされたAnimator（デフォルト: null）</param>
        public TrackInfo(AnimationTrack track, int priority = 0, Animator boundAnimator = null)
        {
            Track = track;
            Priority = priority;
            BoundAnimator = boundAnimator;
            OverrideTracks = new List<TrackInfo>();
        }

        /// <summary>
        /// OverrideTrackを追加する
        /// </summary>
        /// <param name="overrideTrack">追加するOverrideTrack情報</param>
        public void AddOverrideTrack(TrackInfo overrideTrack)
        {
            if (overrideTrack != null)
            {
                OverrideTracks.Add(overrideTrack);
            }
        }

        /// <summary>
        /// トラックが有効かどうか（Muteされておらず、Animatorがバインドされている）
        /// </summary>
        public bool IsValid => !IsMuted && BoundAnimator != null;
    }
}
