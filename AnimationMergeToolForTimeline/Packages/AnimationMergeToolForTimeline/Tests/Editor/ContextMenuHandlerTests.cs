using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using AnimationMergeTool.Editor.UI;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// ContextMenuHandlerクラスの単体テスト
    /// タスク8.2.2: Hierarchyビューメニュー登録のテスト
    /// </summary>
    public class ContextMenuHandlerTests
    {
        #region Hierarchyビューメニュー テスト

        [Test]
        public void HierarchyMenuPath_正しいパスが設定されている()
        {
            // Assert
            Assert.AreEqual(
                "GameObject/Animation Merge Tool/Merge Timeline Animations",
                ContextMenuHandler.HierarchyMenuPath
            );
        }

        [Test]
        public void CanExecuteFromHierarchy_PlayableDirectorが選択されていない場合falseを返す()
        {
            // Arrange
            UnityEditor.Selection.objects = new Object[0];

            // Act
            var result = ContextMenuHandler.CanExecuteFromHierarchy();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void CanExecuteFromHierarchy_PlayableDirectorが選択されている場合trueを返す()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();

            try
            {
                UnityEditor.Selection.objects = new Object[] { go };

                // Act
                var result = ContextMenuHandler.CanExecuteFromHierarchy();

                // Assert
                Assert.IsTrue(result);
            }
            finally
            {
                UnityEditor.Selection.objects = new Object[0];
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CanExecuteFromHierarchy_PlayableDirectorのないGameObjectが選択されている場合falseを返す()
        {
            // Arrange
            var go = new GameObject("TestObject");

            try
            {
                UnityEditor.Selection.objects = new Object[] { go };

                // Act
                var result = ContextMenuHandler.CanExecuteFromHierarchy();

                // Assert
                Assert.IsFalse(result);
            }
            finally
            {
                UnityEditor.Selection.objects = new Object[0];
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GetSelectedPlayableDirectors_PlayableDirectorを持つGameObjectから取得できる()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();

            try
            {
                UnityEditor.Selection.objects = new Object[] { go };

                // Act
                var directors = ContextMenuHandler.GetSelectedPlayableDirectors();

                // Assert
                Assert.AreEqual(1, directors.Length);
                Assert.AreSame(director, directors[0]);
            }
            finally
            {
                UnityEditor.Selection.objects = new Object[0];
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GetSelectedPlayableDirectors_複数のPlayableDirectorを取得できる()
        {
            // Arrange
            var go1 = new GameObject("TestDirector1");
            var director1 = go1.AddComponent<PlayableDirector>();
            var go2 = new GameObject("TestDirector2");
            var director2 = go2.AddComponent<PlayableDirector>();

            try
            {
                UnityEditor.Selection.objects = new Object[] { go1, go2 };

                // Act
                var directors = ContextMenuHandler.GetSelectedPlayableDirectors();

                // Assert
                Assert.AreEqual(2, directors.Length);
            }
            finally
            {
                UnityEditor.Selection.objects = new Object[0];
                Object.DestroyImmediate(go1);
                Object.DestroyImmediate(go2);
            }
        }

        [Test]
        public void ExecuteForPlayableDirectors_nullの場合falseを返す()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] 処理対象のPlayableDirectorがありません。");

            // Act
            var result = ContextMenuHandler.ExecuteForPlayableDirectors(null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ExecuteForPlayableDirectors_空配列の場合falseを返す()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] 処理対象のPlayableDirectorがありません。");

            // Act
            var result = ContextMenuHandler.ExecuteForPlayableDirectors(new PlayableDirector[0]);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region 複数選択対応 テスト (タスク8.2.4)

        [Test]
        public void GetSelectedTimelineAssets_複数のTimelineAssetを取得できる()
        {
            // Arrange
            var timeline1 = ScriptableObject.CreateInstance<TimelineAsset>();
            var timeline2 = ScriptableObject.CreateInstance<TimelineAsset>();
            var timeline3 = ScriptableObject.CreateInstance<TimelineAsset>();

            try
            {
                UnityEditor.Selection.objects = new Object[] { timeline1, timeline2, timeline3 };

                // Act
                var timelineAssets = ContextMenuHandler.GetSelectedTimelineAssets();

                // Assert
                Assert.AreEqual(3, timelineAssets.Length);
            }
            finally
            {
                UnityEditor.Selection.objects = new Object[0];
                Object.DestroyImmediate(timeline1);
                Object.DestroyImmediate(timeline2);
                Object.DestroyImmediate(timeline3);
            }
        }

        [Test]
        public void GetSelectedTimelineAssets_TimelineAsset以外のオブジェクトは除外される()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var go = new GameObject("TestObject");

            try
            {
                UnityEditor.Selection.objects = new Object[] { timeline, go };

                // Act
                var timelineAssets = ContextMenuHandler.GetSelectedTimelineAssets();

                // Assert
                Assert.AreEqual(1, timelineAssets.Length);
                Assert.AreSame(timeline, timelineAssets[0]);
            }
            finally
            {
                UnityEditor.Selection.objects = new Object[0];
                Object.DestroyImmediate(timeline);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GetSelectedPlayableDirectors_PlayableDirectorのないGameObjectは除外される()
        {
            // Arrange
            var go1 = new GameObject("TestDirector");
            var director = go1.AddComponent<PlayableDirector>();
            var go2 = new GameObject("TestObject");

            try
            {
                UnityEditor.Selection.objects = new Object[] { go1, go2 };

                // Act
                var directors = ContextMenuHandler.GetSelectedPlayableDirectors();

                // Assert
                Assert.AreEqual(1, directors.Length);
                Assert.AreSame(director, directors[0]);
            }
            finally
            {
                UnityEditor.Selection.objects = new Object[0];
                Object.DestroyImmediate(go1);
                Object.DestroyImmediate(go2);
            }
        }

        [Test]
        public void CanExecuteFromProject_複数のTimelineAssetが選択されている場合trueを返す()
        {
            // Arrange
            var timeline1 = ScriptableObject.CreateInstance<TimelineAsset>();
            var timeline2 = ScriptableObject.CreateInstance<TimelineAsset>();

            try
            {
                UnityEditor.Selection.objects = new Object[] { timeline1, timeline2 };

                // Act
                var result = ContextMenuHandler.CanExecuteFromProject();

                // Assert
                Assert.IsTrue(result);
            }
            finally
            {
                UnityEditor.Selection.objects = new Object[0];
                Object.DestroyImmediate(timeline1);
                Object.DestroyImmediate(timeline2);
            }
        }

        [Test]
        public void CanExecuteFromHierarchy_複数のPlayableDirectorが選択されている場合trueを返す()
        {
            // Arrange
            var go1 = new GameObject("TestDirector1");
            var director1 = go1.AddComponent<PlayableDirector>();
            var go2 = new GameObject("TestDirector2");
            var director2 = go2.AddComponent<PlayableDirector>();

            try
            {
                UnityEditor.Selection.objects = new Object[] { go1, go2 };

                // Act
                var result = ContextMenuHandler.CanExecuteFromHierarchy();

                // Assert
                Assert.IsTrue(result);
            }
            finally
            {
                UnityEditor.Selection.objects = new Object[0];
                Object.DestroyImmediate(go1);
                Object.DestroyImmediate(go2);
            }
        }

        [Test]
        public void ExecuteForTimelineAssets_nullの場合falseを返す()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] 処理対象のTimelineAssetがありません。");

            // Act
            var result = ContextMenuHandler.ExecuteForTimelineAssets(null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ExecuteForTimelineAssets_空配列の場合falseを返す()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] 処理対象のTimelineAssetがありません。");

            // Act
            var result = ContextMenuHandler.ExecuteForTimelineAssets(new TimelineAsset[0]);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region FBXエクスポートオプション テスト (P12-008)

        [Test]
        public void HierarchyFbxMenuPath_正しいパスが設定されている()
        {
            // Assert
            Assert.AreEqual(
                "GameObject/Animation Merge Tool/Export as FBX",
                ContextMenuHandler.HierarchyFbxMenuPath
            );
        }

        [Test]
        public void AssetsFbxMenuPath_正しいパスが設定されている()
        {
            // Assert
            Assert.AreEqual(
                "Assets/Animation Merge Tool/Export as FBX",
                ContextMenuHandler.AssetsFbxMenuPath
            );
        }

        [Test]
        public void CanExportFbxFromHierarchy_PlayableDirectorが選択されていない場合falseを返す()
        {
            // Arrange
            UnityEditor.Selection.objects = new Object[0];

            // Act
            var result = ContextMenuHandler.CanExportFbxFromHierarchy();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void CanExportFbxFromHierarchy_PlayableDirectorが選択されている場合trueを返す()
        {
            // Arrange
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();

            try
            {
                UnityEditor.Selection.objects = new Object[] { go };

                // Act
                var result = ContextMenuHandler.CanExportFbxFromHierarchy();

                // Assert
                Assert.IsTrue(result);
            }
            finally
            {
                UnityEditor.Selection.objects = new Object[0];
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CanExportFbxFromProject_TimelineAssetが選択されていない場合falseを返す()
        {
            // Arrange
            UnityEditor.Selection.objects = new Object[0];

            // Act
            var result = ContextMenuHandler.CanExportFbxFromProject();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void CanExportFbxFromProject_TimelineAssetが選択されている場合trueを返す()
        {
            // Arrange
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            try
            {
                UnityEditor.Selection.objects = new Object[] { timeline };

                // Act
                var result = ContextMenuHandler.CanExportFbxFromProject();

                // Assert
                Assert.IsTrue(result);
            }
            finally
            {
                UnityEditor.Selection.objects = new Object[0];
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void ExportFbxForPlayableDirectors_nullの場合falseを返す()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] FBXエクスポート対象のPlayableDirectorがありません。");

            // Act
            var result = ContextMenuHandler.ExportFbxForPlayableDirectors(null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ExportFbxForPlayableDirectors_空配列の場合falseを返す()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] FBXエクスポート対象のPlayableDirectorがありません。");

            // Act
            var result = ContextMenuHandler.ExportFbxForPlayableDirectors(new PlayableDirector[0]);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ExportFbxForTimelineAssets_nullの場合falseを返す()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] FBXエクスポート対象のTimelineAssetがありません。");

            // Act
            var result = ContextMenuHandler.ExportFbxForTimelineAssets(null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ExportFbxForTimelineAssets_空配列の場合falseを返す()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "[AnimationMergeTool] FBXエクスポート対象のTimelineAssetがありません。");

            // Act
            var result = ContextMenuHandler.ExportFbxForTimelineAssets(new TimelineAsset[0]);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion
    }
}
