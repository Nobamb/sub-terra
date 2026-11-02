# B-170 부팅 소리 묶음

| 파일 | 칸 | 이름 | 시작 음량 | 길이 | 원본 |
|---|---|---|---|---|---|
| main_menu_boot.wav | 85 MainMenuBoot | main_menu_boot | 0.45 | 2.3초 | sci-fi-os-495230 (0~1.9초) + tvon-108126 (0.25~1.35초, -8dB, 1.15초 지점부터 겹침), 끝 0.25초 페이드 |
| main_menu_type_tick.wav | 86 MainMenuTypeTick | main_menu_type_tick | 0.5 | 0.13초 | ui-processing-data-menu-modern-interface-text-sequence-01-230489 (0.17~0.30초) |

- 모두 44.1kHz 모노 16bit, 최고점 -1dBFS. 최종 크기(가장 큰 0.1초): 켜짐 약 -16dB(창 TV 켜짐 -15.7dB와 비슷), 틱 한 번 약 -25dB(작게 깔리는 정도).
- `preview_boot_with_ticks.mp3`: 켜짐 소리 위에 문구·부제 타이핑 구간(0.32~0.8초, 0.9~1.3초)에 0.06초마다 틱을 얹은 미리듣기. 실제 타이핑 시점은 늘린 부팅 시간표를 따른다.
- 종료 연출 소리는 새 파일 없이 기존 80 `window_tv_close`를 쓴다.
- 출처: Pixabay(Pixabay Content License).
  - https://pixabay.com/sound-effects/film-special-effects-sci-fi-os-495230/
  - https://pixabay.com/sound-effects/film-special-effects-tvon-108126/
  - https://pixabay.com/sound-effects/film-special-effects-ui-processing-data-menu-modern-interface-text-sequence-01-230489/

## `init/sound-effect-address.md`에 붙일 줄

| main_menu_boot | 메인 화면 부팅 인트로(켜짐) | https://pixabay.com/sound-effects/film-special-effects-sci-fi-os-495230/ + https://pixabay.com/sound-effects/film-special-effects-tvon-108126/ | 2026-10-28 | Sci-Fi OS 원본 0~1.9초 + TV On 원본 0.25~1.35초를 -8dB로 1.15초 지점부터 겹침, 끝 0.25초 페이드(→2.3초, 모노, 최고점 -1dBFS). 건너뛰면 코드가 0.15초 페이드. UI 폴더 |
| main_menu_type_tick | 메인 화면 부팅 문구 타이핑 틱(0.06초 간격 제한) | https://pixabay.com/sound-effects/film-special-effects-ui-processing-data-menu-modern-interface-text-sequence-01-230489/ | 2026-10-28 | UI Processing Data Menu Text Sequence 01. 원본 0.17~0.30초 틱 한 번(→0.13초, 모노, 최고점 -1dBFS). UI 폴더 |
