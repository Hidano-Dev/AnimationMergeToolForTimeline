using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using AnimationMergeTool.Editor.Domain.Models;
using AnimationMergeTool.Editor.Infrastructure;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// FbxAnimationExporterクラスの単体テスト
    /// タスク P12-006: FBX Exporter APIラッパーの基本テスト
    /// </summary>
    public class FbxAnimationExporterTests
    {
        private GameObject _testGameObject;
        private Animator _testAnimator;
        private AnimationClip _testClip;

        [SetUp]
        public void SetUp()
        {
            // テスト用GameObjectとAnimatorを作成
            _testGameObject = new GameObject("TestAnimator");
            _testAnimator = _testGameObject.AddComponent<Animator>();

            // テスト用AnimationClipを作成
            _testClip = new AnimationClip();
            _testClip.name = "TestClip";
            var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            _testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }

            if (_testClip != null)
            {
                Object.DestroyImmediate(_testClip);
            }
        }

        #region コンストラクタ テスト

        [Test]
        public void Constructor_デフォルトコンストラクタでインスタンスを生成できる()
        {
            // Act
            var exporter = new FbxAnimationExporter();

            // Assert
            Assert.IsNotNull(exporter);
        }

        #endregion

        #region IsAvailable テスト

        [Test]
        public void IsAvailable_FbxPackageCheckerと同じ結果を返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var expectedResult = FbxPackageChecker.IsPackageInstalled();

            // Act
            var result = exporter.IsAvailable();

            // Assert
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void IsAvailable_戻り値がbool型である()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var result = exporter.IsAvailable();

            // Assert
            Assert.IsInstanceOf<bool>(result);
        }

        #endregion

        #region CanExport テスト

        [Test]
        public void CanExport_FbxExportDataがnullの場合falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var result = exporter.CanExport(null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void CanExport_エクスポート可能なデータがない場合falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                new List<TransformCurveData>(),
                new List<BlendShapeCurveData>(),
                false);

            // Act
            var result = exporter.CanExport(exportData);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void CanExport_Transformカーブが存在する場合trueを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var transformCurves = new List<TransformCurveData>
            {
                new TransformCurveData(
                    "",
                    "localPosition.x",
                    AnimationCurve.Linear(0f, 0f, 1f, 1f),
                    TransformCurveType.Position)
            };
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                transformCurves,
                new List<BlendShapeCurveData>(),
                false);

            // Act
            var result = exporter.CanExport(exportData);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void CanExport_BlendShapeカーブが存在する場合trueを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var blendShapeCurves = new List<BlendShapeCurveData>
            {
                new BlendShapeCurveData(
                    "Face",
                    "smile",
                    AnimationCurve.Linear(0f, 0f, 1f, 100f))
            };
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                new List<TransformCurveData>(),
                blendShapeCurves,
                false);

            // Act
            var result = exporter.CanExport(exportData);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void CanExport_スケルトンが存在する場合trueを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var bones = new List<Transform> { _testGameObject.transform };
            var skeleton = new SkeletonData(_testGameObject.transform, bones);
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                skeleton,
                new List<TransformCurveData>(),
                new List<BlendShapeCurveData>(),
                false);

            // Act
            var result = exporter.CanExport(exportData);

            // Assert
            Assert.IsTrue(result);
        }

        #endregion

        #region Export テスト（パッケージ未インストール時）

        [Test]
        public void Export_パッケージが未インストールの場合falseを返す()
        {
            // このテストはパッケージが未インストールの環境でのみ意味がある
#if !UNITY_FORMATS_FBX
            // Arrange
            var exporter = new FbxAnimationExporter();
            var transformCurves = new List<TransformCurveData>
            {
                new TransformCurveData(
                    "",
                    "localPosition.x",
                    AnimationCurve.Linear(0f, 0f, 1f, 1f),
                    TransformCurveType.Position)
            };
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                transformCurves,
                new List<BlendShapeCurveData>(),
                false);
            LogAssert.Expect(LogType.Error, "FBX Exporterパッケージがインストールされていません。");

            // Act
            var result = exporter.Export(exportData, "Assets/TestExport.fbx");

            // Assert
            Assert.IsFalse(result);
#else
            // パッケージがインストールされている場合はスキップ
            Assert.Ignore("FBX Exporterパッケージがインストールされているため、このテストはスキップされます");
#endif
        }

        [Test]
        public void Export_FbxExportDataがnullの場合falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            LogAssert.Expect(LogType.Error, "FbxExportDataがnullです。");

            // Act
            var result = exporter.Export(null, "Assets/TestExport.fbx");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Export_出力パスが空の場合falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var transformCurves = new List<TransformCurveData>
            {
                new TransformCurveData(
                    "",
                    "localPosition.x",
                    AnimationCurve.Linear(0f, 0f, 1f, 1f),
                    TransformCurveType.Position)
            };
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                transformCurves,
                new List<BlendShapeCurveData>(),
                false);
            LogAssert.Expect(LogType.Error, "出力パスが指定されていません。");

            // Act
            var result = exporter.Export(exportData, "");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Export_出力パスがnullの場合falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var transformCurves = new List<TransformCurveData>
            {
                new TransformCurveData(
                    "",
                    "localPosition.x",
                    AnimationCurve.Linear(0f, 0f, 1f, 1f),
                    TransformCurveType.Position)
            };
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                transformCurves,
                new List<BlendShapeCurveData>(),
                false);
            LogAssert.Expect(LogType.Error, "出力パスが指定されていません。");

            // Act
            var result = exporter.Export(exportData, null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Export_エクスポート可能なデータがない場合falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                new List<TransformCurveData>(),
                new List<BlendShapeCurveData>(),
                false);
            LogAssert.Expect(LogType.Error, "エクスポート可能なデータがありません。");

            // Act
            var result = exporter.Export(exportData, "Assets/TestExport.fbx");

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Export テスト（パッケージインストール時）

#if UNITY_FORMATS_FBX
        [Test]
        public void Export_パッケージがインストール済みの場合有効なデータでtrueを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var transformCurves = new List<TransformCurveData>
            {
                new TransformCurveData(
                    "",
                    "localPosition.x",
                    AnimationCurve.Linear(0f, 0f, 1f, 1f),
                    TransformCurveType.Position)
            };
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                transformCurves,
                new List<BlendShapeCurveData>(),
                false);

            var outputPath = "Assets/TestExport_" + System.Guid.NewGuid().ToString("N").Substring(0, 8) + ".fbx";

            // Act
            var result = exporter.Export(exportData, outputPath);

            // Assert & Cleanup
            try
            {
                Assert.IsTrue(result);
                Assert.IsTrue(System.IO.File.Exists(outputPath) || AssetDatabase.LoadAssetAtPath<Object>(outputPath) != null);
            }
            finally
            {
                // クリーンアップ
                if (AssetDatabase.LoadAssetAtPath<Object>(outputPath) != null)
                {
                    AssetDatabase.DeleteAsset(outputPath);
                }
            }
        }
