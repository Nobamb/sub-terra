using UnityEngine;

namespace SubTerra.App.UI.Inventory
{
    /// <summary>
    /// 인벤토리 도움말 ?의 호버 연출 곡선(순수 계산). MonoBehaviour 없이 EditMode에서 검사한다.
    /// 아이콘: 호버하면 원판 중앙에서 글로우가 천천히 차오르고 은은하게 숨 쉰다.
    /// 설명창: 연결선 → 가로 빛줄기 → 위아래로 열림 → 한 번 깜빡 → 글자가 찍힌다(조작 단말 화면 켜짐과 같은 순서).
    /// </summary>
    public static class InventoryHelpMotion
    {
        public const float GlowRiseSeconds = 0.7f;
        public const float GlowFallSeconds = 0.3f;
        public const float GlowPulsePeriod = 2.4f;
        public const float GlowPulseDepth = 0.22f;
        public const float GlowMaxAlpha = 0.4f;
        public const float GlowStartScale = 0.5f;

        public const float LineDuration = 0.10f;
        public const float OpenDuration = 0.30f;
        public const float ContentStart = 0.24f;
        public const float ContentFadeDuration = 0.16f;
        public const float TypeStart = 0.26f;
        public const float TypeDuration = 0.34f;
        public const float CloseDuration = 0.10f;
        public const float CloseRecoverSpeed = 3f;
        public const float MinScaleY = 0.012f;
        public const float FlickerStart = 0.19f;
        public const float FlickerEnd = 0.24f;
        public const float FlickerAlpha = 0.4f;
        public const float ScanPeriod = 1.6f;
        public const float ShimmerAmount = 0.05f;
        public const float ShimmerSpeed = 6f;

        /// <summary>글로우 단계(0~1)와 숨쉬기 시간으로 만든 최종 밝기(0~1).</summary>
        public static float GlowIntensity(float level, float pulseTime)
        {
            var rise = Smooth(Mathf.Clamp01(level));
            var pulse = GlowPulseDepth * 0.5f * (1f - Mathf.Cos(Mathf.PI * 2f * pulseTime / GlowPulsePeriod));
            return rise * (1f - pulse);
        }

        /// <summary>글로우 단계에 따른 크기 배율. 중앙에서 퍼지듯 0.5→1.</summary>
        public static float GlowScale(float level)
        {
            return Mathf.Lerp(GlowStartScale, 1f, Smooth(Mathf.Clamp01(level)));
        }

        /// <summary>열린 뒤 경과 시간 → 가로 펼침(0 → 1). 처음 0.1초에 수평 빛줄기가 뻗는다.</summary>
        public static float ScaleX(float elapsed)
        {
            return EaseOut(Range(elapsed, 0f, LineDuration));
        }

        /// <summary>열린 뒤 경과 시간 → 세로 펼침. 빛줄기(얇은 선)에서 시작해 위아래로 열린다.</summary>
        public static float ScaleY(float elapsed)
        {
            return Mathf.Lerp(MinScaleY, 1f, EaseOut(Range(elapsed, LineDuration, OpenDuration)));
        }

        /// <summary>열린 뒤 경과 시간 → 투명도. 한 번 깜빡이고, 다 열리면 살짝 일렁인다.</summary>
        public static float Alpha(float elapsed)
        {
            var fade = Smooth(Range(elapsed, 0f, 0.06f));
            if (elapsed >= FlickerStart && elapsed < FlickerEnd)
            {
                return fade * FlickerAlpha;
            }

            if (elapsed < OpenDuration)
            {
                return fade;
            }

            return 1f - ShimmerAmount * 0.5f * (1f + Mathf.Sin((elapsed - OpenDuration) * ShimmerSpeed));
        }

        /// <summary>내용(제목·설명) 투명도. 프레임이 거의 열린 뒤에 나타난다.</summary>
        public static float ContentAlpha(float elapsed)
        {
            return Smooth(Range(elapsed, ContentStart, ContentStart + ContentFadeDuration));
        }

