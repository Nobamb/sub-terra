# B-207 일반 피해 시 짧은 붉은 글리치·틴트 결과 (#359)

## 기준
- 기준 브랜치·커밋: `origin/main` `0c51ec15c858428362aa4f4cf55b0e03b4492cc8` 위의 B-207 문서 커밋 `9322ff84`
- 작업 브랜치: `MVP2-fix151`. 원격 `origin/MVP2-fix151`은 B-207 문서 커밋(`9322ff84`)만 있는 이 작업용 브랜치라 그대로 썼다.
- 커밋 해시: **없음(미커밋)**. 저장소 규칙상 사용자 동의 전에는 커밋·push하지 않았다.

## 바꾼 파일
- 수정 `Scripts/Gameplay/Player/PlayerSurvivalController.cs`: `DamageApplied(cause, appliedDamage, becameIncapacitated)` 이벤트를 추가했다. `ApplyDamage`에서 체력이 실제로 깎였을 때(`appliedDamage > 0`)만 낸다. App 참조는 없다.
- 추가 `Scripts/App/UI/DamageFeedback/DamageHitGlitchTimeline.cs`: 설정(`DamageHitGlitchSettings`)과 순수 시간표. 발동 원인을 고르고 틴트·왜곡 곡선을 만든다.
- 추가 `Scripts/App/UI/DamageFeedback/DamageHitGlitchOverlay.cs`: 런타임에 만드는 전체 화면 Canvas. 틴트, 가장자리 비네트, 붉은 스캔라인, 조각 5개로 이뤄진다.
- 추가 `Scripts/App/Integration/DamageHitGlitchController.cs`: `DamageApplied`를 받아 시간표·오버레이를 돌린다(`LateUpdate`, unscaled 시간).
- 수정 `Scripts/App/Integration/RunFailureRuntimeController.cs`: `Bind`에서 같은 오브젝트의 `DamageHitGlitchController`를 찾고, 없으면 붙여서 생존 컨트롤러에 연결한다. `Unbind`에서 해제한다.
- 테스트 추가 `Tests/EditMode/App/UI/PromptB207DamageHitGlitchTests.cs`
- 교훈 `init/rules/lessons/2026-11-21-issue359-new-script-missing-from-compile.md`
- 씬·프리팹·SFX·ProjectSettings는 바꾸지 않았다.

## 구현
- **켜는 원인:** `Fall`, `StructuralCollapse`(`ApplyCollapse`·`ApplyCollapseImpact`, 디버그 `falling` 포함). 모두 `ApplyDamage`를 지난다.
- **제외:** `GasExposure`(기존 가스 시야만), `OxygenDepleted`(글리치·틴트 모두 없음), `PowerDepleted`·`Unknown`. 무적 시간에 막힌 피해는 이벤트가 나가지 않는다.
- **최종 상수(검사기 `DamageHitGlitchController > Settings`):**

  | 항목 | 값 | 검사기 범위 |
  |---|---|---|
  | `hitGlitchDuration` | 0.28초 | 0.2~0.4 |
  | `hitTintMaxAlpha` | 0.22 | 0~0.5 |
  | `hitGlitchStrength` | 0.28 | 0~0.55, `EscapeWarpTimeline.MaxGlitchIntensity` 이하로 고정 |

  색: 틴트 `(0.86, 0.05, 0.07)`, 글리치 `(1, 0.20, 0.22)`, 보조 `(0.55, 0.02, 0.06)`.
- **곡선:** 시작 직후 길이의 12% 동안 틴트를 최대로 둔 뒤 부드럽게 0으로 줄인다. 왜곡은 `strength × (1−t)^1.5`이고 0.04초마다 조각 위치가 바뀌며 깜빡인다(0.65~1배). 가장자리 비네트는 틴트 알파의 0.9배다.
- **연속 피격:** 켜져 있을 때 다시 맞으면 처음부터 다시 시작한다. 남은 시간을 더하지 않아서, 마지막 피격으로부터 항상 `Duration` 안에 끝난다. 끝 판정에는 float 오차용 0.0005초 여유를 둔다.
- **모션 감소:** `AccessibilityPreferences.ReduceMotion`을 매 프레임 읽는다. 켜져 있으면 왜곡(스캔라인·조각·지터)이 0이고, 같은 길이의 옅은 틴트·비네트만 남는다.
- **조작 미차단:** 오버레이에 `GraphicRaycaster`가 없고 모든 그래픽이 `raycastTarget=false`다. 입력 잠금도 하지 않는다.
- **정렬:** Canvas `sortingOrder = 650`(HUD 600 위, 드론 말풍선 700·모달·사망 창 31,500 아래).
- **치명타:** 피격 연출은 같은 0.28초로만 켜진다. 바로 이어지는 사망 연출(`RunFailurePanelView`, 31,500)이 위를 덮고, 사망 연출은 바꾸지 않았다.
- **기존 유지:** 카메라 흔들림, 캐릭터 깜빡임, `ResolveHurtSfx`, 착지음, 무적 시간, 가스 시야 클래스·텍스처, 사망·워프 글리치는 그대로다. 새 SFX·`SfxId`도 없다.
- **검사기 조절:** 씬에 컴포넌트가 없으면 런타임에 붙는다. 그래서 플레이 중에는 `RunFailureRuntimeController` 오브젝트의 `DamageHitGlitchController`에서 조절하고, 값은 플레이가 끝나면 사라진다. 값을 고정하려면 그 오브젝트에 컴포넌트를 미리 추가하면 된다(`GetComponent`를 먼저 쓴다). 이 경우 `Mine_Demo_Integration.unity` 변경은 규칙대로 별도 커밋으로 둔다.

