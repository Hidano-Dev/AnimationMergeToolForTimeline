namespace AnimationMergeTool.Editor.Infrastructure
{
    /// <summary>
    /// マージされたAnimationClipのファイル名を生成するクラス
    /// </summary>
    public class FileNameGenerator
    {
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
    }
}
