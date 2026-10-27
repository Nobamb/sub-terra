# Sub-Terra D-2 시설 설치·업그레이드 효과음 (정리본, 2026-10-26)

공통: 44.1kHz 16bit **모노** WAV. 20Hz 아래 저음·직류 성분 제거. 앞 무음 제거(소리 시작 전 약 2.5ms만 남김) + 2ms 페이드인, 뒤는 짧은 페이드아웃. 피크 -1dBFS. 음 높이·속도는 바꾸지 않음.
음량(dBFS)은 피크 / 전체 평균(RMS) / 가장 큰 0.1초 평균.
mp3는 들어보기용(192kbps). 게임에는 wav를 넣음.

## 넣을 곳
- 시설 설치 소리(build_place_small·heavy): `Assets/_Project/Audio/SFX/World/`
- 나머지(설치 실패·레벨업·자물쇠·창 열기/닫기): `Assets/_Project/Audio/SFX/UI/`
- 이 묶음 전체(wav + mp3 + README + address-rows.md): `work_process/MVP2/sound/sfx-D2-build/`
- 출처 기록: `address-rows.md`의 10줄을 `init/sound-effect-address.md` 표 맨 아래에 붙이기.
- unlock_glow_long은 비교용. 들어보고 더 맞는 쪽을 unlock_glow 이름으로 넣으면 됨.
- 지금 SfxId에는 이 소리들을 위한 칸이 없음(마지막 번호 CargoHeavyWarning = 65). 새 칸은 66번부터 뒤에 붙이기.

| 파일 | 쓰는 장면 | 원래 → 새 길이 | 한 일 | 피크 / RMS / 0.1초 최대 |
|---|---|---|---|---|
| build_place_small.wav | 작은 시설 설치 | 0.60s → 0.30s | UI Button Heavy Button Press Metallic 통째로, 뒤 무음 정리, 끝 0.05초 페이드. 짧은 금속 "철컥" | -1.0 / -19.6 / -15.0 |
| build_place_heavy.wav | 큰 시설 설치 | 2.11s → 1.37s | Heavy Door Unlocking 앞 1.4초, 끝 0.3초 페이드. 묵직한 금속 잠금 "쿵-철컹" | -1.0 / -17.1 / -10.8 |
| build_fail_resource.wav | 설치 실패: 자원 부족 | 1.08s → 0.64s | Error Notification 앞 0.72초, 끝 0.2초 페이드. 부드러운 "띵-딩" 두 번(약 350Hz). 지금 쓰는 실패음(UiCancelFail, 거칠고 끊기는 버즈)과 아래 build_fail_blocked(낮게 이어지는 버즈)와 다르게 들림 | -1.0 / -13.9 / -10.3 |
| build_fail_blocked.wav | 설치 실패: 놓을 수 없는 자리 | 0.46s → 0.42s | Low UI Error Buzz 통째로, 뒤 무음만 정리. 낮은 "부-" 버즈 | -1.0 / -15.8 / -10.5 |
| upgrade_levelup.wav | 업그레이드 레벨업(구매 성공) | 1.75s → 0.56s | Digital Success Chime 앞 소리 부분만, 끝 0.15초 페이드 | -1.0 / -16.3 / -12.1 |
| unlock_rattle.wav | 해금 1단계: 자물쇠 흔들림 | 4.06s → 0.67s | Metal drawer 원본 2.25~2.92초의 금속 덜그럭 부분만 | -1.0 / -16.7 / -12.4 |
| unlock_click.wav | 해금 2단계: 딸깍(고리 풀림) | 0.58s → 0.44s | Steampunk Gadget Lock and Unlock, 원본이 작아서 +6dB 올리고 -1dB 리미터(실제 평균 약 +3dB) | -1.0 / -19.8 / -16.6 |
| unlock_glow.wav (기본) | 해금 3단계: 떨어진 뒤 빛남 | 3.46s → 1.00s | shimmer_synth_1 앞 1.05초, 끝 0.4초 페이드 | -1.0 / -16.1 / -10.4 |
| unlock_glow_long.wav | 〃 (비교용, 길게) | 3.46s → 1.80s | 같은 원본 앞 1.85초, 끝 0.35초 페이드. 빛이 오래 남는 연출이면 이쪽 | -1.0 / -18.2 / -10.4 |
| window_tv_open.wav | 창 열기(TV 켜짐): 인벤토리·건설창·퀘스트 상세(업그레이드 창은 모션 감소일 때만) | 1.61s → 0.73s | Retro Computer Startup 1 앞 0.75초, 끝 0.2초 페이드 | -1.0 / -14.9 / -7.8 |
| window_tv_close.wav | 창 닫기(TV 꺼짐): 〃 | 1.38s → 0.56s | UI Modal Close Soft Tap 앞 0.58초, 끝 0.12초 페이드 | -1.0 / -8.4 / -4.9 |
| unlock_sequence_preview.mp3 | 들어보기용만 | 2.15s | 흔들림(0초, -3dB) → 딸깍(0.75초) → 빛(1.0초 버전, 1.15초, -4dB)을 이어 붙인 것 | — |

## 참고
- 자물쇠 해금 순서(심층 구역 썸네일 해금 연출과 맞춤): 흔들림 → 딸깍 → 떨어지며 빛. 심층 썸네일 연출은 전체 1초 기준으로 0~0.2초 흔들림, 0.2~0.3초 딸깍(고리 튀어오름), 0.3~0.65초 떨어짐, 그 뒤 색 퍼짐·빛. 소리를 연출 시점마다 따로 재생하면 됨(흔들림 시작에 rattle, 0.2초에 click, 0.3초쯤 glow). 미리듣기 mp3는 소리끼리 겹치지 않게 간격을 넉넉히 둔 것이라 실제 게임 시점과는 다름.
- unlock_rattle은 0.67초라 연출의 흔들림 구간(0.2초)보다 김. 딸깍이 나올 때 rattle을 끊거나 음량을 줄이면 자연스러움.
- window_tv_close는 평균 음량이 커서(RMS -8.4dB) 칸 값 음량 0.5 안팎부터 들어보기 추천. window_tv_open도 앞부분이 큰 편(0.1초 최대 -7.8dB).
- 창 TV 연출은 지금 여는 데 0.18초, 닫을 때는 연출 없음(#241에서 닫는 연출 추가 예정). 소리는 열기/닫기를 누른 순간에 재생하면 됨.
