# B-213 산소 부족 드론 경고 (#351)

## 목표
산소가 구출 표식과 **같은 임계값** 아래로 **처음 떨어지는 순간**에만, 드론 말풍선과 TAB 드론 터미널을 빨간 경고로 한 번 띄운다. 왜(산소 부족)와 어떻게(위로 올라가거나 긴급 탈출)를 보여 준다. 조작을 막거나 일시정지하지 않는다.
기준: https://github.com/Nobamb/sub-terra/issues/351. 관련: #332(산소·착지), #342(HUD 산소 게이지), #345(`O2` 디버그, main에 있음), #359(B-207 피격 틴트, 병합됨·이 경고에 쓰지 말 것).
#351은 연출·UI라 Nobamb 담당이다(mink 작업 금지 댓글 있음). 이번 작업은 PM 요청으로 진행한다.

## 브랜치
1. `git fetch` 후 최신 `origin/main`에서 새 브랜치 **`MVP2-fix157`**을 만든다. B-211=`MVP2-fix155`, B-212=`MVP2-fix156`이므로 이 번호들은 쓰지 않는다. 이 문서를 쓸 때의 main은 `a694acacb343053e04cf8f5321e8831d07b76b99`이다.
2. 원격에 같은 이름이 있으면 빈 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다. 브랜치를 지우지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix157`(또는 실제 쓴 번호)에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치를 지우지 않는다.
3. GitHub 이슈 체크박스·댓글·닫기는 하지 않는다(#351, #332, #342, #345, #359 포함).
4. **새 음원, 새 `SfxId`, `SfxLibrary` 변경 없음.** 심각한 긴급 말풍선 소리 `SfxId.DroneBubbleUrgentSerious`만 재사용한다. 터미널을 열 때 기존 Priority 진입이 `DronePriorityAlert`를 내는 것은 그대로 둬도 된다. 같은 경고에서 소리를 추가로 직접 호출하지 말 것.
5. 프리팹·Scene YAML을 손으로 고치지 않는다. 연출은 코드로 만들어 게임 분위기(기존 드론 말풍선·터미널 CRT/글리치 아트)에 맞춘다. Gameplay가 App을 참조하지 않게 한다. 구출 표식 조건·산소 소모·HUD 게이지·`DamageHitGlitchController`(B-207, 낙하·붕괴 피격용 화면 틴트)는 건드리지 않는다.

## 현재 상태 (main `a694acac`)
- **임계값.** `PlayerSurvivalState.GetRisk`: 남은/최대 `<= oxygenCriticalRatio`이면 `OxygenRiskLevel.Critical`. 코드 기본값 **0.15(15%)**. 주의(Caution) 기본 **0.35(35%)**에서는 이 경고를 띄우지 않는다. `PlayerSurvivalSettings.asset`에는 이 필드가 없어 기본값이 쓰인다. 경고도 `settings.OxygenCriticalRatio` / `OxygenRisk == Critical`을 쓴다. 15% 정확히도 Critical이다(`<=`).
- **구출 표식.** `EmergencyRescueService.IsAvailable`은 **전력 0 또는 산소 Critical**이면 참이다(`EmergencyRescueRuntimeController`가 `OxygenRiskChanged`로 갱신, `EmergencyRescueChipView` 「구출 요청」). 전력만 0이고 산소는 임계값 위이면 표식은 뜨더라도 **이 산소 경고는 울리지 않는다.**
- **드론 UI.** 말풍선은 `DroneDialogueSocket.SetDialogue`. 심각한 긴급은 `DroneUrgentBubbleRule.IsSerious`(지금 목록: `DroneEmergency`, `DroneStructuralWarning`, `DroneGasWarning`)일 때 `SfxId.DroneBubbleUrgentSerious`. TAB 터미널은 `HudPanelChromeController.OpenDiggerBot` / `CloseDiggerBot`(`IsDiggerBotOpen`). 지금은 긴급 대사만으로 터미널을 **강제 열지 않는다.** 기존 `DroneEmergency` 문구는 전력·컨텍스트용(`"{reason} {action} 가능 여부를 즉시 확인하세요."`)이라 산소 안내로 재사용하지 말 것.
- **디버그.** 광산 탐사 중 `O2 <n>`(`DeveloperDebugAdvancedCommands.HandleOxygen` → `TrySetDebugOxygenPercent`)이 남은 산소를 n%로 맞추고 `OxygenRiskChanged`를 바로 띄운다. 예: `O2 14` 또는 `O2 15`는 Critical, `O2 16`은 아님, `O2 100`은 회복. 전력만 0으로 만들 때는 `energy 0`. 얕은 곳(깊이 10m 이하)은 산소가 회복되므로, 테스트는 **10m보다 깊은 광산**에서 한다.
- **기존 연출.** 말풍선은 모션 감소 때 펼침·타이핑만 생략하고 내용은 바로 보여 준다(`DroneDialogueSocket`). 터미널에는 이미 열림 글리치 밴드가 있다. B-207 피격 틴트는 산소 0 틱에도 켜지지 않으며, 이 경고의 화면 전체 연출로 쓰지 않는다. 팀 규칙: 모션 감소는 **큰 전체 화면 움직임만**. 이 이슈는 화면 흔들림·전체 글리치를 금지한다. **경고(말풍선·터미널·효과음)는 모션 감소여도 띄운다.** 가장자리 왜곡·후광은 화면 전체가 아니므로 끄지 않는다. 기존 말풍선 모션 감소 동작은 깨지 말 것.

## 할 일
1. **정책(순수 로직).** 산소가 Safe/Caution → Critical로 **내려가는 순간**에만 발사한다. Critical에 머무는 동안 반복하지 않는다. Critical 위로 회복되면 다시 무장하고, 또 떨어질 때 한 번 더 뜬다. 전력 0만으로는 발사하지 않는다. 씬 로드·이어하기 때 **이미 Critical**이면 떨어지는 순간이 아니므로 바로 띄우지 않고, 회복 뒤에만 다시 무장한다. 구출 가능 여부 공식은 바꾸지 않는다.
2. **말풍선·터미널.** 발사 시 빨간 경고 문구의 긴급 말풍선을 띄우고, TAB을 누르지 않아도 `OpenDiggerBot`으로 터미널을 연다. 창 안 문구도 같은 빨간 경고로 맞춘다. 새 대사 템플릿(예: `dialogue.drone.oxygen.warning`)을 두고 `DroneUrgentBubbleRule` 심각한 목록에 넣어 `DroneBubbleUrgentSerious`가 나게 한다. 기본 문장(다듬어도 됨): 「산소가 부족합니다. 위로 올라가거나 긴급 탈출(구출 요청)을 쓰세요.」 분석 루프(`DroneAnalysisService`)에 섞어 매 틱 반복되지 않게, 이 경고 전용 경로로 한 번만 보여 준다.
3. **연출.** 말풍선·터미널 **가장자리만** 왜곡한다. 화면이 흔들리거나 화면 전체가 글리치되면 안 된다. 터미널 둘레에 빨간 후광이 깜빡인다. `DamageHitGlitch` 오버레이를 켜지 않는다. `timeScale`을 바꾸거나 입력·채굴을 막지 않는다.
4. **닫힘.** 강제 연 터미널은 TAB으로 닫거나, 몇 초 뒤 자동으로 닫힌다. 기본값 **6초**(기존 긴급 말풍선 `urgentVisibleSeconds`와 같음). `[SerializeField]`로 검사기에서 조절한다. 이번에 경고로 연(또는 이미 열린 창을 경고로 바꾼) 터미널에만 자동 닫힘을 적용한다.
5. EditMode: 첫 하강 때 한 번, 유지 중 반복 없음, 회복 후 재무장, 전력만 0이면 없음, 로드 시 이미 Critical이면 즉시 발사 없음. 기존 구출·드론·`O2` 테스트를 깨지 말 것.

## 구현 참고
- 컨트롤러는 App에 둔다(예: `EmergencyRescueRuntimeController`처럼 `PlayerSurvivalController.OxygenRiskChanged` 구독). Gameplay에 App 참조를 넣지 않는다.
- 연결은 `IntegrationRuntimeBinder` 등 기존 바인딩 자리에. 구출 표식 갱신 로직을 이 경고와 섞어 깨지 말 것.
- 임계값을 0.15로 하드코딩하지 말고 `OxygenCriticalRatio` / `OxygenRisk`를 따른다.

## 테스트 (플레이 가이드)
1. 10m보다 깊은 광산에서 `O2 14`(또는 `O2 15`)를 입력한다. 머리 위 구출 표식이 뜨는 것과 같은 타이밍에, 빨간 말풍선과 빨간 터미널이 자동으로 한 번 뜨는지 본다. 산소 부족과 위로 올라가거나 긴급 탈출 안내가 보이는지 본다.
2. 경고가 떠 있는 동안 걷고 점프하고 채굴할 수 있는지, 게임이 멈추지 않는지 본다.
3. 말풍선·터미널 가장자리만 왜곡되고 화면 전체는 흔들리지 않는지, 터미널에 빨간 후광이 깜빡이는지, 기존 심각한 긴급 경고음이 나는지 본다. 설정에서 화면 효과 감소를 켜고 같은 경고를 띄워, 경고 자체는 나오고 화면 전체 연출은 없는지도 본다.
4. TAB으로 터미널을 닫을 수 있는지 본다. 다시 떨어뜨린 뒤(아래 6) 그대로 두면 약 6초 뒤 닫히는지를 본다.
5. 산소를 낮춘 채로 더 기다려도 경고가 다시 뜨지 않는지 본다.
6. `O2 100`(또는 10m 위로 올라가 회복) 뒤에 다시 임계값 아래로 떨어뜨려, 한 번 더 뜨는지 본다. `O2 16`만으로는 이 경고가 안 뜨는지도 본다.
7. 산소는 `O2 100`으로 남기고 `energy 0`으로 전력만 0으로 만든다. 구출 표식만 뜨고 이 산소 경고는 안 뜨는지 본다.
8. Unity Console에 새 빨간 오류가 없는지를 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 산소가 위험할 때 드론이 빨간 경고를 한 번 띄운다 (프롬프트 B-213)`). `MVP2-fix157`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue351-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #351 등은 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-213-low-oxygen-drone-warning-result.md`
  - 기준 main 커밋, 만든 브랜치, 커밋 해시, 바꾼 파일
  - 임계값 출처, 발사·재무장 규칙, 자동 닫힘 초, 대사 문장, 사용한 기존 SFX
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix157` push 확인 [ ] 기준 브랜치 기재 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음 [ ] Critical 하강 시 빨간 말풍선·터미널 한 번 [ ] 왜/어떻게 안내 보임 [ ] 유지 중 반복 없음 [ ] 회복 후 재발사 [ ] 전력만 0이면 없음 [ ] 가장자리만 글리치, 화면 전체 연출 없음 [ ] 터미널 빨간 후광 깜빡임 [ ] `DroneBubbleUrgentSerious`만 재사용, 새 효과음 없음 [ ] 조작·일시정지 안 막음 [ ] TAB 또는 검사기 초 후 닫힘 [ ] 모션 감소여도 경고는 나옴 [ ] `DamageHitGlitch`·구출 조건·산소 소모 미변경 [ ] YAML 손수정 없음 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] 브랜치 삭제 안 함 [ ] #351 등 미수정
