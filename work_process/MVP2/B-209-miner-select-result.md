# B-209 새 게임 전 광부 선택 화면 결과 (#266)

## 기준
- 기준 브랜치·커밋: `origin/main` `fb9ac860` (B-208 `MVP2-fix152`가 이미 main에 병합된 상태라 main에서 땄다)
- 작업 브랜치: `MVP2-fix153` (원격에 같은 이름 없음)
- 커밋 해시: `c4f5e884` (1차 기능), `9daed1b8` (보고서 해시), `2fd6ebfe` (2차 디자인·연출 개선, 아래 "2차" 절), 보고서 해시 기재 커밋은 그 뒤
- 3차 디자인 수정 뒤 사용자 요청으로 PR을 만들었다. #266·#375는 건드리지 않았다. 브랜치는 지우지 않았다.

## 2차: 디자인·연출 개선 (사용자 요청, 시안에 더 맞춤)
요청: 선택지가 단순 도형·fade 위주라 아쉬움 → 시안처럼 테두리·에셋·홀로그램 카드·파티클, TV 켜짐 등장, 더 좋은 닫힘 글리치,
고를 때 위→아래로 얼굴·윤곽 드러남, 고를 때·확정 때 스캔 줄이 위→아래로 내려가며 색 입힘.

- **그림(Codex `gpt-6-luna`, 시안을 참고 이미지로)**: 카드 테두리, 홀로그램 받침대, 버튼 테두리, 화면 테두리 4장.
  검은 배경에 그린 발광 그림을 `work_process/tools/prepare_miner_select_art.py`로 밝기→알파(빛 번짐 유지)·자르기·줄이기 해
  `Resources/UI/MinerSelect/`에 넣었다. 꺼진 버튼용 회색 판은 스크립트가 만든다. 원본·프롬프트는 `work_process/MVP2/images/B-209/source/`.
  import 설정(Sprite, 밉맵 끔, 9분할: 버튼 72/40, 화면 테두리 270/170)은 빌더 `SubTerra/MVP2/Build Prompt-B 209 Miner Select Art Import`
  (`Editor/DataValidation/PromptB209MinerSelectArtBuilder.cs`, 상수 경로 5개만)로 건다.
- **TV 켜짐 등장**: 가운데 가로선이 그어지고 → 화면이 위아래로 펼쳐지고 → 옅은 노이즈가 걷힌다(0.68초, 그동안 입력 막음). `UiCctvOn`.
- **돌아가기**: TV 꺼지듯 가운데로 접혀 가로선이 됐다가 사라진 뒤 메뉴로(0.3초).
- **카드**: Codex 테두리 + 반투명 안쪽(세로 그라데이션) + 흐르는 육각 격자·스캔라인 + 바깥으로만 번지는 빛 + Codex 받침대·빛기둥.
  호버/포커스 때 1.03배 커지고 테두리·빛이 밝아진다.
- **광부 그림 셰이더 다시 씀(`MinerHologramUI`)**: 기본은 반투명 청록 실루엣 + 빛나는 바깥 윤곽(얼굴 안 보임) + 가로 띠 지직거림.
  `_Reveal`: 고르면 스캔 줄이 위→아래로 내려가며 그 위쪽부터 얼굴·머리·옷 선이 드러난다(0.6초). 바꿔 고르면 이전 카드는 아래→위로 다시 실루엣(0.3초).
  `_Fill`: 확정하면 두 번째 스캔 줄이 위→아래로 내려가며 원래 색을 입힌다(0.7초). 줄 위치에 카드 너비의 빛 띠와 불꽃 입자가 따라간다.
- **파티클**: `MinerSelectParticleField`(메시 하나로 그리는 UI 입자, 카드당 최대 220개). 받침대에서 빛 알갱이가 피어오르고(호버·고름 때 더 많이),
  스캔 줄을 따라 불꽃, 고를 때·확정 때 한 번에 뿜는다(확정은 따뜻한 흰·금빛). 화면 아래쪽에 느린 먼지도 떠오른다.
