# EditMode 전체 실행 뒤 범위 밖 프리팹·폰트·통합 씬·PNG가 dirty됨

- 날짜: 2026-10-26
- 태그: testing, editor, assets
- 실수: EditMode 전체 실행 뒤 범위 밖 프리팹·폰트·통합 씬·PNG가 dirty됨
- 원인: 빌더·캡처를 도는 검사가 에셋을 저장
- 다음부터 할 일: 전체 실행 전후 git status를 비교하고 범위 밖 파일은 커밋에서 빼고 복구한다
