# B-214 결과: 시설 30초 주기 연출 + 포탈 내부 홀 회전

## 상태
- 브랜치: `MVP2-fix158` (origin/main `c2be3a3d`에서 생성, 원격에 같은 이름 없음). **커밋·push는 아직 하지 않았다**(사용자 동의 대기).
- 커밋 해시: 미정(커밋 전).
- 기준 main: 문서에는 `c3edb25`로 적혀 있으나 작업 시작 시 `origin/main`은 `c2be3a3d`였다. **그림 PNG 크기·해시 대조는 하지 않았다(미검증).** 대신 보이는 영역(`GetVisibleBounds`) 사각형 기준 비율로 자리를 쓰고, 보관함 이음선과 전진기지 LED는 PNG를 직접 보고 쟀다.

## 바꾼 파일
| 파일 | 내용 |
|---|---|
| `Gameplay/Building/FacilityIdleAnimation.cs` (신규) | 공통 주기 장치. 첫 시작·주기·흔들림·엇갈림·건너뜀·0.15초 중단·정리 |
| `Gameplay/Building/FacilityIdleEffects.cs` (신규) | 시설별 연출 6종(조명·충전기·보건소·보관함·정산기·전진기지) |
| `Gameplay/Building/FacilityIdleAnchors.cs` (신규) | 자리·길이 한 표, BuildingId→종류, 조명 밝기·보관함 뚜껑 곡선 |
| `Gameplay/Building/FacilityIdleSprites.cs` (신규) | 코드로 그린 하트·금화(Point)·glow·고리·와이파이 호·소용돌이·마스크. 한 번만 만들어 공유 |
| `Gameplay/Building/PortalHoleSpin.cs` (신규) | 포탈 내부 홀 회전(켜짐에서만) |
| `Gameplay/Power/FacilityVisualDim.cs` (신규) | 조명 후광 알파 배율(Building→Power, Hazards→Power 참조 방향 유지) |
| `Gameplay/Hazards/LightHaloVisual.cs` | 후광 알파에 `FacilityVisualDim.Multiplier`를 곱함(판정 불변) |
| `Gameplay/Building/BuildingInstance.cs` | `Awake`/`Initialize`에서 `FacilityIdleAnimation.EnsureFor` 호출(설치·복원·이동 공통) |
| `Gameplay/Building/BuildingPlacementSystem.cs` | `MovingInstance` 정적 값 추가(옮기기를 고른 시설은 연출 건너뜀) |
| `App/Integration/Debug/DeveloperDebugFacilityIdleCommands.cs` (신규) + Registry 한 줄 | `idlefx` 명령 |
| `Tests/EditMode/Gameplay/Building/FacilityIdleAnimationTests.cs` (신규), `Hazards/LightVisibilityTests.cs` | EditMode 테스트 |

프리팹·씬 YAML, 시설 PNG, 칸 수, 전력 범위, 상호작용, 건설 연출(B-206), 켜짐 연출(B-185)은 수정하지 않았다. 새 SFX·`SfxId`·`SfxLibrary` 변경 없음. Gameplay→App 참조 없음.

## 최종 값
- 주기 30초, 매 주기 ±3초(주기가 짧으면 주기의 절반까지만), 첫 시작 3~30초, 다음 시작은 연출이 끝난 뒤부터 센다.
- 엇갈림: 같은 종류끼리 남은 시간이 0.5초 이내면 1~2초씩 밀어 준다(최대 16번 재검사).
- 중단: 조건이 깨지면 0.15초 동안 세기를 1→0으로 줄인 뒤 정리.
- 연출 길이: 조명 1.0 / 충전기 1.2 / 보건소 2.0 / 보관함 1.2 / 정산기 1.6 / 전진기지 2.0초.
- 포탈 회전: 8초에 한 바퀴(`PortalHoleSpin.DefaultSecondsPerTurn`), 소용돌이 알파 0.25, 마스크 반지름 = 홀 반지름(폭×0.36)×0.94, 시작 각도 시설마다 무작위.
- PM 조정 숫자 위치: `FacilityIdleAnimation` 상단 상수(인스펙터 노출 포함)와 `FacilityIdleAnchors`(길이·자리·크기).

