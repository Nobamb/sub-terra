# 타일 표시 오인 · PR #168 main 충돌 정리 — 2026-10-15

## 작업 위치와 공유 상태

**두 작업이 서로 다른 작업 폴더에 있다. 커밋 시 아래 위치를 구분한다.**

| 작업 | 경로 | 브랜치 | 상태 |
|---|---|---|---|
| 타일/스캔 표시 | `C:/Users/jeone/sub-terra` (Unity는 그 아래 `sub-terra/`) | `codex/tile-health-placement-followup` | #186/#187과 함께 후속 PR로 게시 |
| #168 main 충돌 | `C:/Users/jeone/sub-terra/.worktrees/pr168-main-reconcile` | `codex/facility-move-runtime-followup` | 커밋·푸시 완료, GitHub 충돌 없음 (`3ca4763e`) |

#168 작업 폴더는 코드/관련 검사만 받은 sparse worktree다. **Unity Hub에 등록하거나 실행하는 프로젝트로 쓰지 않는다.** 새 Library·Unity 임포트 캐시를 만들지 않았다. 기존 첫 번째 프로젝트의 변경이나 사용자 에셋을 옮기지 않았다.

## 타일이 광석처럼 보이는 문제

- 현재 실행 폴더와 main에는 PR #182의 `8171a6aa`가 포함되지 않은 상태였다. #182는 열린 PR이다.
- 일반 암석/광석 교체 후에도 흔들림 복제본이 이전 Sprite를 유지하고, 이전 색/flags를 다른 타일에 복원할 수 있는 코드가 현재 폴더에 남아 있었다.
- 추가로 현재 스캔이 재사용 Point Light2D를 사용해 대상 주변 일반 암석의 색/밝기를 바꾸는 경로도 확인했다. 실제 제보 화면의 모든 원인을 확정한 것은 아니다.
- 이번에는 PR #182에서 해당 표시 수정만 현재 폴더에 적용했다. 단순히 구현이 있다는 답변에서 멈추지 않고 첫 번째 프로젝트에 실제 소스를 반영했다.

### 반영 범위

1. `Scripts/Gameplay/Structural/StructuralCrackOverlay.cs`
   - 복제본에 SourceTile 기록, 타일/스프라이트 변경 시 갱신.
   - 빈 칸이 되면 표시 회수. 다른 타일에 이전 색/flags를 복원하지 않음.
   - 제거 즉시 비활성화하여 Destroy 지연 중에도 이전 그림이 남지 않게 함.
2. `Scripts/Gameplay/DemoWorld/MineLayerTilemapGenerator.cs`
   - 새 지형을 배치하기 전에 이전 구조 표시/숨김 색 정리.
