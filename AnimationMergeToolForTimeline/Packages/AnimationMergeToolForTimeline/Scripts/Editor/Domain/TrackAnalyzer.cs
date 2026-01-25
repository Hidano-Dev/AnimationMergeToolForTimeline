using System.Collections.Generic;
using AnimationMergeTool.Editor.Domain.Models;
using UnityEngine;
using UnityEngine.Timeline;

namespace AnimationMergeTool.Editor.Domain
{
    /// <summary>
    /// TimelineAssetからAnimationTrackを分析・検出するクラス
    /// </summary>
    public class TrackAnalyzer
    {
        /// <summary>
        /// 分析対象のTimelineAsset
        /// </summary>
        private readonly TimelineAsset _timelineAsset;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="timelineAsset">分析対象のTimelineAsset</param>
        public TrackAnalyzer(TimelineAsset timelineAsset)
        {
            _timelineAsset = timelineAsset;
        }

        /// <summary>
        /// TimelineAssetから全てのAnimationTrack情報を取得する
        /// </summary>
        /// <returns>TrackInfoのリスト</returns>
        public List<TrackInfo> GetAllAnimationTracks()
        {
            var result = new List<TrackInfo>();

            if (_timelineAsset == null)
            {
                return result;
            }

            // TimelineAssetから全ての出力トラックを取得し、AnimationTrackのみをフィルタリング
            foreach (var track in _timelineAsset.GetOutputTracks())
            {
                if (track is AnimationTrack animationTrack)
                {
                    var trackInfo = new TrackInfo(animationTrack);
                    result.Add(trackInfo);
                }
            }

            return result;
        }

        /// <summary>
        /// 指定されたAnimationTrackのOverrideTrackを取得する
        /// </summary>
        /// <param name="parentTrack">親となるAnimationTrack</param>
        /// <returns>OverrideTrackのTrackInfoリスト</returns>
        public List<TrackInfo> GetOverrideTracks(AnimationTrack parentTrack)
        {
            // 1.3.3で実装予定
            return new List<TrackInfo>();
        }

        /// <summary>
        /// Muteされていないトラックのみをフィルタリングする
        /// </summary>
        /// <param name="tracks">フィルタリング対象のトラックリスト</param>
        /// <returns>Muteされていないトラックのリスト</returns>
        public List<TrackInfo> FilterNonMutedTracks(List<TrackInfo> tracks)
        {
            // 1.3.4で実装予定
            return new List<TrackInfo>();
        }

        /// <summary>
        /// バインドされていないトラックを検出し、エラーログを出力する
        /// </summary>
        /// <param name="tracks">検出対象のトラックリスト</param>
        /// <returns>バインドされていないトラックのリスト</returns>
        public List<TrackInfo> DetectUnboundTracks(List<TrackInfo> tracks)
        {
            // 1.3.5で実装予定
            return new List<TrackInfo>();
        }
    }
}