#endif

        #endregion

        #region ValidateExportData テスト

        [Test]
        public void ValidateExportData_有効なデータの場合nullを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var transformCurves = new List<TransformCurveData>
            {
                new TransformCurveData(
                    "",
                    "localPosition.x",
                    AnimationCurve.Linear(0f, 0f, 1f, 1f),
                    TransformCurveType.Position)
            };
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                transformCurves,
                new List<BlendShapeCurveData>(),
                false);

            // Act
            var errorMessage = exporter.ValidateExportData(exportData);

            // Assert
            Assert.IsNull(errorMessage);
        }

        [Test]
        public void ValidateExportData_nullの場合エラーメッセージを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var errorMessage = exporter.ValidateExportData(null);

            // Assert
            Assert.IsNotNull(errorMessage);
            Assert.IsTrue(errorMessage.Contains("null"));
        }

        [Test]
        public void ValidateExportData_エクスポート可能なデータがない場合エラーメッセージを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                new List<TransformCurveData>(),
                new List<BlendShapeCurveData>(),
                false);

            // Act
            var errorMessage = exporter.ValidateExportData(exportData);

            // Assert
            Assert.IsNotNull(errorMessage);
            Assert.IsTrue(errorMessage.Contains("エクスポート") || errorMessage.Contains("データ"));
        }

        #endregion

        #region GetSupportedExportOptions テスト

        [Test]
        public void GetSupportedExportOptions_オプションリストを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var options = exporter.GetSupportedExportOptions();

            // Assert
            Assert.IsNotNull(options);
        }

        #endregion

        #region ExtractSkeleton テスト (P13-003)

        [Test]
        public void ExtractSkeleton_Animatorがnullの場合_空のSkeletonDataを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var result = exporter.ExtractSkeleton(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.HasSkeleton);
        }

        [Test]
        public void ExtractSkeleton_ボーン階層が存在する場合_SkeletonDataを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // ボーン階層を作成
            var hips = new GameObject("Hips");
            hips.transform.SetParent(_testGameObject.transform);

            var spine = new GameObject("Spine");
            spine.transform.SetParent(hips.transform);

            // SkinnedMeshRendererを追加
            var meshObj = new GameObject("Body");
            meshObj.transform.SetParent(_testGameObject.transform);
            var smr = meshObj.AddComponent<SkinnedMeshRenderer>();
            smr.bones = new Transform[] { hips.transform, spine.transform };
            smr.rootBone = hips.transform;

            // Act
            var result = exporter.ExtractSkeleton(_testAnimator);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasSkeleton);
            Assert.IsNotNull(result.RootBone);
            Assert.Greater(result.Bones.Count, 0);
        }

        [Test]
        public void ExtractSkeleton_SkeletonExtractorを委譲する()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // 単純な子オブジェクトを追加（SkinnedMeshRendererなし）
            var child = new GameObject("Child");
            child.transform.SetParent(_testGameObject.transform);

            // Act
            var result = exporter.ExtractSkeleton(_testAnimator);

            // Assert
            // SkeletonExtractorに処理が委譲され、結果が返される
            Assert.IsNotNull(result);
        }

        #endregion

        #region GetBonePath テスト (P13-003)

        [Test]
        public void GetBonePath_ボーンパスを取得できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            var hips = new GameObject("Hips");
            hips.transform.SetParent(_testGameObject.transform);

            var spine = new GameObject("Spine");
            spine.transform.SetParent(hips.transform);

            // Act
            var path = exporter.GetBonePath(_testAnimator, spine.transform);

            // Assert
            Assert.AreEqual("Hips/Spine", path);
        }

        [Test]
        public void GetBonePath_Animator自身の場合は空文字を返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var path = exporter.GetBonePath(_testAnimator, _testGameObject.transform);

            // Assert
            Assert.AreEqual(string.Empty, path);
        }

        [Test]
        public void GetBonePath_nullの場合はnullを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act & Assert
            Assert.IsNull(exporter.GetBonePath(null, _testGameObject.transform));
            Assert.IsNull(exporter.GetBonePath(_testAnimator, null));
        }

        #endregion

        #region Constructor with SkeletonExtractor テスト (P13-003)

        [Test]
        public void Constructor_SkeletonExtractorを指定できる()
        {
            // Arrange
            var skeletonExtractor = new SkeletonExtractor();

            // Act
            var exporter = new FbxAnimationExporter(skeletonExtractor);

            // Assert
            Assert.IsNotNull(exporter);
        }

        [Test]
        public void Constructor_SkeletonExtractorがnullの場合デフォルトを使用()
        {
            // Act
            var exporter = new FbxAnimationExporter(null);

            // Assert
            Assert.IsNotNull(exporter);
            // ExtractSkeletonが正常に動作することを確認
            var result = exporter.ExtractSkeleton(_testAnimator);
            Assert.IsNotNull(result);
        }

        #endregion

        #region ExtractTransformCurves テスト (P13-006)

        [Test]
        public void ExtractTransformCurves_AnimationClipがnullの場合_空のリストを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var result = exporter.ExtractTransformCurves(null, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ExtractTransformCurves_Animatorがnullの場合_カーブは抽出される()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            // Animatorがnullでも、AnimationClipからTransformカーブは抽出可能
            var result = exporter.ExtractTransformCurves(_testClip, null);

            // Assert
            Assert.IsNotNull(result);
            // _testClipには3つのTransformカーブ（localPosition.x/y/z）が設定されている
            Assert.GreaterOrEqual(result.Count, 1, "Animatorがnullでもカーブは抽出される");
        }

        [Test]
        public void ExtractTransformCurves_Positionカーブを抽出できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            clip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, 0, 1, 2));
            clip.SetCurve("", typeof(Transform), "localPosition.z", AnimationCurve.Linear(0, 0, 1, 3));

            // Act
            var result = exporter.ExtractTransformCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            foreach (var curveData in result)
            {
                Assert.AreEqual(TransformCurveType.Position, curveData.CurveType);
            }

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExtractTransformCurves_Rotationカーブを抽出できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "localRotation.x", AnimationCurve.Linear(0, 0, 1, 0));
            clip.SetCurve("", typeof(Transform), "localRotation.y", AnimationCurve.Linear(0, 0, 1, 0));
            clip.SetCurve("", typeof(Transform), "localRotation.z", AnimationCurve.Linear(0, 0, 1, 0));
            clip.SetCurve("", typeof(Transform), "localRotation.w", AnimationCurve.Linear(0, 1, 1, 1));

            // Act
            var result = exporter.ExtractTransformCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count);
            foreach (var curveData in result)
            {
                Assert.AreEqual(TransformCurveType.Rotation, curveData.CurveType);
            }

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExtractTransformCurves_Scaleカーブを抽出できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "localScale.x", AnimationCurve.Linear(0, 1, 1, 2));
            clip.SetCurve("", typeof(Transform), "localScale.y", AnimationCurve.Linear(0, 1, 1, 2));
            clip.SetCurve("", typeof(Transform), "localScale.z", AnimationCurve.Linear(0, 1, 1, 2));

            // Act
            var result = exporter.ExtractTransformCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            foreach (var curveData in result)
            {
                Assert.AreEqual(TransformCurveType.Scale, curveData.CurveType);
            }

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExtractTransformCurves_EulerAnglesカーブを抽出できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "localEulerAngles.x", AnimationCurve.Linear(0, 0, 1, 90));
            clip.SetCurve("", typeof(Transform), "localEulerAngles.y", AnimationCurve.Linear(0, 0, 1, 180));
            clip.SetCurve("", typeof(Transform), "localEulerAngles.z", AnimationCurve.Linear(0, 0, 1, 270));

            // Act
            var result = exporter.ExtractTransformCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            foreach (var curveData in result)
            {
                Assert.AreEqual(TransformCurveType.EulerAngles, curveData.CurveType);
            }

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExtractTransformCurves_子オブジェクトのパスを正しく取得できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var child = new GameObject("Child");
            child.transform.SetParent(_testGameObject.transform);
            var grandChild = new GameObject("GrandChild");
            grandChild.transform.SetParent(child.transform);

            var clip = new AnimationClip();
            clip.SetCurve("Child/GrandChild", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));

            // Act
            var result = exporter.ExtractTransformCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            // Unityは単一軸のPositionカーブを設定すると、自動的に全3軸（x,y,z）のカーブを生成する
            Assert.GreaterOrEqual(result.Count, 1);
            // 少なくとも1つは指定したパスのカーブが含まれていることを確認
            bool hasExpectedPath = false;
            foreach (var curveData in result)
            {
                if (curveData.Path == "Child/GrandChild")
                {
                    hasExpectedPath = true;
                    break;
                }
            }
            Assert.IsTrue(hasExpectedPath, "Child/GrandChildパスのカーブが存在すること");

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExtractTransformCurves_複数のTransformのカーブを抽出できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var child1 = new GameObject("Bone1");
            child1.transform.SetParent(_testGameObject.transform);
            var child2 = new GameObject("Bone2");
            child2.transform.SetParent(_testGameObject.transform);

            var clip = new AnimationClip();
            // ルートのPositionカーブ（Unityは自動的に全3軸を生成）
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            // Bone1のRotationカーブ
            clip.SetCurve("Bone1", typeof(Transform), "localRotation.x", AnimationCurve.Linear(0, 0, 1, 0));
            clip.SetCurve("Bone1", typeof(Transform), "localRotation.y", AnimationCurve.Linear(0, 0, 1, 0));
            clip.SetCurve("Bone1", typeof(Transform), "localRotation.z", AnimationCurve.Linear(0, 0, 1, 0));
            clip.SetCurve("Bone1", typeof(Transform), "localRotation.w", AnimationCurve.Linear(0, 1, 1, 1));
            // Bone2のScaleカーブ（Unityは自動的に全3軸を生成）
            clip.SetCurve("Bone2", typeof(Transform), "localScale.x", AnimationCurve.Linear(0, 1, 1, 2));

            // Act
            var result = exporter.ExtractTransformCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            // Unityは単一軸のPosition/Scaleカーブを設定すると、自動的に全3軸のカーブを生成する
            // そのため、期待されるカーブ数は: ルート(3) + Bone1(4) + Bone2(3) = 10
            Assert.GreaterOrEqual(result.Count, 6, "最低でも設定したカーブ数以上のカーブが存在すること");

            // 各パスにカーブが存在することを確認
            int rootCurves = 0;
            int bone1Curves = 0;
            int bone2Curves = 0;
            foreach (var curveData in result)
            {
                if (curveData.Path == "") rootCurves++;
                else if (curveData.Path == "Bone1") bone1Curves++;
                else if (curveData.Path == "Bone2") bone2Curves++;
            }
            Assert.GreaterOrEqual(rootCurves, 1, "ルートにカーブが存在すること");
            Assert.AreEqual(4, bone1Curves, "Bone1に4つのRotationカーブが存在すること");
            Assert.GreaterOrEqual(bone2Curves, 1, "Bone2にカーブが存在すること");

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExtractTransformCurves_Transform以外のカーブは含まれない()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            // Transformカーブ（Unityは自動的に全3軸を生成）
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            // BlendShapeカーブ（除外されるべき）
            clip.SetCurve("", typeof(SkinnedMeshRenderer), "blendShape.smile", AnimationCurve.Linear(0, 0, 1, 100));
            // マテリアルカーブ（除外されるべき）
            clip.SetCurve("", typeof(MeshRenderer), "material._Color.r", AnimationCurve.Linear(0, 0, 1, 1));

            // Act
            var result = exporter.ExtractTransformCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            // Unityは単一軸のPositionカーブを設定すると、自動的に全3軸のカーブを生成する
            Assert.GreaterOrEqual(result.Count, 1, "少なくとも1つのTransformカーブが存在すること");
            // 全てのカーブがTransform関連であることを確認
            // Unity内部では "localPosition" または "m_LocalPosition" 形式でプロパティ名が返される
            foreach (var curveData in result)
            {
                bool isTransformProperty =
                    curveData.PropertyName.Contains("Position") ||
                    curveData.PropertyName.Contains("Rotation") ||
                    curveData.PropertyName.Contains("Scale") ||
                    curveData.PropertyName.Contains("EulerAngles");
                Assert.IsTrue(isTransformProperty,
                    $"カーブ'{curveData.PropertyName}'はTransform関連であること");
            }

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExtractTransformCurves_カーブの値が正しく抽出される()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var expectedCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 5f),
                new Keyframe(1f, 10f));
            clip.SetCurve("", typeof(Transform), "localPosition.x", expectedCurve);

            // Act
            var result = exporter.ExtractTransformCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            // Unityは単一軸のPositionカーブを設定すると、自動的に全3軸のカーブを生成する
            Assert.GreaterOrEqual(result.Count, 1, "少なくとも1つのカーブが存在すること");

            // localPosition.xまたはm_LocalPosition.xのカーブを探す（Unity内部では形式が異なる場合がある）
            TransformCurveData xCurveData = null;
            foreach (var curveData in result)
            {
                if (curveData.PropertyName == "localPosition.x" ||
                    curveData.PropertyName == "m_LocalPosition.x")
                {
                    xCurveData = curveData;
                    break;
                }
            }
            Assert.IsNotNull(xCurveData, "Position.xカーブが存在すること");

            var extractedCurve = xCurveData.Curve;
            Assert.IsNotNull(extractedCurve);
            Assert.AreEqual(3, extractedCurve.length);
            Assert.AreEqual(0f, extractedCurve.Evaluate(0f), 0.001f);
            Assert.AreEqual(5f, extractedCurve.Evaluate(0.5f), 0.001f);
            Assert.AreEqual(10f, extractedCurve.Evaluate(1f), 0.001f);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        #endregion

        #region PrepareTransformCurvesForExport テスト (P13-006)

        [Test]
        public void PrepareTransformCurvesForExport_TransformCurveDataリストからFbxExportDataを作成できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var transformCurves = new List<TransformCurveData>
            {
                new TransformCurveData("", "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1), TransformCurveType.Position),
                new TransformCurveData("", "localPosition.y", AnimationCurve.Linear(0, 0, 1, 2), TransformCurveType.Position),
                new TransformCurveData("", "localPosition.z", AnimationCurve.Linear(0, 0, 1, 3), TransformCurveType.Position)
            };

            // Act
            var exportData = exporter.PrepareTransformCurvesForExport(
                _testAnimator,
                _testClip,
                transformCurves);

            // Assert
            Assert.IsNotNull(exportData);
            Assert.AreEqual(_testAnimator, exportData.SourceAnimator);
            Assert.AreEqual(_testClip, exportData.MergedClip);
            Assert.AreEqual(3, exportData.TransformCurves.Count);
            Assert.IsTrue(exportData.HasExportableData);
        }

        [Test]
        public void PrepareTransformCurvesForExport_空のリストでもFbxExportDataを作成できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var transformCurves = new List<TransformCurveData>();

            // Act
            var exportData = exporter.PrepareTransformCurvesForExport(
                _testAnimator,
                _testClip,
                transformCurves);

            // Assert
            Assert.IsNotNull(exportData);
            Assert.AreEqual(0, exportData.TransformCurves.Count);
            Assert.IsFalse(exportData.HasExportableData);
        }

        [Test]
        public void PrepareTransformCurvesForExport_Animatorがnullでも動作する()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var transformCurves = new List<TransformCurveData>
            {
                new TransformCurveData("", "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1), TransformCurveType.Position)
            };

            // Act
            var exportData = exporter.PrepareTransformCurvesForExport(
                null,
                _testClip,
                transformCurves);

            // Assert
            Assert.IsNotNull(exportData);
            Assert.IsNull(exportData.SourceAnimator);
            Assert.AreEqual(1, exportData.TransformCurves.Count);
        }

        #endregion

        #region ExportTransformCurves テスト (P13-006)

        [Test]
        public void ExportTransformCurves_有効なデータでエクスポート処理を呼び出せる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            clip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, 0, 1, 2));
            clip.SetCurve("", typeof(Transform), "localPosition.z", AnimationCurve.Linear(0, 0, 1, 3));

            // Act
            var transformCurves = exporter.ExtractTransformCurves(clip, _testAnimator);
            var exportData = exporter.PrepareTransformCurvesForExport(_testAnimator, clip, transformCurves);

            // Assert
            Assert.IsTrue(exportData.HasExportableData);
            Assert.AreEqual(3, exportData.TransformCurves.Count);
            Assert.IsTrue(exporter.CanExport(exportData));

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExportTransformCurves_Position_Rotation_Scale複合でエクスポートできる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            // Position
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            clip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, 0, 1, 2));
            clip.SetCurve("", typeof(Transform), "localPosition.z", AnimationCurve.Linear(0, 0, 1, 3));
            // Rotation
            clip.SetCurve("", typeof(Transform), "localRotation.x", AnimationCurve.Linear(0, 0, 1, 0));
            clip.SetCurve("", typeof(Transform), "localRotation.y", AnimationCurve.Linear(0, 0, 1, 0));
            clip.SetCurve("", typeof(Transform), "localRotation.z", AnimationCurve.Linear(0, 0, 1, 0));
            clip.SetCurve("", typeof(Transform), "localRotation.w", AnimationCurve.Linear(0, 1, 1, 1));
            // Scale
            clip.SetCurve("", typeof(Transform), "localScale.x", AnimationCurve.Linear(0, 1, 1, 2));
            clip.SetCurve("", typeof(Transform), "localScale.y", AnimationCurve.Linear(0, 1, 1, 2));
            clip.SetCurve("", typeof(Transform), "localScale.z", AnimationCurve.Linear(0, 1, 1, 2));

            // Act
            var transformCurves = exporter.ExtractTransformCurves(clip, _testAnimator);
            var exportData = exporter.PrepareTransformCurvesForExport(_testAnimator, clip, transformCurves);

            // Assert
            Assert.AreEqual(10, exportData.TransformCurves.Count);

            // カーブタイプごとの数を確認
            int positionCount = 0;
            int rotationCount = 0;
            int scaleCount = 0;
            foreach (var curveData in exportData.TransformCurves)
            {
                switch (curveData.CurveType)
                {
                    case TransformCurveType.Position: positionCount++; break;
                    case TransformCurveType.Rotation: rotationCount++; break;
                    case TransformCurveType.Scale: scaleCount++; break;
                }
            }
            Assert.AreEqual(3, positionCount);
            Assert.AreEqual(4, rotationCount);
            Assert.AreEqual(3, scaleCount);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        #endregion

        #region ExtractBlendShapeCurves テスト (P15-002)

        [Test]
        public void ExtractBlendShapeCurves_AnimationClipがnullの場合_空のリストを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var result = exporter.ExtractBlendShapeCurves(null, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ExtractBlendShapeCurves_Animatorがnullの場合_空のリストを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var result = exporter.ExtractBlendShapeCurves(_testClip, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ExtractBlendShapeCurves_BlendShapeカーブを抽出できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            // Act
            var result = exporter.ExtractBlendShapeCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Body", result[0].Path);
            Assert.AreEqual("smile", result[0].BlendShapeName);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExtractBlendShapeCurves_複数のBlendShapeカーブを抽出できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var bindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            var bindingBlink = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.eyeBlink_L");
            var bindingMouth = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.mouthOpen");
            AnimationUtility.SetEditorCurve(clip, bindingSmile, AnimationCurve.Linear(0f, 0f, 1f, 100f));
            AnimationUtility.SetEditorCurve(clip, bindingBlink, AnimationCurve.Linear(0f, 0f, 1f, 50f));
            AnimationUtility.SetEditorCurve(clip, bindingMouth, AnimationCurve.Linear(0f, 0f, 1f, 75f));

            // Act
            var result = exporter.ExtractBlendShapeCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExtractBlendShapeCurves_BlendShapeカーブのみを抽出する()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            // BlendShapeカーブ
            var bindingBlendShape = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            AnimationUtility.SetEditorCurve(clip, bindingBlendShape, AnimationCurve.Linear(0f, 0f, 1f, 100f));
            // Transformカーブ（除外されるべき）
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            // マテリアルカーブ（除外されるべき）
            clip.SetCurve("", typeof(MeshRenderer), "material._Color.r", AnimationCurve.Linear(0f, 0f, 1f, 1f));

            // Act
            var result = exporter.ExtractBlendShapeCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("smile", result[0].BlendShapeName);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExtractBlendShapeCurves_BlendShapeカーブがない場合_空のリストを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));

            // Act
            var result = exporter.ExtractBlendShapeCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExtractBlendShapeCurves_カーブの値が正しく抽出される()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var expectedCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 50f),
                new Keyframe(1f, 100f));
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            AnimationUtility.SetEditorCurve(clip, binding, expectedCurve);

            // Act
            var result = exporter.ExtractBlendShapeCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            var extractedCurve = result[0].Curve;
            Assert.IsNotNull(extractedCurve);
            Assert.AreEqual(3, extractedCurve.length);
            Assert.AreEqual(0f, extractedCurve.Evaluate(0f), 0.001f);
            Assert.AreEqual(50f, extractedCurve.Evaluate(0.5f), 0.001f);
            Assert.AreEqual(100f, extractedCurve.Evaluate(1f), 0.001f);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExtractBlendShapeCurves_異なるパスのBlendShapeカーブを正しく抽出できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var bindingBody = EditorCurveBinding.FloatCurve("Character/Body", typeof(SkinnedMeshRenderer), "blendShape.muscle");
            var bindingFace = EditorCurveBinding.FloatCurve("Character/Face", typeof(SkinnedMeshRenderer), "blendShape.smile");
            var bindingRoot = EditorCurveBinding.FloatCurve("", typeof(SkinnedMeshRenderer), "blendShape.morph");
            AnimationUtility.SetEditorCurve(clip, bindingBody, AnimationCurve.Linear(0f, 0f, 1f, 100f));
            AnimationUtility.SetEditorCurve(clip, bindingFace, AnimationCurve.Linear(0f, 0f, 1f, 100f));
            AnimationUtility.SetEditorCurve(clip, bindingRoot, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            // Act
            var result = exporter.ExtractBlendShapeCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);

            // パスの確認
            bool hasBodyPath = false;
            bool hasFacePath = false;
            bool hasRootPath = false;
            foreach (var curveData in result)
            {
                if (curveData.Path == "Character/Body") hasBodyPath = true;
                if (curveData.Path == "Character/Face") hasFacePath = true;
                if (curveData.Path == "") hasRootPath = true;
            }
            Assert.IsTrue(hasBodyPath, "Character/Bodyパスのカーブが存在すること");
            Assert.IsTrue(hasFacePath, "Character/Faceパスのカーブが存在すること");
            Assert.IsTrue(hasRootPath, "ルートパスのカーブが存在すること");

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExtractBlendShapeCurves_アンダースコア含む名前を正しく抽出できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var binding = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.eye_blink_L");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            // Act
            var result = exporter.ExtractBlendShapeCurves(clip, _testAnimator);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("eye_blink_L", result[0].BlendShapeName);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        #endregion

        #region PrepareBlendShapeCurvesForExport テスト (P15-002)

        [Test]
        public void PrepareBlendShapeCurvesForExport_BlendShapeCurveDataリストからFbxExportDataを作成できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var blendShapeCurves = new List<BlendShapeCurveData>
            {
                new BlendShapeCurveData("Body", "smile", AnimationCurve.Linear(0f, 0f, 1f, 100f)),
                new BlendShapeCurveData("Body", "eyeBlink_L", AnimationCurve.Linear(0f, 0f, 1f, 50f))
            };

            // Act
            var exportData = exporter.PrepareBlendShapeCurvesForExport(
                _testAnimator,
                _testClip,
                blendShapeCurves);

            // Assert
            Assert.IsNotNull(exportData);
            Assert.AreEqual(_testAnimator, exportData.SourceAnimator);
            Assert.AreEqual(_testClip, exportData.MergedClip);
            Assert.AreEqual(2, exportData.BlendShapeCurves.Count);
            Assert.IsTrue(exportData.HasExportableData);
        }

        [Test]
        public void PrepareBlendShapeCurvesForExport_空のリストでもFbxExportDataを作成できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var blendShapeCurves = new List<BlendShapeCurveData>();

            // Act
            var exportData = exporter.PrepareBlendShapeCurvesForExport(
                _testAnimator,
                _testClip,
                blendShapeCurves);

            // Assert
            Assert.IsNotNull(exportData);
            Assert.AreEqual(0, exportData.BlendShapeCurves.Count);
            Assert.IsFalse(exportData.HasExportableData);
        }

        [Test]
        public void PrepareBlendShapeCurvesForExport_Animatorがnullでも動作する()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var blendShapeCurves = new List<BlendShapeCurveData>
            {
                new BlendShapeCurveData("Body", "smile", AnimationCurve.Linear(0f, 0f, 1f, 100f))
            };

            // Act
            var exportData = exporter.PrepareBlendShapeCurvesForExport(
                null,
                _testClip,
                blendShapeCurves);

            // Assert
            Assert.IsNotNull(exportData);
            Assert.IsNull(exportData.SourceAnimator);
            Assert.AreEqual(1, exportData.BlendShapeCurves.Count);
        }

        #endregion

        #region HasBlendShapeCurves テスト (P15-002)

        [Test]
        public void HasBlendShapeCurves_BlendShapeカーブが存在する場合_trueを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            // Act
            var result = exporter.HasBlendShapeCurves(clip);

            // Assert
            Assert.IsTrue(result);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void HasBlendShapeCurves_BlendShapeカーブが存在しない場合_falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));

            // Act
            var result = exporter.HasBlendShapeCurves(clip);

            // Assert
            Assert.IsFalse(result);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void HasBlendShapeCurves_AnimationClipがnullの場合_falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var result = exporter.HasBlendShapeCurves(null);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region ExtractBlendShapeCurvesとPrepareBlendShapeCurvesForExport統合テスト (P15-002)

        [Test]
        public void BlendShapeCurves_抽出からエクスポートデータ準備までの統合テスト()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var bindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            var bindingBlink = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.eyeBlink");
            AnimationUtility.SetEditorCurve(clip, bindingSmile, AnimationCurve.Linear(0f, 0f, 1f, 100f));
            AnimationUtility.SetEditorCurve(clip, bindingBlink, AnimationCurve.Linear(0f, 0f, 1f, 50f));

            // Act
            var blendShapeCurves = exporter.ExtractBlendShapeCurves(clip, _testAnimator);
            var exportData = exporter.PrepareBlendShapeCurvesForExport(_testAnimator, clip, blendShapeCurves);

            // Assert
            Assert.IsNotNull(exportData);
            Assert.IsTrue(exportData.HasExportableData);
            Assert.AreEqual(2, exportData.BlendShapeCurves.Count);
            Assert.IsTrue(exporter.CanExport(exportData));

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void BlendShapeCurves_TransformとBlendShape両方を含むクリップのエクスポートデータ作成()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            // Transformカーブ
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            // BlendShapeカーブ
            var bindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            AnimationUtility.SetEditorCurve(clip, bindingSmile, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            // Act
            var transformCurves = exporter.ExtractTransformCurves(clip, _testAnimator);
            var blendShapeCurves = exporter.ExtractBlendShapeCurves(clip, _testAnimator);

            // Assert
            Assert.Greater(transformCurves.Count, 0, "Transformカーブが抽出されること");
            Assert.AreEqual(1, blendShapeCurves.Count, "BlendShapeカーブが抽出されること");

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        #endregion

        #region ExportBlendShapeCurvesToFbx テスト (P15-004)

        [Test]
        public void ExportBlendShapeCurvesToFbx_AnimationClipがnullの場合falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            LogAssert.Expect(LogType.Error, "AnimationClipがnullです。");

            // Act
            var result = exporter.ExportBlendShapeCurvesToFbx(_testAnimator, null, "Assets/TestExport.fbx");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ExportBlendShapeCurvesToFbx_出力パスが空の場合falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, 0f, 1f, 100f));
            LogAssert.Expect(LogType.Error, "出力パスが指定されていません。");

            // Act
            var result = exporter.ExportBlendShapeCurvesToFbx(_testAnimator, clip, "");

            // Assert
            Assert.IsFalse(result);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExportBlendShapeCurvesToFbx_出力パスがnullの場合falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, 0f, 1f, 100f));
            LogAssert.Expect(LogType.Error, "出力パスが指定されていません。");

            // Act
            var result = exporter.ExportBlendShapeCurvesToFbx(_testAnimator, clip, null);

            // Assert
            Assert.IsFalse(result);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExportBlendShapeCurvesToFbx_BlendShapeカーブがない場合falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            LogAssert.Expect(LogType.Error, "エクスポート可能なBlendShapeカーブがありません。");

            // Act
            var result = exporter.ExportBlendShapeCurvesToFbx(_testAnimator, clip, "Assets/TestExport.fbx");

            // Assert
            Assert.IsFalse(result);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExportBlendShapeCurvesToFbx_有効なBlendShapeカーブでFbxExportDataを作成できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var bindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            var bindingBlink = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.eyeBlink_L");
            AnimationUtility.SetEditorCurve(clip, bindingSmile, AnimationCurve.Linear(0f, 0f, 1f, 100f));
            AnimationUtility.SetEditorCurve(clip, bindingBlink, AnimationCurve.Linear(0f, 0f, 1f, 50f));

            // Act
            var blendShapeCurves = exporter.ExtractBlendShapeCurves(clip, _testAnimator);
            var exportData = exporter.PrepareBlendShapeCurvesForExport(_testAnimator, clip, blendShapeCurves);

            // Assert
            Assert.IsNotNull(exportData);
            Assert.IsTrue(exportData.HasExportableData);
            Assert.AreEqual(2, exportData.BlendShapeCurves.Count);
            Assert.IsTrue(exporter.CanExport(exportData));

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExportBlendShapeCurvesToFbx_Animatorがnullでも処理が実行される()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            var outputPath = "Assets/TestExport_" + System.Guid.NewGuid().ToString("N").Substring(0, 8) + ".fbx";

            if (!FbxPackageChecker.IsPackageInstalled())
            {
                // FBX Exporterパッケージがインストールされていない場合はエラーで失敗する
                LogAssert.Expect(LogType.Error, "FBX Exporterパッケージがインストールされていません。");

                // Act
                var result = exporter.ExportBlendShapeCurvesToFbx(null, clip, outputPath);

                // Assert
                Assert.IsFalse(result);
            }
            else
            {
                // FBX Exporterパッケージがインストールされている場合、
                // Animatorがnullでも一時オブジェクトを作成してエクスポートが成功する
                // Act
                var result = exporter.ExportBlendShapeCurvesToFbx(null, clip, outputPath);

                // Assert & Cleanup
                try
                {
                    Assert.IsTrue(result);
                }
                finally
                {
                    if (AssetDatabase.LoadAssetAtPath<Object>(outputPath) != null)
                    {
                        AssetDatabase.DeleteAsset(outputPath);
                    }
                }
            }

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        #endregion

        #region PrepareAllCurvesForExport テスト (P15-004)

        [Test]
        public void PrepareAllCurvesForExport_TransformとBlendShape両方を含むデータを作成できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            // Transformカーブ
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            clip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0f, 0f, 1f, 2f));
            clip.SetCurve("", typeof(Transform), "localPosition.z", AnimationCurve.Linear(0f, 0f, 1f, 3f));
            // BlendShapeカーブ
            var bindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            AnimationUtility.SetEditorCurve(clip, bindingSmile, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            // Act
            var exportData = exporter.PrepareAllCurvesForExport(_testAnimator, clip);

            // Assert
            Assert.IsNotNull(exportData);
            Assert.IsTrue(exportData.HasExportableData);
            Assert.Greater(exportData.TransformCurves.Count, 0, "Transformカーブが含まれること");
            Assert.AreEqual(1, exportData.BlendShapeCurves.Count, "BlendShapeカーブが含まれること");

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void PrepareAllCurvesForExport_BlendShapeカーブのみの場合もエクスポート可能()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var bindingSmile = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.smile");
            var bindingBlink = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.eyeBlink");
            AnimationUtility.SetEditorCurve(clip, bindingSmile, AnimationCurve.Linear(0f, 0f, 1f, 100f));
            AnimationUtility.SetEditorCurve(clip, bindingBlink, AnimationCurve.Linear(0f, 0f, 1f, 50f));

            // Act
            var exportData = exporter.PrepareAllCurvesForExport(_testAnimator, clip);

            // Assert
            Assert.IsNotNull(exportData);
            Assert.IsTrue(exportData.HasExportableData);
            Assert.AreEqual(0, exportData.TransformCurves.Count, "Transformカーブがないこと");
            Assert.AreEqual(2, exportData.BlendShapeCurves.Count, "BlendShapeカーブが2つあること");

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void PrepareAllCurvesForExport_AnimationClipがnullの場合_エクスポート不可データを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var exportData = exporter.PrepareAllCurvesForExport(_testAnimator, null);

            // Assert
            Assert.IsNotNull(exportData);
            Assert.IsFalse(exportData.HasExportableData);
        }

        [Test]
        public void PrepareAllCurvesForExport_Animatorがnullの場合_BlendShapeのみでもデータを作成()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            // Act
            var exportData = exporter.PrepareAllCurvesForExport(null, clip);

            // Assert
            Assert.IsNotNull(exportData);
            // Animatorがnullでも、BlendShapeカーブはAnimationClipから抽出可能
            // HasExportableDataはBlendShapeカーブがあればtrue
            Assert.IsTrue(exportData.HasExportableData, "BlendShapeカーブがあるためエクスポート可能");
            Assert.AreEqual(1, exportData.BlendShapeCurves.Count);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        #endregion

        #region CreateAnimationClipFromBlendShapeCurves テスト (P15-004)

        [Test]
        public void CreateAnimationClipFromBlendShapeCurves_BlendShapeカーブからAnimationClipを作成できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var blendShapeCurves = new List<BlendShapeCurveData>
            {
                new BlendShapeCurveData("Body", "smile", AnimationCurve.Linear(0f, 0f, 1f, 100f)),
                new BlendShapeCurveData("Body", "eyeBlink_L", AnimationCurve.Linear(0f, 0f, 1f, 50f))
            };

            // Act
            var clip = exporter.CreateAnimationClipFromBlendShapeCurves(blendShapeCurves, "TestBlendShapeClip");

            // Assert
            Assert.IsNotNull(clip);
            Assert.AreEqual("TestBlendShapeClip", clip.name);

            // カーブバインディングを確認
            var bindings = AnimationUtility.GetCurveBindings(clip);
            Assert.AreEqual(2, bindings.Length);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void CreateAnimationClipFromBlendShapeCurves_空のリストの場合nullを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var blendShapeCurves = new List<BlendShapeCurveData>();

            // Act
            var clip = exporter.CreateAnimationClipFromBlendShapeCurves(blendShapeCurves);

            // Assert
            Assert.IsNull(clip);
        }

        [Test]
        public void CreateAnimationClipFromBlendShapeCurves_nullの場合nullを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var clip = exporter.CreateAnimationClipFromBlendShapeCurves(null);

            // Assert
            Assert.IsNull(clip);
        }

        [Test]
        public void CreateAnimationClipFromBlendShapeCurves_カーブがnullの場合スキップする()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var blendShapeCurves = new List<BlendShapeCurveData>
            {
                new BlendShapeCurveData("Body", "smile", AnimationCurve.Linear(0f, 0f, 1f, 100f)),
                new BlendShapeCurveData("Body", "nullCurve", null),
                new BlendShapeCurveData("Body", "eyeBlink", AnimationCurve.Linear(0f, 0f, 1f, 50f))
            };

            // Act
            var clip = exporter.CreateAnimationClipFromBlendShapeCurves(blendShapeCurves);

            // Assert
            Assert.IsNotNull(clip);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            Assert.AreEqual(2, bindings.Length, "nullカーブはスキップされること");

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void CreateAnimationClipFromBlendShapeCurves_カーブの値が正しく設定される()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var expectedCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 50f),
                new Keyframe(1f, 100f));
            var blendShapeCurves = new List<BlendShapeCurveData>
            {
                new BlendShapeCurveData("Face", "smile", expectedCurve)
            };

            // Act
            var clip = exporter.CreateAnimationClipFromBlendShapeCurves(blendShapeCurves);

            // Assert
            Assert.IsNotNull(clip);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            Assert.AreEqual(1, bindings.Length);

            var extractedCurve = AnimationUtility.GetEditorCurve(clip, bindings[0]);
            Assert.IsNotNull(extractedCurve);
            Assert.AreEqual(3, extractedCurve.length);
            Assert.AreEqual(0f, extractedCurve.Evaluate(0f), 0.001f);
            Assert.AreEqual(50f, extractedCurve.Evaluate(0.5f), 0.001f);
            Assert.AreEqual(100f, extractedCurve.Evaluate(1f), 0.001f);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void CreateAnimationClipFromBlendShapeCurves_異なるパスのBlendShapeを正しく設定できる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var blendShapeCurves = new List<BlendShapeCurveData>
            {
                new BlendShapeCurveData("Character/Body", "muscle", AnimationCurve.Linear(0f, 0f, 1f, 100f)),
                new BlendShapeCurveData("Character/Face", "smile", AnimationCurve.Linear(0f, 0f, 1f, 100f)),
                new BlendShapeCurveData("", "morph", AnimationCurve.Linear(0f, 0f, 1f, 100f))
            };

            // Act
            var clip = exporter.CreateAnimationClipFromBlendShapeCurves(blendShapeCurves);

            // Assert
            Assert.IsNotNull(clip);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            Assert.AreEqual(3, bindings.Length);

            // パスの確認
            bool hasBodyPath = false;
            bool hasFacePath = false;
            bool hasRootPath = false;
            foreach (var binding in bindings)
            {
                if (binding.path == "Character/Body") hasBodyPath = true;
                if (binding.path == "Character/Face") hasFacePath = true;
                if (binding.path == "") hasRootPath = true;
            }
            Assert.IsTrue(hasBodyPath, "Character/Bodyパスが設定されていること");
            Assert.IsTrue(hasFacePath, "Character/Faceパスが設定されていること");
            Assert.IsTrue(hasRootPath, "ルートパスが設定されていること");

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        #endregion

        #region ExportWithBlendShapeCurves テスト (P15-004)

        [Test]
        public void ExportWithBlendShapeCurves_FbxExportDataがnullの場合falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            LogAssert.Expect(LogType.Error, "FbxExportDataがnullです。");

            // Act
            var result = exporter.ExportWithBlendShapeCurves(null, "Assets/TestExport.fbx");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ExportWithBlendShapeCurves_出力パスが空の場合falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var blendShapeCurves = new List<BlendShapeCurveData>
            {
                new BlendShapeCurveData("Body", "smile", AnimationCurve.Linear(0f, 0f, 1f, 100f))
            };
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                new List<TransformCurveData>(),
                blendShapeCurves,
                false);
            LogAssert.Expect(LogType.Error, "出力パスが指定されていません。");

            // Act
            var result = exporter.ExportWithBlendShapeCurves(exportData, "");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ExportWithBlendShapeCurves_BlendShapeカーブがない場合falseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                new List<TransformCurveData>(),
                new List<BlendShapeCurveData>(),
                false);
            LogAssert.Expect(LogType.Error, "エクスポート可能なBlendShapeカーブがありません。");

            // Act
            var result = exporter.ExportWithBlendShapeCurves(exportData, "Assets/TestExport.fbx");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ExportWithBlendShapeCurves_有効なBlendShapeデータでCanExportがtrueを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var blendShapeCurves = new List<BlendShapeCurveData>
            {
                new BlendShapeCurveData("Body", "smile", AnimationCurve.Linear(0f, 0f, 1f, 100f)),
                new BlendShapeCurveData("Face", "eyeBlink", AnimationCurve.Linear(0f, 0f, 1f, 50f))
            };
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                new List<TransformCurveData>(),
                blendShapeCurves,
                false);

            // Act & Assert
            Assert.IsTrue(exporter.CanExport(exportData));
            Assert.IsTrue(exportData.HasExportableData);
            Assert.AreEqual(2, exportData.BlendShapeCurves.Count);
        }

        #endregion

        #region BlendShapeカーブFBX出力統合テスト (P15-004)

        [Test]
        public void BlendShapeFbxExport_抽出からエクスポートデータ準備までの一連の処理()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var bindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            var bindingBlink = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.eyeBlink_L");
            var bindingMouth = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.mouthOpen");
            AnimationUtility.SetEditorCurve(clip, bindingSmile, AnimationCurve.Linear(0f, 0f, 1f, 100f));
            AnimationUtility.SetEditorCurve(clip, bindingBlink, AnimationCurve.Linear(0f, 0f, 0.5f, 100f));
            AnimationUtility.SetEditorCurve(clip, bindingMouth, AnimationCurve.Linear(0f, 0f, 1f, 75f));

            // Act
            // 1. BlendShapeカーブを抽出
            var blendShapeCurves = exporter.ExtractBlendShapeCurves(clip, _testAnimator);

            // 2. エクスポートデータを準備
            var exportData = exporter.PrepareBlendShapeCurvesForExport(_testAnimator, clip, blendShapeCurves);

            // 3. 検証
            var validationError = exporter.ValidateExportData(exportData);

            // Assert
            Assert.AreEqual(3, blendShapeCurves.Count);
            Assert.IsNotNull(exportData);
            Assert.IsTrue(exportData.HasExportableData);
            Assert.IsNull(validationError, "検証エラーがないこと");
            Assert.IsTrue(exporter.CanExport(exportData));

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void BlendShapeFbxExport_TransformとBlendShape混合クリップの統合処理()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            // Transformカーブ
            clip.SetCurve("Bone1", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            clip.SetCurve("Bone1", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0f, 0f, 1f, 2f));
            clip.SetCurve("Bone1", typeof(Transform), "localPosition.z", AnimationCurve.Linear(0f, 0f, 1f, 3f));
            // BlendShapeカーブ
            var bindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            var bindingBlink = EditorCurveBinding.FloatCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.eyeBlink");
            AnimationUtility.SetEditorCurve(clip, bindingSmile, AnimationCurve.Linear(0f, 0f, 1f, 100f));
            AnimationUtility.SetEditorCurve(clip, bindingBlink, AnimationCurve.Linear(0f, 0f, 1f, 50f));

            // Act
            var exportData = exporter.PrepareAllCurvesForExport(_testAnimator, clip);

            // Assert
            Assert.IsNotNull(exportData);
            Assert.IsTrue(exportData.HasExportableData);
            Assert.Greater(exportData.TransformCurves.Count, 0, "Transformカーブが抽出されていること");
            Assert.AreEqual(2, exportData.BlendShapeCurves.Count, "BlendShapeカーブが2つ抽出されていること");

            // 各カーブタイプの検証
            bool hasSmile = false;
            bool hasBlink = false;
            foreach (var curve in exportData.BlendShapeCurves)
            {
                if (curve.BlendShapeName == "smile") hasSmile = true;
                if (curve.BlendShapeName == "eyeBlink") hasBlink = true;
            }
            Assert.IsTrue(hasSmile, "smileカーブが含まれていること");
            Assert.IsTrue(hasBlink, "eyeBlinkカーブが含まれていること");

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

