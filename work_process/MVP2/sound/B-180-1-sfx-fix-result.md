# B-180-1 결과: 메인 메뉴 호버 확인 / 잠긴 업그레이드 쇠 소리 / 퀘스트 완료음 시점

## 기준
- 브랜치 `MVP2-fix126`, 기준 `22f2f5f5`(B-180). B-180-1은 B-179-1과 같은 방식으로 같은 브랜치에 이어서 작업했다.
- 명세: [prompt-B-180-1.md](prompt-B-180-1.md). 이슈: [#271](https://github.com/Nobamb/sub-terra/issues/271), [#272](https://github.com/Nobamb/sub-terra/issues/272), [#273](https://github.com/Nobamb/sub-terra/issues/273). 상위 [#158](https://github.com/Nobamb/sub-terra/issues/158).
- 작업 영역: 명세는 별도 작업 영역을 권했다. 하지만 B-179-1처럼 실행 중인 같은 프로젝트(Unity MCP·플래그 실행기)에서 작업했다. 세이브·`ProjectSettings/*`·패키지·씬/프리팹 YAML·음원·`SfxId` 번호·라이브러리 음량은 바꾸지 않았다.
- 새 음원·새 SfxId는 없다. 기존 `UnlockRattle(76)`, `QuestReward(87)`, `BriefingClose(41)`만 다시 썼다.

## A. 메인 메뉴 네 버튼 호버 무음 (#271): 재현 안 됨, 코드 변경 없음
- Bootstrap → MainMenu Play 상태에서 확인했다.
  - 네 버튼 모두 `MainMenuView` 참조와 화면 버튼이 같다(`MainMenuPanel/MenuContent/*Button`). 각각 `UiSfxButton`(hover=true, gate 없음)이 켜져 있고 `IsInteractable()=true`이다. 버튼 중심 레이캐스트의 맨 위는 해당 버튼이고, 자식(Frame·HoverFrame·Icon·Label)은 raycastTarget=false다.
  - 직접 보낸 PointerEnter에서 `UiHover`가 재생됐다(StartedCount +1).
- **실제 마우스(사용자 호버) 확인**: `EditorApplication.update` 감시로 버튼 하이라이트 전환과 `SfxPlayer.SecondsSinceLastPlayed(UiHover)`를 프레임마다 기록했다. 사용자가 직접 호버한 결과는 아래와 같다.
  - 첫 진입: 이어하기·새 게임·설정·종료 각각 진입마다 UiHover 1번. 사용자도 "다 들림"으로 답했다.
  - 설정을 열고 닫은 뒤, 게임에 들어갔다 메인 메뉴로 돌아온 뒤: 진입마다 1번. 사용자도 "둘 다 소리 남"으로 답했다.
  - 버튼 안에 머무르는 동안은 반복되지 않았고, 나갔다가 다시 들어오면 다시 1번 났다.
- 결론: 현재 브랜치 Editor에서는 재현되지 않는다. 원인을 확인하지 못했으므로 명세대로 공통 `UiSfx`나 다른 화면에 추측성 수정을 하지 않았고, 회귀 검사도 추가하지 않았다(재현할 끊김 지점이 없다).
- 남은 가능성(**미확인**): 보고 당시 빌드가 B-180 이전이었거나 Windows 빌드에서만 생기는 현상, 부팅 연출 중(메뉴 그룹 `blocksRaycasts=false`)에 올려 둔 경우, 음소거·효과음 막대 0. 다시 재현되면 재현 순서(빌드/Editor, 부팅 직후 여부, 설정 값)를 알려 주면 같은 감시로 끊기는 지점을 다시 좁힌다.
- MCP로 넣은 가상 마우스 입력은 Game 뷰 포커스 설정 때문에 UI 모듈에 닿지 않았다. 교훈: `init/rules/lessons/2026-11-02-issue158-mcp-input-gameview-focus.md`.

## B. 잠긴 업그레이드 클릭 (#272): 자물쇠 흔들림 + 쇠 소리
`sub-terra/Assets/_Project/Scripts/App/UI/Progression/UpgradeTreeNodeView.cs`
| 항목 | 위치 |
|---|---|
| ★ 소리·시간 상수 `LockedClickSfx = LockedNodeRattle(95)`, `LockRattleDuration = 0.45f`, 각도 12°, 4회 왕복 | ~31-36 |
| 버튼 소리 연결: 잠긴 노드는 클릭음 None, 풀린 노드는 기존 UiClick(호버·호버 조건은 그대로) | `AttachButtonSfx` ~170, `OnEnable` ~128, `Apply` ~270 |
| 클릭 → 쇠 소리 + 흔들림, 그다음 기존 선택(잠김 안내) | `OnPointerClick` ~165, `TryPlayLockRattle` ~191 |
| 흔들림 진행·끝(제자리 각도 흔들림, 끝나면 원래 자세) | `StepLockRattle`, `SampleLockRattle`, `StopLockRattle` |
| 꺼짐·해금 시작·풀림 시 정리 | `StopRoutines`, `BeginUnlock` ~622, `Apply` ~267 |

- **사용자 피드백 반영(바람 소리)**: 처음에는 76번 `UnlockRattle`을 그대로 썼지만, 사용자가 실제로 들어 보니 바람 소리가 났다.
  - 파형 분석(50ms RMS): `unlock_rattle.wav`(0.67초)는 0~0.3초가 −43dB에서 −14dB로 작게 차오르는 부분이고, 쇠 덜그럭은 0.25~0.6초(최대 −11dB)에 있다. 76번은 B-169 해금 연출의 딸깍과 겹치지 않도록 0.3초에서 끊겨 있어서, 단독으로 재생하면 차오르는 부분만 들린다.
  - 창을 열 때는 흔들림(0.15초) 뒤 0.45초에 딸깍(`UnlockClick`)과 금 가는 소리가 바로 이어져서 둘이 합쳐져 '자물쇠 흔들림'으로 들렸다.
  - 그래서 같은 음원을 쓰는 새 칸 **95 `LockedNodeRattle`**를 추가했다(UI, 음량 0.5, startTime 0.15, maxDuration 없음, fadeOut 0.05, 재생 0.52초). 앞의 차오르는 부분만 건너뛰고 덜그럭은 끝까지 낸다. 흔들림 시간도 0.45초로 맞췄다.
  - 76번 칸(창 열기·노드 해금·심층 썸네일 해금)은 그대로다.
  - `SfxLibraryDefaults.cs` 끝, `Shared/Audio/SfxId.cs`, `Resources/SfxLibrary.asset`(빌더 `PromptB156SfxLibraryBuilder.EnsureAsset`로 95번 1칸만 추가)을 바꿨다.
  - 실제 청취는 **미검증**이다.
- 자물쇠(`Lock`)만 각도로 흔든다. 위치·크기·투명도·고리는 그대로이고, 블라인드·잠김 색·해금 조건 안내도 유지된다. 노드 몸통 흔들림(`PlayShake`, 구매 실패)은 구매 버튼 경로라 이번 클릭과 겹치지 않는다.
- 구매·차감은 없다. 선택(`Clicked` → 잠김 안내)은 기존대로다.
- 무시되는 경우(무음·흔들림 없음): 창 열림·닫힘 연출 중(`UpgradeWindowMotion.IsTransitioningFor`), 이 노드의 해금 연출 중, 이미 흔들리는 중(연타는 쌓이지 않고 끝난 뒤 다시 누르면 다시 1번), 꺼진 노드.
- 창을 닫으면 흔들림이 멈추고 자물쇠가 제자리로 돌아온다. 소리는 0.52초 원샷이라 따로 끊지 않는다.

## D. 심층 구역 썸네일 자물쇠 단색화 (사용자 추가 요청)
- `UI/Progression/DeepZoneThumbnailStrip.cs` `LockPart` ~523: 자물쇠 4조각(몸통·고리 좌/우·윗부분)마다 붙던 `Shadow`(검정 α0.65, 1.5px)를 뺐다. 고리 조각이 겹치는 윗부분에서 이 그림자가 외곽선처럼 보였다. 이제 `LockColor` 단색만 그린다.
- 썸네일 띠는 런타임에 코드로 만들어지므로(프리팹 아님) 프리팹·씬 변경은 없다. 지상 기지와 광산 업그레이드 창 모두 같은 코드를 쓴다.
- 업그레이드 노드의 자물쇠(B-118 빌더 프리팹)는 요청 범위가 아니어서 건드리지 않았다.
- 기존 동작 변경: 잠긴 노드 클릭 때 나던 UiClick이 쇠 소리로 바뀌었다(명세는 "클릭음 없음"이었다고 적었지만, B-180 코드는 UiClick을 냈다).

## C. 퀘스트 완료음 시점과 닫힘음 (#273)
| 항목 | 위치 |
|---|---|
| ★ `CheckAppearSfx = QuestReward(87)`, `CloseSfx = BriefingClose(41)` | `UI/Tutorial/QuestClearPopupMotion.cs` ~26-27 |
| 체크가 커지기 직전(`transition.IsShown` 직후, `ScaleCheck(0→1.1)` 전) 한 번 | `Enter` ~116, `PlayCheckSfx` ~141 |
| 이 창만 닫힘음을 브리핑 종료음으로 | `Awake` ~41 → `StartBriefingPopupMotion.SetCompactCloseSfx` ~111, 닫기 `RequestClose` ~299 |
| 보상 지급 경로의 QuestReward 제거(지급 규칙·시점 그대로) | `UI/Tutorial/DemoObjectivePresenter.cs` `OnGrantResolved` ~344 |

- 순서: 창 열림(기존 GlitchPopupOpen) → 체크 등장 시작에 QuestReward 1번 → 확인·X·닫기 키(모두 `RequestClose` 한 경로)에서 BriefingClose 1번. 닫을 때 GlitchPopupClose와 QuestReward는 나지 않는다.
- 작은 창 공용 상수 `CompactCloseSfx(GlitchPopupClose)`는 그대로다. 심층 해금 창과 시작 브리핑 소리는 바뀌지 않는다.
- 체크가 나오기 전에 닫으면 등장 코루틴이 멈춰 완료음이 나지 않는다. 다시 열면(닫는 중 재열기 포함) 새 표시에서 1번 난다.
- 화물 부족 → 버리기·재시도처럼 늦게 지급되는 경우에도 완료음을 다시 내지 않는다(이미 체크 때 났다). B-178의 "지급 순간에 보상음"을 이번 요청대로 바꾼 것이다.
- `DemoObjectivePresenter.ShouldPlayRewardSfx`는 쓰는 곳이 없어 지웠다.

## 테스트
- 새 EditMode `Tests/EditMode/App/Audio/PromptB1801SfxFixTests.cs`(8개)
  - 잠긴 노드: 클릭음 None, 클릭에 LockedNodeRattle(95) 1번·UiClick 0번, 선택은 그대로, 자물쇠 각도 흔들림·위치 고정, 연타에도 1번, 0.45초 뒤 자세 복귀·잠금/블라인드 유지, 끝난 뒤 다시 1번
  - 95번 칸: 파일 unlock_rattle, startTime 0.15, 자르지 않음(에셋 값도 같음), 76번은 0.3초 컷 그대로, 흔들림 시간 ≤ 재생 길이
  - 심층 구역 자물쇠 조각에 Shadow·Outline 없음, 단색(α1)
  - 풀린 노드: UiClick 그대로, 쇠 소리 없음, 소리 컴포넌트 1개
  - 창 Opening·Closing 중, 해금 연출 중에는 무음·흔들림 없음
  - 흔들리는 중 OnDisable → 멈춤·자세 복귀, 다시 켜면 정상 반응
  - 퀘스트 클리어 닫기: BriefingClose 1번(두 번 불러도), GlitchPopupClose·QuestReward 0번
  - 다른 작은 창은 GlitchPopupClose 유지
- PlayMode `Tests/PlayMode/Integration/QuestClearPopupMotionPlayModeTests.cs`에 2개를 추가했다.
  - 열림 직후 0번 → 체크가 커지기 시작할 때 1번(그 시점의 체크 크기 < 0.6) → 표시 중 반복 없음 → 닫힘은 BriefingClose만 → 다음 표시에서 다시 1번
  - 체크 전에 닫으면 0번, 닫는 중 재열기는 1번
- 기대값을 바꾼 기존 검사: `PromptB178SfxTests.Reward_GrantedOnce`, `Reward_AfterDumpRetry_GrantedOnce`는 지급 경로가 무음이 되고 지급이 1번만 되는지를 보도록 고치고 이름도 바꿨다. `Reward_RuleIgnoresEmptyAndNonGranted`는 대상 메서드를 지워 삭제했다.
- 실행
  - 관련 EditMode 묶음(B-180-1·B-178·B-180·B-177·B-169·B-118 업그레이드 트리·B-95 퀘스트 보상): 첫 실행 106 통과 / 1 실패. 실패는 검사 샘플 시점 실수였다(0.05초가 sin=0 지점). 고친 뒤 **107 통과 / 0 실패**. 교훈: `init/rules/lessons/2026-11-02-issue158-oscillation-sample-zero.md`.
  - PlayMode `QuestClearPopupMotionPlayModeTests` (quick 실행기): **4 통과 / 0 실패**.
  - 전체 EditMode 1차(A~C): 2308 통과 / 23 실패. 23개 모두 `work_process/MVP2/B-172-verification/editmode-full.txt`의 기준 실패 목록에 있어 새 실패는 0개다.
  - 95번 칸·심층 자물쇠 반영 뒤 관련 묶음(사운드 칸 개수 검사·B-155 심층 썸네일 포함 12개 fixture): **176 통과 / 0 실패**.
  - **전체 EditMode 2차(최종): 2309 통과 / 24 실패. 새 실패 0개.** 늘어난 1개는 기준 목록에 있는 프레임 시간 의존 검사 `PromptB150MainMenuTests.DeleteButton_Hover_FadesInWhiteTrashIcon`이다(1차에는 통과했다).
  - 칸 개수 기대값을 93→94로 바꾼 기존 검사: `PromptB156SfxTests`, `PromptB165SfxD1FixTests`, `PromptB168SfxD2Tests`, `PromptB178SfxTests`, `PromptB1791SfxFeedbackFixTests`.
  - 테스트 실행이 범위 밖 파일(`NotoSansKR-Regular_SDF.asset`, `InventoryPanel`·`MainMenuPanel`·`ViewSocket` 프리팹, `Mine_Demo_Integration.unity`, `ProjectSettings/TimeManager.asset`, `work_process/MVP2/103-main-menu/*.png` 2개)을 바꿔서 `git restore`로 되돌렸다.

## 미검증
- B·C는 실제 플레이에서 들어 보지 못했다. 쇠 소리가 요구에 맞는지(필요하면 ★ 상수만 바꾸면 된다), 체크 등장과 완료음의 체감 동기, 닫을 때 브리핑 종료음(음량 1.0)과 다음 창 열림음이 겹치는 느낌은 **미검증**이다.
- A는 Editor에서 실제 호버로 정상 동작을 확인했지만 사용자가 처음 보고한 상황은 재현하지 못했다(원인 미확인).

## 사용자 테스트 가이드
1. 메인 메뉴에서 이어하기·새 게임·설정·종료에 마우스를 올린다 → 들어갈 때마다 호버음 1번. 처음 무음을 들었던 상황(빌드인지, 부팅 직후인지)에서 다시 해 보고 안 나면 그 순서를 알려 준다.
2. 지상 기지에서 업그레이드 창을 완전히 연 뒤 잠긴(자물쇠) 항목을 누른다 → 자물쇠만 짧게 흔들리며 쇠가 덜그럭거리는 소리 1번(바람 소리가 아님). 잠금과 "해금 조건" 안내는 그대로다.
2-1. 같은 창 아래쪽 심층 구역 썸네일의 자물쇠 → 윗부분(고리)에 검은 테두리·그림자 없이 단색으로 보인다.
3. 잠긴 항목을 빠르게 여러 번 누른다 → 흔들림이 쌓이지 않고, 끝난 뒤 다시 누르면 다시 반응한다. 창을 닫았다 열어도 자물쇠 위치·크기가 그대로다.
4. 업그레이드 창이 열리거나 닫히는 중에 잠긴 항목을 누른다 → 쇠 소리 없음. 풀린 항목은 예전처럼 클릭음이 난다.
5. 광산에서 퀘스트를 하나 완료한다 → 창이 열리고 **체크 표시가 생길 때** 완료음 1번.
6. 확인 버튼·X 버튼·기존 닫기 키로 각각 닫아 본다 → 시작 브리핑을 닫을 때와 같은 글리치음만 나고 완료음은 다시 나지 않는다. 보상은 1번만 들어온다.
7. 다음 퀘스트 완료에서도 5~6 순서가 같다. 화물이 가득 차 버리기 창을 거쳐 보상을 받을 때는 완료음이 다시 나지 않는다.
8. 효과음 음량·음소거가 위 소리에 모두 적용되고, Console에 새 빨간 오류나 노란 경고가 없다.