## 테스트
- Unity 컴파일 오류 0, Console 빨간 오류 0.
- EditMode `PromptB207DamageHitGlitchTests`: 16개 통과. 다룬 항목은 다음과 같다.
  - 기본 상수
  - Fall·StructuralCollapse의 시작 틴트·왜곡과 Duration 뒤 0
  - Gas·Oxygen·Power·Unknown 제외
  - 연속 피격 시 길이가 누적되지 않음
  - ReduceMotion이면 왜곡 0과 틴트만
  - 검사기 값 범위 제한
  - 오버레이가 입력을 막지 않음
  - 실제 `PlayerSurvivalController`의 낙하·붕괴·가스·산소 틱과 무적 차단
- 관련 묶음(B-195 워프, B-195-1 웜홀, B-168 SFX, B-200 산소, Debug 전체, PhaseL 실패 정적): 171개 통과, 5개 실패. 실패 5개는 모두 기존 실패다. 변경을 `git stash`로 빼고 같은 Debug 묶음을 돌려도 같은 5개가 실패했다.
  - `PromptB125AdvancedCommandTests.Help_ListsAdvancedCommands_AndSourcesStayGated`: help 문구 `timer ...`
  - `PromptB125TerminalTests.Enter_ConfirmsCandidateBeforeExecuting`와 `Terminal_ShiftArrowsSelectAndWrapCandidates_ForTabOrEnter` ×2: `hp`·`hitgas` 후보 순서
  - `PromptB193DebugCommandTests.Falling_CushionUsesConfiguredRatioAfterCargo`(1.7, 0.25): 4.25 기대, 4 나옴
- EditMode 전체와 PlayMode는 돌리지 않았다(전체 실행이 씬·프리팹을 다시 저장하는 문제, #342·#306 교훈). **미검증.**
- **실제 플레이 확인: 미검증.** 화면 모양, 체감 세기, 이동·점프 중 표시, 사망 연출과 겹치는 모습은 아래 가이드로 확인이 필요하다.

## 사용자 테스트 가이드
1. Bootstrap에서 시작해 광산으로 들어간 뒤, 높은 곳에서 떨어지거나 디버그 `falling 30m`을 쓴다. 짧은 붉은 글리치와 살짝 붉은 화면이 약 0.28초 보였다가 돌아오는지 본다. 그동안 이동·점프가 되는지, 착지음이 그대로인지도 본다.
2. 낙하 피해를 연달아 받는다(무적 0.75초 뒤). 화면이 점점 오래 붉어지지 않는지 본다.
3. 붕괴 낙석에 맞는다. 1과 같은 연출과 기존 피격음이 나는지 본다.
4. `hitgas`로 가스 피해를 받는다. 기존 가스 시야와 흔들림·깜빡임만 있고 붉은 글리치·틴트는 없어야 한다.
5. `O2 0` 뒤 체력이 틱으로 깎일 때 붉은 글리치·틴트가 전혀 없는지 본다.
6. 설정에서 모션 감소를 켜고 1을 다시 한다. 왜곡 없이 옅은 붉은 틴트만 보이는지 본다.
7. 체력이 낮을 때 낙하로 죽는다. 짧은 붉은 연출 뒤 사망 글리치·실패 창이 정상으로 이어지는지 본다.
8. 새 효과음이 없는지, Console에 새 빨간 오류가 없는지 본다.

## 완료 체크리스트
- [ ] `MVP2-fix151` push 확인(미커밋, 사용자 동의 대기)
- [ ] 커밋 해시 기재(미커밋)
- [x] Unity 컴파일 오류 없음
- [x] EditMode 새 실패 없음(실행한 묶음 기준, 기존 실패 5개는 변경 전에도 실패)
- [x] 낙하·붕괴 피해 시 짧은 붉은 글리치+틴트, 검사기 조절(EditMode 확인, 실제 화면은 미검증)
- [x] 가스는 기존 시야만, 붉은 효과 없음(EditMode)
- [x] 산소 0 틱은 글리치·틴트 없음(EditMode)
- [x] 조작 미차단(레이캐스트 없음, 입력 잠금 없음)
- [x] 연속 피격 시 길이 누적 없음
- [x] ReduceMotion이면 왜곡 없이 틴트만
- [x] 새 SFX 없음, 기존 피격·착지음 유지
- [x] 기존 흔들림·깜빡임 유지
- [x] 가스 시야 클래스 미변경
- [x] Gameplay→App 참조 없음
- [x] 프리팹 YAML 손수정 없음
- [x] main 직접 커밋 없음
- [x] PR 안 만듦
- [x] 브랜치 삭제 안 함
- [x] #359 미수정
