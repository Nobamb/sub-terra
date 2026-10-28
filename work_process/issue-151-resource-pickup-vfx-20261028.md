# #151 일반·희귀 자원 획득 시각 피드백

## 작업 기준과 범위
- 최신 main `cdac217c`, 브랜치 `codex/issue-151-resource-pickup-vfx`.
- 첫 Hub 프로젝트 `C:/Users/jeone/sub-terra/sub-terra`, Unity 6000.5.4f1.
- #151 전체가 아니라 **일반 자원/엔진 연료 획득 연출 항목만** 작업. 기존 골드 팝업·채굴 파편·팀원 효과음 및 다른 #151 항목은 변경하지 않음.
- 구현 전 MiningSystem 완료 순서, MiningYieldCommit 성공 수량, GoldPickupVfx 지급 확정 큐, 기존 SFX 연결을 대조.

## 동작
- 일반 자원: 실제 자원 아이콘 한 개가 채굴 칸에서 플레이어 쪽으로 약 0.42초에 회수되어 사라짐.
- 엔진 연료: 약간 큰 아이콘과 청록 테두리·작은 반짝임 3개로 구분, 약 0.7초. 화면 전체 섬광·새 텍스트 팝업 없음.
- MiningYieldCommit 성공의 실제 수락 수량(기본+보너스)을 큐에 넣고 같은 자원의 TileMined에만 한 번 재생. 실패·취소·0개·골드만 있는 블록·지원 아이콘 없는 항목은 재생하지 않음.
- 시각 연출이 인벤토리·골드·에너지·보상·퀘스트를 다시 변경하지 않음. 채굴/회수 소리 추가 없음.
- 최대 8개 동시 연출. 슬롯 재사용하며 희귀 장식은 해당 슬롯이 희귀 자원을 처음 표시할 때만 생성. 매 프레임 오브젝트 생성/전역 탐색/난수 소비 없음.
- 플레이어 이동에 목적지를 맞추고, 장거리 순간이동·비활성화·재바인딩·씬 종료 시 정리. 모션 감소 시 플레이어 위치에서 짧은 페이드만 사용.
- MainScreen 레이어를 사용해 CCTV 미리보기에서 획득 표시 제외.

## 변경 파일
- `Scripts/App/Integration/ResourcePickupVfx.cs` (+ Unity 생성 meta): 시각 표시·이벤트 구독·풀 수명주기.
- `Scripts/App/Integration/IntegrationRuntimeBinder.cs`: UI 준비 후 자동 연결, 지급 성공 큐, 해제.
- `Tests/EditMode/App/Integration/ResourcePickupVfxTests.cs` (+ meta): 확정/실패/중복/일반·희귀/풀/접근성 검사.
- `Tests/PlayMode/Integration/MineDemo/ResourcePickupVfxPlayTests.cs` (+ meta): 격리 실제 광산 지급 및 화면 캡처.
- `Tests/PlayMode/Integration/Bootstrap/UiPlayModeTestSupport.cs`: 광산 씬 로딩 대기 옵션 추가. 기존 기본 10초는 유지하며 이번 검사에서만 30초 사용(지상 검사와 같은 방식).
- lessons.md: EditMode 일반 MonoBehaviour 수명주기를 자동 호출한다고 가정한 검사 실수 기록.
- 원화·씬·프리팹·폰트·ProjectSettings·Packages·Shared 계약 변경 없음. Inspector 재연결 불필요.

## 최소 검사
- 최초 EditMode 4건 중 1건은 EditMode의 OnDisable 미자동 호출 가정으로 실패. 검사에서 명시적으로 수명주기를 호출하도록 보정, 실제 비활성화는 PlayMode에서도 검사.
- 최종 EditMode 4/4 통과(0.641초). 결과 `work_process/tools/issue-151-final`.
- 실제 광산 검사는 슬롯 0 격리 환경. 구리 테스트 타일을 런타임에서만 준비하고 실제 MiningSystem→InventoryService→VFX를 검사. 실제 사용자 저장/씬 에셋은 변경하지 않음.
- 초기 PlayMode는 씬 로딩 10초 초과로 중단. 다음 실행은 옛 GameDataCatalog.MiningTiles(빈 목록)를 조회한 검사 준비 오류로 중단. 실제 MiningSystem에 연결된 MiningTileResolver에서 `tile.copper`를 찾도록 수정. 제품 지급/연출 검증 조건을 완화하지 않음.
- 희귀 자원 화면은 카탈로그의 실제 엔진 연료 아이콘으로 **표시만 시연**하며 심층 해금·엔진 연료 실제 채굴 검증을 뜻하지 않음.
- 캡처는 `work_process/images/resource-pickup-vfx`에 보관.
- 실제 광산 PlayMode 최종 1/1 통과(39.874초): 자동 연결, 실제 MiningSystem→지급 성공→연출 1회, 연출 후 수량 불변, 빈 칸 실패 시 미재생, 실제 OnDisable 정리. 결과 `work_process/tools/issue-151-verified`.
- [화면 비교](images/resource-pickup-vfx/comparison.png) 확인: 일반 구리 아이콘과 엔진 연료의 청록 테두리·반짝임 차이, 회수 중 사라지는 순서. 엔진 연료는 표시 시연이며 실제 채굴 결과가 아님.
- 코드 컴파일 성공, `git diff --check` 통과, ProjectSettings/Packages 내용 변경 없음. 새 스크립트 meta 3개는 Unity가 생성.

## 미검증 / 팀원 확인
1. Bootstrap → 실제 슬롯 → 광산에서 구리/철/리튬 획득 아이콘이 과하지 않고 알아볼 수 있는지.
2. 엔진 연료 실제 채굴 시 일반 자원과 구분되고 골드 팝업/채굴 파편/효과음과 과하게 겹치지 않는지.
3. 카메라 줌·해상도·실제 조작 중 자연스러움, 모션 감소의 체감 확인.
4. Windows exe·성능 측정은 미검증. 연출 수 제한은 성능 개선 측정 결과를 뜻하지 않음.
- 사용자 승인 후 커밋·푸시·PR 게시. #151 전체 자동 종료 없이 자원 획득 연출 항목만 참조한다.
