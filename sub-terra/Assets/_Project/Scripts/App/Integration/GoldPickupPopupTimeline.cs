using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>한 줄(슬롯)의 낙하 모습. 오프셋은 슬롯 중앙 기준 디자인 px, 위쪽이 +.</summary>
    public readonly struct GoldPickupRowMotion
    {
        public float OffsetY { get; }
        public float ScaleX { get; }
        public float ScaleY { get; }
        /// <summary>모션블러 세기 0~1. 0이면 잔상은 꺼진다.</summary>
        public float Blur { get; }

        public GoldPickupRowMotion(float offsetY, float scaleX, float scaleY, float blur)
        {
            OffsetY = offsetY;
            ScaleX = scaleX;
            ScaleY = scaleY;
            Blur = blur;
        }
    }

    /// <summary>
    /// 골드 획득 팝업 시간표(순수 계산, prompt-B 142). View는 시간만 넘기고 값은 여기서 받는다.
    /// 합치기 없는 기준: 0.00~0.14 프레임 등장 / 0.04~0.13 레버 하강 / 0.13~0.22 레버 복귀·정착 /
    /// 0.13~0.25 기본 낙하(0.25 착지) / 0.28~0.40 추가 낙하(0.40 착지) /
    /// 기본만 0.68초, 추가 포함 0.83초부터 마지막 0.22초 동안 떠오르며 퇴장(0.90 / 1.05초).
    /// </summary>
    public static class GoldPickupPopupTimeline
    {
        // 디자인 px (월드 1칸 = 100px, 시설 이름표와 같은 비율)
        public const float BodyWidth = 200f;
        public const float LeverZoneWidth = 56f;
        public const float FrameWidth = BodyWidth + LeverZoneWidth;
        public const float MinSpreadWidth = 44f;
        public const float BaseHeight = 44f;
        public const float BonusHeight = 72f;
        public const float MainSlotHeight = 36f;
        public const float BonusSlotHeight = 22f;
        public const float EdgeThickness = 1.5f;

        // 프레임·레버
        public const float FrameDuration = 0.14f;
        public const float LeverPullStart = 0.04f;
        public const float LeverPullEnd = 0.13f;
        public const float LeverReturnEnd = 0.22f;
        public const float LeverPullAngle = 150f;

        // 낙하
        public const float BaseFallStart = 0.13f;
        public const float FallDuration = 0.12f;
        public const float BaseFallEnd = BaseFallStart + FallDuration;
        public const float BonusFallDelay = 0.15f;
        public const float BonusFallStart = BaseFallStart + BonusFallDelay;
        public const float BonusFallEnd = BonusFallStart + FallDuration;
        // 나중에 추가가 처음 생기면 높이 확장이 끝난 뒤 떨어진다.
        public const float LateBonusFallDelay = 0.10f;
        public const float LandOvershoot = 3f;
        public const float SettleDuration = 0.06f;
        public const float BlurClearDuration = 0.05f;
        public const float StretchY = 1.3f;
        public const float StretchX = 0.94f;

        // 모션블러 잔상
        public const int MainGhostCount = 3;
        public const int BonusGhostCount = 2;
        public const float MainGhostSpacing = 9f;
        public const float BonusGhostSpacing = 5f;
        public const float GhostAlphaBase = 0.5f;
        public const float GhostAlphaDecay = 0.6f;

        // 퇴장·합치기
        public const float ExitLead = 0.43f;
        public const float ExitDuration = 0.22f;
        public const float ExitRise = 0.15f;
        public const float MergeHold = 0.45f;
        public const float MergeRollDuration = 0.22f;
        public const float MergeRestoreDuration = 0.06f;
        public const float ExpandDuration = 0.1f;

        // 착지 반응
        public const float EdgeFlashDuration = 0.12f;
        public const float SweepDuration = 0.18f;

        // 금화
        public const int MaxAliveCoins = 24;
        public const float CoinLife = 0.5f;
        public const float CoinGravity = 1500f;
        public const float CoinLaunchSpeed = 300f;
        public const float CoinFanHalfAngle = 55f;
        public const float CoinStagger = 0.012f;
        public const float CoinPixelSize = 16f;

        public static float EaseOutCubic(float k)
        {
            k = Mathf.Clamp01(k);
            float inv = 1f - k;
            return 1f - inv * inv * inv;
        }

        public static float EaseInOut(float k)
        {
            k = Mathf.Clamp01(k);
            return k * k * (3f - 2f * k);
        }

        /// <summary>한 번만 살짝 넘쳤다가 1에 정착한다. 반복 바운스는 없다.</summary>
        public static float EaseOutBack(float k)
        {
            const float c1 = 1f;
            float u = Mathf.Clamp01(k) - 1f;
            return 1f + (c1 + 1f) * u * u * u + c1 * u * u;
        }

        // ---------- 프레임·레버 ----------

        public static float FrameLevel(float age)
        {
            return Mathf.Clamp01(age / FrameDuration);
        }

        /// <summary>추가 줄이 생긴 뒤 프레임 높이. 처음 한 번만 ease-out으로 확장한다.</summary>
        public static float FrameHeight(bool hasBonus, float sinceBonusStart)
        {
            if (!hasBonus)
            {
                return BaseHeight;
            }

            return Mathf.Lerp(BaseHeight, BonusHeight, EaseOutCubic(sinceBonusStart / ExpandDuration));
        }

        /// <summary>레버 각도(도). 0이 위로 선 상태, 음수는 시계 방향으로 아래까지 당겨진 정도.</summary>
        public static float LeverAngle(float age)
        {
            if (age <= LeverPullStart || age >= LeverReturnEnd)
            {
                return 0f;
            }

            if (age < LeverPullEnd)
            {
                float pull = (age - LeverPullStart) / (LeverPullEnd - LeverPullStart);
                return -LeverPullAngle * EaseInOut(pull);
            }

            float back = (age - LeverPullEnd) / (LeverReturnEnd - LeverPullEnd);
            return -LeverPullAngle * (1f - EaseOutBack(back));
        }

        // ---------- 숫자 낙하 ----------

        /// <summary>fallTime: 낙하 시작 후 경과 시간(음수면 시작 전).</summary>
        public static GoldPickupRowMotion EvaluateRow(float fallTime, float slotHeight)
        {
            if (fallTime <= 0f)
            {
                return new GoldPickupRowMotion(slotHeight, 1f, 1f, 0f);
            }

            if (fallTime < FallDuration)
            {
                // ease-in 낙하. 속도는 진행도에 비례하므로 블러도 진행도를 따른다.
                float k = fallTime / FallDuration;
                return Stretch(slotHeight * (1f - k * k), k);
            }

            float since = fallTime - FallDuration;
            if (since >= SettleDuration)
            {
                return new GoldPickupRowMotion(0f, 1f, 1f, 0f);
            }

            // 착지 후 2~4px 눌렸다가 정착 위치로 복귀.
            float overshoot = -LandOvershoot * Mathf.Sin(Mathf.PI * since / SettleDuration);
            float blur = Mathf.Clamp01(1f - since / BlurClearDuration);
            return Stretch(overshoot, blur);
        }

        private static GoldPickupRowMotion Stretch(float offsetY, float blur)
        {
            return new GoldPickupRowMotion(
                offsetY,
                1f - (1f - StretchX) * blur,
                1f + (StretchY - 1f) * blur,
                blur);
        }

        /// <summary>잔상 i의 위쪽 간격(px). 낙하 속도(블러)에 비례한다.</summary>
        public static float GhostOffset(int ghost, float blur, float spacing)
        {
            return blur <= 0f ? 0f : (ghost + 1) * spacing * blur;
        }

        /// <summary>잔상 i의 알파. 블러에 비례하고 뒤로 갈수록 단계적으로 줄어든다.</summary>
        public static float GhostAlpha(int ghost, float blur)
        {
            return blur <= 0f ? 0f : GhostAlphaBase * blur * Mathf.Pow(GhostAlphaDecay, ghost);
        }

        // ---------- 착지 반응 ----------

        public static float EdgeFlash(float sinceLanding)
        {
            if (sinceLanding < 0f || sinceLanding >= EdgeFlashDuration)
            {
                return 0f;
            }

            return 1f - sinceLanding / EdgeFlashDuration;
        }

        public static float SweepStrength(float sinceStart)
        {
            if (sinceStart < 0f || sinceStart >= SweepDuration)
            {
                return 0f;
            }

            return Mathf.Sin(Mathf.PI * sinceStart / SweepDuration);
        }

        /// <summary>프레임 둘레를 시계 방향으로 도는 빛의 위치. 원점은 프레임 하단 중앙.</summary>
        public static Vector2 PerimeterPoint(float progress, float width, float height, out bool horizontal)
        {
            float length = 2f * (width + height);
            float dist = Mathf.Clamp01(progress) * length;
            float half = width * 0.5f;
            if (dist < width)
            {
                horizontal = true;
                return new Vector2(-half + dist, height);
            }

            dist -= width;
            if (dist < height)
            {
                horizontal = false;
                return new Vector2(half, height - dist);
            }

            dist -= height;
            if (dist < width)
            {
                horizontal = true;
                return new Vector2(half - dist, 0f);
            }

            dist -= width;
            horizontal = false;
            return new Vector2(-half, Mathf.Min(dist, height));
        }

        // ---------- 퇴장 ----------

        public static float ExitAlpha(float exitAmount)
        {
            return 1f - Mathf.Clamp01(exitAmount);
        }

        /// <summary>퇴장 상승량(월드 단위). 플레이어 추적 위치와 분리해 더한다.</summary>
        public static float ExitRiseWorld(float exitAmount)
        {
            return ExitRise * Mathf.Clamp01(exitAmount);
        }

        // ---------- 금화 ----------

        /// <summary>F(amount) = Clamp(3 + 2 × floor(log10(max(amount, 1))), 3, 8).</summary>
        public static int CoinCountFor(int amount)
        {
            int floorLog = 0;
            long value = amount < 1 ? 1 : amount;
            while (value >= 10)
            {
                value /= 10;
                floorLog++;
            }

            return Mathf.Clamp(3 + 2 * floorLog, 3, 8);
        }

        public static int BaseLandingCoinCount(int baseGold)
        {
            return CoinCountFor(baseGold);
        }

        public static int BonusLandingCoinCount(int bonusGold)
        {
            return Mathf.Clamp(CoinCountFor(bonusGold) + 2, 4, 10);
        }

        /// <summary>합치기 증가분 기준 금화 수. 유효 증가분이 0 이하이면 분출하지 않는다.</summary>
        public static int MergeCoinCount(int effectiveIncrease)
        {
            return effectiveIncrease <= 0 ? 0 : Mathf.Clamp(CoinCountFor(effectiveIncrease) - 1, 2, 5);
        }

        /// <summary>살아 있는 금화가 상한을 넘지 않도록 새로 만들 수를 제한한다.</summary>
        public static int LimitCoinSpawn(int alive, int requested)
        {
            if (requested <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(MaxAliveCoins - Mathf.Max(0, alive), 0, requested);
        }

        /// <summary>인덱스 기반 결정적 분출 패턴. 수직 기준 부채꼴로 퍼진다(+x 오른쪽).</summary>
        public static void CoinLaunch(int index, int count, out float vx, out float vy, out float delay)
        {
            index = Mathf.Max(0, index);
            count = Mathf.Max(1, count);
            float t = count <= 1 ? 0.5f : Mathf.Clamp01(index / (float)(count - 1));
            float jitter = ((index * 37 + 11) % 13) / 12f * 2f - 1f;
            float angle = (Mathf.Lerp(-CoinFanHalfAngle, CoinFanHalfAngle, t) + jitter * 6f) * Mathf.Deg2Rad;
            float speed = CoinLaunchSpeed + 45f * (index % 3);
            vx = Mathf.Sin(angle) * speed;
            vy = Mathf.Cos(angle) * speed;
            delay = index * CoinStagger;
        }

        public static Vector2 CoinPosition(Vector2 origin, float vx, float vy, float localTime)
        {
            float t = Mathf.Max(0f, localTime);
            return new Vector2(
                origin.x + vx * t,
                origin.y + vy * t - 0.5f * CoinGravity * t * t);
        }

        public static float CoinSize(int index)
        {
            return CoinPixelSize * Mathf.Lerp(1.1f, 0.85f, (Mathf.Max(0, index) % 3) / 2f);
        }
    }
}
