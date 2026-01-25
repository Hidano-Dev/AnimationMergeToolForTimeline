using System.Collections.Generic;
using AnimationMergeTool.Editor.Domain;
using AnimationMergeTool.Editor.Domain.Models;
using UnityEditor;
using UnityEngine;

namespace AnimationMergeTool.Editor.Infrastructure
{
    /// <summary>
    /// AnimationClipをアセットとしてエクスポートするクラス
    /// </summary>
    public class AnimationClipExporter
    {
        private readonly FileNameGenerator _fileNameGenerator;

        /// <summary>
        /// デフォルトフレームレート
        /// </summary>
        private const float DefaultFrameRate = 60f;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="fileNameGenerator">ファイル名生成器</param>
        public AnimationClipExporter(FileNameGenerator fileNameGenerator)
        {
            _fileNameGenerator = fileNameGenerator;
        }

        /// <summary>
        /// MergeResultからAnimationClipを生成する
        /// </summary>
        /// <param name="mergeResult">マージ結果</param>
        /// <param name="curveBindingPairs">カーブバインディングペアのリスト</param>
        /// <param name="frameRate">フレームレート（デフォルト: 60fps）</param>
        /// <returns>生成されたAnimationClip</returns>
        public AnimationClip CreateAnimationClip(
            MergeResult mergeResult,
            List<CurveBindingPair> curveBindingPairs,
            float frameRate = DefaultFrameRate)
        {
            if (mergeResult == null)
            {
                Debug.LogError("MergeResultがnullです。");
                return null;
            }

            if (curveBindingPairs == null || curveBindingPairs.Count == 0)
            {
                mergeResult.AddErrorLog("カーブデータが空のためAnimationClipを生成できません。");
                return null;
            }

            // フレームレートの検証
            if (frameRate <= 0)
            {
                frameRate = DefaultFrameRate;
            }

            // AnimationClipを作成
            var clip = new AnimationClip
            {
                frameRate = frameRate
            };

            // カーブを設定
            var curveCount = 0;
            foreach (var pair in curveBindingPairs)
            {
                if (pair.Curve == null || pair.Curve.keys.Length == 0)
                {
                    continue;
                }

                AnimationUtility.SetEditorCurve(clip, pair.Binding, pair.Curve);
                curveCount++;
            }

            if (curveCount == 0)
            {
                mergeResult.AddErrorLog("有効なカーブが存在しないためAnimationClipを生成できません。");
                return null;
            }

            // MergeResultにクリップを設定
            mergeResult.GeneratedClip = clip;
            mergeResult.AddLog($"AnimationClipを生成しました。カーブ数: {curveCount}, フレームレート: {frameRate}fps");

            return clip;
        }

        /// <summary>
        /// カーブバインディングペアのリストからAnimationClipを生成する（MergeResult不要版）
        /// </summary>
        /// <param name="curveBindingPairs">カーブバインディングペアのリスト</param>
        /// <param name="frameRate">フレームレート（デフォルト: 60fps）</param>
        /// <returns>生成されたAnimationClip、失敗時はnull</returns>
        public AnimationClip CreateAnimationClip(
            List<CurveBindingPair> curveBindingPairs,
            float frameRate = DefaultFrameRate)
        {
            if (curveBindingPairs == null || curveBindingPairs.Count == 0)
            {
                Debug.LogError("カーブデータが空のためAnimationClipを生成できません。");
                return null;
            }

            // フレームレートの検証
            if (frameRate <= 0)
            {
                frameRate = DefaultFrameRate;
            }

            // AnimationClipを作成
            var clip = new AnimationClip
            {
                frameRate = frameRate
            };

            // カーブを設定
            var curveCount = 0;
            foreach (var pair in curveBindingPairs)
            {
                if (pair.Curve == null || pair.Curve.keys.Length == 0)
                {
                    continue;
                }

                AnimationUtility.SetEditorCurve(clip, pair.Binding, pair.Curve);
                curveCount++;
            }

            if (curveCount == 0)
            {
                Debug.LogError("有効なカーブが存在しないためAnimationClipを生成できません。");
                return null;
            }

            return clip;
        }
    }
}
