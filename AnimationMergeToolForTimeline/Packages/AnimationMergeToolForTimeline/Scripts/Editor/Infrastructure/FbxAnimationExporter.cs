using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AnimationMergeTool.Editor.Domain.Models;
#if UNITY_FORMATS_FBX
using UnityEditor.Formats.Fbx.Exporter;
#endif

namespace AnimationMergeTool.Editor.Infrastructure
{
    /// <summary>
    /// FBXアニメーションエクスポーターの基本クラス
    /// FBX Exporter API（com.unity.formats.fbx）のラッパーとして機能する
    /// </summary>
    public class FbxAnimationExporter
    {
        private readonly SkeletonExtractor _skeletonExtractor;

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public FbxAnimationExporter()
        {
            _skeletonExtractor = new SkeletonExtractor();
        }

        /// <summary>
        /// SkeletonExtractorを指定するコンストラクタ（テスト用）
        /// </summary>
        /// <param name="skeletonExtractor">スケルトン抽出器</param>
        public FbxAnimationExporter(SkeletonExtractor skeletonExtractor)
        {
            _skeletonExtractor = skeletonExtractor ?? new SkeletonExtractor();
        }

        /// <summary>
        /// Animatorからスケルトン情報を取得する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <returns>スケルトン情報（取得できない場合は空のSkeletonData）</returns>
        public SkeletonData ExtractSkeleton(Animator animator)
        {
            return _skeletonExtractor.Extract(animator);
        }

        /// <summary>
        /// ボーンのAnimator相対パスを取得する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="bone">パスを取得するボーン</param>
        /// <returns>Animator相対パス（Animator自身の場合は空文字）</returns>
        public string GetBonePath(Animator animator, Transform bone)
        {
            return _skeletonExtractor.GetBonePath(animator, bone);
        }

        /// <summary>
        /// FBXエクスポート機能が利用可能かどうかを確認する
        /// </summary>
        /// <returns>FBX Exporterパッケージがインストールされている場合はtrue</returns>
        public bool IsAvailable()
        {
            return FbxPackageChecker.IsPackageInstalled();
        }

        /// <summary>
        /// 指定されたエクスポートデータがエクスポート可能かどうかを確認する
        /// </summary>
        /// <param name="exportData">エクスポートデータ</param>
        /// <returns>エクスポート可能な場合はtrue</returns>
        public bool CanExport(FbxExportData exportData)
        {
            if (exportData == null)
            {
                return false;
            }

            return exportData.HasExportableData;
        }

        /// <summary>
        /// FBXファイルにエクスポートする
        /// </summary>
        /// <param name="exportData">エクスポートデータ</param>
        /// <param name="outputPath">出力先パス</param>
        /// <returns>エクスポートが成功した場合はtrue</returns>
        public bool Export(FbxExportData exportData, string outputPath)
        {
            // 入力検証
            if (exportData == null)
            {
                Debug.LogError("FbxExportDataがnullです。");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("出力パスが指定されていません。");
                return false;
            }

            if (!exportData.HasExportableData)
            {
                Debug.LogError("エクスポート可能なデータがありません。");
                return false;
            }

            // パッケージがインストールされているか確認
            if (!IsAvailable())
            {
                Debug.LogError("FBX Exporterパッケージがインストールされていません。");
                return false;
            }

            // P12-007で実装予定: 実際のFBXエクスポート処理
            return ExportInternal(exportData, outputPath);
        }

        /// <summary>
        /// エクスポートデータの検証を行う
        /// </summary>
        /// <param name="exportData">検証対象のエクスポートデータ</param>
        /// <returns>エラーメッセージ（問題がない場合はnull）</returns>
        public string ValidateExportData(FbxExportData exportData)
        {
            if (exportData == null)
            {
                return "FbxExportDataがnullです。";
            }

            if (!exportData.HasExportableData)
            {
                return "エクスポート可能なデータがありません。";
            }

            return null;
        }

        /// <summary>
        /// サポートされているエクスポートオプションを取得する
        /// </summary>
        /// <returns>エクスポートオプションのリスト</returns>
        public IReadOnlyList<string> GetSupportedExportOptions()
        {
            return new List<string>
            {
                "AnimationOnly",
                "WithSkeleton",
                "WithBlendShapes"
            };
        }

