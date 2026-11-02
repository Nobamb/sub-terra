# B-172 채굴·쓰러지기·사다리 그림 교체

- 작업 브랜치: `MVP2-fix116`
- 기준: `5cccca0385fcce0ca0dc7c21345edda411c3e790` (`main`, 걷기 40장 병합)
- 요청: `prompt-B-172.md`, Refs #264
- 단순 반복 작업: GPT-6 Luna에 원본 측정·정렬·프레임 산출, 가이드 매핑, 순서 테스트 초안 및 GUID/PNG 검사를 위임했다. 주 에이전트가 측정 경계, 코드, 실제 실행 결과와 화면을 검토했다.

## 반영한 내용

| 대상 | 변경 |
| --- | --- |
| 게임 그림 | 채굴 24장, 쓰러지기 48장, 사다리 5장. 기존 채굴·쓰러지기 01~08 및 사다리 5장의 GUID 총 21개 유지 |
| 채굴 | 시작 → 파기 정재생/빠른 되돌림 → 실제 Completed 이벤트 → 14번 유지 → 15~24번 마무리 |
| 쓰러지기 | 24fps로 1~48번. 가스만 37~48번 12fps 반복, 무너짐·추락 등은 48번 유지 |
| 구조 복귀 | CanAct 복구 후 원인·채굴 시퀀스·피격 유예를 초기화하고 기본 동작으로 복귀 |
| 사다리 | 이동 거리·그림 순서·효과음 유지. 등 그림과 접촉 유예 동안 양수 배율, 벗어나거나 채굴 그림이 나오면 현재 FacingDirection 적용 |
| 가이드·터미널 | 게임과 같은 그림, 사다리 순서 1,2,3,2,1,4,5,4. 채굴 24장을 약 2초에 한 번 보여 주고 14번 시점에 블록이 사라짐 |

사용자 확인에 따라 **기존 채굴 입력 규칙을 유지**했다. 버튼을 떼어도 이미 시작한 블록은 끝까지 캔다. 실제 `Cancelled` 이벤트나 기존 `SetMining(false)`가 발생한 경우에만 모션을 되감는다. 버튼 해제만으로 채굴 판정을 새로 취소하는 코드는 추가하지 않았다.

이동·점프·사다리 이동 입력은 마무리 중에도 모션을 바로 끊는다. 피격 그림은 기존 우선순위와 0.35초를 유지하며, 끝났을 때 여전히 채굴 중이면 5번부터 재개한다. 완료 후 `SetMining(false)`는 마무리를 끊지 않는다. 마무리 중 새 채굴은 1번부터, 완료 전 짧은 취소 후 재시작은 작업 자세를 이어간다.

## 그림 정렬과 가져오기

정렬 스크립트: [`../tools/prepare_b172_frames.py`](../tools/prepare_b172_frames.py). 각 원본 폴더의 `runtime-frames/`에 출력하며 원화를 다시 그리거나 부위를 변형하지 않는다. RGBa(미리 곱한 알파) 상태에서 Lanczos로 축소한다.

| 기준/동작 | 헬멧 주황 돔 폭 | 공통 배율 | 이동량 (출력 px) | 캔버스 | PPU / 피벗 |
| --- | ---: | ---: | --- | --- | --- |
| 걷기 원본 1번 | 274px | 기존 실제 값 296/960 = 0.3083333 | 기존 유지 | 256×256 | 256 / (0.5,0.5) |
| 쓰러지기 | 315px | 0.307 × 274/315 = 0.2670412698 | (-1,-3) | 256×256 | 256 / (0.5,0.5) |
| 채굴 | 346px | 0.307 × 274/346 = 0.2431156069 | (11,20) | 256×256 | 256 / (0.5,0.5) |
| 사다리 | 기존/신규 불투명 높이 951/1153px | 0.8248048569 | (124,95) | 1254×1254 | 기존 1084 / (0.5,0.5) |

헬멧 측정에서 램프와 챙을 제외했다. 채굴·쓰러지기 배율이 약 9.8% 달라 평균값으로 통일하면 전환 시 머리 크기 차이가 남으므로 동작별 배율을 사용했다. 각 동작 안에서는 전 프레임에 같은 배율과 이동량을 적용한다. 명세 공식의 0.307과 기존 걷기 도구의 실제 0.3083333은 측정 JSON에 구분해 기록했다.

