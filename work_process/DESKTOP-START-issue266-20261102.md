# 데스크탑 시작점 — #266 여자 캐릭터 / 흰 잔점 미해결

## 가장 먼저 읽을 상태

- **흰 잔점 문제는 미해결이다.** 사용자는 최신 GIF에서도 계속 보인다고 확인했다. '정리 완료'라고 다시 보고하지 않는다.
- 작업 브랜치 `codex/issue-266-character-selection`, 기존 PR https://github.com/Nobamb/sub-terra/pull/270. 새 PR을 만들지 않고 여기서 후속 작업한다. main 직접 푸시/PR 병합은 하지 않는다.
- 여자 원본 외형은 **치마**다. 짧은 바지/긴 바지로 바꾸지 않는다. 다리가 치마를 관통하지 않도록 천의 가림 순서를 유지한다. 불투명 원단과 안쪽 가림으로 노출을 방지하고 외부 빛/형광 후광을 넣지 않는다.
- 리깅은 사용자가 거부해 중단했다. 별도 부위 생성·관절 조립을 재개하지 않는다. 기존 여자 전신5자세가 기준이며 남자 모션은 동작 참고다.
- 실제 게임/Unity Assets/씬/프리팹/저장 코드는 아직 변경하지 않았다. 새 PNG/GIF는 검토 후보이며 게임 적용 완료가 아니다.

## 데스크탑에서 받기

기존 데스크탑 저장소 루트의 PowerShell에서 현재 변경 사항을 먼저 확인한다.

```powershell
git status --short
git fetch origin
git switch codex/issue-266-character-selection
git pull --ff-only origin codex/issue-266-character-selection
git rev-parse --show-toplevel
git log -1 --oneline
```

작업 중인 변경 때문에 전환/fast-forward가 거부되면 강제 reset/clean하지 말고 먼저 보존한다. Unity 프로젝트는 저장소 루트가 아니라 그 안의 `sub-terra/`다. 원화/GIF 문제 분석에는 Unity 실행이나 Windows 재빌드가 필요하지 않다. 현재 브랜치와 실제 여는 프로젝트 경로를 먼저 확인한다.

## 비교할 파일

경로는 저장소 루트 기준이다.

| 파일 | 의미 |
|---|---|
| `work_process/references/issue266/white-speck-liked-sheet.png` | 사용자가 '5컷 너무 좋다'고 보낸 원본 시트 그대로. 원화 버전 혼동을 피하기 위한 기준 |
| `work_process/references/issue266/white-speck-reported.gif` | 같은 메시지에서 흰 잔점을 제보한 실제 GIF 그대로 |
| `work_process/images/issue266/female-ladder-skirt-v2.png` | 치마 변경 초기 결과. 관통 표현 지적이 있었음 |
| `work_process/images/issue266/female-ladder-skirt-v3.png` / `v4.png` | 겹침 보정 중간 이력. 특히 v4는 배경 얼룩이 있어 미채택 |
| `work_process/images/issue266/female-ladder-skirt-v5-clean.png` | 치마 가림 보정 및 배경 추출 시도 원화. 흰 잔점 해결 판정 아님 |
| `work_process/images/issue266/female-ladder-skirt-v5-review/` | v5 PNG5장 및 종전 GIF |
| `work_process/images/issue266/female-ladder-skirt-v5-clean-preview/` | 무디더링 GIF, 무손실 WebP, 이전/새 GIF의 디코딩 첫 프레임 비교 |
| `work_process/images/issue266/female-ladder-skirt-v6-opaque-review.png` 및 `female-ladder-skirt-v6-review/ladder-review.gif` | 흰 잔점 정리 요청으로 다시 편집한 불투명 배경 검토본. 사용자에게 여전히 거슬리므로 미채택. 게임용 투명 PNG가 아님 |
| `work_process/images/issue266/female-ladder-skirt-v5-runtime-candidates/` | 1254×1254 게임용 크기 후보5장. 이름에 approved가 있어도 승인/게임 적용됐다는 뜻 아님 |

## 시도했지만 해결되지 않은 방법

