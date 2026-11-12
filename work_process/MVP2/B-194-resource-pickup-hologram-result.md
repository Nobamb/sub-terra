# B-194 자원 수확 홀로그램 등장·아이콘 스캔 공개 결과 (#151, PR #259 참고)

## 기준
- 기준 브랜치/커밋: `main` = `origin/main` = `fcf180d73106ecf959cba29623518e2e245a8f03`
- 만든 브랜치: `MVP2-fix138` (원격에 같은 이름 없음 확인)
- #259(`codex/issue-151-resource-pickup-vfx`)에서 가져온 것: `cfaa8536`의 `ResourcePickupVfx.cs`·테스트·`IntegrationRuntimeBinder` 연결 **구조만**(지급 확정 큐 `QueueAccepted`, 풀 8칸, 연료 판별, 반짝임 3개, `BindResourcePickupVfx` 위치). 타일→플레이어 비행, 모션 감소 축소, 청록 테두리 회전은 가져오지 않음. 파일은 최신 main 위에 새로 작성함(cherry-pick 아님).
- 커밋 해시: **미커밋** (아래 "상태" 참고)

## 바꾼 파일
- `UI/PlayerStatus/PlayerHeadMessageStackModel.cs`: `ShowResult`(진행→결과, 같은 항목 유지·`Level=0`·`Progress=-1`), `PlayerStatusEntry.RestartShow/ConsumeRestart`, 장식 높이 `Headroom`/`DecorationVersion`(스택 높이만 반영, `MaxEntries`에 안 셈), `FindShown/Contains/AttachDecoration/DetachDecoration`. 진행 갱신(`SetProgress`)은 등장 재시작 없음.
- `UI/PlayerStatus/PlayerHeadMessageStack.cs`: `ShowResult` 전달, 뷰 루프에서 `ConsumeRestart()`이면 그림도 되감기.
- `UI/FacilityNameTag/FacilityNameTagVisual.cs`: `RestartShow()` 추가(꺼지지 않고 level·clock만 0, 소리 없음. `PlaySfx`는 그대로 false).
- `Integration/MiningProgressHud.cs`: `Completed`에서 `ShowInfo` → `ShowResult`, `ProgressKey` public.
- `Integration/IntegrationRuntimeBinder.cs`: `resourcePickupVfx` 필드, `BindResourcePickupVfx`(골드 바인딩 뒤·`BindMinimap` 앞), `TryCommitMining`에서 `QueueAccepted`, `OnDestroy`에서 `Unbind`.
- 새 파일: `ResourcePickupVfx.cs`, `ResourceHologramIcon.cs`, `ResourceHologramTimeline.cs`, `ResourceHologramSprites.cs`, 테스트 `ResourcePickupVfxTests.cs`.
- 기존 테스트 수정: `Progress_UpdatesSameEntryAndTurnsIntoResultInPlace`(새 조건), `MiningHud_RoutesProgress...`(소스 문자열 `ShowResult`).

## 연출 요약 (2차: 프로젝터형으로 강화)
- 등장 재시작: 모델 `Level`과 그림 `level`을 함께 되감음. 길이는 기존 `FacilityNameTagTimeline.ShowDuration`(0.26s).
- 프로젝터: 문구 프레임 위 타원 바닥 링·글로우, 프레임에서 아이콘으로 퍼지는 깜박이는 빔, 떠오르는 데이터 입자 14개. 간격 14px.
- 아이콘: 팝인(0.35→살짝 오버슈트→1, 0.34s) 뒤 둥실 떠 있음. 시작은 청록 덮개+가로 줄무늬+줄별 좌우 떨림+색수차 유령(청록·분홍), 두꺼운 스캔 줄+글로우 띠가 위→아래로 지나가며 줄마다 원래 색(방금 드러난 줄은 번쩍). 스캔 기본 0.22s(0.2~0.3 조절).
- 코너 브래킷 8개가 바깥에서 모여 맞물림(흰색→청록).
- 스캔 완료 순간: 섬광, 충격파 고리, 방사형 불꽃 12개(0.5s, 1회). 이후 1.5초마다 얇은 광택 줄이 훑음.
- 엔진 연료: 아이콘 0.62(일반 0.46), 금빛 광선 8줄(회전), 따뜻한 후광 1회, 금색 큰 충격파·불꽃 20개, 반짝임 3개.
- 다음 채굴로 같은 항목이 진행 문구로 돌아가면 0.12s 흐려지며 물러나고 예약 높이 반환. 풀 8칸. 모션 감소는 읽지 않음(줄이지 않는 결정).
- 에디터에서 프레임별(0.06~1.2s) 렌더링해 구리·철·리튬·연료 모양을 확인함(게임 화면 속 위치·크기 감각은 미검증).

## 테스트
- EditMode 전체: Pass 2647 / Fail 27 (2차). 새 테스트 `ResourcePickupVfxTests`와 수정한 `PlayerHeadMessageStackTests`·`FacilityNameTagTests`·골드 검사에는 실패 없음.
- 남은 실패 27건은 터미널 명령 문구, 시설 설치(NRE), 프리팹/씬 레이아웃, 폰트 아틀라스 등 이번 변경 파일과 무관한 영역. **main 기준 동일 실패인지는 직접 대조하지 않음 → 미검증.**
- 실제 플레이로 본 항목(스캔 모습, 색 구분, 후광, CCTV, 콘솔 경고): **전부 미검증**.

## 사용자 테스트 가이드
`work_process/MVP2/prompt-B-194.md`의 "사용자 테스트 가이드" 1~10 그대로.
