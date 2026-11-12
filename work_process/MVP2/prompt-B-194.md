# B-194 자원 획득 홀로그램 등장·스캔 공개 (#151, PR #259)

## 목표
채굴이 끝나 머리 위에 뜨는 수확 수량 홀로그램이 연출 없이 문구만 바뀌는 문제를 고치고, 그 홀로그램 위에 자원 아이콘을 홀로그램처럼 올린다. 아이콘은 처음엔 청록으로 덮였다가, 바코드 스캔처럼 위에서 아래로 원래 색이 드러나게 한다. 엔진 연료는 스캔이 끝난 뒤 따뜻한 후광을 한 번 더해 일반 광물과 구분한다.
기준: https://github.com/Nobamb/sub-terra/issues/151 의 자원 획득 연출, https://github.com/Nobamb/sub-terra/pull/259 (미병합, PM이 이어서 진행). 골드 팝업·채굴 파편·기존 효과음은 유지한다.

## 브랜치
1. `git fetch` 후 최신 `origin/main`(`fcf180d73106ecf959cba29623518e2e245a8f03`, #311 병합. 그 안에 #315/B-192, #318/B-193, #314, #274 포함)에서 새 브랜치 **`MVP2-fix138`**을 만든다.
2. PR #259 브랜치 `codex/issue-151-resource-pickup-vfx`(`41df5dc3`, 마지막 main 동기화 `d2f54697`)는 main보다 약 112커밋 뒤라 여기서 이어가지 않는다. 새 PR도 만들지 않고, #259는 닫거나 댓글하지 않는다.
3. PR #259에서 쓸 부분만 최신 main 위에 옮긴 뒤 아래 연출로 바꾼다. 가져올 핵심은 `cfaa8536`(feat: 일반·희귀 자원 획득 회수 연출 추가)의 `ResourcePickupVfx.cs`·테스트·`IntegrationRuntimeBinder` 연결이다. `41df5dc3`의 교훈 파일·검증 보정은 필요하면 함께 가져온다. 타일에서 플레이어로 날아가는 회수 연출과, 모션 감소 시 연출을 줄이던 처리는 최종 모습으로 쓰지 않는다.
4. 원격에 `MVP2-fix138`이 이미 있으면 비어 있는 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다.
5. 보고서 맨 위에 기준 커밋 해시와, #259에서 가져온 파일·커밋을 적는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix138`에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다. #259·#151에 댓글·체크박스 수정·닫기를 하지 않는다.
3. 새 음원, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다. 수확 홀로그램을 다시 등장시켜도 `FacilityNameTagVisual.PlaySfx`는 끈 채로 둔다(`WorldFacilityTagBeep`·`UiScanTick`이 나면 안 된다). 기존 채굴·획득 효과음은 그대로다.
4. 시각 연출이 인벤토리·골드·에너지 수량을 다시 바꾸면 안 된다. 골드 팝업(`GoldPickupVfx`), 채굴 파편, 머리 위 경고 문구, 시설 이름표 본연의 동작은 유지한다.
5. 모션 감소(`AccessibilityPreferences.ReduceMotion`)는 큰 화면 전환에만 적용하는 기존 결정 그대로다. 이번처럼 작은 UI 연출은 켜져 있어도 등장·스캔·연료 후광을 보통처럼 재생한다. PR #259처럼 플레이어 위치에 붙이거나 시간을 줄이지 않는다.
6. 프리팹 YAML을 손으로 고치지 않는다. 런타임 생성(기존 `ResourcePickupVfx`, `FacilityNameTagVisual.CreateWorld`)을 우선한다.
7. 줄 번호는 `fcf180d7` 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 먼저 읽을 것
- 수확 문구 경로: `IntegrationRuntimeBinder.TryCommitMining` ~831이 성공 시 `MiningYieldCommit.FormatHudFeedback` 결과를 `MiningProgressHud.SetPendingYieldFeedback`에 맡긴다. `MiningProgressHud.OnProgressChanged` ~138은 `Completed`에서 `PlayerHeadMessageStack.ShowInfo("mining.progress", pendingYieldText)`로 같은 키의 진행 문구를 결과로 바꾼다. 예: `구리 +1`, 보너스면 `구리 +1 (+1 수확)`.
- 왜 연출이 없나: `PlayerHeadMessageStackModel.Show` ~257은 같은 키·같은 스타일이면 문구와 유지 시간만 바꾸고 `Level`을 0으로 되돌리지 않는다. `FacilityNameTagVisual.SetWanted` ~364도 이미 켜져 있으면 등장을 다시 시작하지 않는다. 진행("채굴 N%") 동안 이미 펼쳐진 홀로그램에 수량 문구만 덮이는 상태다.
- 재사용할 등장 연출(채굴 중 머리 위 홀로그램과 동일): `FacilityNameTagTimeline`(`ShowDuration = 0.26s`. 모서리 빛 → 프레임 펼침 → 스캔선·글자 드러남), `FacilityNameTagVisual.Tick`/`Apply`, `PlayerStatusTimeline`(유지 2.5s, `MaxEntries = 4`, 골드 슬롯 포함). `PlayerHeadMessageStack`은 이 그림을 `FacilityNameTagLayers.MainScreen`에 두어 CCTV에 안 찍히게 한다.
- PR #259 참고 구현(`41df5dc3`의 `ResourcePickupVfx.cs`): 지급 성공 수량과 `TileMined`의 자원 ID가 맞을 때만 1회, 실패·취소·0개·골드만 있는 블록은 안 띄움, 풀 8칸, 엔진 연료(`DataIds.RareItems.EngineFuel`)만 큰 아이콘·반짝임 3개, 카탈로그 아이콘, 바인딩은 `ActivateUi`에서 `BindTo(miningSystem, player, iconResolver)`. 연결 위치는 최신 main 기준으로 `goldPickupVfx` 필드 ~68 근처, `BindMinimap` ~682 앞, `SetPendingGold` ~841 뒤, `OnDestroy` ~1045.
- 기존 검사: `PlayerHeadMessageStackTests.Progress_UpdatesSameEntryAndTurnsIntoResultInPlace`, `FacilityNameTagTests.Visual_RepeatedWanted_DoesNotRestartTheShowAnimation`, `FacilityNameTagTests`, `PromptB1001GoldPickupTests`.

## A. 수확 홀로그램 등장
- 진행 문구("채굴 N%")가 결과 문구로 바뀔 때, 같은 자리·같은 항목을 유지한 채 `FacilityNameTagTimeline` 등장을 처음부터 빠르게 다시 재생한다. 새 등장 연출을 만들지 말고, 기본 길이는 기존 `ShowDuration`(0.26s)을 쓴다. 더 짧아도 되지만 검사기에서 조절 가능하게 하고, 연속 채굴이 답답하면 안 된다.
- `SetWanted(true)`를 다시 호출하는 것만으로는 재시작되지 않는다. 모델 `Level`과 그림 `FacilityNameTagVisual`의 level을 함께 되감는 API를 추가한다. `ResetImmediate`로 한 프레임 꺼지지 않게 한다.
- 결과로 바뀔 때 채굴 게이지는 끈다(`Progress = -1`).
- 진행 중 퍼센트 갱신(`SetProgress`)은 지금처럼 등장을 다시 시작하지 않는다. 실패·취소는 지금처럼 진행 문구를 내리고 경고만 띄운다.
- 머리 위 `PlaySfx`는 계속 false. 시설 이름표 쪽 소리는 건드리지 않는다.

## B. 아이콘 홀로그램과 색 스캔
- 수확 홀로그램 바로 위에, 카탈로그의 기존 자원 아이콘을 띄운다. 경고 아이콘처럼 이름표 안에 넣지 않는다.
- 처음에는 아이콘을 청록으로 덮고, 가로 줄무늬 노이즈를 살짝 넣으며, 반투명해서 배경이 조금 비치게 한다.
- 곧바로(또는 홀로그램 등장과 겹쳐) 스캔 줄이 위에서 아래로 지나가며 원래 아이콘 색을 드러낸다. 구리·철·리튬이 색으로 구분돼야 한다. 스캔은 0.2~0.3초, 검사기에서 조절. 기본은 이 구간 안 짧은 값.
- 스캔 후에도 홀로그램 느낌이 조금 남아도 되지만, 원래 색이 묻히면 안 된다.
- 아이콘은 수확 홀로그램에 붙여 함께 뜨고 함께 사라지게 한다. 골드 팝업·다른 문구와 겹치지 않게 높이만 반영하고, `MaxEntries = 4`를 아이콘 때문에 더 빨리 잠식하지 않는다. CCTV에 안 찍히게 `MainScreen` 레이어를 쓴다.
- 광물 수량이 0이고 골드만 있는 블록에서는 아이콘을 안 띄운다. 골드 팝업은 그대로다.

## C. 엔진 연료
- 일반 광물보다 아이콘을 조금 크게 두고, PR #259의 반짝임 3개는 유지한다.
- 원래 색이 다 드러난 뒤에, 따뜻한 색(금색이나 주황) 후광이 한 번 퍼지게 한다. 후광까지 청록이면 홀로그램에 묻힌다. PR #259의 청록 테두리 회전은 이 후광을 대신하지 못하면 빼도 된다.
- 연료는 한눈에 일반 광물과 달라야 한다.

## D. 연결·풀·범위
- PR #259와 같이 지급 성공+같은 자원의 채굴 완료가 맞을 때만 아이콘을 1회 띄운다. `QueueAccepted`는 `TryCommitMining`에서 성공 시 `AcceptedBaseQuantity + AcceptedBonusQuantity`, 실패 시 0.
- 풀은 최대 8개. 연타로 쌓여도 새 객체를 한없이 만들지 않는다. 씬 종료·비활성화·장거리 이동 시 정리한다.
- `BindResourcePickupVfx`는 `ActivateUi`의 골드 바인딩 뒤, `BindMinimap` 앞에 넣고 `OnDestroy`에서 `Unbind`한다. 기존 `TryStep`/경고 패턴을 따른다.

## 테스트 (EditMode 추가, 필요하면 PlayMode)
- 진행→결과 전환 시 같은 항목이 유지되고, 게이지가 꺼지며, 등장 level이 0부터 다시 올라간다. 진행 중 퍼센트 갱신은 등장을 다시 시작하지 않는다. `Progress_UpdatesSameEntryAndTurnsIntoResultInPlace`는 이 새 조건에 맞게 고친다.
- 아이콘 스캔: 시작은 청록 덮개, 0.2~0.3초 안에 원래 색이 위에서 아래로 드러남. 연료는 스캔 후에만 따뜻한 후광 1회, 반짝임 3개. 일반 광물은 그 후광이 없음.
- 확정 지급·중복/실패/취소/0개/골드만 있는 블록, 풀 8칸, 연출 후 수량 불변. 모션 감소여도 등장·스캔·후광이 줄어들지 않음(PR #259 검사 조건은 이 규칙에 맞게 수정).
- 기존 `FacilityNameTagTests`·`PlayerHeadMessageStackTests`·골드 검사 새 실패 없음. 머리 위 연출에서 새 효과음이 안 나는지 확인.

## 사용자 테스트 가이드 (직접 플레이)
1. 광산에서 구리를 캔다. "채굴 N%" 홀로그램이 뜨는지 본다.
2. 채굴이 끝나면 같은 자리에서 수량 문구(예: `구리 +1`)가 시설 이름표와 같은 등장으로 빠르게 다시 펼쳐지는지 본다. 문구만 툭 바뀌면 안 된다.
3. 그 홀로그램 위에 구리 아이콘이 청록으로 떴다가, 스캔이 위에서 아래로 지나가며 원래 색이 보이는지 본다.
4. 철·리튬도 3을 반복해, 스캔 뒤 색으로 구분되는지 본다.
5. 엔진 연료를 캔다. 아이콘이 더 크고 반짝임이 있으며, 색이 드러난 뒤 금색/주황 후광이 한 번 퍼지는지 본다. 일반 광물과 바로 구분돼야 한다.
6. 연속으로 여러 칸을 캔다. 연출이 쌓여도 버벅이지 않고, 이전 홀로그램이 자연히 사라지는지 본다.
7. 골드가 같이 나오는 칸과 골드만 있는 칸을 캔다. 골드 팝업은 그대로고, 골드만 있는 칸에는 자원 아이콘이 안 뜨는지 본다.
8. 채굴을 중간에 취소하거나 실패(화물 가득, 범위 밖 등)하면 수량 아이콘이 안 뜨는지 본다.
9. 설정에서 모션 감소를 켜고 2~5를 다시 본다. 이번 연출은 그대로여야 한다.
10. 인벤토리 수량·골드가 연출 때문에 늘어나지 않는지, CCTV에 이 홀로그램이 안 찍히는지, Unity Console에 새 빨간 오류·노란 경고가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 자원 수확 홀로그램 등장과 아이콘 스캔 공개를 추가함 (프롬프트 B-194)`). `MVP2-fix138`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue151-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #151·PR #259는 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-194-resource-pickup-hologram-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, #259에서 가져온 파일, 바꾼 파일·줄
  - 등장 재시작을 어디에 붙였는지, 스캔·연료 후광 시간, 모션 감소를 줄이지 않은 이유
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix138` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음(머리 위 스택·이름표·골드 검사 포함) [ ] 수확 문구가 같은 자리에서 등장 연출을 다시 재생 [ ] 진행 중 퍼센트 갱신은 재등장 없음 [ ] 아이콘이 홀로그램 위에 뜨고 청록→스캔→원래 색 [ ] 구리·철·리튬 색 구분 [ ] 스캔 0.2~0.3초 [ ] 연료는 큰 아이콘·반짝임 3개·스캔 후 따뜻한 후광 1회 [ ] 실패·취소·0개·골드만 있는 블록은 아이콘 없음 [ ] 골드 팝업·파편·기존 효과음 유지 [ ] 수량 불변 [ ] 풀 8칸 [ ] 모션 감소여도 이번 연출 유지 [ ] 머리 위 새 SFX 없음 [ ] CCTV 미표시 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] #259·#151 미수정
