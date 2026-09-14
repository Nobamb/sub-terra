# prompt-B 93 작업 결과

## 변경 내용

- 메인 메뉴·지상·지하 설정에 `키 조작 변경` 버튼을 추가했다.
- 동일한 키보드 배치를 유지하면서 `<` / `>`로 세 조작 방식과 키별 역할·색상·설명을 순환한다.
  1. WASD / 방향키 이동, 마우스 클릭 / Enter 채굴 (기존 기본값).
  2. WASD 이동, 방향키 상하좌우 인접 블록 채굴.
  3. 방향키 이동, WASD 상하좌우 인접 블록 채굴.
- 2·3번에서도 마우스 클릭 / Enter 채굴과 Space 점프를 유지한다. 위·아래 이동은 기존 사다리 이동이다.
- 선택 후 돌아가서 설정의 `적용`을 누르면 확정·저장된다. 취소는 기존 값 유지, 기본값 복원은 1번이다.
- 조작 안내 창은 돌아가기 버튼 또는 Esc로 닫힌다. 지하 Esc는 조작 안내 → 설정 순서로 닫는다.
- 설정이 열린 동안 이동·점프·채굴을 차단한다. 방향 채굴도 기존 거리, 보호 셀, 전력, 광물 보상 검증을 사용한다. 대각선 동시 채굴 입력은 수직 우선이다.

## 파일

`sub-terra/Assets/_Project/` 기준 (새 C# 파일은 `.meta` 포함):

- `Scripts/Shared/ControlPreferences.cs`: 공통 선택값과 설정 입력 차단 상태.
- `Scripts/Gameplay/Player/PlayerKeyboardControls.cs`: 키보드 이동·채굴 방향 분리.
- `Scripts/Gameplay/Player/PlayerController.cs`: 선택된 이동 키 및 설정 입력 차단.
- `Scripts/Gameplay/Mining/PlayerMiningController.cs`: 방향 입력 연결, UI 중 채굴 취소.
- `Scripts/Gameplay/Mining/MiningSystem.cs`: 캐릭터 셀 기준 방향 채굴 진입점.
- `Scripts/App/UI/MainMenu/SettingsSession.cs`: 조작 선택 초안·적용·취소.
- `Scripts/App/UI/MainMenu/SettingsRuntimeApplier.cs`: PlayerPrefs 저장·로드·적용.
- `Scripts/App/UI/MainMenu/ControlSchemePanel.cs`: 공통 키보드 그림과 선택 UI.
- `Scripts/App/UI/MainMenu/MainMenuView.cs`: 메인 메뉴 설정 연결.
- `Scripts/App/UI/SurfaceBase/SurfaceBaseView.cs`: 지상·지하 설정 연결.
- `Scripts/App/Integration/UndergroundMenuController.cs`: Esc 하위 창 우선 닫기.
- `Tests/EditMode/App/UI/PromptB93ControlSchemeTests.cs`: 키 배치, 이동 차단, 방향 채굴, 설정과 UI 검증.
- `Editor/DataValidation/PromptB93ControlSchemeTestRunner.cs`: 전용 테스트 실행 메뉴·플래그.

## 검증 및 연결

- Unity 6000.5.4f1 EditMode 25개 통과: 신규 21개, 기존 지하 설정 회귀 4개.
- 결과: `sub-terra/Temp/prompt-b93-editmode-results.txt`.
- 렌더링: `sub-terra/Temp/prompt-b93-controls-1.png` ~ `prompt-b93-controls-3.png`. 키 설명의 불필요한 줄바꿈을 시각 검토 후 수정했다.
- Unity 컴파일 오류·테스트 실패 없음. 기존 obsolete API 컴파일 경고 및 에디터 라이선스 서비스 로그는 별개로 남아 있다.
- 기존 설정 루트에 런타임으로 생성·연결하므로 Inspector 추가 배선은 없다. Scene, Prefab, 폰트, InputAction 에셋은 변경하지 않았다.
- 조작 설정은 `subterra.settings.controls`에 저장한다. 누락·유효하지 않은 값은 1번으로 복원한다. 게임 세이브 형식은 변경하지 않는다.
- Unity MCP 도구가 제공되지 않아 프로젝트 기존 Editor 플래그 경로로 검증했다. Bootstrap부터 실제 플레이 완주 및 Windows 빌드 검증은 수행하지 않았다.
- 사용자가 수정한 `init/prompt-B.md`는 그대로 유지했다. 커밋·배포는 수행하지 않았다.
