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

7-1. 지금 이런 식으로 개발자 A쪽에서 요구사항이 있던데 이거를 어떻게 구현하면 될까?

Shared에 월드 스냅샷 저장/복원 계약이 아직 없습니다. A-8 구현 전 IWorldSnapshotProvider, WorldSnapshotDto 또는 이에 준하는 공용 DTO의 필드와 소유자를 먼저 합의해야 합니다. A는 채굴·붕괴·건물·가스·전력 변경점 캡처 및 복원을 구현하고, B는 해당 DTO를 SaveService에 저장·로드하는 역할로 분리하면 됩니다.
DTO는 A의 Unity 월드 오브젝트를 직접 저장하지 않고, 저장·복원에 필요한 변경점만 B의 SaveService로 전달하기 위한 공용 데이터 형식입니다. A는 DTO를 만들고 복원하며, B는 DTO를 파일로 저장·로드합니다. 공용 형식이 없으면 A와 B가 서로 다른 필드명·형태로 구현해 저장 데이터가 맞지 않거나 복원이 실패할 수 있습니다.

7-2. 이번에 또 필요한 데이터가 있다는데 이 부분에 대해서 추가로 구현좀 해줄 수 있겠어?

MVP-connect2의 월드 스냅샷 DTO와 IWorldSnapshotProvider merge를 완료했습니다. A-8 구현 전 확인 결과, 현재 계약에는 월드 Seed, 가스 구역 ID/남은 시간, 전력 케이블 연결 정보가 없습니다. A가 정확한 월드 복원을 구현하려면 해당 필드를 Shared DTO에 추가할지, 복원 후 시스템 재계산으로 대체할지 합의가 필요합니다.

8. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/G/research.md, work_process/MVP/MVP-B/G/test.md 파일을 읽고 G 단계의 작업을 수행해줘 G단계의 주요 작업 내용은 시설 목록, 비용, 설명, 전력, 보유 자원과 선택 상태를 표시, 작업자 A의 Preview/유효성 결과와 내가 작업한 비용 가능 여부를 함께 보여주기, 구조·가스·전력 연결 상태를 즉시 이해할 수 있는 HUD로 표현, 설치 성공/취소 뒤 선택과 UI 상태를 확실히 초기화하는 등 데이터·경제·UI를 A 작업자의 건설 배치, 구조 안정도와 가스 결과에 연결하되 Gameplay 계산을 다시 구현하지 않는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

9. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/H/research.md, work_process/MVP/MVP-B/H/test.md 파일을 읽고 H 단계의 작업을 수행해줘 H단계의 주요 작업 내용은 전력 공급/소비와 연결된 시설 및 비활성 원인을 표시, 충전, 플레이어 화물↔보관함 이동과 정산을 안전한 Service 경로로 처리, 전진기지 설치 완료를 체크포인트와 자동 저장 요청에 반영, 역할 경계를 지켜 작업자 A의 연결/거리/활성 판정을 재구현하지 않으면서 작업자 A가 판정한 전진기지 Runtime 상태를 충전, 보관함, 정산, 체크포인트 State와 UI에 연결하는 거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

10. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/I/research.md, work_process/MVP/MVP-B/I/test.md 파일을 읽고 I 단계의 작업을 수행해줘 I단계의 주요 작업 내용은 귀환, 버팀목, 가스 이탈, 인근 광물, 전진기지, 하강, 충전을 비교,생존 위험을 일반 탐사보다 우선,동일 Context와 설정에는 항상 같은 추천과 근거를 반환하고 반복 대사는 쿨다운하되 긴급 위험 알림은 필요한 정책에 따라 재표시하면서 작업자 A의 `DroneContextDto`에 담긴 실제 게임 상태를 결정론적으로 점수화하고, 추천 행동·근거·템플릿 대사를 같은 결과에서 생성하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

11. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/J/research.md, work_process/MVP/MVP-B/J/test.md 파일을 읽고 J 단계의 작업을 수행해줘 J단계의 작업 내용은 클라우드 성공 시 확정된 분석 결과를 벗어나지 않는 대사를 표시 및 실패, 시간 초과, 오프라인, 제한 초과 시 템플릿 대사로 즉시 폴백, API 키를 Unity 클라이언트와 Windows 빌드에 포함하지 않고, 이벤트별 호출 제한, 쿨다운과 사용자가 직접 요청하는 경로를 두면서 Phase I에서 확정된 추천 행동과 근거를 자연스러운 문장으로 표현할 뿐, 게임 판정이나 추천을 클라우드에 맡기지 않는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

12. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/K/research.md, work_process/MVP/MVP-B/K/test.md 파일을 읽고 K 단계의 작업을 수행해줘 K단계의 작업 내용은 `GameSaveData`, 하위 Save DTO와 `saveVersion`을 정의, tmp 기록 → JSON 검증 → 기존 정상 파일 backup → tmp를 정상 파일로 교체하는 순서를 지키고, 정상 파일 실패 시 backup을 시도하고, 둘 다 실패하면 사용자에게 복구 선택지를 제공 및 자동 저장, 슬롯, 이어하기와 이전 버전 마이그레이션을 지원하면서 플레이어·진행·드론 State와 A의 월드 스냅샷을 버전 있는 JSON으로 원자적으로 저장하고, 정상 파일 손상 시 백업으로 복구하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

13. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/L/research.md, work_process/MVP/MVP-B/L/test.md 파일을 읽고 L 단계의 작업을 수행해줘 L단계의 작업 내용은 세이브 유무와 유효성에 따라 이어하기 상태를 표시, 새 게임이 기존 슬롯을 실수로 덮지 않도록 명시적 선택/확인을 두고, Surface Base의 경제와 진행 UI가 기존 Service를 재사용, 탐사 시작 시 State를 준비하고 Integration/Mine Scene으로 전환하는 등 새 게임·이어하기·슬롯·설정·종료를 Main Menu에, 판매·제작·업그레이드·목표·탐사 진입을 Surface Base에 조립하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

14. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/M/research.md, work_process/MVP/MVP-B/M/test.md 파일을 읽고 M 단계의 작업을 수행해줘 M단계의 작업 내용은 Grid/Tilemap, GameplayRoot, ApplicationRoot, HUDCanvas와 EventSystem의 기준 계층을 생성, Shared 인터페이스와 이벤트를 통해 A Producer와 B Consumer를 연결, 저장 복원과 HUD 활성화의 순서를 보장, A Runtime Prefab 내부를 수정하지 않고 전체 플레이 루프를 통합, A의 검증된 Runtime Prefab과 B의 State·UI·Save 서비스를 `Mine_Demo_Integration.unity` 하나에 연결하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

15.
