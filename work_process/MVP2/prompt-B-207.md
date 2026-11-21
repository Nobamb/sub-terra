# B-207 일반 피해 시 짧은 붉은 글리치·틴트 (#359)

## 목표
낙하 등 **일반 피해**를 받는 순간, 아주 짧게 붉은 글리치와 옅은 붉은 화면 틴트가 나타났다가 바로 돌아온다. 조작은 막지 않는다. 가스 피해는 기존 시야 연출만 유지하고 이번 효과를 겹치지 않는다. 산소 0 체력 틱(`OxygenDepleted`)은 **완전히 제외**(붉은 글리치·틴트 없음). 산소 전용 연출은 나중에 따로 한다.
기준: https://github.com/Nobamb/sub-terra/issues/359
관련: #345 디버그(`falling`·`hitgas`·`O2`). 캐릭터 「맞을 때」 모션은 #264에서 따로, 이번은 화면 연출만.
이 이슈는 연출·다듬기라 Nobamb가 진행한다. 최신 댓글: 산소 0 틱은 이 이슈에서 완전히 빼기로 했고, 본문도 그에 맞게 고쳤다.

## 브랜치
1. `git fetch` 후 최신 `origin/main`에서 새 브랜치 **`MVP2-fix151`**를 만든다. 이 문서를 쓸 때의 main은 `0c51ec15c858428362aa4f4cf55b0e03b4492cc8`이다.
2. 작업 시점의 `origin/main`에서 딴다. 원격에 같은 이름이 있으면 빈 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다. 브랜치를 지우지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix151`(또는 실제 쓴 번호)에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다. #359 체크박스·댓글·닫기는 하지 않는다.
3. 새 음원, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다. 기존 피격음·착지음(`ResolveHurtSfx`, 낙하의 `LandHeavy` 등)은 그대로 둔다.
4. 이번은 화면 전체 연출이다. 이슈대로 `AccessibilityPreferences.ReduceMotion`이면 **글리치 왜곡(흔들림·슬라이스·지터)은 끄고**, 옅은 붉은 틴트만 남긴다. 카메라 흔들림은 이미 모션 감소 때 꺼진다(`PlayerCameraFollow.RequestShake`).
5. 프리팹 YAML을 손으로 고치지 않는다. 피해 수치, 무적 시간, 가스 시야 연출, 사망/워프 글리치 본편은 재설계하지 않는다. Gameplay가 App을 참조하지 않게 한다.

## 현재 상태 (main `0c51ec15`)
- **피해 진입.** 모든 체력 깎임은 `PlayerSurvivalController.ApplyDamage` → `PlayerSurvivalState.TryApplyDamage`(무적 `InvulnerabilitySeconds` 기본 0.75초). 실제 깎인 뒤에만 `StartDamageFeedback`과 `ResolveHurtSfx`.
  - `RunFailureCause.Fall` — `ApplyFall`(낙하 높이, 사다리·완충판 보정). 피격음은 없고 착지음이 맡는다.
  - `RunFailureCause.GasExposure` — `ApplyGasFailure`. 피격음 `SfxId.PlayerHurt`.
  - `RunFailureCause.OxygenDepleted` — `AdvanceOxygen`, 약 1초마다(`OxygenDamageInterval`) `OxygenDepletionDamage` 기본 5.
  - `RunFailureCause.StructuralCollapse` — `ApplyCollapse` / `ApplyCollapseImpact`. `StructuralCrackOverlay` 등에서 여전히 호출된다. `SfxId.PlayerHurt`.
  - `RunFailureCause.PowerDepleted`는 열거에만 있고, 지금 `ApplyDamage`로 체력을 깎는 경로는 없다.
- **기존 피격 반응.** `StartDamageFeedback`: `PlayerCameraFollow.RequestShake`(피해 비율로 진폭 0.12~0.5, 길이 약 0.18~0.38초, 모션 감소면 생략)와 무적 동안 캐릭터 스프라이트 깜빡임. **전체 화면 붉은 글리치·틴트는 없다.** 원인과 관계없이 체력이 깎이면 흔들림·깜빡임이 나간다(가스·산소 틱 포함). 이번은 화면 연출만 추가하고, 이 기존 반응은 그대로 둔다.
- **가스 시야(바꾸지 말 것).** `GasExposureEffectController`, `GasVisionOverlayDriver`, `GasPoisonVignetteTexture`, `GasVisionWorldVeil`, `GasPoisonOverlayView`/`Timeline`. 가스 안에 있을 때의 흐림·비네트·장막을 수정하지 않는다.
- **참고할 글리치(그대로 옮기지 말 것).** 사망/워프: `EscapeWarpOverlay`, `RunFailureGlitchArt`(스캔라인·글리치 조각, 마스크는 흰색이고 `Image.color`로 칠함). 창 가장자리: `GlitchPopupChrome`. 짧게 재사용해도 되지만, 화면을 오래 깨거나 워프 암전·창만 깨는 연출을 통째로 복사하지 않는다.

## 할 일
1. **발동 원인(이것만).** `ApplyDamage`에서 체력이 실제로 깎였을 때(`appliedDamage > 0`):
   - **켠다:** `Fall`, `StructuralCollapse`(이슈 8: 일반 피해와 같게 취급. 붕괴 경로가 빠지면 따라 정리).
   - **켜지 않는다:** `GasExposure`(기존 가스 시야만), `OxygenDepleted`(글리치도 틴트도 없음). `PowerDepleted`는 현재 체력 경로가 없으니 넣지 않는다.
2. **연출.** 짧게 붉은 글리치 + 옅은 붉은 화면 틴트, 바로 옅어져 사라진다. 초안 상수(검사기에서 조절, 보고서에 적는다): `HitGlitchDuration = 0.28`(이슈 0.2~0.4초 안), `HitTintMaxAlpha = 0.22`, `HitGlitchStrength = 0.28`(`EscapeWarpTimeline.MaxGlitchIntensity` 0.55보다 약하게). `RunFailureGlitchArt` 스캔라인·조각 등을 붉은색으로 짧게 써도 된다. 조작(이동·점프·채굴)은 막지 않는다.
3. **연속 피격.** 이미 켜져 있으면 **처음부터 짧게 다시** 시작할 뿐, 남은 시간을 더해 오래 붉어지면 안 된다. 길이는 항상 `HitGlitchDuration`을 넘지 않는다.
4. **모션 감소.** 왜곡 글리치는 끄고 옅은 붉은 틴트만(같은 길이, 지터 없음).
5. **기존 유지.** 카메라 흔들림·캐릭터 깜빡임·피격/착지음은 원인별 지금 규칙을 유지한다. 가스 시야 클래스·텍스처는 수정하지 않는다. 사망/워프 글리치 본편은 재설계하지 않는다. 치명타에서 짧은 피격 연출과 사망 글리치가 겹치면, 피격 쪽을 짧게 두고 사망 연출이 이어지게 하고 보고서에 적는다.
6. 화면 오버레이는 App/통합 쪽에 두고, Gameplay `PlayerSurvivalController`는 바인딩이나 이벤트로만 알린다(App 참조 금지). 프리팹 YAML 손수정 없음.
7. EditMode: `Fall`/`StructuralCollapse`면 시작 시 틴트·(모션 감소가 아니면) 글리치가 있고, `Duration` 뒤에는 0. `GasExposure`·`OxygenDepleted`는 틴트·글리치가 0. 연속 재생 시 길이가 쌓이지 않음. ReduceMotion이면 왜곡 0, 틴트만. 기존 무적·흔들림·피격음 테스트를 깨지 말 것.

## 테스트 (플레이 가이드)
1. 높은 곳에서 떨어지거나 디버그 `falling 30m`로 낙하 피해를 받는다(#345: 확인은 `falling`. `fall`·`hit`은 빠짐). 짧게 붉은 글리치와 살짝 붉은 화면이 보이고 곧 원래대로인지, 그동안 이동·점프가 되는지지 확인한다. 기존 착지음은 그대로인지 본다.
2. 연달아 낙하 피해를 여러 번 받아, 화면이 점점 더 오래 붉어지지 않는지 본다.
3. `hitgas`(또는 가스 안으로)로 가스 피해를 받는다. 기존 가스 시야만 보이고 이번 붉은 글리치·틴트는 없어야 한다.
4. `O2 0`으로 산소를 비운 뒤, 체력이 틱으로 깎일 때 붉은 글리치와 틴트가 **전혀** 없는지 본다.
5. 모션 감소를 켜고 1을 다시 한다. 화면이 크게 일그러지지 않고 옅은 붉은 틴트만 남는지 본다.
6. 새 효과음이 없고, 기존 착지음·피격음이 그대로인지, Unity Console에 새 빨간 오류가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 일반 피해에 짧은 붉은 글리치와 틴트를 추가 (프롬프트 B-207)`). `MVP2-fix151`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue359-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #359는 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-207-damage-hit-glitch-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일
  - 발동/제외 원인, 최종 상수, 연속 피격·모션 감소, 가스 시야 미변경, 기존 흔들림·깜빡임·SFX 유지
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix151` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음 [ ] 낙하·붕괴 피해 시 짧은 붉은 글리치+틴트, 검사기 조절 [ ] 가스는 기존 시야만, 붉은 효과 없음 [ ] 산소 0 틱은 글리치·틴트 없음 [ ] 조작 미차단 [ ] 연속 피격 시 길이 누적 없음 [ ] ReduceMotion이면 왜곡 없이 틴트만 [ ] 새 SFX 없음, 기존 피격·착지음 유지 [ ] 기존 흔들림·깜빡임 유지 [ ] 가스 시야 클래스 미변경 [ ] Gameplay→App 참조 없음 [ ] 프리팹 YAML 손수정 없음 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] 브랜치 삭제 안 함 [ ] #359 미수정
