# Prompt-B 134 결과 — 홀로그램 시설 이름표 + 코어 CCTV 연동

## 변경 파일
신규
- `Scripts/App/UI/FacilityNameTag/FacilityNameTagTimeline.cs` — 등장·퇴장 시간표(순수 C#). level(0~1) 하나로 진행을 표현해 도중에 방향이 바뀌어도 이어진다.
- `Scripts/App/UI/FacilityNameTag/FacilityNameTagVisual.cs` — 이름표 한 개의 모양·연출(uGUI 코드 생성). 월드 캔버스(`CreateWorld`)와 CCTV 오버레이(`CreateOverlay`)가 같은 클래스를 쓴다.
- `Scripts/App/UI/FacilityNameTag/FacilityNameTagLayers.cs` — 일반 화면 이름표 레이어와 CCTV 카메라 컬링 마스크.
- `Scripts/App/UI/Outpost/IFacilityNameTagAnchorLocator.cs` — 시설 ID → 이름표 위치(선택 구현 인터페이스, 기존 `IFacilityWorldLocator`는 그대로).
- `Scripts/App/Integration/FacilityNameTagAnchor.cs` — 시설 그림 윗면 중앙 바로 위 계산. 일반 화면과 CCTV가 같은 기준.
- 테스트: `Tests/EditMode/App/UI/FacilityNameTagTests.cs`(13개)

수정
- `Scripts/App/Integration/FacilityProximityLabelController.cs` — 말풍선(검은 사각형) → 홀로그램 이름표. 표시 조건·이름 데이터 유지.
- `Scripts/App/Integration/BuildingInstanceFacilityLocator.cs` — 이름표 위치 조회 추가.
- `Scripts/App/UI/Outpost/CoreCctvPopupView.cs` — CCTV 전용 이름표 오버레이와 선택·카메라 도착 연동.
- `Scripts/App/UI/Outpost/CoreCctvCameraRig.cs` — 미리보기 카메라가 일반 화면 이름표 레이어를 그리지 않도록 컬링 마스크 지정.
- `Tests/PlayMode/Integration/Outpost/CoreCctvPopupPlayModeTests.cs` — 이름표 PlayMode 10개 추가.
- `docs/CHANGELOG.md`

프리팹·씬·폰트·ProjectSettings·패키지는 변경하지 않았다(이름표는 런타임 코드 생성). `Mine_Demo_Integration.unity`, `OutpostPanel.prefab` 불변.

## 디자인
- 어두운 청록 반투명 배경(0.02, 0.10, 0.12, 0.78), 1.5px 청록 테두리선, 모서리 브래킷 8개, 약한 외곽 발광. 두꺼운 금속 프레임 없음. 엘리베이터 홀로그램의 색(`FillColor`/`TealLineColor`/`AccentColor`)·NotoSansKR 폰트와 같은 계열.
- 시설 이름만 표시. 이름은 `ItemDisplayNames.Building`(기존 데이터) 그대로.
- 높이 40px 고정, 폭은 글자 폭 + 좌우 22px 여백(최소 104, 최대 340). 줄바꿈·말줄임 없이 Overflow.
- 위치: `VisualRoot`의 첫 스프라이트 윗면 중앙 + 0.1 (그림이 없으면 위치 + 0.6). 피벗이 하단 중앙이라 시설 그림과 겹치지 않는다.

## 연출 (0.26초 등장 / 0.17초 퇴장)
1. 0~0.22: 작은 네모 모서리 빛.
2. 0.18~0.62: 프레임이 좌우로 펼쳐짐(글자는 아직 없음).
3. 0.55~1: 스캔선이 지나가며 글자가 왼쪽부터 드러남. 글자는 크기를 바꾸지 않고 RectMask2D로만 가린다.
- 표시 중: 글자 고정, 테두리 발광만 2.4초 주기로 ±7.5% 호흡. 글리치·점멸·부유 없음.
- 퇴장: 글자가 먼저 흐려지고 프레임이 중앙 네모로 접힌 뒤 사라짐.

## 일반 화면
- 표시 조건은 그대로(플레이어와 range 2 이내, 버팀목·사다리 제외).
- 시설당 이름표 하나를 재사용. 상태가 바뀔 때만 방향이 바뀌고(`SetWanted`) 같은 방향 요청은 진행을 되돌리지 않는다. 접근·이탈을 반복해도 오브젝트가 늘지 않고 접히는 도중 다시 접근하면 그 자리에서 이어서 펼친다.
- 겹침: 이름표 사각형이 겹치면 플레이어에 가장 가까운 시설 것만 남긴다(기존 “근접 시설” 기준).
- 입력 차단 없음: 모든 그래픽 `raycastTarget=false`, `GraphicRaycaster` 없음.
- 시설이 사라지면 접힌 뒤 제거, 컴포넌트 비활성화 시 모두 파괴.

## CCTV 연동 (오버레이 방식)
- CCTV 이름표는 팝업 `Screen`(RectMask2D) 안의 전용 uGUI 오버레이. 미리보기 카메라의 `WorldToViewportPoint`로 시설 위치를 따라가며 화면 영역에서 잘린다. 노이즈·스캔라인 위, 비네트 아래 순서.
- 일반 화면 이름표는 레이어 `Water`(4, 프로젝트가 쓰지 않는 기본 레이어 — TagManager 무수정)에 두고 CCTV 카메라는 이 레이어를 그리지 않는다. 그래서 (a) 접근으로 뜬 이름표가 CCTV에 섞이지 않고 (b) 플레이어 근처 시설도 CCTV에서 이중 표시되지 않으며 (c) CCTV에서 먼 시설을 골라도 메인 화면에 이름표가 생기지 않는다.
- 첫 연결: 검은 CCTV → 목록 → 첫 시설 자동 선택 → 영상이 켜지기 시작(0.06초 후)하면 이름표 펼침.
- 시설 전환: 선택 즉시 기존 이름표가 제자리에서 접힘 → 카메라 이동 중에는 새 이름표 없음 → 도착(모션블러 잔상 해제 같은 스텝)하면 새 이름표 펼침.
- 같은 항목 재선택은 무시(연출 반복 없음). 연속 선택은 마지막 시설에만 표시. 시설 제거·연결 해제는 팝업의 새 선택을 따라가고, 목록이 비면 검은 화면에 이름표 없음. 닫기·범위 이탈 시 종료 연출 동안 접히고 FinishExit에서 즉시 초기화.
- 시설을 못 찾으면 0.3초 간격으로만 재조회(매 프레임 `FindObjectsByType` 방지).

## 검증
- 전체 EditMode: 1194 통과 / 1 실패 — `IntegrationWiringTests.PlayableMineScene_HasRequestedCharacterTerrainAndMiningLayout`(Missing terrain at (-13, -40)). 변경을 제외한 트리에서도 동일하게 실패(기존 실패).
- 전체 PlayMode(daily, 153개): 151 통과 / 1 실패 — `GasExposurePlayModeTests.H_F02_F05_...`(가스 시야 알파). 변경을 제외한 트리에서도 동일하게 실패(기존 실패). 이름표 PlayMode 10개 전부 통과.
- 참고: `BuildingUiPlayModeTests.PromptB71_CPlacesBuildingWhileEnterDoesNot`는 실제 키보드 장치에 의존해 실행마다 간헐 실패한다(변경 제외 트리에서도 확인).
- PlayMode 확인 항목: 첫 선택·영상 켜짐 시점 / 전환(접힘·이동 중 없음·도착 후 펼침·잔상 해제) / 재선택 / 연속 선택과 이동 중 닫기 / 같은 종류 3개의 실제 인스턴스 추종 / 시설 제거·연결 해제·빈 목록·재연결 / 닫기·재열기·범위 이탈 후 잔여물 없음 / 메인·CCTV 분리(레이어, 먼 시설 선택, 월드 캔버스 아님) / 일반 접근·이탈·반복·제거 / 1280×720·1920×1080·2560×1440 글자 크기·화면 안 클리핑.
- 이름표 테스트가 프레임 수 대신 실제 시간으로 기다리도록 했다(연출이 unscaled 시간이라 프레임이 빠르면 프레임 수 기준은 불안정).

## 캡처 (`work_process/MVP2/facility-name-tag/`)
- `b134-main-approach.gif` — 일반 접근 시 이름표 등장
- `b134-cctv-switch.gif` — CCTV 시설 전환(기존 이름표 접힘 → 카메라 이동 → 도착 후 새 이름표)
- `b134-first-selection-1920x1080.png`, `b134-main-and-cctv-1920x1080.png`, `b134-main-final-1920x1080.png`, `b134-resolution-1280x720.png`, `b134-resolution-2560x1440.png`

## 남은 사항·제한
- 원본 프레임 전체는 `sub-terra/Temp/core-cctv-evidence`(git 제외)에 생성된다.
- 일반 화면에서 퇴장 연출은 플레이어가 멀어지면 카메라가 따라 이동해 화면 밖에서 끝나므로 캡처로는 확인하지 못했고, 시간표 EditMode 테스트와 PlayMode의 정리 검증으로 대신했다.
- `Water` 레이어에 다른 월드 오브젝트를 두면 CCTV에서 보이지 않는다. 현재 프로젝트의 씬·프리팹은 기본 레이어만 쓴다.
- 커밋은 하지 않았다.
