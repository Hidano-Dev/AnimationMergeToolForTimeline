using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using AnimationMergeTool.Editor.Infrastructure;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// 非スケルトンTransform取得機能の単体テスト
    /// タスク P13-004: スケルトン以外のGameObjectのTransform情報取得テスト
    /// 対応要件: FR-082（スケルトン以外のGameObjectのTransformアニメーションも含める）
    /// </summary>
    public class NonSkeletonTransformExtractorTests
    {
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            // テストで作成したGameObjectをクリーンアップ
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();
        }

        /// <summary>
        /// テスト用のGameObjectを作成してリストに追加する
        /// </summary>
        private GameObject CreateTestObject(string name)
        {
            var obj = new GameObject(name);
            _createdObjects.Add(obj);
            return obj;
        }

        /// <summary>
        /// スケルトン + プロップを持つキャラクターを作成する
        /// </summary>
        private (GameObject root, SkeletonExtractor extractor) CreateCharacterWithProps()
        {
            // ルートオブジェクト
            var root = CreateTestObject("Character");
            var animator = root.AddComponent<Animator>();

            // ボーン階層を作成
            var hips = CreateTestObject("Hips");
            hips.transform.SetParent(root.transform);

            var spine = CreateTestObject("Spine");
            spine.transform.SetParent(hips.transform);

            var chest = CreateTestObject("Chest");
            chest.transform.SetParent(spine.transform);

            var rightHand = CreateTestObject("RightHand");
            rightHand.transform.SetParent(chest.transform);

            // SkinnedMeshRendererを追加（ボーンを定義）
            var meshObj = CreateTestObject("Body");
            meshObj.transform.SetParent(root.transform);
            var skinnedMesh = meshObj.AddComponent<SkinnedMeshRenderer>();
            skinnedMesh.bones = new Transform[]
            {
                hips.transform,
                spine.transform,
                chest.transform,
                rightHand.transform
            };
            skinnedMesh.rootBone = hips.transform;

            // プロップ（非スケルトンTransform）を追加
            var sword = CreateTestObject("Sword");
            sword.transform.SetParent(rightHand.transform);
            sword.AddComponent<MeshFilter>(); // MeshFilterを持つのでボーンではない

            var swordRenderer = CreateTestObject("SwordRenderer");
            swordRenderer.transform.SetParent(sword.transform);
            swordRenderer.AddComponent<MeshRenderer>();

            // シールド（別のプロップ）
            var shield = CreateTestObject("Shield");
            shield.transform.SetParent(chest.transform);
            shield.AddComponent<MeshFilter>();

            var extractor = new SkeletonExtractor();
            return (root, extractor);
        }

        /// <summary>
        /// AnimationClipでアニメートされているパスを取得するヘルパー
        /// </summary>
        private HashSet<string> GetAnimatedPaths(AnimationClip clip)
        {
            var paths = new HashSet<string>();
            if (clip == null) return paths;

            var bindings = UnityEditor.AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                paths.Add(binding.path);
            }
            return paths;
        }

        #region コンストラクタ テスト

        [Test]
        public void ExtractNonSkeletonTransforms_メソッドが存在する()
        {
            // Arrange
            var extractor = new SkeletonExtractor();

            // Act & Assert - メソッドが存在することを確認（コンパイルが通れば成功）
            Assert.IsNotNull(extractor);
        }

        #endregion

        #region ExtractNonSkeletonTransforms テスト - 基本機能

        [Test]
        public void ExtractNonSkeletonTransforms_Animatorがnullの場合_空のリストを返す()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var skeleton = new Domain.Models.SkeletonData(null, new List<Transform>());

            // Act
            var result = extractor.ExtractNonSkeletonTransforms(null, skeleton);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ExtractNonSkeletonTransforms_SkeletonDataがnullの場合_全Transformを返す()
        {
            // Arrange
            var (root, extractor) = CreateCharacterWithProps();
            var animator = root.GetComponent<Animator>();

            // Act
            var result = extractor.ExtractNonSkeletonTransforms(animator, null);

            // Assert
            Assert.IsNotNull(result);
            // SkeletonDataがnullの場合、全てのTransformを非スケルトンとして扱う
            Assert.Greater(result.Count, 0);
        }

        [Test]
        public void ExtractNonSkeletonTransforms_プロップTransformを取得できる()
        {
            // Arrange
            var (root, extractor) = CreateCharacterWithProps();
            var animator = root.GetComponent<Animator>();
            var skeleton = extractor.Extract(animator);

            // Act
            var result = extractor.ExtractNonSkeletonTransforms(animator, skeleton);

            // Assert
            Assert.IsNotNull(result);

            // プロップ（Sword, Shield）が含まれていることを確認
            var resultNames = new List<string>();
            foreach (var t in result)
            {
                resultNames.Add(t.name);
            }

            Assert.IsTrue(resultNames.Contains("Sword"), "Swordが非スケルトンTransformとして取得されるべき");
            Assert.IsTrue(resultNames.Contains("Shield"), "Shieldが非スケルトンTransformとして取得されるべき");
        }

        [Test]
        public void ExtractNonSkeletonTransforms_スケルトンボーンは含まれない()
        {
            // Arrange
            var (root, extractor) = CreateCharacterWithProps();
            var animator = root.GetComponent<Animator>();
            var skeleton = extractor.Extract(animator);

            // Act
            var result = extractor.ExtractNonSkeletonTransforms(animator, skeleton);

            // Assert
            var resultNames = new List<string>();
            foreach (var t in result)
            {
                resultNames.Add(t.name);
            }

            // ボーンは含まれない
            Assert.IsFalse(resultNames.Contains("Hips"), "Hipsはスケルトンボーンなので含まれないべき");
            Assert.IsFalse(resultNames.Contains("Spine"), "Spineはスケルトンボーンなので含まれないべき");
            Assert.IsFalse(resultNames.Contains("Chest"), "Chestはスケルトンボーンなので含まれないべき");
            Assert.IsFalse(resultNames.Contains("RightHand"), "RightHandはスケルトンボーンなので含まれないべき");
        }

        #endregion

        #region ExtractNonSkeletonTransforms テスト - MeshFilter/MeshRendererを持つオブジェクト

        [Test]
        public void ExtractNonSkeletonTransforms_MeshFilterを持つオブジェクトを取得できる()
        {
            // Arrange
            var (root, extractor) = CreateCharacterWithProps();
            var animator = root.GetComponent<Animator>();
            var skeleton = extractor.Extract(animator);

            // Act
            var result = extractor.ExtractNonSkeletonTransforms(animator, skeleton);

            // Assert
            bool hasMeshFilterObjects = false;
            foreach (var t in result)
            {
                if (t.GetComponent<MeshFilter>() != null)
                {
                    hasMeshFilterObjects = true;
                    break;
                }
            }

            Assert.IsTrue(hasMeshFilterObjects, "MeshFilterを持つオブジェクトが含まれるべき");
        }

        [Test]
        public void ExtractNonSkeletonTransforms_SkinnedMeshRendererオブジェクトはスケルトンとして除外()
        {
            // Arrange
            var (root, extractor) = CreateCharacterWithProps();
            var animator = root.GetComponent<Animator>();
            var skeleton = extractor.Extract(animator);

            // Act
            var result = extractor.ExtractNonSkeletonTransforms(animator, skeleton);

            // Assert
            var resultNames = new List<string>();
            foreach (var t in result)
            {
                resultNames.Add(t.name);
            }

            // SkinnedMeshRendererを持つBodyオブジェクト自体は含まれないはず
            // （メッシュ描画用のオブジェクトはアニメーション対象外）
            Assert.IsFalse(resultNames.Contains("Body"),
                "SkinnedMeshRendererを持つオブジェクトは含まれないべき");
        }

        #endregion

        #region ExtractNonSkeletonTransforms テスト - 階層構造

        [Test]
        public void ExtractNonSkeletonTransforms_子オブジェクトも再帰的に取得される()
        {
            // Arrange
            var (root, extractor) = CreateCharacterWithProps();
            var animator = root.GetComponent<Animator>();
            var skeleton = extractor.Extract(animator);

            // Act
            var result = extractor.ExtractNonSkeletonTransforms(animator, skeleton);

            // Assert
            var resultNames = new List<string>();
            foreach (var t in result)
            {
                resultNames.Add(t.name);
            }

            // Swordの子であるSwordRendererも取得される
            Assert.IsTrue(resultNames.Contains("SwordRenderer"),
                "プロップの子オブジェクトも再帰的に取得されるべき");
        }

        [Test]
        public void ExtractNonSkeletonTransforms_ボーンの子でも非スケルトンなら取得される()
        {
            // Arrange
            var (root, extractor) = CreateCharacterWithProps();
            var animator = root.GetComponent<Animator>();
            var skeleton = extractor.Extract(animator);

            // Swordはボーン(RightHand)の子だが、スケルトンではない
            var sword = root.transform.Find("Hips/Spine/Chest/RightHand/Sword");

            // Act
            var result = extractor.ExtractNonSkeletonTransforms(animator, skeleton);

            // Assert
            Assert.IsTrue(result.Contains(sword.transform),
                "ボーンの子オブジェクトでもスケルトンでなければ取得されるべき");
        }

        #endregion

        #region ExtractNonSkeletonTransforms テスト - AnimationClipに基づくフィルタリング

        [Test]
        public void ExtractNonSkeletonTransformsWithClip_アニメートされているパスのみ取得できる()
        {
            // Arrange
            var (root, extractor) = CreateCharacterWithProps();
            var animator = root.GetComponent<Animator>();
            var skeleton = extractor.Extract(animator);

            // SwordのみアニメートするAnimationClipを作成
            var clip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            clip.SetCurve("Hips/Spine/Chest/RightHand/Sword", typeof(Transform), "localPosition.x", curve);

            // Act
            var result = extractor.ExtractNonSkeletonTransforms(animator, skeleton, clip);

            // Assert
            var resultNames = new List<string>();
            foreach (var t in result)
            {
                resultNames.Add(t.name);
            }

            Assert.IsTrue(resultNames.Contains("Sword"),
                "アニメートされているSwordは含まれるべき");
            Assert.IsFalse(resultNames.Contains("Shield"),
                "アニメートされていないShieldは含まれないべき");

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ExtractNonSkeletonTransformsWithClip_AnimationClipがnullの場合は全て取得()
        {
            // Arrange
            var (root, extractor) = CreateCharacterWithProps();
            var animator = root.GetComponent<Animator>();
            var skeleton = extractor.Extract(animator);

            // Act
            var result = extractor.ExtractNonSkeletonTransforms(animator, skeleton, null);

            // Assert
            // AnimationClipがnullの場合は通常のExtractNonSkeletonTransformsと同じ動作
            Assert.Greater(result.Count, 0);
        }

        #endregion

        #region ExtractNonSkeletonTransforms テスト - エッジケース

        [Test]
        public void ExtractNonSkeletonTransforms_空のスケルトンの場合_全Transformを返す()
        {
            // Arrange
            var root = CreateTestObject("EmptyCharacter");
            var animator = root.AddComponent<Animator>();

            // プロップのみ追加
            var prop = CreateTestObject("Prop");
            prop.transform.SetParent(root.transform);
            prop.AddComponent<MeshFilter>();

            var extractor = new SkeletonExtractor();
            var skeleton = new Domain.Models.SkeletonData(null, new List<Transform>());

            // Act
            var result = extractor.ExtractNonSkeletonTransforms(animator, skeleton);

            // Assert
            Assert.IsNotNull(result);
            Assert.Greater(result.Count, 0);

            var resultNames = new List<string>();
            foreach (var t in result)
            {
                resultNames.Add(t.name);
            }
            Assert.IsTrue(resultNames.Contains("Prop"), "プロップが含まれるべき");
        }

        [Test]
        public void ExtractNonSkeletonTransforms_非アクティブなGameObjectも取得できる()
        {
            // Arrange
            var (root, extractor) = CreateCharacterWithProps();
            var animator = root.GetComponent<Animator>();
            var skeleton = extractor.Extract(animator);

            // Shieldを非アクティブにする
            var shield = root.transform.Find("Hips/Spine/Chest/Shield");
            shield.gameObject.SetActive(false);

            // Act
            var result = extractor.ExtractNonSkeletonTransforms(animator, skeleton);

            // Assert
            Assert.IsTrue(result.Contains(shield),
                "非アクティブなGameObjectも取得されるべき");
        }

        [Test]
        public void ExtractNonSkeletonTransforms_Animator自身は含まれない()
        {
            // Arrange
            var (root, extractor) = CreateCharacterWithProps();
            var animator = root.GetComponent<Animator>();
            var skeleton = extractor.Extract(animator);

            // Act
            var result = extractor.ExtractNonSkeletonTransforms(animator, skeleton);

            // Assert
            Assert.IsFalse(result.Contains(root.transform),
                "Animator自身は結果に含まれないべき");
        }

        #endregion

        #region GetNonSkeletonPath テスト

        [Test]
        public void GetNonSkeletonPath_非スケルトンTransformのパスを取得できる()
        {
            // Arrange
            var (root, extractor) = CreateCharacterWithProps();
            var animator = root.GetComponent<Animator>();
            var sword = root.transform.Find("Hips/Spine/Chest/RightHand/Sword");

            // Act
            var path = extractor.GetBonePath(animator, sword);

            // Assert
            Assert.AreEqual("Hips/Spine/Chest/RightHand/Sword", path);
        }

        #endregion
    }
}