첫 프레임의 발선은 이미지 위에서 y=240, 아래에서 16px이며 중심은 x=127.5다. 걷기 발선과 일치한다. 투명 픽셀을 포함한 실제 PNG 경계의 최소 여백은 쓰러지기 12px, 채굴 13px, 사다리 99px로 전부 2px 이상이다. 드릴 끝과 엎드린 몸 모두 256 캔버스에 들어가므로 캔버스·피벗 확대가 필요 없었다.

사다리 1번의 불투명 높이는 기존 951 → 신규 956px(+0.5%), 중심 633 → 632.5, 발 y=1102 유지다. 5번은 원화의 웅크림 때문에 발이 약 32px 들린다. 프레임별 위치를 따로 보정하지 않았다. 원본 5번의 왼쪽 위 점은 원본 파일을 보존한 채 정렬 과정의 복사본에서만 제거했다(alpha>0 경계 [15,15,28,28]).

채굴 `13.png`, `14.png`가 이미 숫자 이름으로 제공되어 이름을 바꾼 파일은 없다.

가져오기: Sprite Single, FullRect, Bilinear, 밉맵 없음, 압축 없음, 알파 투명, Clamp. 기존 `.meta`를 삭제하거나 새로 만들지 않았다. 새 프레임의 `.meta`는 Unity가 생성했다.

## 재생 설정

Player 프리팹 VisualRoot의 PlayerAnimationController에 저장된다.

| 설정 | 값 | 이유 |
| --- | ---: | --- |
| startFps | 20 | 1~4번 0.2초 |
| workFps | 12 | 5~13번 정재생 |
| returnFps | 30 | 13→되돌림 지점, 완료 시 13번까지 빠르게 정재생 |
| miningReturnFrame | 5 | Inspector에서 설정, OnValidate 및 순서 계산에서 5~12 제한 |
| completedHoldSeconds | 0.12 | 14번 완료 자세 |
| finishFps | 20 | 15~24번 0.5초 |
| cancelMaxSeconds | 0.3 | 실제 취소 시 현재 자세부터 1번까지 짧게 역재생 |
| restartGraceSeconds | 0.15 | 짧은 취소 뒤 자세 이어가기 |
| knockoutFps | 24 | 48장 2.0초 |
| knockoutBreathingFps | 12 | 가스 숨쉬기 12장 1.0초 |

쓰러진 원인은 FailureRequested DTO의 cause를 저장하고, DTO가 없거나 Unknown이면 survival.State.LastDamageCause를 사용한다. 가스 RescueRequired도 이 경로로 들어간다. 전력 고갈·채굴 시간·에너지·효과음 코드는 변경하지 않았다.

`LadderMinerStyleBuilder.cs`를 확인한 결과 현재 기준 버전은 PNG를 생성하지 않고 가져오기 설정과 프리팹 참조만 적용한다. 따라서 해당 파일을 불필요하게 수정하거나 막지 않았다. 새 `RedrawnMiningKnockoutLadderBuilder`가 승인된 runtime PNG만 원자 교체하고, 대상 프리팹·두 미리보기 클립·GameGuideSkin만 갱신한다. 씬은 저장하지 않는다. 기존 일반 애니메이션 빌더의 Mining/Knockout 장수도 갱신해 재실행으로 8장 설정이 돌아오지 않게 했다.

## 검증 기록

| 검사 | 결과 |
| --- | --- |
| 채굴 순서 클래스 | 10/10 통과 |
| 새 동작·프리팹·에셋·가이드 | 8/8 통과 |
| 최종 관련 EditMode + 호버 재확인 | 40/40 통과 |
| Bootstrap → 새 게임 → 탐사 PlayMode | 1/1 통과, 최종 9.8초 |
| 전체 EditMode, B-172 | **2,169 통과 / 25 실패**, 64.6초 |
| 기준 main 비교 | **2,152 통과 / 24 실패**, 61.2초 |
| PNG·GUID | 77/77 바이트 일치·2px 투명 여백, 기존 GUID 21/21 유지 |
| git diff --check | 통과 |

전체 검사의 실패 24개는 main 상태에서도 같은 이름으로 재현됐다. 기준 비교는 수정한 추적 파일을 백업한 뒤 HEAD 버전으로 복구하고, 새 C# 다섯 파일을 조건부 컴파일로 잠시 제외해 같은 에디터에서 수행했다. 추가된 PNG 파일은 디스크에 남았지만 기준 main의 기존 그림 참조·프리팹·코드로 검사했다. 검사 후 작업 파일을 백업에서 복원했다.

B-172 전체 실행에서만 추가 실패한 `PromptB150MainMenuTests.DeleteButton_Hover_FadesInWhiteTrashIcon`은 수정하지 않은 기존 검사다. `Time.unscaledDeltaTime`이 고정된 동기 EditMode 루프 안에서 Update를 200번 호출하므로, 검사 시작 프레임 시간에 따라 결과가 달라진다. 최종 관련 검사와 함께 재실행하면 통과했다. 이 검사로 인해 **전체 검사가 모두 통과한 상태는 아니다**. 관련 없는 메인 메뉴 코드를 수정하지 않았다. 전체 실패 목록은 아래 원문 로그에 남겼다.

최종 PlayMode에서 실제 MiningSystem Completed 뒤 14·24번 표시, 가스 2초 쓰러지기 뒤 37~48번 반복, 구조 뒤 Idle, 사다리에서 FacingDirection은 음수이면서 VisualRoot x는 양수, 벗어난 뒤 음수 복귀를 검사했다. 채굴·가스·사다리와 가이드/터미널의 실제 Stage 렌더링 화면을 캡처하고 묶어서 검토했다. 원화 세부를 보기 위해 테스트에서만 카메라를 확대했고, 쓰러지기 상태는 유지한 채 실패 창만 숨겼다. 세이브는 MemoryFileSystem으로 격리했다.

검사 도중 생긴 범위 밖 씬·UI 프리팹·ProjectSettings·이전 작업 캡처 변경은 복구했다. 기존 폰트 수정은 최초 백업 바이트를 복원해 보존했다. Packages 및 걷기·대기·점프·피격 그림/속도는 변경하지 않았다. 검증 중 발생했던 컴파일 보조 타입 참조와 PNG 쓰기 오류는 수정됐고 최종 PlayMode에 새 오류는 없었다. 기존 URP stereoTargetEye 경고·API obsolete 컴파일 경고·에디터 라이선스 진단 로그는 남아 있다.

가스 48→37 이음은 요청한 정재생을 그대로 사용한다. **숨쉬기 루프 연결의 자연스러움은 미검증**이며, 왕복 재생으로 바꾸지 않았다. 단단한 블록의 장시간 반복·연속 채굴·실제 좌우 키와 사다리 오르내리기·밝은 지상과 어두운 광산의 경계 픽셀은 아래 수동 점검이 남는다.

- [전체 EditMode 로그](B-172-verification/editmode-full.txt)
- [기준 main 로그](B-172-verification/main-editmode.txt)
- [최종 관련 EditMode 로그](B-172-verification/editmode-targeted.txt)
- [최종 PlayMode 로그](B-172-verification/playmode.txt)

- [그림·GUID 검증 JSON](B-172-asset-verification.json)
- [플레이 화면 묶음](B-172-play-contact-sheet.png)
- [채굴 걷기 크기 비교](../images/player-mining-24f-20261029/walk-comparison-contact-sheet.png)
- [쓰러지기 걷기 크기 비교](../images/player-knockout-48f-20261029/walk-comparison-contact-sheet.png)
- [사다리 기존/신규 비교](../images/player-ladder-5f-20261029/old-new-ladder-contact-sheet.png)
- [채굴 GIF](../images/player-mining-24f-20261029/runtime-preview.gif)
- [쓰러지기 GIF](../images/player-knockout-48f-20261029/runtime-preview.gif)

## 수동 확인 체크리스트

체크되지 않은 항목은 **미검증**이다. 자동 상태 검사와 화면의 자연스러움 평가는 구분한다. 4번의 버튼 해제는 위 사용자 결정이 우선한다.

1. [x] 블록을 캐면 드릴을 내리고(시작) → 파는 동작이 자연스럽게 반복되고 → 블록이 깨질 때 완료 자세 → 드릴을 들고 일어선다.
2. [ ] 단단한 블록(오래 걸림)에서 13→5 되돌아가는 반복이 끊기거나 튀어 보이지 않는다.
3. [ ] 무른 블록(빨리 깨짐)에서도 마무리까지 어색하지 않다.
4. [ ] 채굴 중 이동·점프하면 바로 움직인다. 버튼만 떼면 짧게 일어서며 대기로 간다.
5. [ ] 블록을 연달아 캘 때 매번 처음부터 크게 튀지 않는다.
6. [x] 채굴·쓰러지기 그림의 크기와 발 높이가 걷기·대기와 비슷해서 바뀔 때 커지거나 떠 보이지 않는다. 드릴 끝이 잘리지 않는다.
7. [x] 가스로 쓰러지면 약 2초 동안 쓰러진 뒤 엎드려 숨 쉬는 동작이 계속된다.
8. [ ] 무너진 돌·추락으로 쓰러지면 엎드린 자세에서 멈춘다.
9. [x] 구조·복귀 후 바로 서 있는 그림으로 돌아온다.
10. [ ] 왼쪽을 보고 채굴·쓰러지기를 해도 좌우만 뒤집혀 같게 보인다.
11. [ ] 사다리에서 좌우 키를 눌러도 등 그림·헤드램프가 좌우로 뒤집히지 않는다. 사다리에서 내려와 걸으면 방향이 맞다.
12. [ ] 새 사다리 그림이 오르내릴 때 손발이 번갈아 자연스럽게 움직이고, 램프가 작게 보여 튀지 않는다. 사다리에 붙거나 내릴 때 크기·위치가 튀지 않는다(5번 그림에서 몸이 살짝 들려 보이는지 적기).
13. [x] 게임 가이드·관제 터미널 데모의 사다리·채굴이 게임과 같은 그림으로 나온다.
14. [ ] 밝은 지상·어두운 광산에서 가장자리 검은 테두리나 깨진 픽셀이 없다.
15. [ ] Console에 새 경고·오류가 없다.

11번의 배율·현재 방향 복구는 실제 PlayMode 자동 검사에서 통과했으며, 사용자가 실제 키를 눌러 확인하는 항목으로는 남겨 두었다. 8번은 원인별 EditMode 검사에서 48번 유지가 통과했고 실제 붕괴·추락 연출은 미검증이다. 4번의 버튼 해제 문장은 원본 체크리스트를 보존한 것이며 사용자 결정대로 이번 변경의 기대 동작에서 제외한다.

## 변경 파일과 재적용

- `Assets/_Project/Art/Characters/Player/Frames/Mining/mining_01~24.png` 및 `.meta`
- `Assets/_Project/Art/Characters/Player/Frames/Knockout/knockout_01~48.png` 및 `.meta`
- `Assets/_Project/Art/Characters/Player/Frames/LadderBack/ladder_back_neutral_v2.png`, `ladder_back_style_02~05.png`
- `Assets/_Project/Art/Characters/Player/Animations/PlayerMining.anim`, `PlayerKnockout.anim`
- `Assets/_Project/Prefabs/Gameplay/Player/Player.prefab`
- `Assets/_Project/Scripts/Gameplay/Player/PlayerAnimationController.cs`, `PlayerFacing.cs`, `PlayerMiningAnimationSequence.cs`
- `Assets/_Project/Scripts/Gameplay/Mining/PlayerMiningController.cs`
- `Assets/_Project/Scripts/App/UI/Guide/GuideDemoLibrary.cs`, `UI/ControlTerminal/ControlTerminalDemoClips.cs`
- `Assets/_Project/Editor/RedrawnMiningKnockoutLadderBuilder.cs`, `PlayerAnimationAssetBuilder.cs`, `Editor/DataValidation/PromptB140GameGuideSkinBuilder.cs`
- `Assets/_Project/Resources/UI/GameGuideSkin.asset`
- `Assets/_Project/Tests/EditMode/App/Readiness/PlayerMiningAnimationSequenceTests.cs`, `PlayerRedrawnActionTests.cs`, `PhaseCTraversalStaticTests.cs`
- `Assets/_Project/Tests/PlayMode/Integration/MineDemo/PlayerRedrawnActionPlayModeTests.cs`
- 새 C# 파일의 Unity 생성 `.meta`, 새 정렬 도구·runtime-frames·측정 JSON·비교 이미지·이 보고서와 검사 로그
- `init/rules/lessons.md`: PNG 파일 매핑과 검사 보조 타입 접근 범위에 대한 교훈 2줄

재적용: Unity 프로젝트 기준 `SubTerra/Player/Build B-172 Mining Knockout Ladder`. 승인 runtime-frames 77장이 먼저 있어야 한다. 별도 씬 배선이나 Inspector 수동 연결은 필요 없다. 저장 DTO나 세이브 형식 변경은 없다.

커밋·push·PR은 실행하지 않았다.
