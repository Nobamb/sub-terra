# 전진기지 저장·사다리·시설/채굴 성능 확인 — 2026-10-07

## 작업 위치와 범위

- 저장소: `C:/Users/jeone/sub-terra`
- Unity 프로젝트: `C:/Users/jeone/sub-terra/sub-terra` — Unity Hub 첫 번째 프로젝트.
- 브랜치: `codex/ladder-miner-style`. 이번 변경은 기존 PR #163의 후속 커밋으로 정리한다.
- 기존 사용자 저장을 읽거나 덮어쓰지 않고, 실행 인자 `-subterra-save-root`로 프로젝트 `Temp` 하위의 새 QA 저장 경로를 사용했다.
- Unity 버전·Packages·ProjectSettings·씬·프리팹은 변경하지 않았다. 기존 사용자 수정 폰트도 보존한다.

## 발견한 저장 문제와 수정

`SaveRuntimeController.ContinueRoutine`은 `LoadScene` 호출 후 활성 씬 **이름**만 확인했다. 현재 광산에서 같은 광산 저장을 불러오면 이름이 이미 같아서, 아직 교체되지 않은 씬의 월드에 복원하고 성공을 반환했다. 다음 프레임의 씬 로드가 기존 캐릭터·시설을 제거할 수 있었다.

이름과 함께 씬 핸들이 바뀌었는지 기다리도록 수정했다. 저장 형식은 변경하지 않는다.

최종 실제 Play 검사에서 확인한 것:

1. 실제 건설 정의와 비용으로 2×2 전진기지 설치. QA 설치 셀 `(-13, -1, 0)`.
2. 실제 슬롯 파일 저장 후 `BeginContinue` 실행.
3. 이전 씬과 다른 씬 핸들, 이전 시설과 다른 런타임 EntityId 확인.
4. 같은 시설 ID와 위치로 복원되는지 확인.
5. 네 점유 칸에서 재설치가 거부되는지 확인.

**중간 실행의 저장 통과 기록은 씬 교체 전 판정이었으므로 최종 검증 근거로 쓰지 않는다.** 최종 근거는 `runtime-profile-final-measured-20261007.txt`와 `sub-terra/Logs/runtime-profile-final-compiled-20261007.log`이다.

`SaveContinuePlayModeTests.RuntimeSameSceneContinue_WaitsForReplacementScene`을 추가해 같은 지상 기지 씬을 이어하기하는 경우에도 성공 콜백이 새 씬 로드 전에 반환되지 않도록 검사한다. 이 테스트는 사용자 저장 보호를 위해 프로젝트 Temp 안의 전용 저장 경로 실행 인자가 있어야 한다.

이 영구 PlayMode 회귀 테스트도 실제 Play 코루틴으로 실행해 통과했다. 결과는 `runtime-qa-20261007.txt`와 `sub-terra/Logs/runtime-save-regression-20261007.log`에 남겼다. 전체 Test Runner XML을 생성한 실행은 아니다.

## 사다리 결과와 제한

기존 `LadderBackBootstrapPlayModeTests.Bootstrap_NewGame_ClimbUsesImportedBackFramesOnly`의 코루틴을 실제 Play에서 실행했다. 첫 실행은 다섯 자세, 상승·하강, 정지, 상단 제한, 이탈 후 일반 자세 복귀, 위에서 낙하 진입 후 정상 등반을 통과했다. VisualRoot의 위치가 등반 중 바뀌지 않는 것도 확인했다.

재실행 한 번은 다섯 자세 중 네 자세만 관측돼 실패했다. 실제 렌더 프레임을 수집하는 이 검사는 느린 Editor에서 자세를 건너뛰어 관측할 수 있다. 이를 전체 테스트 통과로 보고하지 않는다. 자세 유효성·진입/이탈에서 추가 위치 보정이 필요한 확정 증거는 찾지 못했으며, 사다리 아트와 이동 코드는 이번에 바꾸지 않았다.

**손·발이 가로대에 닿는 세부 미술적 접점은 화면 검수가 남아 있다.** 코루틴 결과를 픽셀 단위 시각 검수 완료로 해석하지 않는다.

## 측정과 최적화

시설 이름표 컨트롤러가 매 프레임 생성하던 두 HashSet과 임시 List를 재사용한다. 씬 전환·비활성화 때 참조도 비운다. 시설 검색 배열과 이름표 애니메이션은 유지했다.

128개 추가 시설 객체, 30회 준비 후 Refresh 1,000회. `Profiler.GetMonoUsedSizeLong`으로 측정한 관리 메모리 순증가:

| 조건 | 관리 메모리 순증가 | GC 수집 |
| --- | ---: | ---: |
| 수정 전 표본 | 10,784,768 bytes | 0 |
| 수정 후 표본 | 2,514,944 bytes | 0 |

