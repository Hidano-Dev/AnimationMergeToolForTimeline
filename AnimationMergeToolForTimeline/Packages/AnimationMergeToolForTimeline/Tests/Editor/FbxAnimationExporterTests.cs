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
    }
}
