# 엔진 연료 데모 마감

기준: `origin/main` 034b896. 최초 작업 브랜치: `codex/demo-fuel-polish`. 최종 통합 브랜치: `codex/inventory-item-icons`, PR #128.

최초 검증 위치: `C:\Users\jeone\sub-terra\.worktrees\demo-fuel-polish`. 원래 사다리·폰트 변경을 보존하기 위해 별도로 작업했다. 오늘 최종 제출에는 검증된 UI·희귀 정산 보호·QA 변경을 원래 폴더와 PR #128 작업 폴더에 복사 통합했다. 현재 인벤토리 아이콘 4종을 유지하며 기존 1254px 연료 생성본을 덮어 적용하지 않는다. main에는 아직 병합하지 않았다.

## 변경

- `icon_engine_fuel.png`: 각성 문양 석재와 동일했던 이미지를 청록색 에너지 코어 캡슐로 교체. 기존 GUID와 단일 스프라이트 참조를 유지한다. 원본 생성본은 1254px RGBA이며 최종 게임용 파일은 256×256 RGBA다.
- 기존 icon meta에는 Int64 범위를 초과하는 `9333044155266377488` ID가 다중 스프라이트 테이블에 남아 있었다. 이 PNG는 Single 모드이므로 불필요한 다중 테이블을 비워 임포트 오류를 제거한다. 사용 중인 GUID와 `21300000` 단일 스프라이트 참조는 바꾸지 않는다.
- `MiningProgressHud`: 봉인 미해금 안내는 유지하고, 드릴 레벨 부족과 화물 초과 안내에 다음 행동을 추가한다. 실패 안내 동안 작은 진행률 패널을 최소 360×44로 확장하고 숨길 때 원래 크기로 복원한다.
- `EconomyPanelView`: 전체 판매에서 희귀 품목이 제외되며 엔진 연료는 개별 선택 판매한다는 안내를 거래 결과와 함께 계속 표시한다. 공식 판매 레이아웃 빌더도 같은 안내를 사용한다.
- `OutpostService`: 전진기지의 화물/보관함 일괄 정산에서 희귀 품목을 제외하고, 연료 개별 정산은 거부한다. 일반 광물의 기존 정산 규칙은 유지한다.
- `MVP2_WINDOWS_QA.md`: 희귀 제외 규칙과 102번 정상 입력 검수 절차를 추가한다.
- `DemoEngineFuelQaRunner`: CLI PlayMode 검증 동안만 Bootstrap 시작 씬 강제를 해제하고 종료/실패 시 기존 Editor 설정을 복원한다. 기본 실행은 채굴 및 실패 HUD 테스트이며 `-fuelQaTest`로 한 테스트를 지정할 수 있다. 게임 실행 경로는 바꾸지 않는다.

사다리 코드/아트, Scene, UI Prefab, 공용 폰트, 세이브 형식 및 가격 데이터는 이 작업의 수정 대상이 아니다. 판매 안내는 기존 `statusDetailText` 참조를 사용하므로 새 Inspector 배선이 필요하지 않다.

## 검증

- PNG 읽기 검사: RGBA, 알파 범위 0~255, 네 모서리 알파 0 확인.
- 최종 Unity EditMode: 46/46 통과(`final-editmode-results.xml`). 연료 판매/안내, 전진기지 정산, 저장 파일 왕복, 스캔 오버레이 및 채굴 정적 검증을 포함한다. 컴파일 오류와 연료 아이콘 meta 변환 오류가 없다. 기존 Unity API 사용 중단 경고는 남아 있다.
- PlayMode 첫 집중 실행: 25건 중 24건 통과. 실패는 기존 `E_F03_KeyboardAndMouse_UseTheSameTimedCompletionPath`의 건설 배치 중 Enter 채굴 시작 검증이다. main 원본 C# 코드로 동일 테스트를 단독 실행해 같은 실패를 재현했다(`baseline-input-results.xml`). 이번 기능의 회귀라고 볼 증거는 없지만, 실제 입력 정상 여부와 실패 원인은 별도 조사가 필요하다.
- 최종 HUD PlayMode: 2/2 통과(`final-hud-playmode-results.xml`). 실패 표시 시 360×44 확장, 실시간 만료 시 150×18 복원, 비활성화 후 재바인딩과 다음 실패에서도 원래 크기 보존을 확인했다.
- 저장 검증은 임시 파일과 두 칸의 테스트 월드를 사용한다. 광산 저장 및 지상 저장의 MineWorldFallback 경로를 검증하며, 실제 38m 타일 접근·채굴 루트 완주를 대신하지 않는다.
- 회귀 범위: 선택 판매 100G, Surface Base 판매 게이트, 전체 판매 희귀 보호, 전진기지 화물/보관함 보호, 광산/지상 파일 저장·복원 및 파괴 타일 복원, 스캔 종료/채굴 후 오버레이 제거, HUD 실패 표시 크기·수명.
- 수동 인수 검수: Bootstrap부터 정상 입력 40m 루트 완주, 실제 조명과 HUD 가독성, Windows 패키지 재실행, Unity 없는 별도 PC 검수는 별도 확인이 필요하다. 자동화 결과로 이를 통과했다고 간주하지 않는다.

## 아이콘 제작

내장 image_gen을 사용했다. 문양 석재는 화풍·팔레트 참고이며 편집 대상이 아니다. 생성 PNG의 알파를 그대로 보존했으며 새 픽셀 아트를 수동으로 덧그리지 않았다.

사용 프롬프트:

> Use case: stylized-concept. Asset type: Sub-Terra inventory game item icon, single square transparent PNG. Reference image is ONLY a palette and material/style reference: gray stone with luminous cyan geometric glyph. Create a distinct harvested ENGINE FUEL item, not a square stone block: a compact dark graphite cylindrical capsule with a bright cyan crystalline energy core visible through its central window, subtle angular cyan glyph accents, chunky readable silhouette. Cute polished 3D cartoon game art, bold dark outline, beveled edges, soft glossy highlights, three-quarter view. Center one item filling about 80% of square canvas, generous transparent margin. Genuine transparent alpha background, no backdrop, no ground shadow, no text, letters, labels, borders, numbers, extra objects or particles. Must remain clearly recognizable at 32px icon size. Keep detail simple.

교체 전 아이콘 PNG 및 meta 백업: `C:\Users\jeone\sub-terra\ladder_qa\demo_fuel_polish\before\`.

검증 로그 및 XML: `C:\Users\jeone\sub-terra\ladder_qa\demo_fuel_polish\`.

집중 PlayMode 실행 예시(프로젝트 경로는 위 작업 위치 사용):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe' `
  -batchmode -nographics -projectPath '<worktree>\sub-terra' `
  -executeMethod SubTerra.App.Editor.DataValidation.DemoEngineFuelQaRunner.RunPlayMode `
  -fuelQaResults '<검증 폴더>\playmode-results.xml' -logFile '<검증 폴더>\unity-playmode.log'
```

이 전용 실행에는 `-runTests`와 `-quit`를 추가하지 않는다. 완료 콜백이 XML과 종료 코드를 기록하고 설정을 복원한다. 기본 CLI 실행이 Bootstrap 시작 씬 강제 설정과 충돌해 MainMenu로 이동한 현상은 이 검증기로 회피한다.
