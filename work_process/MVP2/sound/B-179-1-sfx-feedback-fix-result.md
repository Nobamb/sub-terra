# B-179-1 슬라이더 반복·끝소리 / 키 호버 / ESC 함께 닫기

- 기준: `MVP2-fix124`, `385b4a98`. `git fetch`로 확인했고 B-179 `b1471534`가 포함되어 있다.
- 작업 브랜치: `MVP2-fix125`. 번호 충돌 없음. B-180은 `fix126`, B-181은 `fix127`로 한 칸씩 밀린다.
- Unity CLI `1.0.0-beta.15`는 최신이지만 이 Editor의 Pipeline 인스턴스는 탐지하지 못했다. 실행 중인 동일 프로젝트의 Unity MCP로 임포트·실행·검사를 진행했다. 패키지·버전 변경 없음.
- Inspector 추가 연결 없음. 기존 설정 스킨의 OnEnable이 전용 오디오 컴포넌트를 붙이며 MainMenu·SurfaceBase·광산의 공통 경로에 적용된다.

## 변경과 연결 위치

아래 C# 경로는 `sub-terra/Assets/_Project/Scripts/` 기준이다.

| 동작 | SfxId | 파일·줄 |
|---|---|---|
| 방향키·스크롤 등 비포인터 변화 | SettingsSliderDrag(91) | `App/UI/MainMenu/SettingsMenuSkin.cs:21`, `:541` |
| 첫 5% 칸 변화: 앞소리·반복 예약 | SettingsSliderDragHead(92), SettingsSliderDragLoop(93) | `App/UI/MainMenu/SettingsMenuSkin.cs:508`, `App/Audio/SliderDragLoopAudio.cs:62` |
| PointerUp·EndDrag: 다음 반복 끝에 끝소리 | SettingsSliderDragTail(94) | `SettingsMenuSkin.cs:518`, `SliderDragLoopAudio.cs:75` |
| 켜진 키 호버 | KeycapPop(27) | `App/UI/ControlTerminal/ControlTerminalView.cs:40`, `:590` |
| 마우스 그림 호버 | UiClick(1) | 같은 파일 `:41`, `:594` |
| ESC·취소: 설정과 키 창 함께 퇴장 | UiPopupClose(7), 한 번 | `SettingsMenuSkin.cs:171`, `ControlSchemePanel.cs:106` |

### 조정 상수

- `SettingsMenuSkin.cs:21~22`: `VolumeDragSfx`, `VolumeKeySfxGap=0.70f`.
- `SliderDragLoopAudio.cs:25~30`: `HeadSfx`, `LoopSfx`, `TailSfx`, **`KeepLoopWhileHeld=true`**, `StopFadeSeconds=0.03f`, DSP 예약 준비 여유 `ScheduleLeadSeconds=0.02`.
- `ControlTerminalView.cs:39~42`: `KeycapPitchStep=0.03f`, `KeyHoverSfx=KeycapPop`, `MouseHoverSfx=UiClick`, `KeyHoverGap=0.06f`.
- 91~94 음량은 `SfxLibraryDefaults.cs` 끝의 네 칸과 `Resources/SfxLibrary.asset`에서 모두 **0.55**. 기존 에셋 값은 빌더가 덮어쓰지 않으므로 에셋의 91 음량은 Editor API로 갱신했다.

### 슬라이더

