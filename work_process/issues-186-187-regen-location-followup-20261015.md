# #186 자동 재생 중 피격 잔상 · #187 설치 위치/비용 분리

## 기준

- Git 루트 `C:/Users/jeone/sub-terra`, Unity 프로젝트 `C:/Users/jeone/sub-terra/sub-terra`, Unity 6000.5.4f1.
- 작업 시작 브랜치 `codex/hud-trail-placement-followup`, HEAD `06c85c1a` (PR #190). 게시 브랜치는 `codex/tile-health-placement-followup`이며 최신 main `28895053`을 fast-forward로 반영했다.
- PR #190의 주황 단색/윤곽 마스크, 설치 클릭 입력 소유권, 잘못된 채굴 경고 억제는 유지한다.
- 사용자 요청 범위는 #186의 자동 재생 대응과 #187의 위치/비용 분리다. #188은 제외했다.
- #190은 main에 병합되어 이번 후속 변경은 별도 PR로 게시한다. 기존 사용자 시설 에셋 및 줄바꿈만 다른 검사 파일은 커밋에서 제외한다.

## 변경 내용

### #186

`Scripts/App/UI/HUD/HudGaugeView.cs`의 HealthDamageTrail.Set은 조금이라도 회복하면 즉시 잔상을 초기화했다. 매 프레임 자동 재생이 발생하면 0.35초 대기 전에 잔상이 사라질 수 있었다.

- 현재 체력이 증가해도 표시 중인 잔상보다 낮으면 잔상과 기존 경과 시간을 유지한다.
- 회복량이 잔상을 따라잡거나 넘으면 현재 체력으로 즉시 정리한다.
- 감소 중 회복량 변화로 잔상이 다시 늘어나지 않도록 감소 결과를 이전 표시량 이하로 제한한다. 항상 현재 체력보다 낮아지지는 않는다.
- 연속 피해는 그때 보이는 잔상에서 다시 시작한다. 최대 체력 변경/비활성은 기존 Reset을 유지한다.
- 본 체력바 즉시 갱신, 0.35초 대기 + 0.4초 감소, 주황색/형태, 전력바 및 실제 체력 판정은 유지한다. 시간 연장/최소 폭 추가는 이번 범위가 아니다.

### #187

기존 CanPlaceAt은 자원 부족이면 위치 검사 전에 반환했다. 브리지는 CannotAfford를 위치 유효로 간주하므로 실제로는 지면이 없거나 다른 시설과 겹친 곳도 Valid 상태가 될 수 있었다.

- `Scripts/Gameplay/Building/BuildingPlacementSystem.cs`: 기존 지형/점유/엘리베이터/거리/구역/바닥 검사를 `CanPlaceLocationAt`으로 분리했다. 선택·정의 유효성은 공통 검사다.
- `CanPlaceAt`과 `TryPlaceAt`은 기존처럼 정의 확인 → 비용 확인 → 같은 위치 검사를 사용한다. C키 후보 검색도 비용 부족을 먼저 반환하는 기존 동작을 유지한다.
- `Scripts/App/Integration/GameplayBuildingPlacementBridge.cs`: 위치 유효 상태는 위치 전용 API로 결정한다. 미리보기의 최종 설치 가능 색은 위치와 비용을 모두 만족해야 한다. 정상 위치에서 자원 충족 여부가 바뀔 때 기존 안내도 갱신되도록 비용 사유를 유지한다.
- 비용 판정과 지형 계산을 UI에 복제하지 않았다. 조회에서 자원을 차감하거나 시설을 생성하지 않는다. 설치 실패 경고 우선순위와 #190의 채굴 입력 차단도 유지한다.

## 변경 파일

1. `Scripts/App/UI/HUD/HudGaugeView.cs`
2. `Scripts/Gameplay/Building/BuildingPlacementSystem.cs`
3. `Scripts/App/Integration/GameplayBuildingPlacementBridge.cs`
4. `Tests/EditMode/App/UI/PromptB106HudTests.cs`
5. `Tests/EditMode/Gameplay/Building/BuildingPlacementTests.cs`
6. 이 작업 기록

위 경로의 기준은 `sub-terra/Assets/_Project/`이며 이 문서는 `work_process/`에 있다.

## 최소 검사 결과

- 실제 asmdef 소유 소스를 사용한 경량 컴파일: App, Player, Building, Mining, App.GameplayIntegration 성공.
- 수정된 App/Building EditMode 검사 파일과 기존 RecordingHudView 도우미 소스 컴파일 성공.
- 현재 DLL에서 순수 관리형 체력 잔상 검사 **5건 PASS**:
  - 대기 후 감소
  - 연속 피해
  - 회복이 잔상을 따라잡을 때 정리 및 Reset
  - 대기 중 소량 재생에도 유지, 타이머 재시작 없음
  - 연속 재생 중 잔상 역방향 증가 없이 최초 피격부터 0.75초 안에 수렴
- Building 회귀 검사: 자원 부족 + 정상 2×2 위치 / 점유 칸 / 지면 부족 / 거리 초과를 구분하고, 최종 실패는 비용 부족 우선이며 차감/생성/선택 변경이 없는지 확인하는 소스를 추가했다. **네이티브 Unity에서 실행하지 않았다.**
- 기존 #190의 프리팹 검사도 소량 회복 시 잔상을 유지하는 새 기대값으로 수정했다. 이 프리팹 검사 역시 미실행이다.
- diff 검사 통과. 씬/프리팹/이미지/.meta/ProjectSettings/Packages 변경 없음. 전체 Demo/Windows 빌드/실제 화면은 실행하지 않았다.
- 임시 컴파일 출력만 정리한다. 기존 세이브/Library/다른 작업 파일은 정리하지 않는다.

## 팀원에게 전달할 기대 동작

1. 재생 업그레이드가 있어도 피해 직후 주황 잔상이 남고 감소한다. 큰 회복으로 실제 체력이 잔상을 따라잡으면 잔상이 정리된다.
2. 자원이 부족해도 실제 겹침/지면 부족/거리 초과 위치를 위치 유효로 오인하지 않는다. 정상 위치라도 자원이 부족하면 설치 가능 색이 되지는 않는다.
3. 실제 클릭/C키에서 자원이 부족하면 자원 부족을 우선 안내한다. 시설 생성·차감이 없어야 한다.
4. #190의 잘못된 지형 경고 중복과 설치 클릭 후 채굴 재사용이 다시 발생하면 안 된다.

구 #190 기록의 ‘회복 시 초기화’는 이번 변경으로 세분화되었다. **모든 회복에서 즉시 초기화하는 것이 아니라, 현재 체력이 표시 중인 잔상을 따라잡았을 때 정리한다.**


## 게시 전 최종 최소 확인

- 최신 main 반영 후 관련 런타임 9개 어셈블리와 변경 검사 소스 4개 어셈블리의 경량 컴파일 성공.
- 현재 컴파일한 소스에서 위 순수 관리형 잔상 검사 5건 재실행 PASS.
- 전체 Unity 테스트/실제 Canvas/설치 물리는 미실행이다. 컴파일 성공을 플레이 검증 완료로 해석하지 않는다.
- #168 무료 이동의 `CanAffordSelection` 예외는 별도 PR #168에 있다. 함께 합칠 때 이 예외를 유지하고, `CanPlaceLocationAt`의 위치 검사에도 이동 중 자기 점유 제외를 유지한다.
