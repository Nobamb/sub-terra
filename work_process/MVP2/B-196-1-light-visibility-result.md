# B-196-1 결과: 켜진 조명이 주변을 분명히 밝히게

- 기준: `MVP2-fix140` `7a2362d5` (PR #328). 커밋·push는 아직 하지 않았다(사용자 동의 대기). 커밋 해시: 미정.

## 바꾼 것
| 항목 | 파일 | 내용 |
|---|---|---|
| A. 후광 | `Scripts/Gameplay/Hazards/LightHaloVisual.cs`(신규), `Resources/LightHalo.shader`(신규, 가산) | 조명 시설 루트에 `LightHalo` 자식. `gas-soft-radial` 스프라이트 지름 = 판정 반지름×2(5칸), 가산 공유 머티리얼(런타임 1개), 색 (1,0.74,0.34), 최대 알파 0.50, sortingOrder 23. 전원 off → 0.15초 페이드 후 렌더러 off, 켜진 채 화면 밖이거나 프레임당 32개 초과면 렌더러 off |
| 전원 판정 | `GasVisionClearanceSource.IsLit` | `PowerFacility.IsInteractionAvailable`(PowerGlow와 같은 기준). 시설이 없으면 항상 true. 시각 전용, `IsCleared`는 안 읽음. `Hazards.asmdef`에 `Power` 참조 추가 |
| B. 암전 구멍 | `Resources/DepthDarknessOverlayUI.shader`, `DepthDarknessOverlayController` | 켜진 화면 안 조명(최대 32, 검사기 `maxLightHoles`)마다 `smoothstep` 구멍. 중심에서 암전을 85%(`lightHoleStrength`) 걷고 반지름 20%(`lightHoleInnerFraction`)부터 감쇠, 반지름 밖은 0. `IsClearedByLight`(플레이어 5칸 안이면 전체 해제)는 그대로 |
| C. 베일 중심 | `GasVisualRules.LightHoleCenterOpacity`=**0.35**, `GasVisionWorldVeil`/`GasVisionOverlayDriver.lightHoleCenterAlpha`, `GasVisionHoleEvaluator.Sample` | 예전 5% → 35%. `LightHoleInnerFraction` 0.35 부드러운 가장자리·가스 안 추가분 +0.08 유지. `GasClearance` 스프라이트 틴트(0.05)는 그대로 |
| D. 깜빡임 | `GasVisualMath.HaloFlicker` | 최대 6% 감쇠, 부드러운 사인 합. `AccessibilityPreferences.ReduceMotion`이면 정확히 1(고정)이고 페이드도 즉시 전환. 빛·구멍·가스는 그대로 |
| 공통 | `GasVisionHoleEvaluator.CopyActiveLights/CopyViewportLights/TryGetCameraWorldView` | 베일·가스 오버레이·암전이 같은 헬퍼 사용: 켜진 조명만, 화면 안만, 32개 상한 |
| 빌더 | `PromptB50GasVisionBuilder.ApplyLightHalo` | `ApplyLightClearance`가 `LightHalo`도 만듦(B49 빌더 경로 포함). `ApplyLightPrefab`만 재실행해 `LightFacility.prefab` 갱신. 런타임에도 `GasVisionClearanceSource.OnEnable`이 후광이 없으면 만든다(중복 생성 안 함) |

## 판정 유지
- `GasZone.Radius/Contains`, `CircleCollider2D`, `GasRadiusBlocks`·`LightClearRadiusBlocks`(5), 생성 1초·접근 페이드, `TryGetNearestLight/IsCleared/IsClearedByLight`, 피해·감속·노출은 수정하지 않았다. 테스트로 5칸 안 true / 5.1칸 false 확인.
- 후광·구멍은 모두 반지름 안쪽에서만 감쇠한다(반지름에서 0).
- **주의(판단 필요)**: 전원이 꺼진 조명은 예전엔 판정만 켜지고 시각은 5% 붉은 구멍이 있었다. 이제 시각 구멍·후광은 전원 off면 없다(판정은 그대로). 요구사항 "전원 off면 사라짐"을 따른 것이다.

## 테스트
- 신규 `LightVisibilityTests` 9개 통과: 판정 불변, 전원 off 시 후광·구멍 없음, 5칸 밖 구멍 0, 베일 중심 0.25~0.45, 화면 밖·상한(32), 후광 지름 ≤ 판정 지름, 모션 감소 시 알파 고정, 셰이더 속성 존재.
- 기존 `GasRealisticVisualTests`·`GasVisionHoleEvaluatorTests`를 새 중심 알파(0.35)에 맞게 수정, 통과.
- EditMode 전체: Pass 2705 / Fail 27. 실패 27개는 터미널·지형·스냅샷·UI 레이아웃 계열로 이번 변경 파일과 무관하다. 변경 전(23:08) 실행에도 같은 계열(PromptB125Terminal, IntegrationWiringTests)이 실패했다. 전체 목록의 전/후 diff는 하지 않았다.
- 셰이더 컴파일: `DepthDarknessOverlayUI`, `LightHalo` supported, 오류 없음.
- 임시 카메라 렌더 1장: 램프 주위 따뜻한 원형 후광, 가장자리 부드럽게 사라짐(판정 반지름 안). 확인함.

## 미검증 (직접 플레이 필요)
- Bootstrap부터 실제 플레이에서: 얕은 곳 후광, 전원 on/off, 가스 근처 구멍 중심, 가스 안 녹색·탁한 시야, 깊이 10m+ 조명 밖에서 램프 주위 밝기, 5칸 안 전체 해제, 모션 감소 설정, Console 새 오류.
- 깊은 곳 암전 구멍의 실제 화면(셰이더 렌더)은 눈으로 못 봤다. 컴파일·속성 존재만 확인.
- 빌더 전체 `Build()`(씬 포함) 재실행은 하지 않았다. 프리팹만 재적용했다.
- `Mine_Demo_Integration.unity`는 수정하지 않았다. 새 직렬화 필드(`lightHoleCenterAlpha`, 암전 구멍 3종)는 코드 기본값이 적용되며, 씬을 다시 저장하면 값이 기록된다.
- 에디터가 테스트 실행 중 씬·폰트·UI 프리팹·ProjectSettings 등을 다시 저장해 dirty가 생겼으나 `git restore`로 되돌렸다.

## 사용자 테스트 가이드
`init/prompt-B.md`의 B-196-1 사용자 테스트 가이드 1~8번과 같다. 추가로: 5번에서 조명이 켜진 상태여야 하며(전원 연결), 후광 세기·암전 구멍 세기는 `LightHaloVisual` / `DepthDarknessOverlayController` 검사기에서 조절한다.

## 추가 보강 (사용자 피드백: 연출이 너무 미미함)
- 원인: 실제 게임의 조명은 전원 없이도 가스 시야를 뚫는다(`IsCleared`, GameplayEventBridge 주석 "조명은 범위와 무관하게 동작"). 그런데 후광·구멍을 케이블 전원(`PowerFacility.IsInteractionAvailable`)에만 묶어 전원을 연결하지 않은 조명은 예전 5% 붉은 틴트만 보였다.
- 수정: `GasVisionClearanceSource.LightLevel` 추가. 케이블 전원이거나 공급 범위 안이면 1, 아니면 `unpoweredLevel`(기본 0.6)로 후광·구멍이 약하게 유지된다. 0으로 두면 예전처럼 꺼질 때 완전히 사라진다.
- 수치 상향: 후광 최대 알파 0.50 → 0.80, 베일/오버레이 구멍 중심 알파 0.35 → 0.45(권장 범위 상단), 암전 구멍 세기 0.85 → 0.92.
- 테스트: `UnwiredLightStillGlowsDimlyBecauseItClearsGasWithoutPower` 추가, 중심 알파 상한 검사 갱신. EditMode Pass 2707 / Fail 26(무관 계열).
- 프리팹 `LightFacility.prefab`을 빌더(`ApplyLightPrefab`)로 재적용(peakOpacity 0.8, unpoweredLevel 0.6). 미검증: 실제 플레이 체감.

## 수치 중간값 조정 (사용자 피드백: 너무 세다)
- 후광 최대 알파 0.80 → 0.65, 베일/오버레이 구멍 중심 알파 0.45 → 0.40, 암전 구멍 세기 0.92 → 0.88 (최초 0.50/0.35/0.85와 직전 0.80/0.45/0.92의 중간). 전원 없는 조명 비율 0.6 유지.
- `LightFacility.prefab` 재적용(peakOpacity 0.65). EditMode Pass 2707 / Fail 26(무관 계열), 조명·가스 관련 실패 없음. 실제 플레이 체감은 미검증.
