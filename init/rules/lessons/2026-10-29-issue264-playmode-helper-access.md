# PlayMode 저장 검사 보조 타입 참조 확인

- 날짜: 2026-10-29
- 태그: testing, playmode, save, compile
- 실수: B-172 새 PlayMode 검사에서 저장 보조 타입·네임스페이스 참조 누락
- 원인: 이웃 검사의 private 보조 타입과 인터페이스 이름을 확인하지 않음
- 다음부터 할 일: 보조 타입 접근 범위와 ISaveFileSystem 실제 메서드를 읽고 컴파일부터 확인한다
- 관련: #264, B-172
