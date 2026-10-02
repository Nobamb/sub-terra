# 생존자 브리핑 문구·간격·닫기 연출 수정

## 변경 내용

- 본문을 사용자 지정 문구로 교체했다. 첫 문장은 `지상에는 더 이상 자원이 남지 않았습니다.`이며, 나머지 문장과 빈 줄을 그대로 유지했다.
- 공용 Noto Sans KR 동적 아틀라스에서 `았`, `땅`은 문자 정보가 있지만 해당 글자 영역의 픽셀이 비어 있었다. 같은 원본 폰트로 브리핑의 제목·본문·버튼에 필요한 글자를 모두 구운 전용 정적 폰트를 적용했다.
- 프레임 높이를 850에서 950으로 늘려 이미지에 포함된 상단·하단 구분선과 본문 사이의 여백을 확보했다. 본문 글자 크기와 줄 간격은 유지했다.
- 제목을 가로 중앙으로 옮기고, 프레임 안쪽 상단 경계와 본문 상단 구분선 사이의 중앙(y=240)에 배치했다.
- 기존 계층과 버튼 배선을 유지하며 Editor 빌더로 대상 브리핑만 수정했다. 씬 저장 중 발생한 무관한 Sprite 직렬화 변경은 원복했다.
- 닫기 0.48초: 본문·버튼 페이드아웃 → 프레임이 위아래로 접힘 → 중앙 가로 신호가 수축 → 초기 화면 글리치 3회를 재사용하며 종료. 접히는 후반부와 화면 글리치는 일부 겹친다. 정지된 배경 캡처는 닫기까지 보관하고 종료·비활성화 시 해제한다.

## 수정·생성 파일

Unity 프로젝트 루트 `sub-terra/` 기준:

- `Assets/_Project/Editor/DataValidation/PromptB120StartBriefingBuilder.cs`
- `Assets/_Project/Scenes/App/Mine_Demo_Integration.unity`
- `Assets/_Project/Scripts/App/Tutorial/DemoObjectiveCatalog.cs`
- `Assets/_Project/Scripts/App/UI/Tutorial/StartBriefingPopupMotion.cs`
- `Assets/_Project/Scripts/App/UI/Tutorial/StartBriefingTimeline.cs`
- `Assets/_Project/Fonts/StartBriefingNotoSansKR_SDF.asset` 및 `.meta`
- `Assets/_Project/Tests/EditMode/App/Tutorial/PromptB120StartBriefingTests.cs`
- `Assets/_Project/Tests/EditMode/App/Tutorial/DemoObjectiveTransitionTests.cs`

보고 문서와 [미리보기](start-briefing-text-layout-preview.png)도 추가했다.

## 검증

- Unity 재컴파일 완료. Console Error 조회 결과 0개.
- EditMode `PromptB120StartBriefingTests`, `DemoObjectiveTransitionTests`: **34 통과, 0 실패**.
- 지정 본문 일치, 브리핑 전용 아틀라스의 모든 출력 글자에 실제 픽셀이 존재하는지 검사했다. 문자 등록 여부만 검사하는 경우 놓치는 빈 글리프를 검출한다.
- 닫기 시 프레임 크기가 단조롭게 줄어드는지, 신호 수축과 화면 글리치 3회의 시간표, 본문이 화면 글리치 전에 사라지는지, 중복 닫기 상태 전이를 검사했다.
- 브리핑 계층을 임시 씬에 복사해 Unity Canvas로 1920×1080 미리보기를 렌더링했다. 본문 6줄·누락 글자 복구·선 여백·제목 정렬을 확인했다. 임시 씬은 닫고 Bootstrap으로 복귀했다.
- `git status`, 관련 diff와 `git diff --check`를 확인했다. 공용 폰트에 검증 중 생긴 자동 변경은 원복했다.

## 참조와 검증 제한

- Inspector 추가 연결은 필요 없다. 기존 Motion·View·버튼 참조를 유지하며 빌더가 전용 폰트를 연결한다.
- 실제 Bootstrap → 새 게임 → 탐사 흐름 및 Play Mode에서의 닫기 영상은 이번 작업에서 확인하지 않았다. 닫기 연출은 코드와 EditMode 시간표·상태 전이 검사로 검증했다.
- 임시 렌더링 중 기존 `PowerConnectionText`의 `✕` 공용 폰트 누락 경고가 나타났다. 생존자 브리핑 밖의 요소이므로 이번 범위에서 수정하지 않았다.
- Windows 빌드는 실행하지 않았다. 세이브 스키마 변경은 없다.
