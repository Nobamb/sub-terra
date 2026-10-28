# init/sound-effect-address.md 에 추가할 줄 (D-2 위험·생존, 2026-10-26)

표 맨 아래(cargo_heavy_warning 줄 다음)에 그대로 붙이면 됨. player_hurt는 두 원본을 합친 파일이라 원본 2개를 한 줄에 적음.

| 파일 이름 | 게임 안에서 쓰는 곳 | 받은 페이지 주소 | 받은 날짜 | 비고 |
|---|---|---|---|---|
| gas_pulse_loop | 가스 구역 안에 있는 동안 맥박(반복, 초록 화면 깜빡임과 맞춤) | https://pixabay.com/sound-effects/film-special-effects-alien-heartbeat-throb-98672/ | 2026-10-26 | Alien_Heartbeat_Throb (freesound_community). 원본 0.015~1.245초 한 주기만(11.08→1.23초), 모노. 박자 사이가 무음이라 이음새 없이 반복됨. 위험 구역에서 빨라지는 건 칸 값(피치)으로. World 폴더 |
| player_hurt | 플레이어 피격(낙하·떨어지는 돌·가스) | https://pixabay.com/sound-effects/film-special-effects-big-hit-sound-effect-241416/ + https://pixabay.com/sound-effects/film-special-effects-metal-crack-electrical-circuit-brake-606771/ | 2026-10-26 | Big Hit Sound Effect (11325622)의 첫 충격 앞 0.35초 + Metal Crack Electrical Circuit Brake (Tutmozis)의 금속 "퉁" 뒤 "지지직" 부분(원본 1.00~1.58초, 600Hz 아래 제거)을 0.10초부터 겹쳐 합침(→0.68초, 모노). 같은 방식 변형 B·C 있음(README). World 폴더 |
| low_health_warning | 체력 30% 아래로 처음 떨어질 때 1번 | https://pixabay.com/sound-effects/film-special-effects-warning-alarm-loop-1-279206/ | 2026-10-26 | Warning Alarm Loop #1 (Audley_Fergine). 반복 경보 중 두 번째·세 번째 주기(원본 0.520~1.486초) 2번 "부-" 만, 끝 0.12초 페이드(3.89→0.97초, 모노). 반복 재생하지 않음. 1주기(0.48초) 버전 있음. 광산 위험 경보(Factory Alarm)와 달리 음 높이가 오르내리지 않는 낮은 버즈음 |
| ceiling_collapse | 천장 무너짐(돌 떨어짐) | https://pixabay.com/sound-effects/horror-boulder-impact-487673/ | 2026-10-26 | Boulder Impact (DRAGON-STUDIO). 앞 1.4초만, 끝 0.3초 페이드(2.11→1.40초, 모노). World 폴더 |
| rescue_holo_open | 전력 0 뒤 머리 위 구조 요청 홀로그램 펼침 | https://pixabay.com/sound-effects/film-special-effects-glitch-sound-4-589418/ | 2026-10-26 | Glitch Sound 4 (asproductionmusic). 앞 무음 0.25초 제거, 뒤 무음 정리, 끝 0.15초 페이드(2.66→1.63초, 모노). World 폴더 |