## 연출 자리(보이는 영역 비율, x 왼→오 / y 아래→위)
| 시설 | 자리 | 비고 |
|---|---|---|
| 조명 | 등불 (0.76, 0.80) | 문서 값 사용. 덧그림 폭 0.34 |
| 충전기 | 빛줄 (0.45, 0.87), 번개 화면 (0.55, 0.54) | 문서 값 |
| 보건소 | 탑 꼭대기 (0.50, 0.99), 십자 (0.50, 0.72) | 문서 값 |
| 보관함 | 이음선 y **0.572** | PNG를 보고 잰 값(문서 0.60은 오렌지 끝 모서리를 가로질러서 0.572로 조정). 경첩은 보이는 왼쪽 아래 |
| 정산기 | 화면 (0.50, 0.70), 화면 폭 0.64 | 금화 최대 = 화면 폭 45% |
| 전진기지 | 돔 (0.50, 0.97), 중심 (0.50, 0.40), LED 12곳 | LED 좌표는 PNG를 눈으로 쟀다(`OutpostLeds`) |
| 포탈 | 홀 (0.50, 0.52), 반지름 0.36 | |

## 판정·규칙
- 켜짐: `PowerFacility.IsInteractionAvailable || IsInSupplyRange || IsInVisualSupplyRange`(연출 전용 값, `GameplayEventBridge`가 모든 시설에 채움. 조명·보관함은 `IsInSupplyRange`가 안 채워져 이 값이 필요했다). 전진기지는 늘 켜짐. 포탈은 `EmergencyEscapePortal.IsPowered`(전력망 연결)와 웜홀 그림 여부.
- 건너뛰는 경우: 전력 없음, 건설 연출 재생 중, 켜짐 연출 재생 중, 시설 비활성, 옮기기 선택 중(`BuildingPlacementSystem.MovingInstance`), 게임 정지(`Time.deltaTime`=0). 철거·비활성은 `OnDisable`/`OnDestroy`에서 즉시 정리.
- 줄인 움직임에서도 주기 연출과 포탈 회전은 유지(건설 연출만 기존대로 생략).
- `LightHalo` 연결: Building이 시설 루트에 `FacilityVisualDim`(Power 어셈블리)을 붙여 배율만 쓰고, `LightHaloVisual`(Hazards)이 16프레임마다 한 번 찾아 알파에 곱한다. `GasVisionClearanceSource`는 값을 읽지 않으며 반지름·`IsCleared`·`LightLevel`은 테스트로 불변 확인.
- 보관함은 Artwork를 몸체·뚜껑 `Sprite.Create` 조각으로 대신하고 원본은 `forceRenderingOff`로 가렸다가 끝나면 조각 Sprite를 Destroy한다.
- 포탈 홀 오브젝트는 켜짐 상태에서 처음 필요할 때 한 번 만든다(건설 연출이 가리는 렌더러 목록에 끼지 않게). 마스크는 정렬 구간을 제한한 `SpriteMask`.

## 테스트 결과
- 새 EditMode 테스트 + `LightVisibilityTests`(후광 배율): **37개 통과, 실패 0**(`FacilityIdleAnimationTests`, `LightVisibilityTests` 필터로 실행).
- 전체 EditMode 1회 실행: Pass 3030 / Fail 38. 실패는 UI·로컬라이징·씬 지형·`BuildingPlacementSystem.CanPlaceLocationAt` NRE·보건소/포탈 지오메트리 등이며 이번 변경 경로(연출 컴포넌트)와 무관해 보이나, **변경 전 기준(baseline) 비교는 하지 않았다(미검증)**.
- 컴파일 오류 없음(Unity 콘솔 Error 0 확인).
- 에디터에서 6종을 연출 중간 시점으로 렌더한 캡처: `work_process/images/facility-idle-20261125/idle-sheet-editmode-render.png`(하트=보건소 탑 위, 와이파이·고리=전진기지, 뚜껑 들림=보관함 확인. 노란 원은 Bootstrap 씬에 있는 별도 오브젝트가 임시 카메라에 찍힌 것).

