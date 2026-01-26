using System.Collections.Generic;
using UnityEngine;
using AnimationMergeTool.Editor.Domain.Models;

namespace AnimationMergeTool.Editor.Infrastructure
{
    /// <summary>
    /// FBXアニメーションエクスポーターの基本クラス
    /// FBX Exporter API（com.unity.formats.fbx）のラッパーとして機能する
    /// </summary>
    public class FbxAnimationExporter
    {
        /// <summary>
        /// FBXエクスポート機能が利用可能かどうかを確認する
        /// </summary>
        /// <returns>FBX Exporterパッケージがインストールされている場合はtrue</returns>
        public bool IsAvailable()
        {
            return FbxPackageChecker.IsPackageInstalled();
        }

        /// <summary>
        /// 指定されたエクスポートデータがエクスポート可能かどうかを確認する
        /// </summary>
        /// <param name="exportData">エクスポートデータ</param>
        /// <returns>エクスポート可能な場合はtrue</returns>
        public bool CanExport(FbxExportData exportData)
        {
            if (exportData == null)
            {
                return false;
            }

            return exportData.HasExportableData;
        }

        /// <summary>
        /// FBXファイルにエクスポートする
        /// </summary>
        /// <param name="exportData">エクスポートデータ</param>
        /// <param name="outputPath">出力先パス</param>
        /// <returns>エクスポートが成功した場合はtrue</returns>
        public bool Export(FbxExportData exportData, string outputPath)
        {
            // 入力検証
            if (exportData == null)
            {
                Debug.LogError("FbxExportDataがnullです。");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("出力パスが指定されていません。");
                return false;
            }

            if (!exportData.HasExportableData)
            {
                Debug.LogError("エクスポート可能なデータがありません。");
                return false;
            }

            // パッケージがインストールされているか確認
            if (!IsAvailable())
            {
                Debug.LogError("FBX Exporterパッケージがインストールされていません。");
                return false;
            }

            // P12-007で実装予定: 実際のFBXエクスポート処理
            return ExportInternal(exportData, outputPath);
        }

        /// <summary>
        /// エクスポートデータの検証を行う
        /// </summary>
        /// <param name="exportData">検証対象のエクスポートデータ</param>
        /// <returns>エラーメッセージ（問題がない場合はnull）</returns>
        public string ValidateExportData(FbxExportData exportData)
        {
            if (exportData == null)
            {
                return "FbxExportDataがnullです。";
            }

            if (!exportData.HasExportableData)
            {
                return "エクスポート可能なデータがありません。";
            }

            return null;
        }

        /// <summary>
        /// サポートされているエクスポートオプションを取得する
        /// </summary>
        /// <returns>エクスポートオプションのリスト</returns>
        public IReadOnlyList<string> GetSupportedExportOptions()
        {
            return new List<string>
            {
                "AnimationOnly",
                "WithSkeleton",
                "WithBlendShapes"
            };
        }

        /// <summary>
        /// 内部エクスポート処理
        /// P12-007で実装予定
        /// </summary>
        private bool ExportInternal(FbxExportData exportData, string outputPath)
        {
#if UNITY_FORMATS_FBX
            // FBX Exporterパッケージがインストールされている場合の処理
            // P12-007で実装予定
            try
            {
                // TODO: 実際のFBXエクスポート処理を実装
                Debug.Log($"FBXエクスポート開始: {outputPath}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"FBXエクスポートに失敗しました: {ex.Message}");
                return false;
            }
#else
            Debug.LogError("FBX Exporterパッケージがインストールされていません。");
            return false;
#endif
        }
    }
}
