# 타이핑 집계를 추가할 때 기존 지역 변수 범위를 확인한다
- 날짜: 2026-11-04
- 태그: audio, drone, csharp, compilation, issue158
- 실수: ApplyTyping의 효과음 집계 변수에 기존 내부 블록의 typed와 같은 이름을 써 CS0136 컴파일 오류가 났다.
- 원인: 긴 메서드의 내부 지역 변수와 새 바깥 범위 변수의 이름 충돌을 놓쳤다.
- 다음부터 할 일: 기존 메서드 전체의 지역 변수 범위를 확인하고, 추가 집계는 의미가 드러나는 별도 이름(typedCharacters)을 쓴 뒤 에디터 컴파일을 먼저 확인한다.
- 관련: #158, B-183, DroneDialoguePanelView.ApplyTyping.
