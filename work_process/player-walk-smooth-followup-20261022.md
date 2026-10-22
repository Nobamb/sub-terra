# PR #242 걷기 후속 — 2026-10-22

## 현재 상태

- 사용자가 2026-10-22 마지막 미리보기를 확인한 뒤 기존 PR #242의 커밋·푸시·갱신을 승인했다. 아래 16장 후속과 데스크탑 인계 자료를 같은 브랜치에 게시한다. 기존 4자세 구현 커밋은 `244f84fe`다.
- 사용자가 새 부품 기반 GIF의 비율·기계적인 다리 동작을 거절했다. 해당 시안을 완성으로 판단한 것은 잘못이었다. 프레임 수/연결 검사 통과를 시각 품질의 근거로 삼지 않는다.
- 거절된 그림·부품은 `work_process/tools/rejected-walk-rig-20261022/`에 보존하고 Assets에서 제외했다. 생성된 16장 시트의 일관성이 부족했던 시안도 제품에 채택하지 않았다.
- #240 진행 코드 3개는 stash `9d6072a34798398c13e4be8c704f942066194b2e` (`issue240-partial-withdraw-pending-visual-review-20261022`)에 보존했다. 검사 미완료이므로 실행 코드 게시 대상이 아니다. #211 stash `d3c8688e3d481e6ce2250cc8f46be7201090b00f`도 유지한다. 데스크탑으로 stash는 전달되지 않으므로 두 변경의 복원용 패치를 `work_process/handoff-20261022/`에 함께 게시한다. 패치는 컴파일·자동 적용되지 않는다.

## 원인과 현재 재작업 방향

- #242 기존 10칸은 서로 다른 4자세를 0.2/0.3초씩 유지한다. 16칸에 중복 그림을 추가하는 것으로는 끊김을 해결하지 못한다.
- 원래 광부의 `Frames/Idle/player_idle_01.png`를 사용한다. 얼굴·헬멧·작업복·신발의 기존 텍스처를 공유하며 새 체형을 생성하지 않는다.
- 새 부품을 생성한 시안, 원화를 여러 딱딱한 조각으로 나눈 시안, 다리 사이까지 연결한 격자 시안은 채택하지 않는다. 특히 연결 격자는 다리가 교차할 때 삼각형이 길게 늘어나 가로 줄무늬가 생겼다. 비교용 소스는 `work_process/tools/rejected-cutout-baker-20261022.cs`, `rejected-connected-mesh-baker-20261022.cs`에 보존한다.
- 현재 `SmoothWalkCycleBaker`는 다리별로 분리한 메시를 원래 UV로 세분화하고, 허벅지→정강이→신발의 가중치를 이어서 변형한다. 다리 사이 연결면과 별도 무릎 덮개를 사용하지 않는다. 양발 반 주기 교대, 접지 구간의 수평 신발, 짧은 발 들기, 팔 반대 스윙, 몸통의 작은 상하 이동을 16단계로 렌더한다. 상체 윤곽은 원화에 유지한다.
- 런타임은 기존 SpriteRenderer 프레임 재생을 유지한다. 16장/16fps, 주기 1초. 런타임 메시나 물리·이동·점프·사다리 코드는 추가하지 않는다.
- PNG 16장의 교차 자세·신발 위치·접합을 비교했다. 플레이 중 자연스러움은 아직 확정하지 않는다. 프레임 수나 자동 검사 통과를 시각 품질 승인으로 바꾸어 보고하지 않는다.

## 변경 파일

- `Scripts/Gameplay/Player/PlayerAnimationController.cs`: 걷기 프레임 수·속도 상수 16.
- `Editor/PlayerAnimationAssetBuilder.cs`: 걷기만 가져와 프리팹 연결·미리보기 클립을 갱신하는 메서드 추가. 전체 Animator/다른 애니메이션을 재생성하지 않는다.
- `Editor/SmoothWalkCycleBaker.cs`, `Editor/WalkBake.shader` 및 `.meta`: 다리 사이 연결면 없이 원화의 메시 UV를 유지하는 Editor 전용 렌더링. Sprite.OverrideGeometry의 UV 재배치 시안은 폐기했다.
- `Art/Characters/Player/Frames/Walk/walk_01.png`~`walk_16.png` 및 `.meta`: 16장의 서로 다른 출력. 기존 01~10 GUID 유지, 11~16 새 GUID.
- `PlayerWalk.anim`, `Player.prefab`: Editor API로 갱신. 프리팹 walkFrames에 6개 참조 추가. Editor 저장 과정에서 기존 코드 기본값인 coyoteTimeSeconds/jumpBufferSeconds 0.1 두 필드도 직렬화됐다. 점프 수치의 실제 동작 변경은 없다.
- `Tests/EditMode/App/Readiness/PlayerWalkCycleTests.cs` 및 `.meta`: 실제 프리팹의 16장 연결, 중복 파일 여부, 16번째 프레임까지 재생, 1초 반복, 클립 속도 검사 1건.

## 확인과 한계

- Editor가 출력한 실제 PNG를 눈으로 비교했으며 GIF는 미리보기 포장이다. 이전 GIF의 색상 점무늬도 피하도록 미리보기 팔레트를 변경했다.
- 현재 프로젝트와 같은 PNG를 포장한 미리보기: `work_process/images/player-walk-separated-skin-preview-20261022.gif`, `player-walk-separated-skin-poses-20261022.png`. 이름에 `original-art-preview`가 들어간 이전 GIF는 거절된 조각 시안이므로 현재 결과로 전달하지 않는다.
- 기존 Idle→Walk 전환 검사는 마지막 빌더 실행 중 통과했다. 16개 PNG의 내용 해시는 모두 다르다. 마지막 검사 `work_process/tools/walk-separated-skin-check-20261022/EditMode-summary.txt`: 1/1 통과, 실패 0, 1.65초, 종료 코드 0. 이 실행에서 C# 컴파일 오류·예외도 발견되지 않았다. 재생 연결 검사이며 미술/동작 품질 승인과 구분한다.
- 전체 Bootstrap 플레이나 Windows 빌드 검사는 하지 않았다. 이 검사 결과는 걷기가 자연스럽다는 판정이 아니다.
- ProjectSettings·패키지·씬 유효 변경 없음. 새 PR을 추가하지 않고 기존 #242를 갱신한다. #211·#240은 이 PR의 실행 코드에 포함하지 않는다.

## 다음 작업자에게

1. 첫 번째 Unity Hub 프로젝트 `C:/Users/jeone/sub-terra/sub-terra`에서 작업한다. 게시 브랜치는 `codex/player-walk-two-leg-cycle`이다. 데스크탑의 절대 경로는 다를 수 있으나 Unity 루트는 저장소 아래 `sub-terra/`여야 한다.
2. Bootstrap에서 시작해 좌우 걷기, 정지→걷기, 걷기→정지, 마지막 프레임→첫 프레임 연결을 확인한다. 걷기 스프라이트만 바꿨고 이동 속도·점프·사다리 로직은 변경하지 않았다.
3. 사용자는 현재 미리보기의 게시를 승인했다. 실제 게임의 이동·정지 전환과 Windows 실행 체감까지 검증 완료한 것은 아니다. 후속 걷기 수정도 #242가 열려 있으면 같은 PR에 이어간다.
4. #240과 #211은 위 stash와 복원용 패치에 보존되어 있다. 다음 순서와 적용 방법은 `work_process/desktop-handoff-20261022.md`를 따른다.
