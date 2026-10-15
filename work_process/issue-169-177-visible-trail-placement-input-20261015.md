# #169 체력 잔상 표시 · #177 잘못된 설치 경고 후속 수정

## 기준과 범위

- 2026-10-15, Unity 6000.5.4f1.
- Git 루트 `C:/Users/jeone/sub-terra`, Unity 프로젝트는 하위 `sub-terra/`.
- 첫 번째 프로젝트 작업을 유지했다. 브랜치 `codex/new-game-random-seed`의 기존 #144/#148/#157 미커밋 변경을 보존했다.
- GitHub 확인 결과 #183은 이미 병합된 상태다. 원격 main을 fetch하고 현재 브랜치를 `e4c352e2`에서 `36826b94`로 fast-forward했다. 겹치는 BuildingMenuPresenter의 기존 표시명 한 줄을 보존 후 재적용했다. 새 커밋·푸시·PR은 수행하지 않았다.
- 사용자 기존 Clinic/Light/Settlement 에셋 3개를 변경하거나 정리하지 않았다.

## 보고된 현상과 근거

1. #183 체력 잔상이 보이지 않음.
   - 실제 코드에서 체력 감소량 전달과 0.35초 대기/0.4초 감소 로직은 연결되어 있다.
   - 잔상은 빨간 hp-bar.png를 복제하고 Image.color에 주황색을 지정했다. 기본 UI 셰이더의 색 곱셈으로 인해 붉은 원본에서 주황색을 만들 수 없어 빨간 본 체력바와 구별하기 어렵다.
   - 원본 hp-bar.png, bar-frame.png 및 BasicHUD.prefab의 Empty/Fill/Frame 순서를 확인했다. 실제 화면의 모든 원인을 재현·확정한 결과는 아니며, 색 표현 오류를 확인하여 고쳤다.
2. 설치 가능한 빈 공간에 클릭해도 ‘설치할 수 없는 지형입니다.’와 ‘자원이 부족합니다.’가 동시에 뜸.
   - 설치 브리지와 PlayerMiningController가 같은 좌클릭을 받는다.
   - MiningProgressHud는 설치 선택 중 NotMineable/InvalidTarget이라는 **채굴 실패**를 ‘설치 불가 지형’으로 치환했다. 타일이 없는 정상 설치 공간도 채굴 대상이 아니므로 잘못된 근거다.
   - 사진의 자원 부족 경고는 실제 비용 판정일 수 있다. 설치 가능 지형이라는 사실만으로 자원이 충족되는 것은 아니다.

## 수정

- `Scripts/App/UI/HUD/HudGaugeView.cs`
  - 원본 체력바는 투명 윤곽 마스크로만 사용하고, 안쪽에 독립적인 불투명 주황 단색을 표시한다. 원본 이미지의 붉은 RGB에 tint를 곱하지 않는다.
  - 잔상은 Empty 위, 즉시 갱신되는 Fill 아래, Frame 아래에 유지한다. 클릭을 가로채지 않는다.
  - 기존 0.35초 대기/0.4초 감소, 연속 피해·회복·최대 체력 변경·비활성 초기화 규칙을 유지한다. 전력바 잔상이나 체력 판정 변경 없음.
- `Scripts/Gameplay/Mining/PlayerMiningController.cs`
  - Integration이 주입하는 조회로 설치용 마우스 입력을 채굴에서 제외한다. Gameplay에서 App을 참조하지 않는다.
  - 설치에 사용한 누르기는 버튼을 놓을 때까지 소유권을 유지한다. 진행 중인 포인터 채굴도 취소하여 설치와 함께 채굴하지 않게 한다.
  - 방향키/Enter 채굴은 기존 경로를 유지한다.
- `Scripts/App/Integration/GameplayBuildingPlacementBridge.cs`
  - 설치 선택 중이거나 이번 프레임에 좌클릭 설치를 시도했다면 해당 포인터 입력을 소유한다. 성공으로 선택이 풀린 직후의 같은 클릭도 채굴로 재사용하지 않는다.
- `Scripts/App/Integration/IntegrationRuntimeBinder.cs`
  - 위 조회를 연결한다. deferredInputBehaviours가 이미 직렬화되어 조기 반환하는 씬에서도 입력 조회 연결이 생략되지 않게 한다.
