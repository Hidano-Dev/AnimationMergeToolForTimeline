using System.Collections.Generic;
using AnimationMergeTool.Editor.Domain.Models;
using UnityEngine;
using UnityEngine.Timeline;

namespace AnimationMergeTool.Editor.Domain
{
    /// <summary>
    /// Extrapolation処理を行うクラス
    /// クリップ範囲外の値をExtrapolation設定に基づいて処理する
    /// FR-032, FR-041, FR-042 対応
    /// </summary>
    public class ExtrapolationProcessor
    {
        /// <summary>
        /// Extrapolation処理のサンプリング用デフォルトフレームレート
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
        /// 指定時間における Extrapolation を考慮した値を取得する
        /// </summary>
        /// <param name="curve">元のAnimationCurve</param>
        /// <param name="clipInfo">クリップ情報（Extrapolation設定を含む）</param>
        /// <param name="time">評価する時間（Timeline上の絶対時間）</param>
        /// <param name="value">出力値</param>
        /// <returns>値が存在する場合はtrue、None設定で範囲外の場合はfalse</returns>
        public bool TryGetExtrapolatedValue(
            AnimationCurve curve,
            ClipInfo clipInfo,
            float time,
            out float value)
        {
            value = 0f;

            if (curve == null || clipInfo == null || curve.keys.Length == 0)
            {
                return false;
            }

            var startTime = (float)clipInfo.StartTime;
            var endTime = (float)clipInfo.EndTime;
            var clipIn = (float)clipInfo.ClipIn;
            var timeScale = (float)clipInfo.TimeScale;
            var clipDuration = (float)clipInfo.Duration;

            // TimeScaleが0以下の場合は無効なので1として扱う
            if (timeScale <= 0)
            {
                timeScale = 1f;
            }

            // クリップ範囲内の場合
            if (time >= startTime && time <= endTime)
            {
                // Timeline時間からソースクリップのローカル時間に変換
                var localTime = (time - startTime) * timeScale + clipIn;
                value = curve.Evaluate(localTime);
                return true;
            }

            // クリップ開始前（PreExtrapolation）
            if (time < startTime)
            {
                return ProcessPreExtrapolation(curve, clipInfo, time, out value);
            }

            // クリップ終了後（PostExtrapolation）
            return ProcessPostExtrapolation(curve, clipInfo, time, out value);
        }

