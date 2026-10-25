# Sub-Terra D-1 효과음 (정리본, 2026-10-25)

공통: 44.1kHz 16bit WAV. 앞 무음 제거(소리 시작 전 약 2.5ms만 남김) + 2ms 페이드인, 뒤 무음 정리 후 짧은 페이드아웃. 20Hz 아래 저음·직류 성분 제거. 피크 -1dBFS. 음 높이·속도는 바꾸지 않음.
음량(dBFS)은 피크 / 전체 평균(RMS) / 가장 큰 0.1초 평균(모노로 합친 기준).
mp3는 들어보기용(192kbps). 게임에는 wav를 넣음.

## 넣을 곳

- 게임 파일(wav 15개): `Assets/_Project/Audio/SFX/World/` (이미 있는 폴더)
- 이 묶음 전체(wav + mp3 + README): `work_process/MVP2/sound/sfx-D1/`
- 효과음 채널: 모두 **World**. 경보(Alert) 채널은 쓰지 않음(cargo_heavy_warning도 World 또는 UI. Alert는 한 번에 1개라 다른 경보를 끊음).
- 기존 World 파일처럼 Unity 가져오기 설정을 "모노로 합치기 + normalize"로 맞추기. 그래서 추천 음량은 이 설정 기준으로 계산함.

| 파일                     | 쓰는 장면                                               | 원래 → 새 길이           | 한 일                                                                                                                  | 피크 / RMS / 0.1초 최대 | 추천 음량 |
| ------------------------ | ------------------------------------------------------- | ------------------------ | ---------------------------------------------------------------------------------------------------------------------- | ----------------------- | --------- |
| footstep.wav             | 광산·시설에서 걸을 때 걷기 동작 한 걸음마다 1번         | 0.42s → 0.39s            | 한 걸음 그대로. 앞 무음 정리, 끝 0.04s 페이드. 순간 소리라 약 +5dB 올리고 리미터                                       | -1.0 / -23.3 / -18.1    | 0.65      |
| jump.wav                 | 점프로 발이 떨어지는 순간                               | 8.04s → 0.50s            | **2026-10-25 교체(B-165).** "Whoosh End" 본체 앞 0.5초만(원본 0.25s부터). 0.04s 페이드인, 끝 0.25s 페이드. 이전 jump-615208(마리오 느낌)에서 교체 | -1.0 / -13.2 / -13.1    | 0.4       |
| land.wav                 | 보통 높이에서 착지                                      | 1.27s → 0.55s            | 앞 0.6초 안의 "툭"만, 끝 0.1s 페이드                                                                                   | -1.0 / -17.1 / -12.4    | 0.45      |
| land_heavy.wav           | 높은 곳에서 떨어져 다칠 때(세게 떨어짐)                 | 2.40s → 1.01s            | 앞 무음 0.2s 제거, "쿠웅" 본체 약 1.0초 + 꼬리 페이드. **2026-10-25(B-165)**: 노트북 스피커에서 안 들리는 아주 낮은 저음을 줄이고(120Hz 아래 -6dB) 1.5kHz 부근을 +4dB, 약 +5dB 리미터 | -1.0 / -13.0 / -10.1    | 0.7       |
| ladder_grab.wav          | 사다리를 잡을 때와 놓을 때                              | 1.44s → 0.20s            | 짧은 "챙"만(0.2초), 끝 0.15s 페이드. 약 +5dB 리미터                                                                    | -1.0 / -22.4 / -20.9    | 0.9       |
| ladder_climb.wav         | 사다리를 한 칸 오르거나 내릴 때마다 1번(반복 소리 아님) | 4.34s → 0.50s            | 나무 발걸음 7번 중 첫 번째(원본 0.60s) 한 걸음만, 끝 0.08s 페이드                                                      | -1.0 / -19.9 / -13.0    | 0.35      |
| ladder_climb_b.wav       | 위와 같음. 번갈아 쓰면 덜 기계적                        | 4.34s → 0.50s            | 두 번째 걸음(원본 1.13s) 한 걸음만, 끝 0.08s 페이드                                                                    | -1.0 / -19.8 / -12.8    | 0.35      |
| elevator_door.wav        | 엘리베이터 문이 열릴 때와 닫힐 때 둘 다                 | 9.40s → 1.58s            | 원본 안 첫 번째 문 소리(열림)의 앞 1.6초만. 작게 시작해 "드르륵" 열리는 부분까지, 끝 0.35s 페이드                      | -1.0 / -16.6 / -12.8    | 0.45      |
| elevator_move_loop.wav   | 엘리베이터가 움직이는 동안 반복                         | 30.67s → 5.50s (한 바퀴) | 가운데 고른 구간(원본 14~20s) 6초를 잘라 끝 0.5초를 처음에 겹쳐 이음 → 5.5초 한 바퀴. 이음매 튐 없음                   | -1.0 / -8.0 / -5.7      | 0.15      |
| elevator_arrive_ding.wav | 엘리베이터가 층에 도착했을 때                           | 3.67s → 1.35s            | "딩"과 울림 1.35초까지, 끝 0.3s 페이드                                                                                 | -1.0 / -17.6 / -12.2    | 0.45      |
| drill_loop.wav           | 채굴 중 드릴 반복 (**추천**)                            | 9.62s → 1.85s (한 바퀴)  | 원본 2~4s 2초를 잘라 끝 0.15초를 처음에 겹쳐 이음 → 1.85초 한 바퀴                                                     | -1.0 / -14.0 / -13.2    | 0.35      |
| block_break.wav          | 바위·광물 블록이 부서질 때                              | 2.98s → 1.06s            | "와장" + 부스러기까지 1.06초, 끝 0.15s 페이드                                                                          | -1.0 / -15.8 / -8.8     | 0.3       |
| pickup_common.wav        | 구리·철·리튬이 화물칸에 들어올 때                       | 1.54s → 0.68s            | 앞 0.7초 안의 획득음만, 끝 0.1s 페이드. **2026-10-25(B-165)**: 획득음 밑에 깔린 들리지 않는 저음(40~80Hz)을 180Hz 아래로 잘라내고 약 +5dB 리미터 | -1.0 / -21.4 / -17.9    | 0.65      |
| pickup_rare.wav          | 엔진 연료(희귀 자원)가 화물칸에 들어올 때               | 5.72s → 0.81s            | 앞부분 첫 반짝임만(약 0.8초). 원본은 2초 넘게 이어지고 뒤에 두 번째 소리가 있음. 0.55s부터 0.3s 동안 자연스럽게 페이드 | -1.0 / -15.8 / -12.2    | 0.5       |
| cargo_heavy_warning.wav  | 화물이 80%를 넘는 순간 1번                              | 1.03s → 0.34s            | 원본은 같은 경고음이 2번 반복. 첫 번째 1번만(0.35초), 끝 0.04s 페이드                                                  | -1.0 / -7.9 / -5.2      | 0.2       |

