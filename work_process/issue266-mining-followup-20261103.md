# #266 채굴 배경·그립·중간 자세 보정 검토본

## 범위와 현재 상태

- 브랜치 `codex/issue-266-character-selection`, 기존 PR #270 관련 후속 원화 작업. 후속 요청에서 커밋·푸시 승인을 받았으며, 최종 검토본·재현 도구·기록만 게시한다. 게임 적용 승인은 별도다.
- 여자 채굴의 핵심 6자세를 보정하고 새 전신 중간 자세 6장을 제작했다. 그중 과하게 웅크린 `middle-03`은 제외하여 최종 검토 재생은 핵심6 + 중간5 = **11자세**다. 팔다리 절단/변형/리깅 없음. 게임 Assets·코드·씬·프리팹 변경 없음.
- 하의는 연속된 밑단의 치마, 갈색 다리 덮개, 허벅지 장비를 유지한다. 1차 중간 시트의 치마 갈라짐/손잡이 이탈과 그립 편집 중 생긴 세 번째 손은 제외한 후속 원화를 사용한다.
- 사용자 답변으로 **코드를 이용한 배경·외곽 정리만 허용**받았다. 신체 조립이나 모션 변형 허용으로 확대 해석하지 않는다.

## 최종 검토용 입력·출력

모든 경로는 `work_process/` 기준이다.

- 핵심 자세의 불투명 흰 배경 마스터: `images/issue266/female-mining-v6-white-20261103.png`.
- 중간 자세의 불투명 흰 배경 마스터: `images/issue266/female-mining-inbetweens-v2-white-20261103.png`.
- 배경 정리한 핵심 6자세: `images/issue266/female-mining-v8-clean-20261103.png`.
- 배경 정리한 중간 6자세: `images/issue266/female-mining-inbetweens-v3-clean-20261103.png`.
- `images/issue266/female-mining-inbetween-review-20261103/`:
  - `key-01..06.png`, `middle-01/02/04/05/06.png`: 선택된 전신 셀11장(512×512). 재현 도구가 추가 생성하는 `middle-03`은 미채택이며 게시하지 않는다.
  - `contact-white.png`, `contact-black.png`: 선택된11자세의 실제 알파 합성 비교표. 마지막 칸은 빈칸이다.
  - `work.gif`: 작업 자세의 왕복 반복 검토. 중간 자세 포함10프레임, 8fps. `middle-03`은 사용하지 않는다.
  - `finish.gif`: 드릴을 들어 올리는 완료 동작, 끝 자세 정지. 8프레임, 6fps.
  - `work-lossless.png`, `finish-lossless.png`: 무손실 APNG. GIF 팔레트 변화 비교용.
  - `sequence.json`, `verification.txt`: 정확한 셀 순서와 최소 파일 검사 결과.
- `female-mining-followup-prompts-20261103.txt`: 내장 image_gen 제작/수정 프롬프트. 원화는 덮어쓰지 않고 버전별 보존했다.

## 배경 정리·검수

- `tools/MiningWhiteMatte-20261103.cs` 및 `tools/female-mining-alpha-clean-20261103.ps1`: 흰 배경의 연결 영역과 명시적으로 지정한 손잡이/팔 사이의 배경 구멍만 제거. 외곽의 흰 매트만 보정하고 분리된 외부 픽셀을 제거한다. 눈·렌즈·피부·반사광을 배경으로 일괄 지우지 않는다.
- `tools/female-mining-inbetween-review-20261103.ps1`: 전신 셀 추출·순서 배열·검토 영상·흰색/검정 합성. 이미지 겹치기/광학 보간으로 중간 자세를 만들지 않는다.
- 두 마스터와 배경 정리본 비교: **불투명 내부 RGB 변경0**, 셀 바깥4px 여백의 비투명 픽셀0. 선택된 원화11장 해시가 모두 다르다. 이 검사는 동작 자연스러움의 합격 판정이 아니다.
- 흰색/검은색 합성에서 종전 외부 얼룩과 손잡이 내부 흰 배경이 제거된 것을 확인했다. 손 두 개/치마 밑단/신발 외곽도 시트로 비교했다.
- GIF2개/APNG2개 디코딩 정상. 이 환경의 ffmpeg는 animated WebP를 읽지 못해 WebP 검증이 실패했고, 최종 비교본은 디코딩 확인한 APNG로 바꿨다. 남아 있는 `*-lossless.webp`는 이번 최종 전달물에 포함하지 않는다.

## 실제 재생 코드와의 차이 / 미검증

- `PlayerMiningAnimationSequence.cs`를 확인했다. 실제 게임은 시작→작업 반복→채굴 완료 들어올리기를 분리한다. 24장 전체를 무한 순환시키지 않는다. 따라서 작업/완료 미리보기도 나눴다.
- 위8fps/6fps는 **원화 비교용 느린 속도**다. 게임 설정의 시작20fps/작업12fps/복귀30fps/완료60fps를 바꾸지 않았다.
- **미검증:** 실제 화면의 반복 재생 인상, 기존24장 전체 대응, 머리 높이·몸통 원근·드릴 크기의 프레임 간 미세 차이, 게임 적용 크기·접점·실제 속도. 과한 웅크림이 확인된 세 번째 중간 자세는 최종 순서와 비교표에서 제외했으며 원본 시트에는 이력으로 남아 있다.
- 검토 포인트: 작업 반복에서 머리/발이 튀는지, 들어올리기의 드릴 회전과 손목이 자연스러운지, 같은 캐릭터·같은 드릴로 보이는지. 승인 전 게임에 적용하지 않는다.
- 기존 사다리 흰 잔점 문제는 이 채굴 작업과 별개이며 해결 판정을 내리지 않았다.

## 재현

원화 비교만 하려면 위 GIF/PNG를 열면 된다. 재생성에는 PowerShell/System.Drawing 및 로컬 ffmpeg가 필요하다. ffmpeg 실행 파일은 저장소에 게시하지 않았으며, 도구가 기대하는 경로는 `work_process/tools/video-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe`다. 실행 파일이 없는 데스크탑에서도 게시된 원화와 미리보기는 그대로 확인할 수 있다.

```powershell
& work_process/tools/female-mining-alpha-clean-20261103.ps1
& work_process/tools/female-mining-inbetween-review-20261103.ps1
```

다음 작업은 선택된11자세의 팀원 리뷰 후 수정할 자세를 특정하는 것이다. 프레임 수만 늘려 전체24장 완료로 표시하거나 다시 전체 원화를 무작정 생성하지 않는다.
