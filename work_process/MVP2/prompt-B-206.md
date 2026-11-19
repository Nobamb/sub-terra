# B-206 건설 시 0.5초 건설 연출 (#151, 관련 #308)

## 목표
시설을 지으면 완성본이 바로 팝인되지 않고, **시설마다 다른 약 0.5초짜리 빠른 건설 과정 연출**을 보여 준 뒤 실제 건물 그림으로 넘긴다. #151의 미완료 항목 「건설 시에 건물이 바로 등장하는 것이 아닌 각자마다의 연출을 통해서 0.5초동안의 빠른 gif를 통해 건설되는 과정을 보여주면서 생성되는 연출 추가」만 다룬다.
기준: https://github.com/Nobamb/sub-terra/issues/151
관련: https://github.com/Nobamb/sub-terra/issues/308 (그림 리뉴얼·30초 주기 연출은 이번 범위가 아니다. 엘리베이터는 #308대로 제외).
연출은 **구현자 AI가 직접 만든다.** 사용자가 미리 그린 시트에 의존하지 않는다. 각 시설의 **인게임 완성 스프라이트**를 바탕으로, 코드 연출 또는 생성 프레임(발판·조립·청록 홀로그램이 쌓이며 완성되는 식 등)으로 건설 과정을 보여 준다. 시설마다 구분이 있어야 하고, 게임의 청록 홀로그램·산업 시설 분위기(`PowerOnEffect` 청록, 기존 시설 그림)에 맞춘다. 나중에 사용자가 시트를 넣으면 그걸 쓰도록 훅만 짧게 남겨도 되지만, 없어도 아홉 시설 모두 연출이 있어야 한다.

