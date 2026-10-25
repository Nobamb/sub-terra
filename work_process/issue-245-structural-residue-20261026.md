# #245 구조 시각 잔재 수정 — 2026-10-26

- 기준 main 8305ee2c. 씬 WorldSystems 직속 StructuralShake_-13_-39(스프라이트만 존재, Collider 없음)와 균열 타일 (-13,-39)을 확인했다. 사용자가 본 원본 화면 좌표를 직접 확인한 것은 아니지만 같은 증상을 만드는 씬 잔재는 확인했다.
- 수정 전 최소 EditMode 3건 모두 FAIL: 목록 밖 잔재 회수 실패, 새 복제본 DontSave 누락, 저장된 씬 잔재 존재.
- 전용 StructuralResidueSceneCleanup Editor 도구로 복제본 1개·균열 타일을 정리했다. Editor 생성 diff 중 대상 오브젝트/부모 참조/균열 Tilemap 변경만 보존했다. 씬 수정은 별도 커밋으로 분리했다. 원본 지형·채굴·보상·구조 위험 규칙과 세이브는 변경하지 않았다.
- StructuralCrackOverlay의 Awake/ClearAll에서 목록 밖 직속 잔재 및 균열 타일을 정리한다. 추적 중인 낙하/먼지와 무관한 자식은 보존한다. 흔들림·낙석·먼지 GameObject는 DontSave로 생성해 씬에 재저장되지 않도록 한다.
- 최종 EditMode 29/29 PASS: StructuralFeedbackTests + StructuralIntegritySystemTests. PlayMode 6/6 PASS: 구조 붕괴/시작 잔재 제거 + 실제 Bootstrap→새 게임→탐사→저장→이어하기 자동 스모크.
- QA 저장 경로는 프로젝트 Temp/Issue245-20261026에 격리. 실제 사용자 슬롯/기존 폴더 보존. 자동 설정 변경 복원.
- 로그/XML/Editor 생성 patch: Builds/Issue245-20261026 (Git 제외).
- Windows 실제 화면, 사용자 세이브1/2의 해당 좌표 육안 확인은 미실행. 사용자 요청대로 Windows 일괄 검증 보류. structuralSystem의 비어 있는 별도 참조는 이번 이슈와 구분해 변경하지 않았다.
