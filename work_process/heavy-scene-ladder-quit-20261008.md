# 시설·채굴·사다리 후속 검사 — 2026-10-08

## 작업 기준 및 Git 상태

Unity Hub 첫 번째 프로젝트 C:/Users/jeone/sub-terra/sub-terra, Unity 6000.5.4f1. PR #162와 #163은 직전 사용자 요청에 따라 순차 병합됐고 현재 기준은 main dc2bc06c다. 이후 사용자가 병합은 팀원이 하도록 정정했으므로 추가 병합은 하지 않는다. 후속 브랜치는 codex/facility-mining-profile이다. 사용자 폰트 변경을 보존한다.

## 시설이 많은 장면과 실제 채굴

일반 Editor Play에서 영구 사다리 검사를 먼저 수행한 후 실제 BuildingPlacementSystem.TryPlaceAt으로 충전기 6개를 설치했다. 매 설치가 성공하면 선택이 해제되므로 다음 설치 전에 다시 선택한다. 이전 단일 충전기 검사 도구는 재선택하지 않아 1개만 설치됐던 것으로 확인했다. 게임 설치 규칙의 버그가 아니며 제품 코드는 수정하지 않았다.

격리된 새 Temp QA 슬롯에서만 실행했다. 설치용 구리 30개를 제공한 뒤 실제 설치 비용을 지불했다. 별도 가짜 BuildingInstance나 프리팹 직접 복제는 하지 않았다. 시설 수 6개, 안정화 시 활성 이름표 2개다. 카메라 따라가기를 끄고 orthographicSize=8, 중심=(마지막 시설 기준 x-4, y=-2)로 고정했다. Game View는 1180×491, vSync 끔/FPS 무제한이다.

시설만 표시하는 구간과 실제 채굴 구간을 각 30초 측정했다. 채굴은 실제 인게임 지형에서 빈 인접 칸을 고르고, 1.35m 거리 판정을 거친 TryStartMiningAtWorldPoint → 실제 deltaTime TickMining → TileMined/보상 트랜잭션 경로로 진행했다. 실제 진행·완료 이벤트를 받는 기존 MiningDebrisVisual을 사용했으며 파편을 임의 생성하지 않았다.

플레이어 입력과 Rigidbody 시뮬레이션은 검사 도구가 제어했고 목표 지점으로 위치를 옮겼다. 따라서 키보드/마우스로 완주한 수동 QA나 모든 물리가 포함된 최악 성능 검증은 아니다. 측정 중에는 에너지나 최대 전력을 변경하지 않았으며 실제 제거 78개, 전력 100→22, 채굴 실패 None이다.

| 항목 | 시설 6개 | 실제 채굴 |
| --- | ---: | ---: |
| 측정 프레임 수 | 1,698 | 2,103 |
| 평균 프레임 간격 | 17.670ms | 14.273ms |
| p95 프레임 간격 | 46.961ms | 20.651ms |
| 이름표 전체 평균 | 0.300373ms | 0.307074ms |
| 시설 탐색 평균 | 0.240048ms | 0.274150ms |
| 이름표 배치 평균 | 0.027868ms | 0.000415ms |
| 앵커 계산, 배치에 포함 | 0.021355ms | 0.000175ms |
| 이름표 연출 평균 | 0.004725ms | 0.002243ms |
| 장면 전체 Canvas.BuildBatch | 0.053199ms | 0.033457ms |
| 장면/Editor 전체 GC 할당 평균 | 31,407bytes | 32,103bytes |

채굴 중 플레이어가 이동하며 시설에서 멀어져 이름표 배치가 감소했다. 실행 순서·Editor 백그라운드·워밍업도 다르므로 채굴이 더 빠르다거나 제품 FPS가 개선됐다고 해석하지 않는다. Canvas는 이름표 전용 값이 아니며 GC도 이름표에만 귀속되지 않는다. GPU 및 standalone Windows 프레임은 이 표에서 측정하지 않았다. Canvas.SendWillRenderCanvases recorder는 지원되지 않았다.

프레임 지연의 주원인을 이름표로 확정할 근거가 부족하므로 전역 시설 레지스트리, 낮은 검색 주기, 파편 감소 등 제품 최적화는 적용하지 않았다. 원본: work_process/heavy-scene-measured-20261008.txt.

## 사다리 접점 판단

최신 코드에서 영구 Bootstrap_NewGame_ClimbUsesImportedBackFramesOnly 코루틴이 통과했다. 실제 Trigger 진입·물리 상승/하강·상단 정지·이탈 일반 이미지 복귀와 상승/하강 거리별 자세 순서, VisualRoot 위치 유지를 확인했다. 전체 Test Runner XML 결과는 아니다.

기존 중립 및 오른손 상승 확대 캡처를 다시 확인했다. 중립 자세에는 손/발과 가로대 사이에 작은 간격이 남고, 오른손 상승 자세는 레일/고정부 부근에 손이 닿는다. 이는 정확한 모든 자세의 접점 일치를 의미하지 않는다. 테스트에서는 캐릭터 전체가 튀는 문제가 재현되지 않았다. 한 자세를 맞추려고 공통 위치를 옮기면 다른 자세와 기존 발 기준이 달라지므로 이번에는 PNG/PPU/피벗/VisualRoot를 변경하지 않았다. 필요할 때는 중립 그림의 손·발만 미술 기준으로 좁게 수정한다.

