# #195 지상 기지 퀘스트 아이콘

- 기준: origin/main fa2a9db4. 브랜치: codex/issue-195-quest-icon.
- 판매·업그레이드 아이콘을 참조해 은색 금속, 청록색 발광, 짙은 외곽선의 체크리스트 아이콘을 제작했다.
- SurfaceBasePanel.prefab의 QuestButton/Icon 스프라이트와 B117 빌더의 에셋 경로만 교체했다. 버튼 위치·크기·호버·퀘스트 연결은 유지한다.
- 새 PNG와 Unity 생성 meta를 포함한다. 프리팹을 Editor로 저장하면서 기존 SurfaceBaseButtonFeedback의 particleColor 기본값 8개도 직렬화됐다. 기존 코드 기본값과 동일해 색상 동작은 바뀌지 않는다.
- 전용 메뉴: SubTerra/UI/Apply Issue 195 Surface Quest Icon. 전체 지상 UI를 재생성하지 않는다.

## 최소 검증

- Unity 6000.5.4f1, 숨김 batchmode에서 BuildAndVerify 실행, 종료 코드 0.
- QuestActions_UseEqualSizeAndSpacing 통과.
- QuestIcon_UsesSurfaceSetAssetWithoutChangingLayout 통과.
- 최초 검사에서 LayoutGroup이 관리하는 너비를 sizeDelta로 검사해 실패했다. 실제 preferredWidth 검사로 바로잡고 재실행했다.
- 실제 Game 화면에서 그림체·인지성 비교는 팀원 확인 대상으로 남긴다. Windows 빌드/전체 Demo 실행은 하지 않았다.

## 인계

- 첫 번째 Unity 프로젝트: C:/Users/jeone/sub-terra/sub-terra.
- SurfaceBase 씬에서 퀘스트 버튼을 판매·업그레이드와 비교한다. 버튼 클릭 시 기존 퀘스트 창이 열린다.
- #119 보상 창 여백은 별도 브랜치/PR이다. #191 드론 조준점 연출은 사용자 요청으로 보류했다.