관측된 순증가는 약 77% 줄었다. 이는 정확한 GC.Alloc 카운터가 아니라 관리 힙 순증가다. Unity Mono의 `GetAllocatedBytesForCurrentThread`는 이 실행에서 0을 반환했고, Editor에서는 GC 비활성화 모드를 지원하지 않아 해당 중간 측정은 폐기했다.

Refresh 1,000회 CPU 시간은 수정 전 416.32ms/4,132.23ms 등 편차가 크고 수정 후 538.49ms였으므로 CPU 속도 향상 비율을 주장하지 않는다.

최종 Editor Play 프레임 표본은 Stopwatch로 연속 프레임 간격을 측정했다. 충전기 프리팹 12개와 시설 검색용 객체 128개를 추가했으며, 카메라·시설 표시·채굴 파편을 실행했다. 측정 중 캐릭터 물리와 시험 시설 충돌체를 끄고 설치·저장 검사는 그 전에 끝냈다.

| 장면 | 표본 | 평균 | p95 | 평균 FPS 환산 |
| --- | ---: | ---: | ---: | ---: |
| 시설 표시 | 222 frames / 약 5초 | 22.63ms | 29.03ms | 약 44.2 |
| 시설 + 채굴 파편 14개/100ms | 178 frames / 약 5초 | 28.14ms | 35.52ms | 약 35.5 |

채굴 파편은 기존 64개 풀을 재사용한다. 파편 개수를 임의로 낮추거나 시설/미술 품질을 줄이지 않았다. 숨긴 Editor의 짧은 표본이며 Windows 실행 빌드 FPS나 GPU 병목 확정 결과가 아니다. 변경 전후 같은 완주 장면의 FPS 향상을 입증한 것은 아니다.

## 회귀 검사

정상 Editor에서 다음 세 EditMode 테스트 메서드를 직접 실행해 통과했다. 전체 Test Runner XML 결과가 아니다.

- `PromptB49_ProximityLabel_ShowsNameExceptSupportAndLadder`
- `PromptB49_ProximityLabel_UsesNotoSansKoreanFont`
- `ProximityLabel_RepeatedRefreshDropsDestroyedAndDisabledFacilities`

원점에 검사 시설을 만들던 기존 이름표 테스트는 수정 전 코드에서도 실패했다. 전역 시설 탐색의 주변 객체 영향을 피하도록 시험 시설과 플레이어를 `(1000,1000)`으로 함께 옮겼고, 이름표 표시·비활성화·삭제·재활성화 검사가 통과했다.

최종 컴파일 로그에 CS 오류가 없고 변경 소스의 `git diff --check`도 통과했다. 임시 실행 스크립트와 `.meta`는 Assets에서 제거했다. 작업 전 폰트 파일과 종료 후 폰트 파일의 해시가 같아 사용자 폰트 변경이 보존됐음을 확인했다.

변경 소스:

- `sub-terra/Assets/_Project/Scripts/App/Save/SaveRuntimeController.cs`
- `sub-terra/Assets/_Project/Scripts/App/Integration/FacilityProximityLabelController.cs`
- `sub-terra/Assets/_Project/Tests/EditMode/App/UI/PromptB49FacilityVisualTests.cs`
- `sub-terra/Assets/_Project/Tests/PlayMode/Integration/Save/SaveContinuePlayModeTests.cs`

Inspector에 새 참조를 연결할 필요는 없다. 이번 저장 문제 수정은 기존 슬롯/시설 데이터에도 적용된다.

## 데스크탑에서 이어서 할 일

1. Bootstrap에서 새 게임/이어하기 후 설치된 전진기지를 확인하고, 사다리 손·발 접점과 경계 진입/이탈을 눈으로 비교한다.
2. 실제 Windows 실행 빌드에서 같은 광산·카메라 위치·시설 수를 유지한 30초 이상 프레임 표본을 얻는다. Editor 수치를 목표 FPS로 확정하지 않는다.
3. 필요한 경우 Profiler로 CPU/GPU·Canvas·시설 검색·파편 비용을 분리한 뒤 다음 최적화를 선택한다.
4. 기존 PR #163은 PR #162에 쌓인 브랜치이므로 #162를 먼저 병합한 뒤 #163의 대상을 main으로 바꿔 최종 차이를 확인한다.

검사를 위한 임시 Unity 스크립트는 Assets에서 제거했다. 로컬 재현 도구와 복구 백업은 이번 PR에 포함하지 않으며, PR에는 영구 회귀 테스트와 최종 검증 기록을 포함한다. QA 종료 대기 때문에 발생한 씬 복구 백업은 로컬 `backups/runtime-qa-recovery*-20261007`에 보존했다.
