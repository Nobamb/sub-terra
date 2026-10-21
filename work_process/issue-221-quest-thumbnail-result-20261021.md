# #221 퀘스트 상세 썸네일 — 2026-10-21

## 원인과 수정

- 이슈: https://github.com/Nobamb/sub-terra/issues/221
- 퀘스트 7~13, 16~18에 배경이 있는 전용 장면 그림이 이미 존재하지만, PromptB1072QuestThumbnailBuilder.Configure가 시설 퀘스트에는 BuildingData.Icon을 우선 연결하고 있었다. 투명 배경의 시설 아이콘이 검은 UI 배경 위에 단독으로 표시된 원인이다.
- 18개 퀘스트 모두 Art/UI/Gameplay/Quest/Thumbnails/<목표 ID>.png를 연결한다. 대상 10개만 실제 씬과 지상 퀘스트 프리팹 참조가 달라지며, 나머지 8개는 같은 그림을 유지한다.
- 긴급 탈출의 기존 장면은 엘리베이터를 보여 줬다. 현재 emergency_escape_portal_v1.png를 참조해 원형 청록 포탈로 이 한 장만 교체했다. 기존 광부·드론·전진기지·동굴 구도를 유지한 장면 그림이다. 나머지 9장은 기존 컨셉 그림을 활용한다.
- 원본 PNG 경로와 .meta GUID를 유지한다. 새 그림은 2171×724이며 기존 3:1 틀에서 preserveAspect로 표시한다. 프레임/여백/버튼/보상/퀘스트 판정은 바꾸지 않는다.
- ApplyReferences Editor 메뉴를 추가해 이미지 임포트 설정이나 다른 UI를 다시 만들지 않고 지하 씬·지상 퀘스트 프리팹 참조만 갱신할 수 있다. 기존 Apply와 다른 시설 빌더에서도 같은 장면 그림 연결 규칙을 사용한다.

## 환경·수정 파일

- 저장소 C:/Users/jeone/sub-terra, Unity 프로젝트 C:/Users/jeone/sub-terra/sub-terra, Unity 6000.5.4f1, 검토용 브랜치 codex/nonvisual-review-20261021.
- Assets/_Project/Editor/DataValidation/PromptB1072QuestThumbnailBuilder.cs
- Assets/_Project/Scenes/App/Mine_Demo_Integration.unity — Editor 빌더로 생성한 썸네일 참조 10개만 변경. 저장 과정의 무관한 자동 직렬화 차이는 되돌렸다.
- Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab — 복사된 지상 퀘스트 창에도 같은 증상이 있어 썸네일 참조만 갱신한다. 지상 본문/버튼/레이아웃을 다시 만들지 않는다.
- Assets/_Project/Art/UI/Gameplay/Quest/Thumbnails/emergency_escape_return.png — 이미지 생성 도구로 기존 그림을 수정. .meta 유지.
- Assets/_Project/Tests/EditMode/App/Tutorial/PromptB107QuestUiTests.cs — 기존 시설 아이콘 일치 검사를 전용 장면 그림 검사로 변경.
- 이 기록. 앞선 #225의 로컬 수정 3개 파일은 별도로 보존한다.

## 최소 검사·게시

- 지하 빌더 ApplyReferences와 지상 ApplySurfaceReferences 모두 종료 코드 0. 씬과 지상 프리팹의 최종 변경은 각각 썸네일 참조 10개뿐이다. 대상 밖 씬·프리팹·폰트·ProjectSettings·패키지·이미지 .meta 변경 없음.
- 대상 10개 그림을 파일로 확인했으며 동굴 배경이 있는 장면임을 확인했다. 실제 팝업 화면 렌더링 확인은 별도다.
- EditMode 1차: 대상 10개 표시와 실제 지하 씬의 18개 연결, 3:1 비율·preserveAspect·raycastTarget=false 검사 11건 통과/실패 0, 5.39초. work_process/tools/issue221-quest-thumbnails-20261021/.
- EditMode 2차: 지상 프리팹의 동일 18개 연결 검사 1건 통과/실패 0, 1.88초. work_process/tools/issue221-surface-thumbnails-20261021/. 통과한 11건은 반복하지 않았다. 총 12건 통과, 컴파일 오류 없음, diff 형식 검사 통과. 라이선스 서비스 404 환경 로그가 있었으나 검사와 종료는 정상 완료했다.
- 사용자 저장 슬롯은 사용하지 않는다. 전체 Demo/Windows 빌드는 실행하지 않는다.
- 아직 커밋·푸시·새 PR 없음. 게시 요청 시 #221 변경만 독립 PR로 분리하고 #225 수정은 기존 PR에 추가한다. 검토용 통합 브랜치 전체를 한 PR에 넣지 않는다.
- 팀원 확인: 첫 Hub 프로젝트 → Bootstrap → 지상/탐사 → 퀘스트 상세 7~13·16~18에서 시설 아이콘 대신 배경 장면이 표시되는지, 특히 긴급 탈출이 엘리베이터가 아닌 원형 포탈인지 확인한다.

## 이후

- #223 정적 심층 썸네일·잠금 상태 표시가 다음 후보다. 해금 연출과 실제 Lv.2/3 탐사 확장은 이번 변경에 포함하지 않는다.
- #211 지상 업그레이드는 재현 조건이 확보되지 않았으므로 추측 수정하지 않는다.
