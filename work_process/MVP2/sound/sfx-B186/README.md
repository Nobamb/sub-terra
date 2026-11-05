# Sub-Terra B-186 효과음 — 2026-11-05

## 상태: PM이 직접 받아 넣음
Pixabay가 자동 다운로드를 막아(HTTP 403) PM이 브라우저로 받아 `power_on_static.wav`를 넣었다.
Unity 길이 0.816초, 에셋 음량 0.8(기본값 0.4에서 PM이 올림 → `SfxLibraryDefaults`도 0.8로 맞춤).
받은 파일이 MP3 데이터였던 것을 2026-11-05에 진짜 PCM wav(모노, 48kHz 16bit, 0.816초, 최고점 -1.4dBFS)로 변환했다(소리·길이 그대로, 스테레오는 평균으로 모노).

## 출처
| 항목 | 내용 |
|---|---|
| 칸 | 106 PowerOnStatic |
| 파일 | `Assets/_Project/Audio/SFX/World/power_on_static.wav` |
| Pixabay 페이지 | https://pixabay.com/sound-effects/film-special-effects-short-electric-zap-561890/ |
| 작성자 | blww |
| 라이선스 | Pixabay Content License |
| 원본 파일명 | 미확인 (short electric zap) |
| 자른 구간 | 미확인 (PM이 받은 그대로, 0.816초) |
| 결과 길이·모노·최고점 | 0.816초(Unity 읽기), 모노, 최고점 -1.4dBFS |

## 원래 목표 규격(참고)
- 44.1kHz 16bit **모노** WAV, 앞 무음 제거.
- 길이 약 0.5~0.6초(길면 정전기가 또렷한 앞부분만). 처음 약 0.35초에 "지직"이 몰리고 약 0.6초 안에 끝나게(연출 시간표와 맞춤).
- 끝 0.05초 페이드, 최고점 -1dBFS 근처.
- 버스 World, 음량 0.4(`SfxLibraryDefaults`, 0.3~0.5 범위에서 조정 가능).
- 넣은 뒤 Unity가 import하면 `SfxLibrary.asset`의 106번 칸에 clip이 자동으로 연결된다(`PromptB156SfxLibraryBuilder`).
- 넣은 뒤 이 README와 `init/sound-effect-address.md`의 `power_on_static` 줄에 작성자·자른 구간·길이를 채운다.
