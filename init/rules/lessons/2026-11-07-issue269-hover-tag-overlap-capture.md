# 호버 이름표는 밑에 깔린 UI 위에서 캡처로 확인한다

- 날짜: 2026-11-07
- 태그: UI, uGUI, 건설창, 호버, 툴팁, PlayMode, 캡처, issue269
- 실수: B-188 레일 호버 홀로그램 이름표를 반투명(알파 0.72) 판으로 만들었다. EditMode 검사는 모두 통과했지만 실제 캡처에서는 첫 아이콘 오른쪽의 "시설을 고르세요" 힌트 글자가 이름표 위·아래로 반쯤 잘린 채 비쳐 지저분했다.
- 원인: 레일 바로 오른쪽(x=72)에 힌트 칩·카드가 붙어 있어 이름표가 반드시 겹치는데, 위치·raycast만 검사하고 겹친 모습은 보지 않았다.
- 다음부터 할 일: 툴팁·이름표처럼 다른 UI 위에 뜨는 요소는 (1) 판을 불투명하게 하고 홀로그램·유리 느낌은 테두리·줄무늬·주사선으로 낸다. (2) 짧은 안내 문구처럼 겹쳐도 되는 요소는 떠 있는 동안 비켜 준다. (3) 빈 상태와 카드가 열린 상태 두 경우를 PlayMode 캡처로 직접 본다.
- 관련: #269, B-188, `Scripts/App/UI/Building/BuildingRailHologram.cs`, `Editor/DataValidation/PromptB188BuildingModeBuilder.cs`, `Tests/PlayMode/Integration/Bootstrap/PromptB110BuildingMenuPlayModeTests.cs`
