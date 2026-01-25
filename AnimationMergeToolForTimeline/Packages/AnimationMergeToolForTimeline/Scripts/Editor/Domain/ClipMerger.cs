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
