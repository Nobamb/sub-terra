# B-196 가스/조명 연출을 더 사실적으로 (#151) 결과

## 기준
- 기준 브랜치·커밋: `origin/main` `663b6f0b` (#327 병합 후). 프롬프트에 적힌 `6c9743a5` 이후 main이 B-195까지 진행돼 최신 main에서 분기했다.
- 만든 브랜치: `MVP2-fix140` (원격에 없음을 `git ls-remote`로 확인).
- 커밋 해시: **없음.** 사용자 동의 전이라 커밋·push 하지 않았다 (CLAUDE.md "동의 없이 커밋 금지"). PR·#151 수정도 하지 않았다.

## 바꾼 것
### A. 녹색 유독가스 (`GasZoneVisual`, 새 `Visuals/GasCloud.shader`)
- Knob 스프라이트 대신 부드러운 방사형 스프라이트 + 셰이더 `SubTerra/GasCloud`. 중심만 진하고 가장자리는 `smoothstep`으로 사라지며, 노이즈로 가장자리가 일렁인다. 판정 반지름(uv 반지름 1) 바깥은 셰이더가 항상 0으로 만든다.
- 바닥 고임: 밀도 중심을 아래로 0.25 내리고, 위쪽 밀도를 0.30까지 낮춘다 (`floorBias` 0.65).
- 노이즈 2장(`Art/VFX/Gas/gas-noise-01.png`, `-02.png`, 128px 심리스 fBm)을 서로 다른 속도·스케일로 월드 좌표에서 스크롤. 스크롤은 셰이더 `_Time`이라 화면 밖이면 그려지지 않아 비용이 없다.
- 입자: `GasMotePool`(전역 상한 48, 구역당 기본 8)에서 SpriteRenderer를 빌려 쓴다. 위치는 시간만의 함수(`GasVisualMath.EvaluateMote`)이며 판정 반지름의 90% 안쪽, 바닥 쪽이다. `isVisible`이 false면 풀에 돌려준다.
- 채도: (0.22, 0.82, 0.34) → (0.30, 0.66, 0.38). 불투명 상한은 `min(maxOpacity, 0.70)`로 70% 이하 고정.
- 머티리얼은 `Visuals/GasCloud.mat` 하나를 모든 구역이 공유. 구역별 값은 `MaterialPropertyBlock`.

### B. 붉은 조명 구멍 (`GasVisionVeil.shader`, `GasVisionOverlayUI.shader`, `GasVisionHoleEvaluator.Sample`, `GasClearance`)
- `step` 칼자국 → `1 - smoothstep(inner*r, r, d)` (`LightHoleInnerFraction` 0.35). 반지름 경계에서 0이라 시각이 판정 범위를 넘지 않는다.
- 색과 알파를 프리멀티플라이드로 보간해 경계 띠가 붉게 튀지 않게 했다 (교훈 파일 참고). 붉은색 (1, 0.12, 0.08) → (0.92, 0.30, 0.24), 중심 알파 5%는 그대로.
- `GasClearance` 스프라이트는 Knob → `gas-soft-radial.png`. `GasVisionClearanceSource.ApplySoftVisual`이 런타임에도 Knob이면 부드러운 스프라이트로 바꾸고 지름을 판정 반지름×2에 맞춘다.
- `TryGetNearestLight`·`IsCleared`·`CopyActiveLights`는 손대지 않았다.

### C. 가스 안 틴트·탁한 시야
- `GasInteriorHaze.Amount`(0~1)를 `GasExposureEffectController`가 갱신한다. 노출 중이고 대피 중이 아닐 때 목표 1, 올라갈 때 1.2초·걷힐 때 0.4초로 서서히 움직인다 (`GasVisualMath.StepHaze`). 컨트롤러가 꺼지면 0으로 리셋.
- 베일/오버레이: 어두운 색이 가스 녹색 쪽으로(`hazeTintStrength` 0.5), 조명 구멍 알파 +0.08, 색이 녹색 쪽으로. 불투명도(접근 연출 0.35→0.95)는 그대로.
- `DepthDarknessOverlayController`: 플레이어 구멍 반지름 −12%(`gasHazeHoleShrink`), 페더 +60%(`gasHazeFeatherGrow`), 어둠 색 약간 녹색(`gasHazeDarkTint` 0.35). `IsClearedByLight` 판정은 그대로.
- **선택 항목(연기 밀려남)은 구현하지 않았다.**

### D. 성능·접근성
- 화면 밖: 입자 풀 반환, 구름은 렌더러 컬링.
- 모션 감소(`ReduceMotion`): 코드에서 분기하지 않는다. 느린 스크롤·입자는 유지하고, 번쩍임/전체 펄스는 추가하지 않았다.

## 검사기 기본값 (프리팹/컴포넌트에서 조절)
- `GasZoneVisual.tuning`: core 0.10, noiseAmount 0.80, edgeWarp 0.26, floorBias 0.65, topDensity 0.30, noiseScaleA/B 0.35/0.22, scrollA (0.020, 0.006), scrollB (−0.012, 0.004), maxOpacity 0.70, motesPerZone 8, moteMaxAlpha 0.22, moteWorldSize 0.55. 전역 입자 상한 48(`GasCloudTuning.GlobalMoteCap`).
- `GasVisionWorldVeil`·`GasVisionOverlayDriver`: lightHoleInnerFraction 0.35, hazeTintStrength 0.5, hazeHoleExtraAlpha 0.08.
- `GasExposureEffectController.hazeSettings`: riseSeconds 1.2, fallSeconds 0.4.
- `DepthDarknessOverlayController`: gasHazeHoleShrink 0.12, gasHazeFeatherGrow 0.6, gasHazeDarkTint 0.35.

## 판정을 유지한 방식
`GasZone.Radius/Contains`, `CircleCollider2D` 반지름, `GasRadiusBlocks`/`LightClearRadiusBlocks`, 생성 1초·접근 페이드, 피해·감속·노출 로직은 수정하지 않았다. 시각 수치는 모두 반지름 안쪽에서만 감쇠하며 테스트로 고정했다.

## 빌더
- 신규 `GasVfxAssetBuilder`(메뉴 `SubTerra/UI/Build Gas Realistic VFX Assets (B-196)`): 노이즈 2장·방사 스프라이트·`GasCloud.mat`을 없을 때만 만든다.
- `PromptB50GasVisionBuilder`가 새 스프라이트·머티리얼·튜닝 기본값을 프리팹에 넣는다. `ApplyGasZonePrefab`/`ApplyLightPrefab` 두 번 실행해 부드러운 상태가 유지됨을 확인했다. **Integration 씬 단계(`ApplyIntegrationScene`)는 돌리지 않았다.**
- 변경된 프리팹: `GasZone.prefab`, `LightFacility.prefab`(GasClearance 자식 재생성으로 fileID 순서가 바뀌어 diff가 크다).

## 테스트
- 새/수정: `GasRealisticVisualTests`(14개 안팎), `GasVisionHoleEvaluatorTests`, `PromptB50GasVisionTests`. 가스 관련 5개 픽스처 29개 전부 통과.
- 전체 EditMode: Pass 2697 / Fail 26. 실패 26건은 모두 가스·조명과 무관한 UI 레이아웃, 디버그 터미널, 건설 배치, 퀘스트 UI 계열이다. **원래 main에서 같은 26건이 실패하는지는 비교하지 않았다 (미검증).**
- 에디터 렌더 캡처로 구름과 조명 구멍의 모양을 확인했다 (프리뷰 한정).

## 미검증
- 실제 플레이(Bootstrap → 광산)에서의 모습, 가스 안 틴트·탁한 시야, 가스 사라짐 후 즉시 걷힘, 여러 구역·화면 밖, 모션 감소 설정, Console 새 경고.
- `SpriteMask`(VisibleOutsideMask)와 커스텀 셰이더 조합, 실제 빌드에서의 셰이더 포함.
- 입자·구름의 실제 프레임 비용.

## 사용자 테스트 가이드
프롬프트 `prompt-B-196.md` "사용자 테스트 가이드" 1~7번을 그대로 따른다.

## 알아둘 점
- 전체 EditMode를 돌리면 폰트 SDF, UI 프리팹, Integration 씬, QualitySettings 등이 다시 저장돼 dirty가 된다. 커밋 전에 `git status`로 범위를 확인하고 이번 작업 파일만 포함해야 한다.