- **버튼**: Codex 테두리(9분할) + 어두운 바탕 + 호버 빛 + 주기적으로 지나가는 빛줄기(올리면 더 자주). 꺼진 `탐사 시작`은 회색 테두리.
- **닫힘 글리치(`MinerSelectGlitchUI` + `MinerSelectScreenGlitch`)**: 닫히기 직전 화면을 한 장 캡처해 맨 위 캔버스(32 700, 인트로 32 767 아래)에 깔고
  셰이더로 가로 띠 찢김·RGB 분리·블록 밀림(일부 청록)·흰 가는 줄·옅은 노이즈·세로 흔들림 → TV 꺼지듯 세로로 접혀 밝은 가로선 → 선이 줄며 검정(0.6초).
  캡처를 못 하면(셰이더 없음 등) 예전 막대 글리치 + 세로 접힘으로 대체한다.
- **제목 장식**: 제목 뒤 빛, 구분선은 빛 띠 + 마름모, 양옆 선도 빛 띠.
- 모션 감소: 등장·돌아가기·닫힘 같은 화면 전체 움직임만 짧은 페이드(0.2초)로 바꾸고 `UiCctvOn`·글리치음도 안 낸다. 카드 안 스캔·색 입힘·입자·안내 글자 글리치는 그대로.

## 3차: 버튼·카드 빛·호버·고른 광부 표현 수정 (사용자 요청)
- 버튼(탐사 시작·돌아가기): Codex 테두리 그림 → 시안처럼 모서리만 둥근 얇은 청록 선(절차 9분할 `MinerSelectArt.RoundedOutline`). 바탕·호버 빛·빛줄기·꺼짐 회색은 그대로, 빛은 둥근 바탕 `Mask` 안에서만. 안 쓰게 된 `miner-button-frame(-off).png` 삭제, 빌더 목록에서 뺌.
- 카드 바깥 빛: 고르지 않은 카드·호버만 한 카드는 0. 고른 카드만 깎인 모서리 테두리를 따라 좁게(`MinerSelectArt.CardGlow`, 꼬리가 그림 가장자리에서 0).
- 호버 확대: `HoverSeconds` 0.15 → 0.3초, 확대에 smoothstep.
- 고른 뒤(확정 전) 광부: 1텍셀 밝기 선명화 + 청록 단색 계조, 확대로 번진 알파 가장자리를 1px로 모음, 스캔라인 30%, 깜빡임·글리치 끔. 확정 스캔 전까지 색은 청록 홀로그램만.
- 테스트: `Card_GlowOnlyWhenSelected_HoverGrowsOverThreeTenths`, `Buttons_UseRoundedOutlineAndGreyWhenOff` 추가, `Art_LoadsFromResources` 갱신. B-209 묶음 26/0.
- 확인: 오프스크린 렌더(고르기 전·고른 뒤)로 버튼 모양, 고른 카드만 빛, 네모 상자 없음, 선명한 청록 광부를 봤다. 실제 플레이에서 마우스 호버 체감·확정 연출은 미검증.
- 교훈: `init/rules/lessons/2026-11-23-b209-linear-glow-tail-box.md`

## 바꾼 파일
- 추가 `Scripts/App/UI/MainMenu/MinerSelect/`
  - `MinerSelectFlow.cs`: 순수 상태(포커스·고름·바꿔 고름·확정·돌아가기, 연타 잠금) + `MinerSelectSession`(슬롯 하나, 확정 뒤 `StartNewGame`을 한 번만 부름)
  - `MinerSelectMotion.cs`: 순수 시간표(초안 상수, 글리치/암전 전환, 안내 글자 글리치)
  - `MinerSelectPanel.cs`: 화면 조립·입력·효과음·연출 적용(코드로 생성, YAML 변경 없음)
  - `MinerSelectCardView.cs`: 카드 한 장(테두리·안쪽·격자·광부 그림·받침대·빛기둥·입자·스캔 줄·이름)
  - `MinerSelectArt.cs`: Codex 그림 로드, 절차 생성 그림(빛기둥·스캔 빛 띠·바깥 빛·그라데이션·빛줄기·입자 점), 셰이더 로드
  - `MinerSelectButtonView.cs`(2차): 홀로그램 버튼
  - `MinerSelectParticleField.cs`(2차): UI 입자
  - `MinerSelectScreenGlitch.cs`(2차): 화면 캡처 글리치 오버레이
- 추가 `Resources/MinerHologramUI.shader`: 실루엣·윤곽, 위→아래 `_Reveal`·`_Fill` 스캔, 띠 지직거림
- 추가 `Resources/MinerSelectGlitchUI.shader`(2차): 캡처 화면 글리치 → TV 꺼짐
- 추가 `Resources/UI/MinerSelect/*.png`(2차, 5장), `Editor/DataValidation/PromptB209MinerSelectArtBuilder.cs`(2차), `work_process/tools/prepare_miner_select_art.py`(2차)
- 수정 `Scripts/App/UI/MainMenu/MainMenuBinder.cs`: 새 게임 → 선택 화면, 돌아가기, 인트로·새 게임 시작 연결
- 수정 `Scripts/App/UI/MainMenu/MainMenuView.cs`: 선택 화면 생성·열기/닫기, 열려 있는 동안 메뉴 내용 숨김·키보드 탐색 차단
- 수정 `Scripts/App/UI/Intro/NewGameIntroPlayer.cs`: `RunNewGameLaunch` 추가(기존 오버레이·건너뛰기·켜짐 연출 재사용)
- 수정 `Scripts/App/UI/Intro/NewGameIntroPolicy.cs`: 첫 광산 진입 판정 제거, `ShouldPlayVideo(isNewGameStart, reduceMotion, videoAvailable)`
- 수정 `Scripts/App/Integration/IntegrationRuntimeBinder.cs`: 첫 광산 진입 인트로 재생·튜토리얼 바인딩 미룸 제거(브리핑은 그대로)
- 수정 `Scripts/Shared/Localization/LocalizationService.cs`: `miner_select.*` 한/영 키 9개
- 테스트: 추가 `Tests/EditMode/App/UI/MainMenu/PromptB209MinerSelectTests.cs`(25개), 수정 `Tests/EditMode/App/UI/NewGameIntroTests.cs`(B-205 정책 테스트를 바뀐 위치에 맞춤)
- 자료: `work_process/MVP2/images/B-209/*.png`(절반 해상도)
- 교훈: `init/rules/lessons/2026-11-23-issue266-playmode-edit-wipes-runtime-ui.md`
- 씬·프리팹·폰트 에셋·SfxLibrary·SfxId·영상 파일·HyperFrames·ProjectSettings 변경 없음. 테스트 실행 중 다시 저장된 `MainMenuPanel.prefab`(줄 끝만)과 103 스크린샷 2장은 `git restore` 했다.
- PM 파일(`init/prompt-B.md`, `work_process/MVP2/prompt-B-209.md`, 시안 PNG)은 커밋에 넣지 않았다.

## 화면 구성 (1920×1080 기준)
- 메인 메뉴 패널 안에 `MinerSelectPanel`을 처음 열 때 코드로 만든다. 열리면 `MenuContent`(로고·슬롯·버튼)를 CanvasGroup 알파 0으로 숨기고, 메뉴 배경(광산 그림)·분위기는 어둡게 비친다. 전체 화면 반투명 막이 아래 메뉴 클릭을 막는다.
- 내용은 1920×1080 `Content` 하나에 두고 `min(w/1920, h/1080)`으로 줄여 다른 비율에서도 카드·버튼이 잘리지 않는다. 화면 테두리·막·글리치 층은 화면 전체에 늘린다.
- 위: `SUB-TERRA OPS`(양옆 선) / `광부 선택` / `MINER REGISTRATION` / 구분선(가운데 점) / 설명 `탐사를 시작할 광부를 선택하세요.`
  - 시안의 `함께`는 뺐다. `께` 글리프가 NotoSansKR 동적 아틀라스에 없어 런타임에 추가되면 `Fonts/*_SDF.asset`이 dirty가 되기 때문이다(폰트 변경 금지 규칙).
- 가운데: 카드 2장(414×610, x=±251). Codex 홀로그램 테두리(`///` 포함)·반투명 안쪽·육각 격자·광부 그림(B-208 가이드 스킨의 `player.idle`/`player.female.idle`)·Codex 받침대·투사 빛·입자·이름(`남자`/`여자`).
- 아래: 안내 `광부를 선택해 주세요`(양옆 선), `탐사 시작`(580×92), 왼쪽 아래 `← 돌아가기`(250×70). 화면 가장자리에 Codex 화면 테두리(9분할).

