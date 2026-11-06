# B-187 배경 그림 교체 + 광물 반짝임 결과 (#250 / #220)

- 브랜치: `codex/issue-220-mine-parallax-background` (기존 PR #250). 기준 `origin/main` 38af13f88(#297)은 이미 이 브랜치에 병합되어 있어 추가 merge·충돌 없음.
- 커밋 해시: 미커밋 (사용자 동의 대기).

## 교체·변경 파일
- `Art/Backgrounds/mine-cavern-{copper,iron,lithium}.png` 1672×941 바이너리만 교체(.meta GUID 유지, 임시 파일 후 원자 교체).
- `MineBackdrop.shader`, `MineBackdropController.cs`, `MineBackdropSettings.cs`, 새 `MineBackdropGlow.cs`, `MineBackdropAssetBuilder.cs`(구 값일 때만 새 기본값으로 이전), `MineBackdropSettings.asset`.
- 테스트: 새 `PromptB187MineBackdropRefreshTests`, `MineBackdropViewportTests`는 가로형(16×9) 기준으로 갱신.

## ★ 값
| 항목 | 이전 | 이후 | 이유 |
|---|---|---|---|
| tint | 0.65/0.70/0.75 | 0.92/0.95/1.00 | 밝게. 전경보다는 어두운 채로 |
| visibleImageHeight | 0.22 | 0.65 | 처음 0.5에서 사용자 QA 후 축소: 가장자리 석판이 보이도록 |
| depthTravelWorld | 160 | 60 | 40m 안팎에서 세로 이동이 충분히 보이게 |
| depthDarkenStart / FullDepth / deepBrightness | - | 2m / 40m / 0.35 | 깊이별 배경 어두워짐 (5m≈0.95, 17m≈0.74, 25m≈0.61, 37m≈0.40, 40m≈0.35) |
| edgePadding | 0.05 | 유지 | |
| horizontalTravelWorld | 160 | 80 | 광산 폭(x -40~40)과 일치시켜 좌우 끝에서 그림 좌우 끝까지 보이게 |
| lithiumGlowBase / PulseBoost | - | 0.08 / 0.08 | |
| copper / iron PulseStrength | - | 0.30 / 0.25 | |
| 반짝 길이 / 간격 | - | 0.35~0.6초 / 2.5~5초 | 랜덤 |
| reducedMotionLithiumGlow | - | 0.08 | 모션 감소 시 고정 |

## 반짝 구현
- 셰이더 방식(권장안). 반짝마다 위상이 바뀌는 얼룩 마스크로 한 시점에 몇 군데만 켜진다.
  리튬: 청록 채널, 구리: 주황 `(r-b)` 광맥, 철: 밝은 회청(min(rgb) 큰 픽셀).
- 타이머는 `MineBackdropGlow`(순수 C#). `Time.timeScale == 0`이면 clock·반짝 정지. 모션 감소면 펄스 0, 리튬 고정 ≤0.08. 깊이 그림 전환·부드러운 보간은 유지.

## 테스트
- EditMode 전체: Pass 2457 / Fail 23. 실패 23개는 건물 배치·HUD·패널 레이아웃·씬 등 배경과 무관한 테스트이며 새 배경 테스트는 모두 통과. 이번 변경 전에도 실패했는지는 **미검증**.
- PlayMode: 실행은 시작되어 깊이 캡처(`work_process/images/mine-depth-backdrop/depth-{5,17,25,37,40}.png`)가 갱신됨. 결과 파일(`subterra-playmode-results.txt`)은 생성되지 않아 통과 여부는 **미검증**.
- 실제 플레이로 반짝 빈도·밝기, 일시정지, 모션 감소, 순간이동은 **미검증**(사용자 테스트 가이드 참고).

## 체크리스트
[ ] #250 브랜치 push (미실시) [x] main merge됨 [x] PNG GUID 유지 [x] SFX 변경 없음 [x] 새 PR 없음 [x] main 직접 커밋 없음 [~] 컴파일·EditMode 새 실패 없음(배경 관련은 없음)

## 사용자 테스트 가이드
`work_process/MVP2/prompt-B-187.md`의 "사용자 테스트 가이드" 1~10번 그대로.

## 추가 반영 (사용자 QA)
- 깊이별 어두워짐 `DepthBrightness`, 시야 축소(0.65)·좌우 이동 폭 80 위 ★ 표 참고.
- 광물 상시 빛(shimmer): 위치마다 위상이 다른 숨쉬기 빛을 광물 픽셀(구리 광맥, 철 하이라이트, 리튬 결정·문양)에만 더함. lithiumShimmer 0.35 / copper 0.18 / iron 0.18. 펄스도 0.2 / 0.45 / 0.4로 상향. 빛은 깊이 어두워짐을 절반만 받아 깊은 층에서도 보임. 모션 감소 시 0, 일시정지 시 clock 정지.
