# #197 옛 전진기지 화면 정리 — 2026-10-18

## 범위와 소유권
- 담당: mink20001203-hub. 이슈 #197에 진행 댓글을 남기고 `origin/main` c4c89655에서 전용 `codex/issue-197-outpost-legacy-cleanup` 브랜치로 시작했다.
- 팀원이 최종 검수·병합한다. Windows 플레이/빌드/Demo 검증은 사용자 요청대로 계속 보류한다.
- #160 후속은 기존 PR #192에서 별도로 진행하며 이 PR에 섞지 않는다.

## 사용 여부 조사
- `OutpostPanelView`/`Presenter`/`Binder`/`IOutpostPanelView`는 현재 CCTV·정산 판매·보관함·충전기/보건소 팝업의 실제 공용 경로다. 전부 삭제하면 거래와 입력이 끊긴다.
- `IntegrationRuntimeBinder.BindOutpostPanelUi`의 서비스 주입, 엘리베이터/긴급 탈출 입력 우선권, 시설 위치/아이콘 및 자원 아이콘 연결을 유지했다. `SettlementSellBackend`, 현대 팝업들, `UndergroundMenuController`의 상위 창 판정 연결도 유지했다.
- Binder의 구 수량 입력·1/5/10·입출고·정산·튜토리얼 버튼 래퍼는 옛 `PanelRoot`만 참조했다. 현재 보관함은 런타임 팝업 이벤트→Binder→Presenter→Service, 판매는 SellSession/Backend를 사용한다.
- `OutpostMineralSelectButton`는 코드 외 참조가 없고 해당 GUID `afa8e41ee8fe7014db102190acd848bc`를 사용하는 씬/프리팹도 없었다.
- PromptB69/70 빌더는 옛 패널과 옛 정적 검사만 사용했다. PhaseH 빌더는 PhaseQ에서 여전히 호출되어 공개 진입점을 보존하되 구 화면 재생성을 없앴다.

## 변경
- 옛 `PanelRoot` 전체를 **Unity Editor/PrefabUtility**로 제거. CCTV·ServicePopup은 형제 오브젝트로 유지하며 런타임 판매/보관 팝업 생성도 유지한다. 씬 YAML 수동 편집 없음. 씬은 해당 프리팹을 참조하므로 별도 수정하지 않았다.
- 옛 렌더링 필드·인터페이스 6개·문자열 조립·버튼/검색 입력 연결 및 숨겨진 튜토리얼 레이어 연결을 제거했다. 도메인 OutpostService/저장 상태/시설 효과·재사용 시간은 변경하지 않았다.
- 경고/재사용 대기 팝업이 ResultText에서 가져오던 폰트를 `messageFont`로 옮겼다. 구 화면 대체 표시 없이 현재 팝업으로만 라우팅하고 호스트/Binder 활성 상태는 유지한다.
- 사용되지 않는 선택 버튼과 구 전용 빌더 2개를 .meta와 함께 삭제했다. 유지 자산의 GUID는 변경하지 않았다.
- PhaseH는 기존 팝업 연결을 검증하고 옛 자식만 제거하는 멱등 정리 도구다. 옛 화면으로 되돌리는 전체 재생성/플래그 폴링을 제거했다.
- 구 계층 존재를 요구하거나 자산을 다시 만드는 검사를 현재 팝업·경고 폰트·Missing Script/멱등성 검사로 교체했다. 실제 보관/판매 연결 회귀 검사 2개를 추가했다.

## 직렬화 감사
- 프리팹 문서 636→373개, 구 계층 문서 263개 제거. 새 문서 없음.
- 살아남은 문서 중 변경은 호스트 자식 목록, View의 구 필드→messageFont, Binder의 구 입력 필드 제거 3개뿐이다. 모든 현대 팝업 문서는 ID별 원문 비교에서 동일했다. Unity가 문서 순서를 재정렬하여 줄 단위 diff는 실제 변경보다 크다.
- Editor가 자동으로 만든 ProjectSettings 변경은 검사 후 해당 자동 변경만 복원한다. 설정/패키지 변경은 PR에 포함하지 않는다.

## 검증
- Unity 6000.5.4f1 Editor 정리/컴파일 성공.
- EditMode 첫 실행 192/193: 아이콘 검사에서 runtime resolver를 주입하지 않고 카탈로그와 옛 fallback 이미지를 비교했다. 실제 Binder와 같은 카탈로그 resolver 주입 경로로 검사 보완 후 **193/193 통과**.
- PlayMode 첫 컴파일에서 추가 검사가 Storage 목록에 없는 GetQuantity를 호출한 오류를 수정했다. 기존 API `OutpostState.GetStorageQuantity`를 사용한다.
- PlayMode 첫 실행 **9/10 통과**: 새 보관/꺼내기 및 정산 판매·닫기 검사, 범위/X키/버튼 닫기, 재사용 대기/전력 경고 검사 통과. CCTV 종료 검사는 같은 CameraRig를 쓰는 긴급 탈출 사전 준비 카메라까지 세어 2/1로 실패했다. 다른 정상 소유자의 카메라만 제외하고 유실/중복 카메라 검출은 유지하도록 검사 보완. 실패한 CCTV 종료 검사만 재실행하여 **1/1 통과**, 대상 10개의 최종 통과 근거를 확보했다.
- 실행 증거: 로컬 `sub-terra/Builds/Issue197-20261018/` XML/로그/ID별 프리팹 비교. 처음 실패 결과도 보존한다.
- 초기 아이콘 실패의 별도 HEAD 프리팹 일시 복원 실행은 자동 승인 검토가 거부해 하지 않았다. 이후 검사 설정 원인을 찾아 현재 프리팹으로 검증했으며, 사용자 허락이 도착한 뒤에도 불필요해진 복원은 수행하지 않았다.
- 고립된 프로젝트 Temp QA 저장 경로 사용. 실제 사용자 저장 슬롯, stash, 기존 미추적 폴더는 보존했다.

## 미검증/후속
- Bootstrap→새 게임→지하→각 시설 직접 조작 및 Windows Demo 완주/종료/이어하기 검증은 사용자 재개 요청 후 실시한다. Editor 검사 통과를 Windows 수동 검증 완료로 보고하지 않는다.
- #196/#198/#199를 확인했으나 현재 팀원의 추가 검토 의견은 없다. 해당 PR의 후속 의견이 생기면 같은 브랜치에서 대응한다.


## 2026-10-18 최신 main 통합 및 검토 준비
- 최신 main a3266026 통합. lessons.md 양쪽 기록을 보존해 충돌을 해소했다. 기존 팝업 공용 연결은 유지한다.
- 새 EditMode194/194, PlayMode10/10 PASS. 첫 실행9/10과 CCTV 재검사의 과거 결과와 구분하며 이번에는 대상10개 전체가 함께 통과했다. 증거: Builds/PRReady-20261018/pr200-*.
- 검사 자동 변경 define/TimeManager는 시작 상태로 복원했다. 사용자 요청으로 Windows 실제 화면 검증은 완료 기준에서 제외하고 미실행으로 명시한다. 구현·관련 자동 검사가 끝나 코드 검수용 Ready로 전환한다.
