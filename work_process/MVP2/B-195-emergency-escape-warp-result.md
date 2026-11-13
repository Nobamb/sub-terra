# B-195 긴급 탈출 워프 연출·창 뒤 검은 네모 제거 결과 (#151)

> **대체됨:** 이 문서의 포탈 연출(글리치 + 카메라 이동)은 B-195-1/195-2에서 포탈=웜홀, 구출=글리치(카메라 이동 없음)로 바뀌었다. 최신 내용은 `B-195-1-escape-rescue-warp-result.md`를 본다.

- 기준: `main` = `origin/main` `6c9743a506e61166e15efe93f0ae5a7977c6f4ac` (#323 병합)
- 만든 브랜치: `MVP2-fix139` (로컬 생성. 원격에 같은 이름 없음 확인)
- 커밋 해시: **없음 — 사용자 동의 전이라 커밋·push 하지 않음**

## 바꾼 파일
| 파일 | 내용 |
|---|---|
| `Scripts/App/UI/EmergencyEscape/EscapeWarpTimeline.cs` (신규) | 연출 시간표(순수 C#)와 검사기 설정 `EscapeWarpSettings` |
| `Scripts/App/UI/EmergencyEscape/EscapeWarpOverlay.cs` (신규) | 전체 화면 암전·간단 글리치 레이어(런타임 생성, 끝나면 삭제) |
| `Scripts/App/Integration/EmergencyEscapePortalRuntimeBridge.cs` | 결제는 즉시, 이동은 연출 뒤. `BeginWarp`/`WarpRoutine`/`CompleteArrival` |
| `Scripts/App/UI/EmergencyEscape/EmergencyEscapePanelBinder.cs` | `IsPanelHidden` 추가(창이 완전히 숨겨졌는지) |
| `Scripts/Gameplay/Player/PlayerCameraFollow.cs` | `BeginCinematic`/`SetCinematicCameraXY`/`EndCinematic`/`GetFollowCameraXY`. 연출 중 `LateUpdate`(자동 스냅 포함) 건너뜀 |
| `Editor/DataValidation/PromptB147EscapePortalTerminalBuilder.cs` | `Window/Panel` Image를 스프라이트·색 없이 `enabled=false`로 생성 |
| `Prefabs/UI/EmergencyEscapePanel.prefab` | 빌더 재실행 결과(빌더가 자식을 전부 다시 만들어 fileID가 바뀌어 diff가 큼) |
| `Tests/EditMode/App/UI/PromptB195EscapeWarpTests.cs` (신규) | 시간표·카메라 우회·브리지·프리팹 검사 11건 |
| `Tests/PlayMode/.../EmergencyEscapePortalPlayModeTests.cs` | 즉시 워프 가정을 "결제 즉시, 이동은 `IsWarping` 종료 뒤"로 수정 |

## 동작
1. 확인 → `TrySpend`(한 번) → 창 닫기(웜홀 빨려듦, 기존 그대로) → 창이 완전히 숨겨진 뒤 연출 시작.
2. 암전 0.25s + 간단 글리치 0.25s(겹침, 최대 강도 0.55, 청록 계열) → 화면 켜짐 0.3s → 카메라 이동 0.7s(이징) → 도착 시 `TeleportPlayer` 1회 → `SnapToTarget`(이미 도착한 자리의 속도 정리) → 추적 재개.
3. 합계 1.25s로 사망 전환 기본(0.4+0.6+0.4=1.4s)보다 짧다. 모든 시간은 `EmergencyEscapePortalRuntimeBridge`의 `warpSettings`(검사기)에서 조절.
4. 일시정지: 브리지가 `WarpPauseOwner`로 따로 `UiPauseGate`를 잡아 창이 닫혀도 워프가 끝날 때까지 월드가 멈춘다. 모든 시간은 unscaled.
5. 이동 이후로 옮긴 것: `DemoObjectiveDirector.AdvancePersistedState`, `SuppressEnterUntilReleased`, `SaveCurrent` → `CompleteArrival`.
6. 모션 감소: 글리치·카메라 이동 없이 페이드 아웃 → 검은 화면에서 이동+`SnapToTarget` → 페이드 인(총 0.55s).
7. CCTV 미리보기(`PreviewWorldPosition`)는 화면이 완전히 검어진 뒤에 해제.
8. 효과음: 새 음원·`SfxId` 없음. 바인더의 기존 `PortalWarp`만 유지. 화면 켜짐은 무음.
9. 실패(골드·전력 부족, 목적지 없음)는 `TrySpend` 앞에서 끝나 연출이 시작되지 않는다. 연출 중 재진입은 "긴급 이동 중입니다."로 막는다.
10. 에디트 모드(`Application.isPlaying == false`)에서는 연출 없이 즉시 이동해 기존 EditMode 검사 조건을 유지.
11. 연출 도중 브리지가 비활성화되면 `OnDisable`에서 도착 처리와 정지 해제를 마쳐 낸 비용만 잃지 않게 한다.

## 재사용
- 사망 전환 클래스를 그대로 호출하지 않고(체력 0 흐름 미호출) `RunFailureGlitchArt`의 스캔라인·할로·글리치 블록 그림, `RunFailureRevealTimeline.Hash01/StepSeconds`, `ResourceSellTimeline` 이징만 재사용했다.
- 오버레이를 프리팹·씬에 넣지 않고 런타임에 만들어 `Mine_Demo_Integration.unity`를 건드리지 않았다(`warpSettings`는 필드 초기값으로 기존 씬 인스턴스에도 적용됨).

## 창 뒤 검은 네모
- `Window/Panel` 오브젝트는 남기되 `Image`를 스프라이트 없이 알파 0 + `enabled=false`로 생성. 빌더를 다시 돌려도 되살아나지 않는다(`Builder_ReapplyDoesNotRevive_TheBlackPanel`).
- `InputShield`는 그대로: 알파 0, `raycastTarget=true`. `Frame`·웜홀·`HexPattern`·`ListPanel`도 그대로. `mine-reset-panel.png` 원본과 다른 팝업은 미수정.

## 테스트
- EditMode 전체: Pass 2666 / Fail 26. 실패 26건은 `PromptB125Terminal`, `IntegrationWiring`, HUD/건설 메뉴/업그레이드 창 레이아웃 등 이번 변경과 무관한 검사(긴급 탈출·워프·카메라 관련 없음). **기준선(변경 전) 실행과의 비교는 하지 않음 — 미검증.** 새 `PromptB195EscapeWarpTests`는 실패 목록에 없음.
- PlayMode `EmergencyEscapePortalPlayModeTests` 7/7 통과(결제 즉시·이동은 연출 뒤·수량 불변·퀘스트 진행은 이동 뒤).
- 전체 PlayMode 일괄 실행은 "unexpected error"로 끝나 못 봄(이번 변경 때문인지 미확인 — **미검증**).
- 전체 EditMode 실행이 범위 밖 에셋(프리팹 3종·씬·폰트·ProjectSettings·work_process 이미지)을 dirty 시켜 `git restore`로 되돌렸다. `work_process/images/b169-unlock-evidence/`, `issue249/`, `work_process/tools/issue225-pixel-comparison/`는 테스트가 만든 untracked 산출물이라 커밋에서 제외 필요.

## 직접 확인하지 못한 것 (미검증)
- 실제 Play에서의 화면 모습(글리치 세기, 켜짐 느낌, 카메라 이동 속도), 전진기지 목적지, 모션 감소 설정 실플레이, Console 새 경고 여부.
- 창 뒤 검은 네모가 사라진 모습의 스크린샷.

## 사용자 테스트 가이드
1. 광산에서 긴급 탈출 포탈을 연다. 창이 웜홀과 함께 열리고, 프레임 뒤·주변에 검은 네모가 없는지 본다.
2. 창이 열린 채 바깥(광산)을 클릭해도 캐릭터가 움직이거나 채굴되지 않는지, Esc/×로 닫으면 웜홀로 빨려 들어가고 캐릭터가 포탈 앞에 그대로인지 본다.
3. 다시 열어 엘리베이터로 이동: 창이 닫힌 뒤 화면이 검어지며 짧은 청록 글리치 → 켜짐 → 카메라가 엘리베이터 쪽으로 이동 → 그 자리에 도착. 창만 닫히며 갑자기 옮겨지면 안 된다.
4. 전진기지가 있으면 3을 반복한다.
5. 골드·전력이 부족하면 안내만 뜨고 연출이 없는지 본다.
6. 설정에서 모션 감소를 켜고 3을 다시 본다(글리치·카메라 이동 없이 페이드 후 목적지).
7. 체력 0 구조 연출이 예전과 같은지, 골드·전력이 연출 중 더 깎이지 않는지, Console에 새 오류·경고가 없는지 본다.

## 완료 체크리스트
[ ] `MVP2-fix139` push (보류) [ ] 커밋 해시 (보류) [x] Unity 컴파일 오류 없음 [△] EditMode 새 실패 없음(기준선 미비교) [x] 결제 즉시·워프는 연출 후 [x] 카메라 이동 후 워프(자동 스냅 우회) [x] 취소·실패는 연출 없음 [x] 창 열기·닫기 웜홀 유지 [x] 검은 Panel 제거·빌더 재실행 후에도 [x] InputShield 유지 [x] 새 SFX 없음 [x] 사망 실패 흐름 미호출 [x] main 직접 커밋 없음 [x] PR 안 만듦 [x] #151 미수정 [ ] 실플레이 확인(미검증)
