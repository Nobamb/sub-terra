# PR #176 심층 해금 조건 통일 — 2026-10-15

## 작업 위치와 범위

- 기존 PR: https://github.com/Nobamb/sub-terra/pull/176
- 브랜치: `codex/demo-failure-recovery`, 시작 head `52f5b4a4`.
- PR base는 기존 `codex/demo-visual-issues-170-174`를 유지한다. 새 PR을 생성하거나 main/부모 PR을 병합하지 않는다.
- 별도 sparse 코드 작업 폴더: `C:/Users/jeone/sub-terra/.worktrees/pr176-unlock-align`. Unity에서 실행할 프로젝트가 아니다.
- 첫 번째 Unity 프로젝트와 기존 사용자 변경은 그대로 보존했다.

## 변경

#176에 남아 있던 과거 기준인 목표 13개 완료 + 드릴 Lv.2를 제거한다. 사용자 합의 및 main의 #148 규칙과 동일하게 **드릴 속도 Lv.2 + 드론 스캔 범위 Lv.2 + 가스 저항 Lv.1**을 모두 갖춰야 해금된다. 목표 개수는 별도 잠금 조건이 아니다.

- DeepZoneUnlockRule.Mvp의 요구 장비와 레벨을 main과 일치시켰다.
- 공통 조건 문구 생성기를 사용해 게임 가이드의 별도 하드코딩 조건을 제거했다.
- 퀘스트 설명·힌트와 구형 심층 패널 안내도 동일 규칙에 맞췄다. 목표 0개 안내는 표시하지 않는다.
- 조건 검사 및 업그레이드 구매 경계 검사, 퀘스트 규칙 검사의 기존 기대값을 갱신했다.
- ProgressionService의 기존 `zone.deep` 영구 해금 우선 처리는 유지한다. 이미 해금한 저장을 다시 잠그지 않는다.
- #176의 귀환/실패 복구·저장 검사 및 부모 PR 변경은 보존한다. 업그레이드 비용·연결선·시설 명칭 변경을 추가하지 않았다.
- 과거 `demo-guidance-review-20261013.md` 등의 목표 13개 + 드릴 조건 기록은 당시 기준이다. 현재 기준은 이 문서와 DeepZoneUnlockRule.Mvp다.

## 최소 확인

- #176 실제 소스 기준 Shared / App / 변경 App EditMode 검사 소스의 경량 컴파일 성공.
- 순수 관리형 검사 5건 PASS:
  1. 드릴 Lv.1이면 목표 수가 충분해도 잠금.
  2. 스캔 Lv.1이면 잠금.
  3. 가스 저항 Lv.0이면 잠금.
  4. 세 조건 충족 시 목표 개수와 관계없이 한 번만 해금. 기존 해금 저장 유지.
  5. 퀘스트 순서 유지 및 공통 규칙의 요구 장비 3종/레벨 일치.
- diff 검사 통과. 실제 Unity/Canvas/구매 UI/Windows 실행 검사는 하지 않았다.
- ProjectSettings/Packages/씬/프리팹/에셋/.meta 변경 없음.
