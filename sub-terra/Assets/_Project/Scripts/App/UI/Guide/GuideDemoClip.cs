using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.Guide
{
    /// <summary>시간에 따라 값이 바뀌는 키프레임 트랙. 구간 사이는 부드럽게 보간하거나(Step이면 계단식) 한다.</summary>
    public sealed class DemoTrack
    {
        private readonly float[] times;
        private readonly float[] values;
        private readonly bool step;

        private DemoTrack(float[] times, float[] values, bool step)
        {
            this.times = times;
            this.values = values;
            this.step = step;
        }

        public static DemoTrack Const(float value)
        {
            return new DemoTrack(new[] { 0f }, new[] { value }, false);
        }

        /// <summary>(시간, 값) 쌍을 이어 붙인 목록. 구간은 smoothstep으로 보간한다.</summary>
        public static DemoTrack Smooth(params float[] timeValuePairs)
        {
            return Build(timeValuePairs, false);
        }

        /// <summary>(시간, 값) 쌍. 값이 다음 키 시각에 바로 바뀐다.</summary>
        public static DemoTrack Hold(params float[] timeValuePairs)
        {
            return Build(timeValuePairs, true);
        }

        /// <summary>start~end 동안 1이고 나머지는 0인 펄스(눌림 표시용).</summary>
        public static DemoTrack Pulse(float start, float end, float ramp = 0.06f)
        {
            return Build(new[] { 0f, 0f, start, 0f, start + ramp, 1f, end, 1f, end + ramp, 0f }, false);
        }

        public static DemoTrack Pulses(params float[] startEndPairs)
        {
            var list = new List<float> { 0f, 0f };
            for (var i = 0; i + 1 < startEndPairs.Length; i += 2)
            {
                list.Add(startEndPairs[i]); list.Add(0f);
                list.Add(startEndPairs[i] + 0.06f); list.Add(1f);
                list.Add(startEndPairs[i + 1]); list.Add(1f);
                list.Add(startEndPairs[i + 1] + 0.06f); list.Add(0f);
            }

            return Build(list.ToArray(), false);
        }

        private static DemoTrack Build(float[] pairs, bool step)
        {
            if (pairs == null || pairs.Length < 2 || pairs.Length % 2 != 0)
            {
                throw new ArgumentException("트랙은 (시간, 값) 쌍이어야 합니다.");
            }

            var count = pairs.Length / 2;
            var t = new float[count];
            var v = new float[count];
            for (var i = 0; i < count; i++)
            {
                t[i] = pairs[i * 2];
                v[i] = pairs[i * 2 + 1];
            }

            return new DemoTrack(t, v, step);
        }

        public float Eval(float time)
        {
            var last = times.Length - 1;
            if (time <= times[0])
            {
                return values[0];
            }

            if (time >= times[last])
            {
                return values[last];
            }

            for (var i = 0; i < last; i++)
            {
                if (time < times[i + 1])
                {
                    if (step)
                    {
                        return values[i];
                    }

                    var span = times[i + 1] - times[i];
                    var x = span <= 0.0001f ? 1f : (time - times[i]) / span;
                    x = x * x * (3f - 2f * x);
                    return Mathf.Lerp(values[i], values[i + 1], x);
                }
            }

            return values[last];
        }
    }

    public enum DemoLayerKind
    {
        Sprite = 0,
        Rect = 1,
        Text = 2
    }

    /// <summary>시연 장면의 한 층. 좌표는 무대 중앙 기준이고 위쪽이 +y다(무대 크기 320x120).</summary>
    public sealed class DemoLayer
    {
        public DemoLayerKind Kind;
        public string SpriteKey = string.Empty;
        /// <summary>Sprite: 높이(가로는 원본 비율). Rect·Text: 가로 x 세로.</summary>
        public Vector2 Size;
        public Color Tint = Color.white;
        public string Content = string.Empty;
        public float FontSize = 12f;
        public bool Bold;
        public bool Pixel;
        public Vector2 Pivot = new Vector2(0.5f, 0.5f);
        public bool UseOutline;
        /// <summary>true면 Size를 가로·세로 그대로 쓴다(9-slice 면·윤곽 스프라이트용).</summary>
        public bool SizeIsRect;
        public DemoTrack X = DemoTrack.Const(0f);
        public DemoTrack Y = DemoTrack.Const(0f);
        public DemoTrack ScaleX = DemoTrack.Const(1f);
        public DemoTrack ScaleY = DemoTrack.Const(1f);
        public DemoTrack Alpha = DemoTrack.Const(1f);
        public DemoTrack Rotation = DemoTrack.Const(0f);
        public string[] FrameKeys;
        public float Fps = 10f;
        public string IdleKey = string.Empty;
        public DemoTrack FrameActive;

        public DemoLayer PosX(DemoTrack track) { X = track; return this; }
        public DemoLayer PosY(DemoTrack track) { Y = track; return this; }
        public DemoLayer Sx(DemoTrack track) { ScaleX = track; return this; }
        public DemoLayer Sy(DemoTrack track) { ScaleY = track; return this; }
        public DemoLayer Fade(DemoTrack track) { Alpha = track; return this; }
        public DemoLayer Rot(DemoTrack track) { Rotation = track; return this; }
        public DemoLayer At(float x, float y) { X = DemoTrack.Const(x); Y = DemoTrack.Const(y); return this; }
        public DemoLayer Scale(DemoTrack both) { ScaleX = both; ScaleY = both; return this; }
        public DemoLayer Color_(Color color) { Tint = color; return this; }
        public DemoLayer PivotAt(float x, float y) { Pivot = new Vector2(x, y); return this; }
        public DemoLayer Outlined() { UseOutline = true; return this; }

        /// <summary>프레임 반복 재생. active가 있으면 1일 때만 재생하고 아니면 idleKey를 보인다.</summary>
        public DemoLayer Animate(string prefix, int count, float fps, string idleKey, DemoTrack active)
        {
            FrameKeys = new string[count];
            for (var i = 0; i < count; i++)
            {
                FrameKeys[i] = prefix + "." + (i + 1);
            }

            Fps = fps;
            IdleKey = idleKey;
            FrameActive = active;
            return this;
        }
    }

    /// <summary>시연에 겹쳐 보이는 키캡. Press가 1이면 눌린 상태다.</summary>
    public sealed class DemoKeyCap
    {
        public string Label = string.Empty;
        public bool Mouse;
        public float X;
        public float Y;
        public DemoTrack Press = DemoTrack.Const(0f);
        public DemoTrack Alpha = DemoTrack.Const(1f);
    }

    public sealed class DemoClip
    {
        public string Id = string.Empty;
        public float Duration = 3f;
        /// <summary>정지 썸네일로 쓸 시각(결과가 알아볼 만큼 보이는 순간).</summary>
        public float ThumbTime = 1f;
        public readonly List<DemoLayer> Layers = new List<DemoLayer>();
        public readonly List<DemoKeyCap> Keys = new List<DemoKeyCap>();

        public IEnumerable<string> UsedSpriteKeys()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Layers.Count; i++)
            {
                var layer = Layers[i];
                if (layer.Kind != DemoLayerKind.Sprite)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(layer.SpriteKey)) set.Add(layer.SpriteKey);
                if (!string.IsNullOrEmpty(layer.IdleKey)) set.Add(layer.IdleKey);
                for (var f = 0; layer.FrameKeys != null && f < layer.FrameKeys.Length; f++)
                {
                    set.Add(layer.FrameKeys[f]);
                }
            }

            return set;
        }
    }
}
