# Windows 성능·종료 경고 후속 조사 — 2026-10-08

## 기준과 범위

- Unity Hub 첫 번째 프로젝트: C:/Users/jeone/sub-terra/sub-terra.
- Unity 6000.5.4f1, 기준 4dabfe44, codex/facility-mining-profile. 조사 후 사용자 승인으로 판매창 수정과 검증 기록을 커밋·푸시하고 PR을 게시한다. 추가 병합은 하지 않는다.
- Windows x64 Development QA, Direct3D 11, AMD Radeon (TM) Graphics, 1280×720 창 실행. 임시 측정 도구는 명시적 인자에서만 실행하고 별도 QA 저장 경로를 사용한다.
- 설정·패키지·버전·게임 Scene/Prefab은 변경하지 않는다. 기존 사용자 폰트 변경은 보존한다.

## ComputeBuffer 경고 원인 분리

동일 빌드에서 게임 Scene을 로드하지 않은 빈 장면, 카메라만 있는 장면, 런타임에 Built-in으로 전환한 카메라 장면, Bootstrap→MainMenu, Bootstrap→MainMenu→SurfaceBase를 비교했다. 각 실행은 정상 종료 코드 0이다.

| 실행 | 종료 ComputeBuffer 경고 수 |
| --- | ---: |
| 빈 장면 | 1 |
| 빈 장면, 2D Animation fallback만 해제 | 0 |
| 카메라만 | 1 |
| Built-in 카메라만 (런타임 진단 전환) | 1 |
| 메인 메뉴 | 1 |
| 지상 기지 | 1 |

빈 장면에서 reflection으로 관찰한 값: GpuDeformationSystem.s_FallbackBuffer.IsValid=True, count=64, stride=64, DeformationManager.s_Instance 없음. 따라서 SpriteSkin이나 시설·채굴 기능이 없어도 공용 버퍼가 생성된다.

설치된 2D Animation 15.1.0의 Runtime/BatchedDeformation/GpuDeformationSystem.cs에서 CreateFallbackBuffer는 AfterSceneLoad에 등록돼 무조건 생성된다. ClearFallbackBuffer 호출은 변형 시스템 Cleanup에 있다. 이 장면은 DeformationManager가 만들어지지 않으므로 그 정리 경로에 들어가지 않는다. 빈 장면에서 이 버퍼만 ClearFallbackBuffer로 해제하는 한 변수 비교에서는 경고가 없어졌다. 이번 재현의 버퍼 소유자와 정리 누락 경로를 확인했다.

해제 비교는 임시 진단이며 제품 수정이 아니다. 패키지 private API에 의존하는 강제 해제, 패키지 파일 편집, 버전 변경, 경고 숨김은 제품에 적용하지 않는다. 실제 수정은 이 재현 근거로 패키지 정리 경로를 검토하는 별도 작업이다. 현재 게임의 종료 경고 자체는 남아 있다.

원본 로컬 근거: work_process/windows-profile-20261008/{empty,empty-release,camera,builtin-camera,menu,surface}/result.txt 및 player.log. 원본 로그에는 로컬 연결 정보가 있어 공유 시 관련 줄만 발췌한다.

## Windows 측정을 막은 판매창 수명주기 오류

첫 Windows 시설 측정에서 SurfaceBase→Mine_Demo_Integration 후 InventoryService.AddMineral이 파괴된 EconomyPanelView.EnsureListContent를 호출해 NullReferenceException으로 중단됐다. 이 실행은 성능 측정 성공으로 계산하지 않는다.

EconomyPanelBinder.BindTo는 비활성 판매창에서 Awake보다 먼저 호출될 수 있다. 이후 Awake가 Presenter를 무조건 새로 생성해, 먼저 연결된 Presenter와 이벤트 구독의 참조를 잃었다. 기존 ProgressionPanelBinder는 같은 순서를 이미 방어한다.

EconomyPanelBinder는 기존 Presenter를 보존하고 BindTo에서도 View를 먼저 찾도록 수정했다. UI 배치·시각 에셋·거래 규칙은 바꾸지 않는다.

새 PlayMode 회귀 검사 BindBeforeAwake_PreservesPresenterAndReleasesInventorySubscription을 일반 Editor Play에서 직접 호출해 통과했다. 실제 비활성 객체에서 BindTo→활성화/Awake→Presenter 동일성→Unbind→View 파괴→구리 추가까지 수행했다. 게시 준비에서 Test Runner가 남긴 economy-lifecycle-test-20261008.xml과 해당 Editor 로그를 다시 확인했다. 선택한 회귀 검사 1개가 Passed(실패 0, 테스트 자체 0.429592초)이며 로그에도 같은 결과 저장 경로가 기록돼 있다. 결과 XML이 없다는 이전 안내를 정정한다. 전체 PlayMode 테스트 통과를 의미하지 않는다.

## 자원 부족과 측정 해석

첫 빌드 준비 중 OS 여유 RAM 약 460MB, 디스크 약 390MB까지 줄었고 Unity 로그에 System.OutOfMemoryException/Could not allocate memory가 기록됐다. 이 실행은 컴파일 회귀나 성능 표본이 아니다.

