# B-187 깊이별 광산 배경 그림 교체 + 광물 은은한 반짝임 (#250 / #220)

## 목표
- 열려 있는 [PR #250](https://github.com/Nobamb/sub-terra/pull/250)의 구리·철·리튬 배경 PNG를 **PM이 준 새 그림**으로 바꾼다. 패럴랙스·깊이 섞임·모션 감소는 유지한다.
- 배경 속 광맥·결정·연료 문양 자리에 **가끔만** 은은한 불빛이 몇 번 생기게 한다. 광산 초기화 창의 cave glow, 심층 해금 썸네일 mineral glint와 비슷한 느낌(#172처럼 요란하지 않게).
- 숫자(tint·보이는 높이·반짝 주기·세기)는 상수 하나씩(★) 두어 PM이 숫자만 바꿔 볼 수 있게 한다.

## 브랜치 (기존 PR #250 갱신)
1. `git fetch` 후 **`codex/issue-220-mine-parallax-background`**를 체크아웃한다. 새 `MVP2-fix*`를 만들지 않는다.
2. 최신 `origin/main`(`38af13f88`, #297 병합)을 **이 브랜치에 merge**한다. 충돌 나면 배경·MineBackdrop 쪽은 브랜치 것을 살리고, main의 최신 SFX·전력·규칙 문서 등은 main을 살린다. 해결 요약을 보고서에 적는다.
3. merge 후 이 브랜치에만 커밋·push 한다. **새 PR은 만들지 않는다.** 기존 #250이 갱신되면 된다. 브랜치 삭제·main 직접 커밋·병합 금지(git-commit.md 11).

## 절대 규칙
1. 채굴·암전·보상·저장·가스·구조·전력 규칙은 건드리지 않는다. 배경은 장식만.
2. 씬·프리팹 YAML·ProjectSettings·Packages는 가능한 한 고치지 않는다. 설정은 기존 Editor 빌더/`MineBackdropSettings`로.
3. SfxId·음원·SfxLibrary를 추가하거나 바꾸지 않는다(이번엔 소리 없음).
4. GUID를 새로 만들지 말고 **기존** `mine-cavern-copper/iron/lithium.png` 파일을 **덮어쓴다**(meta GUID 유지).
5. 줄 번호는 merge 후 HEAD 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 새 그림 (PM 제공)
- 경로(우선순위): 프롬프트와 같이 준 `work_process/MVP2/backdrop/B-187/` 또는 첨부 zip `mine-depth-backgrounds-B187.zip`.
  - `mine-cavern-copper.png` · `mine-cavern-iron.png` · `mine-cavern-lithium.png` (각 1672×941 RGB).
- 원본 파일명이 `litunm.png`였어도 **lithium** 이름으로만 넣는다.
- 대상: `sub-terra/Assets/_Project/Art/Backgrounds/mine-cavern-{copper,iron,lithium}.png` 바이너리만 교체. import(Texture Type·sRGB·압축)는 기존 .meta와 맞춘다.
- 예전 그림은 세로(1024×1536), 새 그림은 가로(1672×941). Viewport가 `imageAspect`로 폭을 다시 계산하므로, 교체 후 **게임 안**에서 좌우 끝·깊이 스크롤을 확인한다.

## 먼저 읽을 것
- PR #250 본문 + Nobamb QA 코멘트(밝기·구간 구분·전환·모션 감소·일시정지)
- `App/Integration/MineBackdropController.cs`, `App/UI/MineBackdropSettings.cs`, `Resources/MineBackdrop.shader`, `Editor/MineBackdropAssetBuilder.cs`
- 반짝임 참고(그대로 복붙하지 말고 배경용으로 단순화):
  - `App/UI/SurfaceBase/MineResetPopupMotion.cs`의 `caveGlows` 알파 숨쉬기
  - `App/UI/Progression/DeepZoneThumbnailStrip.cs`의 MineralGlint(주기적 알파)
- `Shared/AccessibilityPreferences.cs` `ReduceMotion`, ESC 일시정지는 `Time.timeScale == 0`(`UiPauseGate`)

## A. 그림·tint·Viewport
1. PNG 세 장을 위 경로에 덮어쓴다. `MineBackdropSettings` 에셋의 copper/iron/lithium 참조가 그대로인지 빌더로 확인.
2. `tint` 기본값을 지금 어두운 회청(`≈0.65/0.70/0.75`)에서 **흰색에 가깝게** 올린다. 권장 시작: `BackdropTint = (0.92, 0.95, 1, 1)`★. 전경(블록·광석·가스·위험)보다 한 단계 어둡고 흐리게 남길 것.
3. `visibleImageHeight`★·`edgePadding`★·`depthTravelWorld`★를 가로형에 맞게 재조정. 목표는 (a) 좌우 끝·빈 화면 없음 (b) 5/17/25/37/40m에서 구간 색·장식이 화면에 들어옴. 바꾼 값과 이유를 보고서 ★ 표에 적는다.
4. 깊이 섞임(16m·36m부터 `depthBlendDistance`★칸, `themeSmoothingSeconds`★≈0.8~1초)과 순간이동 시 부드러운 전환은 **유지**. 리튬 구간이 짧아도 36m에서 이미 리튬 분위기가 느껴지게(기존 QA).

## B. 광물 은은한 반짝임
1. **리튬:** 셰이더 `_LithiumGlow`를 지금처럼 약한 연속 맥동만 두지 말고, **가끔** 조금 더 밝아졌다 돌아오게 한다. 예: 기본 `0.08`★, 반짝 때 `+0.06~0.10`★, 한 번 길이 `0.35~0.6`초★, 다음 반짝까지 `2.5~5`초★ 랜덤. 청록 채널(`max(0,min(g,b)-r)`)만 키운다(기존 Frag).
2. **구리·철:** 같은 셰이더에 따뜻한 주황(구리)·밝은 회청/금빛(철) 채널을 **약하게** 올리는 짧은 펄스를 추가한다. 화면 전체 flash 금지. 광맥·밝은 픽셀만 살짝. 구현 선택(보고에 고른 것):
   - (권장) 셰이더에서 구간별 색 조건으로 펄스(리튬 cyan과 대칭), 또는
   - 배경 UV에 맞춘 얇은 additive glow 마스크 PNG 1~2장(광맥 자리만, 코드 생성 스프라이트도 가능).
3. 반짝은 **드문드문**. 동시에 화면 전체가 깜빡이면 안 된다. 한 시점에 약한 하이라이트 몇 군데 수준.
4. **모션 감소:** 반짝·맥동 정지. 고정 약한 glow만(리튬 `≤0.08`★) 또는 0. 깊이별 그림 전환은 유지(짧은 페이드/즉시).
5. **일시정지:** `Time.timeScale == 0`(ESC 설정 등)이면 glowClock·반짝 타이머 정지. 지금은 `unscaledDeltaTime`이라 멈추지 않음 → **scaled** delta 또는 timeScale 가드로 고친다. 모션 감소와 별개.
6. 저장 후 이어하기·순간이동 직후에도 현재 깊이 그림이 맞고, 반짝만 타이머가 리셋되어도 된다.

## 손대지 말 것
- 효과음, 전경 타일·광석 스프라이트, 암전 오버레이 로직, 메인 메뉴/지상 배경, AGENTS·규칙 문서(교훈 파일 1개는 허용)

## 테스트
- EditMode: 기존 Issue220/MineBackdrop 검사 + 새 `PromptB187MineBackdropRefreshTests`(이름 맞춰도 됨)
  - 세 Texture 참조 non-null, 가로≥세로(새 그림)
  - tint가 구값(0.65/0.70/0.75)보다 밝음
  - LayerWeights: depth 5→구리, 17→철 우세, 37→리튬 우세
  - ReduceMotion: glow 진폭 0(또는 상수 상한), Viewport는 surfaceOrigin 고정
  - timeScale 0이면 glowClock이 증가하지 않음
- PlayMode 가능하면 Bootstrap→탐사 1회. 직접 못 보면 "미검증".
- `git status`로 범위 밖 씬·프리팹·폰트·SFX 변경 되돌림.

## 사용자 테스트 가이드 (보고서에도 그대로)
1. 새 게임→탐사. 배경이 예전 회색보다 밝고, 메뉴·UI와 어색하지 않은지. 그래도 블록·광석이 먼저 보이는지.
2. 1~15m: 구리 구간 분위기. 15→16m에서 철·가스 쪽으로 부드럽게 바뀌는지.
3. 35→36m에서 리튬(청록 결정·연료 문양)으로 바뀌는지. 사다리로 올라가도 같은 경계에서 반대로 바뀌는지.
4. 엘리베이터·긴급 탈출로 멀리 이동해도 깜빡·끊김 없이 도착 깊이 배경인지.
5. 배경 광맥이 **가끔** 은은히 반짝이는지(계속 깜빡이면 안 됨). 리튬에서 더 또렷한지.
6. ESC 설정 여는 동안 반짝이 멈추는지. 모션 감소 ON이면 이동·반짝 멈추고 깊이 그림만 바뀌는지.
7. 좌우 끝·창 크기 변경 시 빈 공간·가장자리 비침 없는지.
8. 저장→이어하기 후 현재 깊이 배경이 맞는지.
9. 캡처: 5 / 17 / 25 / 37 / 40m를 PR #250에 올려 비교.
10. Console 새 빨간 오류 없는지. ★로 더 밝게/덜 깜빡이게 바꾸고 싶은 값 알려 주기.

## 완료 후
- 커밋 예: `feat: 깊이별 광산 배경 그림 교체와 광물 반짝임 (프롬프트 B-187)`. `codex/issue-220-mine-parallax-background`에 push. 새 PR·브랜치 삭제·main 병합 안 함.
- 교훈 있으면 `init/rules/lessons/YYYY-MM-DD-issue220-backdrop-refresh.md` 하나.
- GitHub 이슈 #220·PR #250 본문/체크는 **PM이** 갱신. 에이전트는 결과 보고서만.
- 보고서 `work_process/MVP2/backdrop/B-187-backdrop-refresh-result.md`: 기준 main 해시, merge 충돌 요약, 커밋 해시, 교체 파일, ★ 표, 반짝 구현 방식, 테스트, 위 가이드, 5·17·25·37·40m 캡처 경로(있으면).
- **체크리스트**: [ ] #250 브랜치 push [ ] main merge됨 [ ] PNG GUID 유지 [ ] SFX 변경 없음 [ ] 새 PR 안 만듦 [ ] main 직접 커밋 없음 [ ] 컴파일·EditMode 새 실패 없음
