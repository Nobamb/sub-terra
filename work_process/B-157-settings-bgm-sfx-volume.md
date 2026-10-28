# B-157 설정창 오디오: 배경음·효과음 음량 따로 추가 (#243)

## 목표
- 설정창 오디오 칸을 **마스터 음량 / 배경음 / 효과음** 세 줄로 만든다. 새 두 줄은 마스터 줄과 모양·동작이 똑같다 (확성기 음소거, 슬라이더, % 숫자).
- 실제 크기는 **마스터 × 채널**이다. 마스터를 끄면 전부 꺼지고, 배경음만 끄면 효과음은 들린다.
- 메인 화면 설정, 지상 기지 설정, 탐사 중 설정(UndergroundSettings) 세 곳이 같은 값이고, 껐다 켜도 유지된다.
- 효과음은 슬라이더 하나다 (UI·월드·경고로 나누지 않는다).
- 기준: 최신 main에서 새 브랜치를 딴다 (B-156 효과음 기반이 합쳐진 0820b93c 이후).

## 절대 규칙 (먼저 읽기)
1. 마스터는 지금 방식 그대로 둔다: `AudioListener.volume`, 키 `subterra.settings.masterVolume`·`subterra.settings.masterMuted`. 키 이름과 기본값(0.5)은 바꾸지 않는다. 기존 플레이어의 마스터 음량이 초기화되면 안 된다.
2. 새 채널 기본값은 **1.0 (100%)**, 음소거 꺼짐이다. 그래야 업데이트 직후 소리 크기가 지금과 같다.
3. 음량 저장은 마스터와 같은 규칙이다. 슬라이더는 미리듣기만 하고, **적용**을 눌러야 저장된다. **취소**하면 원래 값으로 돌아간다. 음소거는 #222처럼 누르는 즉시 저장한다.
4. 줄마다 음소거 규칙은 #222와 같다.
   - 확성기를 누르면 그 채널만 꺼지고 대각선이 그려진다. 슬라이더 값은 그대로다.
   - 다시 누르면 이전 음량으로 돌아온다. 음소거 중 슬라이더를 움직이면 풀리고, 0으로 내리면 대각선을 보인다.
5. 새 UI 스타일은 만들지 않는다. 마스터 줄(캡션, 슬라이더, 필 글로우, 노브, % 글자, 확성기)을 복제해서 쓴다.
6. 프리팹과 씬 직렬화는 고치지 않는다. 줄 추가와 카드 늘리기는 **런타임 생성**으로 한다. 한 번만 만들고 다시 열 때 재사용한다 (줄이 늘지 않게).
7. 시간과 연출은 지금과 같게 둔다 (열기 0.35초, 닫기 0.38초, 비스케일).

## 먼저 읽을 코드 (sub-terra/Assets/_Project/ 기준)
- 저장과 적용: Scripts/App/UI/MainMenu/SettingsRuntimeApplier.cs
  - LoadOrDefaults, Save, Apply, RestoreAppliedVolume, PreviewMasterVolume, SetMasterMuted, IsMasterMuted, MasterMuteChanged
  - BeforeSceneLoad에서 ApplyPersistedControlsOnLoad가 음량을 복원한다.
