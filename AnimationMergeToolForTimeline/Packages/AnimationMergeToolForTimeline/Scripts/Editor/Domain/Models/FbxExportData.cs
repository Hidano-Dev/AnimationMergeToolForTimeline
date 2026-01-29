using System.Collections.Generic;
using UnityEngine;

namespace AnimationMergeTool.Editor.Domain.Models
{
    /// <summary>
    /// FBXエクスポートに必要なデータを保持するDTO
    /// </summary>
    public class FbxExportData
    {
        /// <summary>
        /// ソースAnimator
        /// </summary>
        public Animator SourceAnimator { get; }

        /// <summary>
        /// マージ済みAnimationClip
        /// </summary>
        public AnimationClip MergedClip { get; }

        /// <summary>
        /// スケルトン（ボーン階層）情報
        /// </summary>
        public SkeletonData Skeleton { get; }

        /// <summary>
        /// Transformアニメーションカーブ情報リスト
        /// </summary>
        public IReadOnlyList<TransformCurveData> TransformCurves { get; }

        /// <summary>
        /// BlendShapeアニメーションカーブ情報リスト
        /// </summary>
        public IReadOnlyList<BlendShapeCurveData> BlendShapeCurves { get; }

        /// <summary>
        /// エクスポート可能なデータが存在するか
        /// </summary>
        public bool HasExportableData =>
            (Skeleton != null && Skeleton.HasSkeleton) ||
            (TransformCurves != null && TransformCurves.Count > 0) ||
            (BlendShapeCurves != null && BlendShapeCurves.Count > 0);

        /// <summary>
        /// ソースAnimatorがHumanoidリグか
        /// </summary>
        public bool IsHumanoid { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="sourceAnimator">ソースAnimator</param>
        /// <param name="mergedClip">マージ済みAnimationClip</param>
        /// <param name="skeleton">スケルトン情報</param>
        /// <param name="transformCurves">Transformカーブリスト</param>
        /// <param name="blendShapeCurves">BlendShapeカーブリスト</param>
        /// <param name="isHumanoid">Humanoidリグかどうか</param>
        public FbxExportData(
            Animator sourceAnimator,
            AnimationClip mergedClip,
            SkeletonData skeleton,
            IReadOnlyList<TransformCurveData> transformCurves,
            IReadOnlyList<BlendShapeCurveData> blendShapeCurves,
            bool isHumanoid)
        {
            SourceAnimator = sourceAnimator;
            MergedClip = mergedClip;
            Skeleton = skeleton;
            TransformCurves = transformCurves ?? new List<TransformCurveData>();
            BlendShapeCurves = blendShapeCurves ?? new List<BlendShapeCurveData>();
            IsHumanoid = isHumanoid;
        }
    }

    /// <summary>
    /// スケルトン（ボーン階層）情報を保持するDTO
    /// </summary>
    public class SkeletonData
    {
        /// <summary>
        /// ルートボーンのTransform
        /// </summary>
        public Transform RootBone { get; }

        /// <summary>
        /// 全ボーンのTransformリスト（階層順）
        /// </summary>
        public IReadOnlyList<Transform> Bones { get; }

        /// <summary>
        /// スケルトンが存在するか
        /// </summary>
        public bool HasSkeleton => RootBone != null && Bones != null && Bones.Count > 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="rootBone">ルートボーンのTransform</param>
        /// <param name="bones">全ボーンのTransformリスト</param>
        public SkeletonData(Transform rootBone, IReadOnlyList<Transform> bones)
        {
            RootBone = rootBone;
            Bones = bones ?? new List<Transform>();
        }
    }

    /// <summary>
    /// Transformカーブの種類
    /// </summary>
    public enum TransformCurveType
    {
        Position,
        Rotation,
        Scale,
        EulerAngles
    }

    /// <summary>
    /// Transformアニメーションカーブ情報を保持するDTO
    /// </summary>
    public class TransformCurveData
    {
        /// <summary>
        /// 対象TransformのAnimator相対パス
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// プロパティ名（localPosition.x, localRotation.x等）
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// アニメーションカーブ
        /// </summary>
        public AnimationCurve Curve { get; }

        /// <summary>
        /// カーブの種類
        /// </summary>
        public TransformCurveType CurveType { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">対象TransformのAnimator相対パス</param>
        /// <param name="propertyName">プロパティ名</param>
        /// <param name="curve">アニメーションカーブ</param>
        /// <param name="curveType">カーブの種類</param>
        public TransformCurveData(string path, string propertyName, AnimationCurve curve, TransformCurveType curveType)
        {
            Path = path ?? string.Empty;
            PropertyName = propertyName ?? string.Empty;
            Curve = curve;
            CurveType = curveType;
        }
    }

    /// <summary>
    /// BlendShapeアニメーションカーブ情報を保持するDTO
    /// </summary>
    public class BlendShapeCurveData
    {
        /// <summary>
        /// 対象SkinnedMeshRendererのAnimator相対パス
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// BlendShape名（blendShape.を除いた名前）
        /// </summary>
        public string BlendShapeName { get; }

        /// <summary>
        /// アニメーションカーブ
        /// </summary>
        public AnimationCurve Curve { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">対象SkinnedMeshRendererのAnimator相対パス</param>
        /// <param name="blendShapeName">BlendShape名</param>
        /// <param name="curve">アニメーションカーブ</param>
        public BlendShapeCurveData(string path, string blendShapeName, AnimationCurve curve)
        {
            Path = path ?? string.Empty;
            BlendShapeName = blendShapeName ?? string.Empty;
            Curve = curve;
        }
    }
}
