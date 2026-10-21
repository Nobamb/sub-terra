# #222 음소거 / #223 비연출 표시 작업 — 2026-10-21

## 환경·범위

- 저장소 C:/Users/jeone/sub-terra, Unity 프로젝트 C:/Users/jeone/sub-terra/sub-terra (Hub 첫 번째 프로젝트), Unity 6000.5.4f1.
- 현재 codex/nonvisual-review-20261021에서 기존 게시 수정은 보존하고 작업했다. 이번 변경은 아직 커밋·푸시·PR 등록 전이다. 게시 시 #222와 #223 부분 작업을 독립 PR로 분리한다.
- ProjectSettings·패키지·씬·프리팹 YAML·폰트는 수정 대상이 아니다. 기존 UI에 런타임으로 표시하며, 심층 표시 에셋만 명시된 Editor 빌더로 생성한다.

## #222 설정 음소거

- 공통 SettingsMenuSkin에서 기존 음량 슬라이더 왼쪽에 32px 도트 확성기 버튼을 한 번만 생성한다. 별도 이미지/폰트 글리프 없이 기존 청록색 UI 톤의 Graphic으로 표시한다.
- 클릭하면 슬라이더 값을 바꾸지 않고 AudioListener.volume을 0으로 만든다. 다시 클릭하면 현재 슬라이더 음량으로 복귀한다.
- 음소거 상태는 기존 음량과 별도 PlayerPrefs 키 subterra.settings.masterMuted에 즉시 저장한다. 키가 없는 기존 설정은 비음소거로 처리한다.
- 슬라이더를 실제로 조작하면 음소거를 해제한다. 슬라이더 0에서도 대각선이 보인다. 숨김/재표시와 여러 설정 화면은 같은 상태를 읽는다.
- 메인 메뉴·지상·광산의 기존 설정 적용/음량 복원 경로가 음소거를 유지하며, 시작 시에도 저장된 음소거 상태를 반영한다.
- 음소거는 조작 방식과 같은 즉시 적용 항목이다. 음량 슬라이더 값 자체의 저장은 기존 적용/취소 정책을 유지한다. 취소가 음소거 토글 자체를 되돌리지는 않는다.
- 파일: Scripts/App/UI/MainMenu/SettingsRuntimeApplier.cs, SettingsMenuSkin.cs, 새 SettingsMuteButton.cs(+meta), Tests/EditMode/App/UI/PromptB104SettingsMenuTests.cs.

## #223 제목·심층 단계 표시 (부분 작업)

- 실제 업그레이드 창 제목을 '업그레이드'로 통일한다. 기존 창 크기·위치는 유지하고 관련 빌더 및 구형 가이드 생성 문구도 같은 명칭을 사용한다.
- Resources/DeepZoneDisplaySettings.asset의 단계 목록에서 최대 단계 수와 깊이를 읽는다. Inspector에서 표시용 정의를 바꿀 수 있으며 런타임에 최대 Lv.3/깊이를 하드코딩하지 않는다.
- 초기 표시 정의: Lv.1 36~40M(현재 Demo 지원), Lv.2 41~75M(예정), Lv.3 76~100M(예정).
- 구형 boolean 해금 상태를 표시 시 Lv.0/3 또는 Lv.1/3으로 해석한다. 저장 스키마·실제 접근 판정·광산 깊이·해금 비용은 바꾸지 않는다. 기존 해금을 전체 최대 단계 해금으로 잘못 표시하지 않는다.
- Lv.1 조건은 현재 DeepZoneUnlockRule.Mvp의 드릴 Lv.2 + 스캔 Lv.2 + 가스 저항 Lv.1을 그대로 읽는다. 별도 중복 조건을 만들지 않는다.
- Lv.2의 예정 안내는 #50의 Lv.1 해금·목표 13개를 따른다. Lv.3 비용은 #50에서 예상값으로 남아 있어 확정 수치로 표시하지 않고 준비 중으로 안내한다. 둘 다 실제 해금 버튼/진행 기능이 아니다.
- 이번 작업으로 #223 전체가 완료되지는 않는다. 썸네일·흑백·반짝임·자물쇠·색 복귀 연출과 #50 실제 Lv.2/3 탐사 확장은 제외한다. #224 행성별 분리도 제외한다.
- 파일: Scripts/App/Progression/DeepZoneDisplaySettings.cs(+meta), Scripts/App/UI/Progression/ProgressionPanelView.cs, UpgradeTreeView.cs, Editor/DataValidation/Issue223DisplaySettingsBuilder.cs(+meta), PromptB117SurfaceBaseBuilder.cs, PromptB118UpgradeTreeBuilder.cs, PhaseQPanelLayoutBuilder.cs, Resources/DeepZoneDisplaySettings.asset(+meta), 관련 기존 EditMode 검사 2개 파일.

