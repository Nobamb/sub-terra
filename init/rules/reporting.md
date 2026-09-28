# 결과 보고·작업 요청 형식

14. Agent 작업 결과물 제출 형식
    각 Agent는 작업 완료 후 아래 항목을 보고한다.
    14-1. 수정/생성한 파일 목록
    14-2. 주요 변경 내용과 설계 이유
    14-3. 실행한 테스트와 수동 검증 절차
    14-4. 테스트 결과와 Unity Console 상태
    14-5. 필요한 Inspector 참조와 Integration 방법
    14-6. 남은 TODO, 세이브 호환성 또는 검증 제한
    14-7. 다른 담당자가 참고해야 할 사항

보고 예시:
수정 파일:

- `Assets/_Project/Scripts/Gameplay/Mining/MiningSystem.cs`
- `Assets/_Project/Tests/EditMode/Gameplay/MiningSystemTests.cs`

주요 변경:

- 채굴 완료 시 `IMiningRewardReceiver`로 광물 ID와 수량을 한 번만 전달
- Tilemap 범위 밖 좌표와 전력 부족 상태에서 채굴을 시작하지 않도록 처리

테스트:

- Edit Mode `MiningSystemTests` 통과
- `Gameplay_Mining_Test.unity`에서 구리 보상 1회 발생 확인

주의 사항:

- Integration Scene의 Player Prefab에 `ForegroundTilemap`과 `InventoryService` 연결 필요

15. Agent 작업 요청 템플릿
    Agent에게 작업을 요청할 때는 아래 형식을 사용할 수 있다.

Agent 이름:

작업 목표:

수정/생성할 파일:

참조해야 할 파일:

구현해야 할 시스템 또는 함수:

입력:

출력:

금지 사항:

소유권·세이브·보안 규칙:

완료 조건:

테스트 방법:

예상 실패 케이스:
