using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AnimationMergeTool.Editor.Domain;

namespace AnimationMergeTool.Editor.Tests
{
    /// <summary>
    /// CurveResamplerクラスの単体テスト
    /// </summary>
    public class CurveResamplerTests
    {
        private CurveResampler _resampler;

        [SetUp]
        public void SetUp()
        {
            _resampler = new CurveResampler();
        }

        #region Null・異常系テスト

        [Test]
        public void Resample_nullを渡すとnullを返す()
        {
            var result = _resampler.Resample(null, 30f);
            Assert.IsNull(result);
        }

        [Test]
        public void Resample_frameRateが0以下の場合は入力をそのまま返す()
        {
            var pairs = new List<CurveBindingPair>();
            var result = _resampler.Resample(pairs, 0f);
            Assert.AreSame(pairs, result);

            result = _resampler.Resample(pairs, -1f);
            Assert.AreSame(pairs, result);
        }

        [Test]
        public void Resample_空リストを渡すと空リストを返す()
        {
            var pairs = new List<CurveBindingPair>();
            var result = _resampler.Resample(pairs, 30f);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Resample_カーブがnullの場合はそのまま保持する()
        {
            var binding = new EditorCurveBinding
            {
                path = "Test",
                propertyName = "m_LocalPosition.x",
                type = typeof(Transform)
            };
            var pairs = new List<CurveBindingPair>
            {
                new CurveBindingPair(binding, null)
            };

            var result = _resampler.Resample(pairs, 30f);
            Assert.AreEqual(1, result.Count);
            Assert.IsNull(result[0].Curve);
        }

        [Test]
        public void Resample_キーが0個のカーブはそのまま保持する()
        {
            var binding = new EditorCurveBinding
            {
                path = "Test",
                propertyName = "m_LocalPosition.x",
                type = typeof(Transform)
            };
            var emptyCurve = new AnimationCurve();
            var pairs = new List<CurveBindingPair>
            {
                new CurveBindingPair(binding, emptyCurve)
            };

            var result = _resampler.Resample(pairs, 30f);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(0, result[0].Curve.keys.Length);
        }

        #endregion

        #region リサンプリング基本テスト

        [Test]
        public void Resample_60fpsカーブを30fpsにリサンプリングするとキーフレーム間隔が正しくなる()
        {
            // 60fpsで0秒〜1秒のカーブ（61キーフレーム）
            var binding = new EditorCurveBinding
            {
                path = "",
                propertyName = "m_LocalPosition.x",
                type = typeof(Transform)
            };
            var curve = new AnimationCurve();
            for (var i = 0; i <= 60; i++)
            {
                var t = i / 60f;
                curve.AddKey(new Keyframe(t, t * 10f)); // 線形: 0→10
            }
            var pairs = new List<CurveBindingPair>
            {
                new CurveBindingPair(binding, curve)
            };

            var result = _resampler.Resample(pairs, 30f);

            Assert.AreEqual(1, result.Count);
            var resampledKeys = result[0].Curve.keys;

            // 30fpsで0秒〜1秒 → 31キーフレーム
            Assert.AreEqual(31, resampledKeys.Length);

            // キーフレーム間隔が1/30秒であることを検証
            var interval = 1f / 30f;
            for (var i = 0; i < resampledKeys.Length; i++)
            {
                Assert.AreEqual(i * interval, resampledKeys[i].time, 0.0001f,
                    $"キーフレーム{i}の時間が不正: 期待={i * interval}, 実際={resampledKeys[i].time}");
            }
        }

        [Test]
        public void Resample_リサンプリング後の値がEvaluateと一致する()
        {
            // Sin波カーブを作成
            var binding = new EditorCurveBinding
            {
                path = "Body",
                propertyName = "m_LocalRotation.y",
                type = typeof(Transform)
            };
            var curve = new AnimationCurve();
            for (var i = 0; i <= 120; i++)
            {
                var t = i / 120f; // 120fpsで1秒
                curve.AddKey(new Keyframe(t, Mathf.Sin(t * Mathf.PI * 2f)));
            }
            var pairs = new List<CurveBindingPair>
            {
                new CurveBindingPair(binding, curve)
            };

            var result = _resampler.Resample(pairs, 30f);

            var resampledKeys = result[0].Curve.keys;
            var interval = 1f / 30f;
            for (var i = 0; i < resampledKeys.Length; i++)
            {
                var expectedTime = i * interval;
                var expectedValue = curve.Evaluate(expectedTime);
                Assert.AreEqual(expectedValue, resampledKeys[i].value, 0.0001f,
                    $"キーフレーム{i}の値が不正: 期待={expectedValue}, 実際={resampledKeys[i].value}");
            }
        }

        [Test]
        public void Resample_開始時間がフレーム境界にスナップされる()
        {
            // 開始時間が0.01秒（フレーム境界ではない）のカーブ
            var binding = new EditorCurveBinding
            {
                path = "",
                propertyName = "m_LocalPosition.y",
                type = typeof(Transform)
            };
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0.01f, 1f));
            curve.AddKey(new Keyframe(0.1f, 2f));

