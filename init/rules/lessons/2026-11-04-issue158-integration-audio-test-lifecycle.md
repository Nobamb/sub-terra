# 월드 오디오 통합은 타입 별칭과 명시적 수명주기 호출로 검증한다
- 날짜: 2026-11-04
- 태그: audio, integration, editmode, lifecycle, issue158
- 실수: 통합 바인더에 Gameplay 네임스페이스 전체를 추가해 App의 위험도 enum과 이름이 충돌했다. 새 EditMode 검사에서 SendMessage로 OnDisable을 호출하자 Unity의 ShouldRunBehaviour assertion이 발생했다.
- 원인: App과 Gameplay에 같은 이름의 위험도 타입이 있고, 비실행 EditMode의 엔진 메시지 전달은 일반 런타임 호출과 다르다.
- 다음부터 할 일: 통합 파일은 필요한 Gameplay 컴포넌트만 타입 별칭으로 가져온다. EditMode에서는 reflection으로 수명주기를 직접 호출하고, 실제 SetActive·Pause·UnPause는 PlayMode에서 확인한다.
- 관련: #158, B-184, IntegrationRuntimeBinder, PromptB184SfxWorldLoopTests.
