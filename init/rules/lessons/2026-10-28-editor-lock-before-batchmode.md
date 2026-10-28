# B-170 에디터가 켜져 있는데 꺼진 줄 알고 배치 모드 실행 → 프로젝트 잠금으로 실패

- 날짜: 2026-10-28
- 태그: testing, editor, process
- 실수: B-170 에디터가 켜져 있는데 꺼진 줄 알고 배치 모드 실행 → 프로젝트 잠금으로 실패
- 원인: `tasklist \| grep -i unity \| head -3`이 Hub 줄만 보여 에디터 프로세스를 놓침
- 다음부터 할 일: 배치 모드 전 `Temp/UnityLockfile`과 `tasklist \| grep -i "^Unity.exe"`를 함께 확인하고, 켜져 있으면 MCP·플래그 실행기를 쓴다
