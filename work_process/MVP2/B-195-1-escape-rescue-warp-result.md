# B-195-1 / 195-2 구출 글리치 워프·포탈 웜홀 워프·포탈 창 불투명 결과 (#151)

- 기준: `MVP2-fix139` (HEAD `a3ed95f4`) 위의 **미커밋 작업 트리**. B-195(글리치 워프·검은 Panel 제거)는 같은 작업 트리에 반영돼 있고, 이번에 아래로 대체했다.
- 커밋 해시: **없음 — 사용자 동의 전이라 커밋·push 하지 않음**

## 사용자 피드백과 대응
| 피드백 | 대응 |
|---|---|
| 구출: 켜질 때 캐릭터가 옛 자리에 있다가 화면이 이동하며 갑자기 엘리베이터로 이동 | **카메라 이동을 전부 삭제.** 완전히 검은 화면에서 이동 + 카메라 즉시 정렬 → 켜질 때 이미 엘리베이터 |
| 포탈 창이 반투명 | 프레임 **바깥 실루엣** 안쪽을 불투명하게 채움(`EscapeWindowFillArt`). 프레임 밖 검은 네모 없음 |
| 웜홀 연출: 검은 전체화면 + 빨려 드는 연출로, 더 신경 써서 | 새 연출(아래). 카메라 이동 없음 |

## 연출
- **비상 탈출(전력 고갈 구출)**: 암전 0.25s + 간단 글리치 0.25s(겹침) → **검은 채 0.15s, 이 사이에 이동** → 켜짐 0.30s(켜지는 첫 순간 약한 도착 글리치). 합계 0.70s(사망 전환 1.4s보다 짧음).
- **긴급 탈출 포탈**(합계 1.65s):
  1. 빨림 0.55s: 화면이 점점 검어지고, 제자리에 청록 웜홀이 열리며 캐릭터가 줄고 돌며 웜홀 속으로 빨려 든다(캐릭터 복제 그림이 검은 화면 위에서도 보임).
  2. 암전 0.45s: 완전한 검은 화면에서 웜홀이 커지고 빨아들이는 청록 줄기(110줄)가 중심으로 모이다가 한 점으로 모이며 둥근 빛이 번쩍인다. **번쩍이는 순간 플레이어를 이동**.
  3. 걷힘 0.65s: 검은 화면이 걷히며 이미 도착한 자리에 웜홀이 열리고, 캐릭터가 반대 방향으로 돌며 나온다.
- 창을 닫는 웜홀(기존 `EscapePortalOpenTimeline`)은 그대로. 웜홀 그림은 창과 같은 `EscapeWormholeArt`·입자를 재사용(새 이미지 없음).
- 결제는 두 흐름 모두 성공 직후 한 번(`TrySpend` / `TryRescue`). 이동은 검은 화면에서 한 번. 연출은 수량을 건드리지 않는다.
- 모션 감소: 두 흐름 모두 페이드 아웃 → 검은 화면에서 이동 → 페이드 인(글리치·웜홀·줄기 없음).
- 효과음: 새 음원·`SfxId` 없음. 포탈은 바인더의 기존 `PortalWarp`, 구출은 기존 `RescueWarpSfx(PortalWarp)`. 사망 실패 흐름 미호출.

## 포탈 창 불투명 방식
- 원인: B-195에서 검은 `Window/Panel`을 꺼서 프레임 안쪽 구멍(알파 0)이 비쳤다. 프레임 그림은 모서리가 깎이고 장식이 복잡해 단순 사각형 채움은 구멍/바깥 모서리가 어긋난다.
- 해결: `EscapeWindowFillArt.FromFrame`이 프레임 스프라이트를 절반 해상도(최대 1024)로 줄여 알파를 읽고, **가장자리에서 번지는 빈 칸 = 프레임 바깥**을 구해 그 반대(안쪽 전부)를 어두운 불투명 색(`0.012,0.04,0.062,1`)으로 칠한 스프라이트를 런타임에 만든다. 실제 PNG로 검증: 안쪽 약 98%, 네 모서리 바깥은 투명.
- `View.ApplyArt`가 이 스프라이트를 `Window/Panel`에 입히고 켠다(생성 실패 시 안전한 안쪽 여백 사각형 대체). 빌더는 `Panel`을 프레임과 같은 크기·알파 1·스프라이트 없음·꺼진 상태로 만들고 `windowFill`/`frameImage`를 연결한다.
- `InputShield`(알파 0, `raycastTarget` true), `Frame`, 웜홀, 목록, CCTV, 육각 무늬 유지. `mine-reset-panel.png`·`mine-reset-frame.png` 원본·다른 팝업 미수정.

