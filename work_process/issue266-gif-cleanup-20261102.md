# #266 사다리 GIF 색 얼룩 줄이기

- 사용자는 치마 관통 보정5자세의 외형을 유지하면서 GIF의 얼룩만 줄여 달라고 요청했다. 캐릭터 이미지 생성/재작화/부위 조립은 하지 않았다.
- 이전 GIF는 ffmpeg paletteuse의 sierra2_4a 오차 확산 디더링으로 색을 섞었고 헬멧/장비의 그라데이션에 작은 점무늬가 나타났다. 새 출력은 전체 주기에 공통256색 팔레트, reserve_transparent=0(불투명 검토 배경), dither=0, Lanczos 축소를 사용한다.
- 변경 도구: `tools/female-original-outfit-ladder-preview-20261102.ps1`. 원본 전신5자세는 변경하지 않는다. 새 경로를 사용해 기존 GIF도 보존했다.
- 결과: `images/issue266/female-ladder-skirt-v5-clean-preview/ladder-review.gif`. 8프레임1→2→3→2→1→4→5→4,5fps,384×384. 색 손실을 줄인 비교용 `ladder-lossless.webp`도 같은 폴더에 생성했다(무손실 WebP, 같은 재생 순서/속도).
- GIF에는256색 제한에 따른 완만한 색 단차가 남을 수 있다. 무손실 WebP는 출력 미리보기의 팔레트 양자화를 피한다. 게임은 둘 대신 원본 PNG를 사용하도록 준비하므로 GIF의 점무늬를 원화 질감으로 오인하지 않는다.
- 확인: 기존/새 GIF의 첫 프레임을 PNG로 디코딩해 나란히 시각 비교했다. 새 폴더의5개 pose PNG는 기존 v5 pose PNG와 SHA-256 모두 동일했다. git diff --check 통과. 실제 시간 재생의 체감과 게임 화면은 미검증이다.
- Unity Assets/씬/프리팹/게임 코드 변경 없음. 커밋·푸시·PR #270 갱신하지 않았다.

다음 순서: 치마·관통 보정·깨끗한 GIF를 같은 PR #270에 반영하도록 게시 승인 받기 → 팀원 제작 방향 리뷰 → 승인된 방식의 걷기/채굴 전체 연결과 각각 GIF 공유 → 게임 크기/접점 확인·연결. 리깅은 재개하지 않는다.
