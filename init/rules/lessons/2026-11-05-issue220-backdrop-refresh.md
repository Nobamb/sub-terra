# 테스트가 Time.timeScale을 바꾸면 ProjectSettings가 더러워짐
- 날짜: 2026-11-05
- 태그: EditMode, Time, ProjectSettings
- 실수: 일시정지 검사를 위해 EditMode 테스트에서 Time.timeScale을 0으로 바꿨더니 TimeManager.asset과 여러 프리팹·폰트가 함께 변경되어 있었다.
- 원인: 에디터에서 timeScale 변경은 TimeManager에 저장되고, 전체 테스트 실행이 다른 에셋도 다시 직렬화한다.
- 다음부터 할 일: 일시정지 로직은 timeScale을 인자로 받는 순수 클래스로 검사하고, 실행 후 git status로 ProjectSettings·프리팹·폰트를 되돌린다.
- 관련: PR #250, B-187