## 미검증(실제 플레이 필요)
Bootstrap→광산 실제 플레이에서의 시각 확인 전부: 조명 깜빡임이 등불 위에 보이는지, 충전기 정전기 위치, 정산기 금화 크기·화면 안 위치, 포탈 홀 회전(링 테두리 가림 여부, 다른 `SpriteMask`와의 간섭), Esc 일시정지, 20개 이상 설치 시 프레임, 저장·이어하기, `idlefx` 명령 실사용, 1분 관찰 주기 확인.

## 2차 수정 (사용자 플레이 피드백, 2026-11-26)
| 항목 | 변경 |
|---|---|
| 충전기 | 연출이 안 보였던 원인은 정전기 두께 1~2픽셀·옅은 청록 glow. 지그재그 번개 스프라이트(`FacilityIdleSprites.Bolt`, 폭 0.2·높이 0.32 시설 폭)와 흰색 코어 glow, 번개 화면 glow(0.6폭)로 키움 |
| 전진기지 | 바깥으로 퍼지는 고리 최대 크기 1.0 → 1.25(시설 보이는 폭 대비) |
| 포탈 | 소용돌이 무늬 삭제. 웜홀 복사본만 시계방향(z 음수)으로 8초에 한 바퀴. 홀 중심을 PortalInactive PNG 투명 영역에서 실측해 (0.50, 0.47), 반지름 0.368로 수정(문서 0.52는 높았음). 마스크 정렬 구간 하한 버그 수정(아래 lesson) |
| 보관함 | 경첩을 보이는 영역 오른쪽 아래로 옮기고 시계방향 14° + 0.05칸 들림, 열릴수록 뚜껑 세로 7% 줄여 원근감, 왼쪽 틈에 어두운 안쪽·청록 glow·가는 빛줄 |
| 정산기 | Aseprite로 그린 도트 시트(`Resources/Facilities/SettlementCoinSheet.png`, 16×16 셀: 구리·철·리튬 원석, 변환 섬광 3프레임, 금화 회전 4프레임 + 반짝임). 화면 안쪽을 어두운 청록 패널로 덮고 원석 3개가 톡 튀어나와 섬광과 함께 8bit 금화로 바뀌고 통통 뛰며 회전하다 깜빡이며 사라짐 |

- 도트 원본: `work_process/art-src/settlement_coin_sheet.aseprite`, 생성 스크립트 `settlement_coin_sheet.lua`(Aseprite 1.3.18 `--batch --script`). Chickxel은 GUI 전용이라 CLI가 되는 Aseprite를 썼다.
- 확인: Play Mode에서 실제 게임 카메라로 렌더한 캡처 `play-charger-settlement-portal.png`, `play-storage-outpost-portal-rotation.png`. 포탈은 0°/90°/180° 프레임 간 픽셀 평균 차이가 0.05/0.03으로 회전이 실제로 보인다(수정 전 0.0000).
- 테스트: 새 EditMode 테스트 포함 3034 통과, 기존 실패 38개(baseline과 동일). Play Mode 세션 직후 전체 실행은 정적 상태 오염으로 실패가 124개까지 늘었다가 `RequestScriptReload` 뒤 38개로 돌아왔다.

## 사용자 테스트 가이드
문서 `prompt-B-214.md`의 "테스트 (플레이어 시점)" 1~12번과 같다. `idlefx`(목록) / `idlefx now [light|charger|clinic|storage|settlement|outpost]` / `idlefx period <초|reset>`.

## 완료 체크리스트
[ ] `MVP2-fix158` push 확인(미실행) [ ] 커밋 해시 기재(미실행) [x] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음(새 테스트는 통과, 기존 38개 실패는 baseline 미확인) [ ] 6종 주기 연출이 표대로 나옴(플레이 미검증) [x] 같은 시설끼리 엇갈림(테스트) [x] 꺼진 시설은 주기 연출 없음(테스트) [x] 포탈 켜짐에서만 회전·꺼짐 정지(테스트) [x] 건설·켜짐 연출과 겹치지 않음(테스트) [x] 끊김·이동·철거 시 정리(테스트) [ ] `idlefx` 동작(플레이 미검증) [x] 가스 시야 판정 불변(테스트) [x] 줄인 움직임에서도 유지(테스트) [x] 새 SFX 없음 [x] PNG·칸 수·전력 범위 불변 [x] Gameplay→App 참조 없음 [x] 프리팹 YAML 손수정 없음 [x] main 직접 커밋 없음 [x] PR 안 만듦 [x] 브랜치 삭제 안 함