## 바꾼 파일
| 파일 | 내용 |
|---|---|
| `Scripts/App/Integration/EscapeWarpRunner.cs` (신규) | 두 스타일 공용 진행기. 카메라 미이동, 이동 프레임에서 도착 콜백 1회 + 카메라 즉시 정렬, `Abort` |
| `Scripts/App/Integration/EscapeWarpPlayerProxy.cs` (신규) | 캐릭터 스프라이트를 오버레이용 복제 그림으로 캡처(크기·피벗·회전·좌우반전·색), 월드 렌더러는 연출 중 끄고 복구 |
| `Scripts/App/UI/EmergencyEscape/EscapeWarpTimeline.cs` | 글리치 시간표에서 카메라 이동 제거, 검은 채 머무는 시간 추가, 이동 프레임은 알파 1로 보고 |
| `.../EscapeWormholeWarpTimeline.cs` (신규, 재작성) | 빨림 → 암전 → 걷힘 시간표 |
| `.../EscapeWormholeWarpOverlay.cs` (신규, 재작성) | 검은 전체화면, 줄기, 웜홀(재사용), 캐릭터 복제, 번쩍임(`CoreCctvArt.Soft`) |
| `.../EscapeWormholeSuctionGraphic.cs` (신규) | 이미지 없이 정점 색만 쓰는 빨려 드는 줄기 |
| `.../EscapeWindowFillArt.cs` (신규) | 프레임 실루엣 안쪽 불투명 채움 |
| `.../EmergencyEscapePanelView.cs` | `windowFill`/`frameImage` 필드, `ApplyWindowFill` |
| `Editor/DataValidation/PromptB147EscapePortalTerminalBuilder.cs` | `Panel` 채움 구성·연결 |
| `Prefabs/UI/EmergencyEscapePanel.prefab` | 빌더 재실행 결과(빌더가 자식을 전부 다시 만들어 diff가 큼) |
| `EmergencyEscapePortalRuntimeBridge.cs` / `EmergencyRescueRuntimeController.cs` | 새 진행기 시그니처(카메라 이동·목적지 인자 제거) |
| 테스트 | `PromptB195EscapeWarpTests`(재작성), `PromptB1951EscapeWormholeWarpTests`(재작성), `PromptB181...`, `EmergencyRescuePlayModeTests`, `EmergencyEscapePortalPlayModeTests` |
| lessons | 2026-11-13 중첩 IEnumerator, 2026-11-14 이동 프레임 완전 검정 |

- 이전 B-195에서 `PlayerCameraFollow`에 넣었던 카메라 연출 API(`BeginCinematic` 등)와 `PlayerFacing.VisualRoot`는 **쓰지 않게 되어 제거**했다. Gameplay 어셈블리 파일은 HEAD와 같다.

## 테스트
- 컴파일 오류 0.
- EditMode(관련 fixture 한정) **113 Pass / 0 Fail**: `PromptB1951EscapeWormholeWarpTests`, `PromptB195EscapeWarpTests`(실제 프레임 PNG 실루엣 검사·빌더 재적용·`ApplyArt` 포함), `PromptB181SfxIngameFillTests`, `PromptB85EmergencyRescueChipTests`, `PromptB147EscapePortalTests`, `PromptB135EmergencyRescueUiTests`.
- PlayMode `EmergencyRescuePlayModeTests` **10/10**, `EmergencyEscapePortalPlayModeTests` **7/7**.
- **실제 화면 확인(임시 PlayMode 캡처, 테스트 파일은 삭제)**: 밝은 주황 배경 위에서 ① 포탈 창이 불투명하고 프레임 밖 검은 네모 없음 ② 웜홀: 제자리 웜홀과 줄어드는 캐릭터 → 완전 암전의 큰 웜홀과 수렴하는 줄기 → 둥근 빛 → 엘리베이터에 웜홀이 열리며 도착 ③ 구출: 글리치 → 켜졌을 때 이미 엘리베이터이고 카메라 스윕 없음.
- 전체 EditMode/PlayMode 일괄 실행은 하지 않았다 — **미검증**. 테스트가 바꾼 `ProjectSettings/TimeManager.asset`은 `git restore`로 되돌렸다. `EditorBuildSettings.asset` 변경은 이번 작업 이전부터 있던 것.

## 미검증
- 실제 게임 씬(진짜 플레이어 프리팹·실제 광산·조명)에서의 모습: 복제 그림이 월드 조명을 받지 않아 어두운 광산에서 캐릭터가 살짝 밝게 보일 수 있다. 전진기지 목적지, 모션 감소 실플레이, Console 새 경고.
- 연출 길이·세기(체감)는 검사기(`EscapeWarpSettings`)에서 조절한다.

## 사용자 테스트 가이드
1. 긴급 탈출 포탈을 연다: 창 안쪽이 불투명하고 광산이 비치지 않으며 프레임 밖 모서리에 검은 네모가 없다. 창 밖 클릭이 채굴·이동으로 새지 않는다.
2. 엘리베이터로 이동: 캐릭터 자리에 웜홀이 열리고 화면이 어두워지며 캐릭터가 빨려 든다 → 검은 화면에서 웜홀이 커지며 줄기가 모이고 번쩍 → 걷히며 엘리베이터에서 웜홀이 열리고 캐릭터가 나온다. 카메라가 스윕하면 안 된다.
3. 전진기지가 있으면 2를 반복한다.
4. 전력 0 → 「구출 요청」: 팝업이 사라지고 글리치와 함께 꺼짐 → 켜졌을 때 이미 엘리베이터에 서 있다.
5. 골드·자원 부족, 취소, Esc/×: 안내만 있고 워프가 없다.
6. 모션 감소를 켜고 2와 4: 웜홀·글리치 없이 페이드 후 도착.
7. 연출 중 골드·전력·광물이 더 줄지 않는지, Console 새 오류·경고가 없는지, 체력 0 구조 연출이 예전과 같은지 본다.

## 완료 체크리스트
[ ] push (보류) [ ] 커밋 해시 (보류) [x] 컴파일 오류 없음 [x] 관련 EditMode·PlayMode 새 실패 없음 [x] 포탈 창 프레임 안 불투명 [x] 프레임 밖 검은 네모 없음 [x] InputShield 유지 [x] 포탈 = 검은 전체화면 + 웜홀 빨림 [x] 포탈에 글리치 없음 [x] 구출 = 글리치 후 켜질 때 도착(카메라 이동 없음) [x] 결제 즉시 한 번·이동은 암전 중 한 번 [x] 모션 감소 페이드+스냅 [x] 새 SFX 없음 [x] 사망 실패 흐름 미호출 [x] 공유 스프라이트·다른 팝업 미수정 [x] main 직접 커밋 없음 [x] PR 안 만듦 [x] #151 미수정 [ ] 실제 게임 씬 플레이 확인(미검증)
