# B-168-1 효과음 D-2 고침 (구조 홀로그램 소리, 시설 설치·가스 맥박 음량)

## 목표
- 사용자 확인 결과 두 가지를 고친다.
  1. 전력 0 뒤 머리 위 구조 홀로그램이 펼쳐질 때 글리치 소리(RescueHoloOpen, 70)가 안 들린다. 홀로그램을 눌러 창을 열 때는 소리가 난다.
  2. 시설 설치 소리가 작다(큰 시설이 더 작게 느껴짐). 가스 맥박은 조금 거슬려 10~20% 줄인다.
- 기준: **MVP2-fix110(d315b63b, B-168)**. 이 커밋이 조상이 아니면 멈추고 보고한다. 작업 브랜치는 사용자 관례대로 `MVP2-fix111`.
- 필요하면 Codex 등 다른 에이전트에게 나눠 맡겨도 된다(값 바꾸기·테스트 작성 같은 반복 작업). 결과는 한 커밋/PR로 모은다.

## 절대 규칙
1. 음원 파일은 고치지 않는다. 시작 위치·음량은 칸 값(`SfxLibraryDefaults.cs`와 `Resources/SfxLibrary.asset` **두 곳 같은 값**)으로만 바꾼다.
2. SfxId 번호, 다른 칸 값, 연출 시간, 게임 로직, 입력은 바꾸지 않는다. SfxPlayer 규칙(같은 소리 0.05초, 동시 8개)은 그대로.
3. Alert 버스는 쓰지 않는다. B-166·B-167·B-168 고침을 되돌리지 않는다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/Scripts/` 기준, d315b63b 줄)
- `work_process/MVP2/sound/B-168-sfx-d2-result.md`
- App/UI/EmergencyRescue/EmergencyRescueChipView.cs(`Show` 131-155, 소리 141), EmergencyRescuePanelView.cs(`Bind` 87-105, `SetChipVisible` 136-151)
- App/UI/EmergencyRescue/EmergencyRescuePopupView.cs(`BeginClose` 241-262 UiPopupClose, `Tick` Closing 343-354, `FinishClose` 377-388, `StartOpening` 358-369 UiPopupOpen), EmergencyRescueTimeline.cs(OutroDuration 0.3, ChipShowDuration 0.32)
- App/Integration/EmergencyRescueRuntimeController.cs(`OpenPanel` 134-162, `ClosePanel` 164-173, `OnPopupClosed` 176-199, `BeginDepletionEpisode` 215-228)
- App/Audio/SfxPlayer.cs(`PlayNow` 298-326, `StartVoice` 415-438), SfxLibraryDefaults.cs(70~72·66번 칸 ~185-200)

## 1. 구조 홀로그램 소리
- 확인한 사실(코드 읽기): 펼침은 `OnPopupClosed` → `SetChipVisible(true)` → `ChipView.Show` → `SfxPlayer.Play(RescueHoloOpen)` 경로라 코드상으로는 불린다. 홀로그램을 누르면 `UiClick` + 팝업 `UiPopupOpen`이 나며, 이것은 **기존 소리**이고 RescueHoloOpen이 아니다.
- 추정 원인: 펼침 소리가 팝업 닫기 소리(UiPopupClose, 0.9초)가 시작된 지 0.3초 뒤에 시작된다. 그런데 rescue_holo_open 앞 0.4초가 점점 커지는 부분이라, 소리의 몸통이 펼침(0.32초)이 끝난 뒤 닫기 소리 꼬리에 묻힌다.
- 할 일
  1. **먼저 실제 경로로 확인**한다. PlayMode에서 전력 0 → 팝업 → 닫기 버튼(또는 ESC) → 닫힘 연출 끝까지 진행하고, RescueHoloOpen 재생이 실제로 시작되는지(재생 기록 또는 `StartedCount`, 그 시점의 음량) 확인한다. 두 번째 고갈(팝업 없이 펼침)도 같다.
  2. 재생이 시작되지 않으면 이유를 찾아 고치고 보고서에 적는다. 이 경우 아래 3은 그대로 한다.
  3. 재생이 시작된다면(묻힘) 칸 값을 `startTime 0.12`, 음량 `0.5 → 0.7`로 바꾼다. 그래도 펼침 순간에 묻힐 것 같으면 그 이유를 적고 대안(예: 구조 팝업이 닫히며 홀로그램이 펼쳐지는 경우에만 UiPopupClose를 줄이기)을 **제안만** 한다.
- 홀로그램을 눌러 창이 열릴 때 RescueHoloOpen이 같이 나지 않는지도 확인한다(이미 보이는 홀로그램은 다시 펼쳐지지 않으므로 무음이어야 한다).

## 2. 음량 (칸 값만)
| 칸 | 지금 | 바꿀 값 | 이유 |
|---|---|---|---|
| 71 BuildPlaceSmall | 0.65 | **0.9** | 가장 작은 소리(0.1초 최대 약 -18.7dBFS) → 약 -15.9 |
| 72 BuildPlaceHeavy | 0.6 | **1.0** | 큰 시설이 더 작게 느껴짐. 앞 0.1초가 조용해서 체감이 작다 → 약 -11.6, 작은 시설보다 크게 |
| 66 GasPulseLoop | 0.45 | **0.38** | 약 15% 줄임(-1.4dB) |
| 70 RescueHoloOpen | 0.5 | **0.7** (+startTime 0.12) | 1번 |
- 클립은 모두 피크 -1dBFS이고 칸 음량은 1.0이 최대라 한 소리가 깨지지는 않는다. 큰 시설을 1.0으로 해도 작으면 음원 증폭이 필요하니 **보고만** 한다.

## 손대지 말 것
- 다른 칸, 버스 음량, 설정창, 체력 경고(68) 음량, 업그레이드 창·해금 소리(B-169), 메인 화면 부팅(B-170)

## 테스트
- 기존 `PromptB168SfxD2Tests`의 칸 표 검사를 새 값으로 고친다(Defaults와 asset이 같은지 포함).
- 새 EditMode 검사 `PromptB168_1SfxFixTests`: 71=0.9, 72=1.0, 66=0.38, 70=0.7·startTime 0.12, asset과 같음.
- PlayMode(`PromptB168SfxD2PlayModeTests`에 추가 또는 새 파일): 실제 닫기 경로로 RescueHoloOpen이 1번 시작, 홀로그램 클릭으로 창을 열 때는 RescueHoloOpen 0번.
- EditMode 전체를 다시 돌려 B-168 보고서의 기존 실패 말고 새 실패가 없는지 확인한다(B-168은 고친 뒤 전체 재실행을 못 했다).

## 수동 확인 체크리스트 (보고서에 그대로)
1. 전력이 0이 되면 구조 팝업이 뜬다. 닫으면 머리 위 홀로그램이 펼쳐지는 바로 그 순간 "지직" 글리치가 분명히 들린다.
2. 전력을 다시 채웠다가 또 0이 되면(팝업 없이 홀로그램만 뜰 때)도 펼쳐지는 순간 "지직"이 난다.
3. 홀로그램을 눌러 창을 열면 클릭·팝업 열림 소리만 나고, 펼침 "지직"이 또 나지 않는다.
4. 사다리·버팀목·조명 설치 "철컥"이 전보다 또렷하다.
5. 충전기·보관함 같은 큰 시설 설치 "쿵-철컹"이 작은 시설보다 크게 들린다. 시설을 옮겨 놓을 때도 같다.
6. 가스 구역 맥박 "두-둥"이 전보다 조금 작아 덜 거슬린다. 박자·멈춤 동작은 그대로다.
7. 효과음 음량을 최대로 해도 소리가 찢어지거나 깨지지 않는다. Console에 경고·오류가 없다.

## 완료 후
- 커밋 메시지 예: `fix: 구조 홀로그램 소리와 설치·가스 음량을 고침 (프롬프트 B-168-1)`. 브랜치는 지우지 않는다.
- 결과 보고서 `work_process/MVP2/sound/B-168-1-result.md`: 기준 커밋, 브랜치, 1번 확인 결과(재생이 시작됐는지, 원인), 바꾼 값 표, 테스트 결과, 미검증 항목, 위 체크리스트.