## 반복 소리

- elevator_move_loop: 한 바퀴 5.5초. Unity AudioClip Loop 켜고 반복. 소리가 고르게 이어져서 겹침 방식으로 충분.
- drill_loop: 한 바퀴 1.85초(겹침 방식). 비교용 정방향+역방향 버전(drill_loop_fwdrev)은 쓰지 않기로 해서 뺐다.
- ladder_climb은 반복 소리가 아니라 한 칸마다 1번 트는 소리. ladder_climb / ladder_climb_b를 번갈아 쓰면 자연스러움.

## 음량 메모

- 추천 음량은 "가장 큰 순간 대비 평소 크기"를 맞춰 계산하고, 자주 나는 소리(걸음·사다리)는 더 작게 잡음. 들어보고 조정.
- footstep·ladder_grab은 아주 짧은 순간 소리라 계산상 음량이 높게 나옴(0.65 / 0.9). 너무 크면 0.5 / 0.6 정도로 낮추기.
- elevator_move_loop·cargo_heavy_warning은 소리가 꽉 차 있어서 낮게(0.15 / 0.2) 시작.

## 출처

| 파일                 | Pixabay                                                                                      | 원본 파일명                                                                 |
| -------------------- | -------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| footstep             | https://pixabay.com/sound-effects/film-special-effects-st1-footstep-sfx-323053/              | yarzur_ofc-st1-footstep-sfx-323053.mp3                                      |
| jump                 | https://pixabay.com/sound-effects/film-special-effects-whoosh-end-384629/                    | soundreality-whoosh-end-384629.mp3 (2026-10-25 교체. 이전 원본 https://pixabay.com/sound-effects/film-special-effects-jump-615208/) |
| land                 | https://pixabay.com/sound-effects/film-special-effects-character-fall-impact-250069/         | universfield-character-fall-impact-250069.mp3                               |
| land_heavy           | https://pixabay.com/sound-effects/film-special-effects-heavy-body-fall-352446/               | universfield-heavy-body-fall-352446.mp3                                     |
| ladder_grab          | https://pixabay.com/sound-effects/film-special-effects-metal-clang-sound-81634/              | freesound_community-metal-clang-sound-81634.mp3                             |
| ladder_climb         | https://pixabay.com/sound-effects/film-special-effects-footsteps-on-wood-397989/             | dragon-studio-footsteps-on-wood-397989.mp3                                  |
| ladder_climb_b       | https://pixabay.com/sound-effects/film-special-effects-footsteps-on-wood-397989/             | dragon-studio-footsteps-on-wood-397989.mp3                                  |
| elevator_door        | https://pixabay.com/sound-effects/technology-elevator-door-opening-2-sounds-included-462112/ | freesoundfx-elevator-door-opening-2-sounds-included-462112.mp3              |
| elevator_move_loop   | https://pixabay.com/sound-effects/city-elevator-ride-137245/                                 | gregorquendel_sounddesign-elevator-ride-137245.mp3                          |
| elevator_arrive_ding | https://pixabay.com/sound-effects/film-special-effects-lift-bell-107838/                     | freesound_community-lift-bell-107838.mp3                                    |
| drill_loop           | https://pixabay.com/sound-effects/city-jack-hammer-breaking-up-concrete-short-burst-103896/  | freesound_community-jack-hammer-breaking-up-concrete-short-burst-103896.mp3 |
| block_break          | https://pixabay.com/sound-effects/film-special-effects-rock-destroy-6409/                    | freesound_community-rock-destroy-6409.mp3                                   |
| pickup_common        | https://pixabay.com/sound-effects/film-special-effects-item-pickup-3-540193/                 | yodguard-item-pickup-3-540193.mp3                                           |
| pickup_rare          | https://pixabay.com/sound-effects/film-special-effects-diamond-found-190255/                 | liecio-diamond-found-190255.mp3                                             |
| cargo_heavy_warning  | https://pixabay.com/sound-effects/film-special-effects-warning-sound-6686/                   | freesound_community-warning-sound-6686.mp3                                  |

