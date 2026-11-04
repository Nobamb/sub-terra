# B-184 스캔 표시 "찰칵"·가스 "치익" 반복·무너질 블록 흔들림 반복, 새 음원 (#158 D-2·드론의 B-184 줄)

## 목표
- PM이 고른 새 음원으로 남은 광산 월드 소리 세 가지를 채운다.
  - A. 드론 스캔 링이 지나며 블록에 표시가 붙을 때 작은 카메라 "찰칵": 가까운 블록부터, 한 번에 몇 개만
  - B. 가스가 터져 있는 동안 "치익" 반복: **0에서 시작해 가스가 다 퍼진 직후 최대, 그대로 이어지다 가스가 사라지기 약 2초 전부터 작아져 사라질 때 0**
  - C. 무너질 수 있는 블록이 흔들리는 동안 우르르 반복(붕괴 소리와 별개)
- 소리 id·음량 배율·간격·거리는 상수 하나씩 둬서 PM이 숫자만 바꿔 끼울 수 있게 한다(아래 ★).
- 필요하면 다른 에이전트에게 나눠 맡겨도 된다. 결과는 한 브랜치로 모은다.
- 이번에 안 하는 #158 선택 줄(PM이 음원을 고르지 않음): 시설 전력 켜짐 소리, 머리 위 상태 문구 틱, 스캔 표시가 사라질 때 역순음. 보고서 대응 표에 "이번 범위 아님"으로 적는다.

## 브랜치
1. `git fetch`로 원격 `MVP2-fixNNN` 중 가장 큰 번호를 확인한다. 계획 순서는 B-179-1(`fix125`) → B-180(`fix126`) → B-180-1(`fix127`) → B-181(`fix128`) → B-182(`fix129`) → B-183(`fix130`) → B-184(`fix131`)이다.
2. 가장 큰 번호의 `git log`에서 `(프롬프트 B-179-1)`·`(프롬프트 B-180)`·`(프롬프트 B-180-1)`·`(프롬프트 B-181)`·`(프롬프트 B-182)`·`(프롬프트 B-183)` 커밋 포함 여부를 본다.
3. **B-183 커밋이 들어 있는 가장 최신 브랜치** 위에서 **비어 있는 다음 번호** `MVP2-fixNNN`을 만든다. 보고서 맨 위에 기준 브랜치·커밋, 위 여섯 커밋 포함 여부, 만든 번호와 이유를 적는다.
4. 멈추고 보고: 기준에 B-179 커밋(`b1471534`)이 없을 때, B-183 커밋이 든 브랜치가 없을 때(95~98 칸과 음원 폴더를 B-183이 먼저 만든다).

## 절대 규칙
1. 기존 SfxId 번호·칸 값·버스 음량은 바꾸지 않는다. 새 칸은 **99~101** 세 개(95~98은 B-183).
2. **새 음원은 고치지 않는다.** 길이·이음새는 다듬은 파일 그대로. 조정은 칸 값과 상수로만.
3. 원샷은 `SfxPlayer.Play`. `SfxPlayer`에 멈춤·반복 API를 더하지 않는다. 반복 소리는 따로 AudioSource(`WorldLoopAudio`·`GasPulseAudio` 방식). 경고(Alert) 버스는 쓰지 않는다.
4. **불러오기·이어하기, 초기화(Bind·OnEnable·씬 시작), 조용히 치우는 경로(ClearAll·ClearRestoredZones·OnDisable·씬 전환·탐사 실패)에서는 새로 소리를 내지 않는다.**
5. Gameplay에는 **읽기 전용 속성·이벤트만 더한다**(App을 참조하지 않음). 가스 시간·흔들림 규칙·스캔 연출 시간표는 바꾸지 않는다.
6. 씬·프리팹 YAML은 고치지 않는다. B-164~B-183 소리 연결(가스 맥박 66, 붕괴 69, 스캔 핑 90)은 그대로.
7. 줄 번호는 fix124 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 음원 넣기
- B-183이 쓴 `work_process/MVP2/sound/sfx-B183-184/`에 남은 wav 3개를 `Assets/_Project/Audio/SFX/World/`로 옮긴다. 없으면 멈추고 보고한다. mp3·README·`address-rows.md`는 그대로 둔다.
- 임포트: 기존 빌더를 따르되 **100·101은 Normalize 끔 + PCM**(반복 소리가 +13dB 커짐). 빌더 예외에 넣고, `LoopFileNames`에는 넣지 않는다.
- `init/sound-effect-address.md`: `address-rows.md`의 99~101 세 줄(scan_mark_click·gas_flow_loop·unstable_block_shake_loop)을 표 끝에 붙인다.

