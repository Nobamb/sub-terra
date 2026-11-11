# #267 퀘스트 X 내부 정렬 — 2026-11-03

## 범위와 구현
- 기준 main d2f54697. 전용 codex/issue-267-quest-close-alignment 브랜치.
- PromptB107QuestUiBuilder의 공용 배치(-112,-94), 68×68 유지. 콘텐츠 오른쪽 78/상단 60 여백.
- 전용 Editor 메뉴 Align Quest Details Close Button Only로 지상 SurfaceBasePanel.prefab과 Mine_Demo_Integration.unity의 QuestDetailsCloseButton RectTransform만 저장했다. 일반 빌더도 같은 함수를 호출한다.
- 기본/Hover 이미지는 기존 전체 스트레치 자식, 클릭 영역과 함께 이동. 진행·보상·닫기 이벤트·애니메이션 변경 없음. Inspector 추가 연결 불필요.
- Editor 저장에서 자동 추가된 다른 컴포넌트 기본값은 제외하고, 네이티브 RectTransform 위치 변경 두 줄만 보존했다. Scene은 별도 커밋한다.

## 검증과 증거
- PlayMode 수정 전 1건 실패로 지상·광산의 콘텐츠 정렬 불일치를 재현, 수정 후 1/1 통과.
- 실제 Bootstrap → 새 게임 → 지상 퀘스트 → 탐사 → 광산 퀘스트 경로, Temp/Issue267-20261103의 격리 저장 사용. 최초 브리핑은 배치 WaitForEndOfFrame 제한으로 기존 진행 중 상태로 준비했다.
- 각 화면에서 1366×768 / 1920×1080 / 2560×1440, 본문 오른쪽·MISSION LOG 상단 정렬, 제목 비겹침, 포인터 Raycast와 합성 마우스 X 클릭 후 닫힘 완료 확인. 1920 Hover도 캡처.
- 캡처는 해당 실행 중 Canvas를 임시 카메라로 렌더하고 원래 RenderMode로 복구한 결과. OS 게임 창 조작/Windows 빌드 검증은 아니다.
- 반해상도 전후 비교: images/issue267-quest-close/comparison.jpg. 실제 화면 확인에서 X·제목 비겹침, 화면 안쪽 배치를 확인했다.
- 원본·XML·로그: sub-terra/Builds/Issue267-20261103/ (로컬). 영구 비교 이미지는 PR에 포함.

## 미검증 체크리스트
- [ ] Windows exe에서 반복 클릭·실제 Hover 체감 및 닫힘 연출/소리 청취.
- [ ] 전체 Demo 완주·종료·이어하기 일괄 검증은 사용자 요청대로 보류.