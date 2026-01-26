using UnityEditor;

namespace AnimationMergeTool.Editor.Infrastructure
{
    /// <summary>
    /// FBX Exporterパッケージの存在確認を行うユーティリティクラス
    /// ERR-003（FBX Exporterパッケージ未インストールエラー）に対応
    /// </summary>
    public static class FbxPackageChecker
    {
        /// <summary>
        /// FBX Exporterパッケージの識別子
        /// </summary>
        public const string FbxExporterPackageId = "com.unity.formats.fbx";

        /// <summary>
        /// パッケージ未インストール時のエラーダイアログタイトル
        /// </summary>
        public const string ErrorTitle = "FBX Exporter Required";

        /// <summary>
        /// パッケージ未インストール時のエラーメッセージ
        /// </summary>
        public const string ErrorMessage =
            "FBX出力機能を使用するには、FBX Exporterパッケージ（com.unity.formats.fbx）の" +
            "インストールが必要です。\n\n" +
            "Package Managerから「FBX Exporter」を検索してインストールしてください。";

        /// <summary>
        /// FBX Exporterパッケージがインストールされているかチェック
        /// コンパイルシンボル UNITY_FORMATS_FBX を使用して判定
        /// </summary>
        /// <returns>パッケージがインストールされている場合はtrue</returns>
        public static bool IsPackageInstalled()
        {
#if UNITY_FORMATS_FBX
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// パッケージの存在をチェックし、未インストールの場合はエラーダイアログを表示
        /// </summary>
        /// <param name="showDialog">ダイアログを表示するかどうか（テスト用にfalseを指定可能）</param>
        /// <returns>パッケージがインストールされている場合はtrue、未インストールの場合はfalse</returns>
        public static bool CheckPackageAndShowDialogIfMissing(bool showDialog = true)
        {
            if (IsPackageInstalled())
            {
                return true;
            }

            if (showDialog)
            {
                ShowPackageNotInstalledError();
            }

            return false;
        }

        /// <summary>
        /// FBX Exporterパッケージ未インストール時のエラーダイアログを表示
        /// </summary>
        public static void ShowPackageNotInstalledError()
        {
            EditorUtility.DisplayDialog(ErrorTitle, ErrorMessage, "OK");
        }
    }
}