기존 이름표 검사 3개도 직접 호출해 통과했다(표시·제외, 한글 폰트, 비활성·재활성·삭제 정리).

## Windows 종료 경고 조사

런타임 검사 컴포넌트나 Resources 검사 프리팹 없는 Windows Development QA 빌드 1개로 일반 종료와 할당 추적 종료를 검사한다. QA define은 별도 저장 경로 적용을 위한 것이며 실제 사용자 세이브를 사용하지 않는다.

프로젝트 Assets의 런타임 C#에서 ComputeBuffer/GraphicsBuffer 직접 생성은 발견하지 못했다. URP ShaderData/ConstantBuffer와 2D Animation GpuDeformationSystem에는 생성 코드가 있고 정상 Dispose/Cleanup도 존재한다. 따라서 경고 문장만으로 특정 패키지를 원인으로 단정하거나 임의의 전역 버퍼 해제를 추가하지 않는다. 패키지/Unity 버전과 설정은 바꾸지 않는다.

현재 URP의 GPU Resident Drawer mode는 0이다. 같은 경고를 포함한 공식 이슈 UUM-115886은 GPU Resident Batcher 관련이며 현재 프로젝트 버전보다 이전인 6000.5.3f1에서 수정됐다고 보고되어 있어, 이 프로젝트의 원인으로 그대로 적용할 수 없다.

참고: https://issuetracker-mig.prd.it.unity3d.com/issues/jobtempalloc-memory-leak-warning-is-thrown-when-the-player-is-shut-down
할당 추적 근거: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Shaders/ComputeBuffer.bindings.cs

종료 재현 결과는 아래에 추가한다.
### 최종 종료 검사 결과

- 검사 도구 없는 Windows Development QA 빌드가 성공했다(681,851,027bytes). 별도 새 슬롯으로 기존 -subterra-save-smoke new 경로를 실행해 SurfaceBase까지 진입·저장 후 Application.Quit으로 종료했다. 정상 종료 코드 0이지만 CodeReloadManager destroyed 뒤 ComputeBuffer finalizer 경고가 1회 재현됐다. 시설 설치/채굴을 하지 않은 실행이므로 해당 연출·이름표 검사만의 문제는 아니다.
- 같은 빌드를 CLI -native-leak-detection EnabledWithStackTrace로 실행해도 종료 코드 0, 같은 경고 1회이며 할당 위치는 없었다.
- 명시적 추적을 위한 임시 코드를 넣어 같은 폴더에 빌드를 덮어썼다(681,877,088bytes). RuntimeInitializeOnLoad에서 NativeLeakDetection.Mode를 EnabledWithStackTrace로 설정하고 활성 상태를 로그로 확인했다. 이 실행도 코드 0, 종료 후 같은 경고 1회이며 ComputeBuffer 할당 위치는 나오지 않았다.
- 정확한 버퍼/패키지 소유자는 **미확정**이다. 프로젝트 C# 직접 생성 부재 및 정상 빌드 재현을 확인했지만, 이를 곧바로 Unity 엔진 결함 또는 특정 URP/2D Animation 패키지 결함으로 단정하지 않는다. 패키지 자원 강제 해제·버전 변경·경고 숨김은 적용하지 않았다.
- 추가 원인 분리는 GPU 메모리 소유자/할당 스택을 포함한 최소 재현 프로젝트 또는 Unity 지원 분석이 필요하다. 이 경고를 해결 완료로 보고하지 않는다.

## 최종 정리 및 다음 작업

- 임시 Assets 측정 스크립트/메타, 누수 추적 스크립트/메타와 진단 Windows 빌드를 제거했다. 남아 있던 전용 QA 슬롯도 제거했다. 재현 소스는 work_process/tools에만 보존한다.
- 자동 변경된 UniversalRenderPipelineGlobalSettings.asset을 검사 전 바이트 스냅샷으로 복원했다. ProjectSettings/Packages/씬/프리팹 변경이 없다. 결과 기록을 제외한 게임 코드·설정·에셋의 남은 변경은 기존 사용자 폰트(296 insertions/69 deletions)뿐이다.
- 실행한 Unity/Player는 모두 종료됐다. 정리 후 C: 여유 14.63GB(약 13.62GiB). Library의 빌드/임포트 캐시는 임의로 삭제하지 않았다.
- 게임 동작이나 PNG를 바꾸는 후속 코드 수정은 없고 결과 기록만 새로 남긴다. 사용자 요청으로 결과 문서·측정 수치·빌드 결과·종료 로그 발췌를 codex/facility-mining-profile에 커밋/푸시한다. PR은 다음 작업 후 게시하며 추가 병합은 하지 않는다.
- 다음 범위는 정상 Windows 실행의 더 긴 실제 플레이 CPU/GPU 프로파일 또는 종료 경고의 최소 재현 분리다. 이번 Editor 표본만으로 FPS 병목이 해소됐다거나 모든 시각 접점이 완벽하다고 판단하지 않는다.

공유용 종료 근거: work_process/normal-quit-evidence-20261008.txt. 원본 로그는 로컬에 보존한다: work_process/normal-quit-baseline-20261008.log, normal-quit-trace-20261008.log, normal-quit-explicit-trace-20261008.log. 빌드 결과: work_process/normal-quit-build-20261008.txt, buffer-trace-build-20261008.txt.
