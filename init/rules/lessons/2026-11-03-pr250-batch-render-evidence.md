# 배치 검사에서 임시 렌더러와 프레임 대기 조건 확인
- 날짜: 2026-11-03
- 태그: PlayMode, batchmode, 렌더, 테스트
- 실수: 신규 배경 통합 검사가 최초 브리핑을 기다리며 멈추고 DontSave 배경을 누락 검색했다. 첫 캡처는 검은 화면이었다.
- 원인: 배치 실행의 WaitForEndOfFrame 제한, 일반 Object 검색의 DontSave 제외, ScreenCapture의 배치 화면 미출력을 고려하지 않았다. 테스트 namespace도 첫 컴파일에서 누락했다.
- 다음부터 할 일: 기존 진행 중 상태의 실제 런타임 경로를 사용하고 소유자 자식으로 임시 렌더러를 찾는다. 배치 캡처는 실제 카메라의 URP RenderRequest 결과를 직접 읽고 눈으로 확인한다. 기존 API namespace를 함께 대조한다.
- 관련: PR #250, work_process/issue-250-depth-background-review-20261028.md