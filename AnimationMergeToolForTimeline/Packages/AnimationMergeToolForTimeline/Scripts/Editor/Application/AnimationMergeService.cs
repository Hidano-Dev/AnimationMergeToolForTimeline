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
    /// </summary>
    public class AnimationMergeService
    {
        private readonly FileNameGenerator _fileNameGenerator;
        private readonly AnimationClipExporter _exporter;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AnimationMergeService()
        {
            _fileNameGenerator = new FileNameGenerator();
            _fileNameGenerator.SetFileExistenceChecker(new AssetDatabaseFileExistenceChecker());
            _exporter = new AnimationClipExporter(_fileNameGenerator);
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
                if (trackInfo.IsMuted)
                {
                    continue;
                }

                // PlayableDirectorからトラックにバインドされているAnimatorを取得
                var binding = director.GetGenericBinding(trackInfo.Track);
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
        /// 指定されたAnimator用のトラックを結合する
        /// </summary>
        private MergeResult MergeTracksForAnimator(
            TimelineAsset timelineAsset,
            Animator animator,
            List<TrackInfo> tracks,
            string outputDirectory)
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

                // トラック内のクリップをマージ
                var mergedClip = clipMerger.Merge(clipInfos);
                if (mergedClip == null)
                {
                    result.AddLog($"トラック \"{trackInfo.Track.name}\" のマージに失敗しました。");
                    continue;
                }

                // マージされたクリップからカーブを取得
                var curveBindingPairs = clipMerger.GetAnimationCurves(mergedClip);

                // トラックの時間範囲を計算
                var trackStartTime = (float)clipInfos.Min(c => c.StartTime);
                var trackEndTime = (float)clipInfos.Max(c => c.EndTime);

                foreach (var pair in curveBindingPairs)
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

            // AnimationClipを生成してエクスポート
            var timelineAssetName = timelineAsset.name;
            var animatorName = animator != null ? animator.name : "NoAnimator";

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

                clipInfos.Add(new ClipInfo(timelineClip, animationPlayableAsset.clip));
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
    }

    /// <summary>
    /// AssetDatabaseを使用したファイル存在チェッカー
    /// </summary>
    internal class AssetDatabaseFileExistenceChecker : IFileExistenceChecker
    {
        public bool Exists(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Object>(path) != null;
        }
    }
}
