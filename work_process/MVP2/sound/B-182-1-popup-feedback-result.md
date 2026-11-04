# B-182-1 팝업 열림음·보상 포기음·금화 아이콘 수정

- 작업 브랜치: `fix/b182-popup-feedback` (기준 `MVP2-fix129`). 커밋·푸시하지 않았다.
- 시작 시 `init/prompt-B.md`에 있던 사용자 수정은 보존했다.

## 변경

1. `CargoFullPopupView.Show` / `CargoDiscardPopupView.Show`에서 활성화 직후, 글자 배치를 다시 계산하기 전에 `WindowTvOpen`을 한 번 재생한다. 연출 시간표나 게이지 완료 시점과 분리했다. 기존 음량 배율 0.6은 유지했다. 이후 추가했던 “완료 창 닫힘 글리치보다 먼저 재생” 처리는 사용자 요청으로 롤백했다.
   - 이번 요청의 “팝업이 켜지자마자 한 번”을 페이지 이동·복귀에도 적용했다. 기존 B-182의 `page=true` 열림음 생략은 변경했다.
   - 이미 Opening/Open인 창에 반복 Show를 호출하면 재생하지 않는다. Bind/Unbind, ManualTick 미리보기, SfxQuiet, HideImmediate 정리는 기존 무음을 유지한다.
   - 넘침 경고음은 기존처럼 첫 펼침의 GaugeEnd에서 한 번이며, 페이지 이동·복귀에는 다시 내지 않는다.
2. `DemoObjectiveView.CargoForfeitSfx`를 `StorageTakeAll`로 변경했다. 보상 포기는 gate 통과 뒤 한 번만 재생하며, 같은 창의 “전부” 버리기와 같은 소리를 사용한다.
3. 판매창의 `Art/UI/SurfaceBase/Icons/icon-gold.png`를 다음 위치에 적용했다.
   - `Resources/UI/CargoFullPopupSkin.asset`: 부족 창과 버리기 창 공용.
   - `Scenes/App/Mine_Demo_Integration.unity`: 상세 퀘스트 보상과 퀘스트 클리어 보상의 Gold/Icon, 2곳.
   - `Prefabs/UI/SurfaceBasePanel.prefab`: 지상 퀘스트 보상 Gold/Icon, 1곳.
   - B-107, B-152, B-95-1 빌더의 경로도 변경해 재생성 때 골드바로 되돌아가지 않게 했다.
   - 기존 크기(클리어 40×40, 상세 보상 52×52 등)와 preserveAspect를 유지했다. Scene/Prefab은 Editor API로 저장했고, 무관하게 추가·삭제된 직렬화 필드는 Git hunk 복원으로 제거했다. 최종 Scene은 sprite 2줄, Prefab은 sprite 1줄만 다르다.

## 호출 경로 확인

- 부족 창: `DemoObjectivePresenter.ConfirmClaim` → `CloseClaim` → 보상 판정 → `ShowCapacity` → `SetCapacityChoice`(Render) → `SetCapacityChoiceVisible(true)` → `CargoFullPopupView.Show(false)`(TV 시작).
- 버리기 창: `OnCapacityDumpClicked` → Presenter 선택 처리 → 부족 창 Close(Page) → `OnCapacityPopupClosed` → `ShowDumpPanel(true)` → `CargoDiscardPopupView.Show(true)`.
- 돌아가기: `OnDumpClosedClicked` → 버리기 창 Close(Page) → `OnDumpPopupClosed` → `CargoFullPopupView.Show(true)`.
- 기존 Show(false)에는 별도 재생 지연 타이머가 없었다. 음원 시작 0~0.05초 RMS는 약 0.47로, 앞부분 무음도 확인되지 않았다. 뒤쪽 소리는 게이지 완료(GaugeEnd, 기본 0.54초)의 `CargoHeavyWarning`이라는 설명 후 사용자가 추가 선행 재생만 롤백하도록 요청했다. TV 소리와 경고음은 별개이며, 경고음 시점은 변경하지 않았다.

## 검증

