using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using AnimationMergeTool.Editor.Domain;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// CurvePathCorrectorクラスの単体テスト
    /// </summary>
    public class CurvePathCorrectorTests
    {
        private CurvePathCorrector _corrector;
        private GameObject _rootObject;

        /// <summary>
        /// テスト用ヒエラルキー:
        /// Root (Animator)
        ///   ├── Body
        ///   │   └── BodyMesh (SkinnedMeshRenderer)
        ///   └── Face
        ///       ├── FaceMesh (SkinnedMeshRenderer)
        ///       └── Eye
        ///           └── EyeMesh
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _corrector = new CurvePathCorrector();

            _rootObject = new GameObject("Root");

            var body = new GameObject("Body");
            body.transform.SetParent(_rootObject.transform);

            var bodyMesh = new GameObject("BodyMesh");
            bodyMesh.transform.SetParent(body.transform);
            bodyMesh.AddComponent<SkinnedMeshRenderer>();

            var face = new GameObject("Face");
            face.transform.SetParent(_rootObject.transform);

            var faceMesh = new GameObject("FaceMesh");
            faceMesh.transform.SetParent(face.transform);
            faceMesh.AddComponent<SkinnedMeshRenderer>();

            var eye = new GameObject("Eye");
            eye.transform.SetParent(face.transform);

            var eyeMesh = new GameObject("EyeMesh");
            eyeMesh.transform.SetParent(eye.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (_rootObject != null)
            {
                Object.DestroyImmediate(_rootObject);
            }
        }

        #region CorrectPaths 基本動作テスト

        [Test]
        public void CorrectPaths_パスが解決可能な場合_補正しない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Body/BodyMesh", typeof(Transform), "m_LocalPosition.x");
            var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            var pairs = new List<CurveBindingPair> { new CurveBindingPair(binding, curve) };

            // Act
            var result = _corrector.CorrectPaths(pairs, _rootObject.transform);

            // Assert
            Assert.AreEqual(1, result.CorrectedPairs.Count);
            Assert.AreEqual("Body/BodyMesh", result.CorrectedPairs[0].Binding.path);
            Assert.AreEqual(0, result.CorrectedCount);
        }

        [Test]
        public void CorrectPaths_パスが解決不可で候補1件_補正する()
        {
            // Arrange: "FaceMesh" はルート直下にないが Face/FaceMesh として存在
            var binding = EditorCurveBinding.FloatCurve("FaceMesh", typeof(Transform), "m_LocalPosition.x");
            var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            var pairs = new List<CurveBindingPair> { new CurveBindingPair(binding, curve) };

            // Act
            var result = _corrector.CorrectPaths(pairs, _rootObject.transform);

            // Assert
            Assert.AreEqual(1, result.CorrectedPairs.Count);
            Assert.AreEqual("Face/FaceMesh", result.CorrectedPairs[0].Binding.path);
            Assert.AreEqual(1, result.CorrectedCount);
        }

        [Test]
        public void CorrectPaths_パスが解決不可で候補複数_補正しない()
        {
            // Arrange: "Mesh" という名前のオブジェクトを2箇所に追加
            var mesh1 = new GameObject("Mesh");
            mesh1.transform.SetParent(_rootObject.transform.Find("Body"));
            var mesh2 = new GameObject("Mesh");
            mesh2.transform.SetParent(_rootObject.transform.Find("Face"));

            var binding = EditorCurveBinding.FloatCurve("Mesh", typeof(Transform), "m_LocalPosition.x");
            var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            var pairs = new List<CurveBindingPair> { new CurveBindingPair(binding, curve) };

            // Act
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("複数の候補"));
            var result = _corrector.CorrectPaths(pairs, _rootObject.transform);

            // Assert
            Assert.AreEqual(1, result.CorrectedPairs.Count);
            Assert.AreEqual("Mesh", result.CorrectedPairs[0].Binding.path);
            Assert.AreEqual(0, result.CorrectedCount);
            Assert.AreEqual(1, result.AmbiguousCount);
        }

        [Test]
        public void CorrectPaths_パスが解決不可で候補0件_補正しない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("UnknownNode", typeof(Transform), "m_LocalPosition.x");
            var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            var pairs = new List<CurveBindingPair> { new CurveBindingPair(binding, curve) };

            // Act
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("見つかりませんでした"));
            var result = _corrector.CorrectPaths(pairs, _rootObject.transform);

            // Assert
            Assert.AreEqual(1, result.CorrectedPairs.Count);
            Assert.AreEqual("UnknownNode", result.CorrectedPairs[0].Binding.path);
            Assert.AreEqual(0, result.CorrectedCount);
            Assert.AreEqual(1, result.NotFoundCount);
        }

        [Test]
        public void CorrectPaths_空パス_補正しない()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x");
            var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            var pairs = new List<CurveBindingPair> { new CurveBindingPair(binding, curve) };

            // Act
            var result = _corrector.CorrectPaths(pairs, _rootObject.transform);

            // Assert
            Assert.AreEqual(1, result.CorrectedPairs.Count);
            Assert.AreEqual("", result.CorrectedPairs[0].Binding.path);
            Assert.AreEqual(0, result.CorrectedCount);
        }

        [Test]
        public void CorrectPaths_AnimatorTransformがnull_元リストをそのまま返す()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("FaceMesh", typeof(Transform), "m_LocalPosition.x");
            var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            var pairs = new List<CurveBindingPair> { new CurveBindingPair(binding, curve) };

            // Act
            var result = _corrector.CorrectPaths(pairs, null);

            // Assert
            Assert.AreEqual(1, result.CorrectedPairs.Count);
            Assert.AreEqual("FaceMesh", result.CorrectedPairs[0].Binding.path);
            Assert.AreEqual(0, result.CorrectedCount);
        }

        [Test]
        public void CorrectPaths_pairsがnull_空のリストを返す()
        {
            // Act
            var result = _corrector.CorrectPaths(null, _rootObject.transform);

            // Assert
            Assert.IsNotNull(result.CorrectedPairs);
            Assert.AreEqual(0, result.CorrectedPairs.Count);
        }

        #endregion

        #region IsTargetCurveType テスト

        [Test]
        public void IsTargetCurveType_Transformカーブ_補正対象()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(Transform), "m_LocalPosition.x");

            // Act & Assert
            Assert.IsTrue(_corrector.IsTargetCurveType(binding));
        }

        [Test]
        public void IsTargetCurveType_BlendShapeカーブ_補正対象()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("FaceMesh", typeof(SkinnedMeshRenderer), "blendShape.Smile");

            // Act & Assert
            Assert.IsTrue(_corrector.IsTargetCurveType(binding));
        }

        [Test]
        public void IsTargetCurveType_Animatorカーブ_補正対象外()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("", typeof(Animator), "SomeProperty");

            // Act & Assert
            Assert.IsFalse(_corrector.IsTargetCurveType(binding));
        }

        [Test]
        public void IsTargetCurveType_MeshRendererカーブ_補正対象外()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(MeshRenderer), "material._Color.r");

            // Act & Assert
            Assert.IsFalse(_corrector.IsTargetCurveType(binding));
        }

        [Test]
        public void IsTargetCurveType_SkinnedMeshRendererの非BlendShapeカーブ_補正対象外()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "m_Enabled");

            // Act & Assert
            Assert.IsFalse(_corrector.IsTargetCurveType(binding));
        }

        #endregion

        #region 補正対象外カーブタイプのスキップテスト

        [Test]
        public void CorrectPaths_補正対象外のカーブタイプはパス補正しない()
        {
            // Arrange: Animatorタイプは補正対象外。パスが解決不可でもそのまま返す
            var binding = EditorCurveBinding.FloatCurve("SomeUnknownPath", typeof(Animator), "SomeProperty");
            var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            var pairs = new List<CurveBindingPair> { new CurveBindingPair(binding, curve) };

            // Act
            var result = _corrector.CorrectPaths(pairs, _rootObject.transform);

            // Assert
            Assert.AreEqual(1, result.CorrectedPairs.Count);
            Assert.AreEqual("SomeUnknownPath", result.CorrectedPairs[0].Binding.path);
            Assert.AreEqual(0, result.CorrectedCount);
        }

        #endregion

        #region 実用シナリオテスト

        [Test]
        public void CorrectPaths_ネストAnimatorのパスを親Animator基準に補正する()
        {
            // Arrange: ネストされたAnimator用のクリップが親Animatorのトラックに配置された場合
            // 例: Face > FaceMesh 用のクリップが Root のトラックに配置
            // パス "FaceMesh/..." は "Face/FaceMesh/..." に補正される
            var bindings = new List<CurveBindingPair>
            {
                new CurveBindingPair(
                    EditorCurveBinding.FloatCurve("FaceMesh", typeof(SkinnedMeshRenderer), "blendShape.Smile"),
                    AnimationCurve.Linear(0f, 0f, 1f, 1f)),
                new CurveBindingPair(
                    EditorCurveBinding.FloatCurve("FaceMesh", typeof(SkinnedMeshRenderer), "blendShape.Blink"),
                    AnimationCurve.Linear(0f, 0f, 1f, 0.5f)),
                new CurveBindingPair(
                    EditorCurveBinding.FloatCurve("Eye/EyeMesh", typeof(Transform), "m_LocalPosition.x"),
                    AnimationCurve.Linear(0f, 0f, 1f, 1f))
            };

            // Act
            var result = _corrector.CorrectPaths(bindings, _rootObject.transform);

            // Assert
            Assert.AreEqual(3, result.CorrectedPairs.Count);
            Assert.AreEqual("Face/FaceMesh", result.CorrectedPairs[0].Binding.path);
            Assert.AreEqual("Face/FaceMesh", result.CorrectedPairs[1].Binding.path);
            Assert.AreEqual("Face/Eye/EyeMesh", result.CorrectedPairs[2].Binding.path);
            Assert.AreEqual(3, result.CorrectedCount);
        }

        [Test]
        public void CorrectPaths_同じパスの複数プロパティがキャッシュで効率的に処理される()
        {
            // Arrange: 同じパス "FaceMesh" で異なるプロパティ（x, y, z）
            var pairs = new List<CurveBindingPair>
            {
                new CurveBindingPair(
                    EditorCurveBinding.FloatCurve("FaceMesh", typeof(Transform), "m_LocalPosition.x"),
                    AnimationCurve.Linear(0f, 0f, 1f, 1f)),
                new CurveBindingPair(
                    EditorCurveBinding.FloatCurve("FaceMesh", typeof(Transform), "m_LocalPosition.y"),
                    AnimationCurve.Linear(0f, 0f, 1f, 2f)),
                new CurveBindingPair(
                    EditorCurveBinding.FloatCurve("FaceMesh", typeof(Transform), "m_LocalPosition.z"),
                    AnimationCurve.Linear(0f, 0f, 1f, 3f))
            };

            // Act
            var result = _corrector.CorrectPaths(pairs, _rootObject.transform);

            // Assert: 3件すべてが補正される
            Assert.AreEqual(3, result.CorrectedPairs.Count);
            Assert.AreEqual("Face/FaceMesh", result.CorrectedPairs[0].Binding.path);
            Assert.AreEqual("Face/FaceMesh", result.CorrectedPairs[1].Binding.path);
            Assert.AreEqual("Face/FaceMesh", result.CorrectedPairs[2].Binding.path);
            Assert.AreEqual(3, result.CorrectedCount);
            // プロパティ名が保持されていること
            Assert.AreEqual("m_LocalPosition.x", result.CorrectedPairs[0].Binding.propertyName);
            Assert.AreEqual("m_LocalPosition.y", result.CorrectedPairs[1].Binding.propertyName);
            Assert.AreEqual("m_LocalPosition.z", result.CorrectedPairs[2].Binding.propertyName);
        }

        [Test]
        public void CorrectPaths_補正結果のカーブデータが保持される()
        {
            // Arrange
            var originalCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            var binding = EditorCurveBinding.FloatCurve("FaceMesh", typeof(Transform), "m_LocalPosition.x");
            var pairs = new List<CurveBindingPair> { new CurveBindingPair(binding, originalCurve) };

            // Act
            var result = _corrector.CorrectPaths(pairs, _rootObject.transform);

            // Assert: カーブが同じインスタンスであること
            Assert.AreSame(originalCurve, result.CorrectedPairs[0].Curve);
        }

        [Test]
        public void CorrectPaths_補正結果のバインディングタイプが保持される()
        {
            // Arrange
            var binding = EditorCurveBinding.FloatCurve("FaceMesh", typeof(SkinnedMeshRenderer), "blendShape.Smile");
            var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            var pairs = new List<CurveBindingPair> { new CurveBindingPair(binding, curve) };

            // Act
            var result = _corrector.CorrectPaths(pairs, _rootObject.transform);

            // Assert
            Assert.AreEqual(typeof(SkinnedMeshRenderer), result.CorrectedPairs[0].Binding.type);
            Assert.AreEqual("blendShape.Smile", result.CorrectedPairs[0].Binding.propertyName);
        }

        #endregion

        #region CanResolvePath テスト

        [Test]
        public void CanResolvePath_存在するパス_trueを返す()
        {
            Assert.IsTrue(_corrector.CanResolvePath(_rootObject.transform, "Body/BodyMesh"));
        }

        [Test]
        public void CanResolvePath_存在しないパス_falseを返す()
        {
            Assert.IsFalse(_corrector.CanResolvePath(_rootObject.transform, "NonExistent"));
        }

        [Test]
        public void CanResolvePath_nullTransform_falseを返す()
        {
            Assert.IsFalse(_corrector.CanResolvePath(null, "Body"));
        }

        [Test]
        public void CanResolvePath_空パス_falseを返す()
        {
            Assert.IsFalse(_corrector.CanResolvePath(_rootObject.transform, ""));
        }

        #endregion

        #region FindTransformsByLeafName テスト

        [Test]
        public void FindTransformsByLeafName_一意の名前_1件返す()
        {
            var results = _corrector.FindTransformsByLeafName(_rootObject.transform, "FaceMesh");
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("FaceMesh", results[0].name);
        }

        [Test]
        public void FindTransformsByLeafName_存在しない名前_0件返す()
        {
            var results = _corrector.FindTransformsByLeafName(_rootObject.transform, "NonExistent");
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void FindTransformsByLeafName_nullRoot_0件返す()
        {
            var results = _corrector.FindTransformsByLeafName(null, "Body");
            Assert.AreEqual(0, results.Count);
        }

        #endregion

        #region GetRelativePath テスト

        [Test]
        public void GetRelativePath_直接の子_名前だけを返す()
        {
            var body = _rootObject.transform.Find("Body");
            var path = _corrector.GetRelativePath(_rootObject.transform, body);
            Assert.AreEqual("Body", path);
        }

        [Test]
        public void GetRelativePath_ネストされた子_スラッシュ区切りのパスを返す()
        {
            var faceMesh = _rootObject.transform.Find("Face/FaceMesh");
            var path = _corrector.GetRelativePath(_rootObject.transform, faceMesh);
            Assert.AreEqual("Face/FaceMesh", path);
        }

        [Test]
        public void GetRelativePath_深くネストされた子_正しいパスを返す()
        {
            var eyeMesh = _rootObject.transform.Find("Face/Eye/EyeMesh");
            var path = _corrector.GetRelativePath(_rootObject.transform, eyeMesh);
            Assert.AreEqual("Face/Eye/EyeMesh", path);
        }

        [Test]
        public void GetRelativePath_rootとtargetが同じ_空文字列を返す()
        {
            var path = _corrector.GetRelativePath(_rootObject.transform, _rootObject.transform);
            Assert.AreEqual("", path);
        }

        #endregion

        #region 混合ケーステスト

        [Test]
        public void CorrectPaths_補正対象と対象外が混在する場合_対象のみ補正する()
        {
            // Arrange
            var pairs = new List<CurveBindingPair>
            {
                // 補正対象: Transform、パス解決不可
                new CurveBindingPair(
                    EditorCurveBinding.FloatCurve("FaceMesh", typeof(Transform), "m_LocalPosition.x"),
                    AnimationCurve.Linear(0f, 0f, 1f, 1f)),
                // 補正対象外: Animator
                new CurveBindingPair(
                    EditorCurveBinding.FloatCurve("FaceMesh", typeof(Animator), "SomeParam"),
                    AnimationCurve.Linear(0f, 0f, 1f, 1f)),
                // 補正対象: BlendShape、パス解決不可
                new CurveBindingPair(
                    EditorCurveBinding.FloatCurve("BodyMesh", typeof(SkinnedMeshRenderer), "blendShape.Smile"),
                    AnimationCurve.Linear(0f, 0f, 1f, 1f)),
                // 補正対象: Transform、パス解決可能
                new CurveBindingPair(
                    EditorCurveBinding.FloatCurve("Body", typeof(Transform), "m_LocalPosition.y"),
                    AnimationCurve.Linear(0f, 0f, 1f, 1f))
            };

            // Act
            var result = _corrector.CorrectPaths(pairs, _rootObject.transform);

            // Assert
            Assert.AreEqual(4, result.CorrectedPairs.Count);
            Assert.AreEqual("Face/FaceMesh", result.CorrectedPairs[0].Binding.path); // 補正された
            Assert.AreEqual("FaceMesh", result.CorrectedPairs[1].Binding.path);       // 対象外、そのまま
            Assert.AreEqual("Body/BodyMesh", result.CorrectedPairs[2].Binding.path);  // 補正された
            Assert.AreEqual("Body", result.CorrectedPairs[3].Binding.path);            // 解決可能、そのまま
            Assert.AreEqual(2, result.CorrectedCount);
        }

        #endregion
    }
}
