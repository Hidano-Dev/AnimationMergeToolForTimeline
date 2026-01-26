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

        /// <summary>
        /// AnimationClipをアセットとして保存する
        /// </summary>
        /// <param name="clip">保存するAnimationClip</param>
        /// <param name="timelineAssetName">TimelineAssetの名前（ファイル名生成用）</param>
        /// <param name="animatorName">Animatorの名前（ファイル名生成用）</param>
        /// <param name="directory">保存先ディレクトリ（デフォルト: "Assets"）</param>
        /// <returns>保存されたアセットのパス、失敗時はnull</returns>
        public string SaveAsAsset(
            AnimationClip clip,
            string timelineAssetName,
            string animatorName,
            string directory = "Assets")
        {
            if (clip == null)
            {
                Debug.LogError("保存するAnimationClipがnullです。");
                return null;
            }

            if (string.IsNullOrEmpty(directory))
            {
                directory = "Assets";
            }

            // ディレクトリが存在しない場合はエラー
            if (!AssetDatabase.IsValidFolder(directory))
            {
                Debug.LogError($"保存先ディレクトリが存在しません: {directory}");
                return null;
            }

            // ファイルパスを生成
            string filePath;
            try
            {
                filePath = _fileNameGenerator.GenerateUniqueFilePath(directory, timelineAssetName, animatorName);
            }
            catch (System.InvalidOperationException)
            {
                // IFileExistenceCheckerが設定されていない場合は基本ファイル名を使用
                var baseName = _fileNameGenerator.GenerateBaseName(timelineAssetName, animatorName);
                filePath = $"{directory.TrimEnd('/', '\\')}/{baseName}";
            }

            // アセットとして保存
            AssetDatabase.CreateAsset(clip, filePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"AnimationClipを保存しました: {filePath}");

            return filePath;
        }

        /// <summary>
        /// MergeResultからAnimationClipを生成してアセットとして保存する
        /// </summary>
        /// <param name="mergeResult">マージ結果</param>
        /// <param name="curveBindingPairs">カーブバインディングペアのリスト</param>
        /// <param name="timelineAssetName">TimelineAssetの名前（ファイル名生成用）</param>
        /// <param name="animatorName">Animatorの名前（ファイル名生成用）</param>
        /// <param name="frameRate">フレームレート（デフォルト: 60fps）</param>
        /// <param name="directory">保存先ディレクトリ（デフォルト: "Assets"）</param>
        /// <returns>保存されたアセットのパス、失敗時はnull</returns>
        public string ExportToAsset(
            MergeResult mergeResult,
            List<CurveBindingPair> curveBindingPairs,
            string timelineAssetName,
            string animatorName,
            float frameRate = DefaultFrameRate,
            string directory = "Assets")
        {
            // AnimationClipを生成
            var clip = CreateAnimationClip(mergeResult, curveBindingPairs, frameRate);
            if (clip == null)
            {
                return null;
            }

            // アセットとして保存
            var savedPath = SaveAsAsset(clip, timelineAssetName, animatorName, directory);
            if (savedPath != null)
            {
                mergeResult.AddLog($"アセットを保存しました: {savedPath}");
            }
            else
            {
                // 保存に失敗した場合はGeneratedClipをnullに戻してIsSuccessをfalseにする
                mergeResult.GeneratedClip = null;
                mergeResult.AddErrorLog("アセットの保存に失敗しました。");
            }

            return savedPath;
        }
    }
}
