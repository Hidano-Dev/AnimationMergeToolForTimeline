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

            // 低優先順位カーブのキーを追加（重なり区間外のみ）
            foreach (var key in lowerPriorityCurve.keys)
            {
                // 高優先順位の区間内のキーはスキップ
                if (key.time >= higherPriorityStartTime && key.time <= higherPriorityEndTime)
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

                result = ApplyPartialOverride(
                    result,
                    higherPriority.Curve,
                    higherPriority.StartTime,
                    higherPriority.EndTime);
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

            // 低優先順位カーブのキーを追加（高優先順位の区間外のみ）
            foreach (var key in lowerPriorityCurve.keys)
            {
                // 高優先順位の区間内のキーはスキップ
                if (key.time >= higherStartTime && key.time <= higherEndTime)
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
                    // 高優先順位クリップのPreExtrapolationで低優先順位のキーを上書き
                    for (var time = lowerStartTime; time < higherStartTime; time += frameInterval)
                    {
                        if (extrapolationProcessor.TryGetExtrapolatedValue(
                            higherPriorityCurve, higherPriorityClipInfo, time, out var value))
                        {
                            // 既存のキーを削除して新しい値で置換
                            RemoveKeyAtTime(resultCurve, time);
                            resultCurve.AddKey(new Keyframe(time, value));
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
                    // 高優先順位クリップのPostExtrapolationで低優先順位のキーを上書き
                    for (var time = higherEndTime + frameInterval; time <= lowerEndTime; time += frameInterval)
                    {
                        if (extrapolationProcessor.TryGetExtrapolatedValue(
                            higherPriorityCurve, higherPriorityClipInfo, time, out var value))
                        {
                            // 既存のキーを削除して新しい値で置換
                            RemoveKeyAtTime(resultCurve, time);
                            resultCurve.AddKey(new Keyframe(time, value));
                        }
                    }

                    // 最終時間に近いポイントを追加（精度確保）
                    var lastTime = lowerEndTime;
                    if (extrapolationProcessor.TryGetExtrapolatedValue(
                        higherPriorityCurve, higherPriorityClipInfo, lastTime, out var lastValue))
                    {
                        RemoveKeyAtTime(resultCurve, lastTime);
                        resultCurve.AddKey(new Keyframe(lastTime, lastValue));
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
    /// EditorCurveBindingとAnimationCurveのペアを保持する構造体
    /// </summary>
    public struct CurveBindingPair
    {
        /// <summary>
        /// カーブのバインディング情報
        /// </summary>
        public EditorCurveBinding Binding;

        /// <summary>
        /// アニメーションカーブ
        /// </summary>
        public AnimationCurve Curve;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="binding">バインディング情報</param>
        /// <param name="curve">アニメーションカーブ</param>
        public CurveBindingPair(EditorCurveBinding binding, AnimationCurve curve)
        {
            Binding = binding;
            Curve = curve;
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
        }
    }
}
