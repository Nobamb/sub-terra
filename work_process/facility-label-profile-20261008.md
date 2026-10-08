# 시설 이름표 비용 분리 — 2026-10-08

## 이번 변경

첫 번째 Unity Hub 프로젝트 `C:/Users/jeone/sub-terra/sub-terra`에서 진행했다. 브랜치 `codex/ladder-miner-style`, 기준 커밋 `af5aa217`이다. 원격 main은 `0fdef13c`이며 현재 HEAD의 조상이 아니다. 이번 성능 조사에서는 main 병합과 커밋·푸시를 수행하지 않았다.

시설 이름표의 동작/외형/갱신 주기는 유지하고 CPU Profiler 측정 지점을 추가했다.

- `SubTerra.FacilityLabels.Refresh`: 준비, 검색, 후보 분류, 배치, 정리, 연출을 포함한 전체 Refresh.
- `SubTerra.FacilityLabels.Discovery`: 활성 BuildingInstance 전역 탐색만.
- `SubTerra.FacilityLabels.Layout`: 후보 이름표 생성/텍스트 설정, 앵커 계산, 겹침 판단, 위치 반영.
- `SubTerra.FacilityLabels.Anchor`: 본체 SpriteRenderer 선택과 bounds 기준 위치 계산. Layout 내부에 포함되므로 두 시간을 더하지 않는다. CCTV의 같은 앵커 계산도 포함될 수 있다.
- `SubTerra.FacilityLabels.Animation`: 기존 이름표 등장·퇴장·발광 갱신.

변경 파일은 `FacilityProximityLabelController.cs`, `FacilityNameTagAnchor.cs` 두 개다. 레지스트리/전역 캐시, 검색 간격 증가, 파편 감소, UI 품질 변경은 하지 않았다.

## 실제 설치 장면 측정

Unity 6000.5.4f1 일반 Editor Play, Direct3D 11, Game View 1180×491. 새 전용 QA 저장 경로로 Bootstrap → MainMenu → 새 슬롯 → SurfaceBase → Mine_Demo_Integration 순서로 진입했다. 사용자 실제 슬롯을 사용하지 않았다.

기존 BuildingPlacementSystem.TryPlaceAt과 설치 자원 규칙으로 충전기 설치를 시도했다. 지형/설치 판정을 통과한 설치는 **1개**이며 활성 BuildingInstance도 1개, 이름표도 1개다. 시설이 많은 장면을 검증한 결과로 해석하면 안 된다. 플레이어 위치를 (-38.5, -0.5)에 고정했고 합성 시설 객체나 합성 채굴 파편을 만들지 않았다. 안정화 3초 후 30초 동안 ProfilerRecorder와 프레임 간격을 읽었다.

| 항목 | 평균 |
| --- | ---: |
| 프레임 간격, 2,090개 | 14.363ms |
| 프레임 간격 p95 | 31.163ms |
| 이름표 전체 Refresh | 0.317619ms |
| 활성 시설 탐색 | 0.228896ms |
| 이름표 배치 | 0.024664ms |
| 앵커 계산, 배치에 포함 | 0.019292ms |
| 이름표 연출 갱신 | 0.004650ms |
| Canvas.BuildBatch, 장면 전체 | 0.047720ms |
| GC Allocated In Frame, Editor/장면 전체 | 약 30,663bytes |

시간 marker의 단위는 TimeNanoseconds이며 위 표는 ms로 변환했다. 각 recorder의 LastValue를 매 프레임 읽었다. Canvas.BuildBatch는 이름표 전용 시간이 아니다. Canvas.SendWillRenderCanvases recorder는 지원되지 않아 측정값이 없다. GC 수치 역시 이름표에만 귀속되는 할당량이 아니다. GPU 비용과 standalone Windows FPS는 이번 검사에서 측정하지 않았다. Editor 및 백그라운드 작업이 포함된 단일 표본이다.

이 장면에서 시설 탐색이 이름표 내부 평균 시간의 대부분이지만, 전체 프레임 지연의 주원인이라는 증거는 없다. 실제로 많은 시설과 채굴이 있는 장면의 전후 비교도 없으므로 검색 레지스트리나 시각 품질 축소를 적용하지 않았다.

## 검증 및 정리

- 제품 측정 marker 코드가 Unity에서 컴파일됐다.
- 기존 이름표 검사 3개를 Editor Play 검사 도구에서 직접 호출해 모두 통과했다: 표시/거리/버팀목·사다리 제외, NotoSansKR 폰트, 시설 비활성·재활성·파괴 후 이름표 정리.
- 전체 Test Runner XML 결과가 아니다.
- 첫 임시 도구는 이 버전에 없는 ProfilerCategory.Any 때문에 컴파일 실패했다. 범주를 Scripts/Render/Memory로 구분해 재실행한 결과 위 검사가 통과했다. 실패 코드는 Assets에서 제거했다.
- 임시 측정 컴포넌트 .cs/.meta를 Assets에서 제거하고 도구 원본은 work_process/tools/LabelCostProbe-20261008.cs에 보존했다.
- ProjectSettings/Packages/Assets/Settings 작업 전 스냅샷과 비교했다. 복원해야 할 변경은 없었다. 사용자 NotoSansKR 폰트 변경과 기존 사다리 테스트 변경은 보존했다.
- 새 Windows 빌드는 생성하지 않았다. 측정 후 Unity는 종료됐다. C: 남은 공간은 약 9.95GiB(10.68GB)다. 이번 Editor 실행으로 캐시가 늘었을 가능성은 있지만, Library 전체를 임의 삭제하지 않았다.

## 다음으로 이어갈 범위

1. PR #162/#163 상태 및 main 병합 여부를 정리한 뒤 같은 변경을 사용할 기준 브랜치를 확정한다.
2. 실제 플레이로 시설을 여러 개 설치한 장면에서 같은 marker를 30초 측정한다. 시설 수, 동시에 보이는 이름표 수, 카메라, Game View 해상도를 기록한다.
3. 실제 드릴 채굴 구간에서도 채굴 파편과 UI 비용을 함께 캡처한다. 이번 단일 충전기 측정으로 해당 구간 검증을 대체하지 않는다.
4. 특정 구간이 반복적으로 프레임 예산을 넘을 때만 작은 수정 하나를 적용하고 동일 장면에서 전후 비교한다.

원본 수치: `work_process/label-cost-measured-20261008.txt`.
## 2026-10-08 게시
이후 사용자 요청으로 Profiler marker 두 파일, 위 원본 수치, 이 기록과 데스크탑 인수인계를 PR #163 후속 커밋에 포함한다. 최신 main 자동 병합은 하지 않으며 선행 PR #162 → #163 순서를 유지한다.
