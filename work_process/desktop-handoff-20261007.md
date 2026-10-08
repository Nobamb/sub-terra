# 데스크탑 Codex 인수인계 — 2026-10-07

## 먼저 확인할 상태

- 저장소: `Nobamb/sub-terra`. 노트북 저장소 `C:/Users/jeone/sub-terra`, Unity 프로젝트는 그 아래 `sub-terra/`이다.
- Unity Hub에서 첫 번째 프로젝트를 사용한다. `.worktrees/`의 예전 프로젝트로 검수하지 않는다. 데스크탑 경로는 현지 실제 경로를 확인한다.
- Unity 6000.5.4f1. 브랜치 `codex/ladder-miner-style`, 기존 게시 커밋 `af5aa217`에 2026-10-08 후속 검사·측정 커밋을 추가한다.
- 기존 PR #163: https://github.com/Nobamb/sub-terra/pull/163
- #163은 `codex/facility-grounding-and-placement`에 쌓인 PR이다. PR #162를 먼저 병합하고 #163을 main 대상으로 바꾸는 순서를 권했다. **데스크탑에서 GitHub의 현재 병합 상태를 다시 확인한다.** 오늘 이후 상태를 추측하지 않는다.
- 최신 main을 새로 병합한 턴이 아니다. 데스크탑 시작 시 작업 트리부터 확인하고 fetch로 최신 상태를 파악한다. 미커밋 파일을 덮어쓰거나 main/기능 브랜치를 무조건 reset하지 않는다.

## 게시된 오늘 작업

`af5aa217`로 커밋·푸시하고 PR #163 설명을 갱신했다.

- 같은 씬에서 이어하기할 때 기존 씬에 복원하고 너무 일찍 성공을 반환하던 문제 수정: 새 씬 핸들로 교체될 때까지 기다린다.
- 별도 QA 슬롯에서 실제 2×2 전진기지 설치 → 저장 → 이어하기, 시설 ID/위치 및 네 칸 점유 복원을 확인했다.
- 시설 이름표 컨트롤러의 반복 HashSet/List 할당을 재사용하도록 바꿨다. Editor 표본의 관리 힙 순증가는 약 77% 줄었지만 정확한 GC.Alloc 또는 Windows FPS 개선률로 해석하지 않는다.
- 관련 저장/이름표 회귀 검사와 기록이 게시됐다.

## 2026-10-08 후속 커밋에 포함하는 작업

사용자 요청으로 사다리 검사 코드, 최종 캡처, Windows 측정 결과, 이름표 Profiler marker 및 측정 결과, 인수인계를 PR #163 후속 커밋에 포함한다. 제품 외형과 게임 규칙은 바꾸지 않는다. 데스크탑에서는 원격 codex/ladder-miner-style 브랜치를 받아 아래 파일을 확인한다.

게시하는 사다리 관련 파일(경로는 저장소 기준):

1. `sub-terra/Assets/_Project/Tests/PlayMode/Integration/MineDemo/LadderBackBootstrapPlayModeTests.cs`
2. `work_process/ladder-windows-followup-20261007.md`
3. `work_process/windows-profile-followup-20261007.txt`
4. `work_process/desktop-handoff-20261007.md` (이 파일)
5. 시각 참고: `work_process/images/ladder-contact-neutral-20261007.png`, `ladder-contact-right-20261007.png`, `ladder-contact-left-20261007.png`

위 파일과 FacilityProximityLabelController.cs, FacilityNameTagAnchor.cs, work_process/facility-label-profile-20261008.md, work_process/label-cost-measured-20261008.txt를 후속 커밋에 포함한다. 데스크탑에 같은 파일의 변경이 있다면 pull 전에 차이를 확인한다.

`NotoSansKR-Regular_SDF.asset`은 작업 전부터 있었던 사용자 수정이다. 이번 작업에 임의로 포함/복원하지 않는다. `.worktrees/`, `backups/`, 임시 캡처 폴더, 재현 도구, 실패한 중간 보고서를 일괄 추가하지 않는다.

## 후속 검사 결과

- 사다리: 다섯 자세를 거리별로 검사하고 실제 물리 등반을 따로 검사하도록 개선했다. 이탈 이미지도 고정 0.15초 대신 실제 일반 이미지 복귀를 최대 20초 안에 기다린다.
- 최종 정상 Editor Play에서 영구 테스트 코루틴이 통과했다. 전체 Test Runner XML 결과는 아니다. Windows에서도 진입·다섯 자세·상단 정지·이탈 복귀가 통과했다.
- 중립·좌우 상승 캡처를 직접 확인했다. 전체 VisualRoot가 튀는 문제는 재현되지 않았다. 중립 자세에서 손·발과 가로대 사이의 작은 간격은 남는다.
- Windows x64 QA 구분값을 적용한 Development 빌드에서 1280×720, vSync 끔, FPS 무제한으로 조건별 30초 측정했다. 시설만 106.3FPS, 시설+파편 92.4FPS, 시설+파편에서 이름표 비활성 진단 114.7FPS.
- 충전기 12개 인스턴스(일부는 카메라 밖), 검색용 시설 객체 128개, 물리 고정의 합성 장면이다. 실제 완주·최악 장면·데스크탑 FPS 보증이 아니다. 순차 표본이라 특정 메서드의 병목/최적화 효과를 확정하지 않는다.
- 제품의 파편 수·시설 품질을 낮추거나 추가 성능 수정은 하지 않았다.
- Windows 종료 때 ComputeBuffer 해제 경고가 있었다. 원인 및 기존 발생 여부는 미확인이다.
- 임시 Assets 검사 코드/프리팹과 진단 빌드는 삭제했다. 설정·Packages·씬·프리팹 실제 diff는 없다. 노트북 종료 시 C: 여유 약 12.9GiB.

