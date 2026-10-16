# #193 지상 기지 퀘스트 버튼 — 2026-10-15

## 사용자 지시·환경

- 새 이슈는 새 PR을 만든다. 같은 작업의 후속 수정은 해당 PR 브랜치에서 커밋·푸시하고 중복 PR을 만들지 않는다.
- #193만 구현한다. 기준 main `d4f0e38f`, 브랜치 `codex/surface-base-quest-193`. #192의 별도 잔상/타일 변경은 포함하지 않는다.
- 실제 실행 프로젝트: `C:/Users/jeone/sub-terra/sub-terra` (Unity Hub 첫 번째 프로젝트), Unity 6000.5.4f1.

## 반영 내용

- 지상 보조 액션은 **퀘스트 → 자원 판매 → 업그레이드** 순서다. 크기 386×98, X=-426/0/426, Y=-145로 맞추고 동일 간격을 유지한다.
- 기존 지상 버튼 프레임·호버와 퀘스트 아이콘을 사용한다. 새 이미지나 UI 디자인을 추가하지 않는다.
- 기존 지하 퀘스트 상세창(썸네일·보상 표시·상태·이전/다음·닫기)을 지상 프리팹에 재사용한다.
- 상세창을 복제할 때 게임 진행 Binder·디버그 도구·다른 안내/보상 UI·외부 씬 참조를 제거한다. 같은 빌더를 반복 실행해도 퀘스트 버튼/창은 하나만 남는다.
- 지상 조회 Director에는 원본 GameState를 바인딩하거나 보상 서비스를 연결하지 않는다. 열기·탐색·닫기는 진행도/보상을 변경하지 않으며 실제 진행 변경 이벤트는 표시만 갱신한다.
- 닫기 버튼, Esc/X 최상단 창 닫기, 비활성화 시 구독·팝업 정리를 제공한다.
- 기존 117 빌더의 공통 배치 코드를 갱신했으므로 다시 실행해도 버튼 배치가 되돌아가지 않는다.

## 변경 파일

1. `Scripts/App/UI/SurfaceBase/SurfaceBaseQuestPanel.cs` + Unity 생성 .meta
2. `Scripts/App/UI/SurfaceBase/SurfaceBaseBinder.cs`
3. `Editor/DataValidation/PromptB117SurfaceBaseBuilder.cs`
4. `Editor/DataValidation/PromptB193SurfaceBaseQuestBuilder.cs` + Unity 생성 .meta
5. `Prefabs/UI/SurfaceBasePanel.prefab`
6. `Tests/EditMode/App/UI/PromptB117SurfaceBaseTests.cs`
7. 이 기록

경로 1~6은 `sub-terra/Assets/_Project/` 기준이다. 프리팹의 큰 추가분은 기존 상세창 계층을 복제한 결과다. 원본 지하 씬은 저장하지 않았다. 씬/ProjectSettings/Packages/원본 이미지/기존 GUID/저장 형식 변경 없음.

## 최소 검사 결과

- 경량 App / App.Editor / 변경 검사 소스 컴파일 통과. Unity 실제 스크립트 컴파일도 새 오류 없이 완료했다.
- Unity Editor 배치 메서드 `PromptB193SurfaceBaseQuestBuilder.BuildAndVerify`로 프리팹을 생성했다.
- 기존 검사 클래스의 신규 메서드 **2건을 Editor 안에서 직접 실행하여 PASS**:
  - `QuestActions_UseEqualSizeAndSpacing`: 버튼 크기·순서·간격·레이캐스트·피드백.
  - `SurfaceQuest_ReusesDetailsWithoutChangingProgressOrRewards`: 열기, 이전/다음, 닫기, 재열기, 최상단 닫기, 진행/보상/골드 불변.
- 첫 시도는 EditMode의 일반 MonoBehaviour 활성화 콜백이 실행되지 않아 탐색 이벤트 검사에서 실패했다. 검사에서 비활성화/활성화 콜백을 명시한 뒤 같은 두 검사가 통과했다. 제품 로직의 강제 초기화나 실제 진행 조작으로 우회하지 않았다.
- 최종 Unity 종료 코드 0. 초기 라이선스 조회 오류 로그, obsolete API 경고와 종료 시 AssetImportWorker 정리 메시지는 있었다. 이후 빌더·검사 실행과 정상 종료를 확인했으며 최종 실행에서 예외·컴파일 오류·Missing Script는 발견되지 않았다.
- 원본 씬/프리팹의 다른 서비스를 실행하거나 사용자 저장을 사용하지 않았다. 전체 Test Runner·플레이 완주·Windows 빌드·실제 화면 픽셀 검사는 하지 않았다.
- 코드 diff 검사 및 프리팹 내 버튼/창 중복 부재·진행 Binder/디버그 도구 참조 부재를 확인했다. Editor 저장의 기존 trailing whitespace는 무관한 정리 없이 보존한다.

## 팀원에게 전달할 짧은 확인

지상에서 퀘스트 버튼을 눌러 현재 목표·이전/다음을 보고, X 또는 Esc로 닫아 원래 화면으로 돌아오는지 확인한다. 세 버튼의 시각적 간격과 창 내부 가독성만 추가로 확인하면 된다. 이후 #193 수정은 이 브랜치의 PR에서 계속한다.