- `Scripts/App/Integration/MiningProgressHud.cs`
  - 설치 중 빈 칸·보호 칸의 채굴 실패로 설치 불가 경고를 만들지 않는다. 잔여 mining.failure 항목도 해제한다.
  - 실제 설치 실패 안내는 기존 BuildingPlacementSystem → 브리지 → BuildingMenuPresenter 경로만 담당한다. 자원 부족·겹침·지면 부족·거리 초과 등 실제 판정은 완화하거나 복제하지 않았다.
  - 일반 채굴 및 화물/드릴/전력 등 다른 채굴 실패 안내를 유지한다.

## 최소 검증 결과

- 경량 C# 컴파일 성공: App, Gameplay.Player, Gameplay.Building, Gameplay.Mining, App.GameplayIntegration.
- 변경된 EditMode 검사 2개 파일과 기존 RecordingHudView 도우미 소스 컴파일 성공.
- 변경된 PlayMode 검사 파일 및 기존 UiPlayModeTestSupport 도우미 소스 컴파일 성공.
- 현재 DLL의 순수 관리형 검사 **4건 PASS**:
  - DamageTrail_HoldsThenFallsToCurrentHealth
  - DamageTrail_ConsecutiveDamageContinuesFromVisibleAmount
  - DamageTrail_HealingAndResetDoNotLeaveDamageSegment
  - MiningHud_NonMiningTargetsWhilePlacing_DoNotClaimPlacementIsInvalid
- 실제 프리팹을 복제해 주황색 독립 표시·마스크·레이어 순서·회복/최대치/비활성 초기화·전력 잔상 부재를 확인하는 EditMode 회귀 검사를 추가했다. 네이티브 Unity 실행은 하지 않았다.
- 기존 PlayMode 검사를 새 기대 동작으로 수정했다. 실제 설치 좌클릭에서 채굴 실패 이벤트가 발생하지 않는지도 확인하도록 보강했지만, 이 검사는 실행하지 않았다.
- diff 검사 통과. 씬/프리팹/이미지/설정/패키지/.meta 수정 없음. 빌드·전체 Demo 검사 없음.
- 이번 임시 컴파일 출력만 정리했다. Library·사용자 세이브·기존 임시 작업 폴더를 정리하지 않았다.

## 실제 화면에서 남은 확인

1. 첫 번째 프로젝트 `C:/Users/jeone/sub-terra/sub-terra`의 Bootstrap으로 시작한다.
2. 체력이 한 번에 눈에 띄게 줄어드는 피해를 받는다. 숫자와 빨간 바는 즉시 감소하고, 줄어든 구간에 주황색이 0.35초 남았다가 0.4초 동안 줄어야 한다. 이후 회복 시 잔상이 없어야 한다.
3. 자원이 부족한 시설을 정상 설치 공간에 클릭한다. ‘자원이 부족합니다.’만 나오고, 채굴 판정에서 만든 ‘설치할 수 없는 지형입니다.’가 추가되면 안 된다.
4. 자원이 충분한 경우 실제 설치 가능 위치에는 설치되고, 설치 성공 후 클릭을 계속 누르고 있어도 채굴을 시작하지 않아야 한다. 버튼을 놓고 다시 클릭하면 일반 채굴이 가능해야 한다.
5. 실제 겹침/지면 부족/범위 초과 위치는 각각 기존 설치 실패 이유가 나와야 한다. 실패 후 비용 차감·시설 생성이 없어야 한다.

#183은 닫힌 PR이므로 이번 변경은 별도 후속 PR로 전달한다.

## 공유 기준

- 선행 PR: **#189** (`codex/new-game-random-seed`, 커밋 `8155a36b`) — #144/#148/#157 변경.
- 이번 후속 브랜치: `codex/hud-trail-placement-followup`. PR의 검토 기준은 `codex/new-game-random-seed`로 잡아 체력/설치 후속 변경만 표시한다.
- 병합 순서: **#189 먼저 → 이번 후속 PR의 base를 main으로 맞춘 후 병합**. 팀원 PR을 자동 병합하지 않는다.
- 현재 첫 번째 Unity 프로젝트 폴더에는 선행 작업과 이번 후속 수정이 모두 포함된다. 사용자 기존 에셋·기존 임시 파일은 커밋에서 제외한다.
