# PR #242 바지·무릎 연결 보완 — 2026-10-26

## 위치와 상태

- 저장소 `C:/Users/jeone/sub-terra`, Unity 프로젝트는 하위 `sub-terra/` (Hub 첫 번째 프로젝트), 시작 씬 Bootstrap.
- 브랜치 `codex/player-walk-two-leg-cycle`, 시작 커밋 `d71d51bc`. 기존 #242 후속 수정. 사용자 요청에 따라 이 변경을 같은 브랜치에 커밋·푸시하며 새 PR은 만들지 않는다.
- GitHub 조회 시 #242는 열려 있고 mergeable=false였다. 최신 main 통합/충돌 정리는 이번 시각 수정에 포함하지 않았다.

## 원인과 수정

사용자 GIF와 실제 PNG 16장을 비교했다. 원본 다리가 완전히 떨어진 두 조각인 것보다는, 원화 분할·변형 방식 때문에 잘라 붙인 것처럼 보이는 문제였다.

1. 고정 몸통 메시가 바지 아래까지 덮고, 그 아래에서 허벅지만 움직여 수평 절단선이 생겼다. 확대 비교로 확인했다. 고정 바지 영역을 공통 접합선까지 줄이고, 각 다리 메시를 바지 윗부분까지 확장했다. 접합선은 몸통과 같은 위치를 유지하다 허벅지 변형으로 점진적으로 이어진다.
2. 허벅지·종아리를 각각 회전시킨 위치의 평균은 굽힘 각도가 커질수록 관절 폭을 줄인다. 공통 무릎/발목을 중심으로 회전 각도를 보간하도록 변경했다.

기존 원화, 16장/16fps·1초 주기, 양발 교대·보폭·접지·팔 스윙을 유지했다. 변형 계산은 Editor에서 PNG를 굽는 데만 사용하며 런타임은 기존 SpriteRenderer 재생을 쓴다.

## 변경 파일

- `sub-terra/Assets/_Project/Editor/SmoothWalkCycleBaker.cs`
- `sub-terra/Assets/_Project/Tests/EditMode/App/Readiness/PlayerWalkCycleTests.cs`
- `sub-terra/Assets/_Project/Art/Characters/Player/Frames/Walk/walk_01.png` ~ `walk_16.png`
- `work_process/images/player-walk-knee-preview-20261026.gif`, `player-walk-knee-comparison-20261026.gif`, `player-walk-knee-poses-20261026.png`
- 이 기록, 기존 걷기 기록의 후속 링크, `init/rules/lessons.md`

Idle PNG·기존 .meta GUID·이동/점프/사다리 로직·씬·Packages·ProjectSettings의 내용 변경 없음. 빌더에서 프리팹/클립 저장은 수행했지만 내용 diff는 없다. Inspector 추가 연결 불필요.

## 확인

- 실제 출력 PNG의 16개 자세를 비교하고, 확대 이미지에서 바지의 수평 절단선이 사라진 것을 확인했다.
- [수정 전/후 GIF](images/player-walk-knee-comparison-20261026.gif): 왼쪽 PR #242 원본, 오른쪽 이번 결과. 실제 PNG를 같은 1초 주기로 포장했다.
- [새 걷기 GIF](images/player-walk-knee-preview-20261026.gif), [16개 자세](images/player-walk-knee-poses-20261026.png).
- 최종 EditMode **2/2 통과**, 실패/스킵 0, 0.90초. 양쪽 무릎 폭·허리 접점과 실제 프리팹의 16장 연결·고유 프레임·마지막 프레임·1초 반복을 검사했다.
- 결과: `work_process/tools/walk242-connected-final-20261026/EditMode-summary.txt`.
- 최종 굽기 중 기존 Idle→Walk 전환 검사도 통과. C# 컴파일 오류 없음. Unity 라이선스/연결 서비스 로그는 이번 수정 대상이 아니다.
- **미검증:** Bootstrap 실제 이동 중 좌우 걷기·정지 전환과 Windows exe 체감. 자동 검사와 PNG 비교를 실제 게임 품질 승인으로 간주하지 않는다.
- 수동 체크: Bootstrap → 탐사 → 좌우 걷기 → 정지/재출발. 무릎 위 수평 잘림, 다리 교차, 16→1 프레임 연결에서 새 튐이 없는지 확인한다.

## #249 작업 보존

- 앞선 지상 인벤토리 작업은 stash `3f7e30bdc7327cb42c981bd0ed21bc668c334483`에 경로를 지정해 보존했다. 원래 브랜치는 `codex/issue-249-surface-inventory`.
- 코드·프리팹·검사·새 파일·이미지·기록·lessons 변경을 포함한다. 기존 #211/#240 stash는 유지했다.
- 돌아갈 때 이번 걷기 변경을 먼저 보존하고 #249 브랜치에서 위 stash를 apply한다. 복원 확인 전 stash를 삭제하지 않는다. 이번 걷기 수정은 #242 브랜치로 전달한다. #249는 아직 stash에만 있으므로 다른 PC로 전달하려면 복원 후 별도 커밋·푸시가 필요하다.
