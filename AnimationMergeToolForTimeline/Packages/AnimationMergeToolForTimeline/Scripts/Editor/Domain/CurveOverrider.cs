using System.Collections.Generic;
using AnimationMergeTool.Editor.Domain.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace AnimationMergeTool.Editor.Domain
{
    /// <summary>
    /// 複数トラック間のOverride処理を行うクラス
    /// FR-040, FR-041 対応
    /// 下の段（優先順位が高い）のAnimationCurveで上の段をOverrideする
    /// </summary>
    public class CurveOverrider
    {
        /// <summary>
        /// 2つのEditorCurveBindingが同一プロパティを指しているかを判定する
        /// </summary>
        /// <param name="binding1">1つ目のバインディング</param>
        /// <param name="binding2">2つ目のバインディング</param>
        /// <returns>同一プロパティを指している場合true</returns>
        public bool IsSameProperty(EditorCurveBinding binding1, EditorCurveBinding binding2)
        {
            // path: 対象オブジェクトのパス（Animatorからの相対パス）
            // type: プロパティを持つコンポーネントの型
            // propertyName: プロパティ名
            return binding1.path == binding2.path &&
                   binding1.type == binding2.type &&
                   binding1.propertyName == binding2.propertyName;
        }

        /// <summary>
        /// EditorCurveBindingを一意に識別するキー文字列を生成する
        /// </summary>
        /// <param name="binding">バインディング</param>
        /// <returns>一意識別キー</returns>
        public string GetBindingKey(EditorCurveBinding binding)
        {
            return $"{binding.path}|{binding.type.FullName}|{binding.propertyName}";
        }

        /// <summary>
        /// 2つのカーブバインディングペアのリストから同一プロパティを持つものを検出する
        /// </summary>
        /// <param name="lowerPriorityPairs">低優先順位（上の段）のカーブペアリスト</param>
        /// <param name="higherPriorityPairs">高優先順位（下の段）のカーブペアリスト</param>
        /// <returns>同一プロパティのバインディングキーのセット</returns>
        public HashSet<string> DetectOverlappingProperties(
            List<CurveBindingPair> lowerPriorityPairs,
            List<CurveBindingPair> higherPriorityPairs)
        {
            var overlapping = new HashSet<string>();

            if (lowerPriorityPairs == null || higherPriorityPairs == null)
            {
                return overlapping;
            }

            // 低優先順位側のバインディングキーをセットに格納
            var lowerKeys = new HashSet<string>();
            foreach (var pair in lowerPriorityPairs)
            {
                lowerKeys.Add(GetBindingKey(pair.Binding));
            }

            // 高優先順位側で同じキーがあるものを検出
            foreach (var pair in higherPriorityPairs)
            {
                var key = GetBindingKey(pair.Binding);
                if (lowerKeys.Contains(key))
                {
                    overlapping.Add(key);
                }
            }

            return overlapping;
        }

        /// <summary>
        /// 完全重なりのOverride処理を行う
        /// 高優先順位トラックのカーブで低優先順位トラックのカーブを完全に置換する
        /// </summary>
        /// <param name="lowerPriorityCurve">低優先順位（上の段）のカーブ</param>
        /// <param name="higherPriorityCurve">高優先順位（下の段）のカーブ</param>
        /// <param name="higherPriorityStartTime">高優先順位カーブの開始時間</param>
        /// <param name="higherPriorityEndTime">高優先順位カーブの終了時間</param>
        /// <returns>Override処理後のカーブ</returns>
        public AnimationCurve ApplyFullOverride(
            AnimationCurve lowerPriorityCurve,
            AnimationCurve higherPriorityCurve,
            float higherPriorityStartTime,
            float higherPriorityEndTime)
        {
            // 高優先順位カーブがnullの場合は低優先順位をそのまま返す
            if (higherPriorityCurve == null)
            {
                return lowerPriorityCurve != null ? CopyCurve(lowerPriorityCurve) : new AnimationCurve();
            }

            // 低優先順位カーブがnullの場合は高優先順位をそのまま返す
            if (lowerPriorityCurve == null)
            {
                return CopyCurve(higherPriorityCurve);
            }

            // 高優先順位カーブが低優先順位カーブの全区間をカバーしている場合
            // 高優先順位カーブで完全に置換
            var lowerKeys = lowerPriorityCurve.keys;
            if (lowerKeys.Length == 0)
            {
                return CopyCurve(higherPriorityCurve);
            }

            var lowerStartTime = lowerKeys[0].time;
            var lowerEndTime = lowerKeys[lowerKeys.Length - 1].time;

            // 完全にカバーしている場合は高優先順位を返す
            if (higherPriorityStartTime <= lowerStartTime && higherPriorityEndTime >= lowerEndTime)
            {
                return CopyCurve(higherPriorityCurve);
            }

            // 部分的なOverrideが必要な場合はApplyPartialOverrideを使用
            return ApplyPartialOverride(
                lowerPriorityCurve,
                higherPriorityCurve,
                higherPriorityStartTime,
                higherPriorityEndTime);
        }

        /// <summary>
        /// 部分的重なりのOverride処理を行う
        /// 重なり区間は高優先順位カーブを使用し、非重なり区間は低優先順位カーブを使用する
        /// </summary>
        /// <param name="lowerPriorityCurve">低優先順位（上の段）のカーブ</param>
        /// <param name="higherPriorityCurve">高優先順位（下の段）のカーブ</param>
        /// <param name="higherPriorityStartTime">高優先順位カーブの開始時間</param>
        /// <param name="higherPriorityEndTime">高優先順位カーブの終了時間</param>
        /// <returns>Override処理後のカーブ</returns>
        public AnimationCurve ApplyPartialOverride(
            AnimationCurve lowerPriorityCurve,
            AnimationCurve higherPriorityCurve,
            float higherPriorityStartTime,
            float higherPriorityEndTime)
        {
            var resultCurve = new AnimationCurve();

            // 高優先順位カーブがnullの場合は低優先順位をそのまま返す
            if (higherPriorityCurve == null)
            {
                return lowerPriorityCurve != null ? CopyCurve(lowerPriorityCurve) : resultCurve;
            }

            // 低優先順位カーブがnullの場合は高優先順位をそのまま返す
            if (lowerPriorityCurve == null)
            {
                return CopyCurve(higherPriorityCurve);
            }

            // 高優先順位カーブの実際のキー時間範囲を取得
            var higherKeys = higherPriorityCurve.keys;
            var effectiveStartTime = higherPriorityStartTime;
            var effectiveEndTime = higherPriorityEndTime;

            if (higherKeys.Length > 0)
            {
                // カーブのキーが存在する時間範囲を使用
                // 渡された時間範囲とカーブの実際のキー範囲の両方を考慮
                effectiveStartTime = Mathf.Max(higherPriorityStartTime, higherKeys[0].time);
                effectiveEndTime = Mathf.Min(higherPriorityEndTime, higherKeys[higherKeys.Length - 1].time);
            }

            // 低優先順位カーブのキーを追加（重なり区間外のみ）
            foreach (var key in lowerPriorityCurve.keys)
            {
                // 高優先順位の有効区間内のキーはスキップ
                if (key.time >= effectiveStartTime && key.time <= effectiveEndTime)
                {
                    continue;
                }
                resultCurve.AddKey(key);
            }

            // 高優先順位カーブのキーをすべて追加
            foreach (var key in higherPriorityCurve.keys)
            {
                resultCurve.AddKey(key);
            }

            return resultCurve;
        }

        /// <summary>
        /// アクティブ区間を考慮した部分的Override処理を行う
        /// Extrapolation=None時のGap区間で低優先順位カーブが正しく保持されるようにする
        /// </summary>
        /// <param name="lowerPriorityCurve">低優先順位（上の段）のカーブ</param>
        /// <param name="higherPriorityCurve">高優先順位（下の段）のカーブ</param>
        /// <param name="activeIntervals">高優先順位カーブのアクティブ区間リスト</param>
        /// <returns>Override処理後のカーブ</returns>
        public AnimationCurve ApplyPartialOverrideWithActiveIntervals(
            AnimationCurve lowerPriorityCurve,
            AnimationCurve higherPriorityCurve,
            List<ActiveInterval> activeIntervals)
        {
            var resultCurve = new AnimationCurve();

            // null チェック
            if (higherPriorityCurve == null)
            {
                return lowerPriorityCurve != null ? CopyCurve(lowerPriorityCurve) : resultCurve;
            }

            if (lowerPriorityCurve == null)
            {
                return CopyCurve(higherPriorityCurve);
            }

            // ActiveIntervalsが未設定の場合は従来のApplyPartialOverrideにフォールバック
            if (activeIntervals == null || activeIntervals.Count == 0)
            {
                var higherKeys = higherPriorityCurve.keys;
                if (higherKeys.Length > 0)
                {
                    return ApplyPartialOverride(lowerPriorityCurve, higherPriorityCurve,
                        higherKeys[0].time, higherKeys[higherKeys.Length - 1].time);
                }
                return CopyCurve(lowerPriorityCurve);
            }

            // 1. 高優先順位カーブのキー → アクティブ区間内のもののみ追加
            foreach (var key in higherPriorityCurve.keys)
            {
                if (IsInAnyActiveInterval(key.time, activeIntervals))
                {
                    resultCurve.AddKey(key);
                }
            }

            // 2. 低優先順位カーブのキー → アクティブ区間外のもののみ追加
            foreach (var key in lowerPriorityCurve.keys)
            {
                if (!IsInAnyActiveInterval(key.time, activeIntervals))
                {
                    resultCurve.AddKey(key);
                }
            }

            // 3. ギャップ区間の境界に低優先順位カーブからの遷移キーを追加
            const float boundaryOffset = 0.001f;

            // 3a. 最初のアクティブ区間の直前に遷移キーを追加（ベース→Override遷移）
            var firstInterval = activeIntervals[0];
            if (HasLowerPriorityKeyBefore(lowerPriorityCurve, firstInterval.StartTime))
            {
                var nearFirstStart = firstInterval.StartTime - boundaryOffset;
                if (!HasKeyNearTime(resultCurve, nearFirstStart, boundaryOffset * 0.5f))
                {
                    var value = lowerPriorityCurve.Evaluate(nearFirstStart);
                    resultCurve.AddKey(new Keyframe(nearFirstStart, value));
                }
            }

            // 3b. 最後のアクティブ区間の直後に遷移キーを追加（Override→ベースフォールバック）
            var lastInterval = activeIntervals[activeIntervals.Count - 1];
            if (HasLowerPriorityKeyAfter(lowerPriorityCurve, lastInterval.EndTime))
            {
                var nearLastEnd = lastInterval.EndTime + boundaryOffset;
                if (!HasKeyNearTime(resultCurve, nearLastEnd, boundaryOffset * 0.5f))
                {
                    var value = lowerPriorityCurve.Evaluate(nearLastEnd);
                    resultCurve.AddKey(new Keyframe(nearLastEnd, value));
                }
            }

            // 3c. アクティブ区間間のギャップ境界に遷移キーを追加
            for (var i = 0; i < activeIntervals.Count - 1; i++)
            {
                var gapStart = activeIntervals[i].EndTime;
                var gapEnd = activeIntervals[i + 1].StartTime;

                // ギャップが十分に大きい場合のみ遷移キーを追加
                if (gapEnd - gapStart <= boundaryOffset * 2)
                {
                    continue;
                }

                // ギャップ開始点付近に低優先順位カーブの値を追加
                var nearGapStart = gapStart + boundaryOffset;
                if (!HasKeyNearTime(resultCurve, nearGapStart, boundaryOffset * 0.5f))
                {
                    var value = lowerPriorityCurve.Evaluate(nearGapStart);
                    resultCurve.AddKey(new Keyframe(nearGapStart, value));
                }

                // ギャップ終了点付近に低優先順位カーブの値を追加
                var nearGapEnd = gapEnd - boundaryOffset;
                if (!HasKeyNearTime(resultCurve, nearGapEnd, boundaryOffset * 0.5f))
                {
                    var value = lowerPriorityCurve.Evaluate(nearGapEnd);
                    resultCurve.AddKey(new Keyframe(nearGapEnd, value));
                }
            }

            return resultCurve;
        }

        /// <summary>
        /// 指定時間がいずれかのアクティブ区間内にあるかどうかを判定する
        /// </summary>
        private bool IsInAnyActiveInterval(float time, List<ActiveInterval> intervals)
        {
            foreach (var interval in intervals)
            {
                if (time >= interval.StartTime && time <= interval.EndTime)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 低優先順位カーブに指定時間より前のキーフレームが存在するかどうかを判定する
        /// </summary>
        private bool HasLowerPriorityKeyBefore(AnimationCurve lowerPriorityCurve, float time)
        {
            foreach (var key in lowerPriorityCurve.keys)
            {
                if (key.time < time)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 低優先順位カーブに指定時間より後のキーフレームが存在するかどうかを判定する
        /// </summary>
        private bool HasLowerPriorityKeyAfter(AnimationCurve lowerPriorityCurve, float time)
        {
            foreach (var key in lowerPriorityCurve.keys)
            {
                if (key.time > time)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 指定時間付近にキーフレームが既に存在するかどうかを判定する
        /// </summary>
        private bool HasKeyNearTime(AnimationCurve curve, float time, float tolerance)
        {
            foreach (var key in curve.keys)
            {
                if (Mathf.Abs(key.time - time) < tolerance)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 複数トラックのカーブを優先順位順に統合する
        /// リストは優先順位の低い順（上の段から下の段）にソートされている前提
        /// </summary>
        /// <param name="curvesWithTimeRanges">カーブと時間範囲のリスト（優先順位の低い順）</param>
        /// <returns>統合されたカーブ</returns>
        public AnimationCurve MergeMultipleTracks(List<CurveWithTimeRange> curvesWithTimeRanges)
        {
            if (curvesWithTimeRanges == null || curvesWithTimeRanges.Count == 0)
            {
                return new AnimationCurve();
            }

            // 最初のカーブをベースとする
            var result = CopyCurve(curvesWithTimeRanges[0].Curve);
            if (result == null)
            {
                result = new AnimationCurve();
            }

            // 優先順位の低い順に処理し、高優先順位で上書き
            for (var i = 1; i < curvesWithTimeRanges.Count; i++)
            {
                var higherPriority = curvesWithTimeRanges[i];
                if (higherPriority.Curve == null)
                {
                    continue;
                }

                // ActiveIntervalsが設定されている場合はGap区間を考慮したOverrideを使用
                if (higherPriority.ActiveIntervals != null && higherPriority.ActiveIntervals.Count > 0)
                {
                    result = ApplyPartialOverrideWithActiveIntervals(
                        result,
                        higherPriority.Curve,
                        higherPriority.ActiveIntervals);
                }
                else
                {
                    result = ApplyPartialOverride(
                        result,
                        higherPriority.Curve,
                        higherPriority.StartTime,
                        higherPriority.EndTime);
                }
            }

            return result;
        }

        /// <summary>
        /// AnimationCurveをコピーする
        /// </summary>
        /// <param name="source">コピー元のカーブ</param>
        /// <returns>コピーされたカーブ</returns>
        private AnimationCurve CopyCurve(AnimationCurve source)
        {
            if (source == null)
            {
                return null;
            }

            var copy = new AnimationCurve();
            foreach (var key in source.keys)
            {
                copy.AddKey(key);
            }
            return copy;
        }

        /// <summary>
        /// 部分的重なりのOverride処理を行う（Extrapolation設定対応）
        /// FR-041: 下の段のクリップが上の段のクリップと部分的にしか重ならない場合、
        /// 下の段のクリップのAnimationExtrapolation設定に従って処理する
        /// </summary>
        /// <param name="lowerPriorityCurve">低優先順位（上の段）のカーブ</param>
        /// <param name="lowerPriorityClipInfo">低優先順位クリップの情報</param>
        /// <param name="higherPriorityCurve">高優先順位（下の段）のカーブ</param>
        /// <param name="higherPriorityClipInfo">高優先順位クリップの情報</param>
        /// <param name="extrapolationProcessor">Extrapolation処理用プロセッサ</param>
        /// <returns>Override処理後のカーブ</returns>
        public AnimationCurve ApplyPartialOverrideWithExtrapolation(
            AnimationCurve lowerPriorityCurve,
            ClipInfo lowerPriorityClipInfo,
            AnimationCurve higherPriorityCurve,
            ClipInfo higherPriorityClipInfo,
            ExtrapolationProcessor extrapolationProcessor)
        {
            var resultCurve = new AnimationCurve();

            // 高優先順位カーブがnullの場合は低優先順位をそのまま返す
            if (higherPriorityCurve == null || higherPriorityClipInfo == null)
            {
                return lowerPriorityCurve != null ? CopyCurve(lowerPriorityCurve) : resultCurve;
            }

            // 低優先順位カーブがnullの場合は高優先順位をそのまま返す
            if (lowerPriorityCurve == null || lowerPriorityClipInfo == null)
            {
                return CopyCurve(higherPriorityCurve);
            }

            if (extrapolationProcessor == null)
            {
                extrapolationProcessor = new ExtrapolationProcessor();
            }

            var higherStartTime = (float)higherPriorityClipInfo.StartTime;
            var higherEndTime = (float)higherPriorityClipInfo.EndTime;
            var lowerStartTime = (float)lowerPriorityClipInfo.StartTime;
            var lowerEndTime = (float)lowerPriorityClipInfo.EndTime;

            // Extrapolationが有効な区間を計算
            // PreExtrapolationがNone以外の場合、higherStartTimeより前の区間も高優先順位がカバー
            // PostExtrapolationがNone以外の場合、higherEndTimeより後の区間も高優先順位がカバー
            var effectiveHigherStartTime = higherStartTime;
            var effectiveHigherEndTime = higherEndTime;

            if (higherPriorityClipInfo.PreExtrapolation != TimelineClip.ClipExtrapolation.None)
            {
                // PreExtrapolationが有効なら、低優先順位クリップの開始時間まで高優先順位がカバー
                effectiveHigherStartTime = lowerStartTime;
            }

            if (higherPriorityClipInfo.PostExtrapolation != TimelineClip.ClipExtrapolation.None)
            {
                // PostExtrapolationが有効なら、低優先順位クリップの終了時間まで高優先順位がカバー
                effectiveHigherEndTime = lowerEndTime;
            }

            // 低優先順位カーブのキーを追加（高優先順位の有効区間外のみ）
            foreach (var key in lowerPriorityCurve.keys)
            {
                // 高優先順位の有効区間内のキーはスキップ
                if (key.time >= effectiveHigherStartTime && key.time <= effectiveHigherEndTime)
                {
                    continue;
                }
                resultCurve.AddKey(key);
            }

            // 高優先順位カーブのキーをすべて追加
            foreach (var key in higherPriorityCurve.keys)
            {
                resultCurve.AddKey(key);
            }

            // Extrapolation 処理:
            // 高優先順位クリップが存在しない区間で、高優先順位のExtrapolation設定に基づいて値を追加
            var frameRate = extrapolationProcessor.GetFrameRate();
            var frameInterval = 1f / frameRate;

            // 高優先順位クリップ開始前の区間（PreExtrapolation）
            // 低優先順位クリップの範囲内で、高優先順位より前の区間
            if (higherStartTime > lowerStartTime)
            {
                // PreExtrapolationがNone以外の場合のみ処理
                if (higherPriorityClipInfo.PreExtrapolation != TimelineClip.ClipExtrapolation.None)
                {
                    // Hold処理の場合、高優先順位カーブの最初のキー値を直接使用
                    var higherKeys = higherPriorityCurve.keys;
                    var firstKeyValue = higherKeys.Length > 0 ? higherKeys[0].value : 0f;

                    if (higherPriorityClipInfo.PreExtrapolation == TimelineClip.ClipExtrapolation.Hold)
                    {
                        // Hold: 最初のキー値で区間を埋める
                        for (var time = lowerStartTime; time < higherStartTime; time += frameInterval)
                        {
                            RemoveKeyAtTime(resultCurve, time);
                            resultCurve.AddKey(new Keyframe(time, firstKeyValue));
                        }
                    }
                    else
                    {
                        // その他のExtrapolationモード（Loop, PingPong, Continue）
                        for (var time = lowerStartTime; time < higherStartTime; time += frameInterval)
                        {
                            if (extrapolationProcessor.TryGetExtrapolatedValue(
                                higherPriorityCurve, higherPriorityClipInfo, time, out var value))
                            {
                                RemoveKeyAtTime(resultCurve, time);
                                resultCurve.AddKey(new Keyframe(time, value));
                            }
                        }
                    }
                }
            }

            // 高優先順位クリップ終了後の区間（PostExtrapolation）
            // 低優先順位クリップの範囲内で、高優先順位より後の区間
            if (higherEndTime < lowerEndTime)
            {
                // PostExtrapolationがNone以外の場合のみ処理
                if (higherPriorityClipInfo.PostExtrapolation != TimelineClip.ClipExtrapolation.None)
                {
                    // Hold処理の場合、高優先順位カーブの最後のキー値を直接使用
                    // ExtrapolationProcessorを経由すると時間座標系の変換で値がずれる可能性がある
                    var higherKeys = higherPriorityCurve.keys;
                    var lastKeyValue = higherKeys.Length > 0 ? higherKeys[higherKeys.Length - 1].value : 0f;

                    if (higherPriorityClipInfo.PostExtrapolation == TimelineClip.ClipExtrapolation.Hold)
                    {
                        // Hold: 最後のキー値で区間を埋める
                        for (var time = higherEndTime + frameInterval; time <= lowerEndTime; time += frameInterval)
                        {
                            RemoveKeyAtTime(resultCurve, time);
                            resultCurve.AddKey(new Keyframe(time, lastKeyValue));
                        }
                        // 最終時間のキーを追加
                        RemoveKeyAtTime(resultCurve, lowerEndTime);
                        resultCurve.AddKey(new Keyframe(lowerEndTime, lastKeyValue));
                    }
                    else
                    {
                        // その他のExtrapolationモード（Loop, PingPong, Continue）
                        for (var time = higherEndTime + frameInterval; time <= lowerEndTime; time += frameInterval)
                        {
                            if (extrapolationProcessor.TryGetExtrapolatedValue(
                                higherPriorityCurve, higherPriorityClipInfo, time, out var value))
                            {
                                RemoveKeyAtTime(resultCurve, time);
                                resultCurve.AddKey(new Keyframe(time, value));
                            }
                        }

                        // 最終時間に近いポイントを追加（精度確保）
                        if (extrapolationProcessor.TryGetExtrapolatedValue(
                            higherPriorityCurve, higherPriorityClipInfo, lowerEndTime, out var endValue))
                        {
                            RemoveKeyAtTime(resultCurve, lowerEndTime);
                            resultCurve.AddKey(new Keyframe(lowerEndTime, endValue));
                        }
                    }
                }
            }

            return resultCurve;
        }

        /// <summary>
        /// 指定時間付近のキーフレームを削除する
        /// </summary>
        private void RemoveKeyAtTime(AnimationCurve curve, float time, float tolerance = 0.0001f)
        {
            for (var i = curve.keys.Length - 1; i >= 0; i--)
            {
                if (Mathf.Abs(curve.keys[i].time - time) < tolerance)
                {
                    curve.RemoveKey(i);
                }
            }
        }
    }

    /// <summary>
    /// カーブと時間範囲を保持する構造体
    /// </summary>
    public struct CurveWithTimeRange
    {
        /// <summary>
        /// アニメーションカーブ
        /// </summary>
        public AnimationCurve Curve;

        /// <summary>
        /// カーブの開始時間
        /// </summary>
        public float StartTime;

        /// <summary>
        /// カーブの終了時間
        /// </summary>
        public float EndTime;

        /// <summary>
        /// アクティブ区間のリスト（null = [StartTime, EndTime]全体がアクティブ）
        /// Extrapolation=None設定時のGap区間を識別するために使用
        /// </summary>
        public List<ActiveInterval> ActiveIntervals;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="curve">アニメーションカーブ</param>
        /// <param name="startTime">開始時間</param>
        /// <param name="endTime">終了時間</param>
        public CurveWithTimeRange(AnimationCurve curve, float startTime, float endTime)
        {
            Curve = curve;
            StartTime = startTime;
            EndTime = endTime;
            ActiveIntervals = null;
        }

        /// <summary>
        /// コンストラクタ（アクティブ区間対応）
        /// </summary>
        /// <param name="curve">アニメーションカーブ</param>
        /// <param name="startTime">開始時間</param>
        /// <param name="endTime">終了時間</param>
        /// <param name="activeIntervals">アクティブ区間のリスト</param>
        public CurveWithTimeRange(AnimationCurve curve, float startTime, float endTime,
            List<ActiveInterval> activeIntervals)
        {
            Curve = curve;
            StartTime = startTime;
            EndTime = endTime;
            ActiveIntervals = activeIntervals;
        }
    }

    /// <summary>
    /// アクティブ区間を表す構造体
    /// トラック内でクリップが実際に存在する時間範囲を示す
    /// </summary>
    public struct ActiveInterval
    {
        /// <summary>
        /// 区間の開始時間
        /// </summary>
        public float StartTime;

        /// <summary>
        /// 区間の終了時間
        /// </summary>
        public float EndTime;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="startTime">開始時間</param>
        /// <param name="endTime">終了時間</param>
        public ActiveInterval(float startTime, float endTime)
        {
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}
