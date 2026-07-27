## 기본 세팅 관련 프롬프트

1. init 폴더 안의 파일들의 내용들을 참고해서 process-B에 해당하는 test, research 파일을 work_process/MVP/MVP-B 폴더 내에서 A,B,C... 형태로 단계별ㅀ 폴더를 생성해주면서 해당 단계 폴더 내에 만들어줘 test, research 파일에 대한 구조는 work_process/process-B/ex/test-ex.md, work_process/process-B/ex/research-ex.md 파일의 내용을 참고해서 만들어주면 돼 만약에 단계가 너무 많아서 Z단계까지 있다면 다음 단계는 AA, AB 등으로 이어나가면 돼

## 상세 작업 프롬프트

2. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/A/research.md, work_process/MVP/MVP-B/A/test.md 파일을 읽고 A 단계의 작업을 수행해줘 A단계의 주요 작업 내용은 전역 객체 생성 및 게임 관련 상태 구현, 전역 서비스 중복 생성 방지, 데이터 검증 실패 등에 대한 내용들에 대해 기록 등의 작업을 거치면서 데이터 카탈로그와 실제 세이브 구현은 뒤 단계에서 주입할 수 있게 경계를 두되, 이 단계에서는 새 게임 상태로 Main Menu까지 안전하게 진입하는 것이 목표야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

3. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/B/research.md, work_process/MVP/MVP-B/B/test.md 파일을 읽고 B 단계의 작업을 수행해줘 B단계의 주요 작업 내용은 표시 이름과 저장/연동용 ID를 분리, 구리·철·리튬, MVP 시설과 업그레이드 정의를 코드 수정 없이 편집 및 중복 ID와 필수 참조 누락을 실행 전 자동으로 찾게 하면서 A 단계는 구체 App 클래스가 아니라 합의된 데이터/Shared 경계로 읽을 수 있게 하면서 광물, 시설, 레시피, 업그레이드, 대사 정의를 ScriptableObject 에셋으로 만들고, 영구 ID로 안전하게 조회·검증하는 단일 카탈로그를 구축하는 것이 목표야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

4. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/C/research.md, work_process/MVP/MVP-B/C/test.md 파일을 읽고 C 단계의 작업을 수행해줘 C단계의 주요 작업 내용은 전력, 깊이, 골드, 화물, 가치, 구조, 가스, 건설 선택과 상호작용 안내를 표시, UI가 State를 직접 변경하지 않도록 설정, Scene 로드 후 UI 구독과 참조가 다시 연결되고, 파괴된 UI가 이벤트에 남지 않게 설정, 여러 해상도에서 안전 영역과 레이아웃을 유지하면서 HUD를 State의 읽기 전용 표현으로 만들고 상태 변경 이벤트가 발생한 항목만 갱신하도록 하면 돼 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

5. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/D/research.md, work_process/MVP/MVP-B/D/test.md 파일을 읽고 D 단계의 작업을 수행해줘 D단계의 주요 작업 내용은 `IMiningRewardReceiver`의 B 측 구현체를 제공, 광물별 수량, 현재/최대 화물 중량과 미정산 가치를 관리, 최대 적재량, 잘못된 ID·수량과 중복 지급 경계를 명시 및 인벤토리와 HUD가 한 번의 상태 변경 결과를 즉시 표시하면서 A의 채굴 시스템이 전달한 광물 ID와 수량을 인벤토리에 반영하고, 데이터 카탈로그를 기준으로 총중량과 미정산 가치를 일관되게 계산하는거야
   unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

6. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/E/research.md, work_process/MVP/MVP-B/E/test.md 파일을 읽고 E 단계의 작업을 수행해줘 E단계의 주요 작업 내용은 선택한 광물만 판매하고 정확한 골드를 지급, `IResourceWallet`로 시설 비용의 지불 가능 여부와 실제 차감을 제공, 판매·제작 성공/실패 결과를 UI와 자동 저장 요청에 전달, 중간 실패 시 인벤토리와 골드가 부분 변경되지 않게 하는 기능들을 구현하면서 광물 판매와 시설 제작 비용 검사를 Service 트랜잭션으로 처리하고, 설치 성공 전에는 자원을 차감하지 않도록 하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

7. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/F/research.md, work_process/MVP/MVP-B/F/test.md 파일을 읽고 F 단계의 작업을 수행해줘 E단계의 주요 작업 내용은 드릴 속도·효율, 최대 전력·화물, 드론 스캔·구조 보존, 가스 저항을 단계별 데이터로 관리 및 비용 차감과 레벨 상승을 원자적으로 처리, A가 B의 구체 클래스를 참조하지 않고 효과를 조회하도록 설정, 현재 레벨과 잠금 해제 상태를 후속 Save 단계가 저장할 수 있게 하는 기능을 추가하면서 데이터 기반 업그레이드 구매, 효과 조회와 심층 구역 잠금 해제를 제공하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼
