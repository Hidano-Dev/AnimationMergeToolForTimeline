using UnityEditor;
using UnityEngine;

namespace AnimationMergeTool.Editor.Infrastructure
{
    /// <summary>
    /// AssetDatabaseを使用したファイル存在チェッカー
    /// IFileExistenceCheckerの実装
    /// </summary>
    public class AssetDatabaseFileExistenceChecker : IFileExistenceChecker
    {
        /// <summary>
        /// 指定されたパスにアセットが存在するかを確認する
        /// </summary>
        /// <param name="path">確認するアセットパス</param>
        /// <returns>アセットが存在する場合はtrue</returns>
        public bool Exists(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Object>(path) != null;
        }
    }
}
