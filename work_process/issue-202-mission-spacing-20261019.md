# #202 미션 카드 설명 간격 — 2026-10-19

- 카드 460×116, 제목 20/설명 16 기준 글자 크기와 기존 자동 축소·Ellipsis 정책 유지.
- 제목 y -46→-44, Rect 높이 34→30; 설명 y -82→-76. 헤더 뒤 2px, 제목/설명 사이 2px, 설명 아래 14px 여백 확보. 상태 아이콘은 텍스트 중심에 맞춰 5px 올림.
- DemoObjectiveView 런타임, 기존 전체 빌더, 새 텍스트 간격 전용 Editor 메뉴를 함께 반영. 씬은 Editor API로 적용했고 제목/설명/아이콘 RectTransform의 4줄 변경만 유지했다. 자동 직렬화된 다른 패널 기본값은 제외.
- PromptB107QuestUiTests 14/14 PASS. 반복 적용, 카드 크기, 여백, 겹침 방지, Ellipsis, 기존 상세창/시설창/상태 아이콘 배치를 검사했다.
- 자동 Camera.Render 캡처 quest-basic/quest-hover는 Builds/MissionSpacing-20261019에 보존해 확인. 기존 추적 증거 이미지는 복원.
- Windows 실제 게임 화면 검증은 사용자 요청으로 제외하며 미실행. 팀원 검수 후 병합.
