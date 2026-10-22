# #236 보관함 전체 꺼내기·하단 버튼 배치 — 2026-10-22

## 작업 기준

- 이슈: https://github.com/Nobamb/sub-terra/issues/236
- 최신 main: 1b18513a. 독립 작업 브랜치: codex/issue-236-storage-balanced-withdraw.
- 검사 후 첫 Hub 프로젝트는 codex/nonvisual-review-20261021-latest로 돌려, 기존 #225·#237과 이번 미커밋 #236 변경을 함께 확인하도록 한다. 게시 시에는 독립 #236 브랜치로 전환해 아래 5개 파일만 커밋한다.
- Git 저장소 C:/Users/jeone/sub-terra, Unity 프로젝트 C:/Users/jeone/sub-terra/sub-terra (Hub 첫 번째), Unity 6000.5.4f1.
- #211에는 새 재현 댓글이 없어 보류한다. #225·#237은 열린 PR이므로 반복 구현하지 않는다. #223은 main의 팀원 구현을 유지한다.

## 원인·변경

- 기존 TryTransferAll(false)는 inventory.CanFitExact(items)가 false면 상태를 전혀 바꾸지 않았다. 보관량이 화물 한도보다 크면 빈 화물칸에서도 전체 꺼내기가 거부되는 원인이다.
- 보관량 내림차순 → 같은 수량이면 카탈로그 가치 내림차순 → 동률은 ID 순으로 처리한다. 현재 데이터에서는 엔진 연료 > 리튬 > 철 > 구리 순이다. 이미 들고 있던 종류별 수량을 균등 배분 기준으로 사용하지 않는다.
- 한 바퀴에 자원당 1개씩 꺼내는 결과를 완전한 바퀴 단위로 계산한다. 작은 보관량이 소진되면 나머지만 계속 처리한다. 안 들어가는 무거운 자원은 건너뛰고 가벼운 자원을 계속 처리한다. 수십억 개가 저장돼도 개수만큼 반복하지 않는다.
- 수량 계획이 끝나면 기존 InventoryService.TryAddManyExact로 한 번만 반영하고 보관량을 차감한다. 일부 수용은 성공으로 처리하고 `화물 공간이 부족해 일부만 꺼냈습니다.`를 표시한다. 아무것도 담을 수 없을 때만 실패한다.
- 전체 보관, 단일 자원 선택 수량 이동, 비용·저장 ID·저장 스키마는 유지한다. 기존 수량 오버플로 검사와 인벤토리의 용량 계산 경로를 사용한다.
- 하단 수량 영역 높이 136→164px, 선택 수량 버튼 높이 88→52px. 바로 아래에 같은 폭·높이의 전체 보관/꺼내기를 배치한다. 보관은 왼쪽, 꺼내기는 오른쪽으로 두 줄을 맞춘다. 검색창 아래의 작은 전체 버튼을 제거한다. 팝업 전체 크기와 연출은 유지한다.

## 변경 파일

- Scripts/App/Outpost/OutpostService.cs
- Scripts/App/UI/Outpost/StoragePopupView.cs — 런타임 UI 생성 코드. 씬/프리팹 재생성 없음.
- Tests/EditMode/App/Outpost/OutpostServiceTests.cs
- Tests/PlayMode/Integration/Outpost/PromptB138StoragePopupPlayModeTests.cs
- 이 기록. 경로는 Unity 프로젝트의 Assets/_Project/ 기준이다.

## 최소 검사

- 170kg 예시: 구리 74, 철 125, 리튬 6 → 구리 47, 철 47, 리튬 6 = 169.3kg.
- 적은 수량 소진 후 분배, 기존 화물을 제외한 새 이동량 균등화, 동률 가치 우선, 무거운 자원 건너뛰기, 화물 가득 참, 수량/오버플로/자원 보존.
- 변경 이벤트는 성공한 이동당 1회, 더 담을 수 없는 상태에서 재클릭해도 수량 불변. 부분 이동 결과의 메모리 내 저장/직렬화/복원 확인. 사용자 슬롯은 사용하지 않는다.
- 기존 전량 보관·전량 꺼내기 저장 회귀 검사 1건과 실제 Presenter/버튼/입력 잠금/닫힘·하단 배치 PlayMode 1건을 함께 선택한다.
- 최종 고유 검사: EditMode 10건 + PlayMode 1건 통과. 첫 실행의 테스트 코드 컴파일 오류(내부 보관량 setter 접근)는 공개 TryRestore 경로로 수정했다. 두 번째 실행은 EditMode 7건과 PlayMode 1건 통과, 저장 직렬화 반환값을 bool로 잘못 비교한 3건이 실패했다. 반환형 SaveMigrationStatus.Current에 맞춰 고친 뒤 실패한 3건만 다시 실행해 모두 통과했다. 런타임 분배·버튼 코드는 이 재검사 과정에서 변경하지 않았다.
- 결과: work_process/tools/issue236-balanced-storage-20261022-final/ (EditMode 10건 중 7건 통과·검사 비교식 3건 실패, PlayMode 1건 통과), work_process/tools/issue236-save-20261022-final/ (수정한 3건 통과, 1.24초). 컴파일 오류 없음, diff 형식 검사 통과. 폰트·씬·프리팹·ProjectSettings·패키지 자동 변경 없음.
- headless 검사이며 실제 창의 렌더링·마우스 화면 확인/앱 완전 종료 후 이어하기/Windows 빌드는 별개다.

## 팀원 확인

1. Bootstrap → 탐사 → 보관함. 검색창 아래에 전체 버튼이 없고, 하단에 선택 수량/전체 이동이 두 줄로 정렬되는지 확인.
2. 화물 0/170kg과 위 예시 보관량에서 전체 꺼내기. 47/47/6개, 169.3kg과 부분 이동 안내 확인.
3. 다시 눌러도 추가 수량이 이동하지 않는지, 전체 보관은 원래대로 작동하는지 확인.
4. 실제 저장·완전 종료·이어하기에서 수량이 유지되는지는 여유 있는 환경에서 확인.

## 게시

- 아직 커밋·푸시·새 PR 전이다. 게시 요청 시 #236만 독립 PR로 만들고 #225·#237·#223 변경을 섞지 않는다.