이번 작업에서 시작한 Unity만 정리했다. Git에서 무시되는 재생성 가능한 Library/Bee와 PlayerDataCache 약 956MB를 정리했고 그래픽 초기화를 생략한 Editor 빌드로 전환했다. 중단된 Burst 해시 캐시에서 EndOfStreamException도 관찰해 해당 재생성 캐시를 제거했다. 실제 사용자 저장 파일과 소스·에셋을 삭제하지 않았다.

Windows 측정은 Editor가 종료된 상태에서 진행한다. 숨긴 창·제어된 플레이어·Development 빌드라는 조건을 명시하며, 수동 플레이 전체 또는 Release 성능으로 일반화하지 않는다. 시스템의 메모리 압박/페이징도 프레임 지연의 원인이 될 수 있어 함께 기록한다.

## 성능 결과 및 마무리

수정된 Windows 빌드는 성공했다(681,900,420bytes). 같은 출력 폴더를 갱신했으며 중복 빌드 폴더를 만들지 않았다. 같은 설치·카메라·채굴 조건으로 2회 실행했고 모두 종료 코드 0, 시설 6개 실제 설치, 실제 채굴 78개, 채굴 실패 None, 전력 100→22를 확인했다. 판매창 NullReferenceException은 두 실행 모두 없어졌다.

| 구간 | 평균 프레임 ms (1회 / 반복) | p95 프레임 ms | 최대 프레임 ms | 평균 CPU ms | 평균 GPU ms |
| --- | --- | --- | --- | --- | --- |
| 시설만 | 9.2534 / 8.6718 | 13.5674 / 11.1585 | 265.2127 / 64.4409 | 9.2617 / 8.6816 | 6.0271 / 5.8020 |
| 실제 채굴 | 12.0805 / 11.8556 | 19.7756 / 21.2074 | 100.9157 / 89.8118 | 12.0999 / 11.8697 | 7.4331 / 7.2653 |

- 각 구간 30초, 시설 표본 3,243 / 3,460프레임, 채굴 표본 2,484 / 2,531프레임.
- 33.333ms를 넘는 프레임: 시설 5 / 2개, 채굴 14 / 19개. 50ms 초과: 시설 4 / 1개, 채굴 7 / 4개.
- FrameTimingManager CPU/GPU 값을 위 표에 사용했다. GPU 타임스탬프 표본 수는 시설 3,088 / 3,374, 채굴 2,364 / 2,343이다. 다른 ProfilerRecorder의 원시 나노초 값과 구분한다.
- 이름표 전체 평균 비용은 시설 0.4631 / 0.4119ms, 채굴 0.6419 / 0.6257ms. GPU보다 CPU 프레임 시간이 길지만 이름표만으로 긴 지연을 설명할 수 없다.
- QA.Driver는 목표 선정·위치 제어·Physics2D.SyncTransforms와 실제 MiningSystem.TickMining 및 동기 이벤트를 함께 포함한다. 채굴 평균 약 0.21~0.22ms, 최대 약 74~81ms로, 이 값을 게임 채굴 코드의 순수 비용으로 해석할 수 없다.
- 첫 실행 시작 전 OS AvailableMBytes=1,191, PageReads/sec=3,837. 실행 중 확인에서는 AvailableMBytes=461, PageReads/sec=47. 순간 지연에는 시스템 페이징, 검사 도구, 게임 처리 모두 영향을 줄 수 있다. 개별 장기 지연의 원인을 확정하지 않는다.
- 평균만으로 60FPS가 항상 유지된다고 말하지 않는다. 다음에는 채굴 완료 프레임의 CPU 타임라인과 게임 이벤트·검사 위치 이동 비용을 분리한다. 특정 제품 병목이 확정되지 않아 시설 검색 주기·파편 수·렌더 품질을 임의로 낮추지 않았다.

원시 결과: work_process/windows-profile-measured-20261008.txt, windows-profile-repeat-measured-20261008.txt. 직접 실행 회귀 결과: work_process/economy-lifecycle-direct-result-20261008.txt. 프레임 CSV와 전체 실행 로그는 로컬 work_process/windows-profile-20261008에 보존한다.

## 최종 작업 트리와 인수인계

- 제품 변경: EconomyPanelBinder.cs의 Awake/BindTo Presenter 수명 보정. 검사 추가: EconomyPanelBinderLifecycleTests.cs 및 Unity 생성 .meta.
- 자동 변경된 ProjectSettings.asset과 UniversalRenderPipelineGlobalSettings.asset을 검사 전 바이트 스냅샷으로 복원했다. 기존 사용자 폰트 변경은 그대로 296 insertions/69 deletions이다.
- Assets의 임시 측정 코드·빌더·Prefab·빈 Scene와 관련 .meta를 제거했다. 저장소에 기존부터 추적되던 InitTestScene은 원상 유지한다. 진단 재현 소스는 work_process/tools에만 보존한다.
- WindowsProfileQA 빌드와 전용 QA 저장 폴더를 제거했다. 기존 저장 슬롯과 게임 Scene/Prefab에 변경을 남기지 않았다. 사용자 승인으로 이번 수정과 공유용 검증 기록을 게시하며, 병합은 팀원이 진행한다.
- 종료 경고 원인 분리는 완료했지만 패키지의 실제 수정은 적용하지 않았다. 성능 표본 수집과 판매창 오류 수정은 검증했으며, 세부 CPU 병목에 대한 최적화는 미적용이다.
