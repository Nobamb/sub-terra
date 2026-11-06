# B-186 시설 전력 켜짐 효과음: 짧은 정전기 "지직" (#292 소리 절반)

## 목표
- B-185에서 넣은 전력 켜짐 연출(청록 정전기 + 청록빛)에 **짧은 정전기 소리 하나**를 붙인다.
- 음원은 PM이 골랐다: https://pixabay.com/sound-effects/film-special-effects-short-electric-zap-561890/
- **소리는 이 한 개만.** "웅" 같은 두 번째 층은 만들지 않는다(효과음이 이미 많다는 PM 의견).
- 여러 시설이 한 번에 켜져도 소리는 **한 번**. 불러오기·이어하기는 **무음**.
- 숫자(음량·길이·간격)는 상수 하나씩(★) 두어 PM이 숫자만 바꿔 볼 수 있게 한다.

## 브랜치
1. `git fetch` 후 최신 `origin/main`(B-185 PR #293 병합 `447881b0`, 그 뒤 `278eb442`·`6df37571`까지 있음)에서 새 브랜치 **`MVP2-fix133`**을 만든다.
2. 원격에 `MVP2-fix133`이 이미 있으면 비어 있는 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다.
3. 보고서 맨 위에 기준 커밋 해시와 B-185 커밋(`71028ddc` B-185-1 포함) 포함 여부를 적는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다(11-2). 모든 변경은 `MVP2-fix133`에서만(11-3).
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다(11-9). 브랜치는 지우지 않는다.
3. 연출(`PowerOnEffect` 모양·시간표·★ 값), 전력 규칙, 연출 대상(충전기·보건소·정산 콘솔, B-185-1)을 바꾸지 않는다.
4. 기존 SfxId 값·음원·음량은 바꾸지 않는다. 새 값 하나만 추가.
5. 줄 번호는 `6df37571` 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/` 기준)
- 이슈 #292 본문, `work_process/MVP2/power/B-185-power-on-effect-result.md`(연출 시간표), `B-185-1-power-on-range-result.md`
- Scripts/Gameplay/Power/PowerOnEffect.cs: `Started(PowerFacility, bool)` ~47, `BatchStarted(int, Vector3)` ~49, `FlushPendingStarts` ~176-210, `IsSuppressed`, ★ `SparkWindowSeconds 0.35`·`GlowRiseSeconds 0.15`·`GlowFadeEndSeconds 0.6`·`PowerOnDuration 0.7`
- Scripts/Gameplay/Snapshot/WorldSnapshotSystem.cs ~93-102 (`IsRestoring` + `PowerOnEffect.BeginSuppression/EndSuppression`)
- Scripts/Gameplay/Integration/GameplayEventBridge.cs ~330-371 (복원 중 억제, 판정 끝 `FlushPendingStarts`)
- Scripts/Shared/Audio/SfxId.cs (현재 마지막: 99~101 B-184, 102~105 B-183, 95 LockedNodeRattle)
- Scripts/App/Audio/SfxLibraryDefaults.cs ~266 (`SfxEntry(SfxId.ScanMarkClick, SfxBus.World, volume: …), "scan_mark_click"` 형식), Resources/SfxLibrary.asset
- Scripts/App/Audio/SfxPlayer.cs `Play`, `SecondsSinceLastPlayed` ~82
- 구독 예시: Scripts/App/Integration/DroneScanMarkSfx.cs(IDisposable, 이벤트 구독, `SfxPlayer.Play`) + IntegrationRuntimeBinder.cs ~944(생성·해제)
- Scripts/App/UI/Outpost/CoreCctvPopupView.cs ~476 `UiCctvOn`, ~483 `UiConfirm`/`UiCancelFail`, 그리고 CCTV 창에서 케이블을 연결할 때 나는 소리(찾아서 SfxId를 보고)
- `init/sound-effect-address.md` 마지막 줄들(B-183·B-184 형식), `work_process/MVP2/sound/sfx-B183-184/README.md`

## A. 음원 준비
1. 위 Pixabay 페이지에서 원본을 받는다(Pixabay Content License, 출처 표기 불필요하지만 주소·작성자를 기록). 페이지의 다운로드 주소를 curl/wget으로 받을 수 있으면 그렇게 하고, 브라우저가 필요하면 보고서에 "브라우저 필요"로 적고 멈추지 말고 아래 4로 진행.
2. ffmpeg로 **모노 wav**로 변환, 앞 무음 제거. 길이가 `0.5~0.6초`보다 길면 정전기가 가장 또렷한 앞부분만 남기고 끝 `0.05초` 페이드. 목표: 처음 ~0.35초에 "지직"이 몰리고 ~0.6초 안에 끝남(연출 시간표와 맞춤). 최고점 -1dBFS 근처, 귀를 찌르지 않게.
3. 저장: `Audio/SFX/World/power_on_static.wav` (+ Unity가 만드는 .meta). 원본은 `work_process/MVP2/sound/sfx-B186/`에 두고, 같은 폴더 `README.md`에 **출처를 반드시 적는다**: Pixabay 페이지 URL, 작성자(업로더) 이름, 라이선스(Pixabay Content License), 원본 파일명, 자른 구간(초), 결과 길이·모노 여부·최고점.
4. 받기 실패 시: 같은 이름 자리에 코드 연결은 모두 끝내고, 보고서에 "PM이 `power_on_static.wav`를 이 경로에 넣으면 됨"과 원하는 길이·형식을 적는다. 다른 음원으로 대체하지 않는다.
5. **출처 문서(필수, 빠지면 미완료):**
   - `init/sound-effect-address.md` 표 끝에 한 줄 추가(기존 형식 그대로). Pixabay URL·작성자가 비어 있으면 안 된다:
     `| power_on_static | 시설(충전기·보건소·정산 콘솔)에 전력이 들어올 때 짧은 정전기 "지직"(여러 개 켜져도 한 번) | https://pixabay.com/sound-effects/film-special-effects-short-electric-zap-561890/ | 2026-11-05 | short electric zap (작성자 이름). 원본 a~b초, 끝 0.05초 페이드(원래→결과 길이, 모노, 최고점). World 폴더 |`
   - `work_process/MVP2/sound/sfx-B186/README.md`에도 같은 출처(URL·작성자·라이선스·자른 구간)를 적는다.
   - 결과 보고서의 음원 표에도 같은 Pixabay URL·작성자를 넣는다.
   - 음원이 여러 파일이면 **파일마다** 위 세 곳에 한 줄씩 남긴다(이번엔 `power_on_static` 하나).

## B. 등록
- `SfxId`에 **`PowerOnStatic = 106`** 추가(106이 이미 쓰였으면 다음 빈 값, 95~105와 겹치지 않게, 보고).
- `SfxLibraryDefaults`에 `SfxBus.World`, 음량 `0.4f`★(0.3~0.5 범위)로 `"power_on_static"` 연결. `SfxLibrary.asset`도 기존 방식대로 갱신.

## C. 재생 (App/Integration, 예: `PowerOnSfx.cs`, `DroneScanMarkSfx`처럼 IDisposable)
- **`PowerOnEffect.BatchStarted` 하나만 구독**한다. `Started`(시설마다)는 쓰지 않는다 → 3개가 한 번에 켜져도 한 번.
- `IntegrationRuntimeBinder`에서 다른 World 소리처럼 만들고 해제한다.
- 재생: `SfxPlayer.Play(SfxId.PowerOnStatic)` 한 번. count에 따라 음량을 키우지 않는다.
- 연달아 켜질 때 겹침 방지: 마지막 재생 후 `PowerOnMinGap = 0.25f`★초 안이면 생략.
- **불러오기·이어하기 무음**: B-185가 복원 중에는 `BatchStarted`를 내지 않으면 추가 처리 없음. 실제로 복원 끝에 `FlushPendingStarts`가 이벤트를 내는지 확인하고, 낸다면 `WorldSnapshotSystem.IsRestoring` 또는 `PowerOnEffect.IsSuppressed`로 생략. 확인한 결과를 보고.
- **모션 감소**: 빛만 나오는 경우에도 소리는 낸다(B-185 규칙 그대로).
- **CCTV 연결과 겹침**: CCTV 창에서 케이블 연결 성공음(위에서 찾은 SfxId, `UiConfirm`일 가능성)이 `PowerOnStaticCctvGap = 0.15f`★초 안에 났으면 전력 켜짐 소리를 `PowerOnStaticDuckedVolume = 0.5f`★배로 줄여 낸다(`SecondsSinceLastPlayed` 사용). 줄이는 방식이 SfxPlayer에 없으면 생략으로 대신하고 보고.
- 효과음 음량 설정(World 슬라이더)을 그대로 따른다.

## 손대지 말 것
- 다른 효과음 연결(B-164~B-185), 연출 코드의 모양·시간, 전력 규칙, CCTV 창 동작, `AGENTS.md`·규칙 문서

## 테스트
- EditMode 전체에서 기준의 기존 실패 말고 새 실패 없음. 새 `PromptB186PowerOnSfxTests`:
  - `BatchStarted`(count 1) → `PowerOnStatic` 1번 / count 3 → 1번
  - 0.25초 안에 두 번 → 1번, 그 뒤 → 2번
  - 복원(`RestoreSnapshot`) 중 켜짐 → 0번
  - SfxId 값 106(또는 바꾼 값) 하나만 늘고 기존 값 그대로, 라이브러리에 World·0.4로 등록, wav 존재(없으면 미검증)
  - 해제 뒤 이벤트 → 재생 없음
- 실행 전후 `git status`로 범위 밖 프리팹·폰트·씬·PNG·TimeManager 변경을 되돌린다.

## 사용자 테스트 가이드 (보고서에 그대로 넣기)
1. 전진기지 코어 범위에 충전기를 설치한다.
2. 전력이 들어오는 순간 청록 정전기와 함께 짧은 "지직" 소리가 한 번 나는지 듣는다.
3. 소리가 정전기 튀는 순간과 맞는지, 빛이 사라질 때쯤 소리도 끝나는지 본다.
4. 너무 크거나 귀에 거슬리지 않는지 듣는다.
5. 보건소·정산 콘솔도 켜질 때 같은 소리가 나는지 본다(조명·보관함은 연출이 없으니 소리도 없음).
6. 시설 여러 개가 한 번에 켜지면 소리가 한 번만 나는지 듣는다.
7. 코어 CCTV 창에서 케이블을 연결할 때 연결 소리와 지직이 뭉개지지 않는지 듣는다.
8. 전력을 끊으면 소리 없이 바로 꺼지는지 본다.
9. 저장 후 이어하기로 들어오면 켜진 시설에서 소리가 나지 않는지 듣는다.
10. 설정에서 효과음 음량을 줄이면 지직도 같이 작아지는지 본다.
11. 바꾸고 싶은 음량·간격(★)이 있으면 PM에게 알려 준다.
12. Unity Console에 새 빨간 오류·노란 경고가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 시설 전력 켜짐 정전기 효과음 추가 (프롬프트 B-186)`). `MVP2-fix133`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue292-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #292·#158은 고치지 않는다.** 체크는 PM이 한다.
- 결과 보고서 `work_process/MVP2/power/B-186-power-on-sfx-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일·줄, ★ 상수 위치
  - 음원 표(SfxId | 파일 | 길이 | 버스/음량 | 원본 구간 | **Pixabay URL | 작성자**), 받은 방법(curl/브라우저/실패). `sound-effect-address.md`·`sfx-B186/README.md`에 적은 출처와 같은지 확인
  - 복원 무음 확인 방법, CCTV 연결음 SfxId와 겹침 처리 방식
  - 테스트 결과, 직접 못 들은 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix133` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일·빌드 오류 없음 [ ] EditMode 새 실패 없음 [ ] SfxId 106 추가·기존 값 그대로 [ ] SfxLibraryDefaults·SfxLibrary.asset 등록 [ ] `power_on_static.wav` 존재 [ ] `sound-effect-address.md`에 Pixabay URL·작성자 한 줄 [ ] `sfx-B186/README.md`에 출처(URL·작성자·라이선스·자른 구간) [ ] 결과 보고서 음원 표에 같은 출처 [ ] main 직접 커밋 없음 [ ] PR 안 만듦
