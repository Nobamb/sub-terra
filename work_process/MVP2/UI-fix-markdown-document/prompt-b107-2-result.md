# Prompt-B 107-2 — 상세 퀘스트 장면 썸네일

## 적용 내용

- 18개 퀘스트에 각각 1448×472 PNG 장면 썸네일과 Unity Sprite 참조를 연결했다.
- 위치: `sub-terra/Assets/_Project/Art/UI/Gameplay/Quest/Thumbnails/`.
- 개별 에셋을 나열하던 `QuestThumbnailView`를 하나의 장면 이미지가 영역 전체에 맞춰 표시되는 방식으로 변경했다. 비율을 보존하고 프레임 안쪽 여백을 둔다.
- 구리·철·리튬은 실제 광물 타일을 향해 채굴하는 캐릭터 모션을 사용한다. 건설·보관·충전·회복은 해당 시설과 캐릭터를 함께 배치했다. 가스·탈출 포탈은 기존 Gameplay 프리팹의 비주얼을 사용했다.
- `PromptB1072QuestThumbnailBuilder.Apply`는 Integration 씬의 썸네일 매핑만 갱신한다. 기존 107 UI 빌더도 동일 매핑을 사용하므로 재생성 시 이전 에셋 나열 방식으로 돌아가지 않는다.
- Inspector 수동 연결은 필요 없다. Scene 변경은 `QuestThumbnailView.entries`의 18개 참조에 한정했다.

## 이미지 제작 방식과 범위

모든 퀘스트를 실제로 완료하면서 찍은 플레이 기록은 아니다. Bootstrap으로 초기화한 Play Mode에서 촬영용 지형·캐릭터 모션·시설을 배치하고 Unity 카메라로 렌더한 **연출 스냅샷**이다. AI 이미지 생성은 사용하지 않았다. 기존 게임 아트와 렌더링을 유지하는 데 목적을 두었다.

촬영 시 저장 슬롯은 0이었다. 실제 세이브를 생성하거나 덮어쓰지 않았으며, Tilemap 변경은 촬영 직후 원복하고 Play Mode를 종료했다. 촬영 코드가 Gameplay 소스·프리팹을 수정하지는 않는다.

- [18개 장면 미리보기](evidence/prompt-b107-2/contact-sheet.png): 왼쪽부터 오른쪽, 위에서 아래로 기존 퀘스트 순서.
- [촬영 코드](evidence/prompt-b107-2/capture-thumbnails.cs): Unity MCP RunCommand용, Assets 밖에 보관. 슬롯 0 / Bootstrap 초기화 / Integration 로드 상태가 전제다.

## 검증

- Unity EditMode `PromptB107QuestUiTests`: 첫 실행 **2 passed / 0 failed**.
- 기존 0.3초 hover, 상세창·요약창 배치, 보상 2/3개 배치를 함께 검증했다.
- 추가 검증: 18개 ID별 이미지 경로·1448×472 크기·전체 영역 앵커·비율 보존·raycast 비활성, 알 수 없는 ID의 placeholder 처리.
- 18개 장면을 contact sheet로 확인했고, 첫 테스트가 출력한 긴급 탈출 상세창에서 썸네일이 프레임 내부에 표시되는 것을 확인했다.
- 첫 테스트 직후 Unity Console Error **0개**를 확인했다.
- 이전 107 증빙 이미지가 테스트로 갱신되어 해당 3개 파일만 원복했다. 이후 테스트 증빙 출력 경로는 `evidence/prompt-b107-2`로 분리했다.
- `git diff --check` 통과. 기존 사용자 변경인 `init/prompt-B.md`는 유지했다.

## 검증 제한

증빙 출력 경로 변경 후 재실행과 추가 Play Mode 확인 요청에서 Unity MCP가 응답하지 않았다. Editor 프로세스는 존재했지만 화면 확인용 Computer Use 연결도 사용할 수 없어, 두 번째 실행 완료와 마지막 Editor 재생 상태는 확인하지 못했다. 이 재실행을 통과한 것으로 계산하지 않는다. Windows 빌드 검증은 수행하지 않았다.

핵심 코드·18개 매핑은 첫 테스트에서 통과한 상태이며, 마지막 테스트 파일 차이는 증빙 저장 폴더뿐이다. 재확인은 Unity 메뉴 `SubTerra/UI/Run Prompt-B 107 Tests`에서 실행할 수 있다.
