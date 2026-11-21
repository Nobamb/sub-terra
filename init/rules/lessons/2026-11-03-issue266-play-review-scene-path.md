# 실행 검사 시작 씬은 실제 경로를 검색해 지정
- 날짜: 2026-11-03
- 태그: unity, testing, animation, bootstrap
- 실수: 여자 걷기 Play 검사 도구에 Bootstrap 씬이 App 폴더에 있다고 추정해 첫 실행이 씬 로드 전에 실패했다.
- 원인: 일반적인 통합 씬 경로를 Bootstrap 경로로 잘못 적용했다.
- 다음부터 할 일: 실행 전 실제 씬 파일 경로를 검색한다. 검사 도구의 준비 실패와 게임 동작 실패를 구분하고, 재시도 결과도 별도 기록한다.
- 관련: #266, FemaleLocomotionReviewSmoke.cs.
