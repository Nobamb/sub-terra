# B-151 마우스 커서 드릴·집게 결과

## 사전 확인

- OS `Cursor` API 사용처: **없음**(`Cursor.SetCursor`/`visible`/`lockState` 0건). 지금까지 기본 OS 화살표만 썼다. "손모양 호버"는 실제 커서 변경이 아니라 `FacilityMoveHoverHint`(우클릭 · 이동 안내 태그)였다. 이번 청록 팁은 이 안내가 뜨는 조건(옮길 수 있는 시설 호버)에 맞췄다.
- 입력 경로: 좌클릭 채굴은 `PlayerMiningController`(Gameplay, `leftButton.isPressed`), 우클릭 이동은 `GameplayBuildingPlacementBridge.HandleWorldRightClick`. 두 로직 모두 변경 없음. 커서는 읽기만 한다.

## 에셋·슬라이스

- `Art/UI/Gameplay/Cursor/mouse-cursor.png`(1774×887), `cursor-right-click.png`(1254×313)을 그대로 복사했다. 새로 그린 것 없음.
- 폭이 칸 수로 나눠떨어지지 않아(443.5 / 313.5px) Grid By Cell Count 대신 **같은 크기 칸으로 수동 슬라이스**했다. 드릴 443×443 8칸, 집게 313×313 4칸. 알파 경계를 재 보면 모든 그림이 칸 안에 여유 있게 들어간다.
- 핫스팟(피벗): 알파로 측정한 끝 좌표. 드릴 (55,60)px → (0.124, 0.865), 집게 (64,44)px → (0.204, 0.859). 드릴 8칸은 끝 위치가 ±2px 안으로 정렬돼 있어 칸마다 같은 피벗을 쓴다.
- 밉맵·Trilinear: 443px 칸을 48px로 줄여 그려서 계단 현상을 줄인다.

## 구현

- `Scripts/App/UI/GameplayCursor/`
  - `GameplayCursorSkin`(SO, `Resources/UI/GameplayCursorSkin.asset`): 프레임 참조, 표시 크기(드릴 칸 48px, 집게 칸 55px; 그림 기준 약 41px), 튜닝(시작 속도, 가속·감속, 회전 전환 fps, 떨림, 펄스, 청록 빛, 파티클)을 인스펙터에서 조절한다.
  - `GameplayCursorModel`: 순수 C#. 속도 0~1을 2~8번 칸으로 바꾸고, 인접 칸을 번갈아 보여 회전처럼 보이게 한다. 빠를수록 전환이 잦다. 감속 중 다시 누르면 현재 속도에서 이어서 올라간다.
  - `GameplayCursorController`: 최상위 오버레이 Canvas(sortingOrder 32767, GraphicRaycaster 없음, CanvasGroup.blocksRaycasts=false, 모든 Graphic raycastTarget=false). `Hotspot`은 마우스 좌표에 그대로 두고, 떨림은 자식 `Visual`에만 준다.
  - `GameplayCursorState` / `IGameplayCursorStateSource`: App이 통합 계층을 참조하지 않도록 배치 브리지가 상태를 등록한다.
- `GameBootstrapper`: Play 중 Instance를 잡을 때 `AppRoot` 아래에 `GameplayCursor`를 한 번 붙인다(try/catch).
- `GameplayBuildingPlacementBridge`: 읽기 전용 상태 3개(`CursorMoveMode`/`CursorCarrying`/`CursorHoverGrabbable`)와, 메뉴로 대상을 고르는 중의 호버 조회만 추가했다. 기존 이동·배치 분기는 변경 없음.
- 빌더: `SubTerra/UI/Build Prompt-B 151 Gameplay Cursor`(`PromptB151GameplayCursorBuilder`). 두 텍스처와 스킨 에셋만 다루며 프리팹·씬은 열지 않는다. `SubTerra.App.Editor.asmdef`에 `Unity.2D.Sprite.Editor` 참조를 추가했다(스프라이트 슬라이스 API).

