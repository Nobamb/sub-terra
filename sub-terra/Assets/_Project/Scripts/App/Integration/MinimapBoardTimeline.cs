using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>M키 순환 단계. 닫힘 → 가로형 → 정사각형 → 닫힘.</summary>
    public enum MinimapBoardMode
    {
        Closed = 0,
        Wide = 1,
        Square = 2
    }

    /// <summary>전광판 한 순간의 표시값. 모두 0~1 범위다.</summary>
    public struct MinimapBoardPose
    {
        /// <summary>세로 개방 비율. 마스크 높이에만 쓰고 지도는 늘이지 않는다.</summary>
        public float Open;
        /// <summary>1 = 가로형 폭, 0 = 정사각형 폭.</summary>
        public float Width;
        public float Frame;
        public float Terrain;
        public float Markers;
        /// <summary>개폐 시 수평 광선 밝기.</summary>
        public float Line;

        public static MinimapBoardPose Closed(float width) => new MinimapBoardPose { Width = width };

        public static MinimapBoardPose Settled(MinimapBoardMode mode, float closedWidth) => mode switch
        {
            MinimapBoardMode.Wide => new MinimapBoardPose { Open = 1f, Width = 1f, Frame = 1f, Terrain = 1f, Markers = 1f },
            MinimapBoardMode.Square => new MinimapBoardPose { Open = 1f, Width = 0f, Frame = 1f, Terrain = 1f, Markers = 1f },
            _ => Closed(closedWidth)
        };
    }

    /// <summary>
    /// 미니맵 전광판 상태·연출 계산. unscaled 시간만 받으며 지도 데이터 갱신과는 무관하다.
    /// 전환 중 재입력은 현재 포즈에서 다음 논리 상태로 이어간다.
    /// </summary>
    public sealed class MinimapBoardTimeline
    {
        public const float OpenDuration = 0.22f;
        public const float ShrinkDuration = 0.20f;
        public const float CloseDuration = 0.16f;
        public const float ScanPeriod = 3f;
        public const float RingPeriod = 2.4f;
        public const float BeaconPeriod = 1.1f;
        public const float BeaconMinimum = 0.12f;

        private MinimapBoardPose start;
        private float elapsed;
        private float duration;

        public MinimapBoardMode Mode { get; private set; } = MinimapBoardMode.Closed;
        public MinimapBoardPose Pose { get; private set; } = MinimapBoardPose.Closed(1f);
        public bool IsTransitioning => duration > 0f && elapsed < duration;
        /// <summary>표시 중에만 흐르는 장식 시계. 숨김 중에는 멈춘다.</summary>
        public float DisplayClock { get; private set; }
        public bool IsVisible => Mode != MinimapBoardMode.Closed || Pose.Open > 0f || Pose.Line > 0f || Pose.Frame > 0f;
        /// <summary>0 = 상단, 1 = 하단. 약 3초 주기로 위에서 아래로 흐른다.</summary>
        public float ScanPhase => Mathf.Repeat(DisplayClock, ScanPeriod) / ScanPeriod;
        /// <summary>위치 링 맥동 0~1.</summary>
        public float RingPulse => 0.5f + 0.5f * Mathf.Sin(DisplayClock * Mathf.PI * 2f / RingPeriod);
        /// <summary>플레이어 위치 표시등의 깜빡임 밝기(최소~1).</summary>
        public float BeaconBlink => BeaconBrightness(DisplayClock);

        public static float BeaconBrightness(float clock)
        {
            float wave = Mathf.Sin(clock * Mathf.PI * 2f / BeaconPeriod);
            float on = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(0.5f + 1.2f * wave));
            return Mathf.Lerp(BeaconMinimum, 1f, on);
        }

        public static MinimapBoardMode Next(MinimapBoardMode mode) => mode switch
        {
            MinimapBoardMode.Closed => MinimapBoardMode.Wide,
            MinimapBoardMode.Wide => MinimapBoardMode.Square,
            _ => MinimapBoardMode.Closed
        };

        public static float DurationFor(MinimapBoardMode target) => target switch
        {
            MinimapBoardMode.Wide => OpenDuration,
            MinimapBoardMode.Square => ShrinkDuration,
            _ => CloseDuration
        };

        /// <summary>M 한 번 = 한 단계. 현재 포즈를 시작점으로 삼는다.</summary>
        public MinimapBoardMode Press()
        {
            start = Pose;
            if (Mode == MinimapBoardMode.Closed && start.Open <= 0f && start.Frame <= 0f)
            {
                // 완전히 닫힌 뒤 다시 열 때는 보이지 않는 폭을 가로형으로 미리 맞춘다.
                start.Width = 1f;
            }

            Mode = Next(Mode);
            elapsed = 0f;
            duration = DurationFor(Mode);
            return Mode;
        }

        public void Advance(float unscaledDeltaTime)
        {
            float delta = Mathf.Max(0f, unscaledDeltaTime);
            if (IsTransitioning)
            {
                elapsed = Mathf.Min(duration, elapsed + delta);
                Pose = Evaluate(start, Mode, elapsed / duration);
            }

            if (IsVisible)
            {
                DisplayClock += delta;
            }
            else
            {
                DisplayClock = 0f;
            }
        }

        /// <summary>disable·초기화 시 남은 빛 없이 즉시 닫는다.</summary>
        public void ForceClosed()
        {
            Mode = MinimapBoardMode.Closed;
            Pose = MinimapBoardPose.Closed(Pose.Width);
            elapsed = 0f;
            duration = 0f;
            DisplayClock = 0f;
        }

        public static MinimapBoardPose Evaluate(MinimapBoardPose from, MinimapBoardMode target, float progress)
        {
            float u = Mathf.Clamp01(progress);
            if (u >= 1f)
            {
                return MinimapBoardPose.Settled(target, from.Width);
            }

            var pose = new MinimapBoardPose();
            switch (target)
            {
                case MinimapBoardMode.Wide:
                {
                    // 중앙 광선이 위아래로 펼쳐지고 프레임 → 지형 → 마커 순으로 점등한다.
                    pose.Open = Mathf.Lerp(from.Open, 1f, EaseOut(Remap(u, 0f, 0.55f)));
                    pose.Width = Mathf.Lerp(from.Width, 1f, EaseOut(u));
                    pose.Frame = Mathf.Lerp(from.Frame, 1f, Remap(u, 0.10f, 0.50f));
                    pose.Terrain = Mathf.Lerp(from.Terrain, 1f, Remap(u, 0.35f, 0.75f));
                    pose.Markers = Mathf.Lerp(from.Markers, 1f, Remap(u, 0.60f, 1f));
                    float beam = (1f - from.Open) * Remap(u, 0f, 0.12f) * (1f - Remap(u, 0.45f, 0.85f));
                    pose.Line = Mathf.Max(from.Line * (1f - Remap(u, 0f, 0.3f)), beam);
                    break;
                }
                case MinimapBoardMode.Square:
                {
                    float e = EaseOut(u);
                    pose.Open = Mathf.Lerp(from.Open, 1f, e);
                    pose.Width = Mathf.Lerp(from.Width, 0f, e);
                    pose.Frame = Mathf.Lerp(from.Frame, 1f, e);
                    pose.Terrain = Mathf.Lerp(from.Terrain, 1f, e);
                    pose.Markers = Mathf.Lerp(from.Markers, 1f, e);
                    pose.Line = Mathf.Lerp(from.Line, 0f, e);
                    break;
                }
                default:
                {
                    // 마커 → 지형이 먼저 꺼지고, 프레임이 얇은 수평선으로 압축된 뒤 마지막 빛이 사라진다.
                    pose.Markers = Mathf.Lerp(from.Markers, 0f, Remap(u, 0f, 0.35f));
                    pose.Terrain = Mathf.Lerp(from.Terrain, 0f, Remap(u, 0.05f, 0.5f));
                    pose.Open = Mathf.Lerp(from.Open, 0f, EaseIn(Remap(u, 0f, 0.7f)));
                    pose.Frame = Mathf.Lerp(from.Frame, 0f, Remap(u, 0.25f, 0.7f));
                    pose.Width = from.Width;
                    float peak = Mathf.Max(from.Line, from.Open);
                    pose.Line = Mathf.Lerp(from.Line, peak, Remap(u, 0f, 0.3f)) * (1f - Remap(u, 0.7f, 1f));
                    break;
                }
            }

            return pose;
        }

        public static float Remap(float value, float from, float to) =>
            to <= from ? (value >= to ? 1f : 0f) : Mathf.Clamp01((value - from) / (to - from));

        public static float EaseOut(float x)
        {
            float inv = 1f - Mathf.Clamp01(x);
            return 1f - inv * inv * inv;
        }

        public static float EaseIn(float x)
        {
            float v = Mathf.Clamp01(x);
            return v * v;
        }
    }
}
