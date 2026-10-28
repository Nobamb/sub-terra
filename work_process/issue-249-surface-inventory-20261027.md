# #249 지상 인벤토리·단축키 최신 main 통합

## 실행 위치
- 첫 Hub 프로젝트: `C:/Users/jeone/sub-terra/sub-terra`, Unity 6000.5.4f1.
- 브랜치 `codex/issue-249-surface-inventory-main`, 기준 `origin/main` `3cb418a3`.
- 시작 씬 Bootstrap → 실제 슬롯 → 지상 기지.
- 10/26 보존 작업 `3f7e30bd`에서 관련 변경만 3방향 병합. 최신 main의 #211 업그레이드 바인더 Unbind 수정 유지.

## 구현
- 버튼: 위 퀘스트 / 업그레이드, 아래 자원 판매 / 인벤토리. 기존 크기·그림체 유지.
- 인벤토리: 광산의 InventoryPanel 원본을 중첩 프리팹으로 재사용하고 같은 InventoryService에 연결.
- I 인벤토리 토글, U 업그레이드 토글. 입력칸 작성 중 차단.
- 다른 창·설정·확인창이 열리면 겹쳐 열기 차단. ESC/X는 최상위 창만 닫기.
- 공통 TV 열림·닫힘 연출, 닫는 중 같은 창 재열기, 카드가 딤보다 위에 표시되도록 연결.
- 투명하게 숨긴 구형 판매 카드가 창 열기를 막던 모달 판정 정리.
- 초기화 버튼은 2×2 아래로 배치.

## 변경 범위
- SurfaceBaseBinder/View, PromptB117SurfaceBaseBuilder, Issue249SurfaceInventoryBuilder.
- SurfaceBasePanel.prefab: 기존 Editor 빌더로 생성했던 결과를 복원. 원본 InventoryPanel.prefab은 변경하지 않음.
- 기존 SurfaceBase EditMode 검사와 지상 씬 PlayMode 검사.
- 씬·폰트·ProjectSettings·Packages·저장 형식 변경 없음. 새 Inspector 연결은 빌더에서 수행.
- 지상 퀘스트 닫힘 수정은 별도 #253 브랜치에 있음. 이 브랜치에 중복 적용하지 않음.

## 검증과 인계
- EditMode 4건 통과(1.613초): 버튼 정렬, 원본 인벤토리 재사용/배선, 업그레이드 카드 정렬, 닫기/설정 회귀. 결과 `work_process/tools/issue-249-main-tests/EditMode-summary.txt`.
- 최초 PlayMode 2건은 그래픽 장치 없는 실행으로 Game View 생성 오류/대기 시간 초과. 게임 기능을 바꾸지 않고 그래픽을 켠 실행으로 재검사.
- 그래픽 실행 PlayMode 2건 통과(8.572초): I/U·버튼 토글, ESC·겹침 방지, 실시간 수량 갱신 및 닫는 중 재열기. 결과 `work_process/tools/issue-249-main-graphics/PlayMode-summary.txt`.
- 실제 지상 씬 캡처 960×540 3장을 확인: `work_process/images/issue249/surface-actions.png`, `surface-inventory.png`, `surface-upgrade.png`. 버튼 2×2 및 중앙 창 배치/딤 표시 확인.
- Windows exe·실제 사용자 슬롯의 판매/구매는 미검증. 검사에는 격리 슬롯 사용.
- 코드 컴파일 성공, 코드 diff 공백 검사 통과. 프리팹/메타의 Unity 생성 공백은 유지. ProjectSettings/Packages 내용 변경 없음.
- 팀원 확인: Bootstrap 실제 슬롯에서 2×2 배치, I/U 토글, ESC/X 닫기, 판매 전후 수량 일치, 업그레이드 실제 구매 및 레벨 즉시 갱신, 다른 창과 겹치지 않는지.
- #253과 #249는 각각 독립적인 main 기반 브랜치. #253을 먼저 검토하면 퀘스트 닫힘 확인도 함께 가능.
