using System.Collections.Generic;
using UnityEngine;

namespace AnimationMergeTool.Editor.Domain
{
    /// <summary>
    /// アニメーションカーブを指定フレームレートにリサンプリングするクラス
    /// ソースクリップのフレームレートに依存しないキーフレーム間隔に変換する
    /// </summary>
    public class CurveResampler
    {
        /// <summary>
        /// カーブリストを指定フレームレートにリサンプリングする
        /// </summary>
        /// <param name="pairs">リサンプリング対象のカーブバインディングペアリスト</param>
        /// <param name="frameRate">目標フレームレート（fps）</param>
        /// <returns>リサンプリング後のカーブバインディングペアリスト</returns>
        public List<CurveBindingPair> Resample(List<CurveBindingPair> pairs, float frameRate)
        {
            if (pairs == null || frameRate <= 0)
            {
                return pairs;
            }

            var result = new List<CurveBindingPair>();
            var interval = 1f / frameRate;

            foreach (var pair in pairs)
            {
                if (pair.Curve == null || pair.Curve.keys.Length == 0)
                {
                    result.Add(pair);
                    continue;
                }

                var keys = pair.Curve.keys;
                var startTime = keys[0].time;
                var endTime = keys[keys.Length - 1].time;

                // 開始時間をフレーム境界にスナップ（切り捨て）
                var snappedStart = Mathf.Floor(startTime / interval) * interval;
                // 終了時間をフレーム境界にスナップ（切り上げ）
                var snappedEnd = Mathf.Ceil(endTime / interval) * interval;

                var newCurve = new AnimationCurve();
                for (var t = snappedStart; t <= snappedEnd + interval * 0.01f; t += interval)
                {
                    var value = pair.Curve.Evaluate(t);
                    newCurve.AddKey(new Keyframe(t, value));
                }

                result.Add(new CurveBindingPair(pair.Binding, newCurve));
            }

            return result;
        }
    }
}