- 관련 EditMode 81개 통과 / 실패 0: B-182, B-152, B-153, B-95, B-107. `sub-terra/Temp/b182-1-edit/`.
- 초기 PlayMode 3개 통과 / 실패 0. `sub-terra/Temp/b182-1-play/`.
- 첫 수정 PlayMode 4개 통과 / 실패 0. `sub-terra/Temp/b182-1-play-final/`. 실제 View의 페이지 이동·복귀 경로에서도 내용 등장 완료 전에 AudioSource.isPlaying=true이며, 창마다 StartedCount가 한 번만 증가함을 확인했다.
- 추가 선행 재생 롤백: `PrepareOpenSfx`, `PrepareCapacityOpenSfx`, Presenter의 선행 호출·예측 판정, 해당 변경 전용 테스트 2개를 제거했다. Presenter는 작업 전 코드와 동일하다. 최초 페이지 열림음 연결·포기음·금화 아이콘은 유지했다.
- 롤백 후 최종 검사: 관련 EditMode **46 통과 / 0 실패**, PlayMode **4 통과 / 0 실패**. `sub-terra/Temp/b182-early-rollback/`. 테스트의 TimeManager 자동 변경은 복원했다. 실제 청취는 미검증이며 아래 체크리스트를 유지한다.
- 처음 펼침·페이지 복귀 즉시 열림음, 중복 Show 무음, 포기음 StorageTakeAll 한 번, 초기화·정리 무음, 기존 넘침·접힘 시점을 검사했다.
- B-182의 페이지 무음 기대값은 이번 요청에 맞게 “페이지 표시 시작에 한 번”으로 변경했다. B-95의 골드바 기대값을 금화 경로로 변경하고, 공용 스킨·지상 퀘스트의 금화 참조와 비율·기존 크기를 검사했다.
- 실제 PlayMode 렌더링에서 화물 부족·버리기·공간 확보 완료 화면을 확인했다. 별도 Editor 렌더링으로 퀘스트 클리어·지상 퀘스트 금화 크기와 비율을 확인했다. 모두 절반 해상도로 검사했다.
- 화면: `sub-terra/Temp/b182-play-screens/`, `sub-terra/Temp/b182-1-screens/`.
- 테스트가 덮어쓴 기존 B-107 스크린샷 3개를 작업 전 상태로 복원했다. ProjectSettings, Packages, 폰트, 음원, SfxId/SfxLibrary 변경 없음.
- 최종 Unity Console Error 0 / Warning 0. `git diff --check` 통과. 새 Inspector 연결이나 수동 빌더 실행은 필요 없다.
- 교훈: `init/rules/lessons/2026-11-04-popup-display-audio.md`, `init/rules/lessons/2026-11-04-popup-sound-identification.md`.

## 미검증·수동 체크리스트

- [ ] Bootstrap → 새 게임 → 탐사에서 화물을 채우고 퀘스트 완료 창을 닫아, 부족 창 열림 때 TV 켜짐음, 게이지 완료 때 별도 경고음이 한 번씩 들리는지 직접 청취한다.
- [ ] “버릴 자원 선택”과 “돌아가기”에서도 켜짐음이 한 번씩 즉시 들리고 글리치·페이지음과의 겹침이 자연스러운지 확인한다.
- [ ] “보상 버리기”와 “전부”의 묵직한 소리가 같은지 확인한다. 기존처럼 공간 확보가 끝나는 마지막 버리기는 완료 경로의 소리 생략 규칙을 유지한다.
- [ ] 효과음 음량·음소거 설정을 조작해 확인한다. 실제 사용자 세이브를 이용한 저장·이어하기 및 Windows 빌드는 수행하지 않았다.

## 변경 파일

경로는 `sub-terra/Assets/_Project/` 기준이다.

- Runtime: `Scripts/App/UI/Tutorial/CargoFullPopupView.cs`, `CargoDiscardPopupView.cs`, `DemoObjectiveView.cs`.
- Builder: `Editor/DataValidation/PromptB107QuestUiBuilder.cs`, `PromptB152CargoFullPopupBuilder.cs`, `PromptB951QuestClearRewardUiBuilder.cs`.
- Assets: `Resources/UI/CargoFullPopupSkin.asset`, `Scenes/App/Mine_Demo_Integration.unity`, `Prefabs/UI/SurfaceBasePanel.prefab`.
- Tests: `Tests/EditMode/App/Audio/PromptB182SfxCargoTests.cs`, `Tests/EditMode/App/Tutorial/PromptB107QuestUiTests.cs`, `PromptB152CargoFullPopupTests.cs`, `PromptB95QuestRewardTests.cs`, `Tests/PlayMode/Integration/Audio/PromptB182SfxCargoPlayModeTests.cs`.
- Docs: 이 보고서와 위 교훈 파일.
