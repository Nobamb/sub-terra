# B-156 효과음 기반 만들기 + A 화면 효과음 (#158 1단계: "0. 먼저 할 것" + A)

## 목표
- 게임에 효과음 채널을 처음 만든다. 이후 B·C·D·E 단계는 여기서 만든 API를 그대로 쓴다.
- 공통 UI 소리(클릭·호버·뒤로·확인·취소/불가)와 #158 A 화면의 열기·닫기·확인·실패 소리를 연결한다.
- 위험도 경고음: 코드로 만든 440Hz "삑"을 없애고, 광산 경보음 같은 짧은 "웽-웽" 클립으로 바꾼다.
- 클립은 사용자(PM)가 Pixabay에서 직접 받아 넣는다. **이번에는 클립을 만들지 않는다.** 파일이 없는 칸은 무음이고, 오류가 나지 않아야 한다.
- 기준: 최신 main(#229·#230·#232·#234 포함)에서 새 브랜치를 딴다.

## 절대 규칙 (먼저 읽기)
1. BGM은 건드리지 않는다. 대상: ExplorationBgmController, StructuralRiskFeedback.UpdateDangerBgm, GameBgmChannel, bgm1~4
2. 마스터 음량과 음소거는 기존 방식(AudioListener.volume, SettingsRuntimeApplier)을 따른다. 새 PlayerPrefs 키는 만들지 않는다.
3. 클립 경로와 이름은 아래 표와 똑같이 쓴다. 파일이나 칸이 비면 조용히 건너뛰고, 경고 로그는 칸마다 한 번만 남긴다.
4. 연타해도 소리가 쌓이지 않게 한다. 같은 SfxId는 최소 0.05초 간격이고, 동시 재생은 최대 8개다.
5. 시간은 비스케일이다. UiPauseGate로 게임이 멈춰도 UI 소리와 경보 반복이 나야 한다.
6. 소리는 **연출 타임라인에서 창이 실제로 펼쳐지거나 접히기 시작하는 순간**에 건다.
7. 게임 로직, 입력 순서, 연출 시간, 기존 테스트 기대값은 바꾸지 않는다.

## 먼저 읽을 코드 (sub-terra/Assets/_Project/ 기준)
- 음량과 음소거: Scripts/App/UI/MainMenu/SettingsRuntimeApplier.cs (IsMasterMuted, MasterMuteChanged), SettingsMuteButton.cs (#234)
- 경고음: Scripts/Gameplay/Structural/StructuralRiskFeedback.cs
  - OnRiskChanged → PlayOneShot(ResolveWarningTone())
  - ResolveWarningTone(AudioClip.Create 0.1초), GetPitch 0.85/1.05/1.3
  - 관련: Editor/PhaseGStructuralSceneSetup.cs ("audioSource" 연결), Tests/EditMode/Gameplay/Structural/StructuralFeedbackTests.cs, Tests/EditMode/App/PromptB88ExplorationBgmTests.cs
- 어셈블리
  - Scripts/Shared/SubTerra.Shared.asmdef는 **noEngineReferences: true**다.
  - Gameplay.Player와 Gameplay.Structural은 App을 참조하지 않는다 (Shared만 참조).
- 실행 루트: App/Core/GameBootstrapper.cs (단일 DontDestroyOnLoad). 붙이는 방식은 DeveloperDebugTerminal.AttachToRuntimeRoot를 따른다.
- 기존 폴더 Audio/BGM, 설정 에셋 예시 Resources/DeepZoneDisplaySettings.asset과 그 빌더
- 출처 기록 위치: 저장소 루트 `init/` (Unity 프로젝트 밖, PRD.md 옆)

## 1. 구조 (새 파일)
- **Shared** (Scripts/Shared/Audio/, 엔진 참조 없음)
  - `SfxId` enum: 번호를 고정하고, 뒤 단계는 끝에만 추가한다.
  - `SfxRequests` static: `Play(SfxId, float pitch = 1f)`, `PlayAlarm(SfxId, int repeat, float interval, float pitch)`, 그리고 event. 듣는 쪽이 없어도 예외가 없다. Gameplay 어셈블리는 이것만 부른다.
- **App** (Scripts/App/Audio/ 새 폴더)
  - `SfxLibrary` ScriptableObject (Resources/SfxLibrary.asset): 칸마다 SfxId, AudioClip, 음량, 버스(UI/World/Alert)가 있고, 버스 음량 3개(기본 1)를 둔다.
  - `SfxPlayer`
    - [RuntimeInitializeOnLoadMethod]로 자동 생성해 GameBootstrapper 아래에 붙인다. 없으면 혼자 DontDestroyOnLoad다.
    - 공개 API `SfxPlayer.Play(SfxId)`, SfxRequests 구독
    - AudioSource 풀: UI·World 8개(priority 128) + Alert 전용 1개(priority 0). 모두 2D, playOnAwake 끔
    - 풀이 차면 가장 오래된 UI·World 소스를 끊는다. Alert는 끊지 않는다.
    - IsMasterMuted면 재생하지 않는다. 그 밖의 음량은 AudioListener.volume이 처리하니 따로 곱하지 않는다.
  - `UiSfx.Attach(Button, SfxId click = UiClick, bool hover = false)`: 코드로 만든 버튼용이고 중복 등록을 막는다. 프리팹용으로 `UiSfxButton` 컴포넌트도 둔다. 비활성 버튼을 누르면 UiCancelFail이다.
- AudioMixer는 만들지 않는다 (에디터 API로 생성이 어렵다). 버스는 코드 음량으로 대신한다. 설정창 효과음 슬라이더도 **이번엔 넣지 않는다** (사용자 선택 대기).

## 2. 클립 칸 (폴더 Audio/SFX/UI · World · Alert, .meta 포함 생성)
| SfxId | 파일 이름 (확장자 제외) | 버스 | 쓰는 곳 |
|---|---|---|---|
| UiClick | ui_click | UI | 공통 버튼 클릭 |
| UiHover | ui_hover | UI | 버튼 호버 (선택, 기본 끔) |
| UiBack | ui_back | UI | 뒤로가기·X·ESC 닫기 |
| UiConfirm | ui_confirm | UI | 확인·성공 (긍정) |
| UiCancelFail | ui_cancel_fail | UI | 취소·불가·실패 (부정) |
| UiPopupOpen / UiPopupClose | ui_popup_open / ui_popup_close | UI | A 팝업 열림 / 닫힘 |
| UiScanTick | ui_scan_tick | UI | 시설 라벨 스캔선 틱 (선택, 작게) |
| UiCctvOn | ui_cctv_on | UI | 코어 CCTV 켜짐 |
| UiMineResetWhoosh | ui_mine_reset_whoosh | UI | 광산 초기화 실행 |
| WorldHoloOpen / WorldHoloClose | world_holo_open / world_holo_close | World | 엘리베이터 홀로그램 펼침 / 수축 |
| WorldFacilityTagBeep | world_facility_tag_beep | World | 시설 근접 라벨 등장 (선택) |
| AlarmMineWail | alarm_mine_wail | Alert | 위험도 상승 "웽" 한 번분 |
| AlertRiskTick | alert_risk_tick | Alert | 위험도 하강 틱 |
- 빌더 `Editor/DataValidation/PromptB156SfxLibraryBuilder.cs`
  - 에셋이 없으면 만들고, **빈 클립 칸만** 같은 이름의 .wav/.ogg/.mp3로 채운다. 음량은 덮어쓰지 않는다.
  - Audio/SFX에 파일이 들어오면 AssetPostprocessor로 다시 채운다.
- Import (Audio/SFX 아래 자동): Force To Mono, Decompress On Load, Vorbis 품질 70, Preload 켬

## 3. 출처 기록 `init/sound-effect-address.md`
- 맨 위 규칙: 파일을 넣을 때마다 한 줄을 채운다. Pixabay도 주소를 남긴다. 덮어쓰면 그 줄을 고친다.
- 표 칸: `파일 이름 | 게임 안에서 쓰는 곳 | 받은 페이지 주소 | 받은 날짜 | 비고`
- 위 15개 파일을 미리 넣고, 주소와 날짜는 비운다.
- alarm_mine_wail 비고: "웽 한 번(약 0.4~0.6초). 단계별 반복·높이는 코드가 조절"

## 4. 위험도 경고음 (StructuralRiskFeedback)
- 지울 것: ResolveWarningTone, runtimeWarningTone, AudioClip.Create, OnDestroy 정리, `warningTone` 필드
- `audioSource` 필드는 PhaseG 셋업이 연결하니 남긴다. 다만 재생에는 쓰지 않는다.
- 이전 단계와 비교해서 판단한다.
  - **오를 때만** `SfxRequests.PlayAlarm(AlarmMineWail, repeat, interval, GetPitch(risk))`
  - 내릴 때(Stable 포함)는 `Play(AlertRiskTick)` 한 번, 같은 단계면 무음
- 순수 static `ResolveAlarmRepeat`/`ResolveAlarmInterval`: Caution 1회, Danger 2회 0.34초, CollapseImminent 3회 0.24초. GetPitch 값은 그대로 둔다.
- 새 PlayAlarm이 오면 진행 중인 반복을 끊고 새로 시작한다. 루프는 쓰지 않는다.
- 위험 배경음 위에서도 들리게 한다: Alert 음량 1, priority 0. BGM 음량은 줄이지 않는다. 카메라 흔들림과 UpdateDangerBgm은 그대로 둔다.

## 5. A 화면 연결 (문맥에 맞는 소리 하나만. 클릭음과 결과음이 겹치면 결과음만)
- **메인 화면** (MainMenuView 이벤트 → MainMenuBinder)
  - SlotSelected → UiClick, NewGame·Continue → UiConfirm
  - OnOverwriteConfirm → UiConfirm, OnOverwriteCancel → UiBack, 빈 슬롯 "새 탐사 시작"(OnStartNewGame) → UiConfirm
  - 부팅·슬롯 자석·삭제 소리는 E 몫이다.
- **광산 초기화 전 경고** (SurfaceBaseView.SetMineResetConfirmVisible/HideMineResetConfirm, SurfaceBaseBinder)
  - 열림 UiPopupOpen, 닫힘 UiPopupClose
  - OnResetMineConfirmed 성공 UiConfirm, 실패·골드 부족 UiCancelFail, OnResetMineCancelled UiBack
  - MineResetPopupMotion.PlayOpen/PlayClose를 **부르는 쪽**에 건다. Motion 안에는 넣지 않는다 (공용 연출이다).
- **광산 초기화 팝업** (MineResetClockOverlay.ShowTimedResetPopup → OpenPopupCard, HidePopup)
  - 카드 열림 UiPopupOpen (clockIntro가 있으면 그 콜백 시점), 확인 UiConfirm
  - 실제 초기화 실행(TryResetMine 성공) 때 UiMineResetWhoosh 한 번
- **엘리베이터 홀로그램** (Gameplay/Player/ElevatorHologramPrompt.cs, SfxRequests 사용)
  - BeginShow → WorldHoloOpen, BeginHide → WorldHoloClose, HideImmediately는 무음
- **구조 위험도 HUD**: 4번에서만 처리한다. StructuralHudView에는 넣지 않는다.
- **체력·전력 회복 팝업** (Outpost/FacilityServicePopupView)
  - Show/Hide → UiPopupOpen/Close (HideImmediate는 무음)
  - SetResult isError false → UiConfirm, true → UiCancelFail
- **전진기지 코어 연결 팝업** (CoreCctvPopupView, CoreCctvTimeline)
  - LineEnd 시점에 UiCctvOn
  - DecideTime에 연결 시설이 있으면 UiConfirm, 없으면 UiCancelFail
  - 각각 Show 한 번에 한 번이다.
- **시설 이름·근접 라벨** (FacilityNameTagVisual.SetWanted)
  - true가 될 때 WorldFacilityTagBeep, false는 무음
  - 스캔선은 FacilityNameTagTimeline 시점에 UiScanTick
  - 둘 다 기본 음량 0.3이다.
- **전력 바닥 팝업 + 다시 띄우기** (EmergencyRescuePopupView.Show/BeginClose, EmergencyRescuePanelView.Bind의 reopen)
  - 등장 UiPopupOpen, 닫힘 UiPopupClose, 칩 클릭 UiClick
- 위에 없는 A 화면 버튼(X 등)은 UiSfx.Attach로 UiClick이나 UiBack을 붙인다.

## 손대지 말 것
- **마우스 커서 코드** (B-151: GameplayCursor*, PromptB151GameplayCursorBuilder)
- BGM 전체, SettingsRuntimeApplier의 저장 키와 음소거 동작, SettingsMuteButton 겉모양
- 세이브 형식, 게임 규칙·비용·판정, 연출 시간 상수(…Timeline)
- 씬·프리팹 직렬화 (Mine_Demo_Integration.unity의 `warningTone: {fileID: 0}` 줄도 그대로 둔다)
- #158 B~E 항목 (판매·보관·가이드·발소리·드릴 등)

## 테스트
- EditMode 전체 실행: 기존 실패 목록 외에 새 실패가 없어야 한다. GetPitch와 B88 테스트는 그대로 통과해야 한다.
- 새 EditMode 테스트
  - StructuralRiskFeedback에 AudioClip.Create와 warningTone이 없다.
  - 상승 Caution/Danger/CollapseImminent는 반복 1/2/3, 간격은 줄고 높이는 오른다. 하강은 틱 1회, 같은 단계는 무음이다.
  - 듣는 쪽이 없는 SfxRequests, 빈 클립 칸의 Play: 둘 다 예외 없이 무음이다.
  - 같은 SfxId를 0.05초 안에 두 번 → 한 번만 재생한다. 풀이 넘쳐도 Alert는 유지된다. IsMasterMuted면 무시한다.
  - SfxLibrary에 15칸이 있고, 빌더가 기존 음량을 덮어쓰지 않는다. `init/sound-effect-address.md`에 15개 이름이 있다.
- PlayMode는 이번에 추가한 테스트만 실행한다.

## 수동 확인 체크리스트
클립이 없으면 무음이라, **아무 짧은 wav나 같은 이름으로 임시로 넣고** 확인한다. 임시 파일은 커밋에서 뺀다.
- [ ] 클립이 하나도 없을 때 Console 오류가 없고 게임이 평소와 같다
- [ ] 메인 화면: 슬롯 선택, 새 게임, 덮어쓰기 확인·취소가 한 번씩 난다
- [ ] 광산 초기화: 창 열림·닫힘, 확인, 취소, 골드 부족, 실행 whoosh
- [ ] 엘리베이터 홀로그램이 펼쳐질 때와 접힐 때 소리가 난다
- [ ] 위험도가 오를 때 "웽" 1·2·3회로 점점 빠르고 높다. 위험 배경음 위에서도 들리고, 내려가면 틱만 난다
- [ ] 회복 성공·실패, CCTV 켜짐과 연결 결과, 전력 바닥 팝업과 다시 띄우기
- [ ] 시설 근처를 오가도 비프가 거슬리게 반복되지 않는다
- [ ] 음소거(#222)하면 효과음도 꺼지고, 마스터 음량을 줄이면 같이 줄어든다
- [ ] 연타하거나 창을 빠르게 열고 닫아도 소리가 쌓이지 않는다
- [ ] 커서와 BGM 전환이 이전과 같다

## 완료 후
보고 내용
- 변경 파일, 테스트 결과
- 15칸 표 (SfxId, 경로, 연결한 클래스·메서드)
- 클립 넣는 법 한 줄
- 조정 값 위치 (버스·칸 음량, 경보 반복·간격)
- 남은 문제
직접 확인 못 한 항목은 "미검증"으로 적는다. 커밋은 사용자 확인 후에만 한다.
