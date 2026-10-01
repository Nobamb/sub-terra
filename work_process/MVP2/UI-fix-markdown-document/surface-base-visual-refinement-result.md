# Surface Base 상단 및 액션 아이콘 수정 결과

참고: `concept-image/in-game/surface-base/surface-base-concept.png`.

- 지상 기지 제목을 기준 해상도에서 8px 아래로 이동.
- 화물/골드 아이콘과 왼쪽 정렬 텍스트 사이를 각각 12px로 통일. 두 그룹 중심 간격은 252px이며, 표시 그룹 사이에 여백을 확보.
- 상단 프레임을 폭에 맞춰 늘리고 내부 항목은 프레임 기준 비율 앵커 사용.
- 설정/종료 버튼: 80×70 → 72×62, 아이콘 47×47 → 38×38. 프레임 안쪽에 배치.
- 탐사 버튼: 글자 최대 크기 49 → 62, 왼쪽 광산 아이콘과 오른쪽 문구로 배치.
- 판매/업그레이드: 새 동전 묶음/교차 공구 아이콘 및 내부 배치 적용.
- 초기화: 순환 아이콘, 청록 문구, 별도 노란 비용 텍스트. 기존 런타임 견적 값과 한국어/영어 문구를 사용.

수정 파일 (Unity 프로젝트 `sub-terra/` 기준):

- `Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab`
- `Assets/_Project/Editor/DataValidation/PromptB117SurfaceBaseBuilder.cs`
- `Assets/_Project/Scripts/App/UI/SurfaceBase/SurfaceBaseView.cs`
- `Assets/_Project/Tests/EditMode/App/UI/MineResetSurfaceBaseTests.cs`

새 PNG와 Unity 생성 메타데이터:

`Assets/_Project/Art/UI/SurfaceBase/Icons/` 아래 `icon-cargo.png`, `icon-gold.png`, `icon-mine.png`, `icon-sell.png`, `icon-upgrade.png`, `icon-reset.png`, `icon-settings.png`, `icon-quit.png` 및 각 `.meta`, 폴더 `Icons.meta`.

기존 배경 및 버튼 프레임을 사용한다. 이미지 파일은 투명 PNG이며 Sprite Single, Full Rect, 최대 512, 밉맵 없음, 무압축으로 가져왔다. 기존 프리팹 GUID를 유지했다.

검증:

- 관련 EditMode 테스트 **21/21 통과**, 실패/스킵 0.
- 비용 변경(875G/1250G), 한국어/영어 초기화 문구, 음수 비용의 0G 표시 검증.
- 자원 상태 갱신, 버튼 장식의 레이캐스트 차단 여부, 업그레이드 닫기/설정 전환, 설정 드롭다운 구조 회귀 검증.
- 1920×1080, 3440×1440, 1440×1080 격리 렌더링 확인. 실제 씬의 CanvasScaler(1920×1080, Match Width = 0)를 따름.
- 주요 문구가 한 줄로 표시되고 잘리지 않음.
- 실제 SurfaceBase 씬을 읽고 저장 없이 닫아 Canvas/GraphicRaycaster, EventSystem 1개, Missing Script 0 확인.
- 코드 diff 공백 검사 통과. 수정 범위는 해당 UI/빌더/표시 코드/테스트/아이콘.
- 씬, ProjectSettings, Packages, 폰트, Shared 및 Gameplay 변경 없음.
- Unity Console 오류/예외 0 확인.

증거는 저장소 `Temp/SurfaceBaseRefinement/`의 `surface-base-1920.png`, `surface-base-3440.png`, `surface-base-1440.png`, `editmode-results.xml`, `editmode-summary.txt`에 저장했다.

재적용: `SubTerra/UI/Refine Surface Base Header and Action Icons`. 이 메뉴는 SurfaceBasePanel 프리팹과 지정 아이콘만 수정한다. 기존 전체 117 빌더도 새 배치를 사용한다. Inspector의 새 `resetMineFeeText`는 빌더가 FeeLabel에 연결했다.

실제 탐사·판매·초기화·종료 실행과 Windows 빌드/세이브 QA는 이번 시각 수정에서 실행하지 않았다. 기존 기능 연결을 유지했다.

이미지 생성: 내장 `image_gen` 도구, 원본은 생성 폴더에 남기고 프로젝트로 복사했다. 아래는 각 최종 PNG의 실제 생성 프롬프트다.

<details>
<summary>아이콘 생성 프롬프트 8종</summary>

