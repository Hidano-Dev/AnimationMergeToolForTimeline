using System.Collections.Generic;
using System.Linq;
using AnimationMergeTool.Editor.Domain.Models;
using UnityEngine;
using UnityEngine.Timeline;

namespace AnimationMergeTool.Editor.Domain
{
    /// <summary>
    /// クリップ間のGap情報を保持する構造体
    /// </summary>
    public struct GapInfo
    {
        /// <summary>
        /// Gapの開始時間（秒）
        /// </summary>
        public double StartTime { get; }

        /// <summary>
        /// Gapの終了時間（秒）
        /// </summary>
        public double EndTime { get; }

        /// <summary>
        /// Gapの直前のクリップ（null の場合はタイムラインの最初のGap）
        /// </summary>
        public ClipInfo PreviousClip { get; }

        /// <summary>
        /// Gapの直後のクリップ（null の場合はタイムラインの最後のGap）
        /// </summary>
        public ClipInfo NextClip { get; }

        /// <summary>
        /// Gapの長さ（秒）
        /// </summary>
        public double Duration => EndTime - StartTime;

        /// <summary>
        /// 有効なGapかどうか（長さが正の場合）
        /// </summary>
        public bool IsValid => Duration > 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="startTime">Gapの開始時間</param>
        /// <param name="endTime">Gapの終了時間</param>
        /// <param name="previousClip">直前のクリップ</param>
        /// <param name="nextClip">直後のクリップ</param>
        public GapInfo(double startTime, double endTime, ClipInfo previousClip, ClipInfo nextClip)
        {
            StartTime = startTime;
            EndTime = endTime;
            PreviousClip = previousClip;
            NextClip = nextClip;
        }
    }

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

        /// <summary>
        /// 同一トラック内のクリップ間のGapを検出する
        /// </summary>
        /// <param name="clipInfos">トラック内のクリップ情報リスト（開始時間でソートされている必要はない）</param>
        /// <returns>検出されたGap情報のリスト（開始時間順）</returns>
        public List<GapInfo> DetectGaps(List<ClipInfo> clipInfos)
        {
            var gaps = new List<GapInfo>();

            if (clipInfos == null || clipInfos.Count == 0)
            {
                return gaps;
            }

            // 有効なクリップのみをフィルタリング
            var validClips = clipInfos.Where(c => c != null && c.IsValid).ToList();
            if (validClips.Count == 0)
            {
                return gaps;
            }

            // 開始時間でソート
            var sortedClips = validClips.OrderBy(c => c.StartTime).ToList();

            // 連続するクリップ間のGapを検出
            for (var i = 0; i < sortedClips.Count - 1; i++)
            {
                var currentClip = sortedClips[i];
                var nextClip = sortedClips[i + 1];

                var gapStart = currentClip.EndTime;
                var gapEnd = nextClip.StartTime;

                // Gapが存在する場合（終了時間 < 次の開始時間）
                if (gapStart < gapEnd)
                {
                    gaps.Add(new GapInfo(gapStart, gapEnd, currentClip, nextClip));
                }
            }

            return gaps;
        }

