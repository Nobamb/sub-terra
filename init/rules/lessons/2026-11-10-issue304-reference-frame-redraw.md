# 기존 이미지의 형태 변경은 원본을 직접 편집한다
- 날짜: 2026-11-10
- 태그: issue304, building, UI, imagegen, reference
- 실수: 기존 건설창 분위기의 정사각 프레임을 요청받고 새 디자인을 생성해 원본의 두꺼운 청록 발광 테두리가 약해졌다.
- 원인: 원본 이미지 파일을 편집 입력으로 전달하지 않고 설명만으로 새 이미지를 생성했다.
- 다음부터 할 일: 원본 이미지를 보고 imagegen 참조 경로로 전달한다. 형태 변경 시 유지할 재질·발광·테두리를 명시하고, 9-slice 모서리와 글로우 여백을 실제 UI에 맞춰 확인한다.
- 관련: inventory-window-frame.png, building-card-square.png, PromptB188BuildingModeBuilder.BuildWindowFrame.
