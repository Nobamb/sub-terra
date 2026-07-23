## 기본 세팅 관련 프롬프트

1. init 폴더 안의 파일들의 내용들을 참고해서 process-B에 해당하는 test, research 파일을 work_process/MVP/MVP-B 폴더 내에서 A,B,C... 형태로 단계별ㅀ 폴더를 생성해주면서 해당 단계 폴더 내에 만들어줘 test, research 파일에 대한 구조는 work_process/process-B/ex/test-ex.md, work_process/process-B/ex/research-ex.md 파일의 내용을 참고해서 만들어주면 돼 만약에 단계가 너무 많아서 Z단계까지 있다면 다음 단계는 AA, AB 등으로 이어나가면 돼

## 상세 작업 프롬프트

2. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/A/research.md, work_process/MVP/MVP-B/A/test.md 파일을 읽고 A 단계의 작업을 수행해줘 A단계의 주요 작업 내용은 전역 객체 생성 및 게임 관련 상태 구현, 전역 서비스 중복 생성 방지, 데이터 검증 실패 등에 대한 내용들에 대해 기록 등의 작업을 거치면서 데이터 카탈로그와 실제 세이브 구현은 뒤 단계에서 주입할 수 있게 경계를 두되, 이 단계에서는 새 게임 상태로 Main Menu까지 안전하게 진입하는 것이 목표야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼
