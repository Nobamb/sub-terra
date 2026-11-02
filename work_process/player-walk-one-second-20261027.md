# 전신 신규 걷기 16장 · 1초 주기 적용

> **후속 선택으로 교체됨:** 사용자가 팀원 GIF를 그대로 쓰도록 요청하여 현재는 원본47장/10fps로 교체했다. 이 문서는 이전 16장 적용 이력이다. 최신 내용은 `player-walk-teammate-gif-20261027.md`를 따른다.

## 현재 결과

- 브랜치: `codex/player-walk-two-leg-cycle` (기존 PR #242 작업 브랜치).
- 사용자는 최신 전신 신규 작화 시안을 기준으로 속도 조정과 커밋·푸시를 요청했다. 공통 머리/몸통 합성은 허용하지 않았으며 이번 적용에도 사용하지 않는다.
- 선택한 재생: **16장 / 16fps / 한 주기 1초**. 앞선 새 원화 미리보기는 16장/20fps/0.8초였고, 기존 HEAD의 게임 런타임 상수와 클립은 이미 16장/16fps/1초였다. 이전 보류된 로컬 8장/12fps 설정을 제거하고 게임·클립·가이드·미리보기를 16장/16fps로 통일했다.
- 16장이라는 이유로 반드시 1초여야 하는 것은 아니다. 이번 기본값은 0.8초보다 여유 있는 재생을 위한 선택이며 실제 게임 이동 속도에 맞춘 추가 조율은 가능하다.

## 원화와 등록

- 원본 시트: `images/player-walk-whole-redraw-20261027/whole-sixteen-v1.png`.
- 적용 원화/미리보기: `images/player-walk-whole-redraw-20261027/approved-one-second/`.
- 그림의 신체 부위를 떼어 붙이거나 늘리지 않고 전신 이미지 전체에만 동일한 배율과 위치 정렬을 적용했다. 전체 키는 기존 Idle의 약 226px 높이에, 발은 Unity 좌표 약 16px에 맞췄다. 16장의 공통 배율을 사용해 자세마다 확대/축소 비율을 바꾸지 않는다.
- `work_process/tools/export_approved_walk.py`가 원본 전신 16개의 영역을 추출한다. 임의 격자 등분으로 신발을 자르지 않는다. 원본 시트·내보낸 16 PNG·영역/배율/해시를 함께 보존한다.
- `RedrawnWalkCycleBuilder`는 선택한 16 PNG를 미리 검증한 뒤 런타임 폴더에 복사하고 기존 Editor API로 프리팹·클립·가이드 참조를 갱신한다. 예전 2×2 핵심/중간 시트나 관절 변형 베이커는 실행하지 않는다. 기존 자동화 진입점 `SmoothWalkCycleBaker`도 이 빌더를 호출한다.
- 런타임 `Frames/Walk/walk_01.png`~`walk_16.png`의 기존 `.meta` GUID를 유지했다. Player 프리팹은 기존 16개 참조를 유지하므로 HEAD 대비 실질적인 참조 변경은 없다. `GameGuideSkin`의 걷기 항목만 16개로 맞추며 다른 가이드 그림은 유지한다.
- 게임 가이드/조작 단말은 같은 장수·속도 상수를 사용한다. App에서 Gameplay를 직접 참조하지 않고 Editor 검사에서 두 값의 일치를 확인한다.

## 검증

- Unity 6000.5.4f1, 배치 Editor에서 `RedrawnWalkCycleBuilder.BuildAndValidate` 실행.
- `PlayerWalkCycleTests` **3건 통과 / 실패0 / 누락0 / ExitCode0** (EditMode, 약5.01초).
- 검사 범위: 선택 원화와 런타임 PNG 바이트 일치, 16개 고유 그림, 프레임별 재생과 1초 반복/Idle 복귀, 클립 속도·길이, 가이드 16개 참조/데모 키, 기존 키/발 기준점 및 잘림.
- 빌더 런타임 점검도 이동 입력이 Idle을 Walk로 바꾸는 것을 확인했다. 실제 게임 플레이 녹화나 PlayMode 테스트로 보고하지 않는다.
- 원화 내보내기에서 RGBA/16개 고유 PNG/높이·발·잘림을 확인했다. GIF를 다시 읽어 16장/1000ms 및 느린 재생16장/2000ms를 확인했다. GIF의 10ms 단위 제한 때문에 정상 프레임 표시시간은 60/60/60/70ms를 반복하며, Unity/MP4는 정확히 16fps다.
- 테스트 결과 경로: `work_process/tools/walk16-one-second-20261027/` (로컬 로그/캐시는 커밋하지 않는다).
- ProjectSettings와 패키지 manifest/lock은 실행 전 SHA-256과 일치한다. 버전/설정 변경 없음. Unity로 생성한 변경만 사용하며 프리팹/씬 YAML을 수동 편집하지 않았다.

## 남은 확인과 커밋 범위

- 실제 Bootstrap→탐사에서 좌우 이동 속도와 발 미끄러짐, Idle/점프/사다리 전환의 시각 검토 및 Windows exe는 미검증이다.
- 원화의 일부 발 간격·팔 타이밍·상체 각도 변화는 남아 있는 리뷰 항목이다. 속도 조정이 모든 작화 결함을 해결했다고 주장하지 않는다.
- 최신 전신 16장·필요한 등록 코드·검사·최신 원본/미리보기/내보내기 도구·이 기록만 커밋한다. 실패한 시안·과거 보류 8장 시트·영상 분리 실험·도구 설치 파일·Unity 캐시는 포함하지 않는다.
- 팀원이 확인할 미리보기: `approved-one-second/walk-1s.gif`, 속도 비교: `timing-comparison.mp4` (0.8초/1초). 비교 영상의 80Hz는 두 재생 속도를 같은 타임라인에 표시하는 시계이며 새 그림 80장을 만든 것이 아니다.

## 재실행

- 저장소 안 Unity 프로젝트 `sub-terra/`에서 메뉴 `SubTerra > Build Redrawn Walk Cycle` 실행. 선택한 PNG 16장은 저장소에 포함되어 Python 설치 없이 다시 등록할 수 있다.
- 원본 시트부터 다시 내보낼 때만 Python + Pillow/NumPy/OpenCV/imageio-ffmpeg가 필요하다. Unity 패키지는 추가하지 않는다. `export_approved_walk.py`의 로컬 도구 경로는 선택 사항이며 설치 파일을 Git에 넣지 않는다.
