# Prompt-B 111 — 일반 블록의 드론 스캔 강조 제외

## 원인과 수정

- `DroneSensor`는 이미 금·광물·활성 가스·봉인 신호만 스캔 대상으로 수집한다.
- `DepthDarknessOverlayUI.shader`가 대상 주변 3×3 셀의 광원 효과를 합산하면서, 인접한 일반 블록의 암부까지 지우고 있었다.
- 현재 셀의 점유 여부와 실제 스캔 대상 여부로 광원 효과를 제한했다. 감지되지 않은 점유 블록은 기존 암부를 유지하며, 감지 대상과 빈 공간에는 기존 빛 효과가 남는다.
- 감지 조건, 업그레이드 반경, 펄스 주기·유지 시간, 금·광물·가스·봉인 신호의 색상은 변경하지 않았다.

## 변경 파일

- `sub-terra/Assets/_Project/Resources/DepthDarknessOverlayUI.shader`
- `sub-terra/Assets/_Project/Tests/EditMode/App/Integration/DepthDarknessOverlayControllerTests.cs`
- `sub-terra/Assets/_Project/Tests/EditMode/Gameplay/Drone/DroneSensorUpgradeTests.cs`
- 이 결과 문서

## 검증

- Unity Editor(URP)에서 실제 셰이더를 RenderTexture로 렌더링해 픽셀을 비교했다.
- 수정 전 회귀 테스트 실패: 인접 일반 블록 표본의 명도가 0에서 약 0.894로 상승했다.
- 수정 후 일반 블록의 변·모서리 7곳은 스캔 전후 명도가 같고, 대상 블록 및 빈 공간의 광원은 유지됨을 확인했다.
- 스캔 반경 3·7에서 일반 블록이 대상 목록·활성 표식에 포함되지 않는 테스트를 추가했다.
- Edit Mode: 암부 39개, 드론 센서 7개, 봉인 신호 1개 — **47 통과 / 0 실패 / 0 건너뜀**.
- 결과 XML: Unity 프로젝트의 `Temp/prompt-b111-before.xml`, `Temp/prompt-b111-after.xml`.
- Unity Console 오류·경고 0. `git diff --check` 통과.
- Scene/Prefab/Font/ProjectSettings/Packages 변경 없음. 기존 `init/prompt-B.md` 변경은 보존했다.

## 검증 한계

- 실제 탐사 Game View의 수동 플레이 및 Windows 빌드는 실행하지 않았다. 시각 검증은 실제 셰이더를 사용하는 자동 렌더링 테스트로 수행했다.
- 사용자 설정 파일 `sub-terra/.claude/settings.local.json`은 수정하지 않았다.
