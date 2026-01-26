using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using AnimationMergeTool.Editor.Domain.Models;
using AnimationMergeTool.Editor.Infrastructure;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// SkeletonExtractorクラスの単体テスト
    /// タスク P13-002: Animatorコンポーネントからスケルトンを取得するテスト
    /// </summary>
    public class SkeletonExtractorTests
    {
        private GameObject _testRootObject;
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

            if (_testRootObject != null)
            {
                Object.DestroyImmediate(_testRootObject);
                _testRootObject = null;
            }
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
        /// 簡単なボーン階層を作成する
        /// </summary>
        private GameObject CreateSimpleBoneHierarchy()
        {
            _testRootObject = new GameObject("TestCharacter");
            _createdObjects.Add(_testRootObject);

            var animator = _testRootObject.AddComponent<Animator>();

            // ボーン階層を作成
            var hips = new GameObject("Hips");
            hips.transform.SetParent(_testRootObject.transform);
            _createdObjects.Add(hips);

            var spine = new GameObject("Spine");
            spine.transform.SetParent(hips.transform);
            _createdObjects.Add(spine);

            var chest = new GameObject("Chest");
            chest.transform.SetParent(spine.transform);
            _createdObjects.Add(chest);

            var leftUpperLeg = new GameObject("LeftUpperLeg");
            leftUpperLeg.transform.SetParent(hips.transform);
            _createdObjects.Add(leftUpperLeg);

            var rightUpperLeg = new GameObject("RightUpperLeg");
            rightUpperLeg.transform.SetParent(hips.transform);
            _createdObjects.Add(rightUpperLeg);

            return _testRootObject;
        }

        /// <summary>
        /// SkinnedMeshRendererを持つボーン階層を作成する
        /// </summary>
        private GameObject CreateBoneHierarchyWithSkinnedMesh()
        {
            var root = CreateSimpleBoneHierarchy();

            // SkinnedMeshRendererを追加
            var meshObj = new GameObject("Body");
            meshObj.transform.SetParent(root.transform);
            _createdObjects.Add(meshObj);

            var skinnedMesh = meshObj.AddComponent<SkinnedMeshRenderer>();

            // ボーン配列を設定
            var hips = root.transform.Find("Hips");
            var spine = hips.Find("Spine");
            var chest = spine.Find("Chest");
            var leftUpperLeg = hips.Find("LeftUpperLeg");
            var rightUpperLeg = hips.Find("RightUpperLeg");

            skinnedMesh.bones = new Transform[]
            {
                hips,
                spine,
                chest,
                leftUpperLeg,
                rightUpperLeg
            };
            skinnedMesh.rootBone = hips;

            return root;
        }

        #region コンストラクタ テスト

        [Test]
        public void Constructor_インスタンスを生成できる()
        {
            // Act
            var extractor = new SkeletonExtractor();

            // Assert
            Assert.IsNotNull(extractor);
        }

        #endregion

        #region Extract テスト - nullケース

        [Test]
        public void Extract_Animatorがnullの場合_空のSkeletonDataを返す()
        {
            // Arrange
            var extractor = new SkeletonExtractor();

            // Act
            var result = extractor.Extract(null);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.HasSkeleton);
            Assert.IsNull(result.RootBone);
            Assert.AreEqual(0, result.Bones.Count);
        }

        #endregion

        #region Extract テスト - Genericリグ（SkinnedMeshRendererあり）

        [Test]
        public void Extract_SkinnedMeshRendererからボーンを取得できる()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var root = CreateBoneHierarchyWithSkinnedMesh();
            var animator = root.GetComponent<Animator>();

            // Act
            var result = extractor.Extract(animator);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasSkeleton);
            Assert.IsNotNull(result.RootBone);
            Assert.Greater(result.Bones.Count, 0);
        }

        [Test]
        public void Extract_SkinnedMeshRendererのrootBoneがルートボーンとして設定される()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var root = CreateBoneHierarchyWithSkinnedMesh();
            var animator = root.GetComponent<Animator>();
            var hips = root.transform.Find("Hips");

            // Act
            var result = extractor.Extract(animator);

            // Assert
            Assert.IsNotNull(result.RootBone);
            Assert.AreEqual(hips, result.RootBone);
        }

        [Test]
        public void Extract_SkinnedMeshRendererのボーン配列から全ボーンを取得できる()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var root = CreateBoneHierarchyWithSkinnedMesh();
            var animator = root.GetComponent<Animator>();

            // Act
            var result = extractor.Extract(animator);

            // Assert
            Assert.AreEqual(5, result.Bones.Count); // Hips, Spine, Chest, LeftUpperLeg, RightUpperLeg
        }

        #endregion

        #region Extract テスト - Genericリグ（SkinnedMeshRendererなし）

        [Test]
        public void Extract_SkinnedMeshRendererがない場合_Transform階層から取得する()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var root = CreateSimpleBoneHierarchy();
            var animator = root.GetComponent<Animator>();

            // Act
            var result = extractor.Extract(animator);

            // Assert
            Assert.IsNotNull(result);
            // SkinnedMeshRendererがない場合でもTransform階層からボーンを取得できる
            // P13-003実装後に具体的な動作が決まる
        }

        #endregion

        #region Extract テスト - ボーン階層順序

        [Test]
        public void Extract_ボーンが階層順にソートされている()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var root = CreateBoneHierarchyWithSkinnedMesh();
            var animator = root.GetComponent<Animator>();

            // Act
            var result = extractor.Extract(animator);

            // Assert
            Assert.IsTrue(result.HasSkeleton);

            // ルートボーンが最初に来る
            if (result.Bones.Count > 0)
            {
                Assert.AreEqual(result.RootBone, result.Bones[0]);
            }

            // 親ボーンが子ボーンより先に来る
            for (int i = 0; i < result.Bones.Count; i++)
            {
                var bone = result.Bones[i];
                var parent = bone.parent;

                // 親がボーンリストに含まれている場合、親のインデックスが小さい
                for (int j = i + 1; j < result.Bones.Count; j++)
                {
                    var laterBone = result.Bones[j];
                    Assert.AreNotEqual(laterBone, parent,
                        $"ボーン {bone.name} の親 {parent?.name} がボーン {laterBone.name} より後に来ています");
                }
            }
        }

        #endregion

        #region Extract テスト - 重複ボーン

        [Test]
        public void Extract_同じTransformが複数のSkinnedMeshRendererで参照されていても重複しない()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var root = CreateSimpleBoneHierarchy();
            var animator = root.GetComponent<Animator>();
            var hips = root.transform.Find("Hips");
            var spine = hips.Find("Spine");

            // 複数のSkinnedMeshRendererを追加（同じボーンを参照）
            var meshObj1 = new GameObject("Body1");
            meshObj1.transform.SetParent(root.transform);
            _createdObjects.Add(meshObj1);
            var smr1 = meshObj1.AddComponent<SkinnedMeshRenderer>();
            smr1.bones = new Transform[] { hips, spine };
            smr1.rootBone = hips;

            var meshObj2 = new GameObject("Body2");
            meshObj2.transform.SetParent(root.transform);
            _createdObjects.Add(meshObj2);
            var smr2 = meshObj2.AddComponent<SkinnedMeshRenderer>();
            smr2.bones = new Transform[] { hips, spine };
            smr2.rootBone = hips;

            // Act
            var result = extractor.Extract(animator);

            // Assert
            // 同じボーンが重複していないことを確認
            var uniqueBones = new HashSet<Transform>(result.Bones);
            Assert.AreEqual(uniqueBones.Count, result.Bones.Count, "重複するボーンが存在します");
        }

        #endregion

        #region Extract テスト - エッジケース

        [Test]
        public void Extract_Animatorに子がない場合_空のSkeletonDataを返す()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var root = CreateTestObject("EmptyAnimator");
            var animator = root.AddComponent<Animator>();

            // Act
            var result = extractor.Extract(animator);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.HasSkeleton);
        }

        [Test]
        public void Extract_SkinnedMeshRendererのbonesがnullの場合_エラーなく処理される()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var root = CreateSimpleBoneHierarchy();
            var animator = root.GetComponent<Animator>();

            // SkinnedMeshRendererを追加（bonesは未設定=null）
            var meshObj = new GameObject("Body");
            meshObj.transform.SetParent(root.transform);
            _createdObjects.Add(meshObj);
            meshObj.AddComponent<SkinnedMeshRenderer>();
            // bones配列は設定しない

            // Act & Assert - 例外が発生しないこと
            Assert.DoesNotThrow(() =>
            {
                var result = extractor.Extract(animator);
            });
        }

        [Test]
        public void Extract_SkinnedMeshRendererのbonesに含まれるnullエントリを無視する()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var root = CreateSimpleBoneHierarchy();
            var animator = root.GetComponent<Animator>();
            var hips = root.transform.Find("Hips");
            var spine = hips.Find("Spine");

            // SkinnedMeshRendererを追加（nullを含むbones配列）
            var meshObj = new GameObject("Body");
            meshObj.transform.SetParent(root.transform);
            _createdObjects.Add(meshObj);
            var smr = meshObj.AddComponent<SkinnedMeshRenderer>();
            smr.bones = new Transform[] { hips, null, spine, null };
            smr.rootBone = hips;

            // Act
            var result = extractor.Extract(animator);

            // Assert
            Assert.IsNotNull(result);
            // nullエントリは無視される
            foreach (var bone in result.Bones)
            {
                Assert.IsNotNull(bone, "ボーンリストにnullが含まれています");
            }
        }

        #endregion

        #region IsHumanoid判定 テスト

        [Test]
        public void Extract_AvatarがnullのAnimator_Genericとして処理される()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var root = CreateBoneHierarchyWithSkinnedMesh();
            var animator = root.GetComponent<Animator>();
            // avatar未設定

            // Act
            var result = extractor.Extract(animator);

            // Assert
            Assert.IsNotNull(result);
            // Generic扱いでSkinnedMeshRendererからボーンを取得
            Assert.IsTrue(result.HasSkeleton);
        }

        #endregion

        #region GetBonePath テスト

        [Test]
        public void GetBonePath_ボーンのAnimator相対パスを取得できる()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var root = CreateBoneHierarchyWithSkinnedMesh();
            var animator = root.GetComponent<Animator>();
            var spine = root.transform.Find("Hips/Spine");

            // Act
            var path = extractor.GetBonePath(animator, spine);

            // Assert
            Assert.AreEqual("Hips/Spine", path);
        }

        [Test]
        public void GetBonePath_ルートボーンのパスは直接ボーン名を返す()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var root = CreateBoneHierarchyWithSkinnedMesh();
            var animator = root.GetComponent<Animator>();
            var hips = root.transform.Find("Hips");

            // Act
            var path = extractor.GetBonePath(animator, hips);

            // Assert
            Assert.AreEqual("Hips", path);
        }

        [Test]
        public void GetBonePath_Animator自身のパスは空文字を返す()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var root = CreateBoneHierarchyWithSkinnedMesh();
            var animator = root.GetComponent<Animator>();

            // Act
            var path = extractor.GetBonePath(animator, root.transform);

            // Assert
            Assert.AreEqual(string.Empty, path);
        }

        [Test]
        public void GetBonePath_nullの場合はnullを返す()
        {
            // Arrange
            var extractor = new SkeletonExtractor();
            var root = CreateBoneHierarchyWithSkinnedMesh();
            var animator = root.GetComponent<Animator>();

            // Act & Assert
            Assert.IsNull(extractor.GetBonePath(animator, null));
            Assert.IsNull(extractor.GetBonePath(null, root.transform));
        }

        #endregion
    }
}
