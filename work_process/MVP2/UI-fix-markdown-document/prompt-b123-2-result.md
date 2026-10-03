# Prompt-B 123-2 — '새 광산 구역' 팝업 컨셉 개편 결과

기준 이미지: `concept-image/in-game/init-mine/init-mine-popup-concept.png`

## 기존 구조 조사

- 팝업은 `SurfaceBasePanel.prefab`의 `ResetMineConfirm`(Canvas 700, 전체 화면 딤)이다. 직전 B-123-1에서는 퀘스트 완료창 프레임 한 장 위에 비용 패널과 아이콘·제목·설명 3행, 버튼 2개를 올린 구조였다. 이 구조는 `MineResetSurfaceBaseLayoutBuilder`가 만든다. `SurfaceBase.unity`에는 이 팝업의 오버라이드가 없다.
- 데이터: `SurfaceBaseBinder.OnResetMineClicked`가 `SurfaceBasePresenter.TryGetMineResetQuote`로 보유 골드와 비용을 받아 `SurfaceBaseView.SetMineResetConfirmVisible(true, gold, fee)`를 호출한다. 실행 경로는 `OnResetMineConfirmed` → `SaveRuntimeController.TryResetMine` → `MineResetService.TryReset`이다. 이 경로가 골드 차감, 새 시드, 주기 리셋, 시설 쿨다운 초기화를 처리하고, `finally`에서 팝업을 닫는다.
- 애니메이션은 따로 없었고 `SetActive`로 즉시 켜고 껐다.

## 변경 파일

| 구분 | 파일 |
| --- | --- |
| 신규 | `Scripts/App/UI/SurfaceBase/MineResetPopupTimeline.cs`: 등장·닫기 시간표, 호흡 곡선, 화면 맞춤 배율. 순수 C# |
| 신규 | `Scripts/App/UI/SurfaceBase/MineResetPopupMotion.cs`: 연출 MonoBehaviour. unscaled 시간 사용 |
| 신규 | `Art/UI/SurfaceBase/MineReset/*.png` (28장), `MineResetTitleGlow.mat`, `MineResetCostGlow.mat` (TMP 발광 프리셋. 폰트 에셋은 수정하지 않음) |
| 신규 | `Tests/EditMode/App/UI/MineResetPopupTimelineTests.cs` |
| 신규 | `work_process/.../b123-2-art/make_init_mine_popup_art.py`(그림 생성), `init_mine_popup_layout.json`(좌표 메타) |
| 수정 | `Editor/DataValidation/MineResetSurfaceBaseLayoutBuilder.cs`: 카드를 레이어 구조로 다시 생성. 메뉴는 `SubTerra/UI/Build Prompt-B 123-2 Mine Reset Popup (SurfaceBase only)`(기존 메뉴도 같은 빌드를 실행) |
| 수정 | `SurfaceBaseView.cs`: 연출 연결, 닫는 중에는 보이지 않는 상태로 취급, 골드 천 단위 표기 |
| 수정 | `LocalizationService.cs`: 골드 `{0:N0}`, 초기화·유지 문구를 `·` 구분으로 변경 |
| 수정 | `MineResetSurfaceBaseTests.cs`: 새 레이어 구조 기준으로 다시 작성 |
| Prefab | `Prefabs/UI/SurfaceBasePanel.prefab`: `ResetMineConfirm` 하위만 다시 생성. 빌더가 상수 경로 한 곳만 저장 |

씬, 폰트, 다른 UI 프리팹은 변경하지 않았다(`git status` 확인).

## 레이어 구조

```
ResetMineConfirm (Canvas 700, 딤 Image, CanvasGroup, MineResetPopupMotion)
└ ResetMineCard 1332×1021 (컨셉 비율 그대로: 1px = 1.148 단위)
  ├ Body (RectMask2D: 높이만 펼쳐짐)
  │   ├ Panel        금속 내부 패널
  │   ├ Cave         팝업 내부 광산 배경(육각형·링을 지우고 다시 채움)
  │   ├ CaveGlows/0~2 광물 덩어리별 발광
  │   ├ Frame        각진 청록 외곽 프레임 + 금속 브래킷
  │   └ FrameFlash   등장 순간 테두리 섬광
  ├ ScanLine / EdgeTop / EdgeBottom  TV 켜짐 가로 빛
  ├ Hex
  │   ├ HexRings     HUD 동심 호
  │   ├ HexScale (균일 배율)  HexMine · HexTunnelGlow · HexCrystalGlow · HexBorderGlow · HexBorder
  │   ├ CoreGlow     중앙의 작은 청록 빛
  │   └ Motes/0~9    느린 빛 입자
  └ Content (CanvasGroup: 알파만 변경)
      Title · TitleDivider · Description · CostPanel(AmountRow[GoldIcon, CostText], BalanceText)
      ResetRow · KeepRow(배지 아이콘, 제목, 설명) · TimerRow(시계, 제목 | 설명)
      CancelButton · ConfirmButton(AccentRoot[청록 버튼 그림, 호버])
```

버튼 면을 제외한 모든 Image·TMP는 `raycastTarget=false`이다(테스트로 고정). 상단 X 버튼은 없다.

## 에셋

| 종류 | 방식 |
| --- | --- |
| 외곽 프레임, 금속 브래킷 | 컨셉에서 추출. 바깥 외곽선 다각형(모서리 48px)으로 잘라 내고, 안쪽 발광은 기본 금속색 기준으로 알파를 분리 |
| 내부 광산 배경 | 컨셉에서 추출. 육각형, 링, 윗변 섬광 영역은 지우고 주변 색으로 채운 뒤 흐림과 잔결 처리 |
| 광산 입구 | 컨셉에서 육각형 다각형으로 추출(가려지는 아래 줄은 위 줄을 늘려 채움) |
| 비용·안내·탐사 시간 패널, 버튼 2종 | 컨셉에서 추출 후 글자를 줄 단위 보간과 잔결로 지움. 호버 그림은 밝기를 올려 파생 |
| 골드 바, 수정·광차 배지, 시계, 구분선, 제목 장식선 | 컨셉에서 추출(배경 기준 알파 분리) |
| 육각 테두리·발광, 동심 호, 중앙 빛, 스캔 라인, 광물 발광, 금속 패널 질감 | 코드로 생성 |
| 빛 입자 | 기존 `particle-dot.png` 재사용 |

모든 그림은 레이어 단위로 따로 들어 있다. 컨셉 전체를 한 장으로 깐 부분은 없다. 추출한 그림은 2배로 키워(Lanczos) 4K에서 덜 흐리게 했다. 원본 해상도(1672px)의 한계는 남아 있어 4K 확대 시 정식 아트보다 조금 부드럽다.

## 연출

- **등장(정착 0.60초, 섬광 꼬리 포함 0.95초)**: 0~0.12초 중앙 가로 빛이 좌우로 뻗음 → 0.10~0.42초 `Body` 마스크가 위아래로 펼쳐짐(EaseOutCubic, 탄성 없음, 위아래 가장자리 빛이 따라감) → 0.36~0.64초 테두리 섬광이 짧게 밝아졌다 안정 → 0.28~0.46초 본문 페이드인(알파만 바뀌어 글자 왜곡 없음). 조작은 0.28초부터 가능하다.
- **육각형(겹쳐 진행)**: 0.30초 중앙 작은 빛 → 0.34~0.60초 테두리와 입구가 균일 배율 0.12에서 1로 커짐 → 완성 순간 테두리·광물 섬광 → 0.95초까지 유지 수준으로 감쇠. 이후 위치·크기는 고정된다.
- **유지 발광**: 육각 테두리 발광 2.8초 주기, 육각 내부 광물 3.1초, 터널 빛 3.6초, 배경 광물 3개는 2.4/2.95/3.5초에 위상을 다르게 줬다. 빛 입자 10개는 4.2~6.3초 수명으로 천천히 오른다. 유지 세기는 첫 섬광보다 약하다. 배경 그림, 글자, 버튼에는 밝기 변화가 없다.
- **닫기(0.26초)**: 발광·입자 0.12초 소등, 창 전체 0.04~0.26초 페이드아웃. 끝나면 `SetActive(false)`.
- **안정성**: unscaled 시간(프레임당 진행량 0.05초 제한)을 써서 `timeScale=0`에서도 재생된다. 닫는 중 다시 열면 닫기 콜백을 버리고 0초부터 다시 연다. 닫는 동안은 `IsMineResetConfirmVisible=false`, 루트 `CanvasGroup.interactable=false`이다. 딤은 클릭을 막는다. 화면이 낮으면(예: 21:9) 카드를 균일 배율로 줄인다.
- **중복 차감 방지**: 실행 성공 후 바인더의 `finally`가 닫기를 시작하면 그 즉시 팝업은 보이지 않는 상태가 된다. 따라서 이어진 클릭은 `OnResetMineConfirmed`의 `!IsMineResetConfirmVisible` 검사에서 무시된다. 기존 `mineResetBusy` 가드도 그대로 있다.

