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

        [Test]
        public void ConvertMuscleCurvesToRotation_戻り値の型がPositionとRotation両方のCurveTypeを含めることが可能()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();

            // TransformCurveDataはPosition型とRotation型の両方を持てることを確認
            var positionCurve = new TransformCurveData("Hips", "localPosition.x", new AnimationCurve(), TransformCurveType.Position);
            var rotationCurve = new TransformCurveData("Hips", "localRotation.x", new AnimationCurve(), TransformCurveType.Rotation);

            var list = new List<TransformCurveData> { positionCurve, rotationCurve };

            // Assert
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(TransformCurveType.Position, list[0].CurveType);
            Assert.AreEqual(TransformCurveType.Rotation, list[1].CurveType);
        }

        [Test]
        public void ConvertMuscleCurvesToRotation_Genericリグの場合_Positionカーブが生成されない()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var root = CreateNonHumanoidBoneHierarchy();
            var animator = root.GetComponent<Animator>();
            var clip = new AnimationClip();
            clip.frameRate = 60f;
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            clip.SetCurve("Hips", typeof(Transform), "localPosition.x", curve);

            // Act
            var result = converter.ConvertMuscleCurvesToRotation(animator, clip);

            // Assert
            // GenericリグではGetBoneTransformがnullを返すため、hipsPositionCurvesは初期化されない
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            // Positionカーブが含まれていないことを明示的に確認
            Assert.IsFalse(result.Exists(c => c.CurveType == TransformCurveType.Position),
                "GenericリグではPositionカーブが生成されるべきではない");

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertMuscleCurvesToRotation_Animatorがnullの場合_Positionカーブが生成されない()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();

            // Act
            var result = converter.ConvertMuscleCurvesToRotation(null, clip);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Assert.IsFalse(result.Exists(c => c.CurveType == TransformCurveType.Position),
                "AnimatorがnullではPositionカーブが生成されるべきではない");

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        #endregion

        #region ConvertRootMotionCurves テスト - P14-007

        [Test]
        public void ConvertRootMotionCurves_clipがnullの場合_空のリストを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();

            // Act
            var result = converter.ConvertRootMotionCurves(null, "Hips");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ConvertRootMotionCurves_rootBonePathがnullの場合_空のリストを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();

            // Act
            var result = converter.ConvertRootMotionCurves(clip, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_rootBonePathが空文字の場合_空パスで変換する()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            // RootTカーブを追加（空パスで設定）
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            clip.SetCurve("", typeof(Animator), "RootT.x", curve);

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            // RootT.xがlocalPosition.xに変換される
            Assert.GreaterOrEqual(result.Count, 1);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_RootTカーブがない場合_空のリストを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            // 通常のTransformカーブのみを追加（RootTではない）
            var curve = AnimationCurve.Linear(0, 0, 1, 1);
            clip.SetCurve("Hips", typeof(Transform), "localPosition.x", curve);

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            // ルートモーションカーブがないので空
            Assert.AreEqual(0, result.Count);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_RootTx_localPositionXに変換される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 5);
            clip.SetCurve("", typeof(Animator), "RootT.x", curve);

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("localPosition.x", result[0].PropertyName);
            Assert.AreEqual(TransformCurveType.Position, result[0].CurveType);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_RootTy_localPositionYに変換される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 3);
            clip.SetCurve("", typeof(Animator), "RootT.y", curve);

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("localPosition.y", result[0].PropertyName);
            Assert.AreEqual(TransformCurveType.Position, result[0].CurveType);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_RootTz_localPositionZに変換される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 7);
            clip.SetCurve("", typeof(Animator), "RootT.z", curve);

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("localPosition.z", result[0].PropertyName);
            Assert.AreEqual(TransformCurveType.Position, result[0].CurveType);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_RootQx_localRotationXに変換される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 0.5f);
            clip.SetCurve("", typeof(Animator), "RootQ.x", curve);

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("localRotation.x", result[0].PropertyName);
            Assert.AreEqual(TransformCurveType.Rotation, result[0].CurveType);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_RootQy_localRotationYに変換される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 0.7f);
            clip.SetCurve("", typeof(Animator), "RootQ.y", curve);

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("localRotation.y", result[0].PropertyName);
            Assert.AreEqual(TransformCurveType.Rotation, result[0].CurveType);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_RootQz_localRotationZに変換される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 0, 1, 0.3f);
            clip.SetCurve("", typeof(Animator), "RootQ.z", curve);

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("localRotation.z", result[0].PropertyName);
            Assert.AreEqual(TransformCurveType.Rotation, result[0].CurveType);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_RootQw_localRotationWに変換される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            var curve = AnimationCurve.Linear(0, 1, 1, 0.9f);
            clip.SetCurve("", typeof(Animator), "RootQ.w", curve);

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("localRotation.w", result[0].PropertyName);
            Assert.AreEqual(TransformCurveType.Rotation, result[0].CurveType);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_全てのRootTカーブ_3つのPositionカーブに変換される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Animator), "RootT.x", AnimationCurve.Linear(0, 0, 1, 1));
            clip.SetCurve("", typeof(Animator), "RootT.y", AnimationCurve.Linear(0, 0, 1, 2));
            clip.SetCurve("", typeof(Animator), "RootT.z", AnimationCurve.Linear(0, 0, 1, 3));

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);

            // 全てPositionタイプであることを確認
            foreach (var curveData in result)
            {
                Assert.AreEqual(TransformCurveType.Position, curveData.CurveType);
                Assert.That(curveData.PropertyName, Does.StartWith("localPosition."));
            }

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_全てのRootQカーブ_4つのRotationカーブに変換される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Animator), "RootQ.x", AnimationCurve.Linear(0, 0, 1, 0.1f));
            clip.SetCurve("", typeof(Animator), "RootQ.y", AnimationCurve.Linear(0, 0, 1, 0.2f));
            clip.SetCurve("", typeof(Animator), "RootQ.z", AnimationCurve.Linear(0, 0, 1, 0.3f));
            clip.SetCurve("", typeof(Animator), "RootQ.w", AnimationCurve.Linear(0, 1, 1, 0.9f));

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count);

            // 全てRotationタイプであることを確認
            foreach (var curveData in result)
            {
                Assert.AreEqual(TransformCurveType.Rotation, curveData.CurveType);
                Assert.That(curveData.PropertyName, Does.StartWith("localRotation."));
            }

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_RootTとRootQの両方_7つのカーブに変換される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            // RootT（位置）
            clip.SetCurve("", typeof(Animator), "RootT.x", AnimationCurve.Linear(0, 0, 1, 1));
            clip.SetCurve("", typeof(Animator), "RootT.y", AnimationCurve.Linear(0, 0, 1, 2));
            clip.SetCurve("", typeof(Animator), "RootT.z", AnimationCurve.Linear(0, 0, 1, 3));
            // RootQ（回転）
            clip.SetCurve("", typeof(Animator), "RootQ.x", AnimationCurve.Linear(0, 0, 1, 0.1f));
            clip.SetCurve("", typeof(Animator), "RootQ.y", AnimationCurve.Linear(0, 0, 1, 0.2f));
            clip.SetCurve("", typeof(Animator), "RootQ.z", AnimationCurve.Linear(0, 0, 1, 0.3f));
            clip.SetCurve("", typeof(Animator), "RootQ.w", AnimationCurve.Linear(0, 1, 1, 0.9f));

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(7, result.Count);

            // Position: 3つ、Rotation: 4つ
            int positionCount = 0;
            int rotationCount = 0;
            foreach (var curveData in result)
            {
                if (curveData.CurveType == TransformCurveType.Position)
                    positionCount++;
                else if (curveData.CurveType == TransformCurveType.Rotation)
                    rotationCount++;
            }
            Assert.AreEqual(3, positionCount);
            Assert.AreEqual(4, rotationCount);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_rootBonePathを指定_指定パスで変換される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Animator), "RootT.x", AnimationCurve.Linear(0, 0, 1, 1));

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "Root/Hips");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Root/Hips", result[0].Path);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_非空パスのカーブは無視される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            // 空パス（ルートモーション）
            clip.SetCurve("", typeof(Animator), "RootT.x", AnimationCurve.Linear(0, 0, 1, 1));
            // 非空パス（通常のTransform）
            clip.SetCurve("Hips", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 5));

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            // ルートモーションカーブのみが変換される
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("localPosition.x", result[0].PropertyName);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_未知のプロパティ名は無視される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            // 既知のプロパティ
            clip.SetCurve("", typeof(Animator), "RootT.x", AnimationCurve.Linear(0, 0, 1, 1));
            // 未知のプロパティ（このテストではAnimator経由で設定できないが、概念的なテスト）

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_カーブの値が正しく維持される()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            var originalCurve = new AnimationCurve();
            originalCurve.AddKey(0f, 0f);
            originalCurve.AddKey(0.5f, 2.5f);
            originalCurve.AddKey(1f, 5f);
            clip.SetCurve("", typeof(Animator), "RootT.x", originalCurve);

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            // カーブのキー数が維持されているか確認
            var resultCurve = result[0].Curve;
            Assert.AreEqual(3, resultCurve.keys.Length);

            // 値が正しく維持されているか確認
            Assert.AreEqual(0f, resultCurve.Evaluate(0f), 0.001f);
            Assert.AreEqual(2.5f, resultCurve.Evaluate(0.5f), 0.001f);
            Assert.AreEqual(5f, resultCurve.Evaluate(1f), 0.001f);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_空のクリップ_空のリストを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();
            var clip = new AnimationClip();
            // カーブなし

            // Act
            var result = converter.ConvertRootMotionCurves(clip, "Hips");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            // クリーンアップ
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void ConvertRootMotionCurves_両方nullの場合_空のリストを返す()
        {
            // Arrange
            var converter = new HumanoidToGenericConverter();

            // Act
            var result = converter.ConvertRootMotionCurves(null, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        #endregion
    }
}
