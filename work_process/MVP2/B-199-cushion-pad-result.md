# B-199 버팀목 → 완충판 결과 (#349)

- 기준: `origin/main` `e858b49b56be34dee6b21b148ef00c07f58e22dd` (#350 산소 저장 병합)
- 만든 브랜치: `MVP2-fix143` (원격에 없었음, fix142까지 있음 → 그대로 사용)
- 커밋 해시: **미커밋** (CLAUDE.md "사용자 동의 없이 커밋 금지"에 따라 작업 트리에만 있음. 동의 뒤 커밋·push 예정)
- PR/#349/브랜치 삭제: 하지 않음

## 바꾼 것 요약

| 영역 | 내용 |
|---|---|
| 이름 | 플레이어가 보는 「버팀목」을 「완충판」으로: `ItemDisplayNames`, `Building_Support_Basic.asset`(이름·설명), `BuildingMenu.prefab` 라벨, 가이드(`GameGuideCatalog`), 목표(`DemoObjectiveCatalog`), 드론 대사(`DroneAnalysisService`), 구조 위험 상태 문구, 준비도/최종 기록 문구, 빌더 문자열. 내부 ID(`building.support.basic`)·GUID·비용(구리 1개)은 그대로. |
| 높이 | 새 `CushionPad`(Structural). 윗면 콜라이더·윗면 그림이 높이를 따라 움직이고 기둥은 Tiled로 늘어남. |
| 조절 | 완충판 위에 서 있는 동안만 위/아래 방향키 한 단계. 사다리·수직 이동·채굴 방향에서 빠짐. 설치 미리보기에서는 조절 없음. |
| 착지 피해 | 완충판 착지 = 같은 낙하·같은 짐의 일반 피해 × 0.1. |
| 저장 | `BuildingSnapshotDto.cushionHeight`(float). |
| 아트 | 윗면 1장 + 기둥 반복 조각(임시, 기존 그림을 자름). |
| 힌트 | 캐릭터 위·아래 각진 ↑↓ 키 홀로그램(코드로 그린 임시 그림). |

## 최종 값

- 기본=최소 윗면 높이 **0.8**, 최대 **1.8**(예전 버팀목 착지면), 단계 **0.25** → 0.8 / 1.05 / 1.3 / 1.55 / 1.8 (4단계). 높이 = 지면에서 윗면까지. 값 위치: `CushionPad` 검사기(프리팹 `SupportPillar.prefab`), 기본값 상수는 `CushionPadRules`(Shared).
- 만적 점프 이론 높이 약 1.156 (jumpImpulse 11 × 0.75 / 질량 1, 중력 3). 최소 0.8은 이보다 0.36 낮음. 최대 1.8은 만적으로 못 올라감(PlayMode 검사).
- 착지 비율 필드: `PlayerSurvivalSettings.cushionLandingDamageRatio`(0~1, 기본 0.1, 에셋은 건드리지 않아 기본값 적용). 규칙은 `PlayerFallDamageRules.ScaleDamage(damage, mult, stableLanding, ratio)`. 비율 0이면 0. 옆 땅·일반 블록은 기존 규칙. 예: 낙하 15m 일반 피해 15 → 완충판 2.
- 저장 필드: `BuildingSnapshotDto.cushionHeight`. 옛 세이브(필드 없음=0)·0 이하·NaN은 최소 높이로 복원, 범위 밖은 자름. 저장 직전 `WorldSnapshotSystem.CaptureBuildings`가 살아 있는 판에서 현재 높이를 읽음. 복원은 `BuildingPlacementSystem.TryRestoreBuilding`.

## 입력 규칙 (판단이 필요했던 부분 — 확인 부탁)

- 높이 키는 **방향키 ↑/↓ 한 번 누름 = 한 단계**(꾹 누름 반복 없음).
- 화살표가 이동인 스킴(Classic, 화살표 이동)은 판 위에서 화살표 ↑↓가 이동 입력에서 빠짐. Classic의 W/S, WASD 스킴의 W/S는 판 위에서도 기존 이동(사다리) 유지.
- **WASD 스킴에서는 ↑↓ 화살표가 위/아래 채굴 키**입니다. 이슈 문구 「WASD 스킴의 W/S는 기존 이동 유지」만으로는 이 경우가 명시돼 있지 않아, 판 위에 서 있는 동안만 ↑↓ 화살표 채굴(위/아래 한 칸)을 막고 높이 조절에 쓰도록 했습니다(좌우 화살표 채굴은 그대로). 다르게 하길 원하면 `PlayerKeyboardControls.ReadMiningDirection`의 한 줄을 고치면 됩니다.
- 판 위에서 높이를 바꾸면 몸도 같은 양만큼 옮겨 관통·낙하 피해 없이 판 위에 남김(`PlayerMovement.TryAdjustStandingPlatform`).

## 힌트 홀로그램

- 주기: **광산 입장마다 1회**(새 씬 로드 `LoadSceneMode.Single`마다 초기화, `CushionPadHintSession`). 같은 입장에서 한 번 보이면 다시 안 나옴. 판 위에 0.35초 이상 서 있어야 뜸(스치기 제외).
- 높이를 한 번 바꾸면 사라짐, 판에서 내려가도 사라짐.
- `ReduceMotion`이면 누름 애니메이션·밝기 변화 없이 정지 그림.
- 쓴 소리: 높이 조절 `SfxId.KeycapPop`(오름 피치 1.08 / 내림 0.92), 힌트 `SfxId.KeysHoloOpen`. 새 음원·`SfxId`·`SfxLibrary`·음량 변경 없음.
- 위치/모양: 캐릭터 위·아래에 모서리를 깎은 호박색 키캡(엘리베이터의 청록과 구분). 그림은 `CushionPadHintHologram`이 코드로 만드는 임시 24×24 px(PPU 48). `PlayerController.Awake`가 런타임에 붙임(Player 프리팹 불변).

## 아트: 임시 그림과 사용자가 그릴 것

현재 임시 아트는 `support_pillar_mine.png`(실제 **256×563**, PPU 256; 프롬프트의 252×561과 다름)를 잘라 만든 것입니다. 이미 파일이 있으면 빌더는 덮어쓰지 않습니다.

| 파일 | 크기/PPU | 내용 |
|---|---|---|
| `Art/Facilities/MVP/cushion_pad_top.png` | 256×150 px, PPU 256, 피벗 **윗변 중앙**, Mesh Full Rect | 윗면(밟는 면) 한 장 = 원본 위쪽 150행. **밟는 면은 스프라이트 맨 윗변**이어야 함(콜라이더 윗면과 맞춤). 높이 150px = 0.586 유닛이라 콜라이더 두께 0.4보다 아래로 더 내려오며 그 아래 삼각 받침이 기둥과 만남. |
| `Art/Facilities/MVP/cushion_pad_post.png` | 100×142 px, PPU 256, 피벗 **바닥 중앙**, Mesh Full Rect | 기둥 반복 조각 = 원본 x 78~177, 150~291행(주황 띠 하나 포함). `SpriteRenderer` Draw Mode **Tiled**로 위아래 반복. 위아래가 이어 붙어야 함(심 없음). 폭은 기둥 폭 약 0.39 유닛. |
| (선택) 바닥 조각 | — | 미구현. 필요하면 `CushionPad`에 `baseRenderer` 추가 후 지면 높이에 고정. |

- 기둥 길이 = 높이 − 윗면 그림 높이(0.586) + 겹침 0.04. 최소 높이 0.8에서 기둥 노출 길이 약 0.25(반복 조각 1개가 안 차므로 잘려서 보임), 최대 1.8에서 약 1.25.
- 최종 그림으로 바꿀 때 위 파일을 같은 이름으로 교체(또는 `CushionPad`의 `topRenderer`/`postRenderer` 스프라이트 교체). 윗면 그림 높이가 바뀌면 기둥 길이는 자동 보정됨.
- 건설창 아이콘(`Building_Support_Basic.icon`)은 여전히 기존 `support_pillar_mine.png`.

## 설치 미리보기

`BuildingPlacementPreview`가 윗면 그림만 대표 렌더러로 쓰면 기둥이 빠지므로, 완충판이면 반복 기둥을 자식 렌더러로 같이 그림(기본 최소 높이 모습, 높이 조절 없음). 프리팹에서 윗면(Cap)을 `VisualRoot`의 첫 자식으로 둠. 렌더 비교(스크래치 확인): 미리보기와 실제 설치 모습 일치.

## 바꾼 파일

- Shared: `Contracts/CushionPadContracts.cs`(신규: `IAdjustableLandingPlatform`, `CushionPadRules`), `DTO/WorldElementDtos.cs`(`cushionHeight`)
- Player: `PlayerMovement`(서 있는 판 추적, `TryAdjustStandingPlatform`), `PlayerController`(↑↓ 입력·소리·힌트 부착), `PlayerKeyboardControls`(판 위 입력 분리), `CushionPadHintHologram`(신규), `PlayerSurvivalState`(`ScaleDamage`), `PlayerSurvivalSettings`, `PlayerSurvivalController`
- Structural: `CushionPad.cs`(신규)
- Building/Snapshot: `BuildingPlacementSystem`(복원·높이 조회), `BuildingPlacementPreview`, `WorldSnapshotSystem`
- App 문구: `ItemDisplayNames`, `GameGuideCatalog`, `DemoObjectiveCatalog`, `DroneAnalysisService`, `GameplayHazardStatusBridge`, `Mvp2EssentialFeatureMatrix`, `FinalRunResultRecord`
- Editor: `PromptB199CushionPadBuilder`(신규, 메뉴 `SubTerra/MVP2/Build B-199 Cushion Pad`, 프리팹·데이터·임시 아트·BuildingMenu 라벨만 대상, 씬 저장 안 함), 빌더 문자열 5개(`MvpDataAssetBuilder` 등), `KoreanFontAssetUtility` 시드 글자
- Unity 에디터 API로 생성/수정: `SupportPillar.prefab`, `BuildingMenu.prefab`(라벨 1개), `Building_Support_Basic.asset`(이름·설명), 임시 PNG 2개와 `.meta`
- 테스트: 신규 `PromptB199CushionPadTests`(EditMode), `PlayerCushionPadPlayModeTests`(PlayMode, `Player.PlayModeTests` asmdef에 Structural 참조 추가), 수정 `PhaseFSupportStaticTests`, `MineStructureArtTests`, `BuildingPlacementTests`, `DroneAnalysisServiceTests`, `PromptB140GameGuideTests`, `PromptB49FacilityVisualTests`
- 교훈: `init/rules/lessons/2026-11-16-issue349-baseline-stash-dirty-assets.md`
- 되돌린 파일(테스트·에디터가 더럽힘, 커밋 대상 아님): `Mine_Demo_Integration.unity`, `SurfaceBasePanel/MainMenuPanel/InventoryPanel/ViewSocket.prefab`, `Fonts/NotoSansKR-Regular_SDF.asset`, `ProjectSettings/*`, `work_process/.../main-menu-103-*.png`, `quest-close-corner-mask.png.meta`.

## 테스트 결과

- EditMode 전체: **2759 통과 / 35 실패**. 같은 시점 `HEAD`(내 변경 없음) 기준선은 2741 / 37. 35개는 모두 기준선에도 있던 실패(오디오·디버그·UI 레이아웃 등)이고 **새 실패 없음**. 기준선에만 있던 2개는 `F_S04_BuildingSnapshot_...`(필드 목록이 오래돼 원래 실패, 이번에 `footprintWidth/Height/cushionHeight` 반영해 통과)와 시간 의존 `PlayerLadderMiningTests.Damage_ResumesThirteen...`(실행마다 흔들림).
- 새 EditMode 검사: 이름(데이터·메뉴 라벨·가이드·목표), 최소<만적 점프·최대 1.8·4단계, 클램프·옛 값 정규화, 프리팹 윗면/콜라이더/Tiled, 착지 10%(비율 0=0, 일반 땅 불변), 세이브 왕복·옛 세이브(필드 없음), 입력 분리(판 위 화살표↑↓ 예약·W/S 유지·WASD 채굴 ↑↓ 제외), 힌트 입장당 1회·높이 변경 후 숨김, 복원 시 높이·옛 값=최소, 새 SfxId 없음.
- PlayMode(`SubTerra.Gameplay.Player.PlayModeTests`): **63 통과 / 1 실패**. 실패는 기존 `LadderAnimation_ActiveTriggerContact...`(`ExitLadderMode`의 선택 인자 때문에 리플렉션 호출 실패, 이번 변경과 무관). 새 PlayMode 검사 8개 통과: 실제 물리 착지 피해 약 10%/비율 0, 판 위 4단계 상승·하강 후에도 판 위에 서 있음(관통·낙하 피해 없음), 판 밖 입력 무시, 만적 점프로 최소 판은 오르고 최대는 못 오름, 힌트 표시/숨김/모션 감소 정지, 프리팹 불변.
- Unity Console 빨간 오류 0건(컴파일·테스트 후).
- 렌더 확인: 높이 3단계(0.8/1.3/1.8) 윗면·기둥 늘어남, 설치 미리보기=실제 모습.

## 미검증 (직접 플레이 필요)

- Bootstrap → 새 게임 → 광산에서 실제 입력(키보드)으로 높이 조절, 사다리·채굴과의 겹침을 사람이 눈으로 확인하지 못함(입력은 단위 검사만).
- 실제 광산 조명/깊이 어둠 속에서 홀로그램 가시성, 카메라 거리에서 키 크기.
- 광산을 나갔다 이어하기에서 높이 유지(세이브 왕복은 DTO/복원 단위 검사로만 확인, 전체 세이브 파일 경로는 미확인).
- 소리 느낌(KeycapPop 피치 변화, KeysHoloOpen)은 듣지 않음.
- 임시 그림이 어색해 보이는 정도.
- 완료 체크리스트 중 `MVP2-fix143` push·커밋 해시는 동의 전이라 미완.

## 사용자 테스트 가이드 (직접 플레이)

1. 건설창·가이드·툴팁·관련 목표에서 이름이 완충판인지, 비용이 구리 1개인지 본다.
2. 짐을 최대로 싣고, 기본(최소) 높이 완충판 옆에서 점프해 올라간다.
3. 판 위에 서서 위를 눌러 최대까지 올린다. 예전 버팀목 높이(약 1.8)와 같은지 본다. 만적 점프로는 최대에 못 올라가는 것이 정상이다.
4. 판 위에서 아래를 눌러 최소까지 낮추고, 다시 만적으로 올라갈 수 있는지 본다. 최소보다 낮아지거나 최대보다 높아지지 않아야 한다. 광산을 나갔다 이어서도 높이가 유지되는지 본다.
5. 판에서 내려 위/아래를 눌러도 높이가 안 바뀌는지, 사다리에서는 평소처럼 오르내리는지 확인한다. WASD 스킴이면 판 위에서 ↑↓ 화살표가 채굴이 아니라 높이 조절인지 확인한다.
6. 같은 높이에서 일반 땅과 완충판에 떨어져, 완충판 피해가 약 10%인지 비교한다.
7. 높이를 바꿀 때 윗면이 같이 움직이고, 기둥이 타일처럼 자연스럽게 늘어나는지 본다. 임시 그림이 늘어져 보이는 것은 아트 교체 전이면 허용한다.
8. 광산에 들어와 완충판에 처음 올라가면 위·아래 키 홀로그램이 캐릭터 위아래에 보이는지 확인한다. 높이를 바꾸면 사라지고, 같은 입장에서 다시 올라가도 안 나오는지 본다. 모션 감소를 켜면 홀로그램이 멈춰 있어야 한다.
9. Unity Console에 새 빨간 오류가 없는지 본다.
