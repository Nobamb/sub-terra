# B-205 게임 시작 세계관 인트로 영상 (#375, #151)

## 목표
새 게임 뒤 첫 세계관 브리핑으로 이어지는 CCTV 광산 오프닝 영상을 HyperFrames로 만든다. #151의 **미완료** 항목 「게임 시작할 때 현재 세계관을 설명하는 애니메이션」을 구체화한다(#375). 이미 끝난 「게임 키기 전에 나타나는 짧은 인트로 애니메이션」(`MainMenuIntroDirector`)은 건드리지 않는다.
기준: https://github.com/Nobamb/sub-terra/issues/375, https://github.com/Nobamb/sub-terra/issues/151
#375·#151 댓글에는 이 항목을 바꾸는 추가 설정이 없다. 세계관 문구를 새로 만들지 않는다. 영상 안에서는 자원 이름이나 문양이 나타난 이유를 설명하지 않는다(#375).

## 브랜치
1. `git fetch` 후 최신 `origin/main`에서 새 브랜치 **`MVP2-fix149`**를 만든다. 이 문서를 쓸 때의 main은 `30fa000cdd4c020e4520a8fdc9bfb2e5f4ba1be8`이다. (`MVP2-fix147`은 B-203/#291, `MVP2-fix148`은 B-204/#298용. 이 번호는 쓰지 않는다.)
2. 작업 시점의 `origin/main`에서 딴다. 원격에 같은 이름이 있으면 빈 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다. 브랜치를 지우지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix149`(또는 실제 쓴 번호)에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다. #375·#151 체크박스·댓글·닫기는 하지 않는다.
3. 새 음원·BGM, 새 `SfxId`, `SfxLibrary`, 음량을 먼저 만들지 않는다. #375가 요구하는 기계 저음·짧은 전자 잡음은 **기존 게임 음원부터** 쓴다(예: `ui_cctv_on.mp3`, `briefing_glitch_tick.wav` / `_short.wav`, `empty_slot_noise_loop.wav`, `elevator_move_loop.wav`, `power_on_static.wav`). 부족한 점만 보고서에 적는다.
4. 이 영상은 화면 전체 연출이다. `AccessibilityPreferences.ReduceMotion`이면 영상을 건너뛰거나 마지막 프레임만 보이고 바로 기존 브리핑으로 간다. 메뉴 부팅(`MainMenuIntroDirector`)용 분기를 새로 만들지 않는다.
5. 프리팹 YAML을 손으로 고치지 않는다. 채굴·저장·브리핑 문구·목표 진행 규칙은 바꾸지 않는다.

## 현재 상태 (main `30fa000c`)
- **세계관 문구(브리핑, 영상에는 넣지 않음).** `DemoObjectiveCatalog`: 제목 `생존자 브리핑`, 확인 `작업 시작`, 본문 그대로: 「지상에는 더 이상 자원이 남지 않았습니다. / 배양시설에는 인류의 유전 정보가 보존되어 있습니다. / 유전자 개량에 성공하면, 인류를 다시 태어나게 할 수 있습니다. / 시설을 가동할 자원은 이제 땅 아래에 있습니다. // 내려가십시오. 캐고, 버티고, 살아서 돌아오십시오.」 새 게임은 `SaveRuntimeController.StartNewGame`이 `SceneNames.SurfaceBase`를 연다. 광산은 `TryStartExploration` → `SceneNames.Integration`(`Mine_Demo_Integration`). 첫 목표 `MineBlock`에서 `DemoObjectivePresenter`가 `StartBriefingPopupMotion`으로 브리핑을 띄운다.
- **기존 메뉴 인트로.** `MainMenuIntroDirector` + `MainMenuBootTimeline`(셔터·로고 `Art/UI/MainMenu/MainMenu_Title.png`, 「SUB-TERRA OPS // 신호 연결 중...」). 영상 파일이 아니다. 수정하지 않는다.
- **재사용할 아트·색.** 타일 `Art/Tiles/Ground/`(`ground_normal_*`, `ground_dark_*`, `ground_deep_*` 등), 배경 `Art/Backgrounds/mine-cavern-backdrop.png`, 엘리베이터 `Art/Facilities/MVP/elevator_station_mine.png`·`elevator_door_panel_mine.png`, `Art/Tiles/ProtectedElevatorBlock.png`. 엔진 연료 스캔 문양: 회색 돌 `Art/Tiles/SealedGlyph/sealed_glyph_stone_01.png`, 깨어난 청록 문양 `sealed_glyph_awakened_01.png`(`SealedGlyphScanVisual`, `tile.locked.signal`). 광부 `Art/Characters/Player/Frames/Knockout/knockout_*.png`와 `Idle/player_idle_01.png`, 드론 `Art/Characters/Drone/digger_bot_idle.png`. CCTV 청록은 `CoreCctvPopupBuilder.Cyan` `(0.42, 0.94, 1)`과 `CoreCctvArt` 스캔라인·노이즈. 게임 기본 해상도 1920×1080. 인게임 MP4 폴더는 아직 없다. `com.unity.modules.video`는 이미 있다.

## HyperFrames
작업은 **`hyperframes\sub-terra-intro`**에서 한다. 공식 문서(https://hyperframes.heygen.com)만 따른다. API를 지어내지 않는다. 설치된 CLI가 권위이므로 시작 시 `--help`와 `help render`로 플래그를 확인한다.
문서에서 확인한 사용법: 프로젝트는 `index.html`(루트에 `data-composition-id`, `data-start="0"`, `data-width`, `data-height`, `data-duration`), 장면은 `class="clip"` + `id` + `data-start` + `data-duration`. GSAP는 `{ paused: true }` 타임라인을 `window.__timelines[compositionId]`에 등록. 벽시계·비시드 난수·렌더 중 네트워크 요청은 쓰지 않는다(GSAP 등 스크립트는 프로젝트 안에 둔다). 소리·그림은 `<audio>`/`<img>`에 `data-start`·`data-duration`(필요하면 `data-volume`, `data-fade-in`/`data-fade-out`). `play()`/`pause()`/`currentTime`은 호출하지 않는다. 루프: `lint` → `check` → `preview` → `render --output <파일> --format mp4`. 초안은 `--quality draft`, 프레임 속도는 `--fps`. 최종 품질 이름은 `help render`를 따른다.

## 할 일
1. **`hyperframes\sub-terra-intro`**에 수정 가능한 구성을 만든다. 초안 상수(조절 가능, 보고): **1920×1080**, **30fps**, **약 45초**(장면 템포에 맞춰 조정). 게임 아트·색을 복사해 쓰고 원본은 바꾸지 않는다. 지구 전경은 생략한다. 글리치는 CCTV 연결과 시점 전환에만 짧게. 출력 MP4는 `sub-terra/Assets/_Project/Art/Video/intro_cctv_opening.mp4`(Unity에서 재생 가능한 MP4). `node_modules`와 중간 렌더는 커밋하지 않는다.
2. **장면 순서(#375, 문구를 보태지 않는다).**
   1. **CCTV 연결** — 검은 화면, 짧은 지직·글리치 뒤 광산 입구 **0m** 화면이 잡힌다.
   2. **광산 하강** — CCTV 탐사 시점이 0m부터 지층을 따라 천천히 내려간다. 주변이 점차 어두워지고 깊이와 규모가 드러난다.
   3. **회색 돌의 문양** — 심층 매끈한 회색 돌에 청록 문양이 잠깐 나타난다. **인게임 엔진 연료 스캔과 같은 문양**(`sealed_glyph_awakened_01.png`)을 쓴다. 이름·이유는 쓰지 않는다.
   4. **광부의 각성** — 시점이 위로 돌아오면 엘리베이터 앞에 광부가 쓰러져 있다. 정신을 차리고 일어나 앞의 드론을 바라본다.
   5. **브리핑으로 전환** — 마지막 프레임의 광부·드론 위치와 카메라 구도를 `Mine_Demo_Integration` 게임 시작 화면과 대조해 맞춘다. 기존 생존자 브리핑으로 넘길 지점을 남긴다.
3. **게임 연결.** 기본: 새 게임의 **첫** `Mine_Demo_Integration` 진입에서 MP4를 보여 준 뒤, 기존 `StartBriefingPopupMotion`·`DemoObjectiveCatalog` 브리핑을 이어서 띄운다. 이어하기·두 번째 이후 광산 재진입에서는 재생하지 않는다. `StartNewGame`의 도착 씬(지상 기지)과 브리핑 문구, 메뉴 부팅은 바꾸지 않는다. 건너뛰기는 메뉴 부팅처럼 아무 키/클릭으로 두는 것을 기본으로 하고 보고서에 적는다. ReduceMotion이면 위 규칙대로 생략/마지막 프레임만.
4. EditMode가 실용적이면: 새 게임 첫 광산 진입에서만 재생되고, 이어하기·모션 감소에서는 브리핑으로 바로 가는지.

## 테스트 (플레이 가이드)
1. 새 게임 후 광산에 처음 들어가면, 위 다섯 장면이 순서대로 보이는지 확인한다. 문양은 게임의 엔진 연료 스캔과 같고, 자원 이름 자막이 없는지 본다.
2. 영상이 끝나면 기존 생존자 브리핑이 이어지고, 마지막 프레임이 엘리베이터 앞 시작 구도와 크게 어긋나지 않는지 대조한다.
3. 이어하기로 들어가면 영상이 다시 안 나오는지, 모션 감소면 영상 없이(또는 마지막 프레임만) 브리핑이 뜨는지 본다.
4. 메인 메뉴 켜짐 연출이 이전과 같은지, Unity Console에 새 빨간 오류가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 새 게임 세계관 CCTV 인트로 영상을 추가 (프롬프트 B-205)`). `MVP2-fix149`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue375-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #375·#151은 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-205-cctv-mine-intro-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일, 구성 경로(`hyperframes\sub-terra-intro`), 출력 MP4 경로
  - 해상도·fps·길이·장면 시간, 재사용한 아트·음원, 마지막 프레임 대조, 게임 연결·건너뛰기·ReduceMotion
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix149` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] 다섯 장면 순서·문양 일치·설명 자막 없음 [ ] 수정 가능한 HyperFrames 소스와 Unity 재생 MP4 [ ] 마지막 프레임을 게임 시작 화면과 대조 [ ] 새 게임 첫 광산 진입 후 기존 브리핑 이어짐 [ ] 이어하기·재진입 미재생 [ ] ReduceMotion 생략/축소 [ ] 메뉴 부팅·브리핑 문구 미변경 [ ] 새 SfxId/BGM 없음(기존 음원 우선) [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] 브랜치 삭제 안 함 [ ] #375·#151 미수정
