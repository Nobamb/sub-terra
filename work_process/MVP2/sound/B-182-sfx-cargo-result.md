# B-182 화물 공간 부족·버리기 창 효과음 연결

## 기준
- 기준 브랜치: `origin/MVP2-fix128`, 커밋 `43e0efda`.
- `git fetch` 뒤 원격 fix 번호 최댓값 128 확인. 새 브랜치 `MVP2-fix129` 생성. 계획과 같은 번호이며 다음 작업은 빈 번호를 다시 확인한다.
- B-179 `b1471534`, B-179-1 `2b474db7`, B-180 `22f2f5f5`, B-180-1 `d2f54697`, B-181 `7c2c2957` 및 후속 `43e0efda` 포함.
- B-181 배율 API가 있어 TV 켜짐·꺼짐에 **0.6배 적용**. 새 음원·SfxId·라이브러리 칸·버스 음량 변경 없음. Alert 버스 사용 없음.
- GitHub #158의 E 화물 6줄을 읽고 아래 대응 표에만 기록했다. 이슈는 수정하지 않았다.
- 사용자의 후속 커밋·push·PR 생성 요청에 따라 `MVP2-fix129`를 커밋·push하고 `main` 대상으로 PR을 생성한다. 이번 요청에 해당하는 `init/prompt-B.md`의 182번 추가와 `prompt-B-182.md`도 함께 포함한다.

## 현재 기준과 프롬프트의 차이
프롬프트와 #158은 B-178의 지급 순간 보상음(87)을 전제한다. 그러나 B-180-1에서 보상음은 `QuestClearPopupMotion`의 완료 체크 표시 순간으로 이동했고, `DemoObjectivePresenter.OnGrantResolved`에서는 재생하지 않는다. 선행 소리 연결을 바꾸지 말라는 지시에 따라 이 동작을 유지했다.

따라서 공간 확보의 마지막 버리기 클릭은 `SkipDiscardSfxOnComplete = true`로 틱·전부 소리를 생략하지만 **그 프레임에 보상음 87을 새로 내지는 않는다**. 지급은 기존 서비스가 한 번 수행하고, 0.4초 완료 표시 뒤 작은 닫힘음이 난다. 아래 원문 사용자 가이드 7번의 ‘틱 대신 보상음’ 기대는 현재 기준과 다르다.

## 연결 위치와 ★ 상수
경로는 `sub-terra/Assets/_Project/Scripts/` 기준.

| 순간 | 파일·연결 위치 | 소리·배율 | ★ 상수 |
|---|---|---|---|
| 부족 창 새 열기 | `App/UI/Tutorial/CargoFullPopupView.cs:220`, `Show(false)` | WindowTvOpen(79), 0.6 | 같은 파일 31~33행 `CargoOpenSfx`, `CargoTvVolume` |
| 게이지 넘침 | 같은 파일 327행, `GaugeEnd` 교차 | CargoHeavyWarning(65), 1 | 32행 `CargoOverflowSfx` |
| 버리기 창으로 이동 | `App/UI/Tutorial/DemoObjectiveView.cs:1109`, 선택 gate 통과 직후 | KeysPageTurn(49), 1 | 19행 `CargoPageSfx` |
| 보상 포기 | 같은 파일 1134행 | UiCancelFail(5), 1 | 20행 `CargoForfeitSfx` |
| 1·5·10·필요 버리기 | 같은 파일 `RequestDump`, 처리 전후 모델 화물 중량 감소 확인 | UiScanTick(8), 1 | 21행 `DiscardTickSfx` |
| 전부 버리기 | 같은 지점 `CargoDiscardKind.All` | StorageTakeAll(20), 1 | 22행 `DiscardAllSfx` |
| 돌아가기·X | 같은 파일 `OnDumpClosedClicked`, gate 통과 뒤 | UiClick(1), 1 | 23행 `DiscardBackSfx` |
| 완료 표시 뒤 접기 | `App/UI/Tutorial/CargoDiscardPopupView.cs:455`, Complete → Fold | WindowTvClose(80), 0.6 | 같은 파일 47행 `CargoCloseSfx`, Full의 `CargoTvVolume` 공유 |
| 버리기 창 직접 새 열기 | 같은 파일 286행, `Show(false)` | WindowTvOpen(79), 0.6 | Full의 열기·배율 상수 공유 |
| 이어질 페이지가 없어 접기 | Full `FoldInsteadOfPage`, Discard `FoldInsteadOfPage`·`Close(Fold)` | WindowTvClose(80), 0.6, 한 번 | 같은 닫힘 상수 |

- `DemoObjectiveView` 24행 `DiscardTickGap = 0.08f`: 두 버리기 소리가 하나의 `SfxThrottle`을 공유한다. 소리만 제한하며 버리기 규칙·클릭 gate는 바꾸지 않았다.
- 같은 파일 25행 `SkipDiscardSfxOnComplete = true`: 처리 직후 `Closing + Complete`이면 버리기 소리를 생략한다.
- `StorageTakeAll(20)`은 기존 보관함의 묵직한 전체 이동 소리라 프롬프트 권장값을 선택했다. `BlockBreak(62)`는 채굴 파편 용도라 선택하지 않았다. **두 음원의 직접 청취 비교와 체감 적합성은 미검증**이다. 두 칸·음원은 그대로 두었다.

## 무음·겹침·호버
- 새 열기를 시작할 때 넘침음을 무장하고 `SfxPlayback.Crossed(previous, clock, GaugeEnd)`로 한 번 소비한다. 큰 dt로 `SettleOpen`을 건너뛰는 경우도 한 번이다. 페이지로 복귀할 때는 열림·넘침음이 없다.
- 부족 창의 일반 `Close(false)`에는 닫힘음을 넣지 않았다. 따라서 보상 포기와 닫힘음이 겹치지 않는다. 페이지 이동이 접기로 바뀌는 경우에만 별도 닫힘음이 난다.
- 버리기 창은 `Show` 때 접힘음을 무장하고 Fold 시작 때 한 번 소비한다. Complete는 기존 완료 표시 0.4초 뒤 Fold로 바뀌는 시점에 소비한다. Page 닫기는 무음이다.
- `HideImmediate`, `OnDisable`에서 남은 무장을 버린다. 비활성 컴포넌트는 새 소리를 요청하지 않는다. EditMode와 ManualTick 미리보기도 기본 무음이다. EditMode 검사에서만 비공개 검사용 스위치를 명시적으로 켠다.
- 불러오기 경로를 확인했다. `QuestRewardService.SyncFromProgress`는 복원한 보류 보상을 곧바로 부족 창으로 열지 않고 **완료 창 확인 대기(OfferClaim)**로 올린다. 이후 플레이어가 완료 창을 닫아 부족이 판정되면 일반 사용자 동작으로 새 열림음을 낸다.
- 추가로 Presenter의 Bind/Unbind 동안 실제 View의 `SetCargoSfxQuiet`로 복원·정리 소리와 남은 예약음을 억제한다. Bind의 `finally`에서 이후 사용자 동작을 다시 허용한다. 시간 기반 `CargoStartupQuiet` 상수는 경로 구분을 사용하므로 필요하지 않다.
- 부족 창 2개, 버리기 수량 15개, 돌아가기 1개, 총 **18개 버튼**에 `UiSfxButton`을 한 번씩 부착한다. 클릭 None, hover true, failWhenDisabled false. 기존 `IsInteractable`로 열기 중 호버를 막고, hoverGate로 EditMode·ManualTick·복원·정리 중 호버도 막는다.
- X키의 기존 `TryCloseTopPopup`은 `OnDumpClosedClicked`를 호출하므로 돌아가기와 같은 소리 경로를 쓴다. 실제 키보드 입력은 미검증이다.