## 2026-10-25 고침 (B-165, MVP2-fix105 테스트 뒤)

- **jump**: 마리오 같은 "뿅" 느낌이 분위기를 깨서 가벼운 바람 소리(whoosh)로 바꿈. 비교용 후보는 `jump-candidates/`에 있음(A가 기본).
  - A `jump_A_whoosh_end` (0.50s, 음량 0.4): https://pixabay.com/sound-effects/film-special-effects-whoosh-end-384629/ — 사용자가 고른 느낌. 저음이 많아 노트북 스피커에서는 작게 들릴 수 있음
  - B `jump_B_short_simple` (0.45s, 음량 0.5): https://pixabay.com/sound-effects/film-special-effects-short-simple-whoosh-609448/ — 더 밝고 가벼운 "슉"
  - C `jump_C_quick_swhoosh` (0.40s, 음량 0.65): https://pixabay.com/sound-effects/film-special-effects-quick-swhooshing-noise-80898/ — 바로 시작하는 짧은 "휙"
  - B나 C를 쓰려면 그 wav를 `jump.wav`로 이름 바꿔 넣고 음량만 바꾸면 됨
- **land_heavy**: 높은 데서 떨어져 다쳐도 경고음보다 작게 들림 → 파일을 다듬고 음량 0.35 → 0.7. 게임에서 들리는 크기가 위험도 경보(alarm_mine_wail)와 비슷해짐(약 +9dB)
- **pickup_common**: 구리·철 획득음이 블록 부서지는 소리에 묻힘 → 파일을 다듬고 음량 0.35 → 0.65. 엔진 연료(pickup_rare)보다 약간 작게 맞춤(약 +8dB)
- Unity가 World 파일을 가져올 때 normalize(가장 큰 순간을 최대로 맞춤)를 하므로, 파일 자체 음량만 올리면 게임에서 달라지지 않는다. 그래서 낮은 저음을 덜고 리미터로 평소 크기를 올린 뒤 음량 칸 값도 함께 올렸다.
