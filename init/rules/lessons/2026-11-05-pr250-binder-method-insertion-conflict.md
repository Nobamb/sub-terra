# 통합 바인더의 같은 위치에 메서드를 추가해 병합 충돌
- 날짜: 2026-11-05
- 태그: git, integration, merge
- 실수: 배경 초기화 함수와 main의 효과음 수명주기 함수가 OnDestroy 바로 앞에서 충돌했다.
- 원인: 서로 다른 기능이 같은 메서드 삽입 위치를 사용했다.
- 다음부터 할 일: 기능 전용 보조 함수는 관련 함수 근처에 배치하고, 최신 main과 3-way 병합을 검사해 양쪽 변경이 보존되는지 확인한다.
- 관련: PR #250, IntegrationRuntimeBinder.EnsureMineBackdrop
