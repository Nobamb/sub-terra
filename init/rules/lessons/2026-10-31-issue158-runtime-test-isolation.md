# EditMode에서 Play 상태를 검사할 때 런타임과 마지막 프레임을 격리

- 날짜: 2026-10-31
- 태그: testing, editmode, playmode, audio, isolation
- 실수: B-177의 EnterPlayMode 검사에서 Bootstrap 런타임이 있을 것으로 가정해 첫 실행이 실패했고, 전체 실행에서 기존 애니메이션 검사 두 개가 시간값에 따라 실패했다.
- 원인: Test Framework는 빈 테스트 씬에서 Play에 들어가며, 마지막 Play 프레임의 큰 unscaledDeltaTime이 이후 EditMode 검사에 남을 수 있다.
- 다음부터 할 일: 필요한 런타임은 검사 안에서 독립적으로 만들고 Start·Update와 저장을 막는다. 실제 재생·코루틴 검사는 별도 PlayMode 어셈블리에 두어 EditMode의 시간값을 바꾸지 않는다. 구독·오브젝트를 정리하고 관련 검사와 전체 묶음을 모두 비교한다.
- 관련: #158, B-177, PromptB177SfxMenuTests. 기존 사용자 슬롯과 세이브는 검사에 사용하지 않는다.
