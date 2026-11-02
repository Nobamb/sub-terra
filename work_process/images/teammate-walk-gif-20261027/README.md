# 팀원 GIF 적용본

원본 `source.gif`의 캐릭터와 동작을 유지하고 검정 배경을 투명하게 정리했다.

- 원래 순서47장, 각100ms/10fps, GIF 전체 반복4.7초 유지.
- `transparent-walk.gif`: 투명 GIF.
- `runtime-frames/`: 게임/가이드가 사용하는47개 256×256 RGBA PNG.
- `white-dark-preview.gif`: 흰색/검은색 배경 동시 확인.
- `all-frames-white.png`, `all-frames-dark.png`: 47장 전체 비교.
- `checks.json`: 원본 RGB 보존/재생시간/게임 이미지 해시.
- 얼굴/신체를 새로 그리거나 부위별로 합성/변형하지 않았다. 실제 원본 크기 PNG의 RGB는 원본과 같고, 게임용은 전신 전체의 크기/위치만 정렬했다.
- 구현·검사 결과는 `work_process/player-walk-teammate-gif-20261027.md` 참조.
