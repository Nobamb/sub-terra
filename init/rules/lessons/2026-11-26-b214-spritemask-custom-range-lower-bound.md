# SpriteMask 사용자 정렬 구간의 아래 경계는 포함되지 않아 포탈 웜홀 복사본이 통째로 안 보였다

- 날짜: 2026-11-26
- 태그: spritemask, sorting, vfx, playmode, b214
- 실수: B-214 포탈 홀 회전에서 마스크 구간을 `back = order+1, front = order+1`(복사본 정렬과 같게)로 줬다. EditMode 테스트(`IsHoleVisible`, 각도)는 통과했는데 실제 화면에는 회전이 전혀 안 보였다(프레임 간 픽셀 차이 0).
- 원인: `isCustomRangeActive`일 때 마스크는 (back, front] 구간만 가린다. back과 같은 정렬 값의 `VisibleInsideMask` 렌더러는 어느 마스크의 영향도 못 받아 스텐실이 0이라 통째로 사라진다. 또 마스크 반지름이 홀보다 작아 가장자리 푸른 호가 마스크 밖에 있어 돌려도 변화가 거의 없었다.
- 다음부터 할 일: 정렬 구간은 `back = 렌더러 order - 1 이하, front >= 렌더러 order`로 준다. 마스크 연출은 실제 플레이 카메라로 두 각도를 렌더해 픽셀 차이를 재서 확인한다. 홀·링 크기는 문서 숫자 대신 PNG 투명 영역에서 직접 잰다(문서 홀 중심 0.52는 실제 0.47이었다).
- 관련: B-214, `PortalHoleSpin`
