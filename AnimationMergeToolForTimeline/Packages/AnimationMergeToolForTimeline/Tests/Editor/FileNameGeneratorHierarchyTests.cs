using NUnit.Framework;
using AnimationMergeTool.Editor.Infrastructure;
using UnityEngine;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// FileNameGenerator.GetHierarchicalAnimatorName の単体テスト
    /// 親Animatorが存在する場合のファイル名生成を検証する
    /// </summary>
    public class FileNameGeneratorHierarchyTests
    {
        private GameObject _rootObj;

        [TearDown]
        public void TearDown()
        {
            if (_rootObj != null)
            {
                Object.DestroyImmediate(_rootObj);
            }
        }

        [Test]
        public void GetHierarchicalAnimatorName_親Animatorがない場合Animator名のみ返す()
        {
            // Arrange
            _rootObj = new GameObject("MyCharacter");
            var animator = _rootObj.AddComponent<Animator>();

            // Act
            var result = FileNameGenerator.GetHierarchicalAnimatorName(animator);

            // Assert
            Assert.AreEqual("MyCharacter", result);
        }

        [Test]
        public void GetHierarchicalAnimatorName_親Animatorがある場合親名_子名を返す()
        {
            // Arrange
            _rootObj = new GameObject("ParentModel");
            _rootObj.AddComponent<Animator>();

            var child = new GameObject("ChildModel");
            child.transform.SetParent(_rootObj.transform);
            var childAnimator = child.AddComponent<Animator>();

            // Act
            var result = FileNameGenerator.GetHierarchicalAnimatorName(childAnimator);

            // Assert
            Assert.AreEqual("ParentModel_ChildModel", result);
        }

        [Test]
        public void GetHierarchicalAnimatorName_親にAnimatorがないGameObjectがある場合スキップして探す()
        {
            // Arrange - 構造: GrandParent(Animator) > Parent(なし) > Child(Animator)
            _rootObj = new GameObject("GrandParent");
            _rootObj.AddComponent<Animator>();

            var parent = new GameObject("Parent");
            parent.transform.SetParent(_rootObj.transform);
            // ParentにはAnimatorなし

            var child = new GameObject("Child");
            child.transform.SetParent(parent.transform);
            var childAnimator = child.AddComponent<Animator>();

            // Act
            var result = FileNameGenerator.GetHierarchicalAnimatorName(childAnimator);

            // Assert - 最も近い親Animatorの名前が使われる
            Assert.AreEqual("GrandParent_Child", result);
        }

        [Test]
        public void GetHierarchicalAnimatorName_nullの場合NoAnimatorを返す()
        {
            // Act
            var result = FileNameGenerator.GetHierarchicalAnimatorName(null);

            // Assert
            Assert.AreEqual("NoAnimator", result);
        }

        [Test]
        public void GetHierarchicalAnimatorName_ルートにAnimatorがあり親がない場合名前のみ返す()
        {
            // Arrange
            _rootObj = new GameObject("RootAnimator");
            var animator = _rootObj.AddComponent<Animator>();

            // Act
            var result = FileNameGenerator.GetHierarchicalAnimatorName(animator);

            // Assert
            Assert.AreEqual("RootAnimator", result);
        }

        [Test]
        public void GetHierarchicalAnimatorName_複数階層のAnimatorがある場合最も近い親を使う()
        {
            // Arrange - 構造: Root(Animator) > Middle(Animator) > Child(Animator)
            _rootObj = new GameObject("Root");
            _rootObj.AddComponent<Animator>();

            var middle = new GameObject("Middle");
            middle.transform.SetParent(_rootObj.transform);
            middle.AddComponent<Animator>();

            var child = new GameObject("Child");
            child.transform.SetParent(middle.transform);
            var childAnimator = child.AddComponent<Animator>();

            // Act
            var result = FileNameGenerator.GetHierarchicalAnimatorName(childAnimator);

            // Assert - 最も近い親Animator（Middle）が使われる
            Assert.AreEqual("Middle_Child", result);
        }

        [Test]
        public void GetHierarchicalAnimatorName_ファイル名に反映される()
        {
            // Arrange
            _rootObj = new GameObject("ParentModel");
            _rootObj.AddComponent<Animator>();

            var child = new GameObject("ChildModel");
            child.transform.SetParent(_rootObj.transform);
            var childAnimator = child.AddComponent<Animator>();

            var generator = new FileNameGenerator();

            // Act
            var animatorName = FileNameGenerator.GetHierarchicalAnimatorName(childAnimator);
            var baseName = generator.GenerateBaseName("MyTimeline", animatorName);

            // Assert
            Assert.AreEqual("MyTimeline_ParentModel_ChildModel_Merged.anim", baseName);
        }

        [Test]
        public void GetHierarchicalAnimatorName_FBXファイル名にも反映される()
        {
            // Arrange
            _rootObj = new GameObject("ParentModel");
            _rootObj.AddComponent<Animator>();

            var child = new GameObject("ChildModel");
            child.transform.SetParent(_rootObj.transform);
            var childAnimator = child.AddComponent<Animator>();

            var generator = new FileNameGenerator();

            // Act
            var animatorName = FileNameGenerator.GetHierarchicalAnimatorName(childAnimator);
            var baseName = generator.GenerateBaseName("MyTimeline", animatorName, ".fbx");

            // Assert
            Assert.AreEqual("MyTimeline_ParentModel_ChildModel_Merged.fbx", baseName);
        }

        [Test]
        public void GetHierarchicalAnimatorName_親Animatorなしの場合ファイル名は従来通り()
        {
            // Arrange
            _rootObj = new GameObject("MyAnimator");
            var animator = _rootObj.AddComponent<Animator>();

            var generator = new FileNameGenerator();

            // Act
            var animatorName = FileNameGenerator.GetHierarchicalAnimatorName(animator);
            var baseName = generator.GenerateBaseName("MyTimeline", animatorName);

            // Assert
            Assert.AreEqual("MyTimeline_MyAnimator_Merged.anim", baseName);
        }
    }
}