## 카드 상태별 연출
| 상태 | 방법 |
|---|---|
| 등장 | TV 켜짐: 가로선 → 위아래로 펼침 → 노이즈 걷힘(0.68초, 입력 막음). `UiCctvOn` |
| 기본 | 반투명 청록 실루엣 + 빛나는 바깥 윤곽 + 흐르는 스캔라인 + 가끔 가로 띠 지직거림, 받침대에서 빛 알갱이 |
| 올림/포커스 | 카드 1.03배, 테두리·바깥 빛·받침대·빛기둥 밝아짐, 입자 늘어남(`HoverSeconds 0.15`). `UiHover` |
| 고름 | 스캔 줄이 위→아래(0.6초), 줄 위쪽부터 얼굴·윤곽·옷 선이 청록으로 드러남 + 줄 불꽃 + 받침대 입자 분출. 안 고른 카드 밝기 0.45. 처음 고를 때만 안내 글자 글리치 후 숨김. `탐사 시작` 켜짐. `UiSlotSelect` |
| 바꿔 고름 | 이전 카드는 스캔 줄이 아래→위로 올라가며 다시 실루엣(0.3초), 새 카드는 위→아래로 드러남 |
| 확정 | 두 번째 스캔 줄이 위→아래(0.7초), 줄이 지나간 곳부터 원래 색 + 금빛 불꽃 분출. 입력 잠금. `UiConfirm` |
| 닫힘 글리치 | 캡처 화면 찢김·RGB 분리·블록 → TV 꺼짐 가로선 → 검정(0.6초, 끝 프레임 검정). `BriefingGlitchShort` |
| 돌아가기 | TV 꺼짐 접힘(0.3초) 뒤 메뉴. `UiClick` |
| 모션 감소 | 등장·돌아가기·닫힘은 0.2초 페이드(소리 없음). 카드 스캔·색 입힘·입자·안내 글자 글리치는 그대로 |

## 최종 상수 (`MinerSelectMotion`)
`SilhouetteRevealSeconds 0.6`, `SilhouetteHideSeconds 0.3`, `ColorFillSeconds 0.7`, `GlitchSeconds 0.6`, `HintGlitchOutSeconds 0.25`, `ReducedFadeSeconds 0.2`,
`AppearLineSeconds 0.12`, `AppearOpenSeconds 0.36`, `AppearNoiseTailSeconds 0.2`, `CloseSeconds 0.3`, `HoverSeconds 0.15`, `HoverScale 1.03`, `DimmedBrightness 0.45`, `MaxSliceOffset 48`.
(1차의 `SilhouetteSharpenSeconds 0.25`는 위→아래 스캔이 눈에 보이도록 `SilhouetteRevealSeconds 0.6`으로 바꿨다. 프롬프트 초안의 0.25/0.5/0.5보다 길다.)
코드 상수라 검사기에서 바로 바꾸지는 못한다(코드로 만든 화면이라 직렬화 필드가 없음). 조절하려면 이 파일의 상수를 바꾼다.

## 효과음 (기존만)
- 마우스 올림·키보드 포커스 이동 `UiHover`, 카드 고름/바꿔 고름 `UiSlotSelect`, 탐사 시작 `UiConfirm`, 고르기 전 탐사 시작 `UiCancelFail`(꺼진 버튼 클릭은 `UiSfxButton`, 키보드는 패널), 글리치 전환 `BriefingGlitchShort`.
- (2차) TV 켜짐 등장 `UiCctvOn`(기존 인트로 켜짐 소리). 새 SfxId·SfxLibrary 변경은 없다.
- **돌아가기는 `UiClick`.** 프롬프트의 `UiBack`은 B-158에서 없어진 칸(`[Obsolete]`, 클립 없음)이라 쓰면 무음+경고가 난다. B-158 결정대로 취소음 `UiClick`을 썼다.

## 키보드 조작
←/→ 또는 A/D 카드 이동(끝에서 멈춤), Enter/Space/숫자패드 Enter: 포커스 카드가 고른 카드가 아니면 고름 → 한 번 더 누르면 탐사 시작(포커스 없이 누르면 탐사 시작 시도), Esc 돌아가기. 화면이 열린 프레임의 키 입력은 무시해 메뉴에서 누른 Enter가 바로 실패음으로 이어지지 않는다. 열려 있는 동안 메뉴 바인더의 Esc(설정 열기)·X와 메뉴 슬롯/버튼 키보드 탐색은 막힌다.

