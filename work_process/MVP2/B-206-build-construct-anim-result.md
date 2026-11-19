# B-206 건설 시 0.5초 건설 연출 결과

## 기준
- 기준 브랜치·커밋: `origin/main` `f73ea42e4e68ce238f3e16238dff8c70692ac9fe`
- 만든 브랜치: `MVP2-fix150` (원격에 같은 이름 없음, `MVP2-fix15`만 있음)
- 커밋 해시: **없음(미커밋)** — 저장소 규칙상 사용자 동의 전에는 커밋·push하지 않았다.

## 바꾼 파일
- 추가 `Scripts/Gameplay/Building/FacilityConstructionEffect.cs` (연출 재생·정리, 켜짐 연출 대기)
- 추가 `Scripts/Gameplay/Building/FacilityConstructionRecipes.cs` (시설별 연출 데이터, 숫자는 PM 조정용)
- 추가 `Scripts/Gameplay/Power/IPowerOnVisualGate.cs` (Power는 Building을 참조하지 않으므로 인터페이스로 연결)
- 수정 `BuildingPlacementSystem.TryPlaceAt` (새 설치에서만 `Begin`)
- 수정 `PowerFacility` (켜짐 연출을 게이트가 있으면 건설 뒤로 미룸)
- 수정 `PowerSupplyRangeIndicator.BeginSpread(delay)`, `GasVisionClearanceSource.BeginReveal(delay)` (시작만 지연, 기본값 0이라 기존 호출 동일)
- 수정 `GameplayEventBridge.BeginRangeSpread` (남은 건설 시간만큼 지연)
- 테스트 추가 `Tests/EditMode/Gameplay/Building/FacilityConstructionEffectTests.cs`, 테스트 asmdef에 `SubTerra.Gameplay.Power` 참조 추가
- 교훈 `init/rules/lessons/2026-11-19-issue151-tiled-sprite-slicing.md`

## 구현
- 길이 상수 `FacilityConstructionEffect.BuildAnimSeconds = 0.5`.
- 연출은 시각 전용이다. 완성 렌더러는 `Renderer.forceRenderingOff`로만 가리고(`enabled` 불변) 실제 완성 스프라이트를 띠로 잘라 청록 홀로그램에서 원래 색으로 굳히며 조립한다. 끝나면 조각·효과를 지우고 완성본이 그대로 보인다. 바닥 받침은 먼저 놓인 채 둔다.
- 점유·비용·전력·충돌·상호작용·`BuildingPlaced`는 설치 즉시 지금처럼 동작한다. 새 SFX·`SfxId`는 없고 기존 설치음 그대로다.
- 시설별 연출(청록 홀로그램 톤, 화면 전체 플래시 없음):
  | 시설 | 연출 |
  |---|---|
  | 사다리 | 단이 아래에서 위로 하나씩 올라오고 스캔 빛이 훑는다 |
  | 보관함 | 좌우 함체가 맞물리고 뚜껑이 내려앉으며 먼지가 튄다 |
  | 충전기 | 세 줄 코일이 차례로 솟고 스캔 뒤 방전 섬광·번개 줄 |
  | 조명 | 기둥이 솟고 등이 튀어나와 따뜻한 빛이 두 번 깜빡 |
  | 보건소 | 벽 패널이 좌우에서 번갈아 끼워지고 지붕이 얹힌 뒤 녹청 십자 반짝 |
  | 정산 콘솔 | 본체가 올라오고 화면이 위에서 아래로 줄지어 부팅(스캔 선) |
  | 전진기지 코어 | 모듈이 위에서 차례로 내려꽂히고 충격 고리·먼지 |
  | 비상 탈출 포탈 | 바깥 세로 조각이 중심으로 모여 홀을 이루고 고리가 번짐 |
  | 완충판 | 기둥이 자란 뒤 윗면이 내려앉으며 충격 고리 |
- 시각 순서: 건설 0.5초 → 완성본 → `PowerOnEffect`(청록 빛·정전기)와 B-204 범위 퍼짐. 전력 판정은 미루지 않고 켜짐 연출·퍼짐 시작만 미룬다(이동은 범위 이탈→진입이 없으면 켜짐 연출·퍼짐이 원래 없다)(`IPowerOnVisualGate`, `BeginSpread/BeginReveal(delay)`). 대기 중 범위를 벗어나거나 시설이 꺼지면 켜짐 연출을 만들지 않는다.
- 모션 감소: `AccessibilityPreferences.ReduceMotion`이면 연출 없이 완성본·기능 즉시. `PowerOnEffect`의 모션 감소 규칙은 그대로.
- 복원·이어하기: `TryRestoreBuilding`은 `Begin`을 부르지 않아 연출이 없다.
- 이동(사용자 추가 요청): 이동 가능한 시설(조명·정산 콘솔·충전기·보건소·보관함)을 `TryMoveAt`으로 옮기면 새 위치에서 같은 시설별 0.5초 연출을 다시 보여 준다. 이동은 무료이고 점유·전력은 옮긴 즉시 확정된다. 연출 도중 다시 옮기면 앞 연출을 마무리하고 새 위치에서 처음부터 다시 보인다. 모션 감소면 이동도 연출 없이 즉시.
- 사용자 시트 훅은 넣지 않았다(시트 없이 아홉 시설 모두 동작).