| 칸 | 이름 | 파일 | 버스 | 음량 | 길이 | 쓰는 때 |
|---|---|---|---|---|---|---|
| 99 | ScanMarkClick | scan_mark_click | World | 0.4 | 0.100초 | 표시 하나가 초점을 맞출 때 |
| 100 | GasFlowLoop | gas_flow_loop | World | 0.45 | 4.000초 반복 | 가스가 있는 동안 |
| 101 | UnstableBlockShakeLoop | unstable_block_shake_loop | World | 0.5 | 1.500초 반복 | 블록이 흔들리는 동안 |
- `Shared/Audio/SfxId.cs`, `App/Audio/SfxLibraryDefaults.cs`(파일 끝, 길이 주석), `Resources/SfxLibrary.asset` 세 곳에 같게.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/Scripts/` 기준)
- #158의 `(B-184)` 줄, `work_process/MVP2/sound/sfx-B183-184/README.md`, B-183 보고서
- Gameplay/Drone/DroneScanPulseView.cs `Show` ~51(가까운 순 정렬 ~88, `RevealAt` ~95), `UpdateMarkerVisuals` ~137(초점 번쩍임은 나이 0.16초), Gameplay/Drone/DroneSensor.cs ~222-262(`Show` 뒤 `ScanPulseStarted`), App/Integration/DroneScanPingSfx.cs(`DroneScanPingRule`)
- Gameplay/Hazards/GasHazardSystem.cs `ActivateAt` ~40-52, `RestoreGasZone` ~58-77(~74 이벤트), `ClearRestoredZones` ~80, `TickZones`, Gameplay/Hazards/GasZone.cs `Activate` ~33-55, `Tick` ~70-95, GasVisualRules.cs ~9
- Gameplay/Structural/StructuralCrackOverlay.cs `shakeVisuals` ~36, `UpdateShakeVisual` ~396, `RemoveShakeVisual`, `ClearCell` ~200, `ClearAll` ~218
- App/Integration/IntegrationRuntimeBinder.cs `BindWorldSfx` ~862-930, App/Integration/GasPulseAudio.cs(음량·정지·음소거 판단), App/Integration/WorldLoopAudio.cs

## A. 스캔 표시 "찰칵"
- Gameplay: `DroneScanPulseView`에 이벤트 `MarkerFocused(int order, Vector3 worldPosition)`를 더한다. `UpdateMarkerVisuals`에서 표시 나이가 처음 0.16초(번쩍임 꼭대기)를 넘는 프레임에 표시마다 한 번. `order`는 가까운 순서(0이 가장 가까움). `Clear`·`DeactivateMarkers`·`OnDisable`에서는 부르지 않는다.
- App: `DroneScanMarkSfx`(핑 옆, `BindWorldSfx`에서 `droneSensor.ScanPulseView`에 연결).
  - 회차마다 `ScanPulseStarted(immediate)`에서 `DroneScanPingRule.ShouldPlay`와 **같은 판단**으로 이번 회차 허용 여부를 정한다(즉시 스캔·설정창·정지·도착 "딩"/레벨업 0.6초 안이면 그 회차 전부 무음).
  - 허용된 회차에서 `ScanMarkMaxPerPulse = 6`★개까지, 직전 "찰칵"에서 `ScanMarkMinGap = 0.06f`★ 안이면 그 하나는 생략(늦춰 내지 않음). 피치 `1 + order × 0.015`★(최대 1.1).
  - 표시는 가까운 순으로 번쩍이므로 소리도 가까운 것부터 난다. 클립이 0.1초라 동시에 나는 소리는 최대 2개.
- 표시가 사라질 때 역순음은 이번 범위 아님.

## B. 가스 "치익" 반복 (App, 예: `GasFlowLoopAudio`, `BindWorldSfx`에서 붙임)
- 코드 기준 시간(게임 시간 `Time.deltaTime`): 퍼지는 연출 `GasVisualRules.SpawnDurationSeconds = 1`초 → 그 뒤 `GasHazardSystem.defaultDuration = 12`초(씬 값도 12) 동안 남음 → `RemainingDuration`이 0이 되면 `IsActive=false`·`GasZoneDeactivated`. 합쳐 약 13초. 꺼질 때 화면은 바로 사라진다.
- 새 가스만 소리: `GasZoneActivated`를 받을 때 `zone.IsSpawnComplete`가 false면 새로 터진 가스(`ActivateAt`), true면 불러온 가스(`RestoreGasZone`, ~74, 퍼짐 없이 바로 완료) → **불러온 가스는 끝까지 무음**. 이 구분이 안 되면 Gameplay에 `GasZoneBurst` 이벤트를 `ActivateAt`에만 더하고 이유를 적는다.
- 가스 하나의 크기(0~1), 순수 함수로 분리해 검사:
  1. 퍼지는 중: `SpawnProgress`(0→1, 1초). 부드럽게 `SmoothStep`★.
  2. 다 퍼진 뒤: 1(최대) 유지.
  3. `RemainingDuration ≤ GasFadeOutLead = 2f`★: `RemainingDuration / 2`로 줄어 0에서 끝. 꺼짐 이벤트·`IsActive` false면 0.
- 거리: 플레이어와 가스 중심 거리 − 반경이 `GasHearFull = 2`★칸 안이면 1, `GasHearMax = 14`★칸에서 0(선형). 가스 안에서는 가스 맥박(66)과 같이 난다.
- 여러 가스: AudioSource **하나**. 크기 = 살아 있는 새 가스들의 (크기 × 거리) 중 **가장 큰 값**(가장 가까운 가스 기준). 추적은 최대 `GasTrackMax = 8`★개, 꺼짐 이벤트·`ClearRestoredZones`·파괴된 존은 바로 뺀다.
- 음량 = `칸 × 버스 × EffectiveVolume(Sfx) × 크기`. 크기가 0이면 멈추고, 다시 0보다 커지면 처음부터 튼다.
- 정지: `UiPauseGate.IsHeld`·설정창 열림이면 `Pause`(가스 시간도 멈추므로 풀리면 `UnPause`). 효과음·마스터 음소거, 탐사 실패 처리, 행동불능, 엘리베이터 탑승 중(`IsRiding`)은 약 0.08초 줄여 멈춤. 씬 바뀜·`OnDisable`·`OnDestroy`는 바로 멈추고 추적을 비운다.

## C. 무너질 블록 흔들림 반복 (App, 예: `UnstableShakeLoopAudio`)
- Gameplay: `StructuralCrackOverlay`에 읽기 전용 `ShakingCellCount`와 `TryGetNearestShakingCell(Vector3 world, out Vector3 cellCenter)`를 더한다(`shakeVisuals` 키를 칸 중심 좌표로).
- 대상 크기 = 흔들리는 칸이 있고, 가장 가까운 칸이 `ShakeHearFull = 3`★칸 안이면 1, `ShakeHearMax = 10`★칸에서 0. 원인(받침 없음·충격)은 구분하지 않는다.
- 크기 변화는 켜질 때 `ShakeFadeIn = 0.15f`★, 꺼질 때 `ShakeFadeOut = 0.3f`★로 따라간다. 흔들림이 끝나 블록이 떨어지면 붕괴 소리(69)는 그대로 나고 반복은 줄어 사라진다.
- 씬 시작·불러오기 뒤 `ShakeStartupQuiet = 1.0f`★ 동안은 켜지 않는다(복원으로 다시 생긴 흔들림에 소리가 튀지 않게, 그 뒤에도 흔들리면 서서히 켜짐).
- 정지·음소거·탐사 실패·행동불능·탑승·씬 바뀜·`ClearAll`은 B와 같다.

## 손대지 말 것
- 기존 칸 값, 음원 파일, 가스 지속·퍼짐·피해 규칙, 구조 위험·흔들림·붕괴 규칙, 스캔 주기·연출, B-178 핑 생략 규칙, B-179~B-183 대상, `AGENTS.md`·규칙 문서

## 테스트
- EditMode 전체에서 기준 브랜치의 기존 실패 말고 새 실패가 없어야 한다. 기대값을 바꾼 검사는 이름과 이유를 적는다.
- 새 EditMode `PromptB184SfxWorldLoopTests`, 필요하면 `PromptB184SfxWorldLoopPlayModeTests`(소리 요청을 잡는 방식은 B-178·B-181 검사를 따름)
  - 칸: 99~101 버스·음량·파일, 100·101 임포트 Normalize 끔
  - A: 표시 10개 회차 → 99 최대 6번·가까운 순 피치 증가, 0.03초 간격 두 표시 → 한 번, 즉시 스캔·설정창·"딩" 0.3초 뒤 회차 → 0번, `Clear` 0번
  - B: 크기 함수(퍼짐 0초 0, 0.5초 약 0.5, 1초 1, 남은 5초 1, 남은 1초 0.5, 0초 0), 복원 가스 0, 두 가스 → 가까운 쪽 값, 거리 14칸 0, 정지 중 Pause, 음소거 즉시 0, `OnDisable` 멈춤
  - C: 흔들림 0칸 → 무음, 가까운 칸 2칸 → 1, 10칸 → 0, 시작 0.5초 안 → 0, 칸이 사라지면 0.3초 뒤 0
- PlayMode는 고친 검사만. 실행 전후 `git status`로 범위 밖 프리팹·폰트·씬·PNG·TimeManager 변경을 확인하고 되돌린다.

## 사용자 테스트 가이드 (보고서에 그대로 넣기)
1. 광산에서 드론 스캔(30초마다 "핑")을 기다린다 → 링이 퍼지며 표시가 붙을 때 가까운 블록부터 작은 "찰칵" 몇 번.
2. 광물이 많은 곳에서 스캔해도 "찰칵"이 6번 넘게, 한꺼번에 쏟아지지 않는다.
3. 광산에 막 내려온 직후·업그레이드 직후의 스캔은 "찰칵"이 없다(링만 보임).
4. 가스가 든 블록을 캔다 → 가스가 퍼지는 1초 동안 "치익"이 0에서 커지고, 다 퍼지면 가장 크게 이어진다.
5. 그대로 약 12초 기다린다 → 가스가 사라지기 약 2초 전부터 작아지고, 가스가 없어질 때 소리도 끝난다.
6. 가스에서 멀리 걸어가면 "치익"이 작아지다 안 들린다. 가스 두 개를 터뜨려도 소리가 두 겹으로 커지지 않는다.
7. 가스가 있을 때 저장 후 이어하기 → 불러온 가스에서는 "치익"이 나지 않는다.
8. 가스 "치익" 중 ESC 설정창을 열면 멈추고, 닫으면 이어진다. 엘리베이터를 타면 멈춘다.
9. 받침을 캐서 블록이 흔들리게 만든다 → 흔들리는 동안 작은 우르르 반복. 무너지면 예전 붕괴 소리가 나고 우르르는 줄어 사라진다.
10. 흔들리는 블록에서 멀어지면 우르르가 작아지다 사라진다.
11. 효과음 음량·음소거가 세 소리에 다 적용된다. 음소거하면 반복 소리가 바로 멈춘다.
12. Unity Console에 새 빨간 오류·노란 경고가 없다. 바꿔 보고 싶은 소리·크기·거리(★)가 있으면 PM에게 알려 준다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 스캔 표시 찰칵·가스 치익 반복·블록 흔들림 반복을 연결 (프롬프트 B-184)`). 새 브랜치에 일반 push, 브랜치는 지우지 않는다. PR은 만들지 않는다.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue158-주제.md` 파일 하나로 기록. **`init/rules/lessons.md`에 줄을 추가하지 않는다.**
- **GitHub 이슈 #158은 고치지 않는다.** 체크는 PM이 확인 뒤 한다. 보고서의 대응 표에만 적는다.
- 결과 보고서 `work_process/MVP2/sound/B-184-sfx-world-loop-result.md`
  - 기준 브랜치·커밋(B-179-1~B-183 포함 여부, 만든 번호와 이유), 소리마다 붙인 곳(파일·줄)과 SfxId·배율, ★ 상수 위치, Gameplay에 더한 읽기 전용 속성·이벤트
  - 가스: 실제로 찾은 시간 값(퍼짐·지속·꺼짐), 복원 가스 구분 방법, 여러 가스 처리
  - 임포트 예외(100·101), 테스트 결과, 직접 들어 보지 못한 항목은 "미검증", 위 사용자 테스트 가이드
  - **#158 대응 표**(보고서 안에만): 스캔 표시 찰칵 / 가스 치익 / 블록 흔들림 반복 각각 구현·일부·못 함, 선택 줄 3개는 "이번 범위 아님"