## 새 게임 흐름
1. `새 게임` → (기존 슬롯이면 지금처럼 덮어쓰기 확인) → `MainMenuBinder.OnStartNewGame`: 런타임 없음/잘못된 슬롯은 예전처럼 실패음, 아니면 `MinerSelectSession(slot)`으로 선택 화면을 연다. **여기서는 저장·슬롯을 건드리지 않는다.**
2. 돌아가기: 화면 닫기 + 메뉴 다시 보임 + 덮어쓰기 허락(`OverwriteGate`)도 버림. 다시 `새 게임`을 누르면 다시 묻는다. 슬롯 파일은 그대로(플레이로 바이트 비교 확인).
3. 탐사 시작 → 색 차오름 → 글리치(또는 암전)로 완전히 검게 닫힘 → `LaunchReady` → 바인더가 `NewGameIntroPlayer.Create()` + `RunNewGameLaunch(...)`.
4. `RunNewGameLaunch`: 오버레이를 `DontDestroyOnLoad`로 두고 셔터를 닫은 채 시작 → 영상(정책상 재생할 때만) → **`session.TryStart()` = `StartNewGame(slot, confirmOverwrite: true, gender)`** (덮어쓰기는 이때만 일어남) → `IsUiReady`가 될 때까지 검은 화면 유지(상한 20초) → 한 프레임 뒤 켜짐 연출(이때만 `UiPauseGate`) → 오버레이 제거. 메인 메뉴가 다시 비치는 틈이 없다.
5. 시작이 실패하면(예외 포함) 오버레이를 걷고 선택 화면을 다시 조작 가능 상태로 되돌린 뒤 `UiCancelFail`.
6. 선택 화면을 만들지 못하는 예외 상황에서는 경고를 남기고 예전 흐름(기본 남자)으로 바로 시작한다.

## 성별 전달
`MinerSelectSession.TryStart` → `SaveRuntimeController.StartNewGame(slot, true, flow.Selected)`(B-208 진입점). 플레이로 여자 선택 → 지상 기지 `State.Player.Gender == Female`, 광산 플레이어 스프라이트 `Female/Frames/Idle/player_idle_01.png` 확인.

## 인트로 이동
- 메뉴 쪽: 위 4번. 영상 재생 조건 `ShouldPlayVideo(true, ReduceMotion, VideoAvailable())`. 모션 감소·영상 없음이면 영상 없이 켜짐 연출만. 영상 준비 실패·오류는 기존처럼 건너뛴다.
- 첫 광산 진입: `IntegrationRuntimeBinder`에서 `BeginNewGameIntroIfNeeded`/`FinishNewGameIntro`와 튜토리얼 바인딩 미룸을 지웠다. 시작 브리핑은 B-205 이전처럼 `WireContracts`에서 바로 바인딩된다.
- 이어하기는 `BeginContinue` 경로라 영상이 없다(코드상 이어하기에서 인트로를 부르는 곳이 없음).

## 테스트 결과
- Unity 컴파일 오류 0(새 경고 0. 처음에 `SfxId.UiBack` Obsolete 경고가 나서 위처럼 `UiClick`으로 바꿈).
- EditMode(편집기 플래그 실행, 묶음: `SubTerra.App.Tests.UI.MainMenu`, `NewGameIntroTests`, `PromptB177SfxMenuTests`, `PromptB208FemalePlayerTests`, `PromptB93ControlSchemeTests`): **158 통과 / 0 실패**. 최신 DLL(15:27:29)보다 뒤에 시작한 실행(15:27:53)으로 확인.
  - B-209 새 테스트: 고르기 전 탐사 시작 비활성·거부(실패음만), 고르면 활성, 바꿔 고르기(안내 글자 재등장 없음), 키보드 이동·고름·확정, 돌아가기 한 번만·재열기 초기화, 확정 시 고른 성별·덮어쓰기 true로 진입점 1회, 돌아가기 시 진입점 미호출, 시작 실패 후 재시도, 모션 감소면 노이즈·어긋남 0 + 0.2초 암전, 글리치 끝 프레임 완전 검정, 안내 글리치 0.25초, 패널 색 차오름 → 검정 → `LaunchReady`(모션 감소면 `BriefingGlitchShort` 없음), 한/영 문구, 첫 광산 진입 영상 미재생·브리핑 바인딩 유지(소스 검사).
