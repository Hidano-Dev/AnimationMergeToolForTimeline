using AnimationMergeTool.Editor.Domain.Models;

namespace AnimationMergeTool.Editor.Infrastructure
{
    /// <summary>
    /// AnimationClipをアセットとしてエクスポートするクラス
    /// </summary>
    public class AnimationClipExporter
    {
        private readonly FileNameGenerator _fileNameGenerator;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="fileNameGenerator">ファイル名生成器</param>
        public AnimationClipExporter(FileNameGenerator fileNameGenerator)
        {
            _fileNameGenerator = fileNameGenerator;
        }
    }
}
