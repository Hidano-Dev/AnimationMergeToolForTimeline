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

        /// <summary>
        /// 指定時間におけるブレンドウェイトを計算する
        /// クリップのローカル時間とブレンド情報から、0〜1のウェイト値を返す
        /// </summary>
        /// <param name="clipInfo">クリップ情報</param>
        /// <param name="globalTime">タイムライン上のグローバル時間</param>
        /// <returns>ブレンドウェイト（0.0〜1.0）</returns>
        public float CalculateBlendWeight(ClipInfo clipInfo, double globalTime)
        {
            if (clipInfo == null)
            {
                return 0f;
            }

            // クリップの範囲外の場合
            if (globalTime < clipInfo.StartTime || globalTime > clipInfo.EndTime)
            {
                return 0f;
            }

            var blendInfo = GetBlendInfo(clipInfo);
            float weight = 1f;

            // EaseIn処理（クリップ開始からEaseInDuration期間）
            if (blendInfo.HasEaseIn)
            {
                double easeInEndTime = clipInfo.StartTime + blendInfo.EaseInDuration;
                if (globalTime < easeInEndTime)
                {
                    // EaseIn区間内：カーブを評価
                    double normalizedTime = (globalTime - clipInfo.StartTime) / blendInfo.EaseInDuration;
                    weight *= blendInfo.BlendInCurve.Evaluate((float)normalizedTime);
                }
            }

            // EaseOut処理（クリップ終了からEaseOutDuration前）
            if (blendInfo.HasEaseOut)
            {
                double easeOutStartTime = clipInfo.EndTime - blendInfo.EaseOutDuration;
                if (globalTime > easeOutStartTime)
                {
                    // EaseOut区間内：カーブを評価
                    double normalizedTime = (globalTime - easeOutStartTime) / blendInfo.EaseOutDuration;
                    weight *= blendInfo.BlendOutCurve.Evaluate((float)normalizedTime);
                }
            }

            return Mathf.Clamp01(weight);
        }

        /// <summary>
        /// BlendInfoとクリップ時間情報から直接ブレンドウェイトを計算する
        /// </summary>
        /// <param name="blendInfo">ブレンド情報</param>
        /// <param name="localTime">クリップ内のローカル時間</param>
        /// <param name="clipDuration">クリップの長さ</param>
        /// <returns>ブレンドウェイト（0.0〜1.0）</returns>
        public float CalculateBlendWeight(BlendInfo blendInfo, double localTime, double clipDuration)
        {
            if (clipDuration <= 0)
            {
                return 0f;
            }

            // クリップの範囲外の場合
            if (localTime < 0 || localTime > clipDuration)
            {
                return 0f;
            }

            float weight = 1f;

            // EaseIn処理
            if (blendInfo.HasEaseIn && localTime < blendInfo.EaseInDuration)
            {
                double normalizedTime = localTime / blendInfo.EaseInDuration;
                weight *= blendInfo.BlendInCurve.Evaluate((float)normalizedTime);
            }

            // EaseOut処理
            double easeOutStartTime = clipDuration - blendInfo.EaseOutDuration;
            if (blendInfo.HasEaseOut && localTime > easeOutStartTime)
            {
                double normalizedTime = (localTime - easeOutStartTime) / blendInfo.EaseOutDuration;
                weight *= blendInfo.BlendOutCurve.Evaluate((float)normalizedTime);
            }

            return Mathf.Clamp01(weight);
        }
    }
}
