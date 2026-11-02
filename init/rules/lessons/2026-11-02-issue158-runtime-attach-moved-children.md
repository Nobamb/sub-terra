# 런타임에 버튼을 찾아 소리를 붙일 때는 연출이 옮긴 계층과 EditMode Awake를 함께 고려
- 날짜: 2026-11-02
- 태그: audio, ugui, runtime-attach, editmode, issue158
- 실수: B-180에서 퀘스트 완료 창 닫기 버튼을 `transform.Find("ClaimCloseButton")`로 찾게 썼다. 바로 앞 줄의 `ConfigureQuestClear`가 본문을 `ClaimContent` 아래로 옮겨 실제로는 못 찾는다. CCTV 검사도 항목의 선택 리스너(Awake에서 붙음)가 EditMode에서 돈다고 가정해 한 번 실패했다.
- 원인: 연출 컴포넌트가 초기화하면서 자식 계층을 재배치하고, EditMode에서 AddComponent·Instantiate는 Awake/OnEnable을 부르지 않는다.
- 다음부터 할 일: 버튼을 이름으로 찾을 때는 계층을 바꾸는 Configure 이후에 하위 전체(`GetComponentsInChildren(true)`)에서 찾는다. 검사는 기존 B-154처럼 같은 연출을 거친 실제 구조로 만들고, Awake에서 붙는 리스너에 기대는 단언은 하지 않거나 Awake를 직접 부른다.
- 관련: #158, B-180, QuestClearPopupMotion, PromptB180SfxMenuFillTests.
