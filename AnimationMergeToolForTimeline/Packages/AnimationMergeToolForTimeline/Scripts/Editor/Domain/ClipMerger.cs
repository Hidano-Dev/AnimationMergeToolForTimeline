using System.Collections.Generic;
using System.Linq;
using AnimationMergeTool.Editor.Domain.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace AnimationMergeTool.Editor.Domain
{
    /// <summary>
    /// AnimationClipの統合処理を行うクラス
    /// 複数のClipInfoから単一のAnimationClipを生成する
    ///
    /// サポートするカーブタイプ:
    /// - Transformカーブ（Position, Rotation, Scale）
    /// - Animatorカーブ（マッスル、ルートモーション）
    /// - BlendShapeカーブ（blendShape.* プロパティ）
    /// </summary>
    public class ClipMerger
    {
        /// <summary>
        /// 出力AnimationClipのフレームレート
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
        /// TimelineAssetからフレームレートを取得して設定する
        /// </summary>
        /// <param name="timelineAsset">フレームレート取得元のTimelineAsset</param>
        public void SetFrameRateFromTimeline(TimelineAsset timelineAsset)
        {
            if (timelineAsset == null)
            {
                return;
            }

            var frameRate = (float)timelineAsset.editorSettings.frameRate;
            if (frameRate > 0)
            {
                _frameRate = frameRate;
            }
        }

        /// <summary>
        /// TimelineAssetからフレームレートを取得する
        /// </summary>
        /// <param name="timelineAsset">フレームレート取得元のTimelineAsset</param>
        /// <returns>フレームレート（fps）。timelineAssetがnullの場合は0を返す</returns>
        public static float GetFrameRateFromTimeline(TimelineAsset timelineAsset)
        {
            if (timelineAsset == null)
            {
                return 0f;
            }

            return (float)timelineAsset.editorSettings.frameRate;
        }

        /// <summary>
        /// 複数のClipInfoを統合して単一のAnimationClipを生成する
        /// Transform、Animator、BlendShapeなどすべてのカーブタイプを
        /// バインディング情報（path, type, propertyName）に基づいて統合する
        /// 同じバインディングのカーブは時間軸上でマージされる
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

            // 統合されたカーブを格納する辞書（EditorCurveBindingの文字列表現をキーとして使用）
            var mergedCurves = new Dictionary<string, MergedCurveData>();

            foreach (var clipInfo in clipInfos)
            {
                if (clipInfo?.AnimationClip == null)
                {
                    continue;
                }

                // ClipInfoからすべてのカーブを取得
                var curveBindingPairs = GetAnimationCurves(clipInfo.AnimationClip);

                // 時間オフセットを適用したカーブリストを作成
                var timeOffsetPairs = new List<CurveBindingPair>();
                foreach (var pair in curveBindingPairs)
                {
                    var offsetCurve = ApplyTimeOffset(pair.Curve, clipInfo);
                    if (offsetCurve != null && offsetCurve.keys.Length > 0)
                    {
                        timeOffsetPairs.Add(new CurveBindingPair(pair.Binding, offsetCurve));
                    }
                }

                // シーンオフセット（Position/Rotation）を適用
                if (clipInfo.HasSceneOffset)
                {
                    var sceneOffsetApplier = new SceneOffsetApplier();
                    timeOffsetPairs = sceneOffsetApplier.Apply(
                        timeOffsetPairs,
                        clipInfo.SceneOffsetPosition,
                        clipInfo.SceneOffsetRotation);
                }

                foreach (var pair in timeOffsetPairs)
                {
                    // バインディングを一意に識別する文字列キーを生成
                    var bindingKey = GetBindingKey(pair.Binding);

                    // 同じバインディングのカーブが既にある場合はキーフレームを統合
                    if (mergedCurves.TryGetValue(bindingKey, out var existingData))
                    {
                        MergeCurveKeys(existingData.Curve, pair.Curve);
                    }
                    else
                    {
                        mergedCurves[bindingKey] = new MergedCurveData(pair.Binding, pair.Curve);
                    }
                }
            }

            // 統合したカーブをAnimationClipに設定
            foreach (var kvp in mergedCurves)
            {
                AnimationUtility.SetEditorCurve(resultClip, kvp.Value.Binding, kvp.Value.Curve);
            }

            return resultClip;
        }

        /// <summary>
        /// EditorCurveBindingを一意に識別する文字列キーを生成する
        /// </summary>
        /// <param name="binding">EditorCurveBinding</param>
        /// <returns>一意識別キー</returns>
        private string GetBindingKey(EditorCurveBinding binding)
        {
            return $"{binding.path}|{binding.type.FullName}|{binding.propertyName}";
        }

        /// <summary>
        /// 2つのカーブのキーフレームを統合する
        /// ソースカーブのキーフレームをターゲットカーブに追加する
        /// 同じ時間のキーフレームが存在する場合は後から追加されたものが優先される
        /// </summary>
        /// <param name="targetCurve">統合先のカーブ（変更される）</param>
        /// <param name="sourceCurve">統合元のカーブ</param>
        private void MergeCurveKeys(AnimationCurve targetCurve, AnimationCurve sourceCurve)
        {
            foreach (var key in sourceCurve.keys)
            {
                // 同じ時間に既存のキーがあるかチェック
                var existingIndex = FindKeyAtTime(targetCurve, key.time);
                if (existingIndex >= 0)
                {
                    // 既存のキーを削除して新しいキーで置き換え
                    targetCurve.RemoveKey(existingIndex);
                }
                targetCurve.AddKey(key);
            }
        }

        /// <summary>
        /// 指定した時間にあるキーフレームのインデックスを探す
        /// </summary>
        /// <param name="curve">検索対象のカーブ</param>
        /// <param name="time">時間</param>
        /// <returns>キーフレームのインデックス（見つからない場合は-1）</returns>
        private int FindKeyAtTime(AnimationCurve curve, float time)
        {
            const float tolerance = 0.0001f;
            var keys = curve.keys;
            for (var i = 0; i < keys.Length; i++)
            {
                if (Mathf.Abs(keys[i].time - time) < tolerance)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// カーブに時間オフセットを適用する
        /// ClipInfoの情報（開始時間、ClipIn、TimeScale）に基づいてキーフレームの時間を調整する
        /// AnimationClipがLoop設定でTimelineClipのdurationより短い場合、ループ分のキーフレームを生成する
        /// </summary>
        /// <param name="curve">元のAnimationCurve</param>
        /// <param name="clipInfo">ClipInfo（開始時間、ClipIn、TimeScale情報を含む）</param>
        /// <returns>時間オフセットが適用された新しいAnimationCurve</returns>
        public AnimationCurve ApplyTimeOffset(AnimationCurve curve, ClipInfo clipInfo)
        {
            if (curve == null || clipInfo == null)
            {
                return null;
            }

            var originalKeys = curve.keys;
            if (originalKeys.Length == 0)
            {
                return new AnimationCurve();
            }

            var newCurve = new AnimationCurve();

            // ClipInfoから時間パラメータを取得
            var startTime = (float)clipInfo.StartTime;
            var clipIn = (float)clipInfo.ClipIn;
            var timeScale = (float)clipInfo.TimeScale;
            var duration = (float)clipInfo.Duration;

            // TimeScaleが0以下の場合は無効なので1として扱う
            if (timeScale <= 0)
            {
                timeScale = 1f;
            }

            // ソースAnimationClipの長さを取得
            var sourceClipLength = clipInfo.AnimationClip != null ? clipInfo.AnimationClip.length : 0f;

            // AnimationClipがLoop設定かどうかを確認
            var isLooping = false;
            if (clipInfo.AnimationClip != null)
            {
                var clipSettings = AnimationUtility.GetAnimationClipSettings(clipInfo.AnimationClip);
                isLooping = clipSettings.loopTime;
            }

            // ソースクリップのClipIn以降の有効な長さを計算
            var effectiveSourceLength = sourceClipLength - clipIn;
            if (effectiveSourceLength <= 0)
            {
                effectiveSourceLength = sourceClipLength;
            }

            // TimeScaleを考慮した1ループあたりの実時間
            var loopDurationInTimeline = effectiveSourceLength / timeScale;

            // ループが必要かどうか判定
            // ソースクリップがTimelineClipのdurationより短く、Loop設定の場合にループ処理を行う
            var needsLoop = isLooping && loopDurationInTimeline > 0 && duration > loopDurationInTimeline;

            // ループ回数を計算（1回目も含む）
            var loopCount = needsLoop ? Mathf.CeilToInt(duration / loopDurationInTimeline) : 1;

            const float durationTolerance = 0.0001f;

            for (var loopIndex = 0; loopIndex < loopCount; loopIndex++)
            {
                // 現在のループの開始時間（Timeline上）
                var loopStartTimeInTimeline = loopIndex * loopDurationInTimeline;

                foreach (var key in originalKeys)
                {
                    // 1. ClipInを適用（元のカーブのClipIn以降の部分のみ使用）
                    var sourceTime = key.time;

                    // ClipInより前のキーはスキップ
                    if (sourceTime < clipIn)
                    {
                        continue;
                    }

                    // ソースクリップの長さを超えるキーはスキップ（最初のループ以降では不要だが念のため）
                    if (sourceTime > sourceClipLength + durationTolerance)
                    {
                        continue;
                    }

                    // 2. TimeScaleを適用して実際の再生時間を計算
                    // ClipIn分をオフセットしてから、TimeScaleで割る
                    var localTime = (sourceTime - clipIn) / timeScale;

                    // ループオフセットを加算
                    var localTimeWithLoop = loopStartTimeInTimeline + localTime;

                    // 3. クリップのDurationを超えるキーはスキップ（浮動小数点誤差を考慮）
                    if (localTimeWithLoop > duration + durationTolerance)
                    {
                        continue;
                    }

                    // 4. Timeline上の開始時間を加算
                    var outputTime = startTime + localTimeWithLoop;

                    // 新しいキーフレームを作成
                    var newKey = new Keyframe(outputTime, key.value)
                    {
                        inTangent = key.inTangent * timeScale,
                        outTangent = key.outTangent * timeScale,
                        inWeight = key.inWeight,
                        outWeight = key.outWeight,
                        weightedMode = key.weightedMode
                    };

                    newCurve.AddKey(newKey);
                }
            }

            return newCurve;
        }

        /// <summary>
        /// AnimationClipから全てのAnimationCurveとEditorCurveBinding情報を取得する
        /// </summary>
        /// <param name="clip">取得元のAnimationClip</param>
        /// <returns>EditorCurveBindingとAnimationCurveのペアのリスト</returns>
        public List<CurveBindingPair> GetAnimationCurves(AnimationClip clip)
        {
            var result = new List<CurveBindingPair>();

            if (clip == null)
            {
                return result;
            }

            // 通常のカーブ（Transform, Animator等のfloatプロパティ）を取得
            var curveBindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in curveBindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve != null)
                {
                    result.Add(new CurveBindingPair(binding, curve));
                }
            }

            // オブジェクト参照カーブ（Sprite等）は除外
            // ObjectReferenceKeyframeはAnimationCurveとは異なる形式のため、
            // 本ツールでは対象外とする

            return result;
        }

        /// <summary>
        /// AnimationClipから指定したEditorCurveBindingに対応するカーブを取得する
        /// </summary>
        /// <param name="clip">取得元のAnimationClip</param>
        /// <param name="binding">取得対象のEditorCurveBinding</param>
        /// <returns>AnimationCurve（存在しない場合はnull）</returns>
        public AnimationCurve GetAnimationCurve(AnimationClip clip, EditorCurveBinding binding)
        {
            if (clip == null)
            {
                return null;
            }

            // まず直接取得を試みる
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve != null)
            {
                return curve;
            }

            // 直接取得できない場合は、すべてのバインディングを取得してパス・プロパティ名・型で比較
            var curveBindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var existingBinding in curveBindings)
            {
                if (existingBinding.path == binding.path &&
                    existingBinding.propertyName == binding.propertyName &&
                    existingBinding.type == binding.type)
                {
                    return AnimationUtility.GetEditorCurve(clip, existingBinding);
                }
            }

            return null;
        }
    }

    /// <summary>
    /// EditorCurveBindingとAnimationCurveのペアを保持するクラス
    /// </summary>
    public class CurveBindingPair
    {
        /// <summary>
        /// カーブのバインディング情報
        /// </summary>
        public EditorCurveBinding Binding { get; }

        /// <summary>
        /// アニメーションカーブ
        /// </summary>
        public AnimationCurve Curve { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="binding">EditorCurveBinding</param>
        /// <param name="curve">AnimationCurve</param>
        public CurveBindingPair(EditorCurveBinding binding, AnimationCurve curve)
        {
            Binding = binding;
            Curve = curve;
        }
    }

    /// <summary>
    /// 統合処理中のカーブデータを保持するクラス
    /// </summary>
    internal class MergedCurveData
    {
        /// <summary>
        /// カーブのバインディング情報
        /// </summary>
        public EditorCurveBinding Binding { get; }

        /// <summary>
        /// 統合中のアニメーションカーブ
        /// </summary>
        public AnimationCurve Curve { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="binding">EditorCurveBinding</param>
        /// <param name="curve">AnimationCurve</param>
        public MergedCurveData(EditorCurveBinding binding, AnimationCurve curve)
        {
            Binding = binding;
            Curve = curve;
        }
    }
}
