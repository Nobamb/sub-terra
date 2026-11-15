# B-196 가스/조명 연출을 더 사실적으로 (#151)

## 목표
유독가스와 그 안의 조명 시야가 반투명한 딱딱한 색 덩어리로만 보이지 않게, 가장자리를 부드럽게 하고 천천히 움직이게 한다. 가스 안에서는 주변이 가스 색으로 살짝 물들고 플레이어 시야가 조금 탁해진다.
기준: https://github.com/Nobamb/sub-terra/issues/151 체크 항목 **「가스/조명 연출도 좀 더 사실적으로 묘사해보기」**. 판정·피해·구역 크기는 그대로다.

## 브랜치
1. `git fetch` 후 최신 `origin/main`(`6c9743a506e61166e15efe93f0ae5a7977c6f4ac`, #323 병합. 그 안에 #321/B-194, #322, #320, #319 포함)에서 새 브랜치 **`MVP2-fix140`**을 만든다. (`MVP2-fix139`는 B-195 긴급 탈출 워프용. 원격에 아직 없어도 이 번호는 쓰지 않는다.)
2. 원격에 `MVP2-fix140`이 이미 있으면 비어 있는 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다.
3. 보고서 맨 위에 기준 커밋 해시를 적는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix140`에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다. #151에 댓글·체크박스 수정·닫기를 하지 않는다.
3. 새 음원, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다. 기존 `GasFlowLoopAudio`·`GasPulseAudio`는 그대로 둔다.
4. 시각만 바꾼다. `GasZone.Radius`/`Contains`, `CircleCollider2D` 반지름, `GasVisualRules.GasRadiusBlocks`(5)·`LightClearRadiusBlocks`(5), 생성 1초·접근 페이드 시간, 피해·이동 감속·노출 판정, `GasVisionHoleEvaluator.TryGetNearestLight`/`IsCleared`의 판정 반지름은 바꾸지 않는다. 부드러운 가장자리는 기존 반지름 **안쪽**에서만 흐리게 한다. 시각이 판정 범위보다 커지면 안 된다.
5. `GasType`은 지금 `Toxic`만 있다. 붉은 영역을 두 번째 가스로 만들지 않는다. HUD 중독 비네트(`GasPoisonOverlayView` 등)는 이번 범위가 아니다.
6. 프리팹 YAML을 손으로 고치지 않는다. 런타임·셰이더·빌더를 우선한다. `PromptB50GasVisionBuilder`를 다시 돌려도 딱딱한 예전 모습이 되살아나면 안 된다.
7. 줄 번호는 `6c9743a5` 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 현재 상태 (main `6c9743a5`)
- **녹색 = 유독가스 구름.** `GasZoneVisual`이 자식 `GasCloud`(`GasVisualRules.GasCloudChildName`)에 `SpriteRenderer`를 붙인다. 색은 고정 `GasColor`(0.22, 0.82, 0.34), 불투명도 `GasVisualOpacity`(0.70)×`SpawnProgress`, `sortingOrder = 20`, `VisibleOutsideMask`. 지름은 `zone.Radius * 2 * progress`(기본 반지름 5칸 → 지름 10). 스프라이트는 에디터 내장 `UI/Skin/Knob.psd`, 없으면 64px 원(가장자리가 거의 1px라 커지면 칼같이 보인다). `GasType`별 색 분기는 없다.
- **붉은색 = 가스가 아니라 조명 시야 구멍.** `LightFacility` 자식 `GasClearance`(`LightClearanceChildName`) `SpriteRenderer`: 같은 Knob, 색 (1, 0.12, 0.08), 알파 `LightClearRedOpacity`(0.05), `sortingOrder = 22`, 지름 10(5칸). 가스에 접근하면 `GasVisionWorldVeil`(월드, `sortingOrder = 80`, 카메라를 덮는 사각형 스프라이트, 셰이더 `SubTerra/GasVisionVeil`) 또는 UI `GasVisionOverlayDriver`(`SubTerra/GasVisionOverlayUI`)가 화면을 어둡게(0.04, 0.05, 0.05, 접근 불투명 0.35→0.95) 덮고, 조명 5칸을 `step(distance, radius)`로 **칼같이** 뚫어 같은 붉은 5%만 남긴다. `GasVisionHoleEvaluator.Sample`도 안/밖이 딱딱하다.
- **조명·암전.** 프로젝트에 URP `Light2D`는 없다. 깊이 암전은 `DepthDarknessOverlayController`(셰이더 `SubTerra/DepthDarknessOverlayUI`): 플레이어 주변 구멍 `playerVisibleRadius` 0.11(뷰포트), `edgeFeather` 0.04. 조명 안이면 `GasVisionClearanceSource.IsCleared`로 `IsClearedByLight`가 켜져 암전이 걷힌다. 조명 시설의 `PowerGlow`는 흰 스프라이트(알파 0.28, `sortingOrder` 3)일 뿐 2D 라이트가 아니다.
- 기존 검사: `GasZoneSpawnAndRangeTests`, `GasVisionHoleEvaluatorTests`, `PromptB50GasVisionTests`.

## 할 일
### A. 녹색 유독가스
- 딱딱한 원/사각 덩어리를 없앤다. 중심만 진하고, 바깥으로 부드럽게 사라지게 한다. 유독가스는 공기보다 무겁게 **바닥 쪽에 고이고**, 천장 쪽은 옅게 한다(지금 타입이 `Toxic`뿐이라 이 방향을 기본으로 둔다. 나중에 타입이 늘면 검사기/데이터로 고르게).
- 연기/노이즈 텍스처 2~3장을 서로 다른 느린 속도로 스크롤한다. 그 위에 떠다니는 입자를 조금 띄운다. 입자는 풀링하고 개수 상한을 검사기에 둔다(권장: 구역당 소수, 전역 상한).
- 색 정체성은 녹색을 유지하되 채도를 낮춘다. 기본 불투명 상한은 지금 70%를 넘기지 않는다.
- 생성 1초 동안 중심에서 커지는 타이밍은 유지하되, 커지는 모양도 부드러운 구름이어야 한다.
- 새 아트가 필요하면 `sub-terra/Assets/_Project/Art/VFX/Gas/`에 심리스 흑백 노이즈(예: `gas-noise-01.png`, `gas-noise-02.png`)를 넣고, 셰이더는 기존처럼 `sub-terra/Assets/_Project/Visuals/`(예: `GasCloud.shader`)에 둔다. 모든 구역이 머티리얼을 나눠 쓰지 말고 공유하거나 `MaterialPropertyBlock`을 쓴다.

### B. 붉은 조명 구멍
- `GasClearance` 스프라이트와 베일/오버레이 셰이더의 `step` 칼자국을 부드러운 감쇠로 바꾼다. 중심은 지금처럼 약하게 붉은 빛이 남고, 5칸 경계로 갈수록 스며들게 한다. 구멍 판정 반지름(5칸)과 `IsCleared`는 그대로다.
- 붉은색 정체성은 유지하되 채도를 낮춘다. 이 영역은 조명이므로 바닥에 고이게 하지 않는다.
- `GasVisionHoleEvaluator.Sample`의 시각 샘플이 있으면 감쇠와 맞춘다. 가장 가까운 조명 **여부·반지름** 판정은 유지한다.

### C. 가스 안 조명·시야
- 생성 연출이 끝난 가스 안에 있으면, 주변이 가스 녹색으로 살짝 물들고 플레이어 시야/광원 구멍이 조금 탁해지게 한다. `DepthDarknessOverlayController`의 플레이어 구멍과, 켜져 있는 가스 베일/오버레이를 재사용한다. 암전·조명 해제 판정 자체는 바꾸지 않는다.
- 선택: 플레이어가 들어가면 연기가 살짝 밀려나 보이게 한다. 이동·피격 판정에 힘을 가하지 않는다. 강도는 검사기, 기본은 약하게.
- 탁해짐·틴트 강도도 검사기. 화면이 갑자기 번쩍이면 안 된다.

### D. 성능·접근성
- 화면 밖 구역은 스크롤·입자를 멈추거나 꺼서 비용을 줄인다.
- 모션 감소(`AccessibilityPreferences.ReduceMotion`): 이번은 작은 환경 움직임이므로 **느린 스크롤·입자는 유지**한다. 큰 번쩍임·화면 전체 펄스는 넣지 않는다.
- 모든 수치(감쇠, 바닥 치우침, 스크롤 속도, 입자 상한, 틴트·탁도, 밀어내기)는 검사기에서 조절.

## 테스트 (EditMode 추가, 필요하면 PlayMode)
- `GasZone.Contains`·콜라이더·기본 반지름 5, 조명 구멍 판정 반지름 5, 피해·감속·노출 틱이 이전과 같다. 시각 감쇠를 넣어도 판정 범위가 커지지 않는다.
- 생성 1초·접근 페이드 시간, `IsCleared` on/off가 이전과 같다.
- 구름 머티리얼이 구역마다 복제되지 않고, 입자 수가 상한을 넘지 않는다.
- 기존 `GasZoneSpawnAndRangeTests`, `GasVisionHoleEvaluatorTests`, `PromptB50GasVisionTests` 등에서 색·스프라이트만 가정한 조건은 새 연출에 맞게 고친다. 빌더 재적용 후에도 부드러운 가장자리가 유지된다.

## 사용자 테스트 가이드 (직접 플레이)
1. 광산에서 유독가스를 띄운다. 1초 동안 퍼지되, 예전처럼 칼같은 원/네모가 아니라 가운데가 진하고 가장자리가 부드러운 녹색 연기인지 본다. 바닥 쪽이 더 진하고 천장 쪽은 옅어야 한다. 구역 크기·피해가 시작되는 거리는 예전과 같다.
2. 가까이 서서 연기가 천천히 흐르고, 입자가 조금 떠다니는지 본다. 채도는 예전보다 덜 쨍해야 한다.
3. 가스 안으로 들어간다. 주변이 녹색으로 살짝 물들고 내 주변 밝기가 조금 탁해지는지 본다. 들어가면 연기가 살짝 밀려도 되지만, 캐릭터가 밀리거나 피해 간격이 바뀌면 안 된다.
4. 가스 근처의 조명 시설로 간다. 붉은 시야 구멍이 칼같이 잘리지 않고 가장자리가 부드러운지 본다. 조명 5칸 안에서 암전이 걷히는 판정은 예전과 같다.
5. 가스가 화면 밖으로 나가면 연출이 과하게 남지 않는지, 여러 구역이 있어도 끊기지 않는지 본다.
6. 설정에서 모션 감소를 켜고 1~3을 다시 본다. 느린 흐름은 남아 있고, 화면이 번쩍이면 안 된다.
7. 가스가 사라진 뒤 색 틴트·탁함이 바로 걷히는지, 인벤토리·체력·전력 수량이 연출 때문에 더 깎이지 않는지, Unity Console에 새 빨간 오류·노란 경고가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 가스·조명 연출을 더 사실적으로 (프롬프트 B-196)`). `MVP2-fix140`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue151-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #151은 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-196-gas-lighting-visuals-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일·줄
  - 녹색/붉은색을 어디에 어떻게 바꿨는지, 새 아트·셰이더 경로, 검사기 기본값, 모션 감소 처리, 판정 반지름을 유지한 방식, 빌더 재적용 결과
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix140` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음(가스 판정·조명 구멍 검사 포함) [ ] 녹색 구름 부드러운 감쇠·바닥 고임 [ ] 노이즈 2~3장 스크롤+입자 상한 [ ] 붉은 조명 구멍 부드러운 감쇠 [ ] 채도 낮춤·녹/적 정체성 유지 [ ] 가스 안 살짝 틴트·탁한 시야 [ ] 선택 밀어내기가 판정에 영향 없음 [ ] 구역·콜라이더·구멍 판정 반지름 불변 [ ] 피해·감속·생성 1초 불변 [ ] 머티리얼 공유·화면 밖 비용 감소 [ ] 모션 감소여도 느린 흐름 유지, 번쩍임 없음 [ ] 새 SFX 없음 [ ] 빌더 재실행 후에도 연출 유지 [ ] 프리팹 YAML 손수정 없음 [ ] 수량 불변 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] #151 미수정
