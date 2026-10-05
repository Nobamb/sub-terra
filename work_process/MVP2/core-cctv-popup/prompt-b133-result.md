# Prompt-B 133 결과 — 전진기지 코어 CCTV 팝업

## 변경 파일
- `Scripts/App/UI/Outpost/CoreCctvPopupView.cs` — 팝업 상태(등장·Live·종료), 목록, CCTV 영상 전원, 입력
- `Scripts/App/UI/Outpost/CoreCctvFacilityList.cs` — 연결 시설 목록·선택 모델(순수 C#)
- `Scripts/App/UI/Outpost/CoreCctvTimeline.cs` — 등장·전환·종료 시간표, 목록 배치·스크롤 계산(순수 C#)
- `Scripts/App/UI/Outpost/CoreCctvCameraRig.cs` — CCTV 전용 카메라 1개 + RenderTexture 1개 재사용
- `Scripts/App/UI/Outpost/CoreCctvFacilityItem.cs`, `CoreCctvArt.cs`, `IFacilityWorldLocator.cs`
- `Scripts/App/Integration/BuildingInstanceFacilityLocator.cs` — 시설 ID → 월드 위치(Gameplay를 아는 Integration 쪽에만 둠)
- `Scripts/App/UI/Outpost/OutpostPanelView.cs`, `OutpostPanelBinder.cs` — 코어 모드를 새 팝업으로 연결(닫기·ESC 경로 공유)
- `Scripts/App/Integration/IntegrationRuntimeBinder.cs` — 위치 조회·아이콘(카탈로그 `BuildingData.Icon`) 연결
- `Editor/DataValidation/CoreCctvPopupBuilder.cs` — `OutpostPanel.prefab`에만 `CorePopup` 추가(`SubTerra/UI/Build Core CCTV Popup (OutpostPanel only)`)
- `Editor/DataValidation/PromptB133CoreCctvTestRunner.cs` — 전용 테스트 러너
- `Prefabs/UI/OutpostPanel.prefab`
- 테스트: `Tests/EditMode/App/Outpost/CoreCctvPopupTests.cs`(+`FacilityServicePopupTests.cs` 코어 모드 기대값 갱신), `Tests/PlayMode/Integration/Outpost/CoreCctvPopupPlayModeTests.cs`
- `docs/CHANGELOG.md`

Mine_Demo_Integration.unity, 다른 UI 프리팹·씬, 폰트, ProjectSettings, Packages, 메인 카메라 설정은 변경하지 않았다.

## 유지한 기존 기능
- 연결 시설 판정(`GameplayEventBridge.BuildFacilityStatuses`), 전력 계산, 시설 사용 효과, 저장 데이터, 상호작용 조건은 코드 변경 없음.
- 연결된 시설 = 기존 코어 정보창과 같은 기준(스냅샷의 활성 시설).
- 범위 이탈은 기존대로 플레이어-코어 실제 거리로만 판정. CCTV 카메라 위치는 입력이 아니다.
- E로만 열림. X·ESC로 닫으면 같은 범위 안에서 다시 열리지 않음(기존 규칙).
- 기존 `PanelRoot/CoreRoot` 계층은 프리팹에 남겨 두었고(구 프리팹 폴백), 코어 모드에서는 쓰이지 않는다. 충전기·보건소 팝업, 정산 콘솔, 보관함은 그대로.

## 구현 요점
- 프레임만 가로선 → 세로로 펼치고, 글자·아이콘은 크기가 고정된 Inner를 높이만 변하는 RectMask2D로 잘라 보여 줘 찌그러지지 않는다.
- 등장(시설 있음): 선 0.10s → 프레임 0.34s → 터미널 4줄 → ‘연결되었습니다’ 깜빡임 → 목록 순차 등장 + 영상 켜짐, 총 약 1.34s. 시설 없음은 약 1.06s에 끝나고 검은 화면·REC 없음·목록 영역에만 ‘연결된 시설 없음’.
- 터미널 문구는 실제 값(시설 N개 확인 / 연결된 시설 없음)을 쓰고, 시설이 없으면 ‘영상 연결’ 줄을 찍지 않는다.
- CCTV: 메인 카메라와 같은 화면 배율(월드 1칸당 픽셀)을 정수로 맞추고, RenderTexture를 화면에 보이는 픽셀 크기에 맞춘 Point 필터로 만들어 늘어나거나 번지지 않는다. 카메라 위치는 텍셀 격자에 맞춘다. 해상도 변경 시에만 텍스처를 다시 만든다.
- 이동 0.28s(smoothstep), 이동 중에만 CCTV 안의 RawImage 잔상 2장. 마지막 선택만 따라가고 같은 항목 재선택은 무시.
- 닫힌 동안 카메라 `enabled=false`. 카메라/텍스처는 팝업 파괴 시 정리. 모든 시간은 `unscaledDeltaTime`(timeScale 0에서도 진행).
- 목록 변경은 `SetFacilities`가 달라진 경우에만 반영(등장 연출 재시작 없음). 선택 유지 → 사라지면 같은 자리의 다음 시설 → 모두 사라지면 검은 화면.
- 노이즈·스캔라인·육각 무늬·테두리는 런타임 절차 텍스처. 프로젝트가 Linear 색공간이라 어두운 배경 위 노이즈는 알파 0.01로 낮췄다.

## 검증
- EditMode(전용 101개: 모델·시간표·프리팹 구조·상태 전환·Presenter 흐름 포함) 통과, 전체 EditMode 357개 통과.
- PlayMode `CoreCctvPopupPlayModeTests` 15개 통과(실제 Integration Scene, 실제 시설 프리팹, 실제 GameplayEventBridge 판정): 0/1/다수, 스크롤·키보드·마우스 휠, 같은 종류 시설별 위치, 연속 선택, 선택 시설 제거·추가·연결 해제·재연결, 등장 도중 닫기·범위 이탈·반복 접근·ESC, 메인 카메라 불변, 종료 후 렌더링 중단, timeScale 0, 파괴 시 정리, 7개 해상도 배치.
- 전체 PlayMode 실행: 75개 중 1개 실패 — `GasExposurePlayModeTests.H_F02_F05_...`(가스 시야 오버레이 알파). 코어 팝업·Outpost 코드를 쓰지 않는 독립 테스트이며, 변경 전 트리에서의 동작은 대조하지 못했다.
- 증거: `evidence/` (최종 화면, 등장·시설 전환·종료 GIF, 빈 목록, 스크롤, 해상도 시트).

## 남은 제한
- **첨부 컨셉 이미지는 작업 입력에 포함되지 않아 직접 보지 못했다.** 요구 문장(남색 패널·육각 배경·각진 금속 프레임·청록 발광, 좌 30~35% 목록 / 우 CCTV)과 B-132 팝업의 프레임·패널·폰트를 기준으로 구성했다. 컨셉 이미지와 비교해 색·간격 조정이 필요할 수 있다.
- ‘연결된 시설’은 기존 판정 그대로 엘리베이터 또는 어느 코어의 전력 범위든 활성이면 포함한다(특정 코어 한정이 아님).
- 모션블러는 포스트 프로세싱 없이 잔상 RawImage로 구현했다(승인 없이 프로젝트 설정을 바꾸지 않기 위함).
- CCTV는 월드를 그대로 다시 그리므로 월드 공간 UI(시설 이름 말풍선, 전력 범위 원)가 보일 수 있다.
- 폰트 SDF 에셋(`NotoSansKR-Regular_SDF.asset`)과 `EditorBuildSettings.asset`은 작업 시작 전부터 수정돼 있었고 이번 작업과 무관하다.