## 데이터·문구

- 비용·보유·이용 후 골드는 기존처럼 `TryGetMineResetQuote` 값을 쓴다. 표기는 `3,000 G`처럼 천 단위 쉼표를 넣는다(InvariantCulture). 골드가 부족하면 빨간 부족분 표시와 함께 실행 버튼이 회색으로 비활성화된다.
- 초기화: `채굴한 타일 · 지하 시설 · 붕괴 · 가스`. `MineResetService`가 월드 스냅샷을 새 시드로 바꾸고 시설 쿨다운을 비우는 동작과 일치한다.
- 유지: `업그레이드 · 심층 해금 · 보유 광물`(컨셉 문구). 남은 골드도 유지되지만 비용 패널의 "이용 후 N G"로 보여 주므로 컨셉처럼 문구에서는 뺐다.
- 탐사 시간: `MineResetService.CycleDurationSeconds`로 계산한 `3시간으로 다시 시작`.

## 검증

- Play Mode(Bootstrap → 슬롯 1 이어하기 → SurfaceBase, 3840×2160 Game View)에서 `timeScale=0` 상태로 등장 → 유지 → 취소 닫기를 프레임 단위로 캡처했다.
  - 실시간 GIF: [evidence/prompt-b123-2/prompt-b123-2-open-idle-close.gif](evidence/prompt-b123-2/prompt-b123-2-open-idle-close.gif)
  - 등장 4배 느리게: [evidence/prompt-b123-2/prompt-b123-2-open-slow4x.gif](evidence/prompt-b123-2/prompt-b123-2-open-slow4x.gif)
  - 컨셉과 비교: [evidence/prompt-b123-2/prompt-b123-2-concept-vs-game.png](evidence/prompt-b123-2/prompt-b123-2-concept-vs-game.png)
  - 상태별(기본, 호버, 골드 부족, 영어): [evidence/prompt-b123-2/prompt-b123-2-states.png](evidence/prompt-b123-2/prompt-b123-2-states.png)
- 컨셉 비교: 창 크기·위치, 영역 순서와 정렬, 중앙 육각형 크기가 컨셉과 같다. 글자 폭을 컨셉과 비교해 제목, 설명, 잔액, 안내 문구 크기를 맞췄다.
- 가드 확인(같은 Play Mode, `timeScale=0`, 실제 초기화는 실행하지 않음):
  - 등장 직후 본문 조작 불가 → 정착 후 가능
  - 닫는 중 실행 버튼 5회 호출 → 골드 3000, 유료 초기화 횟수 0 그대로
  - 닫는 중 다시 열기 → `openTime=0`부터 재생, 정착 후 알파·배율·마스크 정상
  - 열기·닫기를 빠르게 6회 반복 → 최종 비활성, 루트 알파 1, 닫는 중 상태 없음
- 처음 녹화에서 제목·비용 글자가 페이드 중 청록 사각형으로 보이는 문제를 발견했다. TMP 언더레이 확장값이 컸던 것이 원인이라 낮춰서 해결했고, 다시 녹화해 확인했다.
- EditMode(필터: MineReset·SurfaceBase 관련 63개): 62개 통과. 실패 1개 `SellPanelPrefabStructureTests.SurfaceBasePanel_ContainsSellChromeHierarchyAndViewBindings`는 HEAD 프리팹에도 `m_SizeDelta: {x: 760, y: 220}`이 없어 이번 변경과 무관한 기존 실패다.
- Console 오류 없음.

## 남은 문제 / 확인 필요

- 실제 유료 초기화 실행(골드 차감·광산 재생성) 경로는 코드를 바꾸지 않았다. 사용자 세이브를 바꾸지 않으려고 Play Mode에서 실행 버튼으로 실제 차감은 하지 않았다. 필요하면 백업한 슬롯에서 한 번 실행해 확인할 것.
- 추출 아트는 컨셉 해상도의 한계로 4K에서 약간 부드럽다. 정식 고해상도 아트가 나오면 같은 이름으로 교체하면 된다(좌표는 빌더 상수).
- 4K 외 해상도는 `FitScale` 단위 테스트로만 확인했다. Windows 빌드와 다른 PC 검증은 하지 않았다.
