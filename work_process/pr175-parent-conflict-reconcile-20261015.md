# PR #175 부모 동기화·충돌 정리 — 2026-10-15

- 기존 #175 브랜치 `codex/demo-visual-issues-170-174`, 시작 원격 head `1cae911d`.
- #176은 팀원에 의해 #175에 이미 병합되어 있다. 이를 보존하고 최신 부모 #168 head `3ca4763e`를 일반 merge했다. main에 PR을 병합하거나 PR 대상을 바꾸지 않는다.
- 작업은 `C:/Users/jeone/sub-terra/.worktrees/pr175-conflict-reconcile`의 sparse 코드 폴더에서 진행한다. Unity 실행 프로젝트가 아니다. 첫 번째 프로젝트의 사용자 변경은 보존한다.

## 충돌 해결

1. ProgressionServiceTests.cs: #176의 드릴2·스캔2·가스저항1 조건 검사를 유지하고 부모의 시설명/영구 ID 검사도 남긴다.
2. BuildingMenuPresenterTests.cs: 부모의 설치 실패 시 선택 유지·자원 부족 우선·반복 경고·자원 차감 없음 기대값을 유지한다. 겹침 안내에 빈 공간을 고르는 해결 문구가 남는지도 확인한다. 실패 시 선택을 비우라는 과거 기대값은 제거한다.

기존 HUD·미션·건설창·사다리 상단 이탈·레일, #176 귀환 실패 복구, #178 보상/붕괴 잔재, 부모의 설치 클릭 보호를 보존한다. 씬/프리팹/이미지/설정/패키지는 수동 변경하지 않았다.

## 최소 확인

- 해당 폴더 실제 소스의 Shared / Player / Building / Mining / App / App.GameplayIntegration / 변경 App EditMode 검사 소스 경량 컴파일 성공.
- 순수 관리형 해금 미달 3건, 조건 충족/중복 해금/기존 저장 1건, 시설명/영구 ID 1건: 총 5건 PASS.
- BuildingMenuPresenterTests는 AssetDatabase가 필요한 검사여서 소스 컴파일만 확인했다. Unity 실제 화면/마우스/전체 Demo/Windows 재실행은 하지 않았다.
- diff·충돌 마커·미해결 인덱스 검사 후 기존 #175 브랜치로 커밋·푸시한다. 새 PR 생성이나 강제 푸시는 하지 않는다.

## 열린 PR 감사

실제 최신 대상 브랜치를 사용한 merge-tree 확인 결과 #175의 위 2개와 #181의 GameplayBuildingPlacementBridge 충돌을 발견했다. #168→main / #182→main / #180→기존 부모 / #192→main은 현재 충돌이 없다. main 감사 기준은 `d4f0e38f`다. #181 충돌은 해당 기존 브랜치에서 별도로 해결한다.

부모 브랜치에 변경이 추가되면 자식 PR의 충돌이 다시 생길 수 있다. 부모→자식 순서로 동기화하고 현재 대상 브랜치와의 충돌을 확인한다. 부모 병합 전에 대상을 main으로 임의 변경하거나 자식 수정으로 부모를 덮어쓰지 않는다. 자동 병합 가능은 실제 동작 검증 완료를 뜻하지 않는다.
