# B-202 키보드·마우스 채굴 과녁 표시 (#306)

## 목표
광산에서 엔터(또는 설정의 채굴 방향키)로 캐질 블록 위, 그리고 마우스가 사거리 안의 캘 수 있는 블록을 가리킬 때, 누르기 전부터 청록 꺾쇠 과녁을 띄운다. 과녁은 지금 채굴 규칙이 고를 칸만 보여 주고, 그 규칙을 바꾸지 않는다. 사거리 밖이거나 캘 수 없으면 숨긴다. 키보드와 마우스가 동시에 목표를 가리키면 **가장 최근 입력이 이긴다.**
기준: https://github.com/Nobamb/sub-terra/issues/306
이 이슈는 Nobamb가 진행한다(연출 포함). 댓글: 「이 이슈는 제가(Nobamb) 작업합니다.」

## 브랜치
1. `git fetch` 후 최신 `origin/main`에서 새 브랜치 **`MVP2-fix146`**를 만든다. 이 문서의 채굴 경로 기준은 `0a9a3f403b80a81f8540dd2c3137f7348c8a7c73`이다. (`MVP2-fix145`는 B-201/#358용. 이 번호는 쓰지 않는다.) 그 이후 main에 B-201이 들어갔을 수 있으나(`30fa000c` 등) `MiningSystem`/`PlayerMiningController`는 바뀌지 않았다. 작업 시점의 `origin/main`에서 딴다.
2. **B-201과의 관계.** 채굴 과녁은 B-201과 독립이다. `EmergencyRescue*`·`CushionPadHintHologram`을 수정하지 않는다. 원격에 `MVP2-fix146`이 다른 커밋으로 있으면 빈 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다.
3. 보고서 맨 위에 기준 커밋 해시를 적는다. 브랜치를 지우지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix146`(또는 실제 쓴 번호)에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다. #306에 댓글·체크박스 수정·닫기를 하지 않는다.
3. 새 음원, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다. 기존 SFX만 쓰고, 과녁에는 소리를 붙이지 않는다.
4. 큰 화면 전체 모션이 아니면 reduce-motion 분기를 새로 넣지 않는다. 예외: 이 이슈가 요구하는 과녁의 조임·미끄러짐만 `AccessibilityPreferences.ReduceMotion`이면 빼고 정지 꺾쇠로 둔다. 다른 채굴·월드 모션에는 넣지 않는다.
5. 프리팹 YAML을 손으로 고치지 않는다. 과녁은 코드로 월드 오브젝트 하나를 만들어 재사용한다(매 프레임 Instantiate 금지).
6. 목표 칸을 고르는 규칙(바라보는 방향, 앞이 비면 위·아래 대체, 사다리에서는 좌우만, 마우스 클릭 칸 계산)과 실제 채굴 시작 동작은 바꾸지 않는다.

## 현재 상태 (채굴 경로 `0a9a3f40`, 이후 해당 파일 변경 없음)
- **키보드 시작.** `PlayerMiningController.TryStartPendingTarget()`(`Scripts/Gameplay/Mining/`). 채굴 방향키가 있으면 `MiningSystem.TryStartMiningInDirection(position, direction, reach)` — 캐릭터 칸의 상하좌우 한 칸, 위·아래 대체 없음. 엔터는 `TryStartMiningFrom(position, FacingDirection, reach)` — 내부 `TryGetDirectionalCell`이 바라보는 쪽 옆칸을 먼저 보고, 타일이 없으면 위·아래 순. 사다리(`PlayerAnimationController.CanMineOnLadder`)에서는 엔터도 `TryStartMiningInDirection`만 쓰고 수직 대체를 쓰지 않으며, 방향키 y≠0이면 시작하지 않는다. `reach` 기본 `DefaultReach` 1.35. 방향키는 `PlayerKeyboardControls.ReadMiningDirection(ControlPreferences.Scheme)`: `Classic`은 0, `WasdMove`는 화살표, `ArrowsMove`는 WASD(대각선은 수직 우선).
- **마우스 시작(클릭).** `CaptureCurrentTarget`이 `Camera.main.ScreenToWorldPoint(Mouse.current.position)`으로 `pendingWorldPoint`를 넣고, `TryStartMiningAtWorldPoint(worldPoint, movement.Position, reach)`를 부른다. 칸은 `TryGetWorldPointCell`: `ForegroundTilemap.WorldToCell` 후 `IsCellInRange`(칸 중심, 허용 거리 `reach + 0.5`). 이어서 `TryStartMining`이 `GetTerrainFailure`(보호 칸·`isMineable`·`requiredDrillLevel`·심층 잠금 `DeepZoneLocked`)와 전력을 검사한다. 미리보기용 `CanPrioritizePointerMining`은 `IsLadderPointerTargetAllowed`(사다리에서는 같은 높이·다른 x만)와 `CanStartMiningAtWorldPoint`(사거리+지형에 더해 `HasMiningPower`·`GetEnergyFailure`)를 쓴다. 조회 함수는 아직 없다.
- **숨길 게이트(키보드·마우스 공통).** 설정창은 `ControlPreferences.IsSettingsOpen`. 가이드·업그레이드 등 창이 열리면 `PauseInputSuspender`가 `UiPauseGate.IsInputBlocked`로 `PlayerMiningController`를 끈다(`OnDisable`에서 채굴 취소·과녁 숨김). 건설 모드는 `BuildingPlacementActivity.IsActive`. 광산 씬은 `SceneNames.Integration`(`Mine_Demo_Integration`), 지상은 `SurfaceBase`. Gameplay.Mining 어셈블리는 Shared·Gameplay.Player만 참조하므로 App(`UiPauseGate`, `SceneNames`)을 끌어오지 말 것. 커서가 UI 위이거나 설치/드론이 포인터를 점유(`pointerClaimedByPlacement`/`pointerClaimedByDrone`)한 동안은 마우스 과녁만 숨긴다.
- **겹침.** `StructuralCrackOverlay` 균열·낙석 정렬 약 90~92, `DroneScanPulseView` 스캔 표식 95. 과녁은 네 모서리만 얇고 반투명하게, 지형·자원 반짝임·균열·스캔을 가리지 않게 그 아래(예: 지형보다 위, 90 미만)에 둔다.
- **테스트.** `MiningSystemPlayModeTests`, `PlayerLadderMiningTests` 등 기존 채굴 픽스처를 깨지 말 것.

## 할 일
1. `MiningSystem`에 채굴을 시작하지 않고 칸만 돌려주는 조회를 만든다. 키보드(이슈 예: `TryGetKeyboardTarget`)는 엔터(바라보기+대체) / 방향키(인접 한 칸) / 사다리(좌우만, 수직 대체 없음)와 같다. 마우스 호버는 클릭과 같은 월드 좌표→`TryGetWorldPointCell`+`IsCellInRange`+사다리 `IsLadderPointerTargetAllowed`로 칸을 고른다. `TryStartMiningFrom`·`TryStartMiningInDirection`·`TryStartMiningAtWorldPoint`도 이 조회를 쓰게 해서 과녁과 실제 채굴 칸이 어긋나지 않게 한다. 대체 순서와 사다리 예외, 클릭 칸 계산은 그대로. 조회가 고른 칸이 `GetTerrainFailure`면 과녁은 숨기되, 채굴 시작 시의 `DrillLevelTooLow` 등 실패 HUD는 삼키지 않는다.
2. 과녁은 월드 오브젝트 하나. (가) 엔터/방향키 기준 목표(누르기 전에도). 이동·방향 전환·사다리 오르내리면 바로 따라가고, 채굴 중에는 그 칸에 붙었다가 깨지면 다음 목표로 옮기거나 사라진다. (나) 마우스가 가리키는 칸이 사거리 안이고 `GetTerrainFailure`가 없으면 같은 과녁을 그 칸에 띄운다. 사거리 밖, 타일 없음, 캘 수 없음, 드릴 부족, 보호/잠금 칸, 사다리에서 좌우가 아니면 그 입력의 과녁은 숨긴다. 전력 부족만으로는 숨기지 않는다(`CanStartMiningAtWorldPoint`는 전력도 보지만, 과녁은 키보드와 같이 지형·사거리만 본다. 클릭/엔터 실패 HUD는 기존대로).
3. **우선권.** 과녁은 하나다. 가장 최근 입력이 이긴다. 마우스가 마지막으로 움직였으면 호버 칸(유효하지 않으면 숨긴다). 엔터 또는 채굴 방향키를 마지막으로 썼으면 키보드 목표. 걷기만으로는 우선권을 바꾸지 않는다. 키보드가 우선인 동안 이동·방향·사다리면 키보드 과녁이 따라가고, 마우스가 우선인 동안 걸어가면 호버 칸을 사거리 기준으로 갱신하다 범위를 벗어나면 숨긴다. 이 규칙을 보고서에 적는다.
4. 모양은 이슈 제안(조절 가능): 칸 네 모서리 청록 ㄱ자 꺾쇠, 아주 천천히 안쪽으로 조였다 풀림, 목표 변경 시 0.08~0.12초 미끄러짐. 새 아트 없이 코드로 그린다. 조임 주기는 천천히(예: 1.5~2초) 두고 보고서에 적는다. `ReduceMotion`이면 조임·미끄러짐 없이 정지 꺾쇠. 새 SFX 없음.
5. 건설 모드(`BuildingPlacementActivity.IsActive`), 설정창, 창이 열려 컨트롤러가 꺼진 때, 지상 기지에서는 키보드·마우스 과녁을 모두 띄우지 않는다.
6. EditMode: 키보드 조회 칸 = 엔터/방향키 시작 칸(빈 앞면의 위·아래 대체 포함), 마우스 조회 칸 = `TryStartMiningAtWorldPoint`와 동일, 사거리 밖·불가 지형·드릴 부족이면 과녁 없음, 사다리에서 키보드·마우스 모두 좌우만, 최근 입력 우선권, ReduceMotion이면 이동 없음. 기존 채굴 테스트를 깨지 말 것.

## 테스트 (플레이 가이드)
1. 광산에서 엔터를 누르기 전에, 바라보는 쪽의 캐질 블록에 과녁이 보이는지 확인한다.
2. 앞이 비어 위나 아래를 캐는 자리에서, 과녁이 있는 칸과 엔터로 실제로 캐지는 칸이 같은지 본다.
3. 손이 안 닿거나, 캘 수 없는 지형이거나, 드릴 레벨이 모자라면 과녁이 사라지는지 확인한다.
4. 걷거나 방향을 바꾸거나 사다리에 오르내릴 때 키보드 과녁이 바로 따라가는지, 사다리에서는 좌우만 가리키는지 본다. 채굴 방향키(설정 조작 방식)를 쓰면 그 방향 칸에 뜨는지 확인한다.
5. 마우스를 사거리 안의 캘 수 있는 블록 위에 올린다. 클릭하기 전에 과녁이 그 칸에 보이는지, 클릭으로 캐지는 칸과 같은지 본다. 사거리 밖이나 캘 수 없는 칸에서는 과녁이 없는지 확인한다.
6. 키보드 과녁이 보이는 상태에서 마우스를 다른 유효 칸으로 움직이면 과녁이 호버 칸으로 바뀌는지, 다시 엔터나 채굴 방향키를 쓰면 키보드 목표로 돌아오는지 본다.
7. 엔터 또는 마우스로 캐는 동안 과녁이 그 칸에 붙어 있고, 블록이 깨지면 다음 목표로 옮겨 가거나 사라지는지 본다.
8. 자원 반짝임, 구조 균열(`StructuralCrackOverlay`), 드론 스캔 표식(`DroneScanPulseView`)을 과녁이 가리지 않는지 본다.
9. 가이드·업그레이드·설정 창이 열려 있거나 건설 모드일 때, 그리고 지상 기지에서는 과녁이 없는지 확인한다. 모션 감소를 켜면 조임·미끄러짐 없이 정지 꺾쇠인지도 본다.
10. Unity Console에 새 빨간 오류가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 키보드·마우스 채굴 목표 칸에 과녁 표시 (프롬프트 B-202)`). `MVP2-fix146`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue306-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #306은 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-202-keyboard-mining-reticle-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일
  - 조회 함수 이름·시작 경로와 공유한 방식, 과녁을 숨기는 조건(전력 부족 처리 포함)
  - **우선권:** 가장 최근 입력이 이긴다. 마우스가 마지막으로 움직였으면 호버 칸, 엔터/채굴 방향키를 마지막으로 썼으면 키보드 목표. 걷기만으로는 우선권을 바꾸지 않는다.
  - 조임 주기·색·정렬 순서, 모션 감소 적용 위치
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드, 필요 아트
- **완료 체크리스트**: [ ] `MVP2-fix146` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음 [ ] 키보드 조회와 엔터/방향키 채굴 칸이 동일 [ ] 마우스 조회와 클릭 채굴 칸이 동일 [ ] 사다리 좌우만, 수직 대체 없음 [ ] 불가·드릴 부족·사거리 밖이면 과녁 없음 [ ] 최근 입력 우선권(마우스 이동 vs 엔터/방향키) [ ] 이동·방향·사다리에서 따라감 [ ] 창·건설 모드·지상에서 과녁 없음 [ ] 균열·스캔·자원 표시를 가리지 않음 [ ] ReduceMotion 시 정지 꺾쇠 [ ] 채굴 시작·목표 선정 규칙 미변경 [ ] 새 SFX 없음 [ ] 프리팹 YAML 손수정 없음 [ ] EmergencyRescue·완충판 홀로그램 미수정 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] 브랜치 삭제 안 함 [ ] #306 미수정