            var pairs = new List<CurveBindingPair>
            {
                new CurveBindingPair(binding, curve)
            };

            var result = _resampler.Resample(pairs, 30f);
            var resampledKeys = result[0].Curve.keys;

            // 開始時間は0.01を切り捨てて0.0にスナップされるべき
            Assert.AreEqual(0f, resampledKeys[0].time, 0.0001f);

            // 全キーフレームが1/30間隔のフレーム境界に乗っていることを検証
            var interval = 1f / 30f;
            for (var i = 0; i < resampledKeys.Length; i++)
            {
                var remainder = resampledKeys[i].time % interval;
                var isOnBoundary = remainder < 0.0001f || (interval - remainder) < 0.0001f;
                Assert.IsTrue(isOnBoundary,
                    $"キーフレーム{i} (time={resampledKeys[i].time}) がフレーム境界に乗っていません");
            }
        }

        [Test]
        public void Resample_終了時間がフレーム境界にスナップされる()
        {
            // 終了時間が0.99秒（フレーム境界ではない）のカーブ
            var binding = new EditorCurveBinding
            {
                path = "",
                propertyName = "m_LocalPosition.z",
                type = typeof(Transform)
            };
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, 0f));
            curve.AddKey(new Keyframe(0.99f, 5f));

            var pairs = new List<CurveBindingPair>
            {
                new CurveBindingPair(binding, curve)
            };

            var result = _resampler.Resample(pairs, 30f);
            var resampledKeys = result[0].Curve.keys;

            // 終了時間は0.99を切り上げて1.0にスナップされるべき
            var lastKey = resampledKeys[resampledKeys.Length - 1];
            Assert.AreEqual(1f, lastKey.time, 0.0001f,
                $"最終キーフレームの時間が不正: 期待=1.0, 実際={lastKey.time}");
        }

        #endregion

        #region バインディング保持テスト

        [Test]
        public void Resample_バインディング情報が保持される()
        {
            var binding = new EditorCurveBinding
            {
                path = "Root/Child",
                propertyName = "m_LocalScale.x",
                type = typeof(Transform)
            };
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, 1f));
            curve.AddKey(new Keyframe(1f, 2f));

            var pairs = new List<CurveBindingPair>
            {
                new CurveBindingPair(binding, curve)
            };

            var result = _resampler.Resample(pairs, 30f);

            Assert.AreEqual(binding.path, result[0].Binding.path);
            Assert.AreEqual(binding.propertyName, result[0].Binding.propertyName);
            Assert.AreEqual(binding.type, result[0].Binding.type);
        }

        [Test]
        public void Resample_複数カーブを同時にリサンプリングできる()
        {
            var pairs = new List<CurveBindingPair>();
            var properties = new[] { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z" };

            foreach (var prop in properties)
            {
                var binding = new EditorCurveBinding
                {
                    path = "",
                    propertyName = prop,
                    type = typeof(Transform)
                };
                var curve = new AnimationCurve();
                for (var i = 0; i <= 60; i++)
                {
                    curve.AddKey(new Keyframe(i / 60f, i));
                }
                pairs.Add(new CurveBindingPair(binding, curve));
            }

            var result = _resampler.Resample(pairs, 30f);

            Assert.AreEqual(3, result.Count);
            foreach (var pair in result)
            {
                Assert.AreEqual(31, pair.Curve.keys.Length);
            }
        }

        #endregion

        #region 単一キーフレームテスト

        [Test]
        public void Resample_単一キーフレームのカーブは1キーフレームにリサンプリングされる()
        {
            var binding = new EditorCurveBinding
            {
                path = "",
                propertyName = "m_LocalPosition.x",
                type = typeof(Transform)
            };
            var curve = new AnimationCurve();
            // 0.0秒はフレーム境界に正確に一致するため、開始=終了でスナップ後1キーになる
            curve.AddKey(new Keyframe(0f, 3f));

            var pairs = new List<CurveBindingPair>
            {
                new CurveBindingPair(binding, curve)
            };

            var result = _resampler.Resample(pairs, 30f);

            Assert.AreEqual(1, result[0].Curve.keys.Length);
            Assert.AreEqual(3f, result[0].Curve.keys[0].value, 0.0001f);
        }

        #endregion
    }
}