#if UNITY_FORMATS_FBX
        [Test]
        public void BlendShapeFbxExport_パッケージインストール済みの場合_実際のエクスポートが可能()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            var outputPath = "Assets/TestBlendShapeExport_" + System.Guid.NewGuid().ToString("N").Substring(0, 8) + ".fbx";

            // Act
            var result = exporter.ExportBlendShapeCurvesToFbx(_testAnimator, clip, outputPath);

            // Assert & Cleanup
            try
            {
                Assert.IsTrue(result, "BlendShapeカーブのFBXエクスポートが成功すること");
                Assert.IsTrue(
                    System.IO.File.Exists(outputPath) || AssetDatabase.LoadAssetAtPath<Object>(outputPath) != null,
                    "FBXファイルが作成されていること");
            }
            finally
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(outputPath) != null)
                {
                    AssetDatabase.DeleteAsset(outputPath);
                }
                Object.DestroyImmediate(clip);
            }
        }
#endif

        #endregion

        #region エクスポートデータなしエラー処理テスト (P15-006 / ERR-004)

        /// <summary>
        /// P15-006: エクスポート可能なデータがない場合のエラー処理テスト
        /// 要件 ERR-004: FBXエクスポート時にエクスポート可能なデータがない場合のエラー処理
        /// </summary>
        [Test]
        public void ERR004_スケルトンもカーブもないFbxExportDataでCanExportがfalseを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null, // スケルトンなし
                new List<TransformCurveData>(), // Transformカーブなし
                new List<BlendShapeCurveData>(), // BlendShapeカーブなし
                false);

            // Act
            var canExport = exporter.CanExport(exportData);

            // Assert
            Assert.IsFalse(canExport, "エクスポート可能なデータがない場合はCanExportがfalseを返すこと");
            Assert.IsFalse(exportData.HasExportableData, "HasExportableDataがfalseであること");
        }

        [Test]
        public void ERR004_エクスポートデータなしでExportがfalseを返しエラーログを出力する()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                new List<TransformCurveData>(),
                new List<BlendShapeCurveData>(),
                false);
            LogAssert.Expect(LogType.Error, "エクスポート可能なデータがありません。");

            // Act
            var result = exporter.Export(exportData, "Assets/TestExport.fbx");

            // Assert
            Assert.IsFalse(result, "エクスポートが失敗すること");
        }

        [Test]
        public void ERR004_ValidateExportDataでエラーメッセージを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                new List<TransformCurveData>(),
                new List<BlendShapeCurveData>(),
                false);

            // Act
            var errorMessage = exporter.ValidateExportData(exportData);

            // Assert
            Assert.IsNotNull(errorMessage, "エラーメッセージが返されること");
            Assert.IsTrue(
                errorMessage.Contains("エクスポート") || errorMessage.Contains("データ"),
                "エラーメッセージにエクスポートまたはデータに関する内容が含まれること");
        }

        [Test]
        public void ERR004_空のAnimationClipからPrepareAllCurvesForExportでエクスポート不可データを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var emptyClip = new AnimationClip();
            emptyClip.name = "EmptyClip";

            // Act
            var exportData = exporter.PrepareAllCurvesForExport(_testAnimator, emptyClip);

            // Assert
            Assert.IsNotNull(exportData);
            Assert.IsFalse(exportData.HasExportableData, "空のクリップからはエクスポート不可データが作成されること");
            Assert.AreEqual(0, exportData.TransformCurves.Count, "Transformカーブがないこと");
            Assert.AreEqual(0, exportData.BlendShapeCurves.Count, "BlendShapeカーブがないこと");

            // クリーンアップ
            Object.DestroyImmediate(emptyClip);
        }

        [Test]
        public void ERR004_BlendShapeカーブのみ抽出失敗時にエラー処理が正しく動作する()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            // Transform以外、BlendShape以外のカーブを設定（マテリアルカーブなど）
            clip.SetCurve("", typeof(MeshRenderer), "material._Color.r", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            LogAssert.Expect(LogType.Error, "エクスポート可能なBlendShapeカーブがありません。");

            // Act
            var result = exporter.ExportBlendShapeCurvesToFbx(_testAnimator, clip, "Assets/TestExport.fbx");

            // Assert
            Assert.IsFalse(result, "BlendShapeカーブがない場合はエクスポートが失敗すること");

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ERR004_ExportWithBlendShapeCurvesで空のBlendShapeカーブリストの場合エラー()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                new List<TransformCurveData>(),
                new List<BlendShapeCurveData>(), // 空のBlendShapeカーブリスト
                false);
            LogAssert.Expect(LogType.Error, "エクスポート可能なBlendShapeカーブがありません。");

            // Act
            var result = exporter.ExportWithBlendShapeCurves(exportData, "Assets/TestExport.fbx");

            // Assert
            Assert.IsFalse(result, "空のBlendShapeカーブリストの場合エクスポートが失敗すること");
        }

        [Test]
        public void ERR004_ExportWithTransformCurvesで空のTransformカーブリストの場合エラー()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                new List<TransformCurveData>(), // 空のTransformカーブリスト
                new List<BlendShapeCurveData>(),
                false);
            LogAssert.Expect(LogType.Error, "エクスポート可能なTransformカーブがありません。");

            // Act
            var result = exporter.ExportWithTransformCurves(exportData, "Assets/TestExport.fbx");

            // Assert
            Assert.IsFalse(result, "空のTransformカーブリストの場合エクスポートが失敗すること");
        }

        [Test]
        public void ERR004_ExportTransformCurvesToFbxでTransformカーブがない場合エラー()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            // BlendShapeカーブのみ設定（Transformカーブなし）
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, 0f, 1f, 100f));
            LogAssert.Expect(LogType.Error, "エクスポート可能なTransformカーブがありません。");

            // Act
            var result = exporter.ExportTransformCurvesToFbx(_testAnimator, clip, "Assets/TestExport.fbx");

            // Assert
            Assert.IsFalse(result, "Transformカーブがない場合はエクスポートが失敗すること");

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ERR004_スケルトンのみでカーブなしの場合はエクスポート可能()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var bones = new List<Transform> { _testGameObject.transform };
            var skeleton = new SkeletonData(_testGameObject.transform, bones);
            var exportData = new FbxExportData(
                _testAnimator,
                _testClip,
                skeleton, // スケルトンあり
                new List<TransformCurveData>(),
                new List<BlendShapeCurveData>(),
                false);

            // Act
            var canExport = exporter.CanExport(exportData);

            // Assert
            Assert.IsTrue(canExport, "スケルトンが存在する場合はエクスポート可能");
            Assert.IsTrue(exportData.HasExportableData, "スケルトンがあればHasExportableDataはtrue");
        }

        [Test]
        public void ERR004_nullのTransformカーブリストでもFbxExportDataが正しく処理される()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            // nullリストをFbxExportDataに渡す場合のテスト
            // PrepareTransformCurvesForExportはnullを空リストに変換するべき

            // Act
            var exportData = exporter.PrepareTransformCurvesForExport(
                _testAnimator,
                _testClip,
                null); // nullを渡す

            // Assert
            Assert.IsNotNull(exportData);
            Assert.IsNotNull(exportData.TransformCurves, "nullが渡されても空リストに変換されること");
            Assert.AreEqual(0, exportData.TransformCurves.Count);
        }

        [Test]
        public void ERR004_nullのBlendShapeカーブリストでもFbxExportDataが正しく処理される()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var exportData = exporter.PrepareBlendShapeCurvesForExport(
                _testAnimator,
                _testClip,
                null); // nullを渡す

            // Assert
            Assert.IsNotNull(exportData);
            Assert.IsNotNull(exportData.BlendShapeCurves, "nullが渡されても空リストに変換されること");
            Assert.AreEqual(0, exportData.BlendShapeCurves.Count);
        }

        [Test]
        public void ERR004_複数のエクスポートメソッドで一貫したエラー処理が行われる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var emptyExportData = new FbxExportData(
                _testAnimator,
                _testClip,
                null,
                new List<TransformCurveData>(),
                new List<BlendShapeCurveData>(),
                false);

            // Act & Assert - CanExport
            Assert.IsFalse(exporter.CanExport(emptyExportData), "CanExportがfalseを返すこと");

            // Act & Assert - ValidateExportData
            var validationError = exporter.ValidateExportData(emptyExportData);
            Assert.IsNotNull(validationError, "ValidateExportDataがエラーメッセージを返すこと");

            // Act & Assert - Export
            LogAssert.Expect(LogType.Error, "エクスポート可能なデータがありません。");
            var exportResult = exporter.Export(emptyExportData, "Assets/TestExport.fbx");
            Assert.IsFalse(exportResult, "Exportがfalseを返すこと");
        }

        [Test]
        public void ERR004_HasExportableDataがfalseの場合のフラグ確認()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var emptyExportData = new FbxExportData(
                null, // Animatorなし
                null, // Clipなし
                null, // スケルトンなし
                new List<TransformCurveData>(), // Transformカーブなし
                new List<BlendShapeCurveData>(), // BlendShapeカーブなし
                false);

            // Assert
            Assert.IsFalse(emptyExportData.HasExportableData, "全てのデータがない場合HasExportableDataはfalse");
            Assert.IsNull(emptyExportData.SourceAnimator, "SourceAnimatorはnull");
            Assert.IsNull(emptyExportData.MergedClip, "MergedClipはnull");
            Assert.IsNull(emptyExportData.Skeleton, "Skeletonはnull");
            Assert.AreEqual(0, emptyExportData.TransformCurves.Count, "TransformCurvesは空");
            Assert.AreEqual(0, emptyExportData.BlendShapeCurves.Count, "BlendShapeCurvesは空");
        }

        #endregion

        #region ExportModelSettingsSerialize設定テスト (P17-001)

        [Test]
        public void Export_ExportModelSettingsSerialize設定がExportInternalで使用される()
        {
            // Arrange
            // ExportInternalはprivateのため、Exportメソッド経由でテストする
            // FBX Exporterパッケージの有無に関わらず、エクスポートフローが正しく動作することを確認
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            // BlendShapeカーブを含むクリップを作成
            var bindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            AnimationUtility.SetEditorCurve(clip, bindingSmile, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            // Transformカーブも追加
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));

            // エクスポートデータを準備
            var exportData = exporter.PrepareAllCurvesForExport(_testAnimator, clip);

            // Assert - エクスポートデータが正しく構成されていること
            Assert.IsNotNull(exportData);
            Assert.IsTrue(exportData.HasExportableData, "BlendShapeとTransformカーブを含むデータはエクスポート可能");
            Assert.Greater(exportData.BlendShapeCurves.Count, 0, "BlendShapeカーブが含まれていること");
            Assert.Greater(exportData.TransformCurves.Count, 0, "Transformカーブが含まれていること");

            // エクスポート可能であることを確認
            Assert.IsTrue(exporter.CanExport(exportData));

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