**확인된 버전 차이:** 첨부한 '좋다고 한5컷'은 v3 PNG와 SHA-256이 같고, 함께 제보한 GIF는 v5 GIF와 SHA-256이 같다. 둘은 같은 원화 버전이 아니다. 이 차이가 흰 점 원인이라는 뜻은 아니지만, 먼저 팀원이 원하는 기준 원화를 확정하고 동일 버전에서 PNG/GIF를 비교해야 한다.

1. GIF sierra2_4a 오차 확산을 제거하고 전체 공통256색 팔레트로 변환했다. 디더링 점무늬는 줄었지만 **사용자가 말한 흰 점 해결은 아니다**.
2. 무손실 WebP도 생성했다. 원화 색 손실 비교용이며 원화 자체의 결함을 고치지 않는다.
3. 배경 알파 표본0, 흰색/검정 배경 합성을 확인했다. 표본 몇 개로 전체 테두리/내부의 잘못된 알파나 흰 픽셀 부재를 증명하지 못한다.
4. 불투명 배경의 v6 편집본도 만들었으나 사용자는 여전히 흰 점이 보인다고 했다. **불투명 배경으로 바꾸는 것만으로 해결됐다고 하지 않는다.**

현재 원인은 미확정이다. 실제 제보 GIF의 특정 흰 점이 디더링/테두리 매트/잘못된 알파/원화에 그려진 하이라이트 중 무엇인지 동일 위치·동일 프레임으로 확인해야 한다. '투명화 흔적일 것'이라는 사용자 설명과 Agent 추정을 확정 원인으로 바꾸지 않는다.

## 다음 작업 순서

1. 팀원과 흰 점 한두 곳의 프레임/좌표를 먼저 맞춘다. 제보 GIF → 해당 원본 PNG → 알파 채널 → 무손실 합성을 같은 위치에서 확대 비교한다. 표시 위치가 확정되기 전에 전체 캐릭터를 또 생성하지 않는다.
2. 원본 시트는 별도로 보존한다. 결함 픽셀만 최소 수정하고 치마 가림·체형·얼굴·장비·5자세를 유지한다. 실제 지적 위치에서 사라진 것을 비교한 뒤 결과를 보여준다. 원래 흰 반사광까지 사용자가 없애려는 것인지 필요하면 해당 위치로 확인한다.
3. 같은 1→2→3→2→1→4→5→4 순서의 GIF와 무손실 비교본을 만든다. 사용자가 문제 삼은 흰 점이 사라지고 외곽/치마 관통/노출이 새로 생기지 않는지 확인한다. 파일 개수/해시/샘플 알파 검사만으로 시각 합격이라 하지 않는다.
4. 같은 PR #270에 최신 결과와 한계를 갱신하고 팀원 리뷰를 받는다. 진행 동의 전 걷기·채굴 등 대량 제작은 보류한다.
5. 동의 후 걷기40장과 채굴24장 연결을 각각 GIF로 먼저 공유한다. 추가 모션→게임 크기/손발 접점→선택/슬롯 저장은 완성 원화가 확보된 뒤 진행한다.

## 도구와 재현

- `work_process/tools/female-original-outfit-ladder-preview-20261102.ps1`: Source/OutputDirectory 매개변수로 시트의5전신 셀을 분리하고 같은8단계 순서 GIF를 생성. 현재 무디더링/공통 팔레트 설정이다.
- `work_process/tools/female-approved-ladder-export-20261102.ps1`: SourceDirectory/OutputDirectory로5장을 같은 전신 균일 배율과 이동만 적용해 게임 크기로 준비. 부위별 변형 없음.
- 프롬프트와 시도 기록: `work_process/female-ladder-skirt-v*-20261102.txt` 계열, `issue266-skirt-correction-20261102.md`, `issue266-skirt-occlusion-fix-20261102.md`, `issue266-gif-cleanup-20261102.md`, `issue266-white-speck-review-20261102.md`.
- 노트북의 다른 리깅/40장 실패 실험 및 수정 중이던 예전 걷기 기록은 이번 인계 커밋에 포함하지 않는다. 그 로컬 자료가 데스크탑에 있다고 가정하지 않는다. 새 스킬/패키지/ProjectSettings 변경도 필요하지 않다.

노트북에서는 작업 종료. **미검증:** 원인 확정, 흰 점 제거, 실제 게임 적용/사다리 접점·전환, 나머지 모션과 선택·저장.