- 값: SettingsSession.cs. SettingsValues의 MasterVolume, CreateDefaults, Clone, CopyFrom. SettingsSession은 Open/Apply/Cancel/ResetDefaults
- 확성기: SettingsMuteButton.cs (#222)
  - Ensure(slider)가 슬라이더 아래에 "MasterMuteButton"을 만든다.
  - 지금은 마스터 전용이다. 슬라이더를 움직이면 직접 PreviewMasterVolume을 부른다.
- 설정 카드: SettingsMenuSkin.cs
  - volumeSlider, volumeFillGlow, volumeCaption, sectionTitles
  - RefreshScale은 1120×1000 기준이다. 슬라이더 파티클이 있다.
  - FormatVolume이 캡션과 섹션 제목 글자를 쓴다.
- 배치 원본: Editor/DataValidation/PromptB104SettingsMenuBuilder.cs
  - CardSize 1080×810, 오디오 제목 y=265, 마스터 줄 y=210, 디스플레이 제목 138, 해상도 86, 프레임 35, 모션 -17, 시스템 -86, 언어 -144, 조작 -212, 키 조작 버튼 -231, 하단 버튼 -348, 제목 365, 닫기 358
  - 카드 배경 Art/UI/MainMenu/Settings/setting-menu-asset.png (1448×1086)
  - **배경 그림에 칸 테두리가 그려져 있다.** 오디오 칸 본문은 한 줄 높이뿐이다. 카드를 그냥 늘리면 테두리가 어긋난다 (지금은 Simple + preserveAspect).
- 화면: MainMenuView.cs, SurfaceBase/SurfaceBaseView.cs
  - masterVolumeSlider, masterVolumeLabel, SetSettingsDraft, ReadSettingsDraft, OnMasterVolumeChanged, MasterVolumePreviewChanged
- 열고 닫는 쪽: MainMenuBinder, SurfaceBaseBinder, Integration/UndergroundMenuController (취소 시 RestoreAppliedVolume)
- BGM: 씬 AudioSource 4개
  - BGM_Title (MainMenu.unity), BGM_SurfaceBase (SurfaceBase.unity), BGM_Underground·BGM_Danger (Mine_Demo_Integration.unity). 모두 volume 1, loop
  - 재생과 멈춤은 ExplorationBgmController, StructuralRiskFeedback.UpdateDangerBgm이 하고, 음량은 건드리지 않는다.
- 효과음 (B-156, main에 있음): Scripts/App/Audio/SfxPlayer.cs
  - isMuted 기본값 = IsMasterMuted, StartVoice/TickVoice는 `entry.Volume * busVolume`, MasterMuteChanged → StopAll
- 테스트: Tests/EditMode/App/UI/PromptB104SettingsMenuTests.cs (Issue222_…), PromptB89UndergroundMenuTests.cs

## 1. 채널 값과 저장 (SettingsRuntimeApplier, SettingsValues)
- `AudioChannel` enum을 추가한다: Master, Bgm, Sfx.
- SettingsValues에 `BgmVolume`, `SfxVolume`를 추가한다 (기본 1). CreateDefaults, Clone, CopyFrom에도 넣는다.
- 새 키
  - `subterra.settings.bgmVolume`, `subterra.settings.sfxVolume`: 키가 없으면 기본 1
  - `subterra.settings.bgmMuted`, `subterra.settings.sfxMuted`
- 채널 공통 API
  - `IsMuted(ch)`, `SetMuted(ch, bool, float volume01)`, `PreviewVolume(ch, float)`
  - `EffectiveVolume(ch)`: Bgm·Sfx는 음소거면 0, 아니면 현재 값. 마스터는 곱하지 않는다 (AudioListener가 이미 곱한다).
  - `event Action<AudioChannel> ChannelChanged`
- 기존 IsMasterMuted, MasterMuteChanged, SetMasterMuted, PreviewMasterVolume은 이름과 동작을 그대로 남긴다 (B-156과 테스트가 쓴다). 안에서 공통 API를 불러도 된다.
- Apply, RestoreAppliedVolume, 시작 시 복원은 세 채널을 모두 반영하고 ChannelChanged를 보낸다.

## 2. 실제 소리에 반영
- **BGM**: `Scripts/App/Audio/BgmVolumeApplier.cs`를 새로 만든다.
  - [RuntimeInitializeOnLoadMethod]로 만들고 DontDestroyOnLoad로 둔다. SfxPlayer와 같은 방식이다.
  - sceneLoaded와 ChannelChanged 때 이름이 `BGM_`로 시작하는 AudioSource를 찾는다.
  - 처음 본 음량을 기준으로 기억하고 `기준 × EffectiveVolume(Bgm)`을 넣는다. 재생·멈춤·전환 코드는 건드리지 않는다 (위험 BGM으로 바뀌어도 음량 유지).
- **효과음**: SfxPlayer
  - StartVoice와 TickVoice 음량에 `EffectiveVolume(Sfx)`를 곱한다. TickVoice는 매 프레임 다시 계산하니, 재생 중에 슬라이더를 움직여도 따라간다.
  - isMuted 기본값을 `IsMasterMuted || IsMuted(Sfx)`로 바꾼다. 효과음 음소거가 켜지면 StopAll한다.
  - CreateForTests의 주입은 그대로 동작해야 한다. 클래스 맨 위 "음량 조절은 AudioListener가 맡는다" 주석도 고친다.
- 만약 브랜치에 SfxPlayer가 없으면 1번만 하고 효과음 연결은 건너뛴 뒤 보고한다.

## 3. 확성기 일반화 (SettingsMuteButton)
- `Ensure(Slider slider, AudioChannel channel = AudioChannel.Master)`로 바꾼다.
  - 오브젝트 이름: Master는 기존 "MasterMuteButton" 그대로, 나머지는 "BgmMuteButton"·"SfxMuteButton"
  - 클릭, 미리듣기, ShowsMuted, 대각선을 모두 그 채널 기준으로 한다.
  - 구독은 ChannelChanged에서 자기 채널만 받는다 (마스터는 MasterMuteChanged도 유지).
- 기존 한 인자 호출과 #222 테스트는 그대로 통과해야 한다.

## 4. 줄 추가와 카드 늘리기 (런타임, SettingsMenuSkin)
- 새 클래스 `SettingsAudioRows`를 만든다. SettingsMenuSkin.OnEnable에서 Ensure한다 (EditMode 테스트도 OnEnable을 직접 부른다).
- 복제: 마스터 캡션(VolumeCaption), 슬라이더, % 글자를 Instantiate한다.
  - 이름: BgmVolumeCaption/BgmVolumeSlider/BgmVolumeLabel, Sfx…도 같은 식. 복제된 "MasterMuteButton" 자식은 지우고 Ensure(slider, Bgm/Sfx)로 새로 만든다.
  - 런타임 리스너와 EventTrigger 콜백은 복사되지 않는다. 다시 건다.
  - 필 글로우, 노브, 파티클은 세 줄 모두 같게 한다. RefreshVolumeGlow와 EmitSliderParticles는 슬라이더 3개를 처리한다.
- 배치 (기준 1920×1080, 카드 중심 기준 y)
  - 줄 간격 52, 카드 높이 810 → **914**. 카드 윗변 위치를 유지하도록 정한다.
  - 마스터 줄과 그 위(제목, 부제, 닫기, 오디오 제목)는 +52
  - 배경음 줄은 마스터 −52, 효과음 줄은 −104
  - 그 아래 전부(디스플레이~하단 버튼)는 −52
- 배경 그림: 오디오 칸 본문 안쪽의 가로 띠만 세로로 늘린다.
  - 띠는 테두리와 글자가 없는 구간이다. 런타임에 border를 준 Sprite.Create + Image.Type.Sliced를 쓰고, pixelsPerUnitMultiplier로 축척을 맞춘다.
  - 오디오 칸 테두리가 세 줄을 감싸고, 나머지 칸은 원래 모양이어야 한다.
  - 깔끔하게 안 되면 억지로 하지 말고, 필요한 그림 크기와 오디오 칸 높이를 보고한다. 사용자가 같은 경로로 새 그림을 넣는다.
- RefreshScale의 세로 기준 1000은 새 카드 높이에 맞춰 늘린다. 1280×720에서 위아래가 잘리지 않아야 한다.
- 열기·닫기 연출과 드롭다운 위치(언어 목록은 위로)가 그대로 맞는지 확인한다.
- 글자 키
  - `settings.bgm_volume` "배경음" / "Music", `settings.sfx_volume` "효과음" / "Sound Effects"
  - FormatVolume처럼 언어를 바꿀 때 갱신한다. 폰트에 새 글자가 있는지 확인한다.
- 숨겨진 bgm_hint 문구("BGM 4종…마스터 음량으로 조절됩니다")는 "배경음은 배경음 슬라이더로 조절됩니다"로 고친다 (숨김은 유지).

## 5. 화면과 바인더 연결
- MainMenuView, SurfaceBaseView
  - SetSettingsDraft는 세 슬라이더 값을 SetValueWithoutNotify로 넣는다.
  - ReadSettingsDraft는 세 값을 읽는다.
  - % 글자는 슬라이더를 움직일 때 갱신한다.
  - 새 슬라이더는 SettingsAudioRows에서 받아 온다. 직렬화 필드는 추가하지 않는다.
- 세 바인더의 적용, 취소, 기본값 흐름은 그대로 두고, 세 채널이 같이 따라오는지만 확인한다.
  - 기본값 버튼: 초안만 1.0으로 바꾼다. 적용 전에는 미리듣기를 하지 않는다 (마스터와 같다).

## 손대지 말 것
- 효과음 클립, SfxLibrary.asset, SfxId 표, 버스 음량, B-156 연결 지점과 #158 B~E 항목
- BGM 재생과 전환 로직, 클립, 씬의 BGM_* AudioSource 설정
- 마스터 키·기본값·동작, 세이브 형식, 해상도·프레임·언어·조작 설정
- **마우스 커서 코드** (B-151: GameplayCursor*)
- 프리팹·씬 직렬화, PromptB104 빌더 재실행 (꼭 필요하면 이유를 보고)
- 관련 없는 리팩터링과 이름 바꾸기

## 테스트
- EditMode 전체 실행: 기존 실패 목록 외에 새 실패가 없어야 한다. Issue222 테스트와 B-156 SfxPlayer 테스트는 그대로 통과해야 한다.
- 새 EditMode 테스트 (PlayerPrefs는 테스트 끝에 원래대로 되돌린다)
  - 새 키가 없으면 Bgm·Sfx는 1, 마스터는 저장값 그대로다. 저장 → 다시 읽기를 하면 세 값과 두 음소거가 유지된다.
  - 줄마다 음소거 → 대각선과 값 유지 → 다시 누르면 복귀. 음소거 중 슬라이더를 움직이면 해제된다. 0이면 대각선이다. 다른 채널은 영향이 없다.
  - Bgm 음소거: BGM_ 소스는 0이고, SfxPlayer는 재생된다. Sfx 음소거: SfxPlayer는 무음이고 BGM은 그대로다. 마스터 음소거: AudioListener 0.
  - 효과음 0.5 → 재생 음량이 entry × bus × 0.5다.
  - 두 프리팹 모두 OnEnable을 두 번 해도 줄이 3개다. 순서는 마스터, 배경음, 효과음이고 서로 겹치지 않으며 카드 안에 있다.
  - 취소하면 세 값이 원래대로, 적용하면 저장된다.
- PlayMode는 이번에 추가한 테스트만 실행한다.

## 수동 확인 체크리스트
실행 경로: Bootstrap → 메인 화면 설정 → 새 게임 → 지상 기지 설정 → 탐사 중 ESC 설정.
- [ ] 설정창에 마스터 / 배경음 / 효과음 세 줄이 같은 모양으로 보인다
- [ ] 설정창이 세로로 늘어나 잘리거나 겹치는 곳이 없다. 오디오 칸 테두리가 세 줄을 감싼다 (1280×720, 1920×1080)
- [ ] 배경음 슬라이더를 내리면 배경음만 작아지고 효과음은 그대로다
- [ ] 효과음 슬라이더를 내리면 효과음만 작아지고 배경음은 그대로다
- [ ] 배경음·효과음 확성기를 누르면 그 소리만 꺼지고, 다시 누르면 원래 음량으로 돌아온다
- [ ] 마스터 음소거는 지금처럼 모든 소리를 끈다
- [ ] 취소하면 슬라이더와 소리가 원래대로 돌아온다
- [ ] 메인 화면 설정과 게임 중 설정이 같은 값이다
- [ ] 게임을 끄고 다시 켜도 세 음량과 음소거 상태가 유지된다. 기존 마스터 음량도 그대로다
- [ ] 위험 BGM으로 바뀌어도 배경음 음량이 유지된다
- [ ] 열기·닫기 연출, 드롭다운, 하단 버튼 좌우 키, 커서가 이전과 같다
- [ ] Console 오류가 없다

## 완료 후
보고 내용
- 기준 커밋, 변경 파일, 테스트 결과
- 새 PlayerPrefs 키 4개와 기본값
- 카드 새 크기와 배경 그림 처리 방법 (늘린 띠 위치 px, 또는 필요한 새 그림 크기)
- BGM_ 이름 규칙으로 찾은 소스 목록
- 남은 문제
직접 확인 못 한 항목은 "미검증"으로 적는다. 커밋은 사용자 확인 후에만 한다.