        /// <summary>
        /// 指定時間がGap内にあるかどうかを判定し、該当するGap情報を返す
        /// </summary>
        /// <param name="gaps">Gap情報のリスト</param>
        /// <param name="time">判定する時間</param>
        /// <param name="gapInfo">該当するGap情報（見つからない場合はデフォルト値）</param>
        /// <returns>Gap内にある場合はtrue</returns>
        public bool TryGetGapAtTime(List<GapInfo> gaps, double time, out GapInfo gapInfo)
        {
            gapInfo = default;

            if (gaps == null)
            {
                return false;
            }

            foreach (var gap in gaps)
            {
                if (time >= gap.StartTime && time < gap.EndTime)
                {
                    gapInfo = gap;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 2つのクリップ間のGap時間を計算する
        /// </summary>
        /// <param name="firstClip">先行するクリップ</param>
        /// <param name="secondClip">後続するクリップ</param>
        /// <returns>Gap時間（秒）。重なりがある場合は負の値、クリップが無効な場合は0</returns>
        public double CalculateGapDuration(ClipInfo firstClip, ClipInfo secondClip)
        {
            if (firstClip == null || secondClip == null)
            {
                return 0;
            }

            if (!firstClip.IsValid || !secondClip.IsValid)
            {
                return 0;
            }

            return secondClip.StartTime - firstClip.EndTime;
        }

        /// <summary>
        /// Gap区間の補間処理を行い、前のクリップのPostExtrapolation設定に基づいてカーブを生成する
        /// </summary>
        /// <param name="sourceCurve">元のAnimationCurve</param>
        /// <param name="gapInfo">Gap情報</param>
        /// <returns>Gap区間の補間されたAnimationCurve（値がない場合やGapが無効な場合はnull）</returns>
        public AnimationCurve FillGapWithExtrapolation(AnimationCurve sourceCurve, GapInfo gapInfo)
        {
            // 無効なパラメータチェック
            if (sourceCurve == null || sourceCurve.keys.Length == 0)
            {
                return null;
            }

            if (!gapInfo.IsValid || gapInfo.PreviousClip == null)
            {
                return null;
            }

            var previousClip = gapInfo.PreviousClip;

            // PostExtrapolationがNoneの場合は値を出力しない
            if (previousClip.PostExtrapolation == TimelineClip.ClipExtrapolation.None)
            {
                return null;
            }

            var gapCurve = new AnimationCurve();
            var frameInterval = 1f / _frameRate;
            var gapStart = (float)gapInfo.StartTime;
            var gapEnd = (float)gapInfo.EndTime;

            // Gap区間をフレームレートに基づいてサンプリング
            for (var time = gapStart; time < gapEnd; time += frameInterval)
            {
                if (TryGetExtrapolatedValue(sourceCurve, previousClip, time, out var value))
                {
                    gapCurve.AddKey(new Keyframe(time, value));
                }
            }

            // Gap終了直前の最後のキーフレームを追加（精度確保のため）
            var lastTime = gapEnd - 0.0001f;
            if (lastTime > gapStart && TryGetExtrapolatedValue(sourceCurve, previousClip, lastTime, out var lastValue))
            {
                // 既存のキーがなければ追加
                var hasKeyAtLastTime = gapCurve.keys.Any(key => Mathf.Abs(key.time - lastTime) < 0.0001f);
                if (!hasKeyAtLastTime)
                {
                    gapCurve.AddKey(new Keyframe(lastTime, lastValue));
                }
            }

            return gapCurve.keys.Length > 0 ? gapCurve : null;
        }

        /// <summary>
        /// Gap区間の指定時間における補間値を取得する
        /// 前のクリップのPostExtrapolation設定に基づいて値を計算する
        /// </summary>
        /// <param name="sourceCurve">元のAnimationCurve</param>
        /// <param name="gapInfo">Gap情報</param>
        /// <param name="time">評価する時間（Timeline上の絶対時間）</param>
        /// <param name="value">出力値</param>
        /// <returns>値が存在する場合はtrue</returns>
        public bool TryGetGapInterpolatedValue(
            AnimationCurve sourceCurve,
            GapInfo gapInfo,
            float time,
            out float value)
        {
            value = 0f;

            // 無効なパラメータチェック
            if (sourceCurve == null || sourceCurve.keys.Length == 0)
            {
                return false;
            }

            if (!gapInfo.IsValid || gapInfo.PreviousClip == null)
            {
                return false;
            }

            // 時間がGap区間内かチェック
            if (time < gapInfo.StartTime || time >= gapInfo.EndTime)
            {
                return false;
            }

            // 前のクリップのPostExtrapolation設定を使用して値を取得
            return TryGetExtrapolatedValue(sourceCurve, gapInfo.PreviousClip, time, out value);
        }
    }
}
