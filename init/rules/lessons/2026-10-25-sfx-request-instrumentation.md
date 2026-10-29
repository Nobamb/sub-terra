# B-166 원인 로그를 SfxPlayer.PlayNow에만 달아 격리 런타임 검사의 요청이 하나도 안 남음

- 날짜: 2026-10-25
- 태그: audio, testing, diagnostics
- 실수: B-166 원인 로그를 SfxPlayer.PlayNow에만 달아 격리 런타임 검사의 요청이 하나도 안 남음
- 원인: 슬롯 0 격리 런타임에는 SfxPlayer가 없음
- 다음부터 할 일: 효과음 원인 로그는 SfxRequests.PlayRequested 구독(요청 지점)에서도 남긴다
