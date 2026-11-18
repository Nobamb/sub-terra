# B-202-1 마우스 채굴 과녁 폴백 결과

- 기준 브랜치 `MVP2-fix146`, 시작 커밋 `509f05b269fbc89531f923286a35c3a8e6d1e1dd`
- 변경분은 `MVP2-fix146` 브랜치에 반영한다.
- 요구사항 출처는 [PR #374의 수정 요청](https://github.com/Nobamb/sub-terra/pull/374#issuecomment-6943303588)이다.

## 변경 파일

- `PlayerMiningController.cs`의 `TryResolveReticleCell()`에서 마우스 과녁을 정할 수 없으면 기존 `TryResolveKeyboardReticleCell()`을 호출한다. 마우스 우선 상태를 유지하므로 유효한 호버가 돌아오면 그 칸을 다시 표시한다. 채굴 중에는 `ActiveCell`을 우선한다.
- `MiningTargetReticleTests.cs`에 `Controller_PointerOutOfReachFallsBackToKeyboard_AndRecoversOnValidHover`를 추가했다. 실제 포인터 좌표로 유효 칸, 사거리 밖, 유효 칸을 차례로 가리키고 사거리 밖에서도 `Source`가 `Pointer`인지 확인한다.
- `init/rules/lessons/2026-11-18-issue306-mining-reticle-pointer-fallback.md`에 교훈을 기록했다.
- 추가 Inspector 연결이나 씬 설정은 필요 없다.

## 검증

- 수정 전 새 테스트는 포인터가 사거리 밖으로 이동한 직후 실패했다. 수정 후 `MiningTargetReticleTests`와 `PlayerLadderMiningTests` 총 47개가 통과했다. 채굴 중 목표 칸 고정, 설정·건설 모드 숨김 테스트를 포함한다.
- `SubTerra.Gameplay.Mining.PlayModeTests` 25개가 통과했다. 키보드·마우스 채굴과 포인터 점유 테스트를 포함한다.
- 세이브 슬롯 0의 격리된 `Mine_Demo_Integration`에서 테스트용 블록 세 칸과 가상 마우스로 확인했다. Game View 캡처에서 유효한 호버 칸 → Enter로 캘 칸 → 유효한 호버 칸 순으로 과녁이 이동했다. 건설 모드에서 과녁이 숨는 것도 확인했다. 캡처 위치는 `sub-terra/Temp/b202-1-captures/`다.
- Unity Console 오류 0개를 확인했다. 변경 범위에 `git diff --check`를 통과했다.

## 미검증 및 수동 확인

- Bootstrap에서 새 게임을 시작해 지상 기지와 광산을 차례로 이동하는 전체 흐름은 미검증이다. 지상 기지에 과녁이 없는지, 광산에서 설정·가이드 창을 열면 과녁이 숨는지 확인한다.
- 광산에서 유효 칸을 가리킨 뒤 사거리 밖이나 캘 수 없는 칸으로 옮긴다. Enter 입력 없이 키보드 목표가 표시되고, 유효한 칸을 다시 가리키면 마우스 과녁으로 복귀하는지 확인한다.
- 키보드 목표 칸에 과녁이 표시된 상태에서 Enter를 눌러 같은 칸이 채굴되는지 확인한다. 건설 모드에서 과녁이 숨고 Console에 새 오류가 없는지도 확인한다.
