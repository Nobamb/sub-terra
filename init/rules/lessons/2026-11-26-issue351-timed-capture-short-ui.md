# 몇 초만 뜨는 UI를 MCP 호출을 나눠 캡처하면 이미 닫힌 화면을 찍는다

- 날짜: 2026-11-26
- 태그: playmode, mcp, capture, ui, testing, issue351
- 실수: B-213 산소 경고(6초 뒤 자동 닫힘)를 플레이로 확인하면서 경고를 띄우는 호출과 `ScreenCapture`를 따로 불렀다. 앞 스크린샷을 읽고 다음 호출을 보내는 사이 6초가 지나 터미널이 이미 닫혀(`open=False phase=Hidden`) 빈 화면을 찍었다. 첫 캡처도 열림 연출 도중이라 터미널이 선 하나로만 보였다.
- 원인: MCP 호출 사이 간격(이미지 읽기·생각 시간)이 실제 시간으로 수 초씩 걸린다. 자동 닫힘·타이머가 있는 UI는 그 사이에 상태가 바뀐다.
- 다음부터 할 일: 짧게 뜨는 UI는 한 호출 안에서 띄우고 `EditorApplication.update` 콜백으로 정해 둔 시각(예: 1.6·4.0·6.6초)마다 상태 로그와 `ScreenCapture.CaptureScreenshot`을 남긴 뒤, 끝나고 로그·그림을 한꺼번에 읽는다. 자동 닫힘 시간 확인도 같은 방식으로 한다.
- 관련: #351, B-213, `2026-11-02-issue158-mcp-input-gameview-focus.md`
