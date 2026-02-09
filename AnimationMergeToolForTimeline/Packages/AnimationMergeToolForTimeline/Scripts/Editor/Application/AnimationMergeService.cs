using System.Collections.Generic;
using System.Linq;
using AnimationMergeTool.Editor.Domain;
using AnimationMergeTool.Editor.Domain.Models;
using AnimationMergeTool.Editor.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AnimationMergeTool.Editor.Application
{
    /// <summary>
    /// アニメーション結合処理を統合するサービスクラス
    /// TrackAnalyzer → ClipMerger → ExtrapolationProcessor → BlendProcessor → CurveOverrider → Exporter
    /// の流れで処理をオーケストレーションする
    /// AnimationClip (.anim) とFBX両方の出力形式をサポート
    /// </summary>
    public class AnimationMergeService
    {
        private readonly FileNameGenerator _fileNameGenerator;
        private readonly AnimationClipExporter _exporter;
        private readonly FbxAnimationExporter _fbxExporter;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AnimationMergeService()
        {
            _fileNameGenerator = new FileNameGenerator();
            _fileNameGenerator.SetFileExistenceChecker(new AssetDatabaseFileExistenceChecker());
            _exporter = new AnimationClipExporter(_fileNameGenerator);
            _fbxExporter = new FbxAnimationExporter();
        }

        /// <summary>
        /// コンストラクタ（依存性注入用）
        /// </summary>
        /// <param name="fileNameGenerator">ファイル名生成器</param>
        /// <param name="exporter">エクスポーター</param>
        public AnimationMergeService(FileNameGenerator fileNameGenerator, AnimationClipExporter exporter)
        {
            _fileNameGenerator = fileNameGenerator;
            _exporter = exporter;
            _fbxExporter = new FbxAnimationExporter();
        }

        /// <summary>
        /// コンストラクタ（完全な依存性注入用）
        /// </summary>
        /// <param name="fileNameGenerator">ファイル名生成器</param>
        /// <param name="exporter">AnimationClipエクスポーター</param>
        /// <param name="fbxExporter">FBXエクスポーター</param>
        public AnimationMergeService(FileNameGenerator fileNameGenerator, AnimationClipExporter exporter, FbxAnimationExporter fbxExporter)
        {
            _fileNameGenerator = fileNameGenerator;
            _exporter = exporter;
            _fbxExporter = fbxExporter ?? new FbxAnimationExporter();
        }

        /// <summary>
        /// PlayableDirectorからアニメーションを結合する
        /// </summary>
        /// <param name="director">対象のPlayableDirector</param>
        /// <param name="outputDirectory">出力ディレクトリ（デフォルト: "Assets"）</param>
        /// <returns>結合結果のリスト（Animator単位）</returns>
        public List<MergeResult> MergeFromPlayableDirector(PlayableDirector director, string outputDirectory = "Assets")
        {
            var results = new List<MergeResult>();

            if (director == null)
            {
                Debug.LogError("[AnimationMergeTool] PlayableDirectorがnullです。");
                return results;
            }

            var timelineAsset = director.playableAsset as TimelineAsset;
            if (timelineAsset == null)
            {
                Debug.LogError("[AnimationMergeTool] PlayableDirectorにTimelineAssetが設定されていません。");
                return results;
            }

            // PlayableDirectorからバインディング情報を取得
            var animatorBindings = GetAnimatorBindings(director, timelineAsset);
            if (animatorBindings.Count == 0)
            {
                Debug.LogError("[AnimationMergeTool] バインドされたAnimatorが見つかりません。");
                return results;
            }

            // Animator単位で処理
            foreach (var kvp in animatorBindings)
            {
                var animator = kvp.Key;
                var tracks = kvp.Value;

                var result = MergeTracksForAnimator(timelineAsset, animator, tracks, outputDirectory);
                if (result != null)
                {
                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// TimelineAssetからアニメーションを結合する
        /// 注意: この方法ではAnimatorバインディング情報が取得できないため、
        /// PlayableDirectorからの処理を推奨
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        /// <param name="outputDirectory">出力ディレクトリ（デフォルト: "Assets"）</param>
        /// <returns>結合結果のリスト（Animator単位）</returns>
        public List<MergeResult> MergeFromTimelineAsset(TimelineAsset timelineAsset, string outputDirectory = "Assets")
        {
            var results = new List<MergeResult>();

            if (timelineAsset == null)
            {
                Debug.LogError("[AnimationMergeTool] TimelineAssetがnullです。");
                return results;
            }

            // TimelineAssetのみからはAnimatorバインディングが取得できないため、
            // 全トラックを単一のAnimationClipとして出力（Animatorはnull）
            var analyzer = new TrackAnalyzer(timelineAsset);
            var allTracks = analyzer.GetAnimationTracksWithPriority();
            var nonMutedTracks = analyzer.FilterNonMutedTracks(allTracks);

            if (nonMutedTracks.Count == 0)
            {
                Debug.LogError("[AnimationMergeTool] 有効なAnimationTrackが見つかりません。");
                return results;
            }

            var result = MergeTracksForAnimator(timelineAsset, null, nonMutedTracks, outputDirectory);
            if (result != null)
            {
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// PlayableDirectorからAnimator単位でバインディング情報を取得する
        /// </summary>
        private Dictionary<Animator, List<TrackInfo>> GetAnimatorBindings(
            PlayableDirector director,
            TimelineAsset timelineAsset)
        {
            var bindings = new Dictionary<Animator, List<TrackInfo>>();
            var analyzer = new TrackAnalyzer(timelineAsset);
            var tracksWithPriority = analyzer.GetAnimationTracksWithPriority();

            foreach (var trackInfo in tracksWithPriority)
            {
                // Muteされたトラックはスキップ（track.mutedプロパティを直接チェック）
                if (trackInfo.Track != null && trackInfo.Track.muted)
                {
                    continue;
                }

                // バインディング取得対象のトラックを決定
                // OverrideTrack（親がAnimationTrack）の場合は親トラックからバインディングを取得
                var bindingSourceTrack = GetBindingSourceTrack(trackInfo.Track);

                // PlayableDirectorからトラックにバインドされているAnimatorを取得
                var binding = director.GetGenericBinding(bindingSourceTrack);
                var animator = binding as Animator;

                if (animator == null)
                {
                    Debug.LogError($"[AnimationMergeTool] トラック \"{trackInfo.Track.name}\" にAnimatorがバインドされていません。");
                    continue;
                }

                trackInfo.BoundAnimator = animator;

                if (!bindings.ContainsKey(animator))
                {
                    bindings[animator] = new List<TrackInfo>();
                }

                bindings[animator].Add(trackInfo);
            }

            return bindings;
        }

        /// <summary>
        /// バインディング取得元のトラックを取得する
        /// OverrideTrack（親がAnimationTrack）の場合はルートの親AnimationTrackを返す
        /// </summary>
        /// <param name="track">対象のトラック</param>
        /// <returns>バインディング取得元のトラック</returns>
        private TrackAsset GetBindingSourceTrack(TrackAsset track)
        {
            if (track == null)
            {
                return null;
            }

            // 親トラックを辿ってルートのAnimationTrackを探す
            var current = track;
            while (current.parent is AnimationTrack parentAnimationTrack)
            {
                current = parentAnimationTrack;
            }

            return current;
        }

        /// <summary>
        /// 指定されたAnimator用のトラックを結合する
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="tracks">トラックリスト</param>
        /// <param name="outputDirectory">出力ディレクトリ</param>
        /// <param name="saveToAsset">trueの場合.animファイルとして保存、falseの場合メモリ上にのみ生成</param>
        private MergeResult MergeTracksForAnimator(
            TimelineAsset timelineAsset,
            Animator animator,
            List<TrackInfo> tracks,
            string outputDirectory,
            bool saveToAsset = true)
        {
            var result = new MergeResult(animator);

            if (tracks == null || tracks.Count == 0)
            {
                result.AddErrorLog("処理対象のトラックがありません。");
                return result;
            }

            result.AddLog($"処理開始: {tracks.Count}個のトラック");

            // フレームレートを取得
            var frameRate = ClipMerger.GetFrameRateFromTimeline(timelineAsset);
            if (frameRate <= 0)
            {
                frameRate = 60f;
            }

            // プロセッサを初期化
            var clipMerger = new ClipMerger();
            clipMerger.SetFrameRate(frameRate);

            var extrapolationProcessor = new ExtrapolationProcessor();
            extrapolationProcessor.SetFrameRate(frameRate);

            var blendProcessor = new BlendProcessor();
            blendProcessor.SetFrameRate(frameRate);

            var curveOverrider = new CurveOverrider();

            // 優先順位順にソート（低い順 = 上の段から下の段）
            var sortedTracks = tracks.OrderBy(t => t.Priority).ToList();

            // 各トラックからカーブデータを収集
            var allCurveData = new Dictionary<string, List<CurveWithTimeRangeAndPriority>>();

            foreach (var trackInfo in sortedTracks)
            {
                var clipInfos = GetClipInfosFromTrack(trackInfo);
                if (clipInfos.Count == 0)
                {
                    result.AddLog($"トラック \"{trackInfo.Track.name}\" にクリップがありません。");
                    continue;
                }

                // トラックの時間範囲を計算
                var trackStartTime = (float)clipInfos.Min(c => c.StartTime);
                var trackEndTime = (float)clipInfos.Max(c => c.EndTime);

                // 同じトラック内のクリップを先に統合（ClipMergerを使用）
                var mergedTrackClip = clipMerger.Merge(clipInfos);
                if (mergedTrackClip == null)
                {
                    continue;
                }

                // 統合されたクリップからカーブを取得
                var mergedCurveBindingPairs = clipMerger.GetAnimationCurves(mergedTrackClip);

                // パス自動補正（Animatorが利用可能な場合のみ）
                if (animator != null)
                {
                    var pathCorrector = new CurvePathCorrector();
                    var correctionResult = pathCorrector.CorrectPaths(
                        mergedCurveBindingPairs, animator.transform);
                    mergedCurveBindingPairs = correctionResult.CorrectedPairs;
                }

                foreach (var pair in mergedCurveBindingPairs)
                {
                    var bindingKey = curveOverrider.GetBindingKey(pair.Binding);

                    if (!allCurveData.ContainsKey(bindingKey))
                    {
                        allCurveData[bindingKey] = new List<CurveWithTimeRangeAndPriority>();
                    }

                    allCurveData[bindingKey].Add(new CurveWithTimeRangeAndPriority
                    {
                        Binding = pair.Binding,
                        Curve = pair.Curve,
                        StartTime = trackStartTime,
                        EndTime = trackEndTime,
                        Priority = trackInfo.Priority
                    });
                }

                // マージされた一時クリップを破棄
                Object.DestroyImmediate(mergedTrackClip);

                result.AddLog($"トラック \"{trackInfo.Track.name}\" を処理しました（優先順位: {trackInfo.Priority}）");
            }

            if (allCurveData.Count == 0)
            {
                result.AddErrorLog("有効なカーブデータがありません。");
                return result;
            }

            // 各バインディングごとにOverride処理を適用
            var finalCurves = new List<Domain.CurveBindingPair>();

            foreach (var kvp in allCurveData)
            {
                // 優先順位の低い順にソート
                var curvesForBinding = kvp.Value.OrderBy(c => c.Priority).ToList();

                if (curvesForBinding.Count == 1)
                {
                    // 単一のカーブの場合はそのまま使用
                    finalCurves.Add(new Domain.CurveBindingPair(
                        curvesForBinding[0].Binding,
                        curvesForBinding[0].Curve));
                }
                else
                {
                    // 複数のカーブがある場合はOverride処理を適用
                    var curveWithTimeRanges = curvesForBinding
                        .Select(c => new CurveWithTimeRange(c.Curve, c.StartTime, c.EndTime))
                        .ToList();

                    var mergedCurve = curveOverrider.MergeMultipleTracks(curveWithTimeRanges);
                    finalCurves.Add(new Domain.CurveBindingPair(
                        curvesForBinding[0].Binding,
                        mergedCurve));
                }
            }

            result.AddLog($"カーブの統合完了: {finalCurves.Count}個のカーブ");

            // AnimationClipを生成
            var timelineAssetName = timelineAsset.name;
            var animatorName = FileNameGenerator.GetHierarchicalAnimatorName(animator);

            if (saveToAsset)
            {
                // .animファイルとして保存
                var savedPath = _exporter.ExportToAsset(
                    result,
                    finalCurves,
                    timelineAssetName,
                    animatorName,
                    frameRate,
                    outputDirectory);

                if (savedPath != null)
                {
                    result.AddLog($"出力完了: {savedPath}");
                }
            }
            else
            {
                // メモリ上にAnimationClipのみ生成（ファイル保存しない）
                var clip = _exporter.CreateAnimationClip(result, finalCurves, frameRate);
                if (clip != null)
                {
                    result.AddLog("AnimationClipをメモリ上に生成しました。");
                }
            }

            return result;
        }

        /// <summary>
        /// TrackInfoからClipInfoのリストを取得する
        /// </summary>
        private List<ClipInfo> GetClipInfosFromTrack(TrackInfo trackInfo)
        {
            var clipInfos = new List<ClipInfo>();

            if (trackInfo?.Track == null)
            {
                return clipInfos;
            }

            foreach (var timelineClip in trackInfo.Track.GetClips())
            {
                var animationPlayableAsset = timelineClip.asset as AnimationPlayableAsset;
                if (animationPlayableAsset?.clip == null)
                {
                    continue;
                }

                // AnimationPlayableAssetからシーンオフセット（Position/Rotation）を取得
                var offsetPosition = animationPlayableAsset.position;
                var offsetRotation = animationPlayableAsset.rotation;

                clipInfos.Add(new ClipInfo(timelineClip, animationPlayableAsset.clip,
                    offsetPosition, offsetRotation));
            }

            return clipInfos;
        }

        /// <summary>
        /// カーブデータと時間範囲、優先順位を保持する内部構造体
        /// </summary>
        private struct CurveWithTimeRangeAndPriority
        {
            public EditorCurveBinding Binding;
            public AnimationCurve Curve;
            public float StartTime;
            public float EndTime;
            public int Priority;
        }

        /// <summary>
        /// PlayableDirectorからアニメーションをメモリ上で結合する（.animファイルを保存しない）
        /// FBXエクスポート用に使用
        /// </summary>
        /// <param name="director">対象のPlayableDirector</param>
        /// <returns>結合結果のリスト（Animator単位）</returns>
        internal List<MergeResult> MergeFromPlayableDirectorInMemory(PlayableDirector director)
        {
            var results = new List<MergeResult>();

            if (director == null)
            {
                Debug.LogError("[AnimationMergeTool] PlayableDirectorがnullです。");
                return results;
            }

            var timelineAsset = director.playableAsset as TimelineAsset;
            if (timelineAsset == null)
            {
                Debug.LogError("[AnimationMergeTool] PlayableDirectorにTimelineAssetが設定されていません。");
                return results;
            }

            // PlayableDirectorからバインディング情報を取得
            var animatorBindings = GetAnimatorBindings(director, timelineAsset);
            if (animatorBindings.Count == 0)
            {
                Debug.LogError("[AnimationMergeTool] バインドされたAnimatorが見つかりません。");
                return results;
            }

            // Animator単位で処理（saveToAsset=false）
            foreach (var kvp in animatorBindings)
            {
                var animator = kvp.Key;
                var tracks = kvp.Value;

                var result = MergeTracksForAnimator(timelineAsset, animator, tracks, null, saveToAsset: false);
                if (result != null)
                {
                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// TimelineAssetからアニメーションをメモリ上で結合する（.animファイルを保存しない）
        /// FBXエクスポート用に使用
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        /// <returns>結合結果のリスト</returns>
        internal List<MergeResult> MergeFromTimelineAssetInMemory(TimelineAsset timelineAsset)
        {
            var results = new List<MergeResult>();

            if (timelineAsset == null)
            {
                Debug.LogError("[AnimationMergeTool] TimelineAssetがnullです。");
                return results;
            }

            var analyzer = new TrackAnalyzer(timelineAsset);
            var allTracks = analyzer.GetAnimationTracksWithPriority();
            var nonMutedTracks = analyzer.FilterNonMutedTracks(allTracks);

            if (nonMutedTracks.Count == 0)
            {
                Debug.LogError("[AnimationMergeTool] 有効なAnimationTrackが見つかりません。");
                return results;
            }

            var result = MergeTracksForAnimator(timelineAsset, null, nonMutedTracks, null, saveToAsset: false);
            if (result != null)
            {
                results.Add(result);
            }

            return results;
        }

        #region FBXエクスポート統合 (P16-004)

        /// <summary>
        /// FBXエクスポート機能が利用可能かどうかを確認する
        /// </summary>
        /// <returns>FBX Exporterパッケージがインストールされている場合はtrue</returns>
        public bool IsFbxExportAvailable()
        {
            return _fbxExporter.IsAvailable();
        }

        /// <summary>
        /// MergeResultからFBXエクスポート用のデータを準備する
        /// </summary>
        /// <param name="mergeResult">マージ結果</param>
        /// <returns>FBXエクスポート用データ</returns>
        public FbxExportData PrepareFbxExportData(MergeResult mergeResult)
        {
            if (mergeResult == null || mergeResult.GeneratedClip == null)
            {
                return null;
            }

            // Humanoidリグの場合はマッスルカーブ→Generic変換を使用
            // マージ後クリップはisHumanMotionがfalseになり得るため、animator.isHumanで判定
            if (mergeResult.TargetAnimator != null &&
                mergeResult.TargetAnimator.isHuman)
            {
                return _fbxExporter.PrepareHumanoidForExport(
                    mergeResult.TargetAnimator,
                    mergeResult.GeneratedClip);
            }

            return _fbxExporter.PrepareAllCurvesForExport(
                mergeResult.TargetAnimator,
                mergeResult.GeneratedClip);
        }

        /// <summary>
        /// MergeResultをFBXファイルとしてエクスポートする
        /// </summary>
        /// <param name="mergeResult">マージ結果</param>
        /// <param name="outputDirectory">出力ディレクトリ</param>
        /// <param name="timelineAssetName">TimelineAsset名（ファイル名生成用）</param>
        /// <returns>エクスポートが成功した場合はtrue</returns>
        public bool ExportToFbx(MergeResult mergeResult, string outputDirectory, string timelineAssetName)
        {
            if (mergeResult == null || mergeResult.GeneratedClip == null)
            {
                Debug.LogError("[AnimationMergeTool] MergeResultまたはGeneratedClipがnullです。");
                return false;
            }

            if (!IsFbxExportAvailable())
            {
                Debug.LogError("[AnimationMergeTool] FBX Exporterパッケージがインストールされていません。");
                return false;
            }

            // FbxExportDataを準備
            var exportData = PrepareFbxExportData(mergeResult);
            if (exportData == null || !exportData.HasExportableData)
            {
                Debug.LogError("[AnimationMergeTool] エクスポート可能なデータがありません。");
                return false;
            }

            // ファイルパスを生成
            var animatorName = FileNameGenerator.GetHierarchicalAnimatorName(mergeResult.TargetAnimator);
            var outputPath = _fileNameGenerator.GenerateUniqueFilePath(
                outputDirectory,
                timelineAssetName,
                animatorName,
                ".fbx");

            // エクスポート実行
            var success = _fbxExporter.Export(exportData, outputPath);

            if (success)
            {
                mergeResult.AddLog($"FBXエクスポート完了: {outputPath}");
            }
            else
            {
                mergeResult.AddErrorLog($"FBXエクスポートに失敗しました: {outputPath}");
            }

            return success;
        }

        /// <summary>
        /// PlayableDirectorからアニメーションを結合し、FBXとしてエクスポートする
        /// .animファイルは生成せず、FBXファイルのみを出力する
        /// </summary>
        /// <param name="director">対象のPlayableDirector</param>
        /// <param name="outputDirectory">出力ディレクトリ（デフォルト: "Assets"）</param>
        /// <returns>結合結果のリスト（Animator単位）</returns>
        public List<MergeResult> MergeAndExportToFbx(PlayableDirector director, string outputDirectory = "Assets")
        {
            if (!IsFbxExportAvailable())
            {
                Debug.LogError("[AnimationMergeTool] FBX Exporterパッケージがインストールされていません。FBXエクスポートをスキップします。");
                return new List<MergeResult>();
            }

            // メモリ上でマージ処理を実行（.animファイルは保存しない）
            var results = MergeFromPlayableDirectorInMemory(director);

            // 各結果をFBXとしてエクスポート
            var timelineAssetName = (director?.playableAsset as TimelineAsset)?.name ?? "Unknown";
            foreach (var result in results)
            {
                if (result.IsSuccess)
                {
                    ExportToFbx(result, outputDirectory, timelineAssetName);
                }
            }

            return results;
        }

        /// <summary>
        /// TimelineAssetからアニメーションを結合し、FBXとしてエクスポートする
        /// .animファイルは生成せず、FBXファイルのみを出力する
        /// </summary>
        /// <param name="timelineAsset">対象のTimelineAsset</param>
        /// <param name="outputDirectory">出力ディレクトリ（デフォルト: "Assets"）</param>
        /// <returns>結合結果のリスト</returns>
        public List<MergeResult> MergeFromTimelineAssetAndExportToFbx(TimelineAsset timelineAsset, string outputDirectory = "Assets")
        {
            if (!IsFbxExportAvailable())
            {
                Debug.LogError("[AnimationMergeTool] FBX Exporterパッケージがインストールされていません。FBXエクスポートをスキップします。");
                return new List<MergeResult>();
            }

            // メモリ上でマージ処理を実行（.animファイルは保存しない）
            var results = MergeFromTimelineAssetInMemory(timelineAsset);

            // 各結果をFBXとしてエクスポート
            var timelineAssetName = timelineAsset?.name ?? "Unknown";
            foreach (var result in results)
            {
                if (result.IsSuccess)
                {
                    ExportToFbx(result, outputDirectory, timelineAssetName);
                }
            }

            return results;
        }

        #endregion
    }

}
