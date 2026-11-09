# 슬라이드로 들어오는 UI는 연출이 끝난 뒤 클릭을 검사한다

- 날짜: 2026-11-07
- 태그: UI, uGUI, PlayMode, 연출, 입력, 건설창, issue269
- 실수: B-188 PlayMode 검사가 B로 건설 레일을 다시 연 1~2프레임 뒤 `>>`를 눌러 "raycast hit 없음"으로 실패했다. 처음엔 다른 코드가 레일을 다시 여는 버그로 의심했다.
- 원인: 레일은 0.16초 동안 왼쪽 화면 밖(x<0, 투명)에서 밀려 들어온다. 첫 열기 뒤에는 대기를 넣었지만 다시 열기 뒤에는 넣지 않았다.
- 다음부터 할 일: 슬라이드·확대 연출이 있는 창을 열 때마다(첫 열기·다시 열기 모두) 연출 길이만큼 기다린 뒤 클릭한다. 실패 메시지에 클릭 좌표·화면 크기·RectTransform 모서리·CanvasGroup 값을 함께 남겨 원인을 바로 가른다. 원인 추적용 임시 로그는 확인 후 바로 지운다.
- 관련: #269, B-188, `Tests/PlayMode/Integration/Bootstrap/PromptB110BuildingMenuPlayModeTests.cs`, `Scripts/App/UI/Building/BuildingModeSlideMotion.cs`
