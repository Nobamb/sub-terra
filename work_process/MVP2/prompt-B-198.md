# B-198 HUD 전력 게이지 아래 산소 게이지 (#342)

## 목표
HUD에서 전력 게이지 바로 아래에 산소 게이지를 넣는다. 값은 #332/#338의 현재/최대 산소이며, 채움 동작은 전력 게이지와 같다. 색은 살짝 회색 느낌이 나는 탁한 반투명 흰색(불투명도 90%). 기존 HUD 프레임의 게이지 윗 영역과 전체 세로를 늘려 새 게이지가 들어가게 한다.
기준: https://github.com/Nobamb/sub-terra/issues/342

## 브랜치
1. `git fetch` 후 최신 `origin/main`(`e9c9ee8be8f04fd074165111928e1f12d02acd51`, #338 산소 기본 병합)에서 새 브랜치 **`MVP2-fix142`**를 만든다. (`MVP2-fix141`까지 이미 있음. 이 번호는 쓰지 않는다.)
2. 원격에 `MVP2-fix142`가 이미 있으면 비어 있는 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다.
3. 보고서 맨 위에 기준 커밋 해시를 적는다. 브랜치를 지우지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix142`(또는 실제 쓴 번호)에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다. #342에 댓글·체크박스 수정·닫기를 하지 않는다.
3. 새 음원, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다.
4. 산소 소모·회복·가스 배율 규칙을 이 이슈에서 다시 설계하지 않는다. 열린 draft #339(행동·화물 배율)에 의존하지 말고, main의 현재/최대 산소 값만 읽는다. 상태창 「산소 위험도」(안전/주의/위험)는 그대로 두고, 이 작업은 HUD 게이지만 담당한다.
5. 프리팹 YAML을 손으로 고치지 않는다. 레이아웃·이미지는 Unity 에디터에서 수정한다. 다른 HUD 영역(체력/전력 동작, 깊이·골드·화물, 가스 경고, 사이드 메뉴)을 재디자인하지 않는다.

## 현재 상태 (main `e9c9ee8`)
- **HUD.** `Prefabs/UI/HUDCanvas.prefab`의 `HudBinder`가 `Prefabs/UI/BasicHUD.prefab`을 붙인다. 스크립트: `Scripts/App/UI/HUD/HudBinder.cs`, `HudPresenter.cs`, `BasicHudView.cs`, `HudGaugeView.cs`. 전력은 `GameState.EnergyChanged` → `BasicHudView.SetEnergyLevel` → `energyGauge.SetValue(current, max)`. 체력은 `HudBinder.BindHealthSource`(`IPlayerHealthSource`)로 따로 붙고, `EnableDamageTrail()`을 쓴다.
- **전력 게이지 구조.** `BasicHUD` 자식 `EnergyGauge`(`HudGaugeView`) + `EnergyLabel`(「전력」). 게이지 자식: `Empty`(`energy-empty.png`), `Fill`(`energy-bar.png`, Image Type Filled, FillMethod Horizontal, 색 흰색 그대로라 그림 색이 보임), `Frame`(`bar-frame.png`). 아트: `Art/UI/Gameplay/HUD/`.
- **배치 (`BasicHudView.ApplyCompactLayout`, Awake에서 덮어씀).** 루트/`HudFrame` 380×208. 체력 게이지 y=16, 전력 게이지 y=52, 둘 다 x=106, 258×32, 줄 간격 36. 그 아래 `TopSeparator` y=89, 깊이/골드/화물, `BottomSeparator` y=158, `StructuralStatusRow` y=164. 이 숫자를 안 고치면 프리팹만 바꿔도 실행 시 되돌아간다.
- **프레임 그림.** `HudFrame`은 `HUD-frame.png`(1635×930, `spriteBorder` 0, Image Type **Simple**이라 세로만 늘리면 늘어남).
- **산소 값 (#338).** `PlayerOxygenState.Remaining`(double)·`Maximum`(float, 기본 100) in `Scripts/Gameplay/Player/PlayerSurvivalState.cs`. 소모는 `PlayerSurvivalController.AdvanceOxygen`(`OxygenPerSecond` 0.5 + `OxygenPerSecondPerMeter` 0.02×깊이). **컨트롤러에 현재량 public getter는 아직 없다.** `OxygenRisk`/`OxygenRiskChanged`만 있다. `IntegrationRuntimeBinder.BindOxygen`이 상태 줄(`StructuralHudView.SetOxygenRisk`, 「산소 위험도」)만 갱신한다. 산소는 런타임 값이라 `GameState`에 없다. #344(0M~10M 회복)·#345(`O2` 디버그)는 아직 없으니 전제하지 않는다.

## 할 일
- 전력 줄 바로 아래에 산소 줄(라벨 「산소」 + `HudGaugeView`)을 복제한다. 채움은 전력과 같은 `SetValue`(감소 0.5초 보간, 증가 즉시). **체력 잔상(`EnableDamageTrail`)은 쓰지 않는다.**
- 값은 `Remaining / Maximum`. `PlayerSurvivalController`에 읽기 전용 접근자(또는 작은 읽기 모델)를 추가하고, `BindOxygen`이 있는 경로에서 HUD에 연결한다. 소모 중에도 게이지가 따라가려면 이벤트나 얇은 갱신이 필요하다(위험도 이벤트만 쓰면 Safe 구간에선 안 줄어든 것처럼 보인다). #339 API는 쓰지 않는다.
- Fill 색: 살짝 회색 기 있는 탁한 반투명 흰색, 알파 90%. 제안값 `new Color(0.86f, 0.88f, 0.90f, 0.9f)`, **검사기에서 조절**. `energy-bar.png`는 이미 전력 색이 들어 있으니 그냥 쓰면 노란/청록이 비친다. 흰/회색 마스크 스프라이트를 재사용하거나, 자리만 남기고 보고서에 적는다.
- 레이아웃: `ApplyCompactLayout`과 프리팹에서 게이지 윗 영역에 한 줄(기존 체력↔전력 간격 36px을 우선)을 넣고, 그 아래 구분선·수치·상태 줄과 `HudFrame`/루트 세로를 같이 내린다. 예: 산소 y=88, 프레임 높이 208→244. 최종 숫자는 구현에서 맞추고 보고서에 적는다. 전력 게이지·다른 HUD와 겹치면 안 된다. 1920×1080, 1280×720, 16:10에서 확인.
- **프레임 아트는 사용자가 직접 한다.** 구현자는 Rect 공간만 만들고, 그림은 (1) 테두리를 잡아 9-slice로 늘리거나 (2) 새/`HUD-frame.png` 교체용 자리를 분명한 이름으로 남긴다. 보고서에 필요한 픽셀 크기(지금 1635×930 비율로 새 높이를 환산), 게이지가 들어가는 윗 영역을 적는다. 임시로 늘어져 보여도 되고, 다른 HUD 그림을 다시 그리지 않는다.
- 새 SFX 없음. 게이지는 기존 0.5초 보간만 쓰므로 모션 감소 전용 처리는 필요 없다.

## 테스트 (EditMode 추가, 필요하면 PlayMode)
- 현재/최대가 게이지 fillAmount(0~1)와 같다. 감소 보간, 가득/0%, 접근자가 최대 0이어도 안전.
- `ApplyCompactLayout` 이후 산소가 전력 바로 아래이고, 프레임 안에 들어가며 전력과 겹치지 않는다.
- 기존 체력 잔상·전력 게이지·산소 위험도 줄 검사가 깨지지 않는다.

## 사용자 테스트 가이드 (직접 플레이)
1. 광산에 들어가 HUD를 본다. 전력 게이지 바로 아래에 산소 게이지가 있고, 둘이 겹치지 않아야 한다. 라벨은 「산소」. 상태 줄의 「산소 위험도」는 따로 남아 있어야 한다.
2. 더 깊이(10M 이하를 포함해) 내려가거나 오래 머문다. **지금 main은 0M부터 산소가 줄어든다**( #344 회복 구간은 아직 없음). 게이지가 그에 맞게 줄어드는지 본다.
3. `O2` 디버그 명령은 #345가 끝난 뒤에만 쓴다. 아직이면 플레이만으로 확인한다. 가능하면 거의 빌 때까지 가서 게이지가 비는지 본다.
4. 1920×1080, 1280×720, 16:10에서 프레임이 잘리거나 게이지가 밖으로 나가지 않는지 확인한다. 임시 프레임이 늘어져 보이는 것은 아트 교체 전이면 허용한다.
5. 체력/전력 게이지 동작, 화물·골드, 가스 경고가 그대로인지, Unity Console에 새 빨간 오류가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: HUD 전력 아래 산소 게이지 추가 (프롬프트 B-198)`). `MVP2-fix142`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue342-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #342는 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-198-hud-oxygen-gauge-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일
  - 산소 값을 읽은 접근자/연결 위치, 색 최종값, `ApplyCompactLayout` 새 좌표, 프레임을 9-slice했는지/플레이스홀더인지와 사용자가 그려야 할 크기·영역
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix142` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음 [ ] 전력 바로 아래 산소 게이지 [ ] 현재/최대 산소로 채움 [ ] 전력과 같은 보간(체력 잔상 없음) [ ] 색이 탁한 반투명 흰색 90%, 검사기 조절 [ ] `ApplyCompactLayout`·프레임 세로가 새 줄에 맞게 늘어남 [ ] 1920×1080·1280×720·16:10에서 겹침/잘림 없음 [ ] 상태창 산소 위험도 불변 [ ] #339 미의존 [ ] 새 SFX 없음 [ ] 프리팹 YAML 손수정 없음 [ ] 아트 요청을 보고서에 명시 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] 브랜치 삭제 안 함 [ ] #342 미수정