## 검증
- 관련 EditMode: **87 통과 / 0 실패**. 초기 개별 실행은 `sub-terra/Temp/b182-verified/`, 최종 상태는 아래 전체 실행 XML에서 같은 87개를 다시 추출해 전부 통과함을 확인했다. 그중 새 B-182 검사는 19개다. 기존 검사 기대값 변경 없음.
- 최종 새 PlayMode: **3 통과 / 0 실패** (`sub-terra/Temp/b182-playmode-verified/`). 실제 `Update`의 시간 진행으로 열림·넘침·완료 뒤 접힘, Page 복귀, 정리·미리보기의 호버 포함 무음을 확인했다. 테스트 전용 Canvas의 실제 렌더링이므로 Bootstrap부터의 통합 플레이 검증을 대신하지 않는다.
- 마지막 클릭은 EditMode에서 실제 InventoryService·QuestRewardService·Presenter·View를 연결해 ‘필요’와 ‘전부’ 각각 지급 1회, 중복 지급 0회, 틱·전부·새 보상음 요청 0회를 확인했다. 완료 표시 직전까지 닫힘음 0회, 이후 80을 한 번 요청함을 확인했다.
- PlayMode 화면 3장(절반 해상도): `sub-terra/Temp/b182-play-screens/`. 부족 창, 버리기 창, ‘공간 확보 완료’ 표시 화면을 검토했다. 기존 문구·게이지·버튼·외곽 프레임이 표시됨을 확인했다.
- 최종 전체 EditMode: **2,354 통과 / 23 실패 / 총 2,377개**, 새 실패 **0개** (`sub-terra/Temp/b182-full-editmode-verified/`). 23개 모두 `work_process/MVP2/B-172-verification/editmode-full.txt`의 기존 실패 목록에 있다. 비교 결과는 같은 출력 폴더의 `baseline-comparison.txt`에 저장했다.
- 중간 재실행에서는 기존 `PromptB153CargoDiscardPopupTests.Font_HasEveryPopupCharacter`가 추가로 실패했다(글자 전체가 없다는 결과). 테스트가 바꾼 폰트를 되돌리고 Editor가 들고 있던 글꼴 상태의 영향으로 추정한다. 도메인 재로드·기존 폰트 재임포트 뒤 단독 1개 통과, 최종 전체 실행에서도 통과했다. 폰트·글리프·아틀라스를 재생성하거나 영구 수정하지 않았다. 초기 새 검사에서 빠진 EditMode OnDisable 호출을 명시적으로 보완했고, 관련 교훈을 남겼다.
- 테스트가 자동 변경한 `NotoSansKR-Regular_SDF.asset`, `InventoryPanel.prefab`, `MainMenuPanel.prefab`, `ViewSocket.prefab`, `Mine_Demo_Integration.unity`, `ProjectSettings/TimeManager.asset`, 기존 메인 메뉴 PNG 2개를 작업 전 상태로 복구했다. 최종 `git status`·`git diff --check` 확인. ProjectSettings·Packages·폰트·프리팹·씬·음원·SfxLibrary 변경 없음.
- Unity 컴파일 및 최종 관련 EditMode·PlayMode에 새 오류 없음. 전체 검사는 기존 실패 때문에 실패 종료한다. 실제 Bootstrap 플레이 중 Console·청취 확인은 아래 미검증 항목에 남겼다.
- 실수 교훈: `init/rules/lessons/2026-11-04-issue158-cargo-test-disable-lifecycle.md`.

### 미검증
- 실제 게임 Bootstrap → 새 게임 → 탐사 → 화물 부족 흐름의 직접 조작과 청취.
- 0.6배 체감 크기, ‘전부’ 후보 두 소리 청취 비교, 실제 마우스 호버·X키 입력, 설정 음량·음소거 조작.
- 다음 퀘스트 창의 글리치음(88)과 접힘음(80)의 청각적 겹침. 선행 B-178~B-181의 지연·보상음은 변경하지 않았다.
- 실제 저장·이어하기 파일을 이용한 재현. 사용자 세이브는 변경하지 않았다.
- Inspector 새 참조·씬 재빌드 필요 없음. 런타임 빌더에서 기존 버튼에 소리 컴포넌트를 붙인다.