- 첫 유효 변화 전에는 무음. 기존 Pending·5% 칸·트랙 클릭 보류를 유지했다. 원샷 간격은 0.70초, 드래그 중에는 91 원샷을 내지 않는다.
- 앞·반복·끝 AudioSource 세 개를 같은 설정 오브젝트에 둔다. 모두 2D·피치 1이고 반복 소스만 loop=true다. SfxPlayer의 API는 변경하지 않았다.
- 샘플 수/주파수로 앞의 종료 시각을 계산하고 `PlayScheduled`로 반복을 잇는다. 손을 놓으면 `SetScheduledEndTime`으로 다음 반복 끝에 멈추고 같은 DSP 시각에 끝소리를 예약한다. 앞 중 놓으면 반복 예약을 취소하고 앞+끝만 재생한다.
- 정확한 반복 경계의 double 오차가 한 바퀴 추가 대기로 번지지 않도록 시간 계산에 샘플보다 훨씬 작은 오차 허용값을 적용했다.
- 재잡기는 이전 소리를 약 0.03초 줄인 뒤 새 앞소리부터 시작한다. 매 프레임 칸×버스×EffectiveVolume(Sfx)를 적용하며 마스터는 AudioListener를 따른다.
- 닫힘·슬라이더 비활성/파괴·음소거·UiPauseGate·포커스 상실·앱 일시정지·클립 누락은 끝소리 예약 없이 취소한다. 드래그 상태도 풀어 다음 입력이 정상 시작된다.
- **수명주기 제한:** 창이 살아 있는 닫힘·포커스 상실 등은 0.03초 페이드다. OnDisable·씬 파괴는 이후 프레임이 없으므로 세 소스를 즉시 Stop한다. 이 경로에서도 반복이 남거나 끝소리가 새로 나지 않는다.

### 키 창과 ESC

- 켜진 키는 `1 + max(0, Row) × 0.03` 피치의 KeycapPop. 마우스는 UiClick/피치 1이다. Idle·기능 키 조건, 0.06초 공통 제한, 자동 시연 무음, 버튼 네 개의 UiHover를 유지했다.
- 설정 닫힘이 처음 받아들여질 때 `ControlSchemePanel.Close(playSound:false)`를 호출한다. 선택 저장은 기존처럼 즉시이고, 독립 X 닫힘은 기본 Close() 경로로 기존 소리를 낸다.
- 설정 상승 0.38초·키 창 퇴장 0.36초 시간표는 변경하지 않았다. 단말 Tick은 긴 프레임을 0.05초로 제한하므로, 설정 상승 후에도 `IsClosing`이면 키 창 퇴장이 끝날 때까지 설정 비활성화 콜백을 잠깐 미룬다. 닫힘을 재호출해도 연출·소리를 다시 시작하지 않는다.

## 음원과 임포트

- `work_process/MVP2/sound/sfx-B179-1/`의 WAV 네 개를 게임 UI 폴더로 이동했다. 입력 폴더에는 MP3·README·address-rows가 남아 있다.
- 이동 전후 SHA-256이 네 파일 모두 일치한다. 음원 파형은 변경하지 않았다. 91의 기존 `.meta` GUID `c6b69dc8467af8041915a702bfa0bac4`도 유지했다.
- 실제 샘플: 91=30775, 92=23991, 93=7481, 94=7225. 모두 44100Hz 모노다.
- `PromptB156SfxPostprocessor`에서 정확히 head/loop/tail 세 파일만 Normalize 끔·PCM 예외를 둔다. Unity의 공개 normalize 속성이 없어 확인된 직렬화 필드 `m_Normalize`로 설정한다. forceToMono·DecompressOnLoad·preload 유지. 긴 반복용 LoopFileNames에는 추가하지 않았다.
- `Temp/b1791-asset-audit.json`: 기준 90칸→최종 93칸, 기존 89칸의 모든 데이터와 버스 볼륨 보존, 91은 volume 0.13→0.55만 변경, 92~94 추가를 확인했다. 칸 8·87~90과 시계 출발 코드는 변경하지 않았다.
- `init/sound-effect-address.md`의 91 출처 설명 교체와 새 세 줄을 반영했다.

## 검증