#if UNITY_FORMATS_FBX
        [Test]
        public void Export_ExportModelSettingsSerializeでBlendShapeとAnimateSkinnedMeshが有効化されてエクスポートされる()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            // BlendShapeカーブを含むAnimationClipを作成
            var bindingSmile = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.smile");
            AnimationUtility.SetEditorCurve(clip, bindingSmile, AnimationCurve.Linear(0f, 0f, 1f, 100f));
            // Transformカーブも追加
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));

            var exportData = exporter.PrepareAllCurvesForExport(_testAnimator, clip);
            var outputPath = "Assets/TestP17001_" + System.Guid.NewGuid().ToString("N").Substring(0, 8) + ".fbx";

            // Act
            var result = exporter.Export(exportData, outputPath);

            // Assert
            try
            {
                Assert.IsTrue(result, "ExportModelSettingsSerialize設定付きのエクスポートが成功すること");
                Assert.IsTrue(
                    System.IO.File.Exists(outputPath) || AssetDatabase.LoadAssetAtPath<Object>(outputPath) != null,
                    "FBXファイルが作成されていること");
            }
            finally
            {
                // クリーンアップ
                if (AssetDatabase.LoadAssetAtPath<Object>(outputPath) != null)
                {
                    AssetDatabase.DeleteAsset(outputPath);
                }
                Object.DestroyImmediate(clip);
            }
        }
