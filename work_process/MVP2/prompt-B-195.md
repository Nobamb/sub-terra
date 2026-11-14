# B-195 긴급 탈출 워프: 검은 화면·간단 글리치 후 시점 이동 (#151)

## 목표
긴급 탈출로 빠져나올 때, 체력 0 구조처럼 화면이 검어지고 간단한 글리치가 난 뒤 다시 켜지게 한다. 화면이 켜진 다음 카메라가 목적지로 움직이고, 도착과 함께 플레이어가 그곳으로 워프한다. 창의 열기·닫기 연출은 이미 있으므로 건드리지 않는다. 창 뒤에 깔린 검은 네모 배경은 없앤다.
기준: https://github.com/Nobamb/sub-terra/issues/151 의 긴급 탈출 체크 항목 중 **워프 연출**과, 창 뒤 검은 네모 제거. 비용·목적지 목록·CCTV 미리보기·실패 안내는 유지한다.

## 브랜치
1. `git fetch` 후 최신 `origin/main`(`6c9743a506e61166e15efe93f0ae5a7977c6f4ac`, #323 병합. 그 안에 #321/B-194, #322, #320, #319 포함)에서 새 브랜치 **`MVP2-fix139`**을 만든다.
2. 원격에 `MVP2-fix139`이 이미 있으면 비어 있는 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다.
3. 보고서 맨 위에 기준 커밋 해시를 적는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix139`에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다. #151에 댓글·체크박스 수정·닫기를 하지 않는다.
3. 새 음원, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다. 쓸 수 있는 기존 음만 아래에 적는다.
4. 긴급 탈출 창의 열기·닫기 연출(`EscapePortalOpenTimeline`, 웜홀 아트)을 바꾸지 않는다. 취소·Esc·× 닫기도 지금과 같게 둔다. 다른 팝업이 같이 쓰는 `mine-reset-panel.png`·`mine-reset-frame.png` 원본과, 전진기지 코어 등 다른 창의 `Panel`/`PanelBackdrop`는 건드리지 않는다.
5. 체력 0 실패 팝업·화물 손실·지상 씬 로드(`RunFailureRuntimeController.ResolveFailure`)를 호출하거나 섞지 않는다. 그림·시간표만 재사용한다.
6. 시각 연출이 골드·전력 차감 시점이나 수량을 다시 바꾸면 안 된다. 성공 결제(`EmergencyEscapeService.TrySpend`)는 지금처럼 확인 직후 한 번만.
7. 줄 번호는 `6c9743a5` 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 현재 상태 (열기·닫기 연출은 유지)
- 창 열기: `EmergencyEscapePanelBinder.Open` ~155가 `SfxId.PortalActivate`와 함께 `SetVisible(true)`. `EscapePortalOpenTimeline`이 웜홀 가속(0.6s) → 창이 튀어나옴(0.6~0.85s) → 접속 줄, `OpenDuration` ≈ 1.15s. 닫기: 웜홀이 창을 빨아들임, `CloseDuration = 0.5s`. `BeginClose` ~376. 스킵은 `SkipOpen`. 이 연출은 이미 있고, **열기·닫기 모두 수정하지 않는다.** (생존자 브리핑의 `EmergencyRescueTimeline`/`GlitchPopupChrome`과는 다른 웜홀 연출이다.)
- 창 뒤 검은 네모: `EmergencyEscapePanel.prefab`의 `Window/Panel` Image다. `PromptB147EscapePortalTerminalBuilder.ApplyTo` ~129가 16px 안쪽으로 늘인 뒤 `mine-reset-panel.png`(거의 검은 네모, RGB ≈ 0,11,18)를 넣는다. 그 위에 테두리만 있는 `Window/Frame`(`mine-reset-frame.png`, 가운데는 투명)이 올라가서, 프레임 밖으로 검은 도형이 비친다. 전체 화면 암전 레이어는 이 프리팹에 없다. 같은 자리의 `Window/InputShield`는 이미 `Color(0,0,0,0)`에 `raycastTarget = true`인 **투명 클릭 차단막**이다(`ApplyTo` ~127). 보이면 안 되고, 지우면 창 밖·빈 곳으로 클릭이 새어 나간다.
- 지금 워프: `EmergencyEscapePortalRuntimeBridge.TryEscapeTo` ~199가 목적지 확인 → `TrySpend` → **즉시** `TeleportPlayer` ~412 → `panelBinder.Close()` → `SuppressEnterUntilReleased` → 저장. 바인더 `ConfirmSelectedDestination` ~254도 성공 시 `SfxId.PortalWarp` 후 `Close()`를 한 번 더 부른다. 카메라는 따로 움직이지 않고, `PlayerCameraFollow`는 대상이 `teleportDistance`(기본 3) 이상 뛰면 `SnapToTarget`으로 즉시 붙는다. 그래서 창이 닫히는 동안 이미 목적지에 가 있다.
- 체력 0 워프(참고·재사용): `RunFailureRuntimeController.ResolveFailure` ~257이 확인 후 `RunFailurePanelView.PlayTransitionRoutine`을 돌린다. `RunFailureTransitionTimeline`은 암전(`darkenDuration`, 주석 기본 0.4s) → 강한 전체 글리치 0.50~0.70s(`ShouldMovePlayer`는 암전 끝, 이동은 화면이 가려진 채 `ApplyPlayerWorldPosition` + `SnapToTarget`) → TV 켜짐(`tvTurnOnDuration`, 주석 기본 0.4s). 그림은 `RunFailureGlitchArt`. 시간은 unscaled.

## 할 일
- 성공 확인 시 결제는 즉시 하되, `TeleportPlayer`는 연출이 끝날 때까지 미룬다. 이중 `Close()`는 한 경로로 정리해도 되지만, 취소 닫기 동작은 유지한다.
- 전환 순서(unscaled, 일시정지는 워프가 끝날 때까지 `UiPauseGate`/`PauseOwner`로 유지):
  1. 검은 화면 + **간단한** 글리치. 사망의 강한 글리치(0.6s 전면)보다 짧고 약하게. `RunFailureGlitchArt`와 시간표 계산을 재사용·짧게 파라미터화한다. 색은 사망용 적색/녹색이 아니라 약한 청록 계열로 구분해, 죽은 것처럼 보이지 않게 한다.
  2. 화면이 다시 켜진다. 사망의 긴 CRT 조리개보다 단순한 켜짐/페이드여도 된다.
  3. 카메라가 선택한 목적지(엘리베이터 `elevatorCenter`/`BoardingAnchor`, 또는 전진기지 착지 좌표)로 **이동**한다. `SnapToTarget`이나 `teleportDistance` 자동 스냅으로 점프하면 안 된다. 연출 동안 자동 스냅을 끄거나 우회하는 API를 `PlayerCameraFollow`에 보탠다. CCTV용 `PreviewWorldPosition`은 암전 전에 해제하되, 화면이 꺼지기 전에 광산으로 튀지 않게 한다. 기본 `smoothTime`(0.18s)만으로 멀리 날리면 너무 빠르니, 검사기 시간으로 보간한다.
  4. 카메라가 도착하는 시점에 `TeleportPlayer`하고 추적을 재개한다. `PlayerMiningController.SuppressEnterUntilReleased`, 데모 진행, `SaveCurrent`는 워프 이후로 옮긴다.
- 시간은 모두 검사기에서 조절. 권장 기본: 암전 0.25s, 간단 글리치 0.25s(암전과 겹쳐도 됨), 화면 켜짐 0.3s, 카메라 이동 0.7s. 사망 기본(0.4+0.6+0.4)보다 짧아야 한다.
- 모션 감소(`AccessibilityPreferences.ReduceMotion`): 큰 화면 전환이므로 글리치와 카메라 이동을 생략하고, 페이드로 암전 → 워프+`SnapToTarget` → 페이드 인만 한다.
- 효과음: 성공 시 기존 `SfxId.PortalWarp`만 유지한다. `PortalClose`는 성공 워프에서 내지 않는다(이미 `busy`일 때 생략). `EscapeArrive`는 내지 않는다(B-165). `RunFailurePanelView.PlayTransitionRoutine`/`CrossedTvTurnOn`에 이미 있는 켜짐음이 있으면 화면 켜짐에 재사용해도 되지만, 실패 전용 스팅어는 쓰지 않는다. 없으면 화면은 무음으로 켠다.
- 실패(골드·전력 부족, 목적지 없음)는 연출 없이 지금처럼 안내만. 창을 닫지 않고 취소한 경우도 워프 연출 없음.
- 창 뒤 검은 네모(`Window/Panel`)를 보이지 않게 한다. 오브젝트를 끄거나 Image 알파를 0으로 두거나, 빌더에서 보이는 `Panel`을 넣지 않게 고친다. `PromptB147` 빌더를 다시 돌리면 검은 네모가 되살아나면 안 된다. `Frame`, 웜홀, 목록·CCTV·버튼·육각 무늬는 유지한다. **`InputShield`는 지우지 말고**, 알파 0 + `raycastTarget` true를 유지해 클릭 차단만 맡긴다. 공유 스프라이트와 다른 팝업(코어 CCTV 등)은 그대로 둔다.

## 테스트 (EditMode 추가, 필요하면 PlayMode)
- 성공 시 결제는 즉시, 좌표 변경은 암전·켜짐 뒤 카메라 이동이 끝날 때 1회만. 골드·전력은 연출 중에 다시 줄지 않음.
- 기본 경로: 암전+간단 글리치 → 화면 켜짐 → 카메라가 스냅이 아니라 이동 → 도착 시 워프. 타임라인 길이는 검사기 기본값.
- 모션 감소: 글리치·카메라 이동 없이 페이드→스냅 워프→페이드 인.
- 취소·Esc·×는 웜홀 닫기만, 플레이어·카메라 위치 불변. 실패 안내는 연출 없음.
- 창을 연 상태에서 `Window/Panel`이 보이지 않고(`enabled` 꺼짐 또는 알파 0), `InputShield`는 알파 0·레이캐스트 켜짐, `Frame`과 웜홀은 그대로인지 검사한다. 빌더를 다시 적용해도 검은 `Panel`이 되살아나지 않아야 한다.
- 기존 `EmergencyEscapeServiceTests`, `PromptB147EscapePortalTests`, `EmergencyEscapePortalPlayModeTests` 등에서 즉시 워프를 가정한 조건은 새 순서에 맞게 고친다. 체력 0 `RunFailure` 검사는 새 실패 없음.

## 사용자 테스트 가이드 (직접 플레이)
1. 광산에서 긴급 탈출 포탈을 연다. 창이 예전처럼 웜홀과 함께 열리는지 본다. 프레임 뒤·주변에 검은 네모가 없어야 한다. 테두리·목록·CCTV는 그대로다.
2. 창이 열린 채로 창 바깥(광산)을 클릭한다. 캐릭터가 움직이거나 채굴되면 안 된다. Esc나 ×로 닫는다. 창이 예전처럼 웜홀로 빨려 들어가고, 캐릭터는 포탈 앞에 그대로인지 본다.
3. 다시 열어 엘리베이터를 고르고 이동한다. 화면이 검어지며 짧은 글리치가 나고, 다시 켜진 뒤 카메라가 엘리베이터 쪽으로 움직인 다음 그 자리에 와 있는지 본다. 창만 닫히며 갑자기 옮겨지면 안 된다.
4. 전진기지가 있으면 그곳으로도 3을 반복한다.
5. 골드나 전력이 부족할 때는 안내만 뜨고 워프 연출이 없는지 본다.
6. 설정에서 모션 감소를 켜고 3을 다시 본다. 글리치와 카메라 이동 없이 페이드 후 목적지에 나타나야 한다.
7. 체력 0 구조 연출이 예전과 같은지, 인벤토리·골드가 연출 때문에 더 깎이지 않는지, Unity Console에 새 빨간 오류·노란 경고가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 긴급 탈출 워프 연출과 창 뒤 검은 배경 제거 (프롬프트 B-195)`). `MVP2-fix139`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue151-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #151은 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-195-emergency-escape-warp-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일·줄
  - 재사용한 사망 전환 클래스, 검사기 시간, 모션 감소 처리, 텔레포트 시점을 옮긴 위치, `Window/Panel`을 어떻게 숨겼는지, `InputShield`를 유지한 방식
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix139` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음(탈출·사망 전환 검사 포함) [ ] 성공 시 결제 즉시·워프는 연출 후 [ ] 암전+간단 글리치 후 화면 켜짐 [ ] 카메라가 목적지로 이동한 뒤 워프 [ ] 자동 스냅으로 점프하지 않음 [ ] 전진기지도 동일 [ ] 취소·실패는 워프 연출 없음 [ ] 창 열기·닫기 웜홀 유지 [ ] 창 뒤 검은 Panel 네모 없음 [ ] InputShield는 투명 클릭 차단으로 유지 [ ] 공유 스프라이트·다른 팝업 미수정 [ ] 빌더 재실행 후에도 검은 네모 없음 [ ] 모션 감소 시 페이드+스냅만 [ ] 기존 PortalWarp만, 새 SFX 없음 [ ] 사망 실패 흐름 미호출 [ ] 수량 불변 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] #151 미수정
