# B-184 월드 효과음 적용 결과

## 기준과 범위

- 기준 브랜치: `origin/MVP2-fix130`, `cc373a5b` — B-183 슬롯·드론 효과음 적용 커밋. `git fetch origin` 후 가장 큰 원격 번호가 130임을 확인했다.
- 선행 커밋 포함: B-179 `b1471534`, B-179-1 `2b474db7`, B-180 `22f2f5f5`, B-180-1 `d2f54697`, B-181 `7c2c2957` 및 후속 `43e0efda`, B-182 `0ed7ac9d` 및 후속 `f3b7f857`, B-183 `cc373a5b`.
- 작업 브랜치: `MVP2-fix131`. B-183을 포함한 최신 기준 위의 비어 있는 다음 번호다.
- B-183은 기존 95번 충돌을 해결해 신규음을 102~105번에 배정했다. 그 번호를 보존하고 B-184 예약 99~101번을 사용했다.
- 작업 시작부터 있던 `init/prompt-B.md`, 프롬프트 B-183/B-184 원문과 원본 음원 폴더는 커밋 범위에서 제외하고 로컬에 보존했다. 사용자 후속 요청에 따라 B-184 구현·검증 기록을 커밋·푸시하고 main 대상 PR을 작성한다.
- GitHub #158의 B-184 항목을 읽고 대응 범위를 확인했으며 이슈는 수정하지 않았다. 가스는 최신 프롬프트의 지속 반복음 요구를 따른다.

## 음원과 연결

경로는 `sub-terra/Assets/_Project/` 기준이다.

| SfxId | 파일 | 버스 / 칸 음량 | 재생 지점 |
|---|---|---|---|
| 99 ScanMarkClick | `Audio/SFX/World/scan_mark_click.wav` (0.100초) | World / 0.4 | 스캔 표시가 처음 0.16초 초점 번쩍임에 도달할 때 |
| 100 GasFlowLoop | `Audio/SFX/World/gas_flow_loop.wav` (4.000초 반복) | World / 0.45 | 새로 분출한 가스가 존재하고 플레이어가 가청 거리 안에 있을 때 |
| 101 UnstableBlockShakeLoop | `Audio/SFX/World/unstable_block_shake_loop.wav` (1.500초 반복) | World / 0.5 | 실제 흔들림 시각 객체가 존재하고 플레이어가 가청 거리 안에 있을 때 |

- WAV 세 개는 원본을 변경하지 않고 복사했다. 세 파일 모두 작업 폴더 원본과 SHA-256이 일치한다. MP3·README·address-rows도 보존했다.
- `Scripts/Shared/Audio/SfxId.cs`, `Scripts/App/Audio/SfxLibraryDefaults.cs`, `Resources/SfxLibrary.asset`에 같은 번호·파일·버스·음량을 등록했다. 기존 칸과 버스 음량은 그대로다.
- 기존 `Editor/DataValidation/PromptB156SfxLibraryBuilder.cs`의 임포트 예외에 100·101 파일만 추가했다. Normalize=0, PCM, DecompressOnLoad. `LoopFileNames`에는 추가하지 않았다.
- 열린 Unity Editor에서 기존 `PromptB156SfxLibraryBuilder.EnsureAsset()`와 `AssetDatabase` API로 임포트·라이브러리 저장을 실행했다. 씬·프리팹 YAML 배선은 변경하지 않았다.
- `init/sound-effect-address.md` 끝에 지정된 출처 세 줄을 추가했다.

## 코드와 조절 상수

| 역할 | 파일과 조절 값 |
|---|---|
| 스캔 표시 | `Scripts/App/Integration/DroneScanMarkSfx.cs` 위쪽 상수: `ScanMarkSfx=99`, `ScanMarkMaxPerPulse=6`, `ScanMarkMinGap=0.06`, `ScanMarkPitchStep=0.015`, `ScanMarkMaxPitch=1.1` |
| 가스 | `Scripts/App/Integration/GasFlowLoopAudio.cs` 위쪽 상수: `GasFlowSfx=100`, `GasFadeOutLead=2`, `GasHearFull=2`, `GasHearMax=14`, `GasTrackMax=8` |
| 흔들림 | `Scripts/App/Integration/UnstableShakeLoopAudio.cs` 위쪽 상수: `ShakeLoopSfx=101`, `ShakeHearFull=3`, `ShakeHearMax=10`, `ShakeFadeIn=0.15`, `ShakeFadeOut=0.3`, `ShakeStartupQuiet=1` |
| 공통 소스 | `Scripts/App/Integration/ProximityLoopAudio.cs`: 각 반복음에 소스 하나, `BlockedFadeOut=0.08`, 설정 음량·일시정지·중단·해제 처리 |
| 런타임 배선 | `Scripts/App/Integration/IntegrationRuntimeBinder.cs:865`의 `BindWorldSfx`. 복원 후 가스/흔들림 컴포넌트를 바인더 객체에 붙이고, 스캔 핑 옆에 표시 이벤트 구독을 연결 |

