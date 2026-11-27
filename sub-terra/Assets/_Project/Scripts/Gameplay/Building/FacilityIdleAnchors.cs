using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    public enum FacilityIdleKind
    {
        None,
        Light,
        Charger,
        Clinic,
        Storage,
        Settlement,
        Outpost,
        Portal
    }

    /// <summary>
    /// B-214: 시설 주기 연출의 자리·길이 한 표. 자리는 FacilityGroundedVisual.GetVisibleBounds 사각형에 대한
    /// 비율(x: 왼쪽 0 → 오른쪽 1, y: 아래 0 → 위 1)이고 크기는 보이는 폭 비율이다. PM이 조정할 숫자는 여기 모은다.
    /// </summary>
    public static class FacilityIdleAnchors
    {
        // ★ 시설별 연출 길이(초)
        public const float LightSeconds = 1.0f;
        public const float ChargerSeconds = 2.6f;
        public const float ClinicSeconds = 2.8f;
        public const float StorageSeconds = StorageOpenFrames / StorageOpenFps;   // 영상 56프레임 ÷ 12fps ≈ 4.67초
        public const float SettlementSeconds = 1.6f;
        public const float OutpostSeconds = 2.0f;

        // 조명: 등불 중심, 밝기 최저값(0.35까지), 덧그림 크기
        public static readonly Vector2 LightLantern = new(0.76f, 0.80f);
        public const float LightMinBrightness = 0.35f;
        public const float LightOverlayWidth = 0.34f;
        public const float LightOverlayMaxAlpha = 0.8f;

        // 충전기·보건소 공통: 아이콘(충전 완료창 번개 / 회복창 하트)이 빈 채로 나타나
        // 아래에서 위로 액체가 차오르며 원래 색으로 돌아온다. 크기는 보이는 폭 비율, 높이는 칸 단위.
        public const float IconWidth = 0.34f;                 // ★ 아이콘 크기(보이는 폭 대비)
        public const float IconLift = 0.42f;                  // ★ 시설 꼭대기에서 아이콘 중심까지(칸)
        public const float IconRise = 0.28f;                  // ★ 연출 동안 위로 떠오르는 거리(칸)
        public const float IconAppearSeconds = 0.25f;         // 빈 아이콘이 톡 나타나는 시간
        public const float IconFillStart = 0.25f;             // 액체가 차오르기 시작하는 시각
        public const float ChargerFillEnd = 1.55f;            // ★ 충전기: 가득 차는 시각(짧고 날카롭게)
        public const float ClinicFillEnd = 1.75f;             // ★ 보건소: 가득 차는 시각(부드럽게)
        public const float IconCompleteSeconds = 0.35f;       // 가득 찬 뒤 번쩍이며 튀는 시간
        public const float IconFadeSeconds = 0.45f;           // 끝에서 옅어지는 시간
        public static readonly Vector2 ChargerIcon = new(0.39f, 0.60f);   // 차양 아래, 기둥과 충전 화면 사이
        public const float ChargerIconRise = 0.15f;                       // 보이는 높이 대비 떠오르는 거리
        public static readonly Vector2 ChargerScreen = new(0.55f, 0.54f); // 번개 화면(차오르는 동안 같이 빛남)
        public static readonly Vector2 ClinicTower = new(0.50f, 0.99f);   // 가운데 탑 꼭대기
        public static readonly Vector2 ClinicCross = new(0.50f, 0.72f);   // 십자 간판(심장 박동에 맞춰 빛남)

        // 보관함: 사용자 영상(work_process/video/storage_open.mp4)의 배경을 지운 프레임 시트.
        // 셀은 StorageGrounded 보이는 영역(GetVisibleBounds)에 대한 비율 사각형을 그대로 덮는다(뚜껑이 들리는 위쪽 여백 포함).
        public const int StorageOpenFrames = 56;
        public const float StorageOpenFps = 12f;
        public const int StorageOpenColumns = 7;
        public const int StorageOpenCellWidth = 276;
        public const int StorageOpenCellHeight = 212;
        public const int SheetPadding = 2;                    // 셀 사이 투명 여백(바이리니어 번짐 방지)
        public static readonly Rect StorageOpenCellRatio = Rect.MinMaxRect(-0.00831f, -0.01538f, 1.01108f, 1.07179f);

        // 아이콘 차오름 시트: 0 = 빈 껍데기, 1~23 = 아래에서 위로 차오름, 24 = 원래 아이콘
        public const int IconFillFrames = 25;
        public const int IconFillColumns = 5;
        public const int IconFillCell = 160;

        // 자원 정산기: 화면 중심, 화면 폭(보이는 폭 대비), 금화 최대 크기(화면 폭 대비)
        public static readonly Vector2 SettlementScreen = new(0.50f, 0.70f);
        // 연출 동안 화면 안쪽을 어두운 청록으로 덮어 원래 화면 아이콘이 도트와 섞이지 않게 한다(화면 폭·높이 대비).
        public static readonly Vector2 SettlementPanelCenter = new(0.50f, 0.672f);
        public static readonly Vector2 SettlementPanelSize = new(0.60f, 0.30f);
        public const float SettlementScreenWidth = 0.64f;
        public const float SettlementCoinScreenRatio = 0.45f;

        // 전진기지: 돔 꼭대기(와이파이), 중심(파형), 테두리 LED(그림에서 잰 12곳)
        public static readonly Vector2 OutpostDome = new(0.50f, 0.97f);
        public static readonly Vector2 OutpostCenter = new(0.50f, 0.40f);
        public const float OutpostWifiRise = 0.3f;            // 칸 단위
        public static readonly Vector2[] OutpostLeds =
        {
            new(0.092f, 0.085f), new(0.050f, 0.178f), new(0.082f, 0.366f),
            new(0.146f, 0.548f), new(0.209f, 0.689f), new(0.334f, 0.836f),
            new(0.908f, 0.085f), new(0.950f, 0.178f), new(0.918f, 0.366f),
            new(0.854f, 0.548f), new(0.791f, 0.689f), new(0.666f, 0.836f)
        };

        // 포탈: 홀 중심, 홀 반지름(폭 대비)
        // 문서의 0.52는 실제 홀 중심보다 높았다. PortalInactive PNG 투명 영역에서 잰 홀 중심은 (0.498, 0.469), 반지름은 폭의 0.368이다.
        public static readonly Vector2 PortalHole = new(0.50f, 0.47f);
        public const float PortalHoleRadius = 0.368f;

        public static FacilityIdleKind KindFor(string buildingId)
        {
            switch (buildingId)
            {
                case "building.light.basic": return FacilityIdleKind.Light;
                case "building.charger.basic": return FacilityIdleKind.Charger;
                case "building.clinic.basic": return FacilityIdleKind.Clinic;
                case "building.storage.basic": return FacilityIdleKind.Storage;
                case "building.settlement.basic": return FacilityIdleKind.Settlement;
                case "building.outpost_core.basic": return FacilityIdleKind.Outpost;
                case "building.escape_portal.emergency": return FacilityIdleKind.Portal;
                default: return FacilityIdleKind.None;
            }
        }

        public static float DurationOf(FacilityIdleKind kind)
        {
            switch (kind)
            {
                case FacilityIdleKind.Light: return LightSeconds;
                case FacilityIdleKind.Charger: return ChargerSeconds;
                case FacilityIdleKind.Clinic: return ClinicSeconds;
                case FacilityIdleKind.Storage: return StorageSeconds;
                case FacilityIdleKind.Settlement: return SettlementSeconds;
                case FacilityIdleKind.Outpost: return OutpostSeconds;
                default: return 0f;
            }
        }

        /// <summary>디버그 명령 이름(light|charger|clinic|storage|settlement|outpost).</summary>
        public static bool TryParseKind(string token, out FacilityIdleKind kind)
        {
            kind = FacilityIdleKind.None;
            if (string.IsNullOrWhiteSpace(token)) return false;
            switch (token.Trim().ToLowerInvariant())
            {
                case "light": case "조명": kind = FacilityIdleKind.Light; return true;
                case "charger": case "충전기": kind = FacilityIdleKind.Charger; return true;
                case "clinic": case "보건소": kind = FacilityIdleKind.Clinic; return true;
                case "storage": case "보관함": kind = FacilityIdleKind.Storage; return true;
                case "settlement": case "정산기": kind = FacilityIdleKind.Settlement; return true;
                case "outpost": case "전진기지": kind = FacilityIdleKind.Outpost; return true;
                default: return false;
            }
        }

        /// <summary>아이콘이 차오른 정도(0 빈 껍데기 ~ 1 가득). 천천히 시작해 가속했다가 끝에서 살짝 늦춘다.</summary>
        public static float IconFillLevel(float t, float fillEnd)
        {
            float x = Mathf.Clamp01((t - IconFillStart) / Mathf.Max(0.01f, fillEnd - IconFillStart));
            return x * x * (3f - 2f * x);
        }

        /// <summary>차오름 정도에 맞는 시트 프레임. 0은 빈 껍데기, 마지막은 원래 아이콘(가득 찼을 때만).</summary>
        public static int IconFillFrame(float level)
        {
            if (level <= 0f) return 0;
            if (level >= 1f) return IconFillFrames - 1;
            return Mathf.Clamp(1 + Mathf.FloorToInt(level * (IconFillFrames - 2)), 1, IconFillFrames - 2);
        }

        /// <summary>보관함 영상 프레임(12fps, 마지막 프레임에서 멈춘다).</summary>
        public static int StorageOpenFrame(float t)
            => Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, t) * StorageOpenFps), 0, StorageOpenFrames - 1);

        // 시간 곡선은 EditMode 테스트로 검증할 수 있게 순수 함수로 둔다.
        private static readonly Vector2[] LightKeys =
        {
            new(0f, 1f), new(0.06f, LightMinBrightness), new(0.16f, 0.9f), new(0.24f, 0.45f),
            new(0.34f, 0.95f), new(0.44f, LightMinBrightness), new(0.60f, 0.7f), new(0.80f, 1f), new(1f, 1f)
        };

        /// <summary>조명 밝기(0.35~1). 2~3번 불규칙하게 떨어졌다가 다시 환해진다. u는 0~1.</summary>
        public static float LightBrightness(float u)
        {
            u = Mathf.Clamp01(u);
            for (int i = 1; i < LightKeys.Length; i++)
            {
                if (u > LightKeys[i].x) continue;
                Vector2 a = LightKeys[i - 1], b = LightKeys[i];
                return Mathf.Lerp(a.y, b.y, Mathf.InverseLerp(a.x, b.x, u));
            }
            return 1f;
        }
    }
}
