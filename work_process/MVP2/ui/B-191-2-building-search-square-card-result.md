# B-191-2 건설창 검색 확정 입력과 정사각 카드

2026-11-10, 작업 브랜치 `MVP2-fix136`.

재개 시점에 `init/prompt-B.md`, `work_process/MVP2/prompt-B-191-1.md`, `ProjectSettings/EditorBuildSettings.asset`가 기존 변경 상태로 기록되어 있었다. `EditorBuildSettings.asset`의 내용 diff는 0이다.

건설 검색창에 Input System이 보낸 확정 문자가 들어오도록 보완했다. 레일 옆 작은 카드는 새 청록 금속 프레임 이미지와 300×300 레이아웃을 적용했다. 너비 300은 기존 값을 사용했다. 후속 요청에 따라 큰 목록·상세창에도 같은 `building-card-square.png` 이미지를 9-slice로 적용했다.

## 원인과 변경

- `TMP_InputField`의 확정 입력 경로는 `Event.PopEvent`를 읽는다. `InputSystem.QueueTextEvent`로 `사다리`를 보냈을 때 기존 필드의 확정 문자열은 빈 값으로 남는 실패를 재현했다. 조합 문자열 표시나 `SetSearchQuery`만 검사하면 이 누락을 발견하지 못한다.
- `BuildingSearchInputField`가 선택 중인 키보드의 `onTextInput`을 받는다. TMP가 기존 경로에서 처리한 문자는 큐에서 제거하고 남은 확정 문자만 TMP의 Append 경로로 전달한다. 조합 문자열은 TMP가 처리한다. 선택 해제와 비활성화 때 구독과 큐를 정리한다.
- 기존 건설창 빌더가 검색 컴포넌트와 View 참조를 연결한다. 프리팹은 Editor 빌더로 저장했다.
- 작은 카드는 전용 이미지, 고정 높이 300, 내부 여백 16, 간격 6을 적용했다. 설명 영역이 남은 높이를 사용하고 비용과 상태가 카드 안에 들어간다.
- 카드와 큰 목록·상세창은 같은 프레임 생성 메서드를 사용한다. 큰 창은 기존 콘텐츠 크기에 맞춰 이미지가 늘어나며 테두리는 창 안에 맞춘다.

## 변경 파일

- `sub-terra/Assets/_Project/Scripts/App/UI/Building/BuildingSearchInputField.cs`와 `.meta`
- `sub-terra/Assets/_Project/Editor/DataValidation/PromptB188BuildingModeBuilder.cs`
- `sub-terra/Assets/_Project/Prefabs/UI/BuildingMenu.prefab`
- `sub-terra/Assets/_Project/Art/UI/Gameplay/building-card-square.png`와 `.meta`
- `sub-terra/Assets/_Project/Tests/EditMode/App/UI/PromptB191FollowupTests.cs`
- `sub-terra/Assets/_Project/Tests/EditMode/App/UI/PromptB191BuildingModeTests.cs`
- `sub-terra/Assets/_Project/Tests/PlayMode/Integration/Bootstrap/PromptB110BuildingMenuPlayModeTests.cs`
- `init/rules/lessons/2026-11-10-issue304-search-committed-input.md`
- `init/rules/lessons/2026-11-10-issue304-current-hover-visual-test.md`
- 이 결과 문서

## 검증

- 수정 전 키보드 확정 입력 회귀 검사 1개 실패. 기대값 `사다리`, 실제값 빈 문자열. `sub-terra/Builds/BuildingUi/baseline/PlayMode-results.xml`.
- 수정 후 PlayMode 3/3 통과. 확정 한글, 조합→확정→다음 조합, 중복 입력 방지, Backspace, 포커스 해제, 기존 시설 선택·비용·설치 상태·레이아웃, 레일 재클릭과 호버를 확인했다. `sub-terra/Builds/BuildingUi/play/PlayMode-summary.txt`.
- 관련 EditMode 57/57 통과. `PromptB110BuildingMenuTests`, `PromptB188BuildingModeTests`, `PromptB191BuildingModeTests`, `PromptB191FollowupTests`. `sub-terra/Builds/BuildingUi/edit/EditMode-summary.txt`.
- 공통 프레임 적용 후에도 EditMode 57/57, 기능 PlayMode 3/3 통과. `sub-terra/Builds/BuildingUi/shared-frame-edit-final/EditMode-summary.txt`, `sub-terra/Builds/BuildingUi/shared-frame/PlayMode-summary.txt`.
- 추가 Visual 검사를 실행하며 이전 시설의 호버 이름표를 검사하던 참조를 현재 `view.RailHologram`으로 수정했다.
- 최종 Visual PlayMode 1/1 통과. 1920×1080, 1366×768, 2560×1440에서 카드·목록·상세·검색·정렬·클릭 영역을 검사했다. `sub-terra/Builds/BuildingUi/shared-frame-visual-final/PlayMode-summary.txt`. 카드·목록·상세 캡처를 절반 해상도로 묶어 검토한 이미지는 `sub-terra/Builds/BuildingUi/shared-frame-preview.jpg`다.
- 사용자의 물리 Esc 입력으로 컴퓨터 조작이 중단되어 이후 직접 UI 조작은 멈췄다. 이미 설정된 Unity 검사와 저장된 화면 캡처로 공통 프레임을 확인했다.
- Bootstrap에서 시작한 격리된 슬롯 0 런타임으로 탐사 화면을 열었다. 화면에서 `사다리` 표시와 사다리만 남는 검색 결과를 확인했다. `sub-terra/Builds/BuildingUi/screen.png`.
- 실제 탐사 화면 카드 캡처를 절반 해상도로 검토했다. 정사각 프레임 안에 이름·설명·비용·상태가 표시된다. `sub-terra/Builds/BuildingUi/card-preview.png`.
- 새 C# 컴파일 오류는 없다. 실행 중 기존 CCTV의 URP `stereoTargetEye` 경고가 발생한다. 수정 전 실패 테스트와 임시 검증 명령 파일의 읽기 충돌 오류가 로그에 포함되어 있으며 임시 검증 코드는 정리했다.
- Unity가 생성한 프리팹의 새 컴포넌트 `m_Name` 빈 값에 공백이 있어 `git diff --check`에 표시된다. 코드 파일에는 해당 공백 오류가 없다.

## 미검증

- 물리 키보드의 Windows 한/영 전환과 실제 IME 조합 타이핑. 자동화 도구의 한/영 키 전환이 지원되지 않아 OS 입력은 한글 문자열 붙여넣기로 확인했고, 조합과 확정 문자 전환은 실제 Scene에서 입력 이벤트와 IME 테스트 입력으로 확인했다.
- 수동 확인은 Bootstrap → 탐사 → B → `>>` → 검색창에서 `사다리`, `충전기`, `ㅅㄷ`를 직접 입력하고 Backspace·Enter·재선택을 확인한다.
- Windows 플레이어 빌드 실행.

## 이미지 생성

내장 `image_gen` 도구를 사용했다. 프로젝트 소비 경로는 `sub-terra/Assets/_Project/Art/UI/Gameplay/building-card-square.png`다. 적용 시 Sprite Single과 9-slice를 설정했다.

생성 프롬프트는 다음과 같다.

```text
Use case: stylized-concept. Asset type: square game UI card background texture for an underground industrial sci-fi game. Create a single exactly square straight-on flat 2D panel, 1024x1024. Deep nearly-black blue-green metal central background, subtle fine scratches mostly near edges, slim cyan luminous double-line frame and small metallic corner joints. Match a restrained industrial teal holographic terminal HUD aesthetic. Frame outer silhouette is a true simple square with tiny clipped corners only, identical overall width and height. No protruding top tab, no side notches, no window header, no buttons, no symbols, no text, no labels, no watermark. At least central 85 percent is calm dark empty space for readable live UI content. Border within 4 percent of image edges, very subtle tight cyan glow. Transparent only outside the outer frame, opaque dark center. Production-ready game sprite, crisp edges.
```