### A. 스캔

- Gameplay `DroneScanPulseView.MarkerFocused(int order, Vector3 worldPosition)`만 추가했다. 표시마다 가까운 순서와 발행 여부를 기억하고 처음 0.16초를 넘을 때 한 번 알린다. 기존 Show·RevealAt·연출 시간표는 유지했다.
- 회차 시작 시 기존 `DroneScanPingRule.ShouldPlay`로 즉시 스캔·설정창·정지·최근 도착/레벨업 0.6초를 판정한다. 허용하지 않은 회차는 끝까지 무음이다.
- 허용 회차는 최대 6회, 직전 클릭과 0.06초 미만이면 해당 표시를 생략한다. 재생 예약은 없다. 피치는 `min(1.1, 1 + order * 0.015)`다.
- 원샷은 `SfxPlayer.Play`. Clear·비활성화·바인딩 해제는 소리를 요청하지 않는다. 첫 스캔에서 View가 생성되는 경우도 `ScanPulseStarted`에서 연결한다.

### B. 가스

- 기존 Gameplay 값을 확인했다: 생성 연출 `GasVisualRules.SpawnDurationSeconds=1`초, `GasHazardSystem.defaultDuration=12`초. 퍼짐 중에는 지속 시간을 차감하지 않아 합계 약 13초다. 게임 시간으로 진행하며 소리가 시간 규칙을 바꾸지 않는다.
- 활성화 이벤트 시 `IsSpawnComplete=false`인 새 가스만 추적한다. 복원은 이미 true로 전달되어 끝까지 무음이다. Bind에서도 기존 존을 모으지 않는다.
- 순수 함수 `GasLevel`: 퍼짐 중 SmoothStep(SpawnProgress), 퍼진 뒤 1, 마지막 2초는 RemainingDuration/2, 비활성은 0.
- 순수 함수 `DistanceLevel`: 중심 거리에서 반경을 뺀 값이 2 이하면 1, 14 이상이면 0, 사이는 선형이다.
- 최대 8개를 추적하고 크기×거리 중 최댓값 하나만 재생한다. 더해서 커지지 않는다. 소멸 이벤트·비활성·파괴된 존을 제거한다.
- Gameplay에는 정리 알림 `GasHazardSystem.ZonesCleared`만 추가했다. `ClearRestoredZones`에서 반복음을 즉시 정지하고 추적을 비운다.

### C. 흔들림

- Gameplay `StructuralCrackOverlay`에 `ShakingCellCount`, `TryGetNearestShakingCell` 및 조용한 전체 정리 알림 `Cleared`를 추가했다. 좌표는 원본 Tilemap의 칸 중심이다.
- 가장 가까운 흔들림의 3칸 안은 1, 10칸 밖은 0이다. 0.15초로 켜지고 0.3초로 꺼진다. 시작·재바인딩·ClearAll·재활성화 후 1초는 무음이다.
- 원인(Unsupported/MiningImpact)을 구분하지 않는다. 기존 붕괴 69번은 유지했다.
- 오디오 컴포넌트와 소스는 바인더 아래에 둔다. 균열 시각 컴포넌트는 ClearAll에서 추적하지 않은 직속 자식을 지우므로 그 아래에 소스를 두지 않는다.

### 공통 중단

