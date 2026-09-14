# prompt-B 92 작업 결과

## 변경

- 긴급 탈출 포탈에서 엘리베이터 또는 전진기지 코어를 선택하면 해당 위치가 화면 중앙에 오도록 시점을 옮긴다.
- 플레이어는 포탈에 그대로 두고, 골드/전력은 청구하지 않는다.
- 패널을 닫거나 취소를 하면 카메라는 플레이어 추적으로 즉시 복귀한다.

## 파일

프로젝트 `sub-terra/Assets/_Project/` 기준:

- `Scripts/Gameplay/Player/PlayerCameraFollow.cs`: 월드 좌표 미리보기/복원 API.
- `Scripts/App/UI/EmergencyEscape/IEmergencyEscapeDestinationPreview.cs`: 선택 미리보기 계약.
- `Scripts/App/UI/EmergencyEscape/EmergencyEscapePanelView.cs`: 드롭다운 선택 이벤트.
- `Scripts/App/UI/EmergencyEscape/EmergencyEscapePanelBinder.cs`: 열기/선택/닫기 시 미리보기 연결.
- `Scripts/App/Integration/EmergencyEscapePortalRuntimeBridge.cs`: 목적지 좌표 해석과 카메라 미리보기.
- `Tests/EditMode/App/UI/PromptB92EmergencyEscapePreviewTests.cs`: 4개 EditMode 테스트.
- `Editor/DataValidation/PromptB92EmergencyEscapePreviewTestRunner.cs`: 테스트 실행 메뉴.

## 검증

- Unity EditMode: 11/11 통과 (Prompt-B 92 4개 + 기존 포탈 7개). 결과 `sub-terra/Temp/prompt-b92-editmode-results.txt`.
- 엘리베이터/코어 미리보기는 화면 중앙 정렬, 플레이어 위치와 골드 불변, 닫기 시 플레이어 추적 복원을 확인했다.
- Integration Scene·UI Prefab은 저장하지 않았다. 런타임에서 `Camera.main`의 `PlayerCameraFollow`를 사용한다.
- 세이브 형식 변경 없음. 커밋/배포 미실행.
