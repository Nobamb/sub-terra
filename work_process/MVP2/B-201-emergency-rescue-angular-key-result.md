# B-201 긴급탈출 R키 각진 홀로그램 결과 (#358)

- 기준: `origin/main` `0a9a3f403b80a81f8540dd2c3137f7348c8a7c73`
- 브랜치: `MVP2-fix145` (원격에 같은 SHA로 이미 있어 이어서 사용)
- 커밋 해시: **미커밋** (사용자 동의 전이라 커밋·push 하지 않음)

## 바꾼 파일
- `Scripts/Shared/HologramKeycapPainter.cs` (신규): 각진 청록 키캡 그리기 공통 코드. 엔진 참조 없는 순수 C#(`HologramPixel` 구조체 반환). 팔각 윗면(Chamfer 6) + 옆면(Depth 5), 모따기 명암, 가로 주사선, 글리프 훅(`GlyphLevel`).
- `Scripts/Gameplay/Player/CushionPadHintHologram.cs`: 키캡 그리기를 공통 코드 호출로 교체(화살표 글리프만 남김). 그림 결과는 동일.
- `Scripts/App/UI/EmergencyRescue/EmergencyRescueArt.cs`: `RoundedRect()` 제거, `HologramKeycap()`(40x40, Point 필터, 글리프 없는 판) 추가.
- `Scripts/App/UI/EmergencyRescue/EmergencyRescueChipView.cs`: `KeycapTop/Side/Rim`(둥근 9-slice) → `KeycapCap` 한 장. 글자 `KeyLabel`=「R」 유지(색만 연한 청백, 윗면 중심으로 +2.5px).
- `Tests/EditMode/App/UI/PromptB135EmergencyRescueUiTests.cs`: 신규 테스트 4개.

## 재사용한 공통 코드
- App은 Gameplay를 참조할 수 없어 `CushionPadHintHologram`을 직접 쓰지 못함 → 두 쪽이 모두 참조 가능한 `SubTerra.Shared.HologramKeycapPainter`로 묶음. 표식은 UI `Image`+캔버스 경로 유지(월드 홀로그램/팔로우 변경 없음).

## 모션 감소
- `EmergencyRescueChipView.TickKeycap`: ReduceMotion이면 유도 누름 시작 안 함·진행 중이면 중단.
- `EmergencyRescueChipView.Apply`: ReduceMotion이면 키캡 이동·축소·밝기·발광 모두 정지. 실제 R/클릭은 컨트롤러 소관이라 그대로 동작.
- 건드리지 않은 것: 표식 등장·접힘·픽토그램 호버·호흡(breathe), 문구, 위치, 등장 조건, `SfxId.RescueHoloOpen`. 새 SFX 없음. 프리팹/씬 YAML 수정 없음.

## 테스트
- EditMode 실행(에디터 열린 상태, 플래그 러너): 신규 `Issue358_*` 4개 통과(각진 키캡·완충판과 같은 공통 그리기 픽셀 일치, 글자 R, ReduceMotion 정지+클릭 동작, 모션 켜면 유도 누름 복귀).
- 컴파일 오류 0.
- 전체 실행에서 기존 실패가 다수 나옴(`PromptB125*`, `IntegrationWiringTests`, `PromptB89*` 등). 실행마다 개수가 달라(32→51) 에디터 상태 의존으로 보이며 키캡과 무관. `PromptB85_ChipFollowTick_PlacesChipAbovePlayerHead`도 `Camera.main`/화면 좌표 의존으로 실패(키캡 변경과 무관해 보이나 **변경 전 베이스라인과 직접 비교는 미검증**).
- 렌더 캡처(에디터에서 ScreenSpaceCamera로 칩 렌더)로 팔각 키캡+R 확인.
- **미검증**: 실제 Play에서 전력 0/산소 위험 때 표식, 완충판 방향키와 나란히 비교, 모션 감소 토글 실플레이, 소리, 완충판 힌트 PlayMode 테스트(`PlayerCushionPadPlayModeTests`) 재실행.

## 사용자 테스트 가이드
1. 광산에서 전력 0 또는 `O2 14`로 만들어 머리 위 구출 요청 표식을 띄운다.
2. 아래 R키가 완충판 위 방향키와 같은 각진 청록 홀로그램인지 비교한다.
3. R 또는 표식 클릭으로 구출 창이 기존처럼 열리는지 확인한다.
4. 모션 감소 켜면 R키가 정지, 끄면 3~5초마다 약하게 눌리는지 본다.
5. 새 효과음 없음, Console 새 오류 없음 확인.