## #158 대응 표 (이슈는 수정하지 않음)
| 항목 | 상태 | 비고 |
|---|---|---|
| 부족 창 켜짐·넘침 경고 | 구현 | 79 0.6배, 65 한 번 |
| 페이지 넘김 | 구현 | gate 통과 뒤 49 |
| 보상 포기 | 구현 | 5, 일반 접힘음 없음 |
| 버리기 틱·전부 | 구현 | 8·20, 중량 감소 확인, 0.08초 간격, 마지막 클릭 생략 |
| 공간 확보 뒤 닫힘·보상 | 일부 | 닫힘 80·기존 지급 1회 확인. 지급 순간 보상음은 기준 B-180-1에서 이미 제거되어 그대로 유지 |
| 돌아가기·X | 구현 | 1, 복귀 열림·경고·Page 닫힘 무음. 실제 X 입력 미검증 |

## 사용자 테스트 가이드 (프롬프트 원문)
화물을 꽉 채운 채로 퀘스트를 깨면 된다. 디버그 터미널(Ctrl + `)로 화물을 채울 수 있으면 그 방법도 보고서에 적어 준다.
1. 화물이 꽉 찬 채로 퀘스트 완료 창을 닫는다 → "화물 공간 부족" 창이 작은 TV 켜짐 소리와 함께 뜨고, 게이지가 넘치는 칸이 밝아질 때 경고음이 한 번 난다.
2. 버튼에 마우스를 올린다 → 호버음. 창이 열리는 동안에는 나지 않는다.
3. "버릴 자원 선택"을 누른다 → 페이지 넘기는 소리와 함께 버리기 창으로 바뀐다.
4. 1개·5개·10개를 누른다 → 누를 때마다 "틱". 빠르게 연타해도 소리가 쌓이지 않는다.
5. "전부"를 누른다 → 더 묵직한 소리가 난다. 버릴 게 없는 칸을 누르면 무음.
6. "돌아가기"나 X키 → 클릭음과 함께 부족 창으로 돌아온다. 이때 켜짐음·경고음은 다시 나지 않는다.
7. 다시 들어가 공간이 딱 맞게 버린다 → "틱" 대신 보상 "띠-링"이 나고, "공간 확보 완료"가 잠깐 보인 뒤 작은 꺼짐 소리와 함께 창이 접힌다.
8. 다음 퀘스트 완료 창이 이어서 뜨면 소리가 한꺼번에 뭉개지지 않는지 들어 본다.
9. 다른 보상으로 1번을 다시 만든 뒤 "보상 버리기"를 누른다 → 실패음만 나고 창이 접힌다(보상 소리 없음).
10. 보상 대기 중에 저장하고 이어하기로 들어온다 → 창이 다시 떠도 켜짐·경고음은 나지 않는다(이 상황이 안 생기면 건너뛰어도 됨).
11. 소리 크기가 다른 창보다 너무 크거나 작지 않은지 본다.
12. 효과음 음량·음소거가 이번 소리에 다 적용되고, Unity Console에 새 빨간 오류·노란 경고가 없다. 바꿔 보고 싶은 소리(★, 특히 "전부" 소리)가 있으면 PM에게 알려 준다.

터미널 명령은 코드에서 확인했다: Editor/Development에서 `Ctrl + 백틱`, `copper 10000`으로 구리를 한도까지 채우고 `questclear` 또는 `qc`로 목표를 진행시킨다. 터미널을 닫은 뒤 퀘스트 완료 창을 닫아 화물 부족을 만든다. `copper -10`, `iron -5`로 화물을 줄일 수도 있다. 이 재현 방법의 직접 조작은 미검증이다.

## 변경 파일
- 런타임: `CargoFullPopupView.cs`, `CargoDiscardPopupView.cs`, `DemoObjectiveView.cs`, `DemoObjectivePresenter.cs` (모두 `sub-terra/Assets/_Project/Scripts/App/UI/Tutorial/`).
- 검사: `sub-terra/Assets/_Project/Tests/EditMode/App/Audio/PromptB182SfxCargoTests.cs`와 `.meta`, `sub-terra/Assets/_Project/Tests/PlayMode/Integration/Audio/PromptB182SfxCargoPlayModeTests.cs`와 `.meta`.
- 문서: 이 보고서와 위 교훈 파일.
- 테스트 검색 중 도메인 리로드로 유실된 이번 요청만 복구하고, 글꼴 상태를 진단·재임포트하는 임시 Editor 스크립트를 사용한 뒤 모두 제거했다. 실행기 자체의 영구 변경은 없다.
