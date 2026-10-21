# 비연출 수정 게시·데스크탑 인계 — 2026-10-21

## 게시 현황

- 수정 PR 8개와 #211 조사 초안 PR 1개를 독립 브랜치에서 커밋·푸시했다. 각 PR은 main ee25ddab을 기준으로 하며 다른 이슈 코드가 포함되지 않는다.
- 게시 후 GitHub 조회에서 9개 모두 open, mergeable=true, mergeable_state=clean임을 확인했다. #211만 draft다. 이후 main 변경에 따라 상태가 달라질 수 있다.
- main과 PR은 병합하지 않았다.

| 이슈 | PR | 상태 | 브랜치 |
|---|---|---|---|
| #215 | [#225](https://github.com/Nobamb/sub-terra/pull/225) | 수정 게시 | `codex/issue-215-structural-tile-visual` |
| #209 | [#226](https://github.com/Nobamb/sub-terra/pull/226) | 수정 게시 | `codex/issue-209-surface-quit` |
| #218 | [#227](https://github.com/Nobamb/sub-terra/pull/227) | 수정 게시 | `codex/issue-218-rescue-indicator` |
| #210 | [#228](https://github.com/Nobamb/sub-terra/pull/228) | 수정 게시 | `codex/issue-210-main-menu-cursor` |
| #217 | [#229](https://github.com/Nobamb/sub-terra/pull/229) | 수정 게시 | `codex/issue-217-quest-reward-close` |
| #219 | [#230](https://github.com/Nobamb/sub-terra/pull/230) | 수정 게시 | `codex/issue-219-empty-slot-start` |
| #216 | [#231](https://github.com/Nobamb/sub-terra/pull/231) | 수정 게시 | `codex/issue-216-remove-move-button` |
| #212 | [#232](https://github.com/Nobamb/sub-terra/pull/232) | 수정 게시 | `codex/issue-212-storage-bulk-transfer` |
| #211 | [#233](https://github.com/Nobamb/sub-terra/pull/233) | 조사 초안·미재현 | `codex/issue-211-surface-upgrade-investigation` |

## 이 PC·데스크탑 실행 기준

- 저장소 C:/Users/jeone/sub-terra, Unity 프로젝트는 C:/Users/jeone/sub-terra/sub-terra (Unity Hub 첫 번째 항목), Unity 6000.5.4f1.
- 여러 PR을 함께 확인할 때만 `codex/nonvisual-review-20261021`을 사용한다. 이 브랜치는 위 9개 브랜치를 합친 검토용이며 main 대체가 아니다.
- 게시한 각 PR 파일과 통합 브랜치의 파일이 모두 동일함을 Git 비교로 확인했다. 이 확인 때문에 Unity 검사를 다시 반복하지 않았다.
- 한 이슈만 작업할 때는 표의 해당 브랜치를 사용한다. 후속 수정도 같은 PR에 추가하고 통합 확인 브랜치의 전체 변경을 개별 PR에 넣지 않는다.
- 기본 진입은 Bootstrap 씬이다. 테스트 실행 파일은 빌드 당시 코드이므로 Git 브랜치만 바꿔도 기존 exe는 바뀌지 않는다.

## 검증 범위

- 앞선 버그 항목 선택 9개 최종 통과, 메뉴/보상/슬롯 선택 12개 최종 통과, 건설창/보관함 EditMode 10개 + PlayMode 1개 최종 통과.
- #211: 실제 지상 씬 PlayMode 1개에서 선행 해금을 지키며 6회 구매한 결과, 구매 직후 상태·라벨·효과 갱신 통과. 실제 이어하기 슬롯·광산에서 복귀·화면 렌더링은 미검증이다.
- #211 검사 환경 준비 중 컴파일 참조·10초 로딩 제한·그래픽 없는 화면 크기 변경·수확량 선행 미구매를 수정했다. 최종 1개 검사만 재실행했으며 기존 통과 검사는 반복하지 않았다.
- 실제 실행 검사에서 TMP 폰트 에셋이 자동 변경돼, 작업 전 깨끗했던 원본으로 돌렸다. 폰트·설정·패키지·씬·프리팹 변경은 게시하지 않았다.
- 원시 검사 로그와 테스트 출력은 work_process/tools/ 아래 로컬 자료다. 실제 사용자 저장 파일은 사용하지 않았다. 전체 Demo/Windows 빌드/완전 종료·이어하기 검증을 새로 수행한 결과가 아니다.

## #211 상태와 다음 작업

- #211은 해결 완료가 아니다. 런타임을 추측 수정하지 않았으며 PR #233은 조사 검사만 담은 초안이다.
- 문제가 나는 구매 항목, 빌드/브랜치, 새 게임·이어하기·복귀 경로, 구매 직후 예외, 창 재오픈 결과를 확보해 특정 조건으로 다시 조사한다.
- 다음 비연출 구현 후보: #222 설정 음소거 토글. 원래 음량 복원, 슬라이더 조작 시 해제, 메인/게임 설정 상태 공유, 재실행 보존을 함께 처리한다.
- 이어서 #223 중 문구 '업그레이드' 통일과 심층 레벨·깊이 설정/표시 부분만 분리할 수 있다. 이미지·색 복귀·자물쇠 연출은 별도 범위다. #224 행성별 체계는 정식 버전 범위이므로 Demo 이후로 둔다.
