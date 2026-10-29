# 새 VFX의 EditMode 비활성화 검사가 실패

- 날짜: 2026-10-28
- 태그: testing, editmode, lifecycle
- 실수: 새 VFX의 EditMode 비활성화 검사가 실패
- 원인: 일반 MonoBehaviour는 EditMode에서 OnDisable을 자동 호출하지 않음
- 다음부터 할 일: 격리 검사에서는 수명주기를 명시하고 실제 비활성화는 PlayMode에서도 확인
