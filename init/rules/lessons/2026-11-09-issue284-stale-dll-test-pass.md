# 컴파일 오류로 남은 옛 DLL에서 테스트가 통과해 새 코드를 검증한 것으로 착각함
- 날짜: 2026-11-09
- 태그: testing, editor, compilation, drone, issue284
- 실수: DroneBubbleMotion에 TV 켜짐 연출을 넣으며 앞부분만 바꿔 옛 WidthScale이 남아 CS0111 컴파일 오류가 났는데, 편집기 테스트 플래그 실행이 이전 DLL로 돌아 65개 통과가 나왔다.
- 원인: 부분 교체 뒤 같은 메서드가 남았는지 보지 않았고, 테스트 전에 Console 오류와 Library/ScriptAssemblies DLL 시각을 확인하지 않았다.
- 다음부터 할 일: 코드 수정 뒤 테스트 전에 Console 오류 0개와 해당 DLL이 원본 파일보다 새것인지 확인한다. 새로 추가한 테스트 개수가 통과 수에 반영됐는지도 본다.
- 관련: #284, B-189, DroneBubbleMotion.WidthScale.
