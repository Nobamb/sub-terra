# 비실행 EditMode 검사에서 비활성화 수명주기를 명시적으로 진행한다
- 날짜: 2026-11-04
- 태그: audio, sfx, editmode, testing, lifecycle, issue158
- 실수: 새 화물 효과음 검사에서 SetActive(false) 뒤 OnDisable이 실행됐다고 가정하고 Tick을 호출해 넘침음 무음 검사가 실패했다.
- 원인: 일반 MonoBehaviour는 비실행 EditMode에서 런타임의 OnDisable 호출을 그대로 보장하지 않는다. 창이 여전히 Opening인 상태를 테스트가 인위적으로 진행했다.
- 다음부터 할 일: EditMode 수명주기 검사는 OnDisable을 명시적으로 호출하고, 실제 활성화·비활성화와 자동 Update 동작은 별도 PlayMode 검사로 확인한다. 검사 중 스크립트를 수정하면 도메인 리로드로 실행기의 비동기 테스트 검색 콜백이 유실될 수 있으므로 실행이 끝난 뒤 수정한다.
- 관련: #158, B-182, PromptB182SfxCargoTests, PromptB182SfxCargoPlayModeTests.
