using System.Collections.Generic;
using System.Linq;
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
            var result = new List<TrackInfo>();

            if (parentTrack == null)
            {
                return result;
            }

            // 親トラックの子トラックを取得し、AnimationTrackのみをフィルタリング
            foreach (var childTrack in parentTrack.GetChildTracks())
            {
                if (childTrack is AnimationTrack overrideTrack)
                {
                    var trackInfo = new TrackInfo(overrideTrack);
                    result.Add(trackInfo);
                }
            }

            return result;
        }

        /// <summary>
        /// Muteされていないトラックのみをフィルタリングする
        /// </summary>
        /// <param name="tracks">フィルタリング対象のトラックリスト</param>
        /// <returns>Muteされていないトラックのリスト</returns>
        public List<TrackInfo> FilterNonMutedTracks(List<TrackInfo> tracks)
        {
            if (tracks == null)
            {
                return new List<TrackInfo>();
            }

            return tracks.Where(t => t != null && !t.IsMuted).ToList();
        }

        /// <summary>
        /// バインドされていないトラックを検出し、エラーログを出力する
        /// </summary>
        /// <param name="tracks">検出対象のトラックリスト</param>
        /// <returns>バインドされていないトラックのリスト</returns>
        public List<TrackInfo> DetectUnboundTracks(List<TrackInfo> tracks)
        {
            if (tracks == null)
            {
                return new List<TrackInfo>();
            }

            var unboundTracks = tracks
                .Where(t => t != null && t.BoundAnimator == null)
                .ToList();

            // バインドされていないトラックに対してエラーログを出力
            foreach (var trackInfo in unboundTracks)
            {
                var trackName = trackInfo.Track != null ? trackInfo.Track.name : "Unknown";
                Debug.LogError($"[AnimationMergeTool] トラック \"{trackName}\" にAnimatorがバインドされていません。");
            }

            return unboundTracks;
        }

        /// <summary>
        /// Timeline上でのトラックのインデックス（位置）を取得する
        /// GetOutputTracks()が返す順序がTimeline上の表示順序（上から下）に対応する
        /// </summary>
        /// <param name="track">インデックスを取得するトラック</param>
        /// <returns>トラックのインデックス（0始まり）。見つからない場合は-1</returns>
        public int GetTrackIndex(TrackAsset track)
        {
            if (_timelineAsset == null || track == null)
            {
                return -1;
            }

            var outputTracks = _timelineAsset.GetOutputTracks().ToList();
            return outputTracks.IndexOf(track);
        }

        /// <summary>
        /// TimelineAssetから全ての出力トラックをインデックス順（Timeline上の表示順序）で取得する
        /// </summary>
        /// <returns>出力トラックのリスト（インデックス順）</returns>
        public List<TrackAsset> GetOutputTracksInOrder()
        {
            if (_timelineAsset == null)
            {
                return new List<TrackAsset>();
            }

            return _timelineAsset.GetOutputTracks().ToList();
        }

        /// <summary>
        /// AnimationTrackをインデックス付きで取得する
        /// インデックスはTimeline上の表示順序（上から下、0始まり）
        /// </summary>
        /// <returns>インデックスとTrackInfoのペアのリスト</returns>
        public List<(int index, TrackInfo trackInfo)> GetAnimationTracksWithIndex()
        {
            var result = new List<(int index, TrackInfo trackInfo)>();

            if (_timelineAsset == null)
            {
                return result;
            }

            var outputTracks = _timelineAsset.GetOutputTracks().ToList();

            for (int i = 0; i < outputTracks.Count; i++)
            {
                if (outputTracks[i] is AnimationTrack animationTrack)
                {
                    var trackInfo = new TrackInfo(animationTrack);
                    result.Add((i, trackInfo));
                }
            }

            return result;
        }
    }
}
