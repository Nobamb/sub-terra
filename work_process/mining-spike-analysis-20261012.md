# 채굴 순간 지연 — 기존 자료의 추가 분석

## 실제 분석한 자료와 한계

2026-10-08 Windows Development 빌드의 heavy-fixed/heavy-repeat 각 30초 시설/채굴 CSV 네 개를 다시 분석했다. 이 CSV에는 frame과 interval_ms만 있다. 마커의 CPU/GPU 비용은 별도 집계 자료에만 있어 개별 긴 프레임과 특정 함수 비용을 대응시킬 수 없다. 2026-10-12의 새 main 성능을 측정한 자료가 아니다.

| 실행/구간 | 프레임 수 | p99 ms | 33.333ms 초과 | 50ms 초과 | 최대 ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| heavy-fixed 시설 | 3243 | 17.6009 | 5 | 4 | 265.2127 |
| heavy-fixed 채굴 | 2484 | 26.2133 | 14 | 7 | 100.9157 |
| heavy-repeat 시설 | 3460 | 13.8053 | 2 | 1 | 64.4409 |
| heavy-repeat 채굴 | 2531 | 31.2353 | 19 | 4 | 89.8118 |

p99는 정렬된 표본의 floor((n−1)×0.99) 위치 값이다. 채굴의 드문 긴 프레임이 두 실행에 남지만 시설만 있는 구간에도 더 긴 지연이 있었다. 채굴 전용 코드만 원인이라고 단정할 수 없다.

기존 집계에서 QA.Driver 최대는 약 74~81ms였으나 목표 탐색·위치 제어·Physics2D.SyncTransforms와 실제 TickMining을 함께 포함한다. 또한 OS 메모리 압박과 페이징이 관찰됐으므로 이 최대값을 게임 채굴의 순수 비용으로 해석하지 않는다. 이름표 평균 약 0.4~0.6ms만으로 90~100ms 지연을 설명할 수도 없다.

## 이번 준비와 실행 상태

- 기존 제품 ProfilerMarker로 Tick/완료/거래/타일 제거/후속 이벤트를 구분한다.
- 별도 Editor 진단 도구에 프레임 번호·간격·성공 채굴 수·구간별/콜백별 ns 값을 같은 CSV 행에 남기는 기능을 추가했다. 수집 불가는 NA로 구분하고, 이전 프레임 값이 남지 않도록 recorder 샘플을 비운다.
- 측정 중 문자열 생성/파일 쓰기를 피하도록 고정 크기 숫자 버퍼를 사용한다. 저장소 경로는 Application.dataPath 기준으로 계산해 노트북 절대 경로에 의존하지 않는다.
- 진단 코드 경량 컴파일은 종료 코드 0. 이번에 전체 씬을 실행하거나 새 성능 표본을 수집하지 않았다. 특정 병목 확정/게임 최적화/개선률은 아직 없다.
- 다음에는 여유 있는 데스크탑에서 CSV와 CPU Timeline을 함께 수집하고, 한 가지 병목이 확인됐을 때만 수정 전후를 같은 환경에서 비교한다. 세부 실행·정리 지침은 desktop-handoff-20261012.md를 따른다.

참고: ProfilerRecorder의 프레임 합산 방식은 Unity의 SumAllSamplesInFrame 옵션을 따른다. https://docs.unity3d.com/ja/current/ScriptReference/Unity.Profiling.ProfilerRecorderOptions.html
