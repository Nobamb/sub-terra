# Prompt-B 155 결과: 업그레이드 창 심층 구역 썸네일

- 기준 브랜치: #235(`codex/issue-223-deep-zone-display`)는 아직 main에 합쳐지지 않았다. 로컬 `codex/issue-223-deep-zone-display`(origin 브랜치 + 최신 main 병합 `37130308`)에서 `feature/b155-deep-zone-thumbnail`을 땄다.
- 커밋하지 않았다.

## 변경 파일 (`sub-terra/Assets/_Project/` 기준)

| 파일 | 내용 |
| --- | --- |
| `Scripts/App/Progression/DeepZoneDisplaySettings.cs` | 레벨별 `thumbnail`·`mineralMask`, `UnlockedLevelCount`, `BuildSummary`, `LevelTitle`, `LevelCondition`, `thumbnailStyle` |
| `Scripts/App/Progression/DeepZoneThumbnailStyle.cs` (신규) | 반짝임·흑백·해금 연출·대체 색 값 |
| `Scripts/App/UI/Progression/DeepZoneThumbnailStrip.cs` (신규) | 썸네일 칸 생성, 잠김/해금 그리기, 반짝임, 해금 연출 |
| `Scripts/App/UI/Progression/DeepZoneRevealTracker.cs` (신규) | 연출 대기 판단(순수 C#, 저장 안 함) |
| `Scripts/App/UI/Progression/UpgradeTreeView.cs` | `DeepZoneStatus` 칸을 높이 140 썸네일 칸으로 교체, `DeepZoneRevealBlocked` |
| `Scripts/App/UI/Progression/ProgressionPanelView.cs` | 해금 창이 보이는 동안(닫는 연출 포함) 연출을 미루는 조회를 넘김 |
| `Resources/DeepZoneThumbnailUI.shader` (신규) | `SubTerra/DeepZoneThumbnailUI`. UI-Default 기반, stencil·RectMask2D 유지 |
| `Resources/DeepZoneDisplaySettings.asset` | sprite 6개 연결, `thumbnailStyle` 기본값. 깊이·문구 그대로 |
| `Editor/DataValidation/Issue223DisplaySettingsBuilder.cs` | 빈 sprite 칸만 채움, Import 설정 보정 |
| `Editor/DataValidation/PromptB155DeepZoneThumbnailTestRunner.cs` (신규) | `SubTerra/Tests/Run Prompt-B 155 ...` 메뉴, `Temp/subterra-run-prompt-b155.flag` |
| `Art/UI/DeepZone/deep-zone-lv{1,2,3}.png`, `-minerals.png` (신규) | 임시 그림. Codex image_gen 원본을 480×160으로 줄이고 광물 마스크를 뽑음 |
| `Tests/EditMode/.../PromptB155DeepZoneThumbnailTests.cs` (신규), `PromptB1181UpgradeWindowTests.cs` | 새 테스트 15개, 기존 테스트는 조건 문구 위치만 Lv.1 칸으로 옮김 |
| `Tests/PlayMode/Integration/PromptB155DeepZoneThumbnailPlayModeTests.cs` (신규) | timeScale 0 재생, 해금 창 대기, 도중 비활성 |

프리팹·씬·ProjectSettings·폰트는 바꾸지 않았다. 전체 테스트가 건드린 파일은 `git restore`로 되돌렸다.

작업 자료(이 폴더): `source/codex-deep-zone-lv*.png`(Codex 원본 2172×724), `tools/build_deep_zone_thumbnails.py`, `preview-thumbnails-masks.png`, `render-1920-*.png`(잠김·연출 5프레임·해금), `render-1280-*.png`.

## 테스트
- EditMode B-155 + B-118-1 + B-56: 44 통과 / 0 실패.
- EditMode 전체: 1898 통과 / 19 실패. 실패 19개는 시설 배치·HUD·건설창 폭·판매창·골드 업그레이드 구매·메인 메뉴 호버 등이다. 이번 변경과 관련이 없다. B107 실패는 앞 테스트가 씬을 더럽혀서 난다(단독 실행 시 통과). 변경 전 기준 실행은 하지 않아 기존 실패인지 직접 비교하지는 않았다(미검증).
- PlayMode B-155만: 2 통과 / 0 실패.

## 크기
- 칸: 1920×1080 기준 324.7×106px(테두리 안 322.7×104). 지상 기지·광산 업그레이드 창 모두 같다. 1280×720에서는 0.667배.
- 트리 영역: 1006×668 → 1006×520. 트리는 폭 기준 맞춤이라 배율(0.914)이 그대로다.
- 권장 원본은 480×160(3:1) 그대로다. 칸 비율이 약 3.1:1이라 위아래 약 3%가 잘린다(Envelope).

## 그림 넣는 법
- 경로: `Assets/_Project/Art/UI/DeepZone/deep-zone-lvN.png`, `deep-zone-lvN-minerals.png`. 같은 이름으로 덮어쓰면 바로 반영된다.
- Import: Sprite(Single), Filter Point, 압축 없음, Mipmap 끔, Mesh Type Full Rect. 새 파일은 `SubTerra/UI/Create Deep Zone Display Settings`를 실행하면 빈 칸에 연결되고 Import도 맞춰진다.
- 마스크: 썸네일과 같은 크기의 투명 PNG. 광물 픽셀만 원래 색(조금 밝게)으로 남기고 나머지는 알파 0. `tools/build_deep_zone_thumbnails.py <source_dir> <out_dir>`로 다시 만들 수 있다.

## 조정 값
`Resources/DeepZoneDisplaySettings.asset` → `Thumbnail Style`
- 반짝임: `glintPeriod` 2.6초(최소 2), `glintPeriodSpread` 0.07, 잠김/해금 색, 알파 0.15~0.45 / 0.6~0.9. 칸마다 위상을 0.37주기씩 어긋낸다.
- 흑백: `lockedGrayBrightness` 0.6, `glintGrayBrightness` 1.1, 셰이더 대체 색 (0.45, 0.45, 0.45).
- 해금: `unlockDuration` 1초, `lockCenter` (0.5, 0.66), `revealRadius` 1.2, `revealFeather` 0.18, `shackleKick` 6, `lockDrop` 40, `lockDropAngle` 25.
- 그림 없음 대체: `fallbackColor`, `fallbackDarkenPerLevel`.

## 동작 메모
- 연출 구간: 0~0.2 진동(sin 8π, 12°) → 0.2~0.3 딸깍(고리 6px, 1→1.1→1) → 0.3~0.65 떨어짐(40px, 25°, EaseIn) / 0.3~1.0 색 퍼짐과 청록 테두리, 칸 테두리 청록 점등 → 0.85~1.0 광물 Pulse. u=1은 해금 상태 그리기와 같은 함수다.
- 해금 직후(DidUnlockNow) 늘어난 레벨만 대기한다. 해금 창이 닫히고 업그레이드 창이 보일 때 한 번 재생된다. 그 사이 DidUnlockNow=false 새로고침이 와도 대기는 유지된다.
- 처음 그릴 때, 다시 열 때, 세이브를 불러올 때는 연출 없이 그린다. 대기 상태는 저장하지 않는다.

## 남은 문제 / 미검증
- 실제 플레이(Bootstrap → 새 게임 → U 창, 지상 기지 창)로 수동 체크리스트를 확인하지 않았다. 확인은 오프스크린 렌더와 테스트로만 했다(미검증).
- 반짝임 체감, 커서 동작, 36~40M 채굴 제한은 미검증이다(관련 코드는 바꾸지 않음).
- 임시 그림은 painterly 톤이다. Point 필터로 약 0.68배 축소 표시되며 렌더에서 거친 느낌은 크지 않았다. 도트 원본으로 바꾸면 그대로 맞는다.