        /// <summary>
        /// 内部エクスポート処理
        /// ModelExporter APIを使用してFBXをエクスポートする
        /// </summary>
        private bool ExportInternal(FbxExportData exportData, string outputPath)
        {
#if UNITY_FORMATS_FBX
            try
            {
                // エクスポート対象のGameObjectを取得
                GameObject exportTarget = GetExportTarget(exportData);
                if (exportTarget == null)
                {
                    Debug.LogError("エクスポート対象のGameObjectを取得できませんでした。");
                    return false;
                }

                // 一時的にAnimationClipをAnimatorにアタッチしてエクスポート
                bool isTemporaryObject = false;
                if (exportData.SourceAnimator == null)
                {
                    // Animatorがない場合は一時オブジェクトを作成
                    exportTarget = CreateTemporaryExportObject(exportData);
                    isTemporaryObject = true;
                }

                try
                {
                    // FBXエクスポート実行
                    string result = ModelExporter.ExportObject(outputPath, exportTarget);

                    if (string.IsNullOrEmpty(result))
                    {
                        Debug.LogError($"FBXエクスポートに失敗しました: {outputPath}");
                        return false;
                    }

                    // アセットデータベースを更新
                    AssetDatabase.Refresh();

                    Debug.Log($"FBXエクスポート完了: {result}");
                    return true;
                }
                finally
                {
                    // 一時オブジェクトを削除
                    if (isTemporaryObject && exportTarget != null)
                    {
                        Object.DestroyImmediate(exportTarget);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"FBXエクスポートに失敗しました: {ex.Message}");
                return false;
            }
#else
            Debug.LogError("FBX Exporterパッケージがインストールされていません。");
            return false;
#endif
        }

        /// <summary>
        /// エクスポート対象のGameObjectを取得する
        /// </summary>
        private GameObject GetExportTarget(FbxExportData exportData)
        {
            if (exportData.SourceAnimator != null)
            {
                return exportData.SourceAnimator.gameObject;
            }

            // スケルトンがある場合はルートボーンのGameObjectを返す
            if (exportData.Skeleton != null && exportData.Skeleton.RootBone != null)
            {
                return exportData.Skeleton.RootBone.gameObject;
            }

            return null;
        }

        /// <summary>
        /// エクスポート用の一時GameObjectを作成する
        /// </summary>
        private GameObject CreateTemporaryExportObject(FbxExportData exportData)
        {
            // 一時的なGameObjectを作成
            var tempObject = new GameObject("TempExportObject");

            // スケルトン情報がある場合はボーン階層を構築
            if (exportData.Skeleton != null && exportData.Skeleton.HasSkeleton)
            {
                // ボーン階層の複製は複雑なため、ルートボーンを参照
                // （Phase 13で詳細実装予定）
                tempObject.transform.position = exportData.Skeleton.RootBone.position;
                tempObject.transform.rotation = exportData.Skeleton.RootBone.rotation;
            }

            // Animatorを追加してAnimationClipを設定
            if (exportData.MergedClip != null)
            {
                var animator = tempObject.AddComponent<Animator>();
                // RuntimeAnimatorControllerの設定はPhase 13以降で実装
            }

            return tempObject;
        }

        #region Transformカーブ出力機能 (P13-006, P13-007)

        /// <summary>
        /// TransformカーブをFBXにエクスポートする
        /// タスク P13-007で実装
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="clip">対象のAnimationClip</param>
        /// <param name="outputPath">出力先パス</param>
        /// <returns>エクスポートが成功した場合はtrue</returns>
        public bool ExportTransformCurvesToFbx(Animator animator, AnimationClip clip, string outputPath)
        {
            if (clip == null)
            {
                Debug.LogError("AnimationClipがnullです。");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("出力パスが指定されていません。");
                return false;
            }

            // Transformカーブを抽出
            var transformCurves = ExtractTransformCurves(clip, animator);

            if (transformCurves.Count == 0)
            {
                Debug.LogError("エクスポート可能なTransformカーブがありません。");
                return false;
            }

            // FbxExportDataを準備
            var exportData = PrepareTransformCurvesForExport(animator, clip, transformCurves);

            // エクスポート実行
            return Export(exportData, outputPath);
        }

        /// <summary>
        /// TransformカーブデータからAnimationClipを生成する
        /// タスク P13-007で実装
        /// </summary>
        /// <param name="transformCurves">Transformカーブ情報のリスト</param>
        /// <param name="clipName">生成するAnimationClipの名前</param>
        /// <returns>生成されたAnimationClip</returns>
        public AnimationClip CreateAnimationClipFromTransformCurves(
            List<TransformCurveData> transformCurves,
            string clipName = "ExportedAnimation")
        {
            if (transformCurves == null || transformCurves.Count == 0)
            {
                return null;
            }

            var clip = new AnimationClip();
            clip.name = clipName;

            foreach (var curveData in transformCurves)
            {
                if (curveData.Curve == null)
                {
                    continue;
                }

                // カーブをAnimationClipに設定
                clip.SetCurve(
                    curveData.Path,
                    typeof(Transform),
                    curveData.PropertyName,
                    curveData.Curve);
            }

            return clip;
        }

        /// <summary>
        /// TransformカーブデータをFbxExportDataに適用してエクスポートを実行する
        /// タスク P13-007で実装
        /// </summary>
        /// <param name="exportData">エクスポートデータ</param>
        /// <param name="outputPath">出力先パス</param>
        /// <returns>エクスポートが成功した場合はtrue</returns>
        public bool ExportWithTransformCurves(FbxExportData exportData, string outputPath)
        {
            if (exportData == null)
            {
                Debug.LogError("FbxExportDataがnullです。");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("出力パスが指定されていません。");
                return false;
            }

            // Transformカーブが存在するか確認
            if (exportData.TransformCurves == null || exportData.TransformCurves.Count == 0)
            {
                Debug.LogError("エクスポート可能なTransformカーブがありません。");
                return false;
            }

            // 通常のエクスポート処理を実行
            return Export(exportData, outputPath);
        }

        /// <summary>
        /// AnimationClipからTransformカーブを抽出する
        /// タスク P13-006で実装
        /// </summary>
        /// <param name="clip">対象のAnimationClip</param>
        /// <param name="animator">対象のAnimator</param>
        /// <returns>Transformカーブ情報のリスト</returns>
        public List<TransformCurveData> ExtractTransformCurves(AnimationClip clip, Animator animator)
        {
            var result = new List<TransformCurveData>();

            if (clip == null || animator == null)
            {
                return result;
            }

            // AnimationClipからカーブバインディングを取得
            var bindings = AnimationUtility.GetCurveBindings(clip);

            foreach (var binding in bindings)
            {
                // Transform型のカーブのみを抽出
                if (binding.type != typeof(Transform))
                {
                    continue;
                }

                // カーブを取得
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                {
                    continue;
                }

                // カーブタイプを判定
                var curveType = DetermineTransformCurveType(binding.propertyName);

                // TransformCurveDataを作成
                var curveData = new TransformCurveData(
                    binding.path,
                    binding.propertyName,
                    curve,
                    curveType);

                result.Add(curveData);
            }

            return result;
        }

        /// <summary>
        /// プロパティ名からTransformカーブタイプを判定する
        /// </summary>
        /// <param name="propertyName">プロパティ名</param>
        /// <returns>TransformCurveType</returns>
        private TransformCurveType DetermineTransformCurveType(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return TransformCurveType.Position;
            }

            if (propertyName.StartsWith("localPosition") || propertyName.StartsWith("m_LocalPosition"))
            {
                return TransformCurveType.Position;
            }

            if (propertyName.StartsWith("localRotation") || propertyName.StartsWith("m_LocalRotation"))
            {
                return TransformCurveType.Rotation;
            }

            if (propertyName.StartsWith("localScale") || propertyName.StartsWith("m_LocalScale"))
            {
                return TransformCurveType.Scale;
            }

            if (propertyName.StartsWith("localEulerAngles") || propertyName.StartsWith("m_LocalEulerAngles"))
            {
                return TransformCurveType.EulerAngles;
            }

            // デフォルトはPosition
            return TransformCurveType.Position;
        }

        /// <summary>
        /// Transformカーブ情報からFbxExportDataを準備する
        /// タスク P13-006で実装
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="clip">対象のAnimationClip</param>
        /// <param name="transformCurves">Transformカーブ情報のリスト</param>
        /// <returns>FbxExportData</returns>
        public FbxExportData PrepareTransformCurvesForExport(
            Animator animator,
            AnimationClip clip,
            List<TransformCurveData> transformCurves)
        {
            // スケルトンを抽出（Animatorがある場合）
            SkeletonData skeleton = null;
            bool isHumanoid = false;

            if (animator != null)
            {
                skeleton = ExtractSkeleton(animator);
                isHumanoid = animator.isHuman;
            }

            // FbxExportDataを作成
            return new FbxExportData(
                animator,
                clip,
                skeleton,
                transformCurves ?? new List<TransformCurveData>(),
                new List<BlendShapeCurveData>(),
                isHumanoid);
        }

        #endregion
    }
}