```json
[
  {
    "name": "cargo",
    "prompt": "Use case: stylized-concept. Asset type: one isolated Unity uGUI icon, transparent PNG. Reference image: attached surface base concept for icon shape/style only. Primary request: A cyan isometric cargo crate cube. Black thin outlines separating top and two side faces, bright turquoise faces, simple small recessed black square markings. Match the tiny top right cargo cube of the reference. Composition: ONE icon centered, occupies 85 percent of square canvas, no other elements. Style: crisp flat game UI pictogram, slightly beveled highlights, few broad solid shapes, very readable at 44 pixels, coordinated science fiction cyan mine interface. Background: actual transparent alpha, including holes/openings. No frame, no button, no scenery, no text, no labels, no watermark, no large glow or shadow. Generate only the specified icon."
  },
  {
    "name": "gold",
    "prompt": "Use case: stylized-concept. Asset type: one isolated Unity uGUI icon, transparent PNG. Reference image: attached surface base concept for icon shape/style only. Primary request: Three small neat stacks of shiny gold coins, tallest stack center back, shorter stacks front left and front right. Warm yellow top surfaces, amber side surfaces, thin black outlines, broad legible shapes. Match top right gold stacks in reference. Composition: ONE icon centered, occupies 85 percent of square canvas, no other elements. Style: crisp flat game UI pictogram, slightly beveled highlights, few broad solid shapes, very readable at 44 pixels, coordinated science fiction cyan mine interface. Background: actual transparent alpha, including holes/openings. No frame, no button, no scenery, no text, no labels, no watermark, no large glow or shadow. Generate only the specified icon."
  },
  {
    "name": "mine",
    "prompt": "Use case: stylized-concept. Asset type: one isolated Unity uGUI icon, transparent PNG. Reference image: attached surface base concept for icon shape/style only. Primary request: A large cyan stylized rocky mountain silhouette with a dark arched mine entrance and cyan timber supports inside it. Jagged mountain peaks, three subtle dark rock fissures, flat bottom, bold cyan silhouette. Match the cave entrance pictogram at left of primary button in reference. Composition: ONE icon centered, occupies 85 percent of square canvas, no other elements. Style: crisp flat game UI pictogram, slightly beveled highlights, few broad solid shapes, very readable at 44 pixels, coordinated science fiction cyan mine interface. Background: actual transparent alpha, including holes/openings. No frame, no button, no scenery, no text, no labels, no watermark, no large glow or shadow. Generate only the specified icon."
  },
  {
    "name": "sell",
    "prompt": "Use case: stylized-concept. Asset type: one isolated Unity uGUI icon, transparent PNG. Reference image: attached surface base concept for icon shape/style only. Primary request: A small stack of three pale icy white/cyan coins, with one extra coin resting front right at the bottom. Elliptical tops, turquoise edges and dark thin outlines. Match resource sale button icon in reference. Composition: ONE icon centered, occupies 85 percent of square canvas, no other elements. Style: crisp flat game UI pictogram, slightly beveled highlights, few broad solid shapes, very readable at 44 pixels, coordinated science fiction cyan mine interface. Background: actual transparent alpha, including holes/openings. No frame, no button, no scenery, no text, no labels, no watermark, no large glow or shadow. Generate only the specified icon."
  },
  {
    "name": "upgrade",
    "prompt": "Use case: stylized-concept. Asset type: one isolated Unity uGUI icon, transparent PNG. Reference image: attached surface base concept for icon shape/style only. Primary request: Two crossed mechanical tools: open ended wrench and screwdriver, icy white faces with turquoise edge accents and thin dark outlines, diagonally crossed into an X. Match upgrade button icon in reference. Composition: ONE icon centered, occupies 85 percent of square canvas, no other elements. Style: crisp flat game UI pictogram, slightly beveled highlights, few broad solid shapes, very readable at 44 pixels, coordinated science fiction cyan mine interface. Background: actual transparent alpha, including holes/openings. No frame, no button, no scenery, no text, no labels, no watermark, no large glow or shadow. Generate only the specified icon."
  },
  {
    "name": "reset",
    "prompt": "Use case: stylized-concept. Asset type: one isolated Unity uGUI icon, transparent PNG. Reference image: attached surface base concept for icon shape/style only. Primary request: Two cyan curved arrows forming a circular refresh cycle, with two clear arrow heads and two gaps, simple flat turquoise strokes with very subtle pale highlight. Match reset button circular arrows icon in reference. Composition: ONE icon centered, occupies 85 percent of square canvas, no other elements. Style: crisp flat game UI pictogram, slightly beveled highlights, few broad solid shapes, very readable at 44 pixels, coordinated science fiction cyan mine interface. Background: actual transparent alpha, including holes/openings. No frame, no button, no scenery, no text, no labels, no watermark, no large glow or shadow. Generate only the specified icon."
  },
  {
    "name": "settings",
    "prompt": "Use case: stylized-concept. Asset type: ONE isolated Unity uGUI icon on transparent alpha background. Reference image: provided surface base concept for visual style. Primary request: A simple eight tooth settings gear, icy pale cyan flat front face, dark circular hole through center, subtle bright turquoise edge bevel. Match the reference image top right gear, broad silhouette very clear at 36 pixels. Centered square canvas, 85 percent occupancy. Crisp flat UI pictogram with subtle bevel, cyan sci-fi game interface. No surrounding button or border, no text, no scenery, no shadow, no large glow. Transparent holes. Generate only this icon."
  },
  {
    "name": "quit",
    "prompt": "Use case: stylized-concept. Asset type: ONE isolated Unity uGUI icon on transparent alpha background. Reference image: provided surface base concept for visual style. Primary request: A simple turquoise EXIT/LOGOUT symbol: an upright open door rectangular frame with a right pointing arrow exiting through it; geometric thin bright cyan door frame and bold cyan arrow. Match reference top right exit door icon, very clear at 36 pixels. No power symbol. Centered square canvas, 85 percent occupancy. Crisp flat UI pictogram with subtle bevel, cyan sci-fi game interface. No surrounding button or border, no text, no scenery, no shadow, no large glow. Transparent holes. Generate only this icon."
  }
]
```

</details>