- 관련 EditMode **88/88 통과**: `Temp/b1791-targeted-editmode.txt`. 임시 검증 코드 제거 후 재검사도 **88/88 통과**, `Temp/b1791-final-targeted-editmode.txt`.
- 새 EditMode 22개: `PromptB1791SfxFeedbackFixTests` 16개(샘플 시각·경계·칸·PCM·기존 칸 보존), `PromptB1791ControlPanelTests` 6개(행별 피치·마우스·간격·저장·퇴장·단독 닫힘).
- 고친 PlayMode **9/9 통과**, `Temp/b1791-playmode.xml`, `Temp/b1791-playmode.txt`: 새 B-179-1 4개, B-179 기존 4개, B-177 슬라이더 1개. 트랙 클릭 실제 ExecuteEvents 순서·긴 유지·재잡기·비포인터 간격·재열기 무음·취소 8경로·시계 보존·설정/키 창 동시 퇴장 검사.
- 전체 최종 EditMode: **2288 통과 / 25 실패**, `Temp/b1791-final-editmode.txt`. 초기 비교 실행 `Temp/b1791-baseline-editmode.txt`는 **2264 통과 / 27 실패**였다. 초기 실행에는 먼저 반영된 키 호버 코드 때문에 기존 B-179 기대값 두 개의 실패가 포함됐다. 이 두 기대값을 요청대로 바꾼 뒤 남은 **25개 실패 집합은 초기와 동일**하다. 새 작업 관련 실패 없음. 초기 실행을 변경 전의 완전히 깨끗한 기준 실행으로 보지는 않는다.
- 기존 시간 의존 검사 묶음 재실행: **54 통과 / 1 실패**, `Temp/b1791-flaky-recheck.txt`. 사다리 24개와 B-150 검사는 통과. 기존 `PromptB138StoragePopupTests.Open_Pops_GrowToPeak_ThenFlyToListIconShrinking`의 0 대 0.000000953674316 비교 실패는 남아 있다. 그 외 실패는 기존 UI/통합/게임플레이/칸41 기대값 문제이며 이번 범위에서 수정하지 않았다.
- Bootstrap→실제 MainMenu에서 설정 열기·1.1초 드래그·놓기·키 창 열기·W/A 호버·ESC와 같은 Binder 취소 경로 실행. `Temp/b1791-smoke.txt`: frame 2558→3862, 유지 중 held=True/loopPlaying=True, 놓은 후 playing=False, W/A 피치 1/1.03, UiPopupClose 한 번, 마지막 settings active=False/terminal hidden=True.
- `Temp/b1791-settings-half.png`, `b1791-controls-half.png`, `b1791-outro-half.png`를 절반 해상도로 확인했다. 기존 오디오 세 줄·설정 레이아웃과 키 창이 보이고, 닫는 중 설정 상승과 키 창 퇴장이 함께 진행된다.
- 실제 재생 구간에 새 App 오류·효과음 누락 경고 없음. 종료 후 compilationFailed=False. Unity AI Toolkit의 계정 API 접근 지연 경고 1개는 별도로 관찰했다.
- 자동 검사가 생성한 범위 밖 폰트·프리팹·씬·PNG·ProjectSettings 변경은 복구했다. 검증용 B1791Smoke/Runner C#과 .meta는 제거했다. `git diff --check` 통과.

### 기존 검사 기대값 변경

- `PromptB156SfxTests.Defaults_HaveAllSlotsWithUniqueIds`, `PromptB165SfxD1FixTests.Volumes_DefaultsAndAssetMatch`, `PromptB168SfxD2Tests.Defaults_D2SlotsNumberedFrom66_MatchTableAndAsset`, `PromptB178SfxTests.Slots87To90_MatchTableAssetAndFiles`: 전체 칸 수 90→93만 변경.
- `PromptB179SfxFeedbackTests.Slot91_MatchesDefaultsAssetImporter_AndSlot8IsUnchanged`: 음량 0.55·짧은 0.698초. 키 호버 관련 두 검사: UiHover→KeycapPop.
- B-177 `Runtime_SettingsTransitionsSliderDropdownAndToggle`와 B-179 `Slider_NoChangeSilent_NonPointerGapAndDisableRecovery`: 비포인터 간격 0.70초. B-179 유지/트랙 클릭 검사는 원샷 요청 대신 전용 AudioSource의 유지 상태와 앞소리 시작 시각을 검사하도록 변경했다.