        /// <summary>보이는 글자 수. 프레임이 열린 직후부터 앞에서부터 찍힌다.</summary>
        public static int VisibleCharacters(float elapsed, int length)
        {
            if (length <= 0)
            {
                return 0;
            }

            var t = Range(elapsed, TypeStart, TypeStart + TypeDuration);
            return Mathf.Clamp(Mathf.CeilToInt(t * length), 0, length);
        }

        /// <summary>주사선 위치(0=위, 1=아래). 다 열린 뒤에만 흐른다.</summary>
        public static float ScanPosition(float elapsed)
        {
            var since = elapsed - OpenDuration;
            return since <= 0f ? 0f : Mathf.Repeat(since, ScanPeriod) / ScanPeriod;
        }

        /// <summary>꺼지는 중 남은 투명도 비율(1→0).</summary>
        public static float CloseAlpha(float closeElapsed)
        {
            return 1f - Mathf.Clamp01(closeElapsed / CloseDuration);
        }

        /// <summary>꺼지는 중 진행(0→1). 열릴 때와 반대로 세로로 접힌다.</summary>
        public static float CloseProgress(float closeElapsed)
        {
            return Mathf.Clamp01(closeElapsed / CloseDuration);
        }

        private static float Range(float value, float from, float to)
        {
            return Mathf.Clamp01((value - from) / (to - from));
        }

        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private static float EaseOut(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }
    }

    /// <summary>
    /// 호버 한 번의 진행 상태. 아이콘 글로우와 설명창 열림·닫힘을 함께 들고 있다.
    /// 닫는 중에 다시 올리면 닫던 길을 빠르게 되감아 이어 가므로 투명도가 튀지 않는다.
    /// </summary>
    public sealed class InventoryHelpHoverState
    {
        private bool hovering;
        private bool shown;
        private float open;
        private float close;
        private float glowLevel;
        private float pulseTime;

        public bool IsHovering => hovering;
        /// <summary>설명창을 켜 둬야 하는 동안 true(호버 중이거나 닫는 모션 중).</summary>
        public bool IsShown => shown;
        public bool IsClosing => shown && !hovering;
        public float OpenElapsed => open;
        public float CloseElapsed => close;
        public float GlowLevel => glowLevel;
        public float GlowIntensity => InventoryHelpMotion.GlowIntensity(glowLevel, pulseTime);
        public float GlowScale => InventoryHelpMotion.GlowScale(glowLevel);
        public bool IsSettled => !hovering && !shown && glowLevel <= 0f;

        public void Enter()
        {
            hovering = true;
            if (!shown)
            {
                shown = true;
                open = 0f;
                close = 0f;
            }
        }

        public void Exit()
        {
            hovering = false;
        }

        /// <summary>움직임 줄이기: 글로우와 창을 한 번에 다 켠다.</summary>
        public void CompleteOpen()
        {
            Enter();
            open = InventoryHelpMotion.TypeStart + InventoryHelpMotion.TypeDuration + InventoryHelpMotion.OpenDuration;
            close = 0f;
            glowLevel = 1f;
            pulseTime = 0f;
        }

        public void Reset()
        {
            hovering = false;
            shown = false;
            open = 0f;
            close = 0f;
            glowLevel = 0f;
            pulseTime = 0f;
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            if (hovering)
            {
                if (close > 0f)
                {
                    close = Mathf.Max(0f, close - deltaSeconds * InventoryHelpMotion.CloseRecoverSpeed);
                }
                else
                {
                    open += deltaSeconds;
                }

                glowLevel = Mathf.Min(1f, glowLevel + deltaSeconds / InventoryHelpMotion.GlowRiseSeconds);
                pulseTime += deltaSeconds;
                return;
            }

            if (shown)
            {
                close += deltaSeconds;
                if (close >= InventoryHelpMotion.CloseDuration)
                {
                    shown = false;
                    open = 0f;
                    close = 0f;
                }
            }

            glowLevel = Mathf.Max(0f, glowLevel - deltaSeconds / InventoryHelpMotion.GlowFallSeconds);
            if (glowLevel <= 0f)
            {
                pulseTime = 0f;
            }
        }
    }
}
