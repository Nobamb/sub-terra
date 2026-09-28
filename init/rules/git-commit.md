# Git·브랜치·PR 규칙

11. Git 작업 규칙
    11-1. `main`은 안정 버전, `develop`은 통합 기준 브랜치로 유지한다.
    11-2. `main` 직접 push는 금지한다.
    11-3. 변경은 기능 브랜치와 Pull Request 단위로 통합하는 것을 기본으로 한다.
    11-4. 하나의 브랜치는 하나의 주요 목적만 담당하고 `feature/a-*`, `feature/b-*`, `fix/*` 형식을 사용한다.
    11-5. Shared 계약, Producer, Consumer, Integration Scene, 통합 테스트 순으로 병합한다.
    11-6. Scene, Prefab과 `.meta` 변경을 PR에서 명시한다.
    11-7. 파일 수를 강제하지 않지만 작업 목적과 다른 담당자 소유 파일은 수정하지 않는다.
    11-8. 작업 완료 후 변경 목적, Inspector 참조, 통합 방법과 검증 결과를 명확히 남긴다.
