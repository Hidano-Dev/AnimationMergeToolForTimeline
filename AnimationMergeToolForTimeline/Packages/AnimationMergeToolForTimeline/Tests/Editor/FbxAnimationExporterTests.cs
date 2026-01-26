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
        public void ExtractTransformCurves_Animatorがnullの場合_空のリストを返す()
        {
            // Arrange
            var exporter = new FbxAnimationExporter();

            // Act
            var result = exporter.ExtractTransformCurves(_testClip, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
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
    }
}
