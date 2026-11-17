# B-201 긴급탈출 R키를 각진 홀로그램으로 (#358)

## 목표
긴급탈출(구출 요청) 표식 아래 R키만, #349 완충판 높이 힌트의 위·아래 방향키와 같은 각진 청록 홀로그램 키캡으로 바꾼다. 글자는 계속 R이다. 표식이 뜨는 조건·타이밍·R키/클릭 동작은 그대로 둔다. 모션 감소가 켜져 있으면 키캡은 누르는 동작 없이 정지 그림이다. 새 효과음은 넣지 않는다.
기준: https://github.com/Nobamb/sub-terra/issues/358
이 이슈는 Nobamb 담당이다(연출·UI).

## 브랜치
1. `git fetch` 후 최신 `origin/main`(`0a9a3f403b80a81f8540dd2c3137f7348c8a7c73`, #370 광산 메뉴 우선순위 병합)에서 새 브랜치 **`MVP2-fix145`**를 만든다. (`MVP2-fix144`까지 이미 있음. 이 번호는 쓰지 않는다. 원격에 `MVP2-fix145`가 이미 같은 SHA로 만들어져 있으면 그 브랜치를 이어서 쓴다.)
2. 원격에 `MVP2-fix145`가 다른 커밋으로 이미 있으면 비어 있는 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다.
3. 보고서 맨 위에 기준 커밋 해시를 적는다. 브랜치를 지우지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix145`(또는 실제 쓴 번호)에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다. #358에 댓글·체크박스 수정·닫기를 하지 않는다.
3. 새 음원, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다. 기존 SFX만 쓴다. 표식 등장 소리 `SfxId.RescueHoloOpen`은 그대로 둔다.
4. 큰 화면 전체 모션이 아니면 reduce-motion 분기를 새로 넣지 않는다. 예외: 이 이슈의 R키 유도 누름은 #349 완충판 키와 같이, `AccessibilityPreferences.ReduceMotion`이 켜져 있으면 정지 그림이어야 한다. 키캡 누름/펄스에만 적용하고, 표식 등장·접힘·픽토그램 호버·호흡(`breathe`)에는 새로 넣지 않는다.
5. 프리팹 YAML을 손으로 고치지 않는다. 표식은 코드로 생성되므로 기존 생성 경로만 수정한다.
6. 표식 틀(「구출 요청」 문구, 사람·화살표 픽토그램, 머리 위 위치, 전력 0 또는 산소 Critical일 때 뜨는 조건)과 R키/클릭으로 구출 창이 열리는 동작은 바꾸지 않는다.

## 현재 상태 (main `0a9a3f40`)
- **R키(아직 둥근 키캡).** `EmergencyRescueChipView`(`Scripts/App/UI/EmergencyRescue/`)가 UI `Image`로 만든다. `EmergencyRescueArt.RoundedRect()`(모서리 반경 9의 9-slice)를 `KeycapTop`/`KeycapSide`/`KeycapRim`에 쓰고, TMP 글자 `DefaultKeyLabel` = 「R」을 얹는다. 색은 진한 청록 `KeyTop`(0.06, 0.17, 0.22), `KeySide`(0.02, 0.07, 0.10)이라 완충판 키와 닮지 않는다. 위치는 `EmergencyRescueChipFollow`, 등장은 `EmergencyRescueRuntimeController`(구출 가능: 전력 0 또는 산소 위험). 등장 시 `SfxId.RescueHoloOpen`.
- **누름 안내.** 등장 뒤 `EmergencyRescueTimeline.KeycapFirstDelay`(1.8초) 후 3~5초마다 `KeycapPulse`(0.34초)로 약하게 내려간다(`KeycapIdleDrop` 3, 실제 입력은 `KeycapPressDrop` 6). `TickKeycap`/`Apply`는 `AccessibilityPreferences.ReduceMotion`을 보지 않아 유도 누름이 계속 돈다. EmergencyRescue 폴더에 ReduceMotion 참조가 없다.
- **완충판 각진 홀로그램(#349, main에 있음).** `CushionPadHintHologram`(`Scripts/Gameplay/Player/CushionPadHintHologram.cs`). 코드로 그린 청록 키캡: 모서리 `Chamfer=6`인 팔각 윗면 + 아래 `Depth=5` 옆면, `KeySize=40`, 면 `FaceX0=3`~`FaceX1=36`, `FaceY0=8`~`FaceY1=36`. 가장자리 `Color32(72,246,240)`, 옆면 `(16,118,132)`→`(6,62,78)`, 안쪽은 세로 그라데이션과 가로 주사선. 월드 `SpriteRenderer` 아틀라스(위 키·아래 키·파티클). 윗면 글리프는 화살표(이 R키에는 쓰지 말 것). 누름: `CycleSeconds` 1.6, `PressSeconds` 0.4, `PressDepth` 0.05. `AccessibilityPreferences.ReduceMotion`이면 누름과 파티클을 생략하고 정지 그림만 둔다. 등장 소리 `SfxId.KeysHoloOpen`(표식에는 새로 붙이지 말 것). 설정은 `Scripts/Shared/AccessibilityPreferences.cs`.
- **테스트.** `PromptB85EmergencyRescueChipTests`, `PromptB135EmergencyRescueUiTests`가 키캡을 검사하면, 둥근 사각형을 전제로 한 부분은 갱신한다. 완충판 힌트 테스트는 깨지 말 것.

## 할 일
1. 바꾸는 것은 표식 아래 R키 모양뿐이다. 제목 「구출 요청」, 픽토그램, 위치, 등장 조건, `DefaultKeyLabel` 「R」, 클릭/R로 구출 창이 열리는 동작은 그대로.
2. 완충판 방향키와 같은 각진 홀로그램 키캡으로 맞춘다(깎인 팔각 윗면 + 옆면 두께, 같은 청록 홀로그램 색). 글자는 R(기존 TMP 또는 그린 글자). 화살표 글리프는 넣지 않는다.
3. `CushionPadHintHologram`의 키캡 그리기(`ShadeTopFace`/`ShadeSide`/`Chamfer`/아틀라스)를 재사용하거나 공통 코드로 묶어 모양·색이 어긋나지 않게 한다. 표식은 UI `Image`+캔버스 경로를 유지하고, 월드 홀로그램으로 바꾸거나 팔로우를 재설계하지 않는다. 사용한 클래스·파일 이름을 보고서에 적는다.
4. 기존처럼 약한 유도 누름과 실제 입력 시 더 분명한 눌림을 유지한다. `ReduceMotion`이 켜져 있으면 유도 누름·키캡 바운스를 멈추고 정지 그림만 둔다. 실제 R/클릭은 계속 동작해야 한다.
5. 새 효과음 없음. `SfxId.RescueHoloOpen`은 그대로. 표식에 `KeysHoloOpen`을 추가하지 않는다.
6. EditMode: 키캡이 `RoundedRect` 반경 9가 아닌 각진 홀로그램인지, 글자가 R인지, ReduceMotion이면 정지인지, 등장·입력은 불변인지. 기존 칩 테스트가 둥근 키캡을 가정하면 수정한다.

## 테스트 (플레이 가이드)
1. 광산에서 전력을 0으로 만들거나, 산소를 위험 수준 아래로 떨어뜨린다. `O2 14`(또는 15% 바로 아래)로 확인해도 된다.
2. 머리 위 구출 요청 표식이 뜨고, 아래 R키가 완충판 위의 방향키와 같은 각진 홀로그램으로 보이는지 나란히 비교한다(모양·색).
3. R을 누르거나 표식을 클릭해 구출 창이 기존처럼 열리는지 확인한다. 표식의 다른 부분(문구, 픽토그램, 뜨는 시점)은 그대로여야 한다.
4. 모션 감소를 켜면 R키가 누르지 않고 멈춰 있는지 확인한다. 모션 감소를 끄면 유도 누름이 다시 보이는지 본다.
5. 표식이 뜨고 닫힐 때 기존 소리만 들리고, 새 효과음이 없는지 확인한다.
6. Unity Console에 새 빨간 오류가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 긴급탈출 R키를 각진 홀로그램 키캡으로 교체 (프롬프트 B-201)`). `MVP2-fix145`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue358-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #358은 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-201-emergency-rescue-angular-key-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일
  - 재사용한 클래스·스프라이트·공통 코드 위치, 키캡 그리는 방식
  - 모션 감소를 적용한 위치, 건드리지 않은 표식 모션
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix145` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음 [ ] R키가 완충판과 같은 각진 홀로그램(모양·색) [ ] 글자 R, 등장 조건·입력 동작 불변 [ ] 표식 틀(문구·픽토그램) 미변경 [ ] ReduceMotion 시 키캡 정지, 꺼지면 유도 누름 유지 [ ] 새 SFX 없음, RescueHoloOpen 유지 [ ] 프리팹 YAML 손수정 없음 [ ] 사용한 클래스·파일명을 보고 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] 브랜치 삭제 안 함 [ ] #358 미수정
