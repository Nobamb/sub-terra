# B-202 키보드·마우스 채굴 과녁 표시 (#306) 결과

- 기준: `origin/main` `30fa000cdd4c020e4520a8fdc9bfb2e5f4ba1be8` (채굴 경로 기준 `0a9a3f40`, 이후 `MiningSystem`/`PlayerMiningController` 변경 없음)
- 브랜치: `MVP2-fix146` (원격에 없는 번호 확인 후 생성). **커밋·push 전 상태** — 사용자 동의 대기.
- 커밋 해시: 미정 (커밋 전)

## 바꾼 파일
- `Scripts/Gameplay/Mining/MiningSystem.cs` — 조회 함수 추가, 시작 함수가 조회를 공유
- `Scripts/Gameplay/Mining/PlayerMiningController.cs` — 과녁 갱신(`LateUpdate`), 최근 입력 추적, 사다리 마우스 판정을 공유 함수로 교체
- `Scripts/Gameplay/Mining/MiningTargetReticle.cs` (신규) — 과녁 월드 오브젝트, 우선권·모션 순수 로직
- `Tests/EditMode/App/Readiness/MiningTargetReticleTests.cs` (신규) — 22개
- 프리팹·씬·SFX·`EmergencyRescue*`·`CushionPadHintHologram` 미수정

## 조회 함수와 시작 경로 공유
| 조회 | 시작 경로 |
|---|---|
| `TryGetKeyboardTarget(origin, facing, miningDirection, onLadder, range, out cell)` | 방향키: `TryStartMiningInDirection`이 내부 `TryGetAdjacentCell`을 사용(조회와 동일 함수). 엔터: `TryStartMiningFrom`이 쓰는 `TryGetDirectionalCell`을 조회가 그대로 호출 |
| `TryGetPointerTarget(worldPoint, origin, onLadder, range, out cell)` | `TryStartMiningAtWorldPoint`와 같은 `TryGetWorldPointCell` + 사다리 `IsLadderPointerCell`(컨트롤러의 `IsLadderPointerTargetAllowed`도 이 함수로 위임) |
| `IsTargetTerrainMineable(cell)` | `GetTerrainFailure == None` (보호·타일 없음·잠금·`isMineable`·드릴 레벨). 전력은 보지 않음 |

- 대체 순서(옆→위→아래), 사다리 예외, 클릭 칸 계산은 그대로. 채굴 시작 시 실패 사유(`DrillLevelTooLow` 등)는 `TryStartMining`이 그대로 내므로 HUD를 삼키지 않음(테스트로 확인).
- 사다리 키보드: 방향키 y≠0이면 없음, 엔터는 바라보는 쪽 좌우 한 칸만(수직 대체 없음).

## 과녁을 숨기는 조건
설정창 열림 / 건설 모드(`BuildingPlacementActivity.IsActive`) / 컨트롤러 비활성(창이 열려 `PauseInputSuspender`가 끈 경우 `OnDisable`에서 숨김) / 목표 칸 없음·사거리 밖·타일 없음·캘 수 없음·드릴 부족·보호/잠금 / 사다리에서 키보드 수직·마우스 좌우 외 칸. 마우스가 우선일 때는 추가로 커서가 UI 위, 설치·드론 포인터 점유, `Camera.main` 없음이면 숨김. **전력 부족만으로는 숨기지 않음**(클릭/엔터 실패 HUD는 기존대로).
- 지상 기지(`SurfaceBase`)에는 `PlayerMiningController`가 없어(씬 GUID 검색 확인) 과녁이 생기지 않음. Mining 어셈블리는 App을 참조할 수 없어 씬 이름 분기는 넣지 않음.

## 우선권
**가장 최근 입력이 이긴다.** 마우스가 마지막으로 움직였거나 눌렸으면 호버 칸(유효하지 않으면 숨김), 엔터 또는 채굴 방향키를 마지막으로 새로 눌렀으면 키보드 목표. **걷기만으로는 바뀌지 않는다.** 시작 상태는 키보드. 키보드 우선인 동안 이동·방향·사다리 상태를 매 프레임 따라가고, 마우스 우선인 동안은 걸어갈 때 호버 칸을 사거리 기준으로 다시 계산해 벗어나면 숨긴다. 채굴 중에는 우선권과 무관하게 캐는 칸(`ActiveCell`)에 붙고, 깨지면 그 프레임에 우선권 기준 다음 목표로 옮기거나 사라진다.

## 모양
- 칸 네 모서리의 청록(0.30, 0.95, 0.92, α0.8) ㄱ자 꺾쇠. 한 장의 8×8 텍스처(팔 두께 2px, 32 PPU)를 코드로 만들어 4방향 flip. 새 아트·SFX 없음.
- 조임: 주기 **1.8초**, 깊이 0.05유닛(코사인으로 천천히 조였다 풀림). 목표 변경 시 **0.10초** SmoothStep 미끄러짐.
- 정렬 순서 **24** (지형·가스 20~23 위, 건설 미리보기 49~50 / `StructuralCrackOverlay` 90~92 / `DroneScanPulseView` 94~95 아래).
- `ReduceMotion`: `MiningReticleMotion`(조임·미끄러짐)에서만 분기. 켜면 목표 칸에 바로 붙은 정지 꺾쇠. 다른 채굴·월드 모션에는 적용 안 함.
- 월드 오브젝트 하나(코너 4)를 재사용, 매 프레임 Instantiate 없음. 갱신 예외는 한 번만 로그하고 과녁만 끔(채굴은 계속).

## 테스트 결과
- 신규 `MiningTargetReticleTests` 22개 통과. `SubTerra.App.Tests.EditMode.Readiness` 전체 115개 통과(`PlayerLadderMiningTests` 포함).
- `SubTerra.App.Tests.Integration` 196 통과 / 3 실패(`IntegrationWiringTests.M_F02`, `M_F04`, `PlayableMineScene_HasRequestedCharacterTerrainAndMiningLayout`). 변경 전 기준선에서도 동일하게 3개 실패 → 기존 실패.
- Unity Console 오류 0.
- **미검증:** 실제 Play(Bootstrap→광산)에서의 육안 확인 전부 — 마우스 호버 우선권 전환(마우스 입력은 EditMode에서 재현하지 않음), 꺾쇠 모양·조임·미끄러짐 느낌, 균열/스캔/자원 반짝임과의 겹침, 창·건설 모드 중 숨김. `MiningSystemPlayModeTests` 등 PlayMode 테스트 미실행. 전체 EditMode 미실행(아래 참고).

## 사용자 테스트 가이드
`work_process/MVP2/prompt-B-202.md`의 "테스트 (플레이 가이드)" 1~10번을 그대로 사용. 필요 아트: 없음.

## 알아둘 점
- 편집기에서 전체 EditMode를 돌리면 `Mine_Demo_Integration.unity`, UI 프리팹, 폰트, `TimeManager.asset` 등이 다시 저장되어 dirty가 된다(#342 교훈과 같은 현상). 한 번 발생해 `git restore`로 되돌렸고, 이후에는 플래그 파일 내용에 fixture 이름을 넣어 해당 fixture만 실행했다.