## 3차 수정 (사용자 요청, 2026-11-27)
| 항목 | 변경 |
|---|---|
| 보관함 | 기존 뚜껑 조각 연출 삭제. 사용자 영상 `work_process/video/storage_open.mp4`(1168×784, 24fps, 6초, 검은 배경)를 배경 제거 후 프레임 시트로 재생. 영상 상자를 StorageGrounded에 닮음변환으로 맞춤(배율 1.305, 평균 색차 4.4/255). 상자 실루엣(밝기>55 덩어리 채움 + 검은 외곽선 12.5px 확장)만 남기고 주황 빛번짐·검은 배경 제거. 영상 18~128프레임을 12fps로 56장, 셀 276×212(그림 픽셀의 0.25배) → `Resources/Facilities/StorageOpenSheet.png`(1960×1728, BC7). 첫 프레임이 원래 그림과 같은 자리라 Artwork를 가리고 같은 정렬 값에서 바로 바꿔 끼운다. 길이 4.67초 |
| 충전기 | 원인: 기존 연출은 재생되고 있었지만 차양 위 흰 빛줄·작은 번개가 충전기 그림의 청록·흰 부분과 겹쳐 1.2초 동안 거의 구분되지 않았다(Play 캡처로 확인). 충전 완료창 번개(`upgrade-icon-bolt.png`)가 차양 위에 속이 빈 채로 톡 나타나 둘레에 청록 정전기가 튀는 동안 아래에서 위로 청록 액체가 차올라 원래 번개로 돌아오고, 가득 찰 때 정전기가 사방으로 터지며 고리가 퍼진다. 번개 화면도 차오르는 만큼 빛난다. 2.6초 |
| 보건소 | 기존 픽셀 하트 회전 삭제. 회복창 하트(`upgrade-icon-heart.png`)가 탑 위에 빈 채로 나타나 두근(쿵-쿵 ×2)거리며 붉은 액체가 아래에서 위로 차오르고, 둘레에서 회복 십자(+)가 피어오르며 십자 간판이 박동에 맞춰 빛난다. 가득 차면 붉은 고리. 2.8초 |

- 아이콘 차오름은 미리 구운 25프레임 시트(0 빈 껍데기, 1~23 물결 수면·거품과 함께 차오름, 24 원래 아이콘): `Resources/Facilities/ChargerIdleIconSheet.png`, `ClinicIdleIconSheet.png`(각 820×820, 셀 160 + 2px 여백). 아이콘 크기 보이는 폭의 0.34, 꼭대기 위 0.42칸에서 0.28칸 떠오름.
- 생성 스크립트: `work_process/art-src/facility-idle-20261127/`(`common.py`·`build.py` 보관함, `icon_fill_bake.py` 아이콘, `regutter.py` 셀 여백). 원본 아이콘·영상은 수정하지 않았다.
- 코드: `FacilityIdleEffects.cs`(`IconFillIdleFx` 공통 + Charger/Clinic, Storage 교체), `FacilityIdleAnchors.cs`(길이·자리·시트 메타, `IconFillLevel/IconFillFrame/StorageOpenFrame` 순수 함수), `FacilityIdleSprites.cs`(시트 로더, `Plus`, 코드 하트 삭제). 새 SFX 없음, 프리팹·씬 수정 없음.
- 테스트: `FacilityIdleAnimationTests` 33개 통과(보관함 영상 프레임·첫 프레임 자리, 충전기/보건소 빈→가득 순서, 시트 로드 추가, 뚜껑 테스트 교체).
- Play 확인: Bootstrap→광산에서 세 시설을 띄워 시점별 캡처 `work_process/images/facility-idle-20261125/play-storage-charger-clinic-v3.png`. **미검증**: 실제 30초 주기로 자연스럽게 나오는 모습의 장시간 관찰, 여러 해상도에서 아이콘 크기 체감, 보관함 영상 BC7 압축 화질 근접 확인.
