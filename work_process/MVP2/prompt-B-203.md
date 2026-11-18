# B-203 가스 지속시간 종료 후 1초 페이드아웃 (#291)

## 목표
가스 지속시간이 끝나면 `SetActive(false)`로 한 프레임에 끄지 말고, 등장과 같은 1초 동안 그림이 흐려지며 사라지게 한다. 페이드아웃 중에는 등장 때와 같이 피해·화면 탁화·가스 경고가 없다. 위험한 지속시간 길이는 지금과 같다. 페이드아웃 중 저장하면 그 가스는 끝난 것으로 보고 불러올 때 나오지 않는다.
기준: https://github.com/Nobamb/sub-terra/issues/291
이 이슈는 Nobamb가 진행한다(연출 포함). 댓글: 「이 이슈는 제가(Nobamb) 작업합니다.」

## 브랜치
1. `git fetch` 후 최신 `origin/main`에서 새 브랜치 **`MVP2-fix147`**를 만든다. 이 문서를 쓸 때의 main은 `0a9a3f403b80a81f8540dd2c3137f7348c8a7c73`이다. (`MVP2-fix146`까지는 다른 프롬프트용. 이 번호는 쓰지 않는다.)
2. **다른 프롬프트와의 관계.** B-201(#358)·B-202(#306)과 파일 겹침 없음. `EmergencyRescue*`·`CushionPadHintHologram`·채굴 과녁/`MiningSystem` 목표 조회는 건드리지 않는다. B-204(#298)와는 `GasVisualRules.cs`만 겹칠 수 있다. 상수 추가가 필요하면 가스 페이드는 기존 `SpawnDurationSeconds`만 따르고 새 상수를 넣지 말 것. 작업 시점의 `origin/main`에서 딴다. 원격에 같은 이름이 다른 커밋으로 있으면 빈 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다.
3. 보고서 맨 위에 기준 커밋 해시를 적는다. 브랜치를 지우지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix147`(또는 실제 쓴 번호)에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다. #291에 댓글·체크박스 수정·닫기를 하지 않는다.
3. 새 음원, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다. 기존 `SfxId.GasFlowLoop`·`GasPulseLoop`만 쓴다.
4. 큰 화면 전체 모션이 아니면 reduce-motion 분기를 새로 넣지 않는다. 가스 등장 1초는 이미 `ReduceMotion` 없이 재생된다. 페이드아웃도 같은 로컬 구름이라 새 분기를 넣지 않는다.
5. 프리팹 YAML을 손으로 고치지 않는다. 기본 지속시간(12초 등)과 `SpawnDurationSeconds` 값 자체는 바꾸지 않는다.
6. 등장 중 무피해 규칙, 농도·반지름·채굴로 가스를 내는 경로, 조명 구멍 판정은 재설계하지 않는다.

## 현재 상태 (main `0a9a3f40`) — 아직 미구현
- `GasZone.Tick`(`Scripts/Gameplay/Hazards/GasZone.cs`): 등장(`spawnElapsed` / `GasVisualRules.SpawnDurationSeconds` = 1)이 끝난 뒤에야 `remainingDuration`을 줄이고, 0이 되면 바로 `IsActive = false`와 `gameObject.SetActive(false)`. 페이드아웃 상태 없음.
- `Contains()`는 `IsActive && IsSpawnComplete`일 때만 참이라, 등장 중에는 피해·탁화·경고가 없다. `GasHazardSystem.ReevaluateExposure`가 `Contains`로 노출을 정한다.
- `GasZoneVisual.ApplyVisual`은 `IsActive ? SpawnProgress : 0`으로 지름·알파(`tuning.OpacityCapped * progress`)를 키운다. `IsActive`가 꺼지면 구름이 즉시 사라진다.
- 저장: `WorldSnapshotSystem.CaptureGasZones`는 `!zone.IsActive`면 건너뛴다. `RestoreGasZone`은 `!isActive` 또는 `isNeutralized`면 null, 복원 시 `Activate(..., playSpawnAnimation: false)`로 등장 연출을 생략한다.
- 소리: `GasFlowLoopAudio`는 등장 중 볼륨을 키우고, 남은 시간이 `GasFadeOutLead`(2초) 아래로 떨어지면 지속시간 안에서 미리 줄인다. `GasZoneDeactivated`면 `StopNow()`. `GasPulseAudio`는 가스 밖에서 짧게 페이드(`FadeOutSeconds` 0.08).

## 할 일
1. 지속시간이 0이 되어도 바로 `SetActive(false)` 하지 않는다. `SpawnDurationSeconds`와 같은 길이로 페이드아웃을 돌린 뒤 끈다. 페이드 전용 초를 새로 적지 말고 등장 상수를 따른다.
2. 페이드는 등장 연출을 거꾸로: `GasZoneVisual`의 지름·알파·입자가 `SpawnProgress`처럼 1→0으로 줄어든다. 게임플레이가 끝난 뒤에도 그림이 남을 수 있게, 시각은 `IsActive`만 보지 말고 페이드 진행도를 쓴다. `Tick`은 페이드가 끝날 때까지 계속 돈다.
3. 페이드가 시작되면 피해·탁화·경고는 끈다. `Contains()`가 거짓이면 된다(예: 페이드 시작과 함께 게임플레이 `IsActive`를 끄거나, `Contains`에서 페이드 중을 제외). 등장 중 무피해와 같은 규칙. `remainingDuration`이 줄어드는 위험한 시간은 지금과 같게 둔다(페이드는 그 이후).
4. 저장: 페이드 중인 가스는 끝난 것으로 취급해 스냅샷에 넣지 않는다. (`IsActive`를 페이드 시작에 끄면 기존 `CaptureGasZones`가 이미 건너뛴다.) 불러오기도 그 가스를 만들지 않는다.
5. 기존 반복음은 페이드에 맞춰 줄이거나, 최소한 페이드가 시작될 때 멈춘다. `GasZoneDeactivated`를 페이드 시작에 보내면 `GasFlowLoopAudio.StopNow()`로 후자가 된다. 새 소리는 넣지 않는다. 지속시간 마지막 2초의 기존 선행 페이드는 유지해도 된다.
6. 복원(`playSpawnAnimation: false`)은 등장·페이드를 생략하고 지금처럼 바로 활성 상태로 둔다.
7. EditMode: 지속시간 종료 직후 `Contains`는 거짓, 오브젝트는 아직 켜져 있고 시각 진행이 1초 동안 줄어든 뒤 꺼지는지, 페이드 중 스냅샷에 안 남는지, 등장이 끝나기 전 `remainingDuration`이 안 깎이는지. 기존 `GasZoneSpawnAndRangeTests`를 깨지 말 것.

## 테스트 (플레이 가이드)
1. 가스 블록을 채굴해 가스를 낸다. 약 1초 등장 동안에는 들어가도 피해가 없는지 본다.
2. 지속시간이 끝날 때까지 기다린다.
3. 가스가 한 번에 사라지지 않고, 등장과 비슷한 약 1초 동안 서서히 흐려지며 사라지는지 본다.
4. 흐려지는 동안 가스 안에 들어가도 피해·탁화·가스 경고가 없는지 본다.
5. 흐려지는 중에 저장하고 불러오면 그 가스가 다시 나타나지 않는지 본다.
6. 가스 반복음이 있다면 페이드가 시작될 때 멈추거나 같이 줄어드는지, 새 효과음이 없는지를 본다.
7. Unity Console에 새 빨간 오류가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 가스 종료 후 등장과 같은 1초 페이드아웃 (프롬프트 B-203)`). `MVP2-fix147`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue291-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #291은 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-203-gas-fade-out-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일
  - 페이드 길이(상수 재사용), `Contains`/저장/소리 처리
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix147` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음 [ ] 종료 후 1초(`SpawnDurationSeconds`) 페이드 후 소멸 [ ] 페이드 중 피해·탁화·경고 없음 [ ] 위험 지속시간 길이 불변 [ ] 페이드 중 저장 시 복원 안 됨 [ ] 기존 가스 루프음 정지 또는 페이드, 새 SFX 없음 [ ] 복원은 등장/페이드 생략 [ ] 프리팹 YAML 손수정 없음 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] 브랜치 삭제 안 함 [ ] #291 미수정
