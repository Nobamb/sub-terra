# Unity 테스트 실행과 UI 완료 기준

Unity 프로젝트는 `sub-terra/`, Editor는 6000.5.4f1, 설치된 Unity Test Framework는 1.7.0이다. 저장소 루트에서 아래 명령을 실행한다. 기본 경로는 해당 프로젝트를 연 **렌더링 가능한 일반 Editor**다. 다른 테스트나 Play를 실행 중이면 먼저 종료한다.

| 범위 | 명령 | 포함하는 검증 |
| --- | --- | --- |
| 빠른 확인 | `./tools/Run-UnityValidation.ps1 -Scope quick -Filter PromptB112 -Mode Both` | 지정 이름을 포함하는 기능 테스트; Visual 제외 |
| 일상 검증 | `./tools/Run-UnityValidation.ps1 -Scope daily -Mode Both` | 프로젝트의 모든 EditMode/PlayMode 기능 테스트 |
| 지정 UI Visual | `./tools/Run-UnityValidation.ps1 -Scope visual -Filter 'PromptB105;PromptB110;PromptB112;Issue119'` | 지정 화면의 해상도·레이아웃 assert와 캡처, 다른 비율의 입력 검증 |
| 전체 검증 | `./tools/Run-UnityValidation.ps1 -Scope full -Mode Both` | 모든 프로젝트 EditMode/PlayMode 테스트, Visual 포함 |

`-Filter`는 테스트 전체 이름에 대한 부분 문자열이며 `;`로 여러 항목을 지정한다. `quick`은 필터가 필수다. `-Mode` 기본값은 `PlayMode`; 데이터 계산 이동까지 포함한 일상/전체 검증에서는 `Both`를 사용한다. `-Output`에 **새 결과 폴더**를 지정할 수 있다. 기본값은 프로젝트 `Temp/validation-<고유 ID>`다. 테스트 실패·0개 선택·결과 누락·시간 초과는 PowerShell 종료 코드 1, 정상 완료는 0이다. 기본 전체 제한 시간은 1200초이며 `-TimeoutSeconds`로 조정한다.