3. `Scripts/Gameplay/Drone/DroneScanPulseView.cs`
   - 주변 암석까지 물들이는 점광원을 대상 칸 내부의 얇은 표식으로 교체 (#149의 기존 PR #182 구현).
   - 감지 대상, 색, 주기/수명/센서 조건은 유지. 일반 타일의 Sprite/자원 종류를 바꾸지 않음.
4. 관련 검사 3개 파일
   - StructuralFeedbackTests: 기존 #182 검사 외에 위험 이벤트 재발행 없이 광석→일반 암석으로 바뀔 때도 Sprite/색이 정상화되는 사례 추가.
   - DroneSensorUpgradeTests / SealedGlyphScanTests: 점광원 대신 대상 표식을 사용하는 기존 #182 기대값 반영.

위 경로는 `sub-terra/Assets/_Project/` 기준이다. 원본 이미지/씬/프리팹을 수정하지 않았으며, 채굴 보상·가스·광산 분포·업그레이드 규칙은 바꾸지 않았다. #182의 #150 변경은 가져오지 않았다. PR #180의 흔들림 완화와 통합할 때 SourceTile/스프라이트 갱신 방어도 함께 보존해야 한다.

**광물 미지급 전체가 해결됐다고 판정한 것은 아니다.** 화면이 보여 주는 그림과 실제 타일/보상이 계속 다르면 다음 조사는 실제 채굴 타일 ID·보상 전달 경로다.

## #168 main 충돌

- 로컬 #168 브랜치 `42068539`를 원격 `746cad62`까지 fast-forward한 뒤 최신 main `f49ed0b9`를 별도 폴더에서 `--no-commit --no-ff`로 병합했다.
- 충돌 파일은 사용자 전달 내용과 같이 BuildingPlacementSystem.cs 하나였다.
- `CanPlaceAt()`의 정상 위치 검사 종료 부분을 `failure=None; return true;`로 정리했다.
- `CanAffordSelection()` 맨 앞에 `IsMoving`이면 성공을 반환하는 예외를 넣었다.
- 신규 설치/단축키의 기존 자원 부족 우선 안내를 보존한다. 이동은 자원이 부족하거나 지갑이 없어도 이 비용 검사에서 통과하며, 이후 지형·다른 시설·엘리베이터·거리 검사는 그대로 수행한다.
- 브리지의 기존 `IsMoving` 비용 예외도 유지되어 있다.
- BuildingPlacementTests의 기존 무료 이동 검사에 자원 부족/지갑 없음에서 CanPlaceAt와 TryFindBestPlacementCell 모두 통과하고, 막힌 칸은 여전히 거부되는 사례를 추가했다. 기존 신규 설치 자원 부족 검사는 유지했다.
- 충돌 마커/미해결 인덱스 항목 없음. 수동으로 ProjectSettings/Packages/씬/프리팹을 변경하지 않았다. main 병합에 포함된 원래 변경과 이번 충돌 해결을 구분해야 한다.
- **아직 병합 커밋/푸시하지 않아 GitHub #168의 충돌 표시는 남아 있을 수 있다.** `git add`는 완료했으며 해당 sparse worktree에서 병합 커밋을 만들고 기존 #168 브랜치로 푸시하면 된다. 현재 루트 폴더에서 커밋해도 #168 변경은 포함되지 않는다.
- 미커밋 #187의 위치/비용 분리 코드는 #168에 가져오지 않았다. 추후 통합할 때 이번 CanAffordSelection의 무료 이동 예외와 자기 점유 제외를 지워서는 안 된다.

## 최소 검사

- 현재 실행 폴더: Structural / DemoWorld / Drone 런타임 및 변경 Structural·Drone 검사 소스 경량 컴파일 성공.
- #168 sparse 폴더: 그 폴더의 실제 소스를 사용해 Shared / Player / Building / Mining / App / App.GameplayIntegration / 변경 BuildingPlacementTests 경량 컴파일 성공.
- 보강 검사에서 지역 변수명 중복 컴파일 오류를 발견해 수정 후 재컴파일 성공했다.
- 실제 NUnit/Tilemap/물리/Canvas/Unity 화면 검사는 실행하지 않았다. 타일 교체/삭제·광산 재생성·가림 구간에서 일반 암석 표시, 자원 0 이동/신규 설치의 실제 실행 판정은 팀원에게 남긴다.
- diff/충돌 검사 수행. 임시 컴파일 출력만 제거하고, #168의 미완료 병합 폴더는 보존한다.

## 다음 공유 시

1. 첫 번째 폴더의 #186/#187 및 타일/스캔 변경을 범위별로 커밋한다. #182에서 가져온 표시 코드가 중복 재구현이 아님을 PR에 적는다.
2. #168 sparse 폴더에서 병합을 커밋·푸시하여 기존 PR #168을 갱신한다. main 자체나 팀원 PR을 자동 병합하지 않는다.
3. #168 공유가 끝나기 전에 이 sparse worktree를 삭제/아카이브하지 않는다. 병합 중에는 브랜치 전환·재병합을 하지 않는다.


## #168 게시 완료 (2026-10-15)

- 사용자 승인 후 #168 전용 sparse 폴더에서 병합 커밋 `0d521a66`, `3ca4763e`를 생성하고 `origin/codex/facility-move-runtime-followup`으로 정상 푸시했다.
- 추가로 갱신된 main `28895053`(#189/#190 포함)을 통합했다. GameplayBuildingPlacementBridge 충돌은 무료 이동과 설치 클릭·채굴 분리를 모두 유지하여 해결했다. 이동 대상 선택 중에도 입력을 보호하며 표시 명칭은 main의 자원 정산기를 사용한다.
- GitHub API 확인: PR #168 head `3ca4763e2ec440bfb91d185963e59e850d96bb19`, `mergeable=true`, `mergeable_state=clean`. PR 자체를 main에 병합하지 않았다.
- 관련 Shared / Player / Building / Mining / App / App.GameplayIntegration 및 BuildingPlacementTests 소스 경량 컴파일 성공. Unity 실제 플레이 검사는 실행하지 않았다.
- PR 설명 추가 요청은 커넥터 오류로 반영되지 않았다. 코드 푸시와 충돌 해소 확인은 완료했다.
- 첫 번째 프로젝트 폴더의 #186/#187 및 타일/스캔 변경은 이번 #168 커밋에 포함하지 않고 그대로 보존했다. 위의 미커밋 병합 안내는 과거 상태이며, #168은 이제 게시 완료다.


## 타일/잔상 후속 게시 범위

- 최신 main `28895053` 반영 후 `codex/tile-health-placement-followup`에서 #186/#187과 타일/스캔 표시 변경을 두 커밋으로 나눠 별도 PR로 게시한다.
- #182는 아직 열려 있으며 타일/스캔 수정 일부가 이번 PR과 중복된다. #182의 업그레이드/해금 변경은 가져오지 않았다. 중복된 표시 수정을 새 작업으로 반복하지 않고 병합 시 양쪽 내용을 비교해야 한다.
- 이번 추가 검사는 관련 런타임 9개 및 변경 검사 소스 4개 어셈블리 경량 컴파일, 잔상 순수 관리형 검사 5건 PASS다. 실제 Tilemap 렌더링/프리팹/설치 물리는 미실행이다.
- 팀원 확인: 위험 표시가 있는 칸의 일반 암석↔광석 교체/삭제 및 재생성 후 그림이 남지 않는지, 드론 스캔이 주변 일반 암석을 물들이지 않는지, 자동 재생 중 피격 잔상이 유지되는지, 자원 부족일 때 위치 판정이 실제 지형을 따르는지 확인한다.
