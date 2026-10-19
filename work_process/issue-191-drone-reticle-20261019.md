# #191 드론 조준점·카메라 초점 연출

## 기준과 병합 순서

- 2026-10-19 확인 당시 #204는 미병합이었다.
- 기반: codex/drone-scan-lifecycle-wave, 865d05e3 (main fa2a9db4 포함).
- 작업: codex/issue-191-drone-reticle. PR 비교 기준은 #204 브랜치다. #204를 먼저 main에 병합한 뒤 이 PR의 기준을 main으로 변경해 병합한다. 팀원 PR을 임의로 병합하지 않았다.
- #206 아이콘·#207 보상 창과 별도다. 두 UI PR은 이 브랜치에 포함하지 않는다.

## 구현

- 기존 사각형 표식을 투명한 중앙, 바깥 코너 브래킷 4개, 안쪽 대각선 4개의 조준점으로 변경했다. 런타임 Sprite를 한 번 생성해 재사용하며 별도 이미지·프리팹 배선이 필요 없다.
- 파동 도달 후 약 0.16초 동안 1.24배에서 1배로 초점을 맞추고 짧게 흰 섬광을 낸다. 수명 마지막 0.28초에는 짧은 섬광 후 확대·페이드한다.
- 드론 거리순으로 정렬하고 표식 시작 간격을 최소 0.012초로 두어 동시 섬광을 줄인다. #204의 확산 링과 대상 무효화·회수를 유지한다.
- 구리: 구리색, 철: 회색, 리튬: 청록색, 금만 있는 블록: 노란색. 자원+금은 자원색 조준점 바깥에 노란 강조를 더한다.
- 엔진 연료는 밝은 녹청색과 작은 입자 3개를 사용한다. 봉인 문양 탐지 종류는 유지해 기존 문양 표시와 연결된다.
- DroneScanTarget에 자원 ID·금 포함 여부를 전달한다. 다른 자원으로 교체된 타일도 이전 색 표식을 회수한다.
- 표식 풀을 재사용하며 색·노란 강조·연료 입자를 초기화하고 비활성화·파괴 시 정리한다. Light2D·패키지·씬·프리팹·세이브 형식은 추가/변경하지 않는다.
- 감지 반경, 업그레이드 해금, 30초 주기·10초 수명, 채굴 보상, 가스 판정은 유지한다.

## 변경 파일

- Scripts/Gameplay/Drone/DroneScanPulseView.cs
- Scripts/Gameplay/Drone/DroneScanTarget.cs
- Scripts/Gameplay/Drone/DroneSensor.cs
- Tests/EditMode/Gameplay/Drone/DroneSensorUpgradeTests.cs
- Tests/EditMode/Gameplay/Drone/SealedGlyphScanTests.cs

## 검증

- 첫 실행: 관련 두 EditMode fixture 18/18 통과, 실패·건너뜀 0, Unity 종료 코드 0.
- 검토 중 자원 ID가 null인 기존 금 블록이 빈 문자열과 다르게 판정될 여지를 보완하고, 해당 경우의 검사를 추가했다.
- 최종 실행: DroneSensorUpgradeTests + SealedGlyphScanTests 19/19 PASS, 실패·건너뜀 0, Unity 종료 코드 0. 결과는 work_process/tools/issue191-check-20261019/final/EditMode-results.xml에 있다(로컬 로그, 커밋 제외).
- 조준점 투명 중앙/코너/X, 초점·섬광·종료 확대, 자원색/금 강조/연료 입자 및 풀 재사용을 확인했다. 기존 월드 준비 게이트·파동·대상 회수·빈칸 활성 가스·봉인 문양 검사도 함께 통과했다.
- 기존 미사용 필드/구형 Unity API 관련 컴파일 경고는 유지했다. 설정·패키지·씬·프리팹 변경은 커밋하지 않는다.
- Windows 실제 화면·전체 Demo·성능 측정은 실행하지 않는다. 팀원은 스캔 해금 후 구리/철/리튬/금/연료를 비교하고, 표식 출현·초점·종료 연출을 확인한다.
- 첫 번째 프로젝트 C:/Users/jeone/sub-terra/sub-terra에서 위 작업 브랜치를 사용한다.
