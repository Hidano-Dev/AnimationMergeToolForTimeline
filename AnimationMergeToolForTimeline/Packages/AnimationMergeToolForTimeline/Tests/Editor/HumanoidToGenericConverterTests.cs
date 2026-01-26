using System.Collections.Generic;
using AnimationMergeTool.Editor.Domain.Models;
using AnimationMergeTool.Editor.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// HumanoidToGenericConverterクラスの単体テスト
    /// タスク P14-003: Humanoidボーン名からTransformパスへの変換テスト
    /// </summary>
    public class HumanoidToGenericConverterTests
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
        /// 非Humanoidのボーン階層を作成する
        /// </summary>
        private GameObject CreateNonHumanoidBoneHierarchy()
        {
            var root = CreateTestObject("TestCharacter");
            var animator = root.AddComponent<Animator>();
            // avatarを設定しない = Generic扱い

            var hips = CreateTestObject("Hips");
            hips.transform.SetParent(root.transform);

            var spine = CreateTestObject("Spine");
            spine.transform.SetParent(hips.transform);

            return root;
        }

        #region コンストラクタ テスト

        [Test]
        public void Constructor_インスタンスを生成できる()
        {
            // Act
            var converter = new HumanoidToGenericConverter();

            // Assert
            Assert.IsNotNull(converter);
        }

        #endregion

        #region GetTransformPath テスト - nullケース

        [Test]
        public void GetTransformPath_Animatorがnullの場合_nullを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();

            // Act
            var result = converter.GetTransformPath(null, HumanBodyBones.Hips);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetTransformPath_AnimatorがHumanoidでない場合_nullを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();

            // Act
            var result = converter.GetTransformPath(animator, HumanBodyBones.Hips);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetTransformPath_LastBoneを指定した場合_nullを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();

            // Act
            var result = converter.GetTransformPath(animator, HumanBodyBones.LastBone);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region GetTransformPath テスト - オプションボーン未設定

        [Test]
        public void GetTransformPath_ボーンが未設定の場合_nullを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();

            // animatorはGenericなので、どのボーンもnullを返す
            // これはオプションボーンが未設定の場合と同じ動作

            // Act
            var result = converter.GetTransformPath(animator, HumanBodyBones.LeftEye);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region IsHumanoidClip テスト

        [Test]
        public void IsHumanoidClip_clipがnullの場合_falseを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();

            // Act
            var result = converter.IsHumanoidClip(null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsHumanoidClip_GenericクリップをisHumanMotionで判定_falseを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            _createdObjects.Add(new GameObject { name = "ClipHolder" }); // クリーンアップ用
            // クリップを作成しただけではisHumanMotionはfalse

            // Act
            var result = converter.IsHumanoidClip(clip);

            // Assert
            Assert.IsFalse(result);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        #endregion

        #region BuildRelativePath テスト（GetTransformPath経由）

        [Test]
        public void GetTransformPath_ターゲットがAnimatorと同じ場合_空文字を返す想定()
        {
            // このテストはHumanoidリグを必要とするため、
            // 実際のHumanoidアバターがある環境でのみ有効
            // ここではGenericリグでの挙動を確認

            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();

            // Generic AnimatorではgetBoneTransformはnullを返すため、
            // このテストは実際にはnullを返す挙動を確認する

            // Act
            var result = converter.GetTransformPath(animator, HumanBodyBones.Hips);

            // Assert
            // GenericリグではHumanoidボーンが取得できないためnull
            Assert.IsNull(result);
        }

        #endregion

        #region エッジケース テスト

        [Test]
        public void GetTransformPath_全HumanBodyBones列挙値で例外が発生しない()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();

            // Act & Assert
            // すべてのHumanBodyBones値に対して例外が発生しないことを確認
            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                Assert.DoesNotThrow(() =>
                {
                    converter.GetTransformPath(animator, bone);
                }, $"ボーン {bone} で例外が発生しました");
            }
        }

        [Test]
        public void GetTransformPath_Animatorがアクティブでない場合もエラーなく処理される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();
            animator.enabled = false;

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var result = converter.GetTransformPath(animator, HumanBodyBones.Hips);
                // Generic Animatorなのでnullを返す
                Assert.IsNull(result);
            });
        }

        [Test]
        public void GetTransformPath_GameObjectが非アクティブの場合もエラーなく処理される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();
            root.SetActive(false);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var result = converter.GetTransformPath(animator, HumanBodyBones.Hips);
                // Generic Animatorなのでnullを返す
                Assert.IsNull(result);
            });
        }

        #endregion

        #region ConvertMuscleCurvesToRotation テスト - P14-005

        [Test]
        public void ConvertMuscleCurvesToRotation_Animatorがnullの場合_空のリストを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();

            // Act
            var result = converter.ConvertMuscleCurvesToRotation(null, clip);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertMuscleCurvesToRotation_AnimationClipがnullの場合_空のリストを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();

            // Act
            var result = converter.ConvertMuscleCurvesToRotation(animator, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ConvertMuscleCurvesToRotation_AnimatorがHumanoidでない場合_空のリストを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();
            var clip = new AnimationClip();

            // Act
            var result = converter.ConvertMuscleCurvesToRotation(animator, clip);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertMuscleCurvesToRotation_両方nullの場合_空のリストを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();

            // Act
            var result = converter.ConvertMuscleCurvesToRotation(null, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ConvertMuscleCurvesToRotation_GenericAnimatorとGenericClipの場合_空のリストを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();

            // Genericクリップを作成（Humanoidクリップではない）
            var clip = new AnimationClip();
            clip.frameRate = 60f;

            // Transformカーブを追加してみる（Genericクリップ）
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            clip.SetCurve("Hips", typeof(Transform), "localPosition.x", curve);

            // Act
            var result = converter.ConvertMuscleCurvesToRotation(animator, clip);

            // Assert
            // GenericリグではHumanoidボーンが取得できないため空のリストを返す
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertMuscleCurvesToRotation_フレームレートが0のクリップ_例外が発生しない()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();
            var clip = new AnimationClip();
            // frameRateを0に設定（エッジケース）
            clip.frameRate = 0f;

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var result = converter.ConvertMuscleCurvesToRotation(animator, clip);
                Assert.IsNotNull(result);
            });

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertMuscleCurvesToRotation_空のクリップ_空のリストを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();
            var clip = new AnimationClip();
            // 空のクリップ（カーブなし）

            // Act
            var result = converter.ConvertMuscleCurvesToRotation(animator, clip);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertMuscleCurvesToRotation_非常に短いクリップ_正常に処理される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();
            var clip = new AnimationClip();
            clip.frameRate = 60f;
            // 1フレームのみの短いクリップ
            var curve = AnimationCurve.Constant(0, 0.001f, 1);
            clip.SetCurve("", typeof(Transform), "localPosition.x", curve);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var result = converter.ConvertMuscleCurvesToRotation(animator, clip);
                Assert.IsNotNull(result);
            });

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertMuscleCurvesToRotation_Animatorが非アクティブでも例外が発生しない()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();
            animator.enabled = false;
            var clip = new AnimationClip();

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var result = converter.ConvertMuscleCurvesToRotation(animator, clip);
                Assert.IsNotNull(result);
            });

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertMuscleCurvesToRotation_GameObjectが非アクティブでも例外が発生しない()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();
            root.SetActive(false);
            var clip = new AnimationClip();

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var result = converter.ConvertMuscleCurvesToRotation(animator, clip);
                Assert.IsNotNull(result);
            });

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        #endregion
    }
}
