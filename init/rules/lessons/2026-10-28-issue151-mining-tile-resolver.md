# 광산 연출 검사에서 구리 정의를 찾지 못함

- 날짜: 2026-10-28
- 태그: mining, testing, catalog
- 실수: 광산 연출 검사에서 구리 정의를 찾지 못함
- 원인: 비어 있는 GameDataCatalog.MiningTiles를 실제 채굴 정의로 가정
- 다음부터 할 일: 광산 타일은 MiningSystem에 연결된 MiningTileResolver에서 실제 영구 ID로 찾는다