- 음량 = 칸 × 버스 × EffectiveVolume(Sfx) × Level. 마스터는 기존 AudioListener 음량을 따른다.
- UiPauseGate/설정창 중 Pause, 풀리면 UnPause. 탐사 실패·행동불능·탑승·런 종료는 0.08초로 줄여 정지한다.
- 음소거는 프롬프트의 자동 검사·사용자 가이드에 맞춰 즉시 정지한다. 본문의 약 0.08초 요구보다 즉시 정지를 우선한 부분이다.
- OnDisable/OnDestroy는 즉시 정지하며 가스 추적과 흔들림 초기 상태를 비운다. SfxPlayer 반복·중단 API와 Alert 버스는 추가하지 않았다.

## 검증

- Unity 6000.5.4f1의 열린 Editor를 사용했다. CLI Pipeline 연결이 없어 기존 Unity MCP와 프로젝트 `TestValidationRunner`로 검증했다. 패키지나 버전은 변경하지 않았다.
- 변경 전 전체 EditMode: 2,398 실행, **2,367 통과 / 31 실패** (`B-184-verification/editmode-baseline.txt`).
- 변경 후 전체 EditMode: 2,432 실행, **2,402 통과 / 30 실패 / 0 건너뜀** (`editmode-full.txt`). 신규 B-184 34개는 모두 통과했다(`editmode-b184.txt`).
- 새 실패 0개. 기준 실패 중 MainMenu 삭제 버튼 호버의 시간 의존 검사 하나가 최종 실행에서 통과했다. 나머지 30개는 기준과 같다(`editmode-failure-comparison.txt`). 전체 테스트가 모두 통과한 상태는 아니다.
- 첫 관련 검사 118개에서는 기존 B-183 노이즈 검사 4개와 새 EditMode 수명주기 호출 검사 1개가 실패했다. 새 검사의 SendMessage를 reflection 직접 호출로 고친 뒤 위 전체 검사에서 통과했다. B-183 4개는 기준에도 있던 실패이며 수정하지 않았다.
- 기존 검사 기대 변경: PromptB156SfxTests, PromptB165SfxD1FixTests, PromptB168SfxD2Tests, PromptB178SfxTests, PromptB1791SfxFeedbackFixTests의 전체 칸 수 98→101. PromptB183SfxMenuDroneTests의 예약 칸이 비어 있다는 기대는 99~101 존재 확인으로 변경했다. 기존 음원·음량 기대는 유지했다.
- 최종 새 PlayMode 4개: **4 실행 / 4 통과 / 0 실패 / 0 건너뜀** (`playmode-summary.txt`, `playmode-tests.txt`). 실제 AudioSource 시간 진행, Pause/UnPause, 음소거·실패·행동불능·탑승·런 종료·OnDisable, 표시 순서·Clear 무음, 흔들림 시작 무음·페이드·ClearAll을 검증했다.
- `Bootstrap_NewGame_MineBindsWorldLoopsAndPlaysGas`: Bootstrap → MainMenu → 새 게임 → SurfaceBase → 탐사 → Integration UI 활성화 → 시작 브리핑 닫기 → 바인더의 두 반복음 컴포넌트 각 1개 → 실제 GasHazardSystem.ActivateAt → AudioSource 재생까지 통과했다. 저장·불러오기·썸네일 서비스는 새 게임 전에 모두 `Temp/B184-smoke-*` 전용 경로로 교체했다. 사용자 슬롯 파일은 쓰지 않았다.
- 첫 통합 검사에서는 현재 SFX 음소거가 켜져 있어 소스가 만들어지지 않는 정상 경로를 실패로 단언했다. 최종 검사는 현재 음소거에 따른 무음을 먼저 확인한 뒤, 해당 가스 컴포넌트의 테스트용 음소거 판정만 대체했다. 사용자 설정과 EffectiveVolume은 유지했다. 따라서 소스 재생을 검증했으며 실제 청취 검증은 아니다. 설정에서 효과음 음소거를 해제해야 실제 게임 소리가 들린다.
- 절반 해상도 `B-184-verification/mine-smoke.png`를 열어 실제 광산·플레이어·가스 퍼짐·HUD 화면을 확인했다. 가스 발생 자체는 채굴 입력 대신 실제 시스템 API로 유도했다.
- 최종 Console: 오류 0, 경고 1. 기존 `CoreCctvCameraRig.EnsureCreated` 73행에서 URP에 `Camera.stereoTargetEye`를 설정하는 경고이며 새 오디오 경로와 무관하다. 범위 밖이므로 수정하지 않았다. `playmode-output.txt`에도 기록했다.
- 자동 테스트가 갱신한 범위 밖 폰트·프리팹·씬·PNG·ProjectSettings는 작업 시작 상태로 복구했다. 시작부터 있던 사용자 수정은 보존했다.
- 교훈: `init/rules/lessons/2026-11-04-issue158-integration-audio-test-lifecycle.md`, `2026-11-04-issue158-world-audio-smoke-mute.md`.

