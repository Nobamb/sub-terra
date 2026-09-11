# prompt-B 96 작업 결과

## 변경 내용

- 미니맵 지형 표시를 반대로 적용했다. **남아 있는 타일**만 밝은 네모 도트로 그리고, **제거된 칸은 그리지 않는다**.
- 이전에는 채굴된 칸을 밝은 도트로 남기고, 남은 지형은 배경에 가까운 어두운 색이라 사실상 안 보였다.
- 화면 안의 셀만 스캔하는 방식은 그대로다. 카메라 이동·채굴 직후 Tilemap의 `HasTile`을 기준으로 갱신한다.

## 파일

`sub-terra/Assets/_Project/` 기준:

- `Scripts/App/Integration/ExplorationMinimap.cs`: 남은 타일만 `RemainingTileDot` 색 도트로 표시.
- `Tests/EditMode/App/UI/PromptB94MinimapTests.cs`: 남은 타일 표시·채굴 후 숨김 검증.
- `Editor/DataValidation/PromptB96MinimapTestRunner.cs`: EditMode 전용 실행 메뉴·플래그.

## 검증 및 연결

- Unity EditMode 10개 통과 (`Pass: 10 Fail: 0`). 결과: `sub-terra/Temp/prompt-b96-editmode-results.txt`.
- Unity 컴파일 오류 없음. Console Error 없음.
- Scene, Prefab, 폰트 에셋은 변경하지 않았다.
- 게임 세이브 형식은 변경하지 않는다. Inspector 추가 배선은 없다.
- Bootstrap부터 실제 플레이 완주 및 Windows 빌드 검증은 수행하지 않았다.
- 커밋·배포는 수행하지 않았다.