## 테스트 결과
- 새 `FacilityConstructionEffectTests` 31개 통과: 아홉 레시피 구분·길이, 시설별 0.5초 뒤 복원(`enabled` 불변·조각 잔존 없음), 마지막 프레임 정합, 남은 시간, 모션 감소, 켜짐 연출 대기/즉시/범위 이탈/비활성, 범위 퍼짐 지연, `TryPlaceAt` 즉시 기능·겹침 불가, 이동 시 연출 재생·연출 중 재이동·모션 감소 이동.
- 새 테스트와 관련 그룹(B185 켜짐 연출, 설치 테스트, 완충판·포탈·시설 시각 등) 201개 통과.
- Unity Console 오류 0.
- EditMode 전체: 2874 통과 / 29 실패. 29개는 UI 레이아웃·디버그 터미널·씬 구성 등이고, 설치 관련 3건(`M_F02`, `M_F04`, `G_F03`)은 내 변경을 stash한 기준선에서도 똑같이 실패해 기존 실패임을 확인했다. 나머지 26건은 이번 변경 범위 밖이라 기준선 재실행은 하지 않았다(**미검증**).
- 전체 실행이 프리팹·`Mine_Demo_Integration.unity`·폰트 등을 더럽혀서 `git restore`로 되돌렸다. 최종 `git status`에는 이번 작업 파일만 남아 있다.
- 시각 확인: 프리뷰 씬에서 아홉 시설의 건설 프레임을 카메라로 렌더해 눈으로 확인했다(사다리는 타일 모드 버그를 찾아 수정).

## 미검증
- 실제 Bootstrap → 광산 플레이에서 아홉 시설을 지어 보는 확인, 저장 후 이어하기, 모션 감소 토글, 설치음·Console 확인.
- 라이트 2D 조명이 있는 실제 광산 화면에서의 색감(프리뷰는 글로벌 라이트 1개).

## 사용자 테스트 가이드
1. Bootstrap에서 시작해 아홉 시설을 각각 지어, 0.5초 건설 연출이 서로 다르게 보인 뒤 완성 그림이 되고, 이어서 전력 켜짐(청록 빛·정전기, 해당 시설)과 범위 퍼짐이 나오는지 본다.
2. 연출이 끝나기 전에도 충전·보관·사다리·완충판 착지가 바로 되는지, 같은 칸에 겹쳐 짓지 못하는지 본다.
3. 모션 감소를 켜고 지으면 연출 없이 완성본이 바로 보이고 기능도 바로인지 본다.
4. 저장 후 이어하기에서는 건설 연출이 다시 안 나오는지, 이동 가능한 시설(조명·정산 콘솔·충전기·보건소·보관함)을 옮기면 새 위치에서 같은 연출이 나오는지 본다.
5. 기존 설치음이 나고 Unity Console에 새 빨간 오류가 없는지 본다.

## 완료 체크리스트
[ ] `MVP2-fix150` push (미실시, 사용자 동의 대기) [ ] 커밋 해시 (미커밋) [x] Unity 컴파일 오류 없음 [x] EditMode 새 실패 없음(기존 29건은 변경 전부터) [x] 아홉 시설 각각 다른 0.5초 연출 후 완성본 [x] 사용자 시트 없이 동작 [x] 기능은 설치 즉시 [x] 건설 연출 다음 PowerOnEffect·범위 퍼짐 [x] ReduceMotion이면 연출 생략 [x] 복원·이어하기 미재생(이동은 사용자 요청으로 재생) [x] 새 SFX 없음 [x] 그림 리뉴얼·30초 주기 연출 안 함 [x] 엘리베이터 미변경 [x] 완성 그림·아이콘 미교체 [x] 프리팹 YAML 손수정 없음 [x] main 직접 커밋 없음 [x] PR 안 만듦 [x] 브랜치 삭제 안 함 [x] #151·#308 미수정
