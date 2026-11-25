# 테스트 실행과 플레이 시작이 겹친 뒤 편집기 소리가 전부 끊겼다

- 날짜: 2026-11-25
- 태그: audio, editor, playmode, test, mcp, b211
- 실수: B-211 확인 중 미뤄 둔 PlayMode 테스트 실행과 MCP 플레이 시작이 겹쳤다. 그 뒤 효과음·배경음이 모두 안 났고, 사용자가 알려 줄 때까지 몰랐다.
- 원인: Console에 `FMOD failed to switch back to normal output ... Error initializing output device (60)`가 떴다. 편집기 오디오 출력이 다시 켜지지 않아 `AudioSettings.dspTime`이 멈춰 있었다. 게임 코드·음량 설정(`subterra.settings.*`)·Game 뷰 음소거는 정상이었다.
- 다음부터 할 일: 플레이·테스트를 끊거나 겹치게 한 뒤에는 Console에서 FMOD 오류를 찾고, `dspTime`을 두 번 재서 흐르는지 확인한다. 멈췄으면 `AudioSettings.Reset(AudioSettings.GetConfiguration())`로 출력을 다시 켠다(편집기를 다시 켜도 된다). 소리 문제를 코드에서 찾기 전에 이것부터 본다.
- 관련: B-211, `2026-11-25-b211-playmode-flag-runs-whole-suite.md`, `2026-11-04-issue158-world-audio-smoke-mute.md`
