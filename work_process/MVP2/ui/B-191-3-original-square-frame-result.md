# B-191-3 원본 건설창 프레임의 정사각형 편집

2026-11-10.

기존 건설창이 사용하던 `Assets/_Project/Art/UI/Inventory/inventory-window-frame.png`를 imagegen 편집 입력으로 전달해 정사각형으로 다시 만들었다. 원본의 두꺼운 청록·흰색 이중 발광 테두리, 금속 모서리와 짙은 청록색 긁힘 질감을 살렸다. 위쪽 탭과 측면 굴곡을 정리했다.

최종 이미지는 `sub-terra/Assets/_Project/Art/UI/Gameplay/building-card-square.png`에 적용했다. 기존 meta GUID를 유지했다. 큰 목록·상세창은 560×560, 작은 카드는 300×300으로 배치한다. 목록 높이는 420으로 두어 여섯 행이 온전히 보인다. 네온 선이 창 경계에 맞고 글로우가 바깥으로 번지도록 프레임 여백과 9-slice를 조정했다.

## 변경 파일

- `sub-terra/Assets/_Project/Art/UI/Gameplay/building-card-square.png`와 `.meta`
- `sub-terra/Assets/_Project/Editor/DataValidation/PromptB188BuildingModeBuilder.cs`
- `sub-terra/Assets/_Project/Prefabs/UI/BuildingMenu.prefab`
- `sub-terra/Assets/_Project/Tests/EditMode/App/UI/PromptB191BuildingModeTests.cs`
- `sub-terra/Assets/_Project/Tests/EditMode/App/UI/PromptB191FollowupTests.cs`
- `init/rules/lessons/2026-11-10-issue304-reference-frame-redraw.md`
- 이 보고서

## 이미지 생성

내장 `image_gen` 도구를 두 번 사용했다. 첫 편집으로 원본의 외형을 정사각형으로 만들고, 두 번째 편집으로 테두리 배치와 발광 여백을 좁혀 작은 카드의 가독성을 확보했다. 최종 PNG는 1254×1254이며 외곽에 알파 채널이 있다.

## 검증

- 관련 EditMode 57/57 통과. `sub-terra/Builds/BuildingUi/original-style-edit-final/EditMode-summary.txt`.
- Visual PlayMode 1/1 통과. Bootstrap 기반 격리된 탐사 Scene에서 1920×1080, 1366×768, 2560×1440의 카드·목록·상세·검색·정렬·클릭 영역을 확인했다. `sub-terra/Builds/BuildingUi/original-style-visual/PlayMode-summary.txt`.
- 카드·목록·상세를 절반 해상도로 묶은 화면을 검토했다. 네온 프레임 안에 이름·설명·비용·상태가 표시되고 큰 목록은 여섯 행이 온전히 보인다. `sub-terra/Builds/BuildingUi/original-style-preview.jpg`.
- Unity Console 오류 0개. 입력 코드와 Inspector 연결은 기존 경로를 사용하며 프리팹은 Editor 빌더로 저장했다.
- 미검증 항목은 Windows 플레이어 빌드 실행이다. 수동 확인 시 B → 시설 선택 → 카드, `>>` → 목록 → 행 선택 → 상세에서 프레임과 텍스트를 확인한다.

첫 편집 프롬프트

```text
Use case: precise-object-edit. Edit target: the provided existing game window frame image. Adapt THIS EXACT frame into a true square version for the construction window and small construction card of the same game. Preserve its distinctive bright thick cyan/white double-line neon outline, strong cyan outer glow, weathered dark teal metal interior, scratched peripheral texture and metallic beveled corner brackets. Preserve the visual identity and brightness of the supplied frame. Geometry change only: make the main panel exactly square with four equal-length straight sides and small identical 45-degree clipped corners. Remove the raised top-center tab and remove both side indentations, continuing the same cyan double-outline straight across them. Uniform square silhouette, centered, equal transparent outer glow margins on all four edges. The central panel is opaque nearly black teal and calm enough for live readable UI text, with subtle wear concentrated near the edges. No text, labels, symbols, buttons, extra objects, logos, or watermark. Straight-on flat 2D game UI sprite. Deliver a square 1024x1024 RGBA texture with actual transparency outside the cyan glow, opaque dark center. Keep the frame inside the outer 8 percent band so a 96-pixel 9-slice border can preserve all corners. This is a faithful square adaptation of the reference, retaining the bold luminous border.
```

테두리 보정 프롬프트

```text
Edit this square frame image with one geometric refinement while preserving its exact original bright white/cyan double-line neon style, dark scratched teal center, corner brackets and square shape. Move the luminous frame outline outward so the main WHITE outline lies only 4 percent of the image width in from ALL four image edges (approximately 40px on a 1024 square). The corner bevel and metal brackets must fit entirely within the outer 9 percent of the square. The glowing band should be compact, smooth and evenly feathered to actual transparency outside, without paint splashes or ragged cyan blobs. Keep a small even transparent safety margin so the glow is not clipped at the canvas edge. Opaque dark interior. No top tab, no side notches, equal straight sides, small identical clipped corners. Preserve the original vivid cyan brightness and weathered metallic texture. This is for a small readable game card: at least central 88 percent must be dark quiet interior free of bright border. No text, icons, labels, buttons or extra elements. Square RGBA asset.
```

