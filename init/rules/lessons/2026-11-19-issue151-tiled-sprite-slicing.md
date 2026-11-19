# 타일 모드로 늘려 그리는 스프라이트를 단순 스프라이트로 보고 잘랐다
- 날짜: 2026-11-19
- 태그: sprite, vfx, build-effect, render, issue151
- 실수: 건설 연출이 완성 그림을 가로 띠로 잘라 조립하는데, 사다리 프리팹(`SpriteRenderer.drawMode = Tiled`, size 1×5)을 `sprite.rect` 기준으로 잘랐다. 조각 EditMode 검사 30개는 모두 통과했지만 실제 카메라 렌더에서는 사다리가 1칸짜리 작은 "H"만 보였다.
- 원인: 스프라이트 원본은 1×1칸이고 5칸 높이는 렌더러의 `size`와 `tileMode`가 만든다. 보이는 범위도 `sprite.bounds`가 아니라 `renderer.size`다.
- 다음부터 할 일: 그림을 잘라 쓰거나 범위를 재는 코드는 `drawMode != Simple`을 먼저 확인해 같은 모드의 띠로 나누고 범위는 `size`로 계산한다. 시각 연출은 데이터 검사만으로 끝내지 말고 프리뷰 씬 렌더 시트로 시설마다 프레임을 눈으로 확인한다.
- 관련: B-206, `FacilityConstructionEffect.GetDrawnBounds`, `Ladder_Buildable.prefab`
