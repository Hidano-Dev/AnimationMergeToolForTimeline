using UnityEngine;
using UnityEngine.Timeline;

namespace AnimationMergeTool.Editor.Domain.Models
{
    /// <summary>
    /// TimelineClipの情報を保持するデータモデル
    /// </summary>
    public class ClipInfo
    {
        /// <summary>
        /// TimelineClip参照
        /// </summary>
        public TimelineClip TimelineClip { get; }

        /// <summary>
        /// AnimationClip参照
        /// </summary>
        public AnimationClip AnimationClip { get; }

        /// <summary>
        /// シーンオフセット位置（AnimationPlayableAsset.position）
        /// Hierarchy上で手動設定されたPosition値
        /// </summary>
        public Vector3 SceneOffsetPosition { get; }

        /// <summary>
        /// シーンオフセット回転（AnimationPlayableAsset.rotation）
        /// Hierarchy上で手動設定されたRotation値
        /// </summary>
        public Quaternion SceneOffsetRotation { get; }

        /// <summary>
        /// 開始時間（秒）
        /// </summary>
        public double StartTime => TimelineClip?.start ?? 0;

        /// <summary>
        /// 終了時間（秒）
        /// </summary>
        public double EndTime => TimelineClip?.end ?? 0;

        /// <summary>
        /// ClipIn（トリミング開始位置、秒）
        /// </summary>
        public double ClipIn => TimelineClip?.clipIn ?? 0;

        /// <summary>
        /// TimeScale（再生速度）
        /// </summary>
        public double TimeScale => TimelineClip?.timeScale ?? 1;

        /// <summary>
        /// PreExtrapolation設定（クリップ開始前の動作）
        /// </summary>
        public TimelineClip.ClipExtrapolation PreExtrapolation =>
            TimelineClip?.preExtrapolationMode ?? TimelineClip.ClipExtrapolation.None;

        /// <summary>
        /// PostExtrapolation設定（クリップ終了後の動作）
        /// </summary>
        public TimelineClip.ClipExtrapolation PostExtrapolation =>
            TimelineClip?.postExtrapolationMode ?? TimelineClip.ClipExtrapolation.None;

        /// <summary>
        /// EaseInDuration（ブレンドイン時間、秒）
        /// </summary>
        public double EaseInDuration => TimelineClip?.easeInDuration ?? 0;

        /// <summary>
        /// EaseOutDuration（ブレンドアウト時間、秒）
        /// </summary>
        public double EaseOutDuration => TimelineClip?.easeOutDuration ?? 0;

        /// <summary>
        /// BlendInCurve（ブレンドインカーブ）
        /// </summary>
        public AnimationCurve BlendInCurve => TimelineClip?.mixInCurve;

        /// <summary>
        /// BlendOutCurve（ブレンドアウトカーブ）
        /// </summary>
        public AnimationCurve BlendOutCurve => TimelineClip?.mixOutCurve;

        /// <summary>
        /// クリップの長さ（秒）
        /// </summary>
        public double Duration => TimelineClip?.duration ?? 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="timelineClip">TimelineClip参照</param>
        /// <param name="animationClip">AnimationClip参照</param>
        public ClipInfo(TimelineClip timelineClip, AnimationClip animationClip)
            : this(timelineClip, animationClip, Vector3.zero, Quaternion.identity)
        {
        }

        /// <summary>
        /// コンストラクタ（シーンオフセット付き）
        /// </summary>
        /// <param name="timelineClip">TimelineClip参照</param>
        /// <param name="animationClip">AnimationClip参照</param>
        /// <param name="sceneOffsetPosition">シーンオフセット位置</param>
        /// <param name="sceneOffsetRotation">シーンオフセット回転</param>
        public ClipInfo(TimelineClip timelineClip, AnimationClip animationClip,
            Vector3 sceneOffsetPosition, Quaternion sceneOffsetRotation)
        {
            TimelineClip = timelineClip;
            AnimationClip = animationClip;
            SceneOffsetPosition = sceneOffsetPosition;
            SceneOffsetRotation = sceneOffsetRotation;
        }

        /// <summary>
        /// シーンオフセットが設定されているかどうか
        /// </summary>
        public bool HasSceneOffset =>
            SceneOffsetPosition != Vector3.zero ||
            SceneOffsetRotation != Quaternion.identity;

        /// <summary>
        /// クリップが有効かどうか（TimelineClipとAnimationClipが両方存在する）
        /// </summary>
        public bool IsValid => TimelineClip != null && AnimationClip != null;
    }
}
