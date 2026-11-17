# 같은 머티리얼의 스프라이트가 서로 다른 텍스처를 쓰면 한쪽 그림으로 통일돼 보인다
- 날짜: 2026-11-16
- 태그: sprite, batching, hologram, render
- 실수: 완충판 안내 키(위·아래)의 텍스처를 따로 만들고 같은 `Sprites/Default` 머티리얼을 공유시켰다. 텍스처 픽셀을 읽어 화살표 방향이 맞다고만 확인하고 "수정 완료"라고 보고했는데, 실제 화면에서는 두 키가 같은 화살표로 그려졌다(처음엔 둘 다 ▼, 고친 뒤엔 둘 다 ▲).
- 원인: 같은 머티리얼·같은 sortingOrder의 SpriteRenderer가 한 배치로 묶이면서 한 텍스처로 그려졌다. CPU 쪽 텍스처 데이터는 정상이라 픽셀 읽기로는 발견되지 않았다. 위 키를 끄자 아래 키가 정상(▼)으로 보여서 확인했다.
- 다음부터 할 일: 한 묶음으로 보이는 스프라이트는 한 장의 아틀라스 텍스처를 공유하고, 그림이 다른 렌더러는 머티리얼도 따로 둔다. 방향·모양 같은 그림 문제는 텍스처 데이터가 아니라 실제 카메라 렌더 결과를 캡처해 눈으로 확인한다(렌더러를 하나씩 꺼서 겹침도 점검).
- 관련: CushionPadHintHologram, PlayerCushionPadPlayModeTests.Hint_UpArrowPointsUpAndDownArrowPointsDown
