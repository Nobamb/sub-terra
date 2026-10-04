# 노출 블록 모서리 둥글기 보완

- 요청: 팀원 리뷰 대기 중 이전의 블록 모서리를 조금 더 둥글게 하는 작업 진행.
- 브랜치: fix/mine-corner-rounding. main 3db78130과 확인 완료된 사다리 PR #155의 4993ad52 상태를 유지하며 분리. 사용자 확인 후 커밋/푸시/PR 게시 승인. #155 미병합이므로 PR base는 codex/player-review-ladder-top-walk로 지정해 모서리 변경만 표시한다. #155 병합 후 base를 main으로 변경하고 병합한다.
- 기존 MineTileCornerVisual은 양쪽 이웃이 모두 공기인 바깥 꼭짓점만 처리한다. 원본 PNG 전체를 둥글게 깎으면 연결 블록 사이에 틈이 생기므로 이미지 교체는 하지 않음.
- MineRoundedCorners.shader의 기본 반경만 셀의 0.065 → 0.12로 확대. 안티앨리어싱, 조명 처리, 직선 경계/연결된 내부 보호 방식은 유지.
- 검증에서 기본 반경을 런타임 재질에서 읽는 오류를 확인해, MineTileCornerVisual이 기존 재질 속성 복사 후 셰이더 기본 반경을 명시 설정하도록 보완. 최종 재질의 실제 값 0.12 확인.
- Tile ID, 채굴 보상, 세이브, 충돌, Player/시설/씬/Prefab/meta/ProjectSettings는 수정하지 않음. Inspector 추가 연결 불필요. 새 Play 진입 때 기존 런타임 생성기가 효과를 연결한다.
- 검증 항목: 단독 블록/연결 블록/내부 꼭짓점의 마스크, 채굴 후 노출 갱신, 실제 런타임 재질 반경/셰이더 패스, 비활성화 시 기존 재질 복원, 40m 생성 및 채굴 경계 회귀.
- 화면에서 곡률이 적절한지는 Bootstrap → 새 게임 → 탐사 후 L자 돌출 모서리/단독 블록에서 확인. 긴 직선 경계와 연결된 내부가 직각인 것은 의도한 동작이다. 충돌 모양은 원래 격자 그대로 유지한다.

## 검증 결과

- Unity 6000.5.4f1에서 새 모서리 테스트 2개 통과: 바깥/연결 모서리 판정, 실제 반경 0.12, 마스크 갱신 및 비활성화 재질 복원, 셰이더 SetPass 성공.
- 증거: output/player-review/rounded-corners-verified.xml 및 로그. 마스크는 생성 텍스처를 검사하며 최종 화면 픽셀 비교/GPU 모서리 형상 검증은 하지 않았다. 초기 Material.GetTexture 기반 검사에서는 조회 오류가 발생해 생성 텍스처 검사와 셰이더 컴파일 검사를 분리했다.
- 기존 RuntimeGenerator_RendersFortyMetersAndMiningRejectsBoundary 테스트는 DeepZoneLocked 대신 InvalidTarget으로 실패. 런타임 두 파일을 HEAD와 동일하게 복원한 별도 기준 실행에서도 같은 실패 재현(output/player-review/rounded-corners-baseline.xml). 이번 수정으로 새로 생긴 실패는 아니며 범위 밖으로 보존. 최종 수정 상태로 재적용 완료.
- 관련 C# 컴파일 오류 없음. 프로젝트 전체 테스트/Windows 빌드/자동 Bootstrap 화면 검증 미실행. 사용자가 직접 확인 후 개선된 것을 승인했다. 추후 곡률이 과하면 0.10, 부족하면 최대 0.15 범위에서 추가 조정 가능.
