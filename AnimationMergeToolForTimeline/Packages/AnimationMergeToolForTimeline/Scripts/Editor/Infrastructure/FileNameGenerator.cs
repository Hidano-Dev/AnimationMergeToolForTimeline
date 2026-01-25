using System;

namespace AnimationMergeTool.Editor.Infrastructure
{
    /// <summary>
    /// ファイルの存在確認を行うインターフェース
    /// </summary>
    public interface IFileExistenceChecker
    {
        /// <summary>
        /// 指定されたパスにファイルが存在するかを確認する
        /// </summary>
        /// <param name="path">確認するファイルパス</param>
        /// <returns>ファイルが存在する場合はtrue</returns>
        bool Exists(string path);
    }

    /// <summary>
    /// マージされたAnimationClipのファイル名を生成するクラス
    /// </summary>
    public class FileNameGenerator
    {
        private readonly IFileExistenceChecker _fileExistenceChecker;

        /// <summary>
        /// デフォルトコンストラクタ（ファイル存在確認なし）
        /// </summary>
        public FileNameGenerator()
        {
            _fileExistenceChecker = null;
        }

        /// <summary>
        /// ファイル存在確認機能付きコンストラクタ
        /// </summary>
        /// <param name="fileExistenceChecker">ファイル存在確認を行うオブジェクト</param>
        public FileNameGenerator(IFileExistenceChecker fileExistenceChecker)
        {
            _fileExistenceChecker = fileExistenceChecker;
        }

        /// <summary>
        /// 基本ファイル名を生成する
        /// 形式: {TimelineAsset名}_{Animator名}_Merged.anim
        /// </summary>
        /// <param name="timelineAssetName">TimelineAssetの名前</param>
        /// <param name="animatorName">Animatorの名前</param>
        /// <returns>生成されたファイル名</returns>
        public string GenerateBaseName(string timelineAssetName, string animatorName)
        {
            // nullまたは空文字の場合は"Unknown"を使用
            var timeline = string.IsNullOrEmpty(timelineAssetName) ? "Unknown" : timelineAssetName;
            var animator = string.IsNullOrEmpty(animatorName) ? "Unknown" : animatorName;

            return $"{timeline}_{animator}_Merged.anim";
        }

        /// <summary>
        /// 重複を回避したファイルパスを生成する
        /// 既存ファイルがある場合は連番を付与する: (1), (2) ...
        /// </summary>
        /// <param name="directory">保存先ディレクトリ</param>
        /// <param name="timelineAssetName">TimelineAssetの名前</param>
        /// <param name="animatorName">Animatorの名前</param>
        /// <returns>重複を回避したファイルパス</returns>
        public string GenerateUniqueFilePath(string directory, string timelineAssetName, string animatorName)
        {
            if (_fileExistenceChecker == null)
            {
                throw new InvalidOperationException("ファイル存在確認機能が設定されていません。IFileExistenceCheckerを指定したコンストラクタを使用してください。");
            }

            var baseName = GenerateBaseName(timelineAssetName, animatorName);
            var nameWithoutExtension = baseName.Substring(0, baseName.Length - 5); // ".anim"を除去
            var extension = ".anim";

            // ディレクトリパスの末尾スラッシュを正規化
            var normalizedDirectory = directory.TrimEnd('/', '\\');

            var filePath = $"{normalizedDirectory}/{baseName}";

            if (!_fileExistenceChecker.Exists(filePath))
            {
                return filePath;
            }

            // 連番を付与して重複を回避
            var counter = 1;
            while (true)
            {
                filePath = $"{normalizedDirectory}/{nameWithoutExtension}({counter}){extension}";
                if (!_fileExistenceChecker.Exists(filePath))
                {
                    return filePath;
                }
                counter++;
            }
        }
    }
}