## 동작 규칙

| 상황 | 표시 |
|---|---|
| 손 뗌 | 드릴 1번 고정 |
| 좌클릭 유지 | 3~4번에서 시작해 약 1.6초 만에 7~8번. ±1.2px 떨림(그림만) |
| 뗌 | 약 1.7초에 걸쳐 1번으로 감속. 도중 재클릭 시 이어서 가속 |
| 시설 옮기기(우클릭/메뉴) | 집게 시트. 전환 0.12초 동안 1.14배 펄스 + 청록 링 |
| 대상 고르는 중 / 시설 호버 / 옮기는 중 | 집게 2번 / 3번 / 4번(잡기) |
| 옮길 수 있는 시설 호버 | 끝에 청록(#3FE0D0) 빛 + 작은 파티클(최대 8개, 2.5~4.5px) |
| 옮기기 종료 | 드릴로 돌아오며 그 순간 좌클릭 상태 속도를 그대로 쓴다 |

## 커스텀 커서를 켜는 화면

- **SurfaceBase, Mine_Demo_Integration**(인게임 전부). MainMenu와 Bootstrap에서는 OS 커서를 쓴다. 인게임 설정 팝업 위에서도 커서가 맨 앞에 나온다.

## 지연(lag)

- 위치는 `LateUpdate`에서 매 프레임 `Mouse.current.position`을 읽어 맞추고, 연출 시간은 `unscaledDeltaTime`을 써서 timeScale 0(팝업 정지)에서도 멈추지 않는다.
- 소프트웨어 커서라 OS 하드웨어 커서보다 렌더 파이프라인상 약 1프레임 늦는 것은 구조적으로 남는다. 저프레임에서는 체감될 수 있다. 하드웨어 커서(`Cursor.SetCursor`)로는 떨림·빛·파티클을 할 수 없어 스펙대로 UI 방식을 유지했다.

## 테스트

- 새 EditMode `PromptB151GameplayCursorTests` 11개 전부 통과: 슬라이스 8/4·끝 피벗, 정지 1번, 가속, 감속, 감속 중 재클릭 비리셋, 이동 ON 집게/OFF 드릴, 호버일 때만 청록 빛·파티클, raycast 비차단, 떨림이 핫스팟을 움직이지 않음, 상태 제공자 연동, 적용 씬.
- 전체 EditMode: 1808 통과 / 19 실패. 실패는 이번 변경과 무관하다. 브리지·부트스트래퍼 수정을 되돌린 트리에서 같은 18개가 똑같이 실패했다(빌딩 패널 폭, HUD 수치, 스냅샷 필드, 시설 타일맵, `PlayableMineScene` Missing terrain 등). 나머지 1개 `PromptB150MainMenuTests.DeleteButton_Hover_FadesInWhiteTrashIcon`은 전체 실행 부하에서만 실패하고 단독 실행하면 통과하는 타이밍 테스트다.
- 전체 실행 중 기존 테스트의 빌더가 프리팹·폰트·씬·`TimeManager.asset`을 건드려서, 해당 파일은 `git restore`로 원복했다.
- PlayMode: 이번 작업에서 추가하거나 수정한 PlayMode 테스트가 없다.
- 에디터 확인(Bootstrap에서 Play): MainMenu에서는 OS 커서가 보이고 커스텀 커서는 꺼져 있다. SurfaceBase에서는 OS 커서가 숨고 드릴이 표시되며, 마우스 좌표와 드릴 끝이 일치한다. 가짜 상태 제공자로 집게 3번과 청록 빛·파티클이 나오는 것도 확인했다.

## 수동 확인 필요

- 실제 탐사 씬에서 좌클릭 채굴 중 가속·떨림 체감, 우클릭 시설 이동 시 집게 전환, UI 버튼·팝업 클릭이 막히지 않는지.
