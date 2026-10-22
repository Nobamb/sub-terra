# #213 창 열림 연출 — 2026-10-22

## 범위와 실행 위치

- 저장소: C:/Users/jeone/sub-terra. Unity 프로젝트는 Hub 첫 번째 항목 C:/Users/jeone/sub-terra/sub-terra, Unity 6000.5.4f1.
- 작업 브랜치: codex/issue-213-window-open-motion. 기준 main 1b18513a. 시작 씬 Bootstrap.
- 건설창·인벤토리·지상/지하 공용 퀘스트 상세창에만 적용한다. 퀘스트 완료 보상창, 다른 팝업, 효과음은 이번 범위가 아니다.
- #211 미커밋 진단 3개 파일은 `issue211-purchase-trace-pending-20261022`라는 stash(3f9fc5357d498ff469365f4f09d5ee08aa1ac338)에 보관했다. 삭제하거나 #213 브랜치에 섞지 않는다. #211은 기존 초안 PR #233에 이어서 게시할 변경이다.

## 구현

- TerminalWindowOpenMotion이 기존 DroneTerminalTimeline.Open과 RunFailureGlitchArt.GetCrtBeamSprite를 재사용한다. 새 그림·음원·렌더 텍스처·패키지는 추가하지 않는다.
- 가로선이 펼쳐진 뒤 위아래로 열리며 총 0.18초에 원래 크기로 복구한다. 좌상단 피벗인 건설창도 중심 위치를 유지한다. 기준점·앵커를 변경하지 않고 연출이 더한 위치 보정만 회수한다.
- Time.unscaledDeltaTime으로 일시정지 중에도 재생한다. 모션 감소 설정이 켜져 있으면 연출을 생략하고, 도중 켜져도 즉시 정상 크기로 복구한다.
- 입력 잠금·CanvasGroup alpha/interactable/blocksRaycasts·구매/보상/저장 규칙은 변경하지 않는다. 가로선은 raycastTarget=false이며 레이아웃 계산에서 제외한다.
- 닫기는 기존처럼 즉시 처리한다. 중간 닫힘·부모 비활성화 시 크기·위치를 복구하고 가로선을 숨긴다. 매번 컴포넌트나 가로선을 새로 만들지 않는다.
- 이미 열린 창의 내용 갱신·퀘스트 이전/다음 이동에서는 열기 연출을 재시작하지 않는다.
- View에서 런타임에 연결하므로 빌더 실행이나 scene/prefab YAML 편집은 필요하지 않다. 스크립트 3개에 연결하고 공통 스크립트·검사 및 각각의 새 .meta만 추가한다.

## 검사

- PlayMode 대상 3건: 실제 BuildingMenu/InventoryPanel 프리팹의 연속 표시·닫기/일시정지 중 완료/위치·크기 복구/컴포넌트 재사용, 실제 SurfaceBasePanel의 퀘스트 상세 갱신/부모 비활성화, 모션 감소·입력 속성 보존.
- 첫 실행 2건 통과·1건 검사 준비 오류(CanvasGroup의 Unity null을 C# null 병합으로 처리). 명시적인 Unity null 비교로 수정했다. 건설창 중앙 펼침 보정도 포함한 최종 3건 모두 통과, 실패/건너뜀 0, 1.40초. 소스 수정 시각 이후 재컴파일 결과이며 최종 컴파일 오류 없음.
- 원시 결과: work_process/tools/issue213-window-open-20261022/, 최종 재검사: work_process/tools/issue213-window-open-20261022-final/.
- headless 검사이며 실제 화면의 외관 확인·실제 키보드 B/I/G·마우스 조작·Windows 빌드 확인 완료를 뜻하지 않는다. 사용자 슬롯·광산 씬·전체 Demo 검사는 실행하지 않는다.

## 팀원 화면 확인

1. Bootstrap에서 탐사 진입 후 B/I로 창을 열면 청록 가로선 → 짧은 세로 펼침이 보이는지 확인.
2. 열리는 중 B/I/X/ESC로 닫고 다시 열어도 창이 납작하게 남지 않는지 확인. 정지 중 열기도 확인.
3. 지상 퀘스트 버튼과 지하 퀘스트 상세창을 열어 같은 연출인지, 이전/다음 클릭에서 매번 다시 접히지 않는지 확인.
4. 모션 감소를 켜면 즉시 표시되는지 확인. 음원 추가나 다른 팝업 연출 변경은 기대하지 않는다.

## 게시

- 아직 커밋·푸시·PR 전. 요청 시 #213만 새 PR로 분리한다. #211 진단 변경이나 검토용 통합 브랜치를 새 PR에 포함하지 않는다.
