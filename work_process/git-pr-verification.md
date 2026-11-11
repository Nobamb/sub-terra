# Sub-Terra PR 점검과 검사 결과 기록

## 푸시 전 점검

PR 브랜치에서 Git 루트를 열고 실행한다. `origin/main`은 PR의 대상 브랜치가 main인 경우다.

```powershell
git fetch origin
git status --short
git diff --cached --check
git diff --stat origin/main...HEAD
git diff --check origin/main...HEAD
git merge-tree --write-tree --name-only origin/main HEAD
```

- `git status`와 `git diff --cached`로 이번 커밋에 들어갈 파일을 확인한다. 세 점 비교는 커밋한 PR 변경을 보여준다.
- 가상 병합의 종료 코드 0은 충돌 없음, 1은 충돌이다. 그 외 값이면 명령 오류를 확인한다. 가상 병합은 작업 파일과 스테이징 영역을 건드리지 않는다.
- 푸시한 뒤 PR의 최신 커밋을 확인하고, 다른 사람이 브랜치나 main을 갱신했으면 그 커밋 기준으로 다시 점검한다.

## PR에 남길 내용

- PR 커밋과 비교한 main 커밋을 짧은 해시로 적는다.
- 정적 검사, Unity EditMode·PlayMode, 실제 플레이 중 실행한 항목과 결과를 기록한다. 이전 커밋에서 실행한 결과는 그 시점의 결과임을 밝힌다.
- 실제 클릭, 화면, Windows 빌드 등 남은 확인은 미검증으로 적는다.
- PR의 Checks 탭은 자동 워크플로나 권한이 있는 외부 검사 서비스가 기록한다. 로컬 검사 결과는 당장 PR 본문에서 공유할 수 있다.

## 필요할 때 쓰는 명령

- `gh auth status --hostname github.com`으로 GitHub CLI 로그인 상태를 확인한다. CLI가 설치된 환경에서 실행한다.
- `gh pr checks 311`로 PR의 GitHub 검사 상태를 조회한다.
- `git log -S 'ApplyDetailsCloseLayout' --oneline`으로 함수명이나 문구가 바뀐 커밋을 찾는다.
- `git worktree list`로 Unity Hub에 등록된 프로젝트의 실제 브랜치를 대조한다. 저장소 아래 `sub-terra/`가 Unity 프로젝트 경로다. 별도 작업 폴더에서 Unity를 열면 그 폴더에 Library가 생겨 디스크를 추가로 사용한다.

PR의 `Git diff check`는 GitHub가 만든 시험 병합에서 대상 브랜치 대비 변경분의 공백 오류와 충돌 표시를 검사한다. Unity 실행 결과는 PR 본문에 별도로 기록한다.

## 데스크탑에서 이어갈 검증

1. [#311](https://github.com/Nobamb/sub-terra/pull/311) 브랜치에서 Unity 건설창 빌더를 실행해 프리팹을 재생성한다. 광산에서 안내 아래 채굴·설치와 버튼 클릭을 확인한다.
2. 노트북에서 전달받은 #279 인계 문서와 패치로 변경을 복원한 뒤 설치 미리보기, 지형 가림, 저장 후 이어하기를 확인한다.
