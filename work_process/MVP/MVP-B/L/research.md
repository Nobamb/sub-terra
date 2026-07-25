# Phase L — Main Menu와 Surface Base

## 1. 개요

`process-B`의 B-11을 구현한다. 새 게임·이어하기·슬롯·설정·종료를 Main Menu에, 판매·제작·업그레이드·목표·탐사 진입을 Surface Base에 조립한다.

## 2. 작업 목표

- 세이브 유무와 유효성에 따라 이어하기 상태를 표시한다.
- 새 게임이 기존 슬롯을 실수로 덮지 않도록 명시적 선택/확인을 둔다.
- Surface Base의 경제와 진행 UI가 기존 Service를 재사용한다.
- 탐사 시작 시 State를 준비하고 Integration/Mine Scene으로 전환한다.

## 3. 구현 범위

- `Scenes/MainMenu/MainMenu.unity`, `Scenes/SurfaceBase/SurfaceBase.unity`
- `Scripts/App/UI/MainMenu/`, `Scripts/App/UI/SurfaceBase/`, 설정 UI
- `Prefabs/UI/SaveSlotPanel.prefab`, 필요한 B 소유 메뉴 Prefab
- `Tests/PlayMode/Integration/Menu/`

## 4. 권장 구현 방향

1. Main Menu의 상태 모델을 선택 슬롯, 슬롯 메타데이터, 계속 가능 여부, 로드 오류, 게임 버전으로 정의한다.
2. 새 게임은 슬롯 선택 → 기존 세이브 확인 → 필요 시 명시적 덮어쓰기 확인 → 새 State 생성 → 첫 저장/SurfaceBase 또는 Mine 진입 순서로 처리한다.
3. 이어하기는 Phase K 검증 결과가 성공 또는 backup 복구 가능일 때만 활성화한다. 손상 이유를 사용자에게 설명하고 무조건 새 게임으로 덮지 않는다.
4. 설정은 MVP에 필요한 오디오/해상도 등 실제 지원 항목만 두고 적용·취소·기본값 경로를 만든다.
5. 종료는 저장 dirty 상태와 진행 중 저장을 확인한 뒤 플랫폼에 맞는 종료 Service를 호출한다. Editor 전용 종료 처리를 분리한다.
6. Surface Base 판매·제작·업그레이드는 Phase E/F presenter 또는 Service를 재사용하고 별도 경제 로직을 만들지 않는다.
7. 목표, 최근 탐사 결과와 심층 잠금은 ProgressState 읽기 모델에서 표시하며 잠긴 이유를 함께 보여준다.
8. 탐사 시작은 선택 슬롯/RunState를 준비하고 SceneLoader로 Mine Integration Scene을 연다. 버튼 연타로 중복 Scene 로드하지 않게 한다.
9. 각 Scene의 UI가 `OnEnable/OnDisable` 구독 수명을 지키고 EventSystem이 중복되지 않는지 확인한다.

## 5. 보안 및 안정성 기준

- 세이브 삭제/덮어쓰기는 정확한 슬롯과 사용자 확인 없이는 수행하지 않는다.
- 슬롯 표시명으로 파일 경로를 직접 조합하지 않는다.
- UI는 골드, 재고, 레벨을 직접 바꾸지 않는다.
- 로드 실패 후 기존 파일을 보존하고 오류 상태에서 탐사 Scene으로 진입하지 않는다.

## 6. 완료 기준

- 새 게임과 정상/backup 이어하기가 의도한 슬롯에서 작동한다.
- 세이브가 없거나 복구 불가능하면 이어하기가 비활성/오류 표시된다.
- Surface Base에서 판매·제작·업그레이드와 잠금 표시가 작동한다.
- 탐사 시작 시 중복 로드 없이 Integration Scene으로 이동한다.