## 최소 검사·이후

- 선택 검사: 음소거/복귀·슬라이더 유지·슬라이더 조작 해제·저장값 재적용·0 상태·기존 두 설정 프리팹, 구형 심층 해금 해석·임의 깊이/단계 설정 반영·제목/크기 유지·안내 중복 및 공간 보존.
- Editor 빌더 EnsureAsset는 에셋이 이미 있으면 덮어쓰지 않는다. 설정 에셋 생성 후 이 검사들만 실행한다.
- 최종 EditMode 6개 선택·실행·통과, 실패/건너뜀 0, ExitCode=0, 2.24초. 컴파일 오류 없음, diff 형식 검사 통과. 원시 결과 work_process/tools/settings-deepzone-20261021/.
- 검사 대상 밖의 추적 에셋 변경은 없다. 새 C# 파일 3개와 표시 에셋의 .meta가 Unity에서 생성됐으며 기존 GUID는 변경하지 않았다.
- 실제 소리 청취·화면 육안 확인·완전 종료 재시작은 수행하지 않았다. 저장값 재적용 검사는 실제 앱 재시작을 대체하지 않는다. 사용자 설정 키와 AudioListener 값은 각 검사 finally에서 원래 상태로 복원했다.
- 팀원 확인: 메인·지상·광산 설정에서 확성기 클릭/재클릭, 음소거 후 슬라이더 이동, 원래 음량 유지와 대각선, 적용된 음량으로 재실행. 업그레이드 제목과 심층 안내 3단계가 잘리지 않는지 확인한다.
- 다음: 팀원 화면/소리 확인 후 각 이슈별 게시. #211은 기존 PR #233 미재현 조사 기록에 따라 특정 재현 슬롯/경로가 확보되면 조사한다.

## 게시 갱신

- 위 미커밋·미게시 문구는 작업 당시 기록이다. 이후 사용자 요청으로 #222는 PR #234, #223 비연출 부분은 PR #235에 독립 커밋·푸시했다.
- #234: 4dc8f328, codex/issue-222-settings-mute. #235: 4cf84314, codex/issue-223-deep-zone-display.
- 두 PR은 main ee25ddab 기준이며 GitHub 조회에서 충돌 없음(mergeable=true, clean)을 확인했다. #235는 Refs #223으로 연결하고 이슈를 자동으로 닫지 않는다.
- 첫 번째 프로젝트의 codex/nonvisual-review-20261021에도 두 변경을 통합했다. 각 PR의 대상 파일과 통합 파일이 동일함을 비교했으며 Unity 검사를 반복하지 않았다. main과 PR은 병합하지 않았다.
- 다음 비연출 후보는 #221 퀘스트 썸네일의 검은 배경 스타일 교체다. 7~13, 16~18번 전체를 한꺼번에 제작하기보다 1장으로 톤·구도를 맞춘 뒤 확대한다. 이어서 #223 정적 심층 이미지·잠금 표시에 한정할 수 있다. #211은 특정 재현 조건 확보 후 조사한다.
