# 새 PlayMode 검사가 광산 씬을 남겨 뒤에 도는 Gameplay 엘리베이터 검사 7개가 실패

- 날짜: 2026-10-25
- 태그: testing, playmode, scene
- 실수: 새 PlayMode 검사가 광산 씬을 남겨 뒤에 도는 Gameplay 엘리베이터 검사 7개가 실패
- 원인: 검사 뒤 씬 정리 안 함
- 다음부터 할 일: 실제 씬을 여는 PlayMode 검사는 TearDown에서 빈 씬으로 바꾸고 나머지 씬을 내린 뒤, 이웃 묶음과 함께 돌려 본다