## 변경 파일

- 런타임: `SfxId.cs`, `SfxLibraryDefaults.cs`, 새 `SliderDragLoopAudio.cs`, `SettingsMenuSkin.cs`, `ControlTerminalView.cs`, `ControlSchemePanel.cs`.
- Editor: `PromptB156SfxLibraryBuilder.cs`의 세 파일 임포트 예외.
- 에셋: `Resources/SfxLibrary.asset`, 교체한 91 WAV, 새 head/loop/tail WAV와 Unity 생성 .meta. 새 런타임 C# .meta.
- 검사: 기존 EditMode C# 5개, 기존 PlayMode C# 2개, 새 EditMode 2개·PlayMode 1개와 Unity 생성 .meta.
- 문서: 출처 표, 본 보고서, `init/rules/lessons/2026-11-02-issue158-slider-audio-segments.md`.
- 기존 사용자 변경 `init/prompt-B.md`, 입력 프롬프트와 참고용 MP3·README·address-rows는 보존하며 작업 커밋에서 제외한다.

## #158 대응 표

| 항목 | 상태 |
|---|---|
| 슬라이더 반복·끝소리 | 구현, 자동 검사·메인 화면 런타임 확인. 직접 청음 미검증 |
| 슬라이더 크기 | 네 칸 0.55 반영, 체감 크기 미검증 |
| 키 조작 창 키 소리 | 구현, 행별 피치·무음 조건 검사와 메인 화면 요청 확인 |
| ESC 함께 닫기 | 구현, 종료 전 퇴장 완료·닫힘음 1회 검사와 실제 화면 확인 |

## 미검증

스피커/헤드폰 직접 청음(이음새·체감 크기·길이), 실제 손으로 ESC·방향키·드래그 입력, 지상 기지·광산의 실제 플레이, Windows 빌드는 미검증이다. 공통 코드와 격리된 PlayMode 검사는 위와 같이 확인했다. 아래 사용자 테스트를 수행한다.

## 사용자 테스트 가이드 (프롬프트 원문)

1. 메인 화면 설정에서 음량 막대를 잡고 이리저리 계속 끈다 → "탁 웅-웅" 다음 "웅웅웅"이 끊김 없이 이어진다.
2. 손을 떼면 끝소리가 붙으면서 자연스럽게 멈춘다(뚝 끊기지 않음).
3. 막대를 누르기만 하고 안 움직이면 소리가 없다. 막대 빈 곳을 한 번 누르면 짧게 한 번("탁 웅-") 난다.
4. 잡은 채 손을 멈추고 있으면 "웅웅"이 계속된다. 이게 어색하면 PM에게 알려 준다.
5. 크기가 다른 메뉴 소리(클릭·창 열림)와 비슷한지 들어 본다. 크거나 작으면 말해 준다.
6. 막대를 선택한 채 방향키로 움직이면 짧은 소리가 한 번씩, 빠르게 눌러도 겹치지 않는다.
7. 끄는 도중 ESC로 창을 닫거나, 다른 창(Alt+Tab)으로 나가면 소리가 바로 멈추고 계속 울리지 않는다.
8. 지상 기지·광산 설정창에서도 1~3번이 똑같다.
9. "키 조작 변경" 창에서 글자가 켜진 키에 마우스를 올리면 창이 열릴 때 나는 키 "톡" 소리가 난다. 빠르게 훑어도 쌓이지 않고, 흐린 키는 무음.
10. 마우스 그림에 올리면 "딸깍" 소리는 그대로다.
11. 키 조작 창을 연 채 ESC → 키 창과 설정창이 같이 닫히는 연출이 보이고 닫힘 소리는 한 번만 난다. X키는 지금처럼 키 창만 닫는다.
12. 효과음 음량·음소거가 새 소리에 다 적용되고, Unity Console에 새 빨간 오류·노란 경고가 없다.
