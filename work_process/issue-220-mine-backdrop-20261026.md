# #220 광산 배경 이동 — 2026-10-26

## 기준과 분리

- origin/main `8305ee2c` 기반 `codex/issue-220-mine-parallax-background`. Unity 6000.5.4f1, 저장소 `C:/Users/jeone/sub-terra`, 첫 Hub 프로젝트 `C:/Users/jeone/sub-terra/sub-terra`.
- 직전 #241은 커밋 `62316959`, PR #248(https://github.com/Nobamb/sub-terra/pull/248)에 게시했다. #220은 main 기반 독립 작업이므로 아직 병합되지 않은 #248의 창 연출이나 #242 걷기가 여기서 함께 보이는 것은 아니다. 해당 PR은 각 브랜치에서 확인한다.
- #211/#240/#245는 각각 PR #233/#247/#246에 이미 구현돼 재작업하지 않는다. #243은 완료·닫힘, #158은 팀원 효과음 범위를 유지한다. #188은 기존 사용자 보류를 유지하고 정식 버전 전용 이슈도 확장하지 않는다.

## 이슈 범위와 구현

- 어두운 동굴 배경 한 장을 광산의 지형·시설·구조 표시 뒤에 배치한다. 원본 BackgroundTilemap은 비어 있고, 이 작업은 해당 타일맵을 수정하거나 충돌/채굴 대상으로 쓰지 않는다.
- 플레이어 X/Y에서 이미지 안의 표시 영역을 계산한다. 깊어지면 아랫부분, 좌우로 움직이면 같은 쪽이 천천히 보인다. 화면 비율과 원본 비율을 함께 적용해 그림을 늘이지 않는다.
- Resources/MineBackdropSettings.asset에 이미지, tint, 깊이/가로 이동 범위, 화면에 보이는 이미지 비율, 가장자리 여백, 보간 시간을 노출했다. 기본 가로/깊이 전체 이동 범위는 각각 160 월드 단위, 이미지 높이의 22% 표시, 네 변 5% 여백, 보간 0.25초다. 현재 40m와 좌우 끝에서도 이미지 끝까지 사용하지 않으며 앞으로 설정을 조정할 수 있다.
- 카메라의 위치·확대·화면 비율은 배경 판에 매 프레임 반영하고, 표시 영역 이동만 지수 보간한다. 순간이동으로 큰 좌표 변화가 생겨도 텍스처를 교체하거나 빈 영역을 그리지 않는다. 모션 감소에서는 표시 영역을 지표 기준으로 고정한다.
- IntegrationRuntimeBinder.WireContracts에서 광산 카메라·플레이어에 한 번 연결한다. GameObject/Mesh/Material은 씬 수명으로 생성·재사용하고 명시적으로 정리한다. DontSave로 임시 오브젝트의 씬 저장을 막고, 렌더는 카메라 뒤쪽·동일 정렬 레이어의 최하단(-32000)에 둔다. 매 프레임 텍스처 생성·검색·할당은 없다.
- 기존 암전 마스크가 배경 위에도 적용된다. 밝기·암전 반경·자원 표시·UI·채굴·저장 규칙은 변경하지 않았다. 지상 배경은 이번 범위가 아니다.

## 파일과 에셋 연결

- Scripts/App/UI/MineBackdropSettings.cs: 표시 영역 설정과 경계/화면 비율 계산.
- Scripts/App/Integration/MineBackdropController.cs: 카메라 배경 렌더 및 보간·수명 관리.
- Scripts/App/Integration/IntegrationRuntimeBinder.cs: 광산 진입 시 자동 연결 1곳.
- Resources/MineBackdrop.shader, MineBackdropSettings.asset: URP 2D 단일 배경 패스와 런타임 설정.
- Art/Backgrounds/mine-cavern-backdrop.png: 1024×1536 동굴 배경. 무채색·낮은 대비로 제작하고 tint로 추가 감광한다.
- Editor/MineBackdropAssetBuilder.cs: 이 이미지 가져오기 설정과 이 설정 에셋만 생성/갱신하는 도구. Unity가 .meta를 생성했다. 추가 Inspector 연결이나 씬/프리팹 저장이 필요하지 않다.
- Tests/EditMode/App/UI/MineBackdropViewportTests.cs, Tests/PlayMode/Integration/Bootstrap/Issue220MineBackdropTests.cs: 범위·비율·이동·보간·중복 생성·정리 검사.

## 그림 제작

- 내장 image_gen 도구로 새 배경 1장을 생성했다. 외부 다운로드 에셋이나 API 키를 사용하지 않았다. 참고는 프로젝트 ground_normal_01.png의 바위 화풍이며, 원본 타일은 수정하지 않았다.
- 사용 프롬프트는 `work_process/issue-220-mine-backdrop-prompt-20261026.txt`에 보존했다. 생성 그림을 작업 공간 Art/Backgrounds에 복사하고 해당 경로에 연결했다.
- 원본 이미지 자체는 확인했으나 실제 플레이 화면에서 광부·타일과 합성한 결과의 승인을 뜻하지 않는다.

## 검사

- Editor 전용 빌더 실행 성공, 종료 코드 0. 이미지 mipmap/readable 끄기, bilinear/clamp, max size 2048, 압축 가져오기 및 설정의 이미지 참조 확인.
- 첫 실행 EditMode 4/4 통과(1.40초), PlayMode 1/1 통과(0.13초), 실패·건너뜀 0, 종료 코드 0. 가로/초광각/세로 비율과 미래 맵 여백·이동 방향, 카메라 덮기·순간이동 보간·모션 감소·단일 렌더러·입력/충돌 비관여·활성/비활성만 선택했다. C# 컴파일 오류/셰이더 오류 로그 없음. 에디터 RelayService의 TaskCanceledException 연결 로그는 있으나 선택 검사 실패는 없었다.
- 결과 경로: work_process/tools/issue220-backdrop-check-20261026/. 전체 Demo·사용자 슬롯·Windows 빌드·성능 측정은 실행하지 않는다.

## 미검증 — 수동 체크리스트

- [ ] Bootstrap → 새 게임/이어하기 → 탐사에서 실제 배경이 지형 뒤에 보이는지.
- [ ] 0m/40m 및 좌우 끝에서 이동 방향이 맞고 이미지 경계나 빈 공간이 나타나지 않는지.
- [ ] 엘리베이터·긴급 탈출·카메라 목적지 미리보기에서 깜빡임 없이 이어지는지.
- [ ] 구리/철/가스/구조 표시와 시설이 구분되고 기존 암전이 유지되는지. 배경이 과하면 tint 또는 visibleImageHeight만 조정한다.
- [ ] 초광각·창 크기 변경, 모션 감소 설정에서 정상인지.
- 실제 화면 확인 전에는 이슈 전체를 완료로 보고하지 않는다. 사용자의 커밋·푸시·PR 게시 승인을 받았으며, 별도 PR에서 팀원 리뷰를 기다린다. PR에도 위 화면 미검증 항목을 그대로 명시한다. ProjectSettings·Packages·기존 씬/프리팹/폰트/세이브 데이터 변경 없음.