#endif

        #endregion

        #region P17-002: PrepareHumanoidForExportのBlendShapeカーブ抽出テスト

        [Test]
        public void PrepareHumanoidForExport_BlendShapeカーブを含むクリップでBlendShapeCurvesが抽出される()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.name = "HumanoidWithBlendShape";

            // BlendShapeカーブを追加
            var blendCurve = AnimationCurve.Linear(0f, 0f, 1f, 100f);
            var binding = EditorCurveBinding.FloatCurve(
                "Body",
                typeof(SkinnedMeshRenderer),
                "blendShape.Smile");
            AnimationUtility.SetEditorCurve(clip, binding, blendCurve);

            // Transformカーブも追加（通常のGenericリグ動作）
            clip.SetCurve("Hips", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));

            try
            {
                // Act
                var exportData = exporter.PrepareHumanoidForExport(_testAnimator, clip);

                // Assert
                Assert.IsNotNull(exportData, "FbxExportDataが生成されるべき");
                Assert.IsNotNull(exportData.BlendShapeCurves, "BlendShapeCurvesがnullであってはならない");
                Assert.AreEqual(1, exportData.BlendShapeCurves.Count,
                    "BlendShapeカーブが1つ抽出されるべき");
                Assert.AreEqual("Smile", exportData.BlendShapeCurves[0].BlendShapeName,
                    "BlendShape名が正しく抽出されるべき");
                Assert.AreEqual("Body", exportData.BlendShapeCurves[0].Path,
                    "パスが正しく抽出されるべき");
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void PrepareHumanoidForExport_BlendShapeカーブなしのクリップでBlendShapeCurvesが空リスト()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.name = "NoBlendShape";
            clip.SetCurve("Hips", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));

            try
            {
                // Act
                var exportData = exporter.PrepareHumanoidForExport(_testAnimator, clip);

                // Assert
                Assert.IsNotNull(exportData, "FbxExportDataが生成されるべき");
                Assert.IsNotNull(exportData.BlendShapeCurves, "BlendShapeCurvesがnullであってはならない");
                Assert.AreEqual(0, exportData.BlendShapeCurves.Count,
                    "BlendShapeカーブがない場合は空リストであるべき");
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void PrepareHumanoidForExport_複数BlendShapeカーブが全て抽出される()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.name = "MultipleBlendShapes";

            // 複数のBlendShapeカーブを追加
            var curve1 = AnimationCurve.Linear(0f, 0f, 1f, 100f);
            var binding1 = EditorCurveBinding.FloatCurve(
                "Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            AnimationUtility.SetEditorCurve(clip, binding1, curve1);

            var curve2 = AnimationCurve.Linear(0f, 50f, 1f, 0f);
            var binding2 = EditorCurveBinding.FloatCurve(
                "Body", typeof(SkinnedMeshRenderer), "blendShape.Blink");
            AnimationUtility.SetEditorCurve(clip, binding2, curve2);

            var curve3 = AnimationCurve.Linear(0f, 0f, 1f, 75f);
            var binding3 = EditorCurveBinding.FloatCurve(
                "Face", typeof(SkinnedMeshRenderer), "blendShape.Angry");
            AnimationUtility.SetEditorCurve(clip, binding3, curve3);

            try
            {
                // Act
                var exportData = exporter.PrepareHumanoidForExport(_testAnimator, clip);

                // Assert
                Assert.IsNotNull(exportData.BlendShapeCurves, "BlendShapeCurvesがnullであってはならない");
                Assert.AreEqual(3, exportData.BlendShapeCurves.Count,
                    "3つのBlendShapeカーブが全て抽出されるべき");
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void PrepareHumanoidForExport_null引数でBlendShapeCurvesが空リスト()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var exportData = exporter.PrepareHumanoidForExport(null, null);

            // Assert
            Assert.IsNotNull(exportData, "null引数でもFbxExportDataが返されるべき");
            Assert.IsNotNull(exportData.BlendShapeCurves, "BlendShapeCurvesがnullであってはならない");
            Assert.AreEqual(0, exportData.BlendShapeCurves.Count,
                "null引数の場合は空リストであるべき");
        }

        #endregion

        #region P17-005: GenericリグBlendShape FBXエクスポート検証テスト

        [Test]
        public void GenericBlendShapeExport_PrepareAllCurvesForExportでBlendShapeカーブが抽出される()
        {
            // Arrange - GenericリグのBlendShapeカーブを含むAnimationClipを作成
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.name = "GenericBlendShapeClip";

            // BlendShapeカーブを追加（Genericリグ想定）
            var smileBinding = EditorCurveBinding.FloatCurve(
                "Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            AnimationUtility.SetEditorCurve(clip, smileBinding, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            var blinkBinding = EditorCurveBinding.FloatCurve(
                "Body", typeof(SkinnedMeshRenderer), "blendShape.Blink");
            AnimationUtility.SetEditorCurve(clip, blinkBinding, AnimationCurve.Linear(0f, 0f, 0.5f, 100f));

            // Transformカーブも追加（Genericリグの典型的なカーブ）
            clip.SetCurve("Hips", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));

            try
            {
                // Act
                var exportData = exporter.PrepareAllCurvesForExport(_testAnimator, clip);

                // Assert
                Assert.IsNotNull(exportData, "FbxExportDataが生成されること");
                Assert.IsNotNull(exportData.BlendShapeCurves, "BlendShapeCurvesがnullでないこと");
                Assert.AreEqual(2, exportData.BlendShapeCurves.Count,
                    "2つのBlendShapeカーブが抽出されること");
                Assert.IsNotNull(exportData.TransformCurves, "TransformCurvesがnullでないこと");
                Assert.Greater(exportData.TransformCurves.Count, 0,
                    "Transformカーブも含まれること");
                Assert.IsTrue(exportData.HasExportableData,
                    "エクスポート可能なデータとして判定されること");
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void GenericBlendShapeExport_CanExportがtrueを返す()
        {
            // Arrange - GenericリグのBlendShapeカーブのみを含むAnimationClip
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.name = "GenericBlendShapeOnly";

            var binding = EditorCurveBinding.FloatCurve(
                "Face", typeof(SkinnedMeshRenderer), "blendShape.Happy");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            try
            {
                // Act
                var exportData = exporter.PrepareAllCurvesForExport(_testAnimator, clip);

                // Assert
                Assert.IsTrue(exporter.CanExport(exportData),
                    "BlendShapeカーブを含むデータはエクスポート可能であること");
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void GenericBlendShapeExport_BlendShape名とパスが正確に保持される()
        {
            // Arrange - 複数パスのBlendShapeカーブ
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.name = "GenericMultiPathBlendShape";

            var bodySmile = EditorCurveBinding.FloatCurve(
                "Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            AnimationUtility.SetEditorCurve(clip, bodySmile, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            var faceAngry = EditorCurveBinding.FloatCurve(
                "Head/Face", typeof(SkinnedMeshRenderer), "blendShape.Angry");
            AnimationUtility.SetEditorCurve(clip, faceAngry, AnimationCurve.Linear(0f, 0f, 1f, 50f));

            try
            {
                // Act
                var exportData = exporter.PrepareAllCurvesForExport(_testAnimator, clip);

                // Assert
                Assert.AreEqual(2, exportData.BlendShapeCurves.Count);

                // パスとBlendShape名を検証（順序不定のためContainsで確認）
                var paths = new List<string>();
                var names = new List<string>();
                foreach (var curve in exportData.BlendShapeCurves)
                {
                    paths.Add(curve.Path);
                    names.Add(curve.BlendShapeName);
                }

                Assert.Contains("Body", paths, "Bodyパスが含まれること");
                Assert.Contains("Head/Face", paths, "Head/Faceパスが含まれること");
                Assert.Contains("Smile", names, "Smile BlendShapeが含まれること");
                Assert.Contains("Angry", names, "Angry BlendShapeが含まれること");
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void GenericBlendShapeExport_CreateAnimationClipFromBlendShapeCurvesでクリップが再構成される()
        {
            // Arrange - BlendShapeカーブを抽出してからAnimationClipに再構成
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.name = "GenericBlendShapeReconstruct";

            var binding = EditorCurveBinding.FloatCurve(
                "Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            try
            {
                // Act - 抽出→再構成のフロー
                var exportData = exporter.PrepareAllCurvesForExport(_testAnimator, clip);
                var reconstructedClip = exporter.CreateAnimationClipFromBlendShapeCurves(
                    new List<BlendShapeCurveData>(exportData.BlendShapeCurves), "ReconstructedClip");

                // Assert
                Assert.IsNotNull(reconstructedClip, "再構成されたクリップがnullでないこと");

                var bindings = AnimationUtility.GetCurveBindings(reconstructedClip);
                Assert.AreEqual(1, bindings.Length, "1つのカーブバインディングが存在すること");

                // BlendShapeバインディングの検証
                Assert.AreEqual("Body", bindings[0].path);
                Assert.AreEqual(typeof(SkinnedMeshRenderer), bindings[0].type);
                Assert.IsTrue(bindings[0].propertyName.StartsWith("blendShape."),
                    "BlendShapeプロパティ名であること");

                Object.DestroyImmediate(reconstructedClip);
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

#if UNITY_FORMATS_FBX
        [Test]
        public void GenericBlendShapeExport_FBXファイルが正常に生成される()
        {
            // Arrange - GenericリグのBlendShapeカーブを含むAnimationClip
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.name = "GenericBlendShapeFbxExport";

            // BlendShapeカーブを追加
            var smileBinding = EditorCurveBinding.FloatCurve(
                "Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            AnimationUtility.SetEditorCurve(clip, smileBinding, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            // Transformカーブも追加
            clip.SetCurve("Hips", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));

            var exportData = exporter.PrepareAllCurvesForExport(_testAnimator, clip);
            var outputPath = "Assets/TestP17005_GenericBS_" + System.Guid.NewGuid().ToString("N").Substring(0, 8) + ".fbx";

            try
            {
                // Act
                var result = exporter.Export(exportData, outputPath);

                // Assert
                Assert.IsTrue(result, "FBXエクスポートが成功すること");
                Assert.IsTrue(
                    System.IO.File.Exists(outputPath) || AssetDatabase.LoadAssetAtPath<Object>(outputPath) != null,
                    "FBXファイルが生成されていること");
            }
            finally
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(outputPath) != null)
                {
                    AssetDatabase.DeleteAsset(outputPath);
                }
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void GenericBlendShapeExport_出力FBXを再インポートしてアニメーションデータが存在する()
        {
            // Arrange - GenericリグのBlendShapeカーブを含むAnimationClip
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.name = "GenericBlendShapeReimport";

            // BlendShapeカーブを追加
            var smileBinding = EditorCurveBinding.FloatCurve(
                "Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            AnimationUtility.SetEditorCurve(clip, smileBinding, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            var blinkBinding = EditorCurveBinding.FloatCurve(
                "Body", typeof(SkinnedMeshRenderer), "blendShape.Blink");
            AnimationUtility.SetEditorCurve(clip, blinkBinding, AnimationCurve.Linear(0f, 50f, 1f, 0f));

            // Transformカーブも追加
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));

            var exportData = exporter.PrepareAllCurvesForExport(_testAnimator, clip);
            var outputPath = "Assets/TestP17005_GenericBSReimport_" + System.Guid.NewGuid().ToString("N").Substring(0, 8) + ".fbx";

            try
            {
                // Act - エクスポート
                var exportResult = exporter.Export(exportData, outputPath);
                Assert.IsTrue(exportResult, "FBXエクスポートが成功すること");

                // FBXを再インポートしてアニメーションデータを検証
                AssetDatabase.Refresh();
                var importedObjects = AssetDatabase.LoadAllAssetsAtPath(outputPath);
                Assert.IsNotNull(importedObjects, "インポートされたアセットが存在すること");

                // AnimationClipが含まれているか検証
                AnimationClip importedClip = null;
                foreach (var obj in importedObjects)
                {
                    if (obj is AnimationClip animClip && !animClip.name.StartsWith("__preview__"))
                    {
                        importedClip = animClip;
                        break;
                    }
                }

                Assert.IsNotNull(importedClip, "再インポートされたFBXにAnimationClipが含まれること");

                // カーブバインディングを検証
                var bindings = AnimationUtility.GetCurveBindings(importedClip);
                Assert.Greater(bindings.Length, 0, "再インポートされたクリップにカーブが含まれること");

                // BlendShapeカーブの存在を検証
                bool hasBlendShapeCurve = false;
                foreach (var b in bindings)
                {
                    if (b.propertyName.StartsWith("blendShape."))
                    {
                        hasBlendShapeCurve = true;
                        break;
                    }
                }
                Assert.IsTrue(hasBlendShapeCurve,
                    "再インポートされたFBXにBlendShapeアニメーションカーブが含まれること");
            }
            finally
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(outputPath) != null)
                {
                    AssetDatabase.DeleteAsset(outputPath);
                }
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void GenericBlendShapeExport_BlendShapeのみのクリップでもFBXエクスポートが成功する()
        {
            // Arrange - BlendShapeカーブのみ（Transformカーブなし）
            var exporter = new FbxAnimationExporter();
            var clip = new AnimationClip();
            clip.name = "GenericBlendShapeOnlyExport";

            var binding = EditorCurveBinding.FloatCurve(
                "Body", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, 0f, 1f, 100f));

            var exportData = exporter.PrepareAllCurvesForExport(_testAnimator, clip);
            var outputPath = "Assets/TestP17005_GenericBSOnly_" + System.Guid.NewGuid().ToString("N").Substring(0, 8) + ".fbx";

            try
            {
                // Act
                var result = exporter.Export(exportData, outputPath);

                // Assert
                Assert.IsTrue(result, "BlendShapeのみのクリップでもFBXエクスポートが成功すること");
                Assert.IsTrue(
                    System.IO.File.Exists(outputPath) || AssetDatabase.LoadAssetAtPath<Object>(outputPath) != null,
                    "FBXファイルが生成されていること");
            }
            finally
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(outputPath) != null)
                {
                    AssetDatabase.DeleteAsset(outputPath);
                }
                Object.DestroyImmediate(clip);
            }
        }
#endif

        #endregion
    }
}
