# Sub-Terra B-179-1 효과음 (설정 음량 슬라이더: 잡는 동안 반복 + 놓으면 끝소리) — 2026-11-02

공통: 44.1kHz 16bit **모노** WAV. 음 높이는 바꾸지 않음. mp3는 들어보기용(192kbps), 게임에는 wav를 넣음.
원본: B-179 `settings_slider_drag.wav`(칸 91, 0.877초, ui_scan_tick을 늘린 것)를 세 조각으로 나눔. 자른 자리는 "웅-웅" 떨림이 가장 작아지는 곳(0.544초, 0.714초) 근처의 0 교차점.

## 넣을 곳
- 게임 파일 4개 → `Assets/_Project/Audio/SFX/UI/`
  - `settings_slider_drag.wav`는 **같은 이름 파일을 덮어씀**(칸 91, 이제 "한 번 짧게" 소리)
  - `settings_slider_drag_head.wav`, `settings_slider_drag_loop.wav`, `settings_slider_drag_tail.wav`는 새 칸 92·93·94
- 이 묶음 전체: `work_process/MVP2/sound/sfx-B179-1/`
- 출처 기록: `address-rows.md`의 줄을 `init/sound-effect-address.md`에 반영(91 줄은 고치고, 92~94 줄은 추가)

| 칸 | 파일 | 길이 | 쓰는 때 | 피크 / RMS |
|---|---|---|---|---|
| 91 | settings_slider_drag.wav | 0.698초 | 방향키·스크롤 등 "한 번" 바뀔 때. 앞+끝을 10ms 교차 페이드로 붙임 | -1.0 / -15.3 dBFS |
| 92 | settings_slider_drag_head.wav | 0.544초 | 끌기 시작: 시작 "탁" + 첫 "웅-웅" | -1.0 / -14.5 dBFS |
| 93 | settings_slider_drag_loop.wav | 0.1696초(7481샘플) | 잡고 있는 동안 반복("웅" 한 번) | -10.4 / -16.5 dBFS |
| 94 | settings_slider_drag_tail.wav | 0.164초 | 손을 뗄 때 끝 꼬리 | -11.7 / -20.3 dBFS |

## 이음새 검사(숫자)
- 반복 조각은 앞 10ms에 원본의 다음 부분(끝 다음 10ms)을 섞어, 끝 → 처음이 원래 소리처럼 이어지게 함.
- 샘플 간 차이: 반복 끝→처음 0.0013, 반복 끝→꼬리 0.0013, 앞→반복 0.0043 (원본 전체 99% 지점 0.0246, 최대 0.0302). 모두 평소 파형 변화보다 훨씬 작음.
- 2차 차분(딸깍 검출) 2ms 최대값: 전체 중앙값 0.0015 / 95% 0.0021. 반복 이음새 0.0004~0.0007(중앙값보다 작음), 앞→반복 0.0031(95%보다 조금 큼, "탁" 소리의 0.0161보다 훨씬 작음). 딸깍 없음.
- 미리듣기: `preview_drag_hold_1.5s.mp3` = 앞 + 반복 6번 + 꼬리(약 1.73초).

## 크기(음량) 측정 — 칸 값 0.55 추천
- 게임 UI 소리 11개(UiClick·Hover·Confirm·CancelFail·PopupOpen·PopupClose·ScanTick·CctvOn·KeycapPop·KeysPageTurn·KeysHoloOpen)의 "가장 큰 0.1초 RMS"(칸 음량 포함) 중앙값 **-15.5dB**, EBU 순간 음량 최대 중앙값 **-19.1 LUFS**. (Unity 임포트의 Normalize로 피크를 0dB로 맞춘 경우와 안 맞춘 경우 둘 다 거의 같음: -15.4/-18.9)
- B-179 소리 @0.13: -29.4dB / -32.6 LUFS → 다른 UI 소리보다 약 **14dB 작음**(사용자 체감 "훨씬 작다"와 일치).
- @0.55: 앞 조각 -16.9dB / -20.1 LUFS(중앙값보다 1~1.5dB 낮음, 예전 틱 UiScanTick@0.5의 -16.3dB와 비슷), 반복 -21.9 LUFS(계속 나므로 한 발 더 낮게). 0.65면 중앙값과 같아짐.
- **네 칸 모두 같은 값**으로 둬야 이음새에서 크기가 튀지 않음.

## 주의(임포트)
- 92·93·94는 **Normalize 끔**(조각마다 피크를 0dB로 올리면 반복이 +10dB, 꼬리가 +12dB 커져 이음새가 튐). 압축은 PCM 권장(짧고, 이어 붙일 때 코덱 앞뒤 여백 없음).
- 91은 기존처럼 둬도 됨(피크 -1dB라 Normalize 영향 1dB).
