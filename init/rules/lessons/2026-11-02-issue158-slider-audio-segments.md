# 슬라이더 반복은 입력 수명과 음원 조각의 샘플 경계를 함께 맞춤
- 날짜: 2026-11-02
- 태그: audio, slider, dsp, pcm, ugui, issue158
- 실수: B-179는 긴 드래그에도 원샷 한 번만 내서 유지 피드백이 끊겼고, 음량 0.13이 다른 메뉴 소리보다 작았다.
- 원인: 중복 재생 방지와 잡는 동안의 지속 재생은 별개이며, 앞·반복·끝을 조각별로 정규화하면 원본 이음새 음량도 달라진다.
- 다음부터 할 일: 반복 전용 AudioSource를 DSP 샘플 시간으로 연결하고, 놓을 때 다음 반복 경계에 끝소리를 예약한다. 짧은 조각은 PCM·Normalize 끔으로 임포트한다.
- 다음부터 할 일: 정확한 반복 경계의 double 반올림도 검사한다. 현재 Unity AudioImporter의 정규화는 공개 속성이 아니라 확인된 직렬화 속성 m_Normalize로 설정한다.
- 다음부터 할 일: 창 닫힘·음소거·포커스 상실에서는 예약된 반복과 끝소리까지 취소하고, 다음 드래그가 정상 시작되는지 검사한다.
- 관련: #158, B-179-1, SliderDragLoopAudio, PromptB1791SfxFeedbackFixTests, PromptB1791SfxFeedbackFixPlayModeTests.
