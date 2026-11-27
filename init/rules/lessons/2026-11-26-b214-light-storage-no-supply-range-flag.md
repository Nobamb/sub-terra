# 조명·보관함은 IsInSupplyRange가 영영 false라서 주기 연출이 한 번도 안 나왔다

- 날짜: 2026-11-26
- 태그: power, vfx, playmode, b214
- 실수: B-214 주기 연출의 "켜짐"을 문서대로 `IsInteractionAvailable || IsInSupplyRange`로 봤다. EditMode 테스트는 `SetSupplyRangePowered`를 직접 불러 통과했고, 실제 플레이 확인 없이 "적용 완료"라고 보고했다. 사용자가 설치해 보니 30초가 지나도 아무 연출이 없었다.
- 원인: `GameplayEventBridge.IsProximityPoweredFacility`가 충전기·보건소·정산기에만 `IsInSupplyRange`를 채운다. 조명·보관함은 범위와 무관하게 동작해서 케이블 없이 범위 안에 지으면 `IsInSupplyRange`도 `IsInteractionAvailable`도 계속 false다.
- 다음부터 할 일: 문서의 전력 조건 필드가 시설 종류마다 실제로 채워지는지 브리지 코드를 읽어 확인한다. 테스트가 값을 손으로 넣으면 이 연결을 검증하지 못하므로, Bootstrap→광산 Play Mode에서 시설을 `TryPlaceAt`으로 설치해 상태(`CanPlay`)를 조회하는 확인을 꼭 한다. 완료 보고에 "플레이 미검증"을 숨기지 않는다. 새 연출 전용 값은 판정 필드를 바꾸지 않고 따로 추가한다(`PowerFacility.IsInVisualSupplyRange`).
- 관련: B-214, `FacilityIdleAnimation.IsPowered`, `GameplayEventBridge.RefreshFacilityPowerRange`
