# 표시 이름을 바꾸면 이름에 기대는 검색·정렬 검사도 함께 찾는다
- 날짜: 2026-11-10
- 태그: issue304, building, data, displayName, search, test
- 실수: 시설 이름에서 "기본"을 지우자 초성 `ㄱㅂ` 검색 결과가 비어 정렬 검사(View_SortReordersListOnlyKeepsQueryAndResetsOnClose)가 실패했다. 고치면서 "기본 사다리" 문자열을 직접 쓰는 순수 검색 TestCase까지 일괄 치환할 뻔했다.
- 원인: 데이터 표시 이름을 소비하는 검사를 `"기본 조명"` 같은 완성 문자열로만 찾았고, 초성·부분 검색어처럼 이름에서 파생된 값은 찾지 않았다. 치환 범위를 줄 단위로 확인하지 않았다.
- 다음부터 할 일: displayName을 바꾸기 전에 카탈로그 이름을 쓰는 검사(CatalogItems·GameDataCatalog)에서 검색어·정렬 기대값을 함께 확인한다. 실데이터 기반 검사만 고치고, 문자열을 직접 넘기는 순수 함수 검사는 건드리지 않는다. 일괄 치환 뒤 diff를 줄 단위로 본다.
- 관련: Data/Buildings/*.asset, MvpDataAssetBuilder, PromptB191BuildingModeTests, prompt-B-191-3.
