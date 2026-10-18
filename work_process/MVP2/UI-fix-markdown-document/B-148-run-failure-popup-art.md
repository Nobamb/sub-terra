# 사망 원인별 팝업 이미지

내장 `image_gen` 도구로 생성한 전용 이미지. 바깥 배경의 투명도를 보존하고 Unity에서 Single Sprite / Full Rect / Mipmap Off로 가져온다.

- 가스 (`RunFailureCause.GasExposure`): `Assets/_Project/Resources/UI/RunFailure/gas-death-popup.png`
- 그 외 (`StructuralCollapse`, `Fall` 등): `Assets/_Project/Resources/UI/RunFailure/impact-death-popup.png`

`RunFailurePanelView`가 Resources에서 직접 로드한다. 별도 Inspector 연결이나 Scene 재저장은 필요 없다. 기존 공용 프레임·틴트·상시 후광 대신 생성 이미지 자체를 표시하며, 글리치 복제본에도 같은 이미지를 적용한다.

## 변경 및 검증

- `Scripts/App/UI/RunFailure/RunFailurePanelView.cs`: 원인별 이미지 로딩, 글리치 복제본 교체, 새 프레임에 맞춘 내부 여백 및 9-slice 테두리 표시.
- `Tests/EditMode/App/Run/RunFailureServiceTests.cs`: 5가지 원인별 본체·글리치 이미지 선택 및 재표시 회귀 테스트.
- 추가 에셋: 위 PNG 2개와 Unity가 생성한 PNG `.meta` 2개 및 `RunFailure.meta` 폴더 메타.
- Edit Mode `RunFailureServiceTests`: 23 passed / 0 failed / 0 skipped.
- 1920×1080 Unity Preview Scene에서 가스(빈 화물)·추락(화물 2종) 화면 렌더 확인. 검증용 이미지는 `Temp/run-failure-GasExposure.png`, `Temp/run-failure-Fall.png`.
- 최종 Unity Console 오류 0개. 실제 게임 진행 중 사망 재현과 플레이어 빌드는 실행하지 않음.
- 저장 데이터 및 사망 판정 로직 변경 없음.

## 최종 생성 프롬프트: 가스

Use-case: ui-mockup. Production game UI sprite, not a screenshot. Create ONE complete empty game-over popup panel for a subterranean sci-fi mining survival game. Landscape 4:3, intended display 880 x 675. Orthographic perfectly front-facing flat 2D asset, precisely centered and symmetrical, outer silhouette filling entire canvas with only 1% transparent margin. Exterior background genuinely transparent; panel interior OPAQUE near-black forest green. Entire physical frame made of matte oxidized DARK GREEN painted industrial metal, thick beveled chamfered corners, inset layered edges, bolts and machined vents. Restrained lime-green indicator strips integrated INTO the frame, no neon halo, no bloom outside panel, no added light overlay. Toxic gas death visual identity: small non-text biohazard/respirator relief symbols confined to upper corners, subtle poison corrosion and chemical staining around outer rim. Premium hand-painted realistic 2D game-interface style with crisp machined details, grounded industrial sci-fi. Layout must leave the full inner rectangle x=8%..92%, y=6%..93% plain dark green-black and very low contrast for runtime heading, subtitle, inventory list, preservation badge and bottom button. Decorative details ONLY within outer 6% edges and tiny corners. No text, no letters, no numbers, no mock UI content, NO buttons or interior compartments or grids, no characters, no scene, no skulls, no gore. This must be a fully new green physical popup shell, not a blue panel tinted with green light.

## 최종 생성 프롬프트: 충돌·추락

Use-case: ui-mockup. Production game UI sprite, not a screenshot. Create ONE complete empty GAME OVER popup panel for subterranean sci-fi mining survival. Landscape 4:3, intended display 880 x 675. Orthographic perfectly front-facing flat 2D asset, centered symmetrical, outer silhouette filling canvas with only 1% transparent margin. Exterior genuinely transparent, interior OPAQUE almost-black burgundy. Entire thick physical frame made of matte DARK CRIMSON and oxblood-red painted industrial metal, beveled chamfered corners, layered inset edges, bolts, machined mechanical vents. Red warning-strip inlays and a few sharp diagonal impact fractures/chips confined to the outer metal corners communicate structural collapse and fall death. Tiny solid triangular warning-symbol reliefs in upper outer corners, no lettering. Red coloration is the actual painted material, not a lighting filter. Restrained scarlet indicator strips built INTO rim, NO neon halo and NO bloom outside panel. Premium hand-painted realistic 2D interface art, crisp machined details, grounded industrial sci-fi. Leave entire inner rectangle x=8%..92%, y=6%..93% plain dark red-black and low contrast for runtime heading, subtitle, inventory list, preservation badge, bottom button. Decorations ONLY within outer 6% edges and tiny corners. No text/letters/numbers, no mock UI, no buttons or inner compartments/grids, no characters, no background scene, no skulls, no gore. A fully new red physical popup shell, not a blue panel tinted by red light.
