# MVP-B 단계별 작업 안내

`init/prompt-B.md`의 1번 지시와 `init/process-B.md`의 `B-0`~`B-14`를 실행 가능한 작업 단위로 나눈 문서 모음이다. 각 단계 폴더의 `research.md`를 따라 구현하고, 같은 폴더의 `test.md`로 완료 여부를 검증한다.

## 단계 매핑

| 단계 | 원본 절차 | 주제 | 선행 단계 |
| :-- | :-- | :-- | :-- |
| A | B-0 | Bootstrap과 기본 프로젝트 상태 | 없음 |
| B | B-1 | ScriptableObject 데이터 카탈로그 | A |
| C | B-2 | 기본 HUD와 Game State | A, B |
| D | B-3 | 인벤토리와 화물 시스템 | B, C |
| E | B-4 | 경제, 판매와 제작 | B, D |
| F | B-5 | 업그레이드와 진행도 | B, E |
| G | B-6 | 건설 메뉴와 위험 UI | C, E, A의 배치 계약 |
| H | B-7 | 전진기지 UI와 정산 | D, E, G, A의 전진기지 Runtime |
| I | B-8 | 드론 판단과 템플릿 대사 | C, A의 `DroneContextDto` |
| J | B-9 | 선택적 클라우드 AI 대사 | I |
| K | B-10 | 로컬 세이브 시스템 | A, D, F, A의 월드 스냅샷 계약 |
| L | B-11 | Main Menu와 Surface Base | E, F, K |
| M | B-12 | Integration Scene 구성 | A~I, K, L, A 인수인계 |
| N | B-13 | 데모 흐름과 튜토리얼 | M |
| O | B-14 | Windows 빌드와 배포 | N |

J 단계는 선택 사항이다. J를 생략해도 I의 규칙 기반 추천과 템플릿 대사만으로 전체 MVP와 오프라인 플레이가 완성되어야 한다.

## 공통 실행 원칙

1. Unity 프로젝트 루트는 저장소 안의 `sub-terra/`이다.
2. 현재 `ProjectSettings/ProjectVersion.txt`의 `6000.5.4f1`과 현재 패키지 버전을 유지한다. 기획 문서와 버전 표기가 다르더라도 작업 중 임의 업·다운그레이드하지 않는다.
3. B의 구현은 `Assets/_Project/Scripts/App/`, `Data/`, `Prefabs/UI/`, B 소유 Scene·Tests·Editor 폴더에만 둔다.
4. `Scripts/Gameplay/`, Gameplay Runtime Prefab, Gameplay Test Scene, Tilemap은 A 소유이므로 수정하지 않는다.
5. `Scripts/Shared/` 계약이 없거나 요구와 다르면 임의로 만들거나 바꾸지 말고, 필요한 시그니처와 사용 위치를 Issue로 남긴다.
6. UI 입력은 Service를 호출하고, Service가 State를 바꾸고 이벤트를 발행한 뒤 UI가 표시만 갱신한다.
7. ScriptableObject에는 정적 정의만, State와 Save DTO에는 플레이어별 런타임 값만 저장한다.
8. Scene·Prefab은 Unity Editor 또는 검토 가능한 Editor Script로 수정한다. YAML을 직접 편집하지 않는다.
9. 각 단계는 해당 `test.md`의 자동 테스트와 수동 검증을 통과하고 Console 오류가 없어야 완료로 표시한다.

## 단계별 기록 방식

- 테스트 결과는 각 `test.md`의 “검증 결과 요약”에 실행 환경, 날짜, 통과/실패/미실행과 근거를 기록한다.
- A 작업물이 필요한 검증은 가짜 구현으로 단위 테스트한 결과와 실제 Runtime Prefab으로 통합 검증한 결과를 구분한다.
- 실패 시 증상, 재현 절차, 기대 결과, 소유 영역과 요청 범위를 남긴다.
- 각 단계 PR에는 그 단계와 직접 관련된 파일만 포함한다.

## 단계별 Git 작업

| 단계 | 권장 브랜치 |
| :-- | :-- |
| A | `feature/b-bootstrap` |
| B | `feature/b-game-data` |
| C | `feature/b-hud` |
| D | `feature/b-inventory` |
| E | `feature/b-economy` |
| F | `feature/b-progression` |
| G | `feature/b-building-ui` |
| H | `feature/b-outpost-ui` |
| I | `feature/b-drone-analysis` |
| J | `feature/b-cloud-dialogue` |
| K | `feature/b-save-system` |
| L | `feature/b-menus` |
| M | `feature/b-integration-scene` |
| N | `feature/b-demo-flow` |
| O | `feature/b-windows-build` |

각 단계는 `develop`의 최신 상태에서 브랜치를 만들고, 구현 → 해당 단계 자동 테스트 → 관련 회귀 테스트 → Unity Console 확인 → 소유 영역 diff 확인 → PR 순서로 진행한다. Shared 계약, Unity/패키지 버전, ProjectSettings 변경은 별도 합의와 양쪽 검증 없이 단계 PR에 섞지 않는다.

## A 작업물 수령이 필요한 단계

G, H, I, K, M에서는 다음 순서를 추가한다.

1. A의 인수인계 문서와 필수 참조·이벤트 목록을 확인한다.
2. A의 독립 Test Scene에서 기능을 먼저 검증한다.
3. Runtime Prefab을 원본 그대로 배치하고 Shared 계약으로 B 구현을 연결한다.
4. 대역 기반 테스트와 실제 Runtime 기반 통합 테스트 결과를 구분해 남긴다.
5. A 단독 Scene에서도 재현되는 문제는 Runtime을 직접 고치지 않고 기능명, 위치, 재현 절차, 현재/기대 결과와 요청 범위를 포함한 Issue로 보낸다.

## 최종 완료 감사

O 단계 완료 전에 다음 인도물이 현재 파일 또는 빌드 산출물로 실제 존재하는지 확인한다.

- Bootstrap, Main Menu, Surface Base, Mine Demo Integration Scene
- GameDataCatalog와 Mineral·Building·Recipe·Upgrade 데이터
- Game/Player/Inventory/Upgrade/Progress/Run State
- Inventory, Economy, Crafting, Progression, Drone, Save/Load Service
- 템플릿 대사와 선택 시 안전한 Cloud 폴백
- HUD와 process-B에 지정된 B 소유 UI Prefab
- Save Migration, Build Profiles, README, CHANGELOG
- Integration QA 결과와 다른 PC에서 검증된 Windows Release ZIP

개별 클래스가 존재하는 것만으로 완료로 보지 않는다. 새 게임 → 탐사 → 채굴/건설/위험 → 귀환/정산/업그레이드 → 저장 → 프로세스 종료 → 이어하기 → 월드 복원 → 데모 종료가 하나의 빌드에서 완주되어야 한다.
