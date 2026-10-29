# QA 검사에서 저장 목록에 없는 GetQuantity 호출

- 날짜: 2026-10-18
- 태그: save, storage, testing
- 실수: QA 검사에서 저장 목록에 없는 GetQuantity 호출
- 원인: 인접 테스트의 실제 API 미확인
- 다음부터 할 일: OutpostState 수량은 GetStorageQuantity를 사용하고 기존 검사 호출을 먼저 확인