## 다음 작업 순서

### 1. 작업 파일 및 브랜치 정리

후속 게시 파일이 데스크탑에 있는지 먼저 확인한다. PR #162/#163의 병합 상태와 최신 main을 확인한다. 기존 사용자 변경을 보존하고 적절한 기능 브랜치에서 시작한다. PR #163 후속 커밋까지 가져왔는지 확인한다.

### 2. 실제 플레이 장면의 이름표 비용 분리

Bootstrap → 새 게임/별도 QA 슬롯 이어하기 → 탐사로 시작한다. 실제 설치 시설과 채굴을 포함한 장면에서 카메라·해상도·시설 수를 고정하고 30초씩 측정한다. CPU Profiler에서 `FacilityProximityLabelController.Refresh`, 시설 탐색, 이름표 위치 계산, Canvas 갱신, 채굴 파편 비용을 따로 확인한다. CPU/GPU 및 GC를 기록한다. 이름표를 끈 FPS 차이만으로 시설 검색 하나를 원인으로 단정하지 않는다.

실제 프레임 부족과 특정 비용이 확인되면 가장 작은 변경을 하나만 적용하고 같은 조건으로 전후 비교한다. 실제 시설 생성·제거·비활성화·이어하기에서도 이름표가 즉시 맞는지 확인한다. 품질 축소나 전역 캐시/레지스트리 도입을 먼저 하지 않는다.

### 3. 사다리 미술 접점은 필요한 경우에만

데스크탑의 실제 게임 배율에서 최종 캡처와 비교해 중립/좌우 상승의 손·발 간격이 거슬리는지 확인한다. 필요하면 먼저 중립 그림 한 장 또는 시각 앵커만 좁게 조정한다. 크기·중심·윤곽선·헬멧/배낭 외형을 유지한다. 접점을 맞추려고 전체 캐릭터 위치를 매 프레임 흔들지 않는다. 진입/이탈/상단 제한과 저장된 플레이어 위치는 보존한다.

### 4. 종료 경고 재현

임시 진단 컴포넌트가 없는 정상 Windows QA 빌드에서도 종료 시 ComputeBuffer 경고가 생기는지 먼저 확인한다. 실제 소유자와 생성/해제 수명주기가 확인될 때만 수정한다.

## 작업 원칙

`AGENTS.md`, `init/rules`, 관련 작업 기록을 읽는다. ProjectSettings/Packages/Unity 버전은 바꾸지 않는다. 기존 builder/Editor API가 있는 씬/프리팹 YAML을 수동 편집하지 않는다. 사용자 세이브를 검사 도구로 덮어쓰지 않는다. `-subterra-save-root`는 UNITY_EDITOR 또는 프로젝트 QA/Development define이 있어야 적용된다. QA define 없이 테스트를 실행하지 않는다.

좁은 검사부터 실행한다. 임시 테스트 씬/코드/빌드를 게임에 남기지 않는다. 디스크를 확인하고 여러 QA 빌드/ZIP을 중복 생성하지 않는다. 커밋·푸시·PR 병합은 사용자 승인 범위에서만 수행한다.

## 데스크탑 Codex에 붙여 넣을 시작 요청

> `work_process/desktop-handoff-20261007.md`를 먼저 읽고 오늘 작업 상태부터 확인해줘. 노트북 후속 테스트/기록 및 이름표 측정 marker가 포함된 최신 기능 브랜치를 가져왔는지 파일 존재와 Git 상태를 먼저 확인하고, 기존 PR #162/#163 및 최신 main 상태를 확인해줘. 이후 실제 플레이 장면에서 시설 이름표 검색과 Canvas 비용을 분리해 측정하고, 병목이 확인된 부분만 최소 수정해줘. 사다리 중립 접점은 작은 간격만 남아 있으니 제품 위치/크기를 무리하게 바꾸지 말아줘. 저장과 사용자 폰트는 보존하고 임시 빌드는 한 개만 사용해줘.

## 2026-10-08 후속 진행
시설 이름표에 Refresh/Discovery/Layout/Anchor/Animation Profiler marker를 추가했다. 첫 번째 Unity 프로젝트에서 실제 충전기 1개 설치 장면을 30초 측정했고 기존 이름표 검사 3개를 직접 호출해 통과했다. 평균 전체 0.318ms, 시설 탐색 0.229ms. 시설 다수·실제 채굴·Windows 성능 검증은 이번 표본에 포함되지 않는다. 성능 병목 확정 없이 동작/품질 최적화는 추가하지 않았다. 상세 기록은 work_process/facility-label-profile-20261008.md, 원본은 work_process/label-cost-measured-20261008.txt. 제품 코드 두 파일과 이 기록도 2026-10-08 후속 게시 범위에 포함한다. 원격 main 0fdef13c는 현재 기능 브랜치에 병합되지 않았다. Assets 임시 검사 도구는 제거했고 사용자 폰트와 사다리 검사 변경은 보존했다.
