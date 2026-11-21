# PowerShell 기준점 배열은 각 수식의 괄호를 분리
- 날짜: 2026-11-03
- 태그: tooling, powershell, animation, pivot
- 실수: 정렬 메타데이터에 비율 배열을 만들며 쉼표를 포함한 나눗셈 식의 괄호를 생략해 생성이 중단됐다.
- 원인: PowerShell이 배열 원소를 연산자 피연산자로 묶어 Object[] 나눗셈을 시도했다.
- 다음부터 할 일: 각 원소 수식에 괄호를 사용하고 생성된 JSON의 원소 수/실수값을 확인한다. 검토용 발 원점과 기존 VisualRoot를 유지하는 Sprite pivot 후보도 구분한다.
- 관련: #266, work_process/tools/female-motion-alignment-20261103.ps1.
