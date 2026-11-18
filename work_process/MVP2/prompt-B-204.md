# B-204 전진기지·조명 범위가 빠르게 퍼지는 연출 (#298)

## 목표
전진기지 코어를 설치(또는 범위가 새로 잡힐 때)하면 범위 **표시만** 중심에서 바깥으로 빠르게 퍼진다. 연출 중에도 최종 범위 안이면 전력 시설은 설치 직후부터 지금처럼 바로 쓸 수 있다. 조명을 켠 직후에는 밝아지는 범위가 빠르게 커지고, 범위 표시가 점점 불투명해지며 주변 어둠이 점점 밝아진다. 이미 켜진 조명 범위로 들어가면 지금처럼 바로 밝아진다. 코어가 꺼지거나 범위 밖으로 나갈 때, 조명을 끄는 연출은 이번 범위가 아니다.
기준: https://github.com/Nobamb/sub-terra/issues/298
관련: #292 시설 전력 켜짐 연출, #172 과한 연출 주의, #282 모션 감소.
이 이슈는 Nobamb가 진행한다(연출 포함). 댓글: 「이 이슈는 제가(Nobamb) 작업합니다.」

## 브랜치
1. `git fetch` 후 최신 `origin/main`에서 새 브랜치 **`MVP2-fix148`**를 만든다. 이 문서를 쓸 때의 main은 `0a9a3f403b80a81f8540dd2c3137f7348c8a7c73`이다. (`MVP2-fix147`은 B-203/#291용. 이 번호는 쓰지 않는다.)
2. **다른 프롬프트와의 관계.** B-201(#358)·B-202(#306)과 파일 겹침 없음. `EmergencyRescue*`·`CushionPadHintHologram`·채굴 과녁/`MiningSystem`은 건드리지 않는다. B-203(#291)과 독립적으로 진행해도 된다. `GasVisualRules.cs`를 고칠 때는 가스 등장/페이드 상수(`SpawnDurationSeconds` 등)를 바꾸지 말고, 범위 퍼짐 상수는 이 기능 쪽(예: `PowerSupplyRangeIndicator` 또는 조명 시각 클래스)에 두는 편이 안전하다. 작업 시점의 `origin/main`에서 딴다. 원격에 같은 이름이 다른 커밋으로 있으면 빈 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다.
3. 보고서 맨 위에 기준 커밋 해시를 적는다. 브랜치를 지우지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix148`(또는 실제 쓴 번호)에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다. #298에 댓글·체크박스 수정·닫기를 하지 않는다.
3. 새 음원, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다. 범위 퍼짐에 새 소리를 붙이지 않는다. `PowerOnEffect`의 기존 무음 연출(#292, 충전기·보건소·정산)을 재설계하지 않는다.
4. 큰 화면 전체 모션이 아니면 reduce-motion 분기를 새로 넣지 않는다. 예외: 이 이슈(#282)가 요구하는 범위 퍼짐·어둠이 밝아지는 연출만 `AccessibilityPreferences.ReduceMotion`이면 줄이거나 생략하고 최종 범위로 바로 둔다. 이미 켜진 조명에 들어갈 때의 즉시 밝음, 시설 전력 판정은 그대로다.
5. 프리팹 YAML을 손으로 고치지 않는다. 과하지 않게(#172). 퍼짐은 가스 등장(1초)보다 빠르게, 길이는 상수로 두어 조절 가능하게 한다.
6. 전력·조명 **판정 반지름**(코어 `PowerSupplyRangeIndicator.DefaultRadius` 10, 조명 `GasVisualRules.LightClearRadiusBlocks` 5)과, 이미 켜진 조명에 들어가면 바로 밝아지는 규칙은 바꾸지 않는다.

## 현재 상태 (main `0a9a3f40`) — 아직 미구현
- **코어 범위 표시.** `PowerSupplyRangeIndicator`(`Scripts/Gameplay/Power/`, 파란 `LineRenderer` 원, `RangeColor` 알파 0.25, `DefaultRadius` 10). `Configure`/`OnEnable`이 최종 반지름으로 바로 `RebuildCircle()`. `Contains()`는 이 반지름으로 판정. `GameplayEventBridge.EnsureOutpostRangeIndicators`가 설치 후 `building.outpost_core.basic` 전원 노드에 붙인다. `IsWithinPowerSupplyRange`가 충전기·보건소·정산(`IsProximityPoweredFacility`)의 `PowerFacility.SetSupplyRangePowered`에 쓰인다. 브리지 `OnEnable`은 복원 때 `RefreshFacilityPowerRange(false)`로 **연출 없이** 맞춘다.
- **조명.** `building.light.basic`. 시야 구멍은 `GasVisionClearanceSource`(자식 `GasClearance`, 판정 5칸). 범위 그림은 가장자리 부드러운 붉은 스프라이트(`LightClearRedOpacity` 0.05). 후광은 `LightHaloVisual`(`LightHaloFadeSeconds` 0.15, `ReduceMotion`이면 페이드 없이 바로 켜고 끔). 주석: 켜진 채 화면 밖이었다가 다시 보이면 **바로** 보이게 한다. 암전 구멍은 `DepthDarknessOverlayController.RefreshLightHoles` → `GasVisionHoleEvaluator.CopyViewportLights`가 최종 반지름을 바로 넣는다. `IsCleared` 판정과 시각 구멍은 이미 분리되어 있다.
- 퍼짐 연출(원이 커지거나, 켤 때 어둠이 점점 열리는 것)은 아직 없다. 후광의 짧은 알파 페이드만 있다.

## 할 일
1. **전진기지 코어.** 설치(또는 범위가 새로 잡힐 때)에 `PowerSupplyRangeIndicator` 원만 중심에서 최종 반지름까지 빠르게 퍼지게 한다. `Contains()`와 전력 공급은 연출 중에도 **최종 반지름**으로 판정한다. 복원·이미 있던 코어는 퍼짐 없이 최종 원으로 둔다(`RefreshFacilityPowerRange(false)`와 같은 취지). 끄거나 범위 밖으로 나가는 연출은 넣지 않는다.
2. **조명 켠 직후.** 밝아지는 **시각** 범위가 빠르게 커지게 한다(후광·암전 구멍·`GasClearance` 범위 스프라이트). 범위 표시는 점점 불투명해지고, 주변 어둠은 점점 밝아진다. 가스 시야를 뚫는 `IsCleared`/`Contains` 판정 반지름은 켠 직후 최종값(5칸)을 유지한다. 조명을 끄는 연출은 넣지 않는다.
3. **이미 켜진 조명** 범위로 들어가거나, 켜진 채 화면 밖으로 나갔다 다시 보이면 지금처럼 바로 밝게 둔다. `LightHaloVisual`의 그 즉시 표시 규칙을 깨지 말 것. 저장 복원으로 이미 켜져 있는 조명에도 퍼짐을 다시 돌리지 않는다.
4. 퍼짐 속도는 가스 등장(1초)보다 빠르게(초안 예: 0.25~0.35초). 상수로 두고 보고서에 적는다. `ReduceMotion`이면 퍼짐을 생략하거나 아주 짧게 하고 최종 범위로 바로 간다. #172처럼 요란한 파동·여러 겹 링·큰 화면 플래시는 넣지 않는다. 새 SFX 없음.
5. `PowerOnEffect`(정전기·빛, #292)는 재설계하지 않는다. 범위 퍼짐만 추가한다.
6. EditMode: 코어는 원이 커지는 동안 `Contains(최종 가장자리)`가 참, 복원/모션 감소면 즉시 최종 원. 조명은 켠 직후 시각 반지름이 커지고, 이미 켜진 채 범위에 들어가면 즉시 최종 밝기. 기존 전력·조명 구멍 테스트를 깨지 말 것.

## 테스트 (플레이 가이드)
1. 전진기지 코어를 설치한다.
2. 파란 범위 표시가 중심에서 바깥으로 빠르게 퍼지는지 본다.
3. 연출이 끝나기 전에도 범위 안 충전기·보건소·정산 콘솔에 전력이 바로 들어오는지 본다.
4. 조명을 설치하고 켠다.
5. 켠 직후 범위가 빠르게 커지고, 범위 표시가 불투명해지며 주변 어둠이 점점 밝아지는지 본다.
6. 이미 켜진 조명 범위 밖으로 나갔다가 다시 들어가면, 지금처럼 바로 밝아지는지 본다.
7. 모션 감소를 켜고 코어·조명을 다시 설치/켜서, 퍼짐이 줄거나 생략되고 최종 범위로 바로 가는지 본다.
8. Unity Console에 새 빨간 오류가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 전진기지·조명 범위를 빠르게 퍼지는 연출로 추가 (프롬프트 B-204)`). `MVP2-fix148`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue298-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #298은 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-204-range-spread-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일
  - 퍼짐 상수, 코어 판정(최종 반지름)과 시각 분리, 조명 켠 직후/이미 켠 상태/복원 처리, 모션 감소
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix148` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음 [ ] 코어 범위 표시만 퍼지고 전력은 즉시 [ ] 복원된 코어는 퍼짐 없음 [ ] 조명 켠 직후 시각 범위·불투명·어둠이 빠르게 변함 [ ] 이미 켜진 조명 진입은 즉시 밝음 [ ] 끄기 연출 없음 [ ] ReduceMotion 시 퍼짐 생략/축소 [ ] 과하지 않음, 새 SFX 없음 [ ] PowerOnEffect 미재설계 [ ] 프리팹 YAML 손수정 없음 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] 브랜치 삭제 안 함 [ ] #298 미수정
