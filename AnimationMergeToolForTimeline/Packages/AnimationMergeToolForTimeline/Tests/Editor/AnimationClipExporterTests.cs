using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using AnimationMergeTool.Editor.Domain;
using AnimationMergeTool.Editor.Domain.Models;
using AnimationMergeTool.Editor.Infrastructure;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// AnimationClipExporterクラスの単体テスト
    /// タスク7.2.2: AnimationClip作成機能のテスト
    /// </summary>
    public class AnimationClipExporterTests
    {
        private AnimationClipExporter _exporter;
        private FileNameGenerator _fileNameGenerator;

        [SetUp]
        public void SetUp()
        {
            _fileNameGenerator = new FileNameGenerator();
            _exporter = new AnimationClipExporter(_fileNameGenerator);
        }

        #region CreateAnimationClip（MergeResult付き）テスト

        [Test]
        public void CreateAnimationClip_有効なカーブデータからAnimationClipを生成できる()
        {
            // Arrange
            var mergeResult = new MergeResult(null);
            var curveBindingPairs = CreateTestCurveBindingPairs();

            // Act
            var clip = _exporter.CreateAnimationClip(mergeResult, curveBindingPairs, 60f);

            // Assert
            Assert.IsNotNull(clip);
            Assert.AreEqual(60f, clip.frameRate);
            Assert.IsTrue(mergeResult.IsSuccess);
            Assert.AreSame(clip, mergeResult.GeneratedClip);
        }

        [Test]
        public void CreateAnimationClip_MergeResultがnullの場合nullを返す()
        {
            // Arrange
            var curveBindingPairs = CreateTestCurveBindingPairs();
            LogAssert.Expect(LogType.Error, "MergeResultがnullです。");

            // Act
            var clip = _exporter.CreateAnimationClip(null, curveBindingPairs, 60f);

            // Assert
            Assert.IsNull(clip);
        }

        [Test]
        public void CreateAnimationClip_カーブリストがnullの場合nullを返しエラーログを追加する()
        {
            // Arrange
            var mergeResult = new MergeResult(null);

            // Act
            var clip = _exporter.CreateAnimationClip(mergeResult, null, 60f);

            // Assert
            Assert.IsNull(clip);
            Assert.IsFalse(mergeResult.IsSuccess);
            Assert.IsTrue(mergeResult.Logs.Count > 0);
            Assert.IsTrue(mergeResult.Logs[0].Contains("[Error]"));
        }

        [Test]
        public void CreateAnimationClip_カーブリストが空の場合nullを返しエラーログを追加する()
        {
            // Arrange
            var mergeResult = new MergeResult(null);
            var curveBindingPairs = new List<CurveBindingPair>();

            // Act
            var clip = _exporter.CreateAnimationClip(mergeResult, curveBindingPairs, 60f);

            // Assert
            Assert.IsNull(clip);
            Assert.IsFalse(mergeResult.IsSuccess);
            Assert.IsTrue(mergeResult.Logs.Count > 0);
        }

        [Test]
        public void CreateAnimationClip_フレームレートが0以下の場合デフォルト60fpsを使用する()
        {
            // Arrange
            var mergeResult = new MergeResult(null);
            var curveBindingPairs = CreateTestCurveBindingPairs();

            // Act
            var clip = _exporter.CreateAnimationClip(mergeResult, curveBindingPairs, 0f);

            // Assert
            Assert.IsNotNull(clip);
            Assert.AreEqual(60f, clip.frameRate);
        }

        [Test]
        public void CreateAnimationClip_負のフレームレートの場合デフォルト60fpsを使用する()
        {
            // Arrange
            var mergeResult = new MergeResult(null);
            var curveBindingPairs = CreateTestCurveBindingPairs();

            // Act
            var clip = _exporter.CreateAnimationClip(mergeResult, curveBindingPairs, -30f);

            // Assert
            Assert.IsNotNull(clip);
            Assert.AreEqual(60f, clip.frameRate);
        }

        [Test]
        public void CreateAnimationClip_指定したフレームレートが設定される()
        {
            // Arrange
            var mergeResult = new MergeResult(null);
            var curveBindingPairs = CreateTestCurveBindingPairs();

            // Act
            var clip = _exporter.CreateAnimationClip(mergeResult, curveBindingPairs, 30f);

            // Assert
            Assert.IsNotNull(clip);
            Assert.AreEqual(30f, clip.frameRate);
        }

        [Test]
        public void CreateAnimationClip_複数のカーブを正しく設定できる()
        {
            // Arrange
            var mergeResult = new MergeResult(null);
            var curveBindingPairs = new List<CurveBindingPair>
            {
                CreateCurveBindingPair("", typeof(Transform), "localPosition.x"),
                CreateCurveBindingPair("", typeof(Transform), "localPosition.y"),
                CreateCurveBindingPair("", typeof(Transform), "localPosition.z")
            };

            // Act
            var clip = _exporter.CreateAnimationClip(mergeResult, curveBindingPairs, 60f);

            // Assert
            Assert.IsNotNull(clip);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            Assert.AreEqual(3, bindings.Length);
        }

        [Test]
        public void CreateAnimationClip_nullカーブをスキップする()
        {
            // Arrange
            var mergeResult = new MergeResult(null);
            var curveBindingPairs = new List<CurveBindingPair>
            {
                CreateCurveBindingPair("", typeof(Transform), "localPosition.x"),
                new CurveBindingPair(
                    new EditorCurveBinding { path = "", type = typeof(Transform), propertyName = "localPosition.y" },
                    null),
                CreateCurveBindingPair("", typeof(Transform), "localPosition.z")
            };

            // Act
            var clip = _exporter.CreateAnimationClip(mergeResult, curveBindingPairs, 60f);

            // Assert
            Assert.IsNotNull(clip);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            Assert.AreEqual(2, bindings.Length);
        }

        [Test]
        public void CreateAnimationClip_空のカーブをスキップする()
        {
            // Arrange
            var mergeResult = new MergeResult(null);
            var curveBindingPairs = new List<CurveBindingPair>
            {
                CreateCurveBindingPair("", typeof(Transform), "localPosition.x"),
                new CurveBindingPair(
                    new EditorCurveBinding { path = "", type = typeof(Transform), propertyName = "localPosition.y" },
                    new AnimationCurve()), // キーがないカーブ
                CreateCurveBindingPair("", typeof(Transform), "localPosition.z")
            };

            // Act
            var clip = _exporter.CreateAnimationClip(mergeResult, curveBindingPairs, 60f);

            // Assert
            Assert.IsNotNull(clip);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            Assert.AreEqual(2, bindings.Length);
        }

        [Test]
        public void CreateAnimationClip_全てのカーブが無効な場合nullを返す()
        {
            // Arrange
            var mergeResult = new MergeResult(null);
            var curveBindingPairs = new List<CurveBindingPair>
            {
                new CurveBindingPair(
                    new EditorCurveBinding { path = "", type = typeof(Transform), propertyName = "localPosition.x" },
                    null),
                new CurveBindingPair(
                    new EditorCurveBinding { path = "", type = typeof(Transform), propertyName = "localPosition.y" },
                    new AnimationCurve())
            };

            // Act
            var clip = _exporter.CreateAnimationClip(mergeResult, curveBindingPairs, 60f);

            // Assert
            Assert.IsNull(clip);
            Assert.IsFalse(mergeResult.IsSuccess);
        }

        [Test]
        public void CreateAnimationClip_ログにカーブ数とフレームレートが記録される()
        {
            // Arrange
            var mergeResult = new MergeResult(null);
            var curveBindingPairs = CreateTestCurveBindingPairs();

            // Act
            _exporter.CreateAnimationClip(mergeResult, curveBindingPairs, 60f);

            // Assert
            Assert.IsTrue(mergeResult.Logs.Count > 0);
            var log = mergeResult.Logs[mergeResult.Logs.Count - 1];
            Assert.IsTrue(log.Contains("カーブ数"));
            Assert.IsTrue(log.Contains("フレームレート"));
        }

        #endregion

        #region CreateAnimationClip（MergeResult不要版）テスト

        [Test]
        public void CreateAnimationClip_MergeResult不要版_有効なカーブデータからAnimationClipを生成できる()
        {
            // Arrange
            var curveBindingPairs = CreateTestCurveBindingPairs();

            // Act
            var clip = _exporter.CreateAnimationClip(curveBindingPairs, 60f);

            // Assert
            Assert.IsNotNull(clip);
            Assert.AreEqual(60f, clip.frameRate);
        }

        [Test]
        public void CreateAnimationClip_MergeResult不要版_カーブリストがnullの場合nullを返す()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "カーブデータが空のためAnimationClipを生成できません。");

            // Act
            var clip = _exporter.CreateAnimationClip((List<CurveBindingPair>)null, 60f);

            // Assert
            Assert.IsNull(clip);
        }

        [Test]
        public void CreateAnimationClip_MergeResult不要版_カーブリストが空の場合nullを返す()
        {
            // Arrange
            var curveBindingPairs = new List<CurveBindingPair>();
            LogAssert.Expect(LogType.Error, "カーブデータが空のためAnimationClipを生成できません。");

            // Act
            var clip = _exporter.CreateAnimationClip(curveBindingPairs, 60f);

            // Assert
            Assert.IsNull(clip);
        }

        [Test]
        public void CreateAnimationClip_MergeResult不要版_フレームレートが0以下の場合デフォルト60fpsを使用する()
        {
            // Arrange
            var curveBindingPairs = CreateTestCurveBindingPairs();

            // Act
            var clip = _exporter.CreateAnimationClip(curveBindingPairs, 0f);

            // Assert
            Assert.IsNotNull(clip);
            Assert.AreEqual(60f, clip.frameRate);
        }

        [Test]
        public void CreateAnimationClip_MergeResult不要版_指定したフレームレートが設定される()
        {
            // Arrange
            var curveBindingPairs = CreateTestCurveBindingPairs();

            // Act
            var clip = _exporter.CreateAnimationClip(curveBindingPairs, 24f);

            // Assert
            Assert.IsNotNull(clip);
            Assert.AreEqual(24f, clip.frameRate);
        }

        #endregion

        #region SaveAsAsset テスト

        [Test]
        public void SaveAsAsset_有効なAnimationClipをAssets直下に保存できる()
        {
            // Arrange
            var curveBindingPairs = CreateTestCurveBindingPairs();
            var clip = _exporter.CreateAnimationClip(curveBindingPairs, 60f);
            var timelineAssetName = "TestTimeline_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            var animatorName = "TestAnimator";

            // Act
            var savedPath = _exporter.SaveAsAsset(clip, timelineAssetName, animatorName, "Assets");

            // Assert
            try
            {
                Assert.IsNotNull(savedPath);
                Assert.IsTrue(savedPath.StartsWith("Assets/"));
                Assert.IsTrue(savedPath.EndsWith(".anim"));

                // アセットが実際に存在するか確認
                var loadedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(savedPath);
                Assert.IsNotNull(loadedClip);
            }
            finally
            {
                // クリーンアップ
                if (!string.IsNullOrEmpty(savedPath) && AssetDatabase.LoadAssetAtPath<AnimationClip>(savedPath) != null)
                {
                    AssetDatabase.DeleteAsset(savedPath);
                }
            }
        }

        [Test]
        public void SaveAsAsset_clipがnullの場合nullを返す()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "保存するAnimationClipがnullです。");

            // Act
            var savedPath = _exporter.SaveAsAsset(null, "TestTimeline", "TestAnimator", "Assets");

            // Assert
            Assert.IsNull(savedPath);
        }

        [Test]
        public void SaveAsAsset_存在しないディレクトリの場合nullを返す()
        {
            // Arrange
            var curveBindingPairs = CreateTestCurveBindingPairs();
            var clip = _exporter.CreateAnimationClip(curveBindingPairs, 60f);
            LogAssert.Expect(LogType.Error, "保存先ディレクトリが存在しません: Assets/NonExistentFolder12345");

            // Act
            var savedPath = _exporter.SaveAsAsset(clip, "TestTimeline", "TestAnimator", "Assets/NonExistentFolder12345");

            // Assert
            Assert.IsNull(savedPath);
        }

        [Test]
        public void SaveAsAsset_ディレクトリが空文字の場合Assetsに保存される()
        {
            // Arrange
            var curveBindingPairs = CreateTestCurveBindingPairs();
            var clip = _exporter.CreateAnimationClip(curveBindingPairs, 60f);
            var timelineAssetName = "TestTimeline_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            var animatorName = "TestAnimator";

            // Act
            var savedPath = _exporter.SaveAsAsset(clip, timelineAssetName, animatorName, "");

            // Assert
            try
            {
                Assert.IsNotNull(savedPath);
                Assert.IsTrue(savedPath.StartsWith("Assets/"));
            }
            finally
            {
                // クリーンアップ
                if (!string.IsNullOrEmpty(savedPath) && AssetDatabase.LoadAssetAtPath<AnimationClip>(savedPath) != null)
                {
                    AssetDatabase.DeleteAsset(savedPath);
                }
            }
        }

        [Test]
        public void SaveAsAsset_ファイル名が正しい形式で生成される()
        {
            // Arrange
            var curveBindingPairs = CreateTestCurveBindingPairs();
            var clip = _exporter.CreateAnimationClip(curveBindingPairs, 60f);
            var timelineAssetName = "MyTimeline";
            var animatorName = "MyAnimator";

            // Act
            var savedPath = _exporter.SaveAsAsset(clip, timelineAssetName, animatorName, "Assets");

            // Assert
            try
            {
                Assert.IsNotNull(savedPath);
                Assert.IsTrue(savedPath.Contains("MyTimeline_MyAnimator_Merged"));
            }
            finally
            {
                // クリーンアップ
                if (!string.IsNullOrEmpty(savedPath) && AssetDatabase.LoadAssetAtPath<AnimationClip>(savedPath) != null)
                {
                    AssetDatabase.DeleteAsset(savedPath);
                }
            }
        }

        #endregion

        #region ExportToAsset テスト

        [Test]
        public void ExportToAsset_MergeResultとカーブからアセットを生成して保存できる()
        {
            // Arrange
            var mergeResult = new MergeResult(null);
            var curveBindingPairs = CreateTestCurveBindingPairs();
            var timelineAssetName = "TestTimeline_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            var animatorName = "TestAnimator";

            // Act
            var savedPath = _exporter.ExportToAsset(mergeResult, curveBindingPairs, timelineAssetName, animatorName, 60f, "Assets");

            // Assert
            try
            {
                Assert.IsNotNull(savedPath);
                Assert.IsTrue(mergeResult.IsSuccess);
                Assert.IsTrue(mergeResult.Logs.Exists(log => log.Contains("アセットを保存しました")));
            }
            finally
            {
                // クリーンアップ
                if (!string.IsNullOrEmpty(savedPath) && AssetDatabase.LoadAssetAtPath<AnimationClip>(savedPath) != null)
                {
                    AssetDatabase.DeleteAsset(savedPath);
                }
            }
        }

        [Test]
        public void ExportToAsset_カーブが空の場合nullを返す()
        {
            // Arrange
            var mergeResult = new MergeResult(null);
            var curveBindingPairs = new List<CurveBindingPair>();

            // Act
            var savedPath = _exporter.ExportToAsset(mergeResult, curveBindingPairs, "TestTimeline", "TestAnimator", 60f, "Assets");

            // Assert
            Assert.IsNull(savedPath);
            Assert.IsFalse(mergeResult.IsSuccess);
        }

        [Test]
        public void ExportToAsset_存在しないディレクトリの場合エラーログを追加する()
        {
            // Arrange
            var mergeResult = new MergeResult(null);
            var curveBindingPairs = CreateTestCurveBindingPairs();
            LogAssert.Expect(LogType.Error, "保存先ディレクトリが存在しません: Assets/NonExistentFolder12345");

            // Act
            var savedPath = _exporter.ExportToAsset(mergeResult, curveBindingPairs, "TestTimeline", "TestAnimator", 60f, "Assets/NonExistentFolder12345");

            // Assert
            Assert.IsNull(savedPath);
            Assert.IsFalse(mergeResult.IsSuccess);
            Assert.IsTrue(mergeResult.Logs.Exists(log => log.Contains("[Error]")));
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// テスト用のカーブバインディングペアリストを作成する
        /// </summary>
        private List<CurveBindingPair> CreateTestCurveBindingPairs()
        {
            return new List<CurveBindingPair>
            {
                CreateCurveBindingPair("", typeof(Transform), "localPosition.x")
            };
        }

        /// <summary>
        /// テスト用のカーブバインディングペアを作成する
        /// </summary>
        private CurveBindingPair CreateCurveBindingPair(string path, System.Type type, string propertyName)
        {
            var binding = new EditorCurveBinding
            {
                path = path,
                type = type,
                propertyName = propertyName
            };

            var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            return new CurveBindingPair(binding, curve);
        }

        #endregion
    }
}
