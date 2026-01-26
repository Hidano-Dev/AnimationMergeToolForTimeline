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
        private readonly HumanoidToGenericConverter _humanoidConverter;

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public FbxAnimationExporter()
        {
            _skeletonExtractor = new SkeletonExtractor();
            _humanoidConverter = new HumanoidToGenericConverter();
        }

        /// <summary>
        /// SkeletonExtractorを指定するコンストラクタ（テスト用）
        /// </summary>
        /// <param name="skeletonExtractor">スケルトン抽出器</param>
        public FbxAnimationExporter(SkeletonExtractor skeletonExtractor)
        {
            _skeletonExtractor = skeletonExtractor ?? new SkeletonExtractor();
            _humanoidConverter = new HumanoidToGenericConverter();
        }

        /// <summary>
        /// SkeletonExtractorとHumanoidToGenericConverterを指定するコンストラクタ（テスト用）
        /// </summary>
        /// <param name="skeletonExtractor">スケルトン抽出器</param>
        /// <param name="humanoidConverter">Humanoid→Generic変換器</param>
        public FbxAnimationExporter(SkeletonExtractor skeletonExtractor, HumanoidToGenericConverter humanoidConverter)
        {
            _skeletonExtractor = skeletonExtractor ?? new SkeletonExtractor();
            _humanoidConverter = humanoidConverter ?? new HumanoidToGenericConverter();
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

        #region Humanoid→Generic変換統合 (P14-009)

        /// <summary>
        /// HumanoidアニメーションをGeneric形式に変換してFBXエクスポート用データを準備する
        /// タスク P14-009: FbxAnimationExporterとの統合
        /// </summary>
        /// <param name="animator">対象のAnimator（Humanoidリグ）</param>
        /// <param name="humanoidClip">変換元のHumanoidアニメーションクリップ</param>
        /// <returns>エクスポート用に変換されたFbxExportData</returns>
        public FbxExportData PrepareHumanoidForExport(Animator animator, AnimationClip humanoidClip)
        {
            if (animator == null || humanoidClip == null)
            {
                return new FbxExportData(
                    animator,
                    humanoidClip,
                    null,
                    new List<TransformCurveData>(),
                    new List<BlendShapeCurveData>(),
                    false);
            }

            // スケルトンを抽出
            var skeleton = ExtractSkeleton(animator);
            bool isHumanoid = animator.isHuman;

            // Transformカーブのリストを準備
            var transformCurves = new List<TransformCurveData>();

            // Humanoidアニメーションの場合、マッスルカーブをRotationカーブに変換
            if (isHumanoid && _humanoidConverter.IsHumanoidClip(humanoidClip))
            {
                // マッスルカーブを変換
                var muscleCurves = _humanoidConverter.ConvertMuscleCurvesToRotation(animator, humanoidClip);
                transformCurves.AddRange(muscleCurves);

                // ルートモーションカーブを変換
                string rootBonePath = GetRootBonePath(animator);
                var rootMotionCurves = _humanoidConverter.ConvertRootMotionCurves(humanoidClip, rootBonePath);
                transformCurves.AddRange(rootMotionCurves);
            }
            else
            {
                // 非Humanoidの場合は通常のTransformカーブ抽出
                transformCurves = ExtractTransformCurves(humanoidClip, animator);
            }

            return new FbxExportData(
                animator,
                humanoidClip,
                skeleton,
                transformCurves,
                new List<BlendShapeCurveData>(),
                isHumanoid);
        }

        /// <summary>
        /// AnimatorがHumanoidリグかどうかを確認する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <returns>Humanoidリグの場合true</returns>
        public bool IsHumanoidAnimator(Animator animator)
        {
            return animator != null && animator.isHuman;
        }

        /// <summary>
        /// AnimationClipがHumanoid形式かどうかを確認する
        /// </summary>
        /// <param name="clip">対象のAnimationClip</param>
        /// <returns>Humanoid形式の場合true</returns>
        public bool IsHumanoidClip(AnimationClip clip)
        {
            return _humanoidConverter.IsHumanoidClip(clip);
        }

        /// <summary>
        /// HumanoidアニメーションをGenericカーブに変換する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="humanoidClip">変換元のAnimationClip</param>
        /// <returns>変換されたTransformカーブのリスト</returns>
        public List<TransformCurveData> ConvertHumanoidToGenericCurves(Animator animator, AnimationClip humanoidClip)
        {
            var result = new List<TransformCurveData>();

            if (animator == null || humanoidClip == null)
            {
                return result;
            }

            if (!animator.isHuman)
            {
                return result;
            }

            // マッスルカーブを変換
            var muscleCurves = _humanoidConverter.ConvertMuscleCurvesToRotation(animator, humanoidClip);
            result.AddRange(muscleCurves);

            // ルートモーションカーブを変換
            string rootBonePath = GetRootBonePath(animator);
            var rootMotionCurves = _humanoidConverter.ConvertRootMotionCurves(humanoidClip, rootBonePath);
            result.AddRange(rootMotionCurves);

            return result;
        }

        /// <summary>
        /// HumanoidアニメーションをFBXにエクスポートする
        /// </summary>
        /// <param name="animator">対象のAnimator（Humanoidリグ）</param>
        /// <param name="humanoidClip">エクスポートするHumanoidアニメーションクリップ</param>
        /// <param name="outputPath">出力先パス</param>
        /// <returns>エクスポートが成功した場合true</returns>
        public bool ExportHumanoidAnimation(Animator animator, AnimationClip humanoidClip, string outputPath)
        {
            if (animator == null)
            {
                Debug.LogError("Animatorがnullです。");
                return false;
            }

            if (humanoidClip == null)
            {
                Debug.LogError("AnimationClipがnullです。");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("出力パスが指定されていません。");
                return false;
            }

            // Humanoid用のエクスポートデータを準備
            var exportData = PrepareHumanoidForExport(animator, humanoidClip);

            if (!exportData.HasExportableData)
            {
                Debug.LogError("エクスポート可能なデータがありません。");
                return false;
            }

            // 通常のエクスポート処理を実行
            return Export(exportData, outputPath);
        }

        /// <summary>
        /// ルートボーンのパスを取得する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <returns>ルートボーンのパス（取得できない場合は空文字）</returns>
        private string GetRootBonePath(Animator animator)
        {
            if (animator == null || !animator.isHuman)
            {
                return string.Empty;
            }

            // Hipsボーンをルートとして使用
            var hipsTransform = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hipsTransform == null)
            {
                return string.Empty;
            }

            return GetBonePath(animator, hipsTransform);
        }

        #endregion

        #region BlendShapeカーブ抽出機能 (P15-002, P15-003)

        /// <summary>
        /// AnimationClipからBlendShapeカーブを抽出する
        /// タスク P15-003で本格実装予定
        /// </summary>
        /// <param name="clip">対象のAnimationClip</param>
        /// <param name="animator">対象のAnimator</param>
        /// <returns>BlendShapeカーブ情報のリスト</returns>
        public List<BlendShapeCurveData> ExtractBlendShapeCurves(AnimationClip clip, Animator animator)
        {
            var result = new List<BlendShapeCurveData>();

            if (clip == null || animator == null)
            {
                return result;
            }

            // AnimationClipからカーブバインディングを取得
            var bindings = AnimationUtility.GetCurveBindings(clip);

            foreach (var binding in bindings)
            {
                // BlendShapeカーブのみを抽出
                if (!IsBlendShapeBinding(binding))
                {
                    continue;
                }

                // カーブを取得
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                {
                    continue;
                }

                // BlendShape名を抽出（"blendShape."プレフィックスを除去）
                string blendShapeName = ExtractBlendShapeName(binding.propertyName);

                // BlendShapeCurveDataを作成
                var curveData = new BlendShapeCurveData(
                    binding.path,
                    blendShapeName,
                    curve);

                result.Add(curveData);
            }

            return result;
        }

        /// <summary>
        /// AnimationClipにBlendShapeカーブが含まれているかを確認する
        /// </summary>
        /// <param name="clip">対象のAnimationClip</param>
        /// <returns>BlendShapeカーブが存在する場合true</returns>
        public bool HasBlendShapeCurves(AnimationClip clip)
        {
            if (clip == null)
            {
                return false;
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                if (IsBlendShapeBinding(binding))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// BlendShapeカーブ情報からFbxExportDataを準備する
        /// タスク P15-003で本格実装予定
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="clip">対象のAnimationClip</param>
        /// <param name="blendShapeCurves">BlendShapeカーブ情報のリスト</param>
        /// <returns>FbxExportData</returns>
        public FbxExportData PrepareBlendShapeCurvesForExport(
            Animator animator,
            AnimationClip clip,
            List<BlendShapeCurveData> blendShapeCurves)
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
                new List<TransformCurveData>(),
                blendShapeCurves ?? new List<BlendShapeCurveData>(),
                isHumanoid);
        }

        /// <summary>
        /// EditorCurveBindingがBlendShapeカーブかどうかを判定する
        /// </summary>
        /// <param name="binding">判定対象のバインディング</param>
        /// <returns>BlendShapeカーブの場合true</returns>
        private bool IsBlendShapeBinding(EditorCurveBinding binding)
        {
            // SkinnedMeshRenderer型でblendShape.プレフィックスを持つプロパティ
            return binding.type == typeof(SkinnedMeshRenderer) &&
                   !string.IsNullOrEmpty(binding.propertyName) &&
                   binding.propertyName.StartsWith("blendShape.");
        }

        /// <summary>
        /// プロパティ名からBlendShape名を抽出する
        /// </summary>
        /// <param name="propertyName">プロパティ名（例: "blendShape.smile"）</param>
        /// <returns>BlendShape名（例: "smile"）</returns>
        private string ExtractBlendShapeName(string propertyName)
        {
            const string prefix = "blendShape.";
            if (string.IsNullOrEmpty(propertyName) || !propertyName.StartsWith(prefix))
            {
                return string.Empty;
            }

            return propertyName.Substring(prefix.Length);
        }

        #endregion
    }
}
