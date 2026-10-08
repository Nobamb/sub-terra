# 2026-10-07 사다리 크기 보정 · 전진기지 2×2 복구

작업 프로젝트는 Unity Hub 첫 번째 `C:\Users\jeone\sub-terra\sub-terra`이며, 브랜치는 `codex/ladder-miner-style`이다. 이전 사다리 외형 변경과 시설 PR #162의 작업을 보존한다. 사용자가 인게임 사다리 크기를 확인했고 커밋·푸시·PR 게시를 승인했다. 시설 PR #162는 아직 열려 있으므로 이번 PR은 `codex/facility-grounding-and-placement`를 기준으로 한다. #162를 먼저 병합하고 이번 PR의 main 대상 변경 여부를 확인한다.

## 사다리

`PlayerAnimationController`는 사다리 전환에서 Transform 크기를 변경하지 않는다. 기본 광부의 불투명 높이는 226px/256 PPU = 0.8828125m인데 새 등반 중립 자세는 957px/1254 PPU = 0.7631579m였다. 이미지 전체 캔버스 크기에 맞춘 배율 때문에 실제 광부가 약 14% 작아졌다.

새 5장의 공통 PPU를 1084로 맞춘다. 중립 자세 높이는 약 0.88284m이고, 나머지 4장도 헬멧 위부터 발끝까지 기본 광부와 0.015m 이내로 맞춘다. 중앙 피벗·그림·포즈 순서·이동 거리·물리·Collider는 유지한다. 원본 사다리 01~05의 PPU와 GUID는 변경하지 않는다.

`LadderMinerStyleBuilder.ApplyFiveFrames()`와 `SubTerra > Player > Apply Miner Style Ladder Five Frames`가 수정된 배율을 적용한다. `StyledLadderFrames_KeepStandingMinerHeightAndFootLevel`에서 각 PNG의 실제 불투명 픽셀을 읽어 평상시 높이·발 위치와 비교한다.

## 전진기지

- 신규 설치 영역: 2×2. 원본 `Art/Facilities/MVP/outpost_core_cartoon_v3.png`를 그대로 복구한다.
- 그림은 가로 1.8m·세로 1.78m 안에서 같은 비율로 확대하며, 실제 불투명 발끝이 설치 영역 아래 지면에 0.08m 겹치도록 정렬한다.
- 건설 메뉴 아이콘·설치 미리보기·설치 프리팹은 같은 원본 Sprite를 사용한다.
- 좁은 `OutpostCoreTall` 이미지로 덮어쓰던 런타임 교체를 해제한다. 기존 파일은 삭제하지 않는다.
- 새 2×2 시설은 네 칸 중 하나라도 막히면 비용을 차감하지 않고 설치를 거절한다.
- 기존 저장에 명시된 1×2 시설은 그대로 복원한다. 주변 시설로 강제 확장하지 않는다. 크기 정보 없는 원래 저장 및 새 2×2 저장은 2×2로 복원한다. 실제 사용자 세이브는 변경하지 않는다.

## 엘리베이터 옆 기본 코어 제거

통합 씬의 `OutpostCore_Demo`는 시작 전력 5를 공급하는 PowerNode이자 드론의 기지 거리 기준도 겸했다. 시설 그림만 없애고 보이지 않는 시설 인스턴스를 남기지 않는다. 그림·하위 비주얼·BuildingInstance를 제거하고, 고정 기지 전력 노드 `SurfaceBasePowerSupply`만 유지한다. 기존 Transform/PowerNode fileID·네트워크·드론 참조·위치는 보존한다. 따라서 근접 이름표나 E키 전진기지 상호작용은 없어지고 기존 전력·거리 계산은 유지된다.

전용 적용 메뉴는 `SubTerra > Facilities > Restore Outpost Two By Two And Remove Demo Core`이다. 이 메뉴는 전진기지 프리팹·설치 정의·아이콘·통합 씬만 저장한다. Play 중이거나 저장하지 않은 씬 수정이 있으면 적용하지 않는다. 다른 시설과 ProjectSettings·Packages는 변경하지 않는다.

## 검증

최종 상태: 사용자가 Unity를 종료한 뒤 첫 번째 프로젝트의 일반 Editor에서 빌더 적용을 완료했다. 새 5장의 PPU 1084, 전진기지 프리팹·아이콘·설치 정의 2×2, 통합 씬의 기본 코어 제거를 실제 에셋에 저장했다. 기존 사용자 폰트 변경은 보존했다.

검사 결과는 `ladder-outpost-verification-20261007.txt`에 기록했다. Unity 일반 Editor에서 관련 검사 메서드 16개를 직접 호출해 모두 통과했다. 전체 Test Runner XML·Windows 빌드·사용자 저장 슬롯 검증은 실행하지 않았다. 로그는 `sub-terra/Logs/ladder-outpost-focused-20261007.log`이다. 컴파일 13.94초, C# 컴파일 오류·게임 코드 예외·ShouldRunBehaviour 어설션은 없었다. 기존 Unity 계정/entitlement 경고는 있었으나 이번 일반 Editor 실행과 검사에는 영향을 주지 않았다.

