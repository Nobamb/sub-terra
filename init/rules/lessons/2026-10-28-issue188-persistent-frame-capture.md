# 해금 연출 캡처가 Unity 종료 후 사라짐

- 날짜: 2026-10-28
- 태그: testing, rendering, animation
- 실수: 해금 연출 캡처가 Unity 종료 후 사라짐
- 원인: 증거를 종료 시 정리되는 Temp 폴더에 저장
- 다음부터 할 일: 화면 증거는 work_process에 보관하고 캡처 지연이 자세를 건너뛰면 수동 틱으로 핵심 자세를 고정
