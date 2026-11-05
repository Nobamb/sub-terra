# 팝업 열림음은 실제 표시 경로에서 확인한다
- 날짜: 2026-11-04
- 태그: audio, sfx, ui, cargo, timing, testing
- 실수: 화물 창의 Show(false) 직후 재생 횟수만 확인해, 실제 페이지 이동에서 열림음이 생략되는 것과 사용자가 느끼는 재생 시점 차이를 놓쳤다.
- 원인: 처음 열기와 Show(true) 페이지 이동이 다른 경로였고, 재생 호출은 활성화 뒤 글자 배치를 계산한 다음에 있었다. 호출 횟수만으로 실제 청취 지연의 원인까지 확정할 수는 없다.
- 다음부터 할 일: 표시 시작에서 열림음을 내고 연출 완료 시점과 분리한다. Presenter/View의 페이지 이동·복귀와 중복 클릭도 검사한다. AudioSource가 내용 등장 전에 재생 중인지 확인하고, 직접 청취하지 못한 체감은 미검증으로 남긴다.
- 관련: B-182-1, CargoFullPopupView.Show, CargoDiscardPopupView.Show, PromptB182SfxCargoTests, PromptB182SfxCargoPlayModeTests.
