# Sub-Terra B-183·B-184 효과음 — 2026-11-02

공통: 44.1kHz 16bit **모노** WAV. 한 번 나는 소리는 최고점 -1dBFS. 반복 소리는 크기를 정해 둔 값 그대로(최고점 맞춤 안 함).
mp3(`preview/`)는 들어보기용(192kbps, 반복 소리는 약 6초 이어 붙임). 게임에는 wav를 넣음.
이 묶음 전체: `work_process/MVP2/sound/sfx-B183-184/` (한 번만 넣으면 B-183·B-184가 같이 씀)
출처 기록: `address-rows.md` (ui_slot_select 줄은 고치고 나머지는 붙임)

## 파일
| 프롬프트 | 칸(SfxId) | 파일 | 길이 | 버스 | 추천 음량 | 넣을 폴더 | 자른 방법 |
|---|---|---|---|---|---|---|---|
| B-183 | 16 UiSlotSelect(있는 칸) | ui_slot_select.wav | 0.300초 | UI | 0.8 → **0.65** | SFX/UI | 188052 원본 0.020~0.320초("딸"+"깍"+짧은 쇠 울림), 끝 0.12초 페이드 |
| B-183 | 95 EmptySlotNoiseLoop | empty_slot_noise_loop.wav | 2.000초 반복 | UI | 0.4 | SFX/UI | 152056 원본 10.1초부터 2초, 0.25초 교차 페이드 반복, RMS -24dBFS |
| B-183 | 96 DronePriorityAlert | drone_priority_alert.wav | 0.600초 | UI | 0.5 | SFX/UI | 515666 원본 0~0.60초, 끝 0.08초 페이드 |
| B-183 | 97 DroneBubbleUrgent | drone_bubble_urgent.wav | 0.230초 | World | 0.6 | SFX/World | 491835 원본 0.080~0.310초("삐빅" 두 번, 약 1975Hz) |
| B-183 | 98 DroneBubbleUrgentSerious | drone_bubble_urgent_serious.wav | 0.230초 | World | 0.6 | SFX/World | 위와 같은 자리에서 "삐"·"빅"을 각각 **7반음 낮춤**(약 1318Hz). 박자·길이는 그대로 |
| B-184 | 99 ScanMarkClick | scan_mark_click.wav | 0.100초 | World | 0.4 | SFX/World | 569825 원본 0.088~0.188초(짧은 "찰칵"), 끝 0.03초 페이드 |
| B-184 | 100 GasFlowLoop | gas_flow_loop.wav | 4.000초 반복 | World | 0.45 | SFX/World | 411772 원본 2.0초부터 4초(가장 고른 구간 ±0.3dB), 0.5초 교차 페이드, 원래 크기 그대로(RMS -22.3dBFS) |
| B-184 | 101 UnstableBlockShakeLoop | unstable_block_shake_loop.wav | 1.500초 반복 | World | 0.5 | SFX/World | 379298 원본 1.7초부터 1.5초, 0.25초 교차 페이드, RMS -22dBFS |

## 크기 측정(피크 / RMS / 가장 큰 0.1초)
| 파일 | 피크 | RMS | 최대 0.1초 | 추천 음량 적용 시 최대 0.1초 |
|---|---|---|---|---|
| ui_slot_select | -1.0 | -16.3 | -11.9 | @0.65 → -15.6 |
| empty_slot_noise_loop | -12.9 | -24.0 | -23.5 | @0.4 → -31.5 (배경처럼 작게) |
| drone_priority_alert | -1.0 | -11.5 | -9.7 | @0.5 → -15.7 |
| drone_bubble_urgent | -1.0 | -13.3 | -12.6 | @0.6 → -17.0 |
| drone_bubble_urgent_serious | -1.0 | -13.3 | -12.4 | @0.6 → -16.8 |
| scan_mark_click | -1.0 | -20.8 | -20.8 | @0.4 → -28.8 (여러 개가 이어지므로 작게) |
| gas_flow_loop | -7.7 | -22.3 | -21.6 | @0.45 → -28.5 |
| unstable_block_shake_loop | -8.8 | -22.0 | -20.5 | @0.5 → -26.5 |
- 기준: 게임 UI 소리 11개의 "가장 큰 0.1초" 중앙값 약 -15.5dB(B-179-1 측정). 한 번 소리는 그 근처, 반복 소리는 계속 나므로 더 낮게 잡음.

## 반복 이음새 검사(숫자, 두 번 이어 붙여 측정)
| 파일 | 이음새 50ms 크기 − 평균 | 이음새 샘플 차이 ÷ 평균 차이 | 이음새 2차 차분 ÷ 전체 99.9% |
|---|---|---|---|
| empty_slot_noise_loop | +0.3dB | 0.19 | 0.82 |
| gas_flow_loop | +0.0dB | 1.49 | 0.55 |
| unstable_block_shake_loop | +0.7dB | 2.69 | 0.22 |
- 세 개 모두 이음새의 2차 차분이 평소 소리의 99.9% 값보다 작음 → 딸깍 튐 없음. 크기 차이도 1dB 미만.
- 직접 들어 보지는 않았음(숫자로만 확인).

## 낮은 "삐빅" 만드는 법
- 491835 "삐빅"을 통째로 늦추면(음 높이 내림) 길이도 1.5배로 늘어남. 그래서 "삐"와 "빅"을 따로 7반음 낮추고(1.5배 느리게 읽기), 원래 길이만큼만 자르고 8ms 페이드, **원래 자리에 다시 놓음** → 박자·길이(0.23초)가 같고 음만 완전5도 낮음(1975Hz → 1318Hz로 측정).
- 7반음(완전5도)을 고른 이유: 6반음은 불협화음이라 "고장"처럼 들릴 수 있고, 7반음은 낮고 무겁지만 같은 소리 가족으로 들림. 더 낮게/높게 바꾸고 싶으면 다시 만들어 줄 수 있음.

## 주의(임포트)
- 95·100·101(반복 3개)은 **Normalize 끔 + PCM**, `loop = true`인 따로 둔 AudioSource에서 틀 것. Normalize가 켜지면 반복 소리가 +8~13dB 커짐.
- 한 번 소리(16·96~99)는 최고점 -1dB라 Normalize 영향은 1dB 정도.
- `ui_slot_select.wav`는 비어 있던 칸 16을 채움(새 칸 아님). 넣으면 ui_click 대신 이 소리가 남.
