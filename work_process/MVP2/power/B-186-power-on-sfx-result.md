# B-186 시설 전력 켜짐 효과음 — 결과 (2026-11-05)

## 상태 요약
- 코드·등록·테스트 완료. Pixabay 자동 다운로드가 막혀 PM이 직접 받아 `power_on_static.wav`를 넣었다(Unity 길이 0.816초, 에셋 음량 0.8).
- PM이 올린 음량 0.8에 맞춰 `SfxLibraryDefaults`도 0.8로 고침(프롬프트의 0.3~0.5 범위를 벗어남). 길이도 목표 0.6초보다 길다.
- 받은 파일이 MP3 데이터였으나 진짜 PCM wav(모노, 48kHz, 16bit)로 변환했다.
- 직접 듣기는 PM 확인 필요(미검증).

## 기준
- 브랜치 `MVP2-fix133` (원격에 없던 번호), 기준 `origin/main` = `6df37571` (B-185-1 `71028ddc` 포함).

## 바꾼 파일
| 파일 | 내용 |
|---|---|
| Scripts/Shared/Audio/SfxId.cs | `PowerOnStatic = 106` 추가(기존 값 그대로) |
| Scripts/App/Audio/SfxLibraryDefaults.cs | World 버스, 음량 `0.8f`★, `"power_on_static"` |
| Resources/SfxLibrary.asset | 106번 칸 추가(`PromptB156SfxLibraryBuilder.EnsureAsset`). clip은 wav를 넣으면 자동 연결. 같은 실행에서 99번 `volume: 1.0`이 `1`로 바뀜(값 동일) |
| Scripts/App/Integration/PowerOnSfx.cs (신규) | `PowerOnEffect.BatchStarted`만 구독 |
| Scripts/App/Integration/IntegrationRuntimeBinder.cs | `BindWorldSfx` 끝에서 생성·구독, `OnDestroy`에서 해제 |
| Tests | 신규 `PromptB186PowerOnSfxTests`. 슬롯 개수를 하드코딩한 기존 테스트 6곳(101→102 등)을 한 칸 늘어난 값으로 수정 |
| init/sound-effect-address.md, work_process/MVP2/sound/sfx-B186/README.md | 출처 줄(작성자 blww) |

## ★ 상수 (PowerOnSfx.cs)
`PowerOnMinGap 0.25f`, `PowerOnStaticCctvGap 0.15f`, `PowerOnStaticDuckedVolume 0.5f`. 음량 0.8f는 SfxLibraryDefaults.

## 음원 표
| SfxId | 파일 | 길이 | 버스/음량 | 원본 구간 | Pixabay URL | 작성자 |
|---|---|---|---|---|---|---|
| 106 PowerOnStatic | power_on_static.wav | 0.816초 | World / 0.8 | 미확인(PM이 받은 그대로) | https://pixabay.com/sound-effects/film-special-effects-short-electric-zap-561890/ | blww |
- 받은 방법: curl → Cloudflare 확인 화면 HTTP 403. 브라우저 필요.
- `sound-effect-address.md`·`sfx-B186/README.md`의 출처와 같음. 작성자·자른 구간은 파일을 받을 때 두 곳에 채워야 한다(**완료 체크리스트의 출처 항목은 미완료**).

## 동작
- `BatchStarted` 하나만 구독 → 시설 3개가 한 번에 켜져도 1번. count로 음량을 키우지 않음.
- 마지막 재생 후 0.25초 안이면 생략.
- 복원 무음: `PowerFacility`가 `PowerOnEffect.IsSuppressed`일 때 연출을 시작하지 않아 `QueueStart`·`BatchStarted` 자체가 없다(코드로 확인, 기존 B-185-1 테스트 `SuppressedRestore_…`가 검증). 추가로 `PowerOnSfx`도 `IsSuppressed`면 생략.
- 모션 감소: 빛만 나오는 경우에도 `BatchStarted`가 오므로 소리는 난다.
- CCTV 겹침: 케이블 연결 성공음은 `CoreCctvPopupView`의 `SfxId.UiConfirm`. 그 소리가 0.15초 안에 났으면 `SfxPlayer.Play(id, 1, 0.5f)`(음량 배율)로 줄여서 낸다. World 슬라이더는 버스 음량으로 그대로 적용된다.

## 테스트 (EditMode, 에디터에서 실행)
- Pass 2446 / Fail 23 / Skip 0. wav·에셋 연결·World/0.8 검증 포함.
- 오디오·전력 관련 실패 없음. 남은 23건은 프리팹·씬·HUD 레이아웃 테스트(`UI.*`, `IntegrationWiringTests` 등)로 이번에 건드리지 않은 영역. **기준(`origin/main`) 상태와 직접 비교하지는 않았음 → 기준 실패인지 미검증.**
- 실행 중 Unity가 폰트·프리팹·씬·ProjectSettings·PNG를 건드려 `git restore`로 되돌림.
- 직접 들어 보기, PlayMode 확인: **미검증**(wav가 없음).

## 사용자 테스트 가이드
1. 전진기지 코어 범위에 충전기를 설치한다.
2. 전력이 들어오는 순간 청록 정전기와 함께 짧은 "지직"이 한 번 나는지 듣는다.
3. 소리가 정전기 튀는 순간과 맞고, 빛이 사라질 때쯤 끝나는지 본다.
4. 너무 크거나 거슬리지 않는지 듣는다.
5. 보건소·정산 콘솔도 같은 소리가 나는지 본다(조명·보관함은 연출이 없으니 소리도 없음).
6. 여러 개가 한 번에 켜지면 소리가 한 번만 나는지 듣는다.
7. 코어 CCTV 창에서 케이블 연결 시 연결 소리와 지직이 뭉개지지 않는지 듣는다.
8. 전력을 끊으면 소리 없이 꺼지는지 본다.
9. 저장 후 이어하기로 들어오면 켜진 시설에서 소리가 나지 않는지 듣는다.
10. 설정에서 효과음 음량을 줄이면 지직도 작아지는지 듣는다.
11. 바꾸고 싶은 음량·간격(★)이 있으면 PM에게 알린다.
12. Unity Console에 새 빨간 오류·노란 경고가 없는지 본다.

## 완료 체크리스트
[x] push [ ] 커밋 해시(PR 본문 참고) [x] 컴파일 오류 없음 [x] EditMode 새 실패 없음(기준 비교는 미검증) [x] SfxId 106 추가·기존 값 그대로 [x] SfxLibraryDefaults·SfxLibrary.asset 등록 [x] `power_on_static.wav` 존재 [x] address.md 작성자 [x] README 작성자(구간은 받은 그대로라 미확인) [x] main 직접 커밋 없음 [x] PR 안 만듦
