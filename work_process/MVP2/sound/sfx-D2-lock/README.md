# #252 업그레이드 창 자물쇠 연출 효과음 (2026-10-26)

연출: MVP2-fix108 (f71535a8, B-167). 소리 연결은 B-169. 파일은 모두 모노 44.1kHz 16bit, 피크 -1dBFS. UI 폴더.

| 훅(UpgradeWindowMotion) | 시점 | 소리 | 길이 | 시작 음량 |
|---|---|---|---|---|
| ShakeStarted | 처음 열기 0.15초 | unlock_rattle(76, 기존) | 0.3초에서 끊김 | 칸 값 그대로 |
| CrackStarted | 처음 0.45초 / 다시 0.08초 | unlock_click(77, 기존) + upgrade_lock_crack(새, **처음 열기만**) | 0.44 / 0.60초 | 칸 값 / 0.25 |
| Shattered | 처음 0.75초 / 다시 0.2초 | upgrade_lock_shatter(새) | 0.78초 | 0.55 |
| CloseStarted | 닫기 0초 | upgrade_window_suck_in(새) | 0.38초(끝으로 갈수록 커짐) | 0.35 |
| FragmentsRegathered | 닫기 0.2초 | upgrade_lock_regather(새, 부서짐을 거꾸로) | 0.25초(끝에 "착") | 0.5 |

- 원본: glass-cracking-504028(DRAGON-STUDIO), glass-break-3-102271(freesound_community), spooky-steel-reverse-89390(freesound_community). 주소는 address-rows.md.
- 미리듣기: preview_open_full.mp3(처음 열기 1.2초 흐름), preview_open_quick.mp3(다시 열기), preview_close.mp3(닫기). 위 시작 음량으로 섞은 것.
- 크기 비교(가장 큰 0.1초 RMS): shatter -10.3dB, crack -14.1dB, suck_in -13.6dB, regather 약 -14dB, 기존 unlock_click -16.6dB. 그래서 금 가는 소리는 0.25로 딸깍 밑에 깔고, 부서짐을 가장 크게 둠.
- 닫기는 0.3초라 빨려 들어감(0~0.38초)과 조각 모임(0.2~0.45초)이 겹친다. 빨려 들어감을 작게(0.35) 두고 조각 모임 끝의 "착"이 마지막에 들리게 함. 조각 모임 "착"은 연출이 끝난 뒤 약 0.15초에 나온다. 늦게 느껴지면 B-169에서 조각 모임 소리를 닫기 0.05초에 미리 내는 쪽으로 바꿀 수 있음.