- (2차) 같은 묶음 **164 통과 / 0 실패**(최신 DLL 16:11:58 뒤 16:12:11 실행). 추가 6개: TV 켜짐 등장 동안 입력 막힘·무대가 펼쳐지며 끝에 1(모션 감소면 페이드, `UiCctvOn` 없음), 고를 때·확정 때 스캔 줄이 위→아래로만 움직임, TV 꺼짐 돌아가기, 닫힘 글리치의 TV 꺼짐 접힘, Codex 그림 5장·9분할·셰이더 2개 로드. 기존 테스트는 돌아가기가 접힘 뒤에 한 번 오도록, 상수 이름(`SilhouetteRevealSeconds`)에 맞춰 고쳤다.
- EditMode 전체·PlayMode 전체는 돌리지 않았다(범위 밖 씬·프리팹 재저장 위험, 교훈 #342/#306). **미검증**: 그 밖의 묶음.

## 플레이 확인 (Bootstrap → MainMenu, MCP로 버튼 `onClick`·패널 메서드 호출)
| 항목 | 결과 |
|---|---|
| 기존 슬롯 `새 게임` → 덮어쓰기 확인 → 선택 화면, 둘 다 실루엣, 탐사 시작 회색 | 확인 (`01-default.png`) |
| 여자 고름: 또렷해짐·남자 어두워짐·안내 글자 사라짐·탐사 시작 켜짐 | 확인 (`02-selected-female.png`) |
| 남자로 바꿔 고름, 안내 글자 다시 안 나옴 | 확인 |
| 돌아가기(두 번 호출해도 한 번) → 메뉴 복귀, 슬롯 파일 바이트 동일, 다시 새 게임 시 덮어쓰기 다시 물음·처음 상태 | 확인 |
| 키보드 경로(→→ Enter Enter)로 여자 확정 | 패널 메서드 호출로 확인 |
| 여자 확정 → 색 차오름 → 검정 → 인트로 영상 바로 재생, 메뉴 안 비침 | 확인 (`03`, `04`). 글리치 프레임 자체는 0.5초라 캡처 못 함 |
| 영상 끝 → 지상 기지, `Gender=Female`, 오버레이 제거, timeScale 1 | 확인 (`05`) |
| 첫 광산 진입: 영상 없음, 생존자 브리핑 뜸, 플레이어 여자 그림 | 확인 (`06`) |
| 모션 감소 + 남자: 글리치음 한 번도 안 남, 영상 없이 지상 기지, `Gender=Male` | 확인 |
| Console 새 빨간 오류 | 없음(기존 `CoreCctvCameraRig` stereoTargetEye 경고 1개만) |
| (2차) TV 켜짐 등장: 가로선 → 펼침 → 노이즈 걷힘 | 확인 (`07-tv-on-appear-sequence.png`, 실제 플레이 캡처) |
| (2차) 고름 위→아래 스캔으로 얼굴 드러남, 확정 위→아래 색 입힘 | 확인 (`09-scan-reveal-and-color-sequence.png`, 실제 플레이 캡처) |
| (2차) 키보드 포커스 카드 커짐·빛, 탐사 시작 호버 빛 | 확인 (`02`, 포커스·호버는 `MoveFocus`/`SetKeyboardActive`로 켬) |
| (2차) 닫힘 글리치가 실제로 화면 캡처 경로를 탐 → 검정 → 인트로 오버레이 | 확인(`UsesScreenGlitch`=true, 패널 `Black`, 인트로 생성). 모양은 `08-close-glitch-sequence.png`(캡처 화면에 같은 셰이더·같은 시간표를 편집기에서 고정 시각으로 그린 것) |

플레이 전 세이브 폴더를 백업했고 끝난 뒤 원본으로 되돌려 바이트 비교했다. (2차 확인 중 한 번은 영상이 끝날 때까지 플레이를 켜 둬서 슬롯 2가 새 게임으로 덮였다. 백업으로 되돌리고 9개 파일 모두 바이트 동일을 확인했다.)

## 미검증 (직접 보지 못한 것)
- 실제 마우스 호버·클릭, 실제 키보드 입력(←/→/A/D/Enter/Space/Esc). MCP 입력은 Game 뷰 포커스가 없으면 버려져(교훈 #158) 패널 메서드를 직접 불렀다. 호버 밝아짐·`UiHover`는 눈·귀로 미확인.
- 닫힘 글리치를 실제 60fps로 눈으로 본 것은 아니다. 편집기 창이 포커스가 없으면 프레임이 낮아 실시간 캡처가 듬성듬성했고, 모양은 편집기 고정 시각 렌더로 확인했다.
- 실제 마우스로 카드를 올렸을 때의 커짐·빛·입자(포커스 경로로만 확인).
- 아무 키로 영상 건너뛰기(실제 입력 필요), 영상이 끝난 뒤 켜짐 연출의 모습.
- 저장 후 메인 메뉴로 나가 이어하기(테스트 가이드 10번). 코드상 이어하기는 선택 화면·영상을 타지 않는다.
- 효과음이 실제로 들리는지(요청 기록만 확인).
- 4:3·21:9 등 다른 해상도에서의 실제 화면(`Fit` 계산만 있음).

## 사용자 테스트 가이드
1. Bootstrap에서 플레이 → 빈 슬롯(또는 기록 슬롯)에서 `새 게임`. 바로 시작하지 않고 광부 선택 화면이 뜨는지, 둘 다 청록 실루엣이고 `탐사 시작`이 회색인지 본다. 회색 버튼을 누르면 실패음만 나는지 본다.
2. 각 카드에 마우스를 올린다. 카드가 살짝 커지고 테두리·받침대·빛 알갱이가 밝아지며 호버음이 나는지 본다. (화면이 처음 뜰 때 TV처럼 가로선에서 펼쳐지는지도 본다.)
3. `여자`를 누른다. 스캔 줄이 위에서 아래로 내려가며 얼굴·윤곽이 드러나고 남자가 어두워지는지, `광부를 선택해 주세요` 글자가 지직거리며 사라지는지, `탐사 시작`이 켜지는지 본다.
4. `남자`를 눌러 바꾸고 다시 `여자`. 안내 글자가 다시 안 나오는지 본다.
5. `돌아가기`(또는 Esc) → 화면이 TV 꺼지듯 접힌 뒤 메뉴 복귀. 다시 `새 게임` → TV 켜지듯 처음 상태로 뜨는지 본다.
6. ←/→(A/D)로 카드 이동, Enter로 고르고 한 번 더 Enter로 시작되는지 본다.
7. `여자` → `탐사 시작`. 스캔 줄이 위에서 아래로 내려가며 원래 색이 입혀지고, 화면이 찢기듯 지직거리다 TV 꺼지듯 가로선으로 접혀 검게 닫힌 뒤 바로 인트로 영상이 나오는지, 그 사이 메뉴가 비치지 않는지 본다.
8. 영상 끝(또는 아무 키) → 지상 기지가 켜지는 연출 뒤, 캐릭터·가이드 그림이 여자인지 본다.
9. 광산 첫 진입: 영상 없이 생존자 브리핑이 뜨는지 본다.
10. 저장 → 메인 메뉴 → 이어하기: 선택 화면·영상 없이 여자로 이어지는지 본다.
11. 기록 슬롯 `새 게임` → 덮어쓰기 확인 → 선택 화면에서 `돌아가기` → 그 슬롯 이어하기: 기존 저장이 그대로인지 본다.
12. 설정에서 애니메이션 연출 감소를 켜고 `남자`로 7번: 화면 전체 지직거림 없이 짧게 어두워진 뒤 넘어가고(영상 생략), 카드 색 차오름은 보이는지 본다.
13. 새 효과음이 없는지, Console에 새 빨간 오류가 없는지 본다.

## 완료 체크리스트
- [x] `MVP2-fix153` push 확인
- [x] 기준 브랜치 기재(main `fb9ac860`)
- [x] 커밋 해시 기재
- [x] Unity 컴파일 오류 없음
- [x] EditMode 새 실패 없음(관련 묶음 158/0)
- [x] 새 게임 → 선택 화면
- [x] 고르기 전 탐사 시작 비활성
- [x] 고름 시 윤곽·색 또렷, 바꿔 고르기
- [x] 확정 시 원래 색 차오름
- [x] 글리치 → 인트로 바로 재생, 메뉴 안 비침 (글리치 화면 자체는 미검증)
- [x] 고른 성별로 게임 시작
- [x] 첫 광산 진입 영상 없음·브리핑 유지
- [x] 돌아가기 시 슬롯 미변경
- [ ] 키보드 조작 — 로직·패널 메서드로만 확인, 실제 키 입력 미검증
- [x] 모션 감소 시 글리치만 꺼짐
- [x] 기존 UI 효과음만 (`UiBack` 대신 `UiClick`, 위 설명)
- [x] 영상 파일·HyperFrames 미변경
- [x] Gameplay→App 참조 없음
- [x] YAML 손수정 없음
- [x] main 직접 커밋 없음
- [x] PR 안 만듦
- [x] 브랜치 삭제 안 함
- [x] #266·#375 미수정