- 새 사다리 5장의 실제 불투명 픽셀 높이·발 위치 및 Player 프리팹 5장 연결: 2개.
- 전진기지 네 칸 각각 막힐 때 설치 거절·비용 미차감: 4개.
- 크기 없는 원래 저장·명시된 1×2·새 2×2 복원: 3개.
- 실제 2×2 전진기지 설치 → JSON 직렬화 → 복원, ID·좌표·네 칸 점유 유지: 1개.
- 원본 그림 비율·불투명 발끝 접지(2×2 및 기존 1×2)·설치 데이터·메뉴/미리보기 그림 일치: 5개.
- 기본 코어 제거 및 시작 전력 5·네트워크·드론 Transform 참조 유지: 1개.

`images/ladder-size-20261007.png`는 같은 배율로 렌더한 평상시 광부와 등반 5장이며, 높이와 발 접점을 이미지로 확인했다. `images/outpost-two-by-two-20261007.png`는 복구한 2×2 원본 전진기지와 지면선이다. 렌더 종료 시 임시 Camera.targetTexture 해제 순서 경고가 있었으며, 그림 자체는 정상 생성·확인했다. 임시 검사 스크립트와 .meta는 제거했다.

씬 저장 과정에서 Unity가 기존 표시 캐시를 재직렬화했다. 제거된 기본 코어 구성 외에 미연결 prefab override가 정리됐고, UI 신규 기본 null 필드와 기존 경계 타일 Sprite/Color 캐시가 갱신됐다. 읽기 전용 비교에서 8개 Tilemap의 모든 셀·실제 Tile 인덱스·flags·행렬·오브젝트가 동일함을 확인했다. 경계 타일의 현재 원본 에셋 표시가 반영됐으며, 지형 배치·채굴 데이터는 변경하지 않았다. YAML을 수동 편집하지 않았다.

## 디스크 정리

시작 시 남은 공간은 약 15.06GiB였다. Unity 종료, 절대 경로, Git에서 제외된 캐시임을 확인한 뒤 예전 작업 폴더 `.worktrees/elevator-outpost-ride-followup/sub-terra/Library`만 제거했다(논리 파일 크기 약 3.67GiB). 소스·백업·작업 트리 자체·현재 첫 번째 프로젝트의 Library는 보존했다. 검사 종료 후 여유 공간은 약 18.85GiB다. 전체 빌드나 새 프로젝트 Library 생성을 하지 않았다.

## 변경 파일

- 사다리: `Editor/LadderMinerStyleBuilder.cs`, 새 LadderBack 5장 `.meta`, 기존 작업 Player 프리팹 연결.
- 전진기지: `Editor/DataValidation/PromptB49FacilityVisualBuilder.cs`, `Scripts/Gameplay/Building/FacilityGroundedVisual.cs`, `BuildingPlacementSystem.cs`, `Prefabs/Gameplay/Power/OutpostCore.prefab`, `Data/Buildings/Building_OutpostCore_Basic.asset`, `Data/Buildings/Placement/outpost_core_basicPlacement.asset`, `Scenes/App/Mine_Demo_Integration.unity`.
- 관련 검사: `PhaseCTraversalStaticTests.cs`, `PromptB49FacilityVisualTests.cs`, `BuildingPlacementTests.cs`, `WorldSnapshotSystemTests.cs`, `LadderBackBootstrapPlayModeTests.cs`.
- 작업 기록·검사 결과·비교 PNG 2개.

ProjectSettings·Packages 변경 없음. 원본 PNG·기존 GUID 보존. 임시 검사 파일 정리 및 Git 상태·diff 검사 완료. 기능/에셋 변경과 통합 씬 저장은 별도 커밋으로 게시한다. 기존 사용자 폰트·개인 백업·임시 QA 폴더는 커밋 범위에서 제외한다.

사용자 확인은 첫 번째 프로젝트의 Bootstrap에서 탐사 후 평상시 → 사다리 크기 변화, 기본 코어 제거, 새 전진기지 2×2 설치 및 접지 상태를 비교한다. 기존 좁은 저장 시설의 크기는 호환성 때문에 그대로 유지한다.

## 다음 작업

1. 팀원은 #162 → 이번 PR 순서로 병합하고 첫 번째 프로젝트의 최신 main에서 Bootstrap부터 실행한다.
2. 실제 저장 슬롯에서 신규 전진기지 2×2 설치 → 저장 → 이어하기를 한 번 확인한다. 기존 1×2 시설은 그대로 유지되는 것이 정상이다. 네 칸 막힘 및 엘리베이터 이동 공간 거부 시 비용 차감이 없는지도 확인한다. 새 Windows 빌드/전체 테스트는 필요가 확인될 때만 진행한다.
3. 다음 시각 보완은 사다리의 손·발 가로대 접점과 진입/이탈 연결이다. 현재 승인된 크기·외형은 유지하고 실제로 튀는 위치나 프레임만 좁게 보정한다.
4. 이후 채굴 파편·블록 모서리·시설 표시가 많은 장면에서 프레임을 측정하고 비용이 큰 연출만 최적화한다. 근거 없이 전체 시설 그림을 다시 제작하지 않는다.