## 변경 파일

- WAV 3개 및 Unity 생성 `.meta` 3개, `Resources/SfxLibrary.asset`.
- Shared SfxId, App SfxLibraryDefaults, 기존 PromptB156SfxLibraryBuilder.
- 신규 App Integration: DroneScanMarkSfx, GasFlowLoopAudio, UnstableShakeLoopAudio, ProximityLoopAudio 및 `.meta`.
- 기존 IntegrationRuntimeBinder, DroneScanPulseView, GasHazardSystem, StructuralCrackOverlay.
- 신규 EditMode/PlayMode 테스트 2파일 및 `.meta`, 위 기존 기대값 검사 6파일.
- 출처 표, 이 보고서, 검증 요약과 교훈 파일.

## 미검증

- 직접 청취한 음량 균형·반복 이음새·다른 효과음과의 조화는 미검증이다.
- 물리 키보드·마우스로 광물을 채굴하여 가스를 발생시키는 전체 조작, 실제 저장 후 이어하기, 30초 자연 스캔과 붕괴를 함께 관찰하는 장시간 플레이, Windows 빌드는 미검증이다.

## 사용자 테스트 가이드

1. 광산에서 드론 스캔(30초마다 "핑")을 기다린다 → 링이 오면 표시가 붙을 때 가까운 블록부터 작은 "찰칵" 몇 번.
2. 광물이 많은 곳에서 스캔해도 "찰칵"이 6번 넘게, 한꺼번에 쏟아지지 않는다.
3. 광산에 막 내려온 직후·업그레이드 직후의 스캔은 "찰칵"이 없다(링만 보임).
4. 가스가 든 블록을 캔다 → 가스가 퍼지는 1초 동안 "치익"이 0에서 커지고, 다 퍼지면 가장 크게 이어진다.
5. 그대로 약 12초 기다린다 → 가스가 사라지기 약 2초 전부터 작아지고 가스가 없어지면 소리도 끝난다.
6. 가스에서 멀리 걸어가면 "치익"이 작아지다 안 들린다. 가스 두 개를 연속으로 터뜨려도 소리가 두 겹으로 커지지 않는다.
7. 가스가 있을 때 저장·이어하기 → 불러온 가스에서는 "치익"이 나지 않는다.
8. 가스 "치익" 중 ESC 설정창을 열면 멈추고, 닫으면 이어진다. 엘리베이터를 타면 멈춘다.
9. 받침을 캐서 블록이 흔들리게 만든다 → 흔들리는 동안 작은 우르르 반복. 무너지면 예전 붕괴 소리가 나고 우르르는 줄어 사라진다.
10. 흔들리는 블록에서 멀어지면 우르르가 작아지다 사라진다.
11. 효과음 음량·음소거가 세 소리에 다 적용된다. 음소거하면 반복 소리가 바로 멈춘다.
12. Unity Console에 새 빨간 오류·노란 경고가 없다. 바꿔 보고 싶은 소리·크기·거리(★)가 있으면 PM에게 알려 준다.

## #158 대응 표 (보고서만)

| 항목 | 상태 | 비고 |
|---|---|---|
| 스캔 표시 찰칵 | 구현 | 99, 6회·0.06초 제한, 핑 생략 규칙 공유 |
| 가스 치익 | 구현 | 100, 새 분출만·퍼짐/마지막 2초·거리·단일 소스 |
| 블록 흔들림 반복 | 구현 | 101, 실제 흔들림·거리·초기 무음·페이드 |
| 시설 전력 켜짐 | 이번 범위 아님 | 선택 음원 미선정 |
| 머리 위 상태 문구 틱 | 이번 범위 아님 | 선택 음원 미선정 |
| 스캔 표시 사라질 때 역순음 | 이번 범위 아님 | 선택 음원 미선정 |
