# #126 드릴 끝 청록빛 충전

## 작업 기준
- 최신 main `cdac217c`에서 새 브랜치 `codex/issue-126-drill-tip-charge` 생성.
- 첫 Hub 프로젝트 `C:/Users/jeone/sub-terra/sub-terra`, Unity 6000.5.4f1.
- 10/28 확인 시 #254·#255·#256은 열린 PR이며 아직 main 미병합. 해당 기능을 다시 구현하지 않음.
- #252와 효과음 B-169는 main에 반영됨. 팀원 효과음 구현은 변경하지 않음.
- #126의 10/27 갱신 요구: 드릴 회전 중 끝의 모양을 따라 청록빛이 서서히 차오르고 놓으면 회전 감속과 함께 사라짐. 점 불꽃·회색 파편 추가 안 함.

## 구현
- GameplayCursorModel: 회전 시작 속도로 곧바로 뛰지 않는 별도 DrillCharge(0~1). 가속/감속 설정으로 점진적으로 회전 세기를 따라감. 집게 전환 시 즉시 0.
- GameplayCursorController: 드릴 끝에 해당하는 원뿔 그라데이션을 한 번 생성해 재사용. 원본 Sprite 알파를 UI Mask로 사용해 그림 바깥으로 새지 않도록 클리핑.
- 빛을 Visual의 자식으로 두어 드릴 떨림·피벗·크기와 함께 움직임. 클릭 핫스팟은 변경 없음.
- 모든 Image는 raycastTarget=false, 기존 Canvas도 입력 비차단 유지.
- Bootstrap의 공용 커서를 그대로 사용하므로 메인 메뉴·지상·광산에 같은 로직 적용. Time.unscaledDeltaTime 유지.
- 기존 집게 호버 빛·파티클과 이동 기능은 그대로 유지.
- 변경 파일: GameplayCursorController.cs, GameplayCursorModel.cs, PromptB151GameplayCursorTests.cs, 이 기록.
- 원화·씬·프리팹·설정·패키지 변경 없음. 새 Inspector 연결이나 에셋 빌더 실행 불필요.

## 최소 검사
- PromptB151GameplayCursorTests: 새 충전 가속/감속, 드릴 자식 정렬/원뿔 분포/집게 전환, 기존 회전·핫스팟·입력 비차단·화면 활성화 검사.
- 결과: `work_process/tools/issue-126-tests`.
- 초기 커서 묶음 EditMode 13/13 통과(1.002초). 가속·감속·집게/파티클·마우스 핫스팟·레이캐스트 회귀 포함.
- 이후 짧은 클릭 해제 직후 더 밝아질 수 있는 경계 조건을 코드 검토에서 발견. 해제 후 빛의 목표를 0으로 수정하고 전용 검사에 추가.
- 후속 전용 결과: `work_process/tools/issue-126-final` (전체 묶음 재실행 대신 바뀐 2건만 실행).
- 후속 EditMode 2/2 통과(0.653초), 코드 컴파일 성공. 실제 화면/Windows 검증은 아래 미검증 목록으로 유지.
- `git diff --check` 통과. ProjectSettings/Packages 내용 변경 없음.

## 미검증 / 수동 확인
1. Bootstrap → 메인 메뉴에서 클릭 유지: 끝에 빛이 점진적으로 차오르는지, 놓으면 감속과 함께 꺼지는지 확인.
2. 지상·광산에서 같은 크기·모양·클릭 지점인지 확인.
3. 설정/팝업이 열린 상태에서도 자연스럽게 보이고 버튼 클릭을 막지 않는지 확인.
4. 시설 이동의 집게 모드에서 드릴 충전 빛이 남지 않는지 확인.
5. 실제 화면의 원뿔 윤곽·밝기와 Windows exe는 아직 미검증. 자동 검사만으로 시각적 완성으로 판단하지 않음.
- 사용자 승인 후 커밋·푸시하고 #126 전용 PR로 전달. 아래 실제 화면 확인 항목을 PR 본문에도 포함한다.
