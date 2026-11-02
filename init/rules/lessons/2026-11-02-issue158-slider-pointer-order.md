# 슬라이더 드래그 소리는 값 변경과 포인터 이벤트 순서를 함께 처리
- 날짜: 2026-11-02
- 태그: audio, ugui, slider, pointer, issue158
- 실수: B-177의 0.07초 틱 제한만으로는 긴 슬라이더 드래그의 소리 중첩을 막지 못했다.
- 원인: 시간 간격과 한 번 잡고 놓는 입력 수명은 다르다. 트랙 클릭은 Slider가 EventTrigger의 PointerDown보다 먼저 값을 바꾸기도 한다.
- 다음부터 할 일: 드래그별 재생 여부를 따로 보관하고, 포인터를 받기 전 값 변경은 해당 프레임까지 보류한다. 실제 ExecuteEvents 순서·1초 이상 끌기·놓고 다시 잡기·비활성화를 검사한다.
- 관련: #158, B-179, SettingsMenuSkin, PromptB179SfxFeedbackPlayModeTests.