        /// <summary>
        /// PreExtrapolation処理（クリップ開始前の動作）
        /// </summary>
        private bool ProcessPreExtrapolation(
            AnimationCurve curve,
            ClipInfo clipInfo,
            float time,
            out float value)
        {
            value = 0f;
            var startTime = (float)clipInfo.StartTime;
            var clipIn = (float)clipInfo.ClipIn;
            var timeScale = (float)clipInfo.TimeScale;
            var clipDuration = (float)clipInfo.Duration;

            if (timeScale <= 0) timeScale = 1f;

            // ソースクリップの最初のキーフレームの時間と値
            var firstKeyTime = clipIn;
            var firstKeyValue = curve.Evaluate(firstKeyTime);

            switch (clipInfo.PreExtrapolation)
            {
                case TimelineClip.ClipExtrapolation.None:
                    // 値を出力しない
                    return false;

                case TimelineClip.ClipExtrapolation.Hold:
                    // 最初のキーの値を維持
                    value = firstKeyValue;
                    return true;

                case TimelineClip.ClipExtrapolation.Loop:
                    // クリップの長さでループ
                    value = EvaluateLooped(curve, clipInfo, time, clipIn, clipDuration * timeScale);
                    return true;

                case TimelineClip.ClipExtrapolation.PingPong:
                    // クリップの長さで往復
                    value = EvaluatePingPong(curve, clipInfo, time, clipIn, clipDuration * timeScale);
                    return true;

                case TimelineClip.ClipExtrapolation.Continue:
                    // 最初のキーフレームの接線を延長
                    value = EvaluateContinuePre(curve, clipIn, startTime, time);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// PostExtrapolation処理（クリップ終了後の動作）
        /// </summary>
        private bool ProcessPostExtrapolation(
            AnimationCurve curve,
            ClipInfo clipInfo,
            float time,
            out float value)
        {
            value = 0f;
            var startTime = (float)clipInfo.StartTime;
            var endTime = (float)clipInfo.EndTime;
            var clipIn = (float)clipInfo.ClipIn;
            var timeScale = (float)clipInfo.TimeScale;
            var clipDuration = (float)clipInfo.Duration;

            if (timeScale <= 0) timeScale = 1f;

            // ソースクリップの最後のキーフレームの時間と値
            var lastKeyTime = clipIn + clipDuration * timeScale;
            var lastKeyValue = curve.Evaluate(lastKeyTime);

            switch (clipInfo.PostExtrapolation)
            {
                case TimelineClip.ClipExtrapolation.None:
                    // 値を出力しない
                    return false;

                case TimelineClip.ClipExtrapolation.Hold:
                    // 最後のキーの値を維持
                    value = lastKeyValue;
                    return true;

                case TimelineClip.ClipExtrapolation.Loop:
                    // クリップの長さでループ
                    value = EvaluateLooped(curve, clipInfo, time, clipIn, clipDuration * timeScale);
                    return true;

                case TimelineClip.ClipExtrapolation.PingPong:
                    // クリップの長さで往復
                    value = EvaluatePingPong(curve, clipInfo, time, clipIn, clipDuration * timeScale);
                    return true;

                case TimelineClip.ClipExtrapolation.Continue:
                    // 最後のキーフレームの接線を延長
                    value = EvaluateContinuePost(curve, lastKeyTime, endTime, time);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Loop Extrapolationの評価
        /// </summary>
        private float EvaluateLooped(
            AnimationCurve curve,
            ClipInfo clipInfo,
            float time,
            float clipIn,
            float sourceClipDuration)
        {
            var startTime = (float)clipInfo.StartTime;
            var timeScale = (float)clipInfo.TimeScale;

            if (timeScale <= 0) timeScale = 1f;
            if (sourceClipDuration <= 0) return curve.Evaluate(clipIn);

            // Timeline時間からの差分を計算
            var timeDelta = time - startTime;

            // ソースクリップの長さでモジュロ演算（負の値も考慮）
            var loopedTime = timeDelta * timeScale;
            loopedTime = Mathf.Repeat(loopedTime, sourceClipDuration);

            return curve.Evaluate(clipIn + loopedTime);
        }

        /// <summary>
        /// PingPong Extrapolationの評価
        /// </summary>
        private float EvaluatePingPong(
            AnimationCurve curve,
            ClipInfo clipInfo,
            float time,
            float clipIn,
            float sourceClipDuration)
        {
            var startTime = (float)clipInfo.StartTime;
            var timeScale = (float)clipInfo.TimeScale;

            if (timeScale <= 0) timeScale = 1f;
            if (sourceClipDuration <= 0) return curve.Evaluate(clipIn);

            // Timeline時間からの差分を計算
            var timeDelta = time - startTime;

            // ソースクリップの長さでPingPong演算
            var pingPongTime = Mathf.Abs(timeDelta * timeScale);
            pingPongTime = Mathf.PingPong(pingPongTime, sourceClipDuration);

            return curve.Evaluate(clipIn + pingPongTime);
        }

        /// <summary>
        /// Continue Extrapolation（クリップ開始前）の評価
        /// 最初のキーフレームの接線を延長
        /// </summary>
        private float EvaluateContinuePre(
            AnimationCurve curve,
            float clipIn,
            float startTime,
            float time)
        {
            if (curve.keys.Length == 0) return 0f;

            // 最初のキーフレームを取得
            var firstKey = curve.keys[0];
            var firstKeyValue = curve.Evaluate(clipIn);

            // 接線を取得（inTangent を使用）
            var tangent = firstKey.inTangent;

            // 時間差を計算してExtrapolate
            var timeDelta = time - startTime;
            return firstKeyValue + tangent * timeDelta;
        }

        /// <summary>
        /// Continue Extrapolation（クリップ終了後）の評価
        /// 最後のキーフレームの接線を延長
        /// </summary>
        private float EvaluateContinuePost(
            AnimationCurve curve,
            float lastKeyTime,
            float endTime,
            float time)
        {
            if (curve.keys.Length == 0) return 0f;

            // 最後のキーフレームを取得
            var lastKey = curve.keys[curve.keys.Length - 1];
            var lastKeyValue = curve.Evaluate(lastKeyTime);

            // 接線を取得（outTangent を使用）
            var tangent = lastKey.outTangent;

            // 時間差を計算してExtrapolate
            var timeDelta = time - endTime;
            return lastKeyValue + tangent * timeDelta;
        }

        /// <summary>
        /// 指定されたExtrapolationモードが値を出力するかどうかを判定する
        /// </summary>
        /// <param name="mode">Extrapolationモード</param>
        /// <returns>値を出力する場合はtrue</returns>
        public static bool HasValue(TimelineClip.ClipExtrapolation mode)
        {
            return mode != TimelineClip.ClipExtrapolation.None;
        }
    }
}
