# init/sound-effect-address.md 에 추가할 줄 (B-178 퀘스트 보상·작은 글리치 창·드론 스캔, 2026-10-31)

표 맨 아래(main_menu_type_tick 줄 다음)에 그대로 붙이면 됨.

| 파일 이름 | 게임 안에서 쓰는 곳 | 받은 페이지 주소 | 받은 날짜 | 비고 |
|---|---|---|---|---|
| quest_reward | 퀘스트 보상 지급(보상이 실제로 들어올 때 한 번) | https://pixabay.com/sound-effects/film-special-effects-success-221935/ | 2026-10-31 | Success (UpdatePelgo). 원본 뒷부분 0.885~1.985초만(두 번째 "띠-링"과 마무리), 끝 0.30초 페이드(2.12→1.10초, 모노, 최고점 -1dBFS). UI 폴더 |
| glitch_popup_open | 작은 글리치 창 열림: 퀘스트 클리어 창·심층 구역 해금 창 | https://pixabay.com/sound-effects/film-special-effects-glitch-sound-2-589408/ | 2026-10-31 | Glitch Sound 2 (asproductionmusic). 원본 0.10~1.00초, 끝 0.20초 페이드(1.57→0.90초, 모노, 최고점 -1dBFS). 예전 브리핑 글리치 원본(지금은 379466로 바뀜). 최종은 150Hz 아래를 깎은 가벼운 버전(저음 원본은 _heavy 비교본). UI 폴더 |
| glitch_popup_close | 작은 글리치 창 닫힘: 퀘스트 클리어 창·심층 구역 해금 창 | https://pixabay.com/sound-effects/film-special-effects-glitch-sound-2-589408/ | 2026-10-31 | glitch_popup_open과 같은 원본 0.10~0.80초를 거꾸로, 앞 0.01초·끝 0.15초 페이드(→0.70초, 모노, 최고점 -1dBFS). 최종은 150Hz 아래를 깎은 가벼운 버전(_heavy 비교본 있음). UI 폴더 |
| drone_scan_ping | 드론 30초 스캔 링이 퍼지기 시작할 때 | https://pixabay.com/sound-effects/film-special-effects-submarine-sonar-386154/ | 2026-10-31 | Submarine Sonar. 앞 무음 0.057초 제거, 0.9초까지만, 끝 0.35초 페이드(1.10→0.90초, 모노, 최고점 -1dBFS). World 폴더 |