Windows PowerShell에서 스크립트 실행 정책이 제한된 경우, 시스템 설정을 바꾸지 않고 해당 프로세스에만 적용한다:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-UnityValidation.ps1 -Scope daily -Mode Both
```

Editor 메뉴:

- `SubTerra/Tests/Validation/Daily PlayMode`
- `SubTerra/Tests/Validation/Visual PlayMode`
- `SubTerra/Tests/Validation/Full EditMode and PlayMode`
- 기존 `SubTerra/Tests/Run Play Mode Tests`와 `Temp/subterra-run-playmode.flag`는 이제 일상 PlayMode를 실행한다. 기존 `Temp/subterra-playmode-results.txt`와 `.done`도 기록한다.

기존 기능별 메뉴에 전달된 명시적인 assembly 목록과 기존 필수 QA/빌드 경로는 유지한다. UI 완료 판단에는 그 메뉴 하나만 사용하지 말고, 위 기능 + Visual 경로를 사용한다.

`LadderBackBootstrapPlayModeTests`는 기존 안전 규칙상 Editor 시작 인자 `-subterra-save-root <새로운 sub-terra/Temp 하위 폴더>`가 필요하다. 폴더는 테스트 실행 전 존재하지 않아야 한다. 현재 Editor에 이 인자가 없으면 이 테스트는 Ignore로 기록되므로 전체 검증 완료로 간주하지 않는다. 해당 검증은 같은 프로젝트를 닫은 뒤 이 인자를 넣어 렌더링 Editor를 새로 시작하고 `full -Mode Both`로 실행한다. 반복 실행마다 새 경로를 사용한다. 다른 UI 테스트는 자체 임시 저장 경로 또는 슬롯 0을 사용한다.

## 결과와 필터 확인

각 모드에 별도 파일을 남긴다.

- `<Mode>-discovered.txt`: 프로젝트에서 발견한 전체 실행 후보, Functional/Visual 및 RunState.
- `<Mode>-selected.txt`: 필터 적용 뒤 선택된 전체 테스트 이름, RunState, Category.
- `<Mode>-progress.txt`: 실제 시작·완료 이름, 성공/실패/스킵 상태, 테스트별 초 단위 시간.
- `<Mode>-results.xml`: NUnit 결과, assert 수, 실패 메시지/스택.
- `<Mode>-stages.tsv`: 계측한 준비·씬·해상도·입력·실제 애니메이션·캡처 시간. `setup-total`은 하위 구간을 포함하므로 중복 합산하지 않는다.
- `<Mode>-summary.txt`: 선택/실행/성공/실패/스킵/inconclusive 수, 시간, 누락·예상 밖 테스트, 종료 코드.
- `complete.txt`: 두 모드가 모두 끝난 뒤의 최종 종료 코드. `Both`에서는 한 모드 실패가 다른 모드의 성공으로 덮이지 않는다.

`Run PlayMode`와 `Run EditMode`를 따로 실행하며 모드별 결과를 확인한다. 카테고리 제외 문자열에 의존하지 않고, 실제 발견 목록에서 Visual 여부를 결정한 뒤 UTF `Filter.testNames`에 전체 이름을 전달한다. 테스트가 0개이면 필터 성공으로 간주하지 않는다. 선택된 이름과 결과의 이름·개수를 대조한다. 현재 Visual에 `Explicit`를 붙이지 않는다. 이후 Explicit가 추가되어도 `full`은 이름을 직접 선택하므로 기본 필터에서 제외하지 않는다. 스킵 사유는 XML/진행 기록에서 별도로 확인한다.

## 렌더링과 batch mode

Visual/full은 일반 Editor와 Game View가 필요하다. 러너와 캡처 헬퍼는 batch/Null graphics 환경의 Visual 실행을 명시적으로 실패시킨다. `WaitForEndOfFrame`은 Editor batch mode에서 실행되지 않는다([Unity 문서](https://docs.unity.com/ja-jp/engine/6000.5/script-reference/unityengine/waitforendofframe)). 테스트 캡처는 해당 yield를 사용하지 않고 `CaptureScreenshot` 요청 뒤 실시간 제한 안에 PNG 저장 완료·해상도를 확인한다. 테스트가 Game View를 활성화하고 선택한 크기를 복원한다.

캡처 파일은 `sub-terra/Temp/visual/prompt-b105-2`, `prompt-b110`, `prompt-b112`, `sub-terra/Temp/issue119-<해상도>.png`에 기록한다. 이전 증거 파일을 자동으로 덮어쓰거나 커밋하지 않는다. 보관할 증거는 실행 완료 뒤 별도 복사한다. 일상 기능 테스트는 캡처 함수를 호출하지 않는다.

Editor를 닫은 뒤 기능/데이터 테스트를 batch로 실행하려면:

```powershell
./tools/Run-UnityValidation.ps1 -Scope daily -Mode Both -Batch
```

직접 진입점은 `SubTerra.App.Editor.DataValidation.TestValidationRunner.Batch` 또는 호환 진입점 `HeadlessTestRunner.Run`. 옵션은 `-validationScope`, `-validationMode`, `-validationFilter`, `-validationOutput`이다. `-quit`를 따로 붙이지 않는다. 러너가 결과 기록 뒤 실제 실패 여부로 Editor를 종료한다. **batch 경로의 실제 실행 여부와 제한은 작업 결과 보고서를 확인한다.**

## UI 작업 완료와 배포

- 단일 UI 작업 완료 전 해당 이름의 기능 테스트와 관련 Visual을 **모두** 실행한다. 예: `quick -Filter PromptB112 -Mode Both`, 이어서 `visual -Filter PromptB112`.
- 공통 UI Canvas, 폰트, 프레임, 입력·레이아웃 헬퍼 변경은 `visual`을 필터 없이 실행해 관련 화면 전체를 검증한다. 새 해상도 테스트는 메서드에 `Category("Visual")`를 지정하고 기능 기준 해상도 검증은 기본 경로에 남긴다.
- 큰 변경·배포 전에는 `full -Mode Both`와 기존 [Windows QA](MVP2_WINDOWS_QA.md), [필수 QA 규칙](../init/rules/testing-qa.md)을 모두 유지한다. Phase P Windows Development/QA/Release Build 메뉴, 데모 완주, 새 게임/이어하기, 다른 Windows PC 재실행, 세이브 생성·호환성·Console/Player.log 검증을 대체하지 않는다.
- Visual 야간 자동 실행은 제공되지 않는다. 이 문서의 명령/Editor 메뉴가 제공된 실제 실행 경로다.

## 분리 전후 검증 대응표

메서드 이름은 각 기존 fixture 안에 있으며 `F`는 기본 기능 실행, `V`는 Visual 실행이다.

| 분리 전 검증 | 분리 후 위치 |
| --- | --- |
| B105 `InGameMenu_ActionsHoverTransitionsAndHiddenShortcuts`: 6개 아이콘/연결, 시설·인벤토리·업그레이드·가이드 토글, 설정 열기/닫기, Quit 연결 | F: 동일 메서드. 버튼의 실제 마우스 클릭·레이캐스트도 확인 |
| B105 시간 정지 중 모든 hover 중간 알파/파티클/최종 알파/페이드 | F: 동일 메서드. 실제 연출 완료 조건과 실시간 timeout |
| B105 `/` 닫기/재열기, 전환 중 중복 Toggle 무시, 0.25초 중간 위치·incoming 비활성, 두 root 동시 표시 금지, 완료 위치 | F: 동일 메서드. 0.25초 검증 대기 유지 |
| B105 닫힌 상태의 B/I/U/G/Escape, 재활성화 시 열린 상태·위치 복원, 저장 슬롯 0 | F: 동일 메서드 |
| B105 open/closed/slash 캡처와 1920×1080·2560×1440·1366×768 각 open/closed 경계 assert·전환·캡처 | V: `InGameMenu_OpenClosedLayout_AtThreeResolutions`. 기준 해상도의 경계·실제 클릭 가능성은 F에도 유지 |
| B110 `BuildingMenu_SelectionCostsAvailabilityAndLayout`: 바인딩/구조, 초기 미선택·비용 숨김, 버팀목 ID/하나만 선택/자원 부족 문구·기호 | F: 동일 메서드. V의 공유 상태 시퀀스에서도 확인 |
| B110 구리 획득 후 충분 비용 행/목록 ready glyph/설치 배너, 코어 2행·3행 숨김·일부 부족 문구 | F: 동일 메서드. V: `BuildingMenu_StatesAndLayout_AtThreeResolutions`에서 상태별 캡처와 assert |
| B110 B 닫기·선택 취소·재열기 미선택, X 닫기·슬롯 0 | F: 동일 메서드. V에서도 마지막 해상도 입력 경로 보존 |
| B110 상태 4개 캡처, 3해상도 화면 경계·퀘스트/우측 메뉴 겹침·텍스트 잘림 assert/캡처 | V: `BuildingMenu_StatesAndLayout_AtThreeResolutions`. 기준 해상도의 필수 텍스트/배치·버튼 레이캐스트는 F에도 유지 |
| B112 `InventoryPanel_LiveValuesStatesAndLayout`: I 열기·PanelRoot, 빈 화물 0/4 종류·보유 없음·서비스 일치 | F: 동일 메서드 |
| B112 열린 상태 구리4/철3/연료1 갱신, 종류3/4·행 수량/보유상태, 85% 이상·가득 참·100% 조건 | F: 동일 메서드. V의 공유 상태 시퀀스에서도 assert 유지 |
| B112 도움말 hover, I 닫기·닫힌 동안 철 감소·재열기 최신값, X 닫기·슬롯 0 | F: 동일 메서드. V에서도 마지막 해상도 입력 경로 보존 |
| B112 empty/normal/near-full/full/help 5개 캡처, 3해상도 화면 경계·퀘스트/건설창/우측 메뉴 겹침·텍스트 잘림 assert/캡처 | V: `InventoryPanel_StatesAndLayout_AtThreeResolutions`. 기준 해상도 필수 텍스트/배치·닫기 버튼 실제 클릭은 F에도 유지 |
| Issue119 점유 슬롯 요청, normal/backup 저장 바이트·State 참조 보존, 취소/X 버튼 실제 클릭·2px 이동·drag 차단, X키 | F: `OccupiedSlot_CancelCloseKeySortingAndConfirm`, 1920×1080. V: `OccupiedSlot_LayoutAndInput_AtTwoAspectRatios`, 두 비율 모두 같은 입력·보존 assert |
| Issue119 설정창 > 덮어쓰기 sorting, X키 설정 먼저 닫기·덮어쓰기 유지·다음 X 닫기 | F: 위 점유 슬롯 기능 테스트. V: 위 두 비율 테스트 |
| Issue119 hover 완료·확인 버튼 scale 유지, 확인 연속/Presenter 중복 호출에도 starts/SurfaceBase load 각1, ActiveSlot1·실제 저장 변경 | F: 위 점유 슬롯 기능 테스트. V에도 유지 |
| Issue119 본문 overflow 없음·1줄·팝업 화면 경계, 1920×1080·1280×1024 캡처 | V: 위 두 비율 테스트. 기준 해상도의 본문/화면 경계는 F에도 유지 |
| Issue119 빈 슬롯 팝업 없이 시작, 연타에도 starts/load 각1·ActiveSlot2 | F: `EmptySlot_StartsWithoutPopup_AndIgnoresRepeatedNewGameClick`, 기준 해상도 |
| Issue119 빈 슬롯 1280×1024 기능·중복 시작 방지 | V: `EmptySlot_StartsWithoutPopup_AtFiveByFourAspectRatio` |
| PlayerMovement 순수 CargoSpeedPolicy 경계값 5개, CargoLoadEffectPolicy 계산 4개 | EditMode `CargoPolicyCalculationTests`의 기존 이름/파라미터/assert 그대로 이동. 실제 jump/낙하/MonoBehaviour 결합 테스트는 PlayMode 유지 |

상태 시퀀스를 공유하는 B110/B112는 별도 테스트마다 독립 환경을 생성한다. Visual은 한 환경에서 여러 해상도를 검증해 해상도마다 씬/데이터를 다시 준비하지 않는다. 런타임·Prefab·Scene·Font·ProjectSettings·Packages 변경으로 테스트를 빠르게 만들지 않는다.
