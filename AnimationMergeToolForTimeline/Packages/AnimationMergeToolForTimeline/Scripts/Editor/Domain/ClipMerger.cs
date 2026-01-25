using System.Collections.Generic;
using AnimationMergeTool.Editor.Domain.Models;
using UnityEditor;
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

            // TODO: 3.2.3〜3.2.4で実装予定
            // - 時間オフセット適用機能
            // - カーブ統合機能

            return resultClip;
        }

        /// <summary>
        /// カーブに時間オフセットを適用する
        /// ClipInfoの情報（開始時間、ClipIn、TimeScale）に基づいてキーフレームの時間を調整する
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

            foreach (var key in originalKeys)
            {
                // 1. ClipInを適用（元のカーブのClipIn以降の部分のみ使用）
                var sourceTime = key.time;

                // ClipInより前のキーはスキップ
                if (sourceTime < clipIn)
                {
                    continue;
                }

                // 2. TimeScaleを適用して実際の再生時間を計算
                // ClipIn分をオフセットしてから、TimeScaleで割る
                var localTime = (sourceTime - clipIn) / timeScale;

                // 3. クリップのDurationを超えるキーはスキップ
                if (localTime > duration)
                {
                    continue;
                }

                // 4. Timeline上の開始時間を加算
                var outputTime = startTime + localTime;

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

            // カーブの補間モードを維持するため、元のカーブの設定を可能な限りコピー
            // （AddKeyで追加したキーのtangentModeは個別に設定する必要がある）

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

            return AnimationUtility.GetEditorCurve(clip, binding);
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
}
