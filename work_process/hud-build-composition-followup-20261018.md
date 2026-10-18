# HUD 축소 누락 원인과 검토 빌드 — 2026-10-18

## 원인
- 사용자 화면의 큰 HUD/미션/시설창 및 펼쳐진 우측 메뉴는 축소 전 구성이었다.
- 이번 Windows 빌드20261018-095024는 PR192(f2f8f30b) 단독 구성이며 PR180의 UI 축소가 포함되지 않았다. 빌드 대상을 고를 때 기존 축소 반영 여부를 확인하지 못했다.
- GitHub PR175는 2026-10-15 병합 기록(98fa93e2)이 있지만, 현재 원격 main c4c89655와 PR192에는 그 커밋이 조상이 아니며 BasicHudView.ApplyCompactLayout도 없다. 현재 main에서 빠진 경위는 이 조사로 확정하지 않는다.
- 기존 PR180(codex/demo-visual-polish, da8226ac)에는 축소 코드와 에셋이 보존돼 있다. 같은 코드를 PR192에 중복 추가하거나 새 파생 PR을 만들지 않았다.

## 처리
- PR192 7cb88367 + PR180 da8226ac을 무충돌 임시 조합했다. Git index tree: 2f33f989c1e4a24b4c1b29c1d9bf7aa2fa97f1e5.
- 기존 축소 기준: HUD380×208, 미션460×116, 시설창 폭500, 우측 메뉴 기본 접힘. 게임 화면 픽셀과 달리 Canvas 기준 치수다.
- 조합 EditMode PromptB105SideMenuTests/PromptB106HudTests/PromptB107QuestUiTests/PromptB110BuildingMenuTests/StructuralFeedbackTests: **46/46 PASS**. 관련 XML/로그는 Builds/WindowsValidation-20261018/hud-combined.*.
- 검사 중 자동 변경된 씬/설정/증거 이미지는 로컬 백업 후 검사 전 index 기준으로 복원했다. 먼저 만든20261018-110110 대신 이 부수 변경을 제외한 **20261018-110530**을 최종 검토 후보로 사용한다.
- QA Windows x64 빌드20261018-110530 성공, Unity 정상 종료0. ZIP/DLL/Data/manifest 생성.
- ZIP SHA256: 5efd9a3c135da47de5d5c9047807276fbe0d869d65ae19c6663260b8da279cb9
- 빌드 후 임시 병합을 abort해 PR192 7cb88367로 복귀했다. 팀원의 PR/main을 최종 병합하지 않았다. 기존 미추적 작업 폴더/사용자 저장/씬 meta를 보존했고 추적 파일 잔여 diff는 없었다.

## 다음 실행 후보
- sub-terra/Builds/HudReview-20261018/HUD 축소 확인용.lnk
- 대상: sub-terra/Builds/Windows-x64/Qa/20261018-110530/sub-terra/sub-terra.exe
- 바로가기는 별도 HudReview-20261018/player-saves의 QA13/18 복사본만 사용한다. 실제 사용자 슬롯이나 기존 QA 원본은 변경하지 않는다.
- 동일 이름의 예전 실행 파일과 혼동하지 않도록 Build ID와 소스 조합을 함께 확인한다.

## 보류/팀원 전달
- 사용자 요청으로 나머지 Demo 플레이 검증을 중단했다. 새 검토 빌드는 실행하지 않았고 화면 육안/완주/이어하기/종료 경고 통과로 보고하지 않는다.
- 기존 QA는13/18. ComputeBuffer 경고는 수정 전·후 및 메인 메뉴만 실행한 종료에서도 남아 있다. 이번 UI 조합 빌드에서 해결됐다고 주장하지 않는다.
- 기존 PR180에서 HUD 축소를 검수하고 현재 main 반영 여부를 확인해야 한다. 팀원 병합 후 PR192를 최신 main과 맞춰 검증하면 단독 빌드에도 축소가 유지되는지 확인할 수 있다.
- 런타임 소스/씬/프리팹/설정/패키지/폰트를 새로 수정하지 않았다. 이번 공유 변경은 이 기록지뿐이다.