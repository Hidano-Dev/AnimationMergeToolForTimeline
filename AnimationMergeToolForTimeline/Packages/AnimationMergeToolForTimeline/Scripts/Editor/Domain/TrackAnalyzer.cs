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

        /// <summary>
        /// AnimationTrackに優先順位を割り当てて取得する
        /// Timeline上で下にあるトラック（インデックスが大きい）ほど高い優先順位を持つ
        /// </summary>
        /// <returns>優先順位が設定されたTrackInfoのリスト（優先順位の低い順）</returns>
        public List<TrackInfo> GetAnimationTracksWithPriority()
        {
            var result = new List<TrackInfo>();

            if (_timelineAsset == null)
            {
                return result;
            }

            var outputTracks = _timelineAsset.GetOutputTracks().ToList();
            var animationTracks = new List<(int index, AnimationTrack track)>();

            // AnimationTrackのみを抽出しインデックスを記録
            for (int i = 0; i < outputTracks.Count; i++)
            {
                if (outputTracks[i] is AnimationTrack animationTrack)
                {
                    animationTracks.Add((i, animationTrack));
                }
            }

            // 優先順位を割り当て（インデックスが大きいほど高優先 = 優先順位の数値が大きい）
            for (int i = 0; i < animationTracks.Count; i++)
            {
                var (index, track) = animationTracks[i];
                var trackInfo = new TrackInfo(track, index);
                result.Add(trackInfo);
            }

            return result;
        }

        /// <summary>
        /// トラックリストに優先順位を割り当てる
        /// Timeline上の位置（インデックス）に基づいて優先順位を設定する
        /// 下にあるトラック（インデックスが大きい）ほど高い優先順位を持つ
        /// </summary>
        /// <param name="tracks">優先順位を割り当てるトラックリスト</param>
        public void AssignPriorities(List<TrackInfo> tracks)
        {
            if (tracks == null || _timelineAsset == null)
            {
                return;
            }

            var outputTracks = _timelineAsset.GetOutputTracks().ToList();

            foreach (var trackInfo in tracks)
            {
                if (trackInfo?.Track == null)
                {
                    continue;
                }

                var index = outputTracks.IndexOf(trackInfo.Track);
                if (index >= 0)
                {
                    trackInfo.Priority = index;
                }
            }
        }

        /// <summary>
        /// 指定されたトラックの親GroupTrackを取得する
        /// </summary>
        /// <param name="track">親を取得するトラック</param>
        /// <returns>親GroupTrack。親がない場合はnull</returns>
        public GroupTrack GetParentGroup(TrackAsset track)
        {
            if (track == null)
            {
                return null;
            }

            return track.parent as GroupTrack;
        }

        /// <summary>
        /// 指定されたGroupTrack内の子トラックを取得する
        /// </summary>
        /// <param name="groupTrack">子トラックを取得するGroupTrack</param>
        /// <returns>子トラックのリスト</returns>
        public List<TrackAsset> GetChildTracksInGroup(GroupTrack groupTrack)
        {
            var result = new List<TrackAsset>();

            if (groupTrack == null)
            {
                return result;
            }

            foreach (var childTrack in groupTrack.GetChildTracks())
            {
                result.Add(childTrack);
            }

            return result;
        }

        /// <summary>
        /// 指定されたGroupTrack内のAnimationTrackのみを取得する
        /// </summary>
        /// <param name="groupTrack">AnimationTrackを取得するGroupTrack</param>
        /// <returns>AnimationTrackのTrackInfoリスト</returns>
        public List<TrackInfo> GetAnimationTracksInGroup(GroupTrack groupTrack)
        {
            var result = new List<TrackInfo>();

            if (groupTrack == null)
            {
                return result;
            }

            foreach (var childTrack in groupTrack.GetChildTracks())
            {
                if (childTrack is AnimationTrack animationTrack)
                {
                    result.Add(new TrackInfo(animationTrack));
                }
            }

            return result;
        }

        /// <summary>
        /// TimelineAsset内の全てのGroupTrackを取得する
        /// </summary>
        /// <returns>GroupTrackのリスト</returns>
        public List<GroupTrack> GetAllGroupTracks()
        {
            var result = new List<GroupTrack>();

            if (_timelineAsset == null)
            {
                return result;
            }

            foreach (var track in _timelineAsset.GetRootTracks())
            {
                if (track is GroupTrack groupTrack)
                {
                    result.Add(groupTrack);
                }
            }

            return result;
        }

        /// <summary>
        /// トラックがGroupTrack内に含まれているかを判定する
        /// </summary>
        /// <param name="track">判定対象のトラック</param>
        /// <returns>GroupTrack内に含まれている場合true</returns>
        public bool IsTrackInGroup(TrackAsset track)
        {
            if (track == null)
            {
                return false;
            }

            return track.parent is GroupTrack;
        }

        /// <summary>
        /// 階層構造を含めてトラックの深さ（ネストレベル）を取得する
        /// ルートレベルは0、GroupTrack内は1、ネストされたGroupTrack内は2...
        /// </summary>
        /// <param name="track">深さを取得するトラック</param>
        /// <returns>トラックの深さ（ネストレベル）</returns>
        public int GetTrackDepth(TrackAsset track)
        {
            if (track == null)
            {
                return -1;
            }

            int depth = 0;
            var parent = track.parent;

            while (parent is GroupTrack)
            {
                depth++;
                parent = (parent as TrackAsset)?.parent;
            }

            return depth;
        }

        /// <summary>
        /// 階層構造を含めてAnimationTrackに優先順位を割り当てて取得する
        /// GetOutputTracks()の順序に基づき、下にあるトラック（インデックスが大きい）ほど高い優先順位を持つ
        /// GroupTrack内のトラックも含めて正しい順序で優先順位を計算する
        /// </summary>
        /// <returns>優先順位が設定されたTrackInfoのリスト（優先順位の低い順）</returns>
        public List<TrackInfo> GetAnimationTracksWithPriorityIncludingHierarchy()
        {
            var result = new List<TrackInfo>();

            if (_timelineAsset == null)
            {
                return result;
            }

            // GetOutputTracks()はGroupTrack内のトラックも含めてフラット化された順序で返す
            // この順序がTimeline上の視覚的な表示順序（上から下）に対応する
            var outputTracks = _timelineAsset.GetOutputTracks().ToList();

            for (int i = 0; i < outputTracks.Count; i++)
            {
                if (outputTracks[i] is AnimationTrack animationTrack)
                {
                    var trackInfo = new TrackInfo(animationTrack, i);
                    result.Add(trackInfo);
                }
            }

            return result;
        }

        /// <summary>
        /// 階層構造を含めてトラックリストに優先順位を割り当てる
        /// GetOutputTracks()の順序に基づいて優先順位を設定する
        /// 下にあるトラック（インデックスが大きい）ほど高い優先順位を持つ
        /// </summary>
        /// <param name="tracks">優先順位を割り当てるトラックリスト</param>
        public void AssignPrioritiesIncludingHierarchy(List<TrackInfo> tracks)
        {
            if (tracks == null || _timelineAsset == null)
            {
                return;
            }

            // GetOutputTracks()はGroupTrack内のトラックも含めてフラット化された順序で返す
            var outputTracks = _timelineAsset.GetOutputTracks().ToList();

            foreach (var trackInfo in tracks)
            {
                if (trackInfo?.Track == null)
                {
                    continue;
                }

                var index = outputTracks.IndexOf(trackInfo.Track);
                if (index >= 0)
                {
                    trackInfo.Priority = index;
                }
            }
        }
    }
}
