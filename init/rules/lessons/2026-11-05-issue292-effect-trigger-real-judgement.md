# 시설 켜짐 연출을 실제 게임 켜짐 판정이 아닌 케이블 전력 신호에 붙임
- 날짜: 2026-11-05
- 태그: vfx, power, gameplay, integration, playmode, issue292
- 실수: B-185 켜짐 연출을 `PowerNode.PowerStateChanged`(케이블 연결 + 공급/수요 용량)에 붙였다. 실제 게임에서 충전기·보건소·정산 콘솔이 켜졌는지는 `GameplayEventBridge`의 코어·엘리베이터 반경 판정으로 정하고, 케이블 용량 판정은 거의 켜지지 않아 실제 플레이에서 연출이 전혀 나오지 않았다.
- 원인: 이름이 맞는 신호(`IsPowered`)를 실제 게임 판정으로 가정하고, HUD·CCTV가 쓰는 `BuildFacilityStatuses`까지 거슬러 확인하지 않았다. 확인도 미리보기 씬에서 `Tick`을 직접 돌려서 실제 설치 경로를 한 번도 거치지 않았다.
- 다음부터 할 일: 상태 변화에 붙는 연출·소리는 먼저 "플레이어가 보는 켜짐/꺼짐을 정하는 코드"(HUD·팝업이 읽는 DTO를 만드는 곳)를 찾아 같은 판정에 붙인다. 화면 확인은 Bootstrap → 광산 → 실제 `TryPlaceAt` 같은 실제 플레이 경로의 PlayMode 검사로 하고, 미리보기 씬 결과만으로 완료라고 하지 않는다.
- 관련: #292, B-185, B-185-1, `GameplayEventBridge.RefreshFacilityPowerRange`, `PowerFacility.SetSupplyRangePowered`, `PromptB185PowerOnEffectPlayModeTests`, EmergencyEscapePortal.IsPowered 주석(공급 5 < 수요 30).