## 브랜치
1. `git fetch` 후 최신 `origin/main`에서 새 브랜치 **`MVP2-fix150`**를 만든다. 이 문서를 쓸 때의 main은 `f73ea42e4e68ce238f3e16238dff8c70692ac9fe`이다.
2. 작업 시점의 `origin/main`에서 딴다. 원격에 같은 이름이 있으면 빈 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다. 브랜치를 지우지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix150`(또는 실제 쓴 번호)에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다. #151·#308 체크박스·댓글·닫기는 하지 않는다.
3. 새 음원, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다. 설치음은 기존 `BuildingPlacementSfx.ResolvePlaced`(`SfxId.BuildPlaceSmall` / `BuildPlaceHeavy`, `GameplayBuildingPlacementBridge`의 `BuildingPlaced`)를 그대로 둔다.
4. **모션 감소 예외(사용자가 지정).** 보통은 큰 화면 전체 모션이 아니면 ReduceMotion 분기를 새로 넣지 않지만, 이번은 예외다. `AccessibilityPreferences.ReduceMotion`이면 건설 연출을 **생략**하고 완성본이 즉시 보이게 한다. 기능도 설치 직후 즉시(아래 4번과 같음). 기존 `PowerOnEffect`의 모션 감소 규칙은 그대로 둔다.
5. 프리팹 YAML을 손으로 고치지 않는다. 설치 판정, 발자국 칸 수, 비용, 전력·조명 판정 반지름, 복원/이동 규칙은 바꾸지 않는다. 시설 완성 그림·아이콘을 교체하거나 `PowerOnEffect`를 재설계하지 않는다. 그림 리뉴얼·30초 주기 연출은 넣지 않는다.

## 현재 상태 (main `f73ea42e`) — 건설 연출은 아직 없음
- **설치.** `BuildingPlacementSystem.TryPlaceAt`이 `RuntimePrefab`을 `Instantiate`하고, 비용 차감·칸 점유·충돌·전력 바인딩·`BuildingPlaced`를 같은 프레임에 끝낸다. 복원은 `TryRestoreBuilding` + `BuildingRestored`. 이동은 `TryMoveAt` + `BuildingMoved`(건설이 아님).
- **인게임 그림(연출의 바탕).** 월드에 실제로 보이는 스프라이트를 쓴다. 조명 `light_basic_cartoon_v3.png`, 보건소 `clinic_basic_cartoon_v3.png`, 정산 콘솔 `settlement_console_cartoon_v3.png`, 전진기지 코어 `outpost_core_cartoon_v3.png`, 포탈 `emergency_escape_portal_v1.png`, 사다리 `ladder_segment_mine.png`. 충전기·보관함 프리팹에는 사선 그림(`charger_basic_cartoon_v2.png`, `storage_basic_cartoon_v2.png`)이 있지만, `FacilityGroundedVisual.ResolveArtwork`가 설치·미리보기에서 정면 `Resources/Facilities/ChargerGrounded`·`StorageGrounded`로 바꾼다. 완충판 월드/미리보기는 `cushion_pad_top.png`+`cushion_pad_post.png`(`SupportPillar.prefab`의 `CushionPad`). 아홉 모두 설치 그림은 있다.
- **전력 켜짐(#292, `PowerOnEffect`).** `PowerFacility.SetSupplyRangePowered`가 꺼짐→켜짐이고 `allowEffect`이며 억제가 아니면 `PowerOnEffect.Play`(0.7초 청록 빛·정전기). 복원은 `BeginSuppression`. `GameplayEventBridge.OnBuildingPlaced`가 설치 직후 `BeginRangeSpread`(B-204)를 돌린다.

## 할 일
1. **새 설치만 연출.** `TryPlaceAt`/`BuildingPlaced`로 새로 지은 아홉 시설 모두. 상수 `BuildAnimSeconds = 0.5`(이슈 값, 조절 가능). 0.5초 동안 해당 시설만의 건설 과정이 보인 뒤 완성 스프라이트로 넘긴다. 복원·이동·이어하기에서는 재생하지 않고 완성본을 바로 둔다.
2. **시설마다 다른 연출.** 공통 페이드/스케일만으로 아홉을 똑같이 만들지 않는다. 완성 스프라이트(위 인게임 그림, 충전기·보관함은 Grounded, 완충판은 윗면+기둥)를 조립·홀로그램으로 쌓거나 드러내는 식으로, 시설 형태에 맞게 나눈다(예: 사다리는 단이 올라오고, 보관함은 함체가 조립되고, 포탈은 홀이 모이며, 완충판은 기둥 후 윗면). 청록 홀로그램·산업 시설 톤을 유지하고, 요란한 화면 전체 플래시는 넣지 않는다. 생성 프레임을 파일로 둘 수도, 런타임 코드로 그릴 수도 있다. (선택, 짧게) 나중에 사용자 시트가 있으면 그걸 우선해도 되지만, 지금은 시트가 없어도 연출이 있어야 한다.
3. **기능은 설치 즉시.** 전력·충돌·상호작용(충전, 보관, 정산, 포탈, 사다리, 완충판 착지 등)은 연출과 관계없이 **설치 성공 즉시** 지금처럼 동작한다. 연출은 시각만. 칸 점유·자원 차감·`BuildingPlaced`도 지금과 같다.
4. **PowerOnEffect와의 시각 순서.** 기능은 이미 켜져 있으므로 전력 판정을 미루지 않는다. 시각만: 건설 연출 0.5초를 먼저 보여 주고, 완성본으로 넘어가는 시점에 `PowerOnEffect`와 B-204 범위 퍼짐을 시작한다(겹치면 청록 빛·정전기와 건설 홀로그램이 싸운다). 구현 시 기존 `BeginRangeSpread`/켜짐 연출 시작 시점만 이 순서에 맞게 조정하되, 모션 감소이거나 복원·이동이면 지금처럼 즉시(건설 연출 없음, PowerOnEffect는 기존 규칙)로 둔다. `PowerOnEffect` 자체의 길이·색·모션 감소는 바꾸지 않는다.
5. **모션 감소.** 켜져 있으면 건설 연출 없이 완성본 즉시 표시. 기능도 즉시. PowerOnEffect는 기존 ReduceMotion 처리를 따른다.
6. EditMode: 새 설치에서 0.5초 연출이 있고 0초에도 기능(점유·전력/상호작용 가능 상태)이 이미 켜져 있는지. 0.5초 뒤에 완성 그림으로 바뀌고 그때 PowerOnEffect가 시작되는지(해당 시설). ReduceMotion이면 연출 없이 즉시 완성 그림. 복원·이동은 연출 없음. 기존 설치·복원·전력 켜짐 테스트를 깨지 말 것.

## 테스트 (플레이 가이드)
1. 아홉 시설을 각각 지어 0.5초 건설 연출이 서로 다르게 보인 뒤 완성 그림이 되는지, 그 다음에 전력 켜짐(청록 빛·정전기, 해당 시설)이 나오는지 본다.
2. 연출이 끝나기 전에도 충전·보관·사다리 이용·완충판 착지 등 상호작용이 바로 되는지, 같은 칸에 겹쳐 짓지는 못하는지 본다.
3. 모션 감소를 켜고 다시 지으면 연출 없이 완성본이 바로 보이고 기능도 바로인지 본다.
4. 저장 후 이어하기, 시설 이동에서는 건설 연출이 다시 안 나오는지 본다.
5. 기존 설치음이 나고, Unity Console에 새 빨간 오류가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 건설 시 0.5초 시설별 연출을 추가 (프롬프트 B-206)`). `MVP2-fix150`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue151-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #151·#308은 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-206-build-construct-anim-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일
  - 시설별 연출 방식, 길이 상수, 기능은 즉시인 점, PowerOnEffect·범위 퍼짐의 시각 순서, ReduceMotion, 복원·이동
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix150` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음 [ ] 아홉 시설 각각 다른 0.5초 건설 연출 후 완성본 [ ] 사용자 시트 없이도 동작 [ ] 기능(전력·충돌·상호작용)은 설치 즉시 [ ] 시각적으로 건설 연출 다음 PowerOnEffect·범위 퍼짐 [ ] ReduceMotion이면 연출 생략, 완성·기능 즉시 [ ] 복원·이동·이어하기 미재생 [ ] 새 SFX 없음, 기존 설치음 유지 [ ] 그림 리뉴얼·30초 주기 연출 안 함 [ ] 엘리베이터 미변경 [ ] 완성 그림·아이콘 미교체 [ ] 프리팹 YAML 손수정 없음 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] 브랜치 삭제 안 함 [ ] #151·#308 미수정
