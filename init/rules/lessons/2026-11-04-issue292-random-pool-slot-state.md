# 무작위 개수 연출은 쓰지 않는 풀 칸도 같은 상태로 맞춘다
- 날짜: 2026-11-04
- 태그: vfx, power, pooling, random, testing, issue292
- 실수: 전력 켜짐 정전기를 2~4개 무작위로 쓰면서 이번에 쓰는 칸에만 sortingLayer·sortingOrder를 넣었다. 4개가 뽑힌 첫 묶음 실행은 통과했고, 단독 실행에서 2~3개가 뽑히자 남은 칸의 정렬값이 0으로 남아 정렬 검사가 실패했다.
- 원인: 풀 칸 초기화를 "보이는 칸" 분기 뒤에 두었고, 무작위 개수에 따라 결과가 바뀌는 검사를 한 번만 돌렸다.
- 다음부터 할 일: 풀 재사용 연출은 공통 상태(정렬·레이어·비활성)를 분기 전에 모든 칸에 넣는다. 무작위 개수·위치가 있는 검사는 단독으로 여러 번 돌려 우연히 통과한 것이 아닌지 확인한다. 연출 난수는 UnityEngine.Random 대신 개별 System.Random을 써서 게임 결정성에 영향을 주지 않는다.
- 관련: #292, B-185, PowerOnEffect.ScheduleSparks, PromptB185PowerOnEffectTests.EffectSortsJustInFrontOfFacility.
