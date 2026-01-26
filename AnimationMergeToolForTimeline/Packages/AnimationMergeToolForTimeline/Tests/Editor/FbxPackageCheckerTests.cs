using NUnit.Framework;
using AnimationMergeTool.Editor.Infrastructure;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// FbxPackageCheckerクラスの単体テスト
    /// FBX Exporterパッケージの存在チェック機能をテストする
    /// </summary>
    public class FbxPackageCheckerTests
    {
        #region 定数テスト

        [Test]
        public void FbxExporterPackageId_正しいパッケージIDが定義されている()
        {
            // Assert
            Assert.AreEqual("com.unity.formats.fbx", FbxPackageChecker.FbxExporterPackageId);
        }

        [Test]
        public void ErrorTitle_正しいエラータイトルが定義されている()
        {
            // Assert
            Assert.AreEqual("FBX Exporter Required", FbxPackageChecker.ErrorTitle);
        }

        [Test]
        public void ErrorMessage_正しいエラーメッセージが定義されている()
        {
            // Assert
            Assert.IsNotNull(FbxPackageChecker.ErrorMessage);
            Assert.IsTrue(FbxPackageChecker.ErrorMessage.Contains("com.unity.formats.fbx"));
            Assert.IsTrue(FbxPackageChecker.ErrorMessage.Contains("FBX Exporter"));
        }

        #endregion

        #region IsPackageInstalled テスト

        [Test]
        public void IsPackageInstalled_戻り値がbool型である()
        {
            // Act
            var result = FbxPackageChecker.IsPackageInstalled();

            // Assert
            Assert.IsInstanceOf<bool>(result);
        }

        [Test]
        public void IsPackageInstalled_コンパイルシンボルに基づいた結果を返す()
        {
            // Act
            var result = FbxPackageChecker.IsPackageInstalled();

            // Assert
            // UNITY_FORMATS_FBXシンボルが定義されているかどうかに応じて結果が変わる
            // このテストは、メソッドが正常に動作することを確認する
#if UNITY_FORMATS_FBX
            Assert.IsTrue(result, "UNITY_FORMATS_FBXシンボルが定義されている場合はtrueを返すべき");
#else
            Assert.IsFalse(result, "UNITY_FORMATS_FBXシンボルが定義されていない場合はfalseを返すべき");
#endif
        }

        #endregion

        #region CheckPackageAndShowDialogIfMissing テスト

        [Test]
        public void CheckPackageAndShowDialogIfMissing_パッケージがインストール済みの場合trueを返す()
        {
            // このテストはパッケージがインストールされている環境でのみ意味がある
#if UNITY_FORMATS_FBX
            // Act
            // 注意: showDialogをfalseにしてダイアログを表示しない
            var result = FbxPackageChecker.CheckPackageAndShowDialogIfMissing(showDialog: false);

            // Assert
            Assert.IsTrue(result);
#else
            // パッケージがインストールされていない場合はスキップ
            Assert.Ignore("FBX Exporterパッケージがインストールされていないため、このテストはスキップされます");
#endif
        }

        [Test]
        public void CheckPackageAndShowDialogIfMissing_パッケージが未インストールの場合falseを返す()
        {
            // このテストはパッケージがインストールされていない環境でのみ意味がある
#if !UNITY_FORMATS_FBX
            // Act
            // 注意: showDialogをfalseにしてダイアログを表示しない
            var result = FbxPackageChecker.CheckPackageAndShowDialogIfMissing(showDialog: false);

            // Assert
            Assert.IsFalse(result);
#else
            // パッケージがインストールされている場合はスキップ
            Assert.Ignore("FBX Exporterパッケージがインストールされているため、このテストはスキップされます");
#endif
        }

        #endregion

        #region エラーメッセージ内容テスト

        [Test]
        public void ErrorMessage_Package_Managerへの案内が含まれている()
        {
            // Assert
            Assert.IsTrue(FbxPackageChecker.ErrorMessage.Contains("Package Manager"));
        }

        [Test]
        public void ErrorMessage_日本語で記述されている()
        {
            // Assert
            // エラーメッセージには日本語が含まれていることを確認
            Assert.IsTrue(FbxPackageChecker.ErrorMessage.Contains("インストール") ||
                          FbxPackageChecker.ErrorMessage.Contains("必要"));
        }

        #endregion
    }
}
