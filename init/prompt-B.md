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

15. work_process/MVP/MVP-B 폴더 내에서 work_process/MVP/MVP-B/N/research.md, work_process/MVP/MVP-B/N/test.md 파일을 읽고 N 단계의 작업을 수행해줘 N단계의 작업 내용은 탐사 시작부터 심층 신호와 데모 종료까지 필수 흐름을 완주하게 하도록 하고, 현재 목표와 다음 행동은 명확히 보이되 조작을 과도하게 막지 않기, 구조·가스 등 긴급 UI가 튜토리얼보다 우선시하면서 정산, 업그레이드와 잠금 결과를 실제 State와 동기화하면서 Phase M의 통합 기능을 처음 플레이하는 사람이 막힘 없이 경험하도록 목표, 안내, 드론 대사, 결과와 기본 사운드를 순서에 맞춰 연출하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

## 빌드 및 배포 전 MVP 단계까지 작업 후 추가 수정 방안

16. 게임 내에서 우선 게임 UI 부분을 좀 더 수정해주었으면 좋겠어 지금 게임 내 화면이 거의 검은 화면에 몇몇 도트들만 존재하고 있고, 심지어 캐릭터가 밟고 있는 지형 UI도 아예 보이지 않아서 X축에서 일정 부분 이동하면 갑자기 캐릭터가 떨어지게 되는데다가 지형 위에 있는 특정 색깔의 도트들은 어떤 역할을 하는지도 도저히 알 수가 없어 그리고 그 밖에도 게임 시작할 때 버튼 및 텍스트들이 있는 게임 창도 자체적으로 크기도 작은 데다가 지금 게임 전체화면에서 윗쪽에 몰려있는데 크기도 늘리면서 중앙으로 배치해주면 좋겠고 새 게임을 클릭할 때 뜨게 되는 세이브를 덮어쓰겠냐는 팝업에서도 지금 텍스트가 팝업 바깥으로 빠져나와있는데다가 버튼들도 중앙에서 살짝 윗부분으로 쏠린 형태인데 팝업 크기랑 버튼 크기, 텍스트 크기들 30% 더 늘려주면서 텍스트랑 버튼 배치는 중앙을 기준으로 각각 위아래로 10%떨어진 위치로 배치해줘 그리고 새 게임 시작할 때 게임 진입전에 나타나는 surface base 관련 텍스트 및 버튼 그룹은 게임 윗부분에 쏠려있고 드릴 관련 레벨 및 목표 관련 텍스트 그룹은 아랫부분에서 살짝 오른쪽으로 쏠린 형태이던데 이 내용들을 하나로 합치고 정 중앙에 배치하는 식으로 수정해주면 좋겠어

17. 이번에는 드론의 크기가 플레이어블 캐릭터의 크기보다 더 커졌어 플레이어블 캐릭터의 크기도 지형 블록 크기의 0.7배 정도로 잡아주고, 양 옆에 세로로 블록이 있는데 그 세로로 세워진 블록을 캐 릭터가 통과를 하는 문제가 있어 이 부분도 캐릭터가 그 세로로 세워진 블록들을 통과할 수 없도록 수 정해 그리고 캐릭터가 지금 지형 블록 위에 떠있는 형태로 위에 있는데 캐릭터의 바닥 부분이 지형 블 록의 윗부분에 맞닿게 수정해주고 지형 위에 있는 각양각색의 오브젝트들은 전진기지 코어 빼면 다 지형블록에서 생겨나는 거잖아 마침 클릭이나 엔터를 눌러도 지형 블록이 파지지 않는데 클릭 또는 엔터를 누르면 지형 블록이 사라지도록 수정도 해주면서 해당 블록에서 오브젝트들이 나타나는 형태로 만들어주고 지형 블록이 지금 밑에 한줄만 있잖아 지형블록도 밑에 40줄로 늘려서 수정해

18. 지금 보니까 PRD에 구현되어야 할 것들이 아직 게임내에 다 구현이 안된 것으로 보여 지금 구현해야 될 대상들 중에 찾은것만 이정도야

### 1. 버팀목(Support Pillar) 월드 클릭 배치 & 시각화

- **현재 상태**: UI에서 버팀목을 구매해도 화면 상에 나타나지 않음.
- **필요 작업**: 버팀목 구매 후 마우스를 움직이면 **배치 미리보기(Preview)**가 보이고, 원하는 위치에 좌클릭 시 **버팀목 오브젝트가 실제 월드에 설치**되어 주변 암석의 붕괴를 막아주도록 연동해야 합니다.

---

### 2. 수직 이동 수단 (시작 엘리베이터 & 사다리/발판)

- **현재 상태**: 수직 이동용 스크립트 미구현.
- **필요 작업**:
  - 지상(Surface Base)과 지하 40m 탐사 구역을 연결하는 **시작 엘리베이터(Elevator)** 기능 구현.
  - 깊은 수직 구멍을 뚫었을 때 점프만으로 올라오지 못하는 구간을 위한 **사다리(Ladder) 또는 버팀목 발판 상호작용** 추가.

---

### 3. 카메라 추종(Camera Follow) 및 맵 경계(Confiner) 설정

- **현재 상태**: 1줄 맵 기준 카메라 세팅.
- **필요 작업**: 지층이 지하 40줄로 깊어짐에 따라, **카메라가 플레이어를 부드럽게 추적(Cinemachine / Follow Camera)**해야 하며, 맵 바깥의 검은 빈 공간이 과도하게 렌더링되지 않도록 **카메라 경계(Confiner)**를 설정해야 합니다.

---

### 4. 지하 40줄 자연스러운 자원/가스 분포 밸런스

- **현재 상태**: 낱개 도트 5개가 지상에 노출되어 있음.
- **필요 작업**: 40줄 지하 지층 생성 시:
  - **상층 (1~15m)**: 암석 위주 + 구리 맥(Ore Vein)
  - **중층 (15~35m)**: 철 + 가스 포켓 위험 지대
  - **심층 (35~40m)**: 리튬 + 잠긴 신호(Locked Signal)
    이처럼 **깊이별 자원 분포 비율(암석 85~90%, 자원 10% 내외)**에 맞게 지층 내부에 자연스럽게 매립 생성되도록 구성해야 합니다.

---

### 5. 드론(Digger-Bot) 대사 팝업 및 상황 알림 연동

- **현재 상태**: 드론 모델만 플레이어를 따라다님.
- **필요 작업**: 위험 가스 포켓 근처에 가거나 지반이 붕괴될 때, 인벤토리가 가득 찼을 때 드론 머리 위에 **상황별 템플릿/생성형 대사 팝업**이 표시되어 플레이어에게 가이드를 주도록 연결해야 합니다.

그래서 이 내용을 포함해서 PRD 내에서 추가로 구현이 안된 부분들을 더 조사해서 추가로 구현해야 될 내용들을 모두 정리한 후에 work_process/MVP2/MVP-B 폴더에 A폴더부터 해서 순서대로 test, research 파일들을 정리해서 만들어줘 PRD나 기타 게임에 필요한 요소 및 수정해야 될 요소들 전체를 정리해서 MVP2 폴더 내에 정리한 것들을 작업을 하게 되면 전체적인 게임의 흐름과 핵심 콘텐츠가 다 구현될 수 있도록 말이야

## 상세 작업 프롬프트(MVP 2차 작업)

19. work_process/MVP2/MVP-B 폴더 내에서 work_process/MVP2/MVP-B/A/research.md, work_process/MVP2/MVP-B/A/test.md 파일을 읽고 2차 MVP A 단계의 작업을 수행해줘 A단계의 작업 내용은 PRD 필수 기능을 Definition/Runtime/Restore/Play 네 수준으로 추적 및 Integration Scene의 Missing Script, 누락 참조, 중복 시스템과 임시 placeholder를 자동 탐지, 실제 입력 기반 최종 완주 테스트의 뼈대를 만들고 대역 테스트와 실제 Runtime 테스트 결과를 구분하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

20. work_process/MVP2/MVP-B 폴더 내에서 work_process/MVP2/MVP-B/B/research.md, work_process/MVP2/MVP-B/B/test.md 파일을 읽고 2차 MVP B 단계의 작업을 수행해줘 B단계의 작업 내용은 1~15m 상층, 15~35m 중층, 35~40m 심층 규칙을 구현, 전체 지층의 암석 85~90%, 자원/가스/신호 약 10%를 목표 범위로 두고 낱개 점이 아닌 2~5칸 광맥과 위험 포켓을 생성, 시작/엘리베이터/필수 튜토리얼 경로는 항상 통과 가능하게 보장하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

21. work_process/MVP2/MVP-B 폴더 내에서 work_process/MVP2/MVP-B/C/research.md, work_process/MVP2/MVP-B/C/test.md 파일을 읽고 2차 MVP C 단계의 작업을 수행해줘 C단계의 작업 내용은 Surface Base와 Mine 시작 지점을 연결하는 엘리베이터를 만들기, 엘리베이터 호출/탑승/이동/도착 상태를 명확히 표시하기, 깊은 shaft에서 사용할 사다리 또는 설치형 발판을 제공하기, 이동 중 입력·충돌·세이브 전환이 꼬이지 않게 하는 작업을 하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

22. work_process/MVP2/MVP-B 폴더 내에서 work_process/MVP2/MVP-B/D/research.md, work_process/MVP2/MVP-B/D/test.md 파일을 읽고 2차 MVP D 단계의 작업을 수행해줘 D단계의 작업 내용은 수평·수직 이동을 부드럽게 추적, 카메라 viewport 전체가 월드 경계 밖으로 나가지 않게 설정, Surface Base와 Mine의 서로 다른 경계를 지원하도록 설정, 엘리베이터 이동/구조 실패 시 순간이동에 적절히 대응하도록 하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

23. work_process/MVP2/MVP-B 폴더 내에서 work_process/MVP2/MVP-B/E/research.md, work_process/MVP2/MVP-B/E/test.md 파일을 읽고 2차 MVP E 단계의 작업을 수행해줘 E단계의 작업 내용은 키보드/마우스 모두 같은 채굴 검증과 완료 경로를 사용, 타일별 채굴 시간과 드릴 업그레이드 효과를 적용, 시작/진행/완료 시 전력 정책을 명확히 하고 부족하면 채굴을 막게 하고, Inventory 중량이 Player 속도에 단계적으로 반영, 채굴 보상과 월드 오브젝트가 정확히 한 번만 생성되도록 하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

23-1. 지금 블록을 캐게 될 때 클릭은 문제가 없는데 엔터를 기준으로 블록을 캐게 되면 가까이 있는 방향(오른쪽에 가까이 있으면 오른쪽 기준, 왼쪽에 가까이 있으면 왼쪽 기준)으로 2칸+아래로 1칸에 위치한 블록을 캐도록 되어있던데 이 부분을 수정해줘 수정 방향은 가까이 있는 방향에 해당하는 바로 옆의 블록을 우선시해서 캐도록 하고 바로 옆의 블록이 없다면 위=>아래 순으로 우선 순위를 정해서 캐도록 해줘 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

24. work_process/MVP2/MVP-B 폴더 내에서 work_process/MVP2/MVP-B/F/research.md, work_process/MVP2/MVP-B/F/test.md 파일을 읽고 2차 MVP F 단계의 작업을 수행해줘 F단계의 작업 내용은 건설 메뉴에서 버팀목을 선택하면 커서 Preview를 표시, 유효/무효 위치와 실패 이유를 즉시 보여주기, 좌클릭 성공 시 Runtime Support를 생성하고 비용을 한 번만 차감, 설치된 Support가 주변 구조 계산에 실제 영향을 주도록 하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

25. work_process/MVP2/MVP-B 폴더 내에서 work_process/MVP2/MVP-B/G/research.md, work_process/MVP2/MVP-B/G/test.md 파일을 읽고 2차 MVP G 단계의 작업을 수행해줘 G단계의 작업 내용은 안정/주의/위험/붕괴 임박 단계를 시각·음향으로 구분, 위험 셀 주변에 균열 타일/overlay를 표시, Seed와 변경 상태가 같으면 같은 셀이 붕괴하게 설정, 붕괴가 Player 피해/행동불능 시스템에 전달될 이벤트를 제공하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

26. work_process/MVP2/MVP-B 폴더 내에서 work_process/MVP2/MVP-B/H/research.md, work_process/MVP2/MVP-B/H/test.md 파일을 읽고 2차 MVP H 단계의 작업을 수행해줘 H단계의 작업 내용은 가스 진입 즉시 경고하고 강도에 따라 효과를 적용 및 전력 지속 감소, 이동 감속, 시야 제한을 실제 상태에 반영,누적 노출이 피해/구조 실패 입력으로 전달되게하기, 이탈·보호 업그레이드·전진기지 시설로 대응 가능하게 하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

27. work_process/MVP2/MVP-B 폴더 내에서 work_process/MVP2/MVP-B/K/research.md, work_process/MVP2/MVP-B/K/test.md 파일을 읽고 2차 MVP K 단계의 작업을 수행해줘 K단계의 작업 내용은 드론 `ViewSocket` 위에 World Space 대사 팝업을 표시, 화면 패널에는 추천 행동과 실제 근거 수치를 유지, 긴급 위험은 일반 탐사 대사보다 우선하고 쿨다운을 우회/갱신, 생성형 AI가 없어도 템플릿으로 전체 데모가 동작하도록 하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

28. work_process/MVP2/MVP-B 폴더 내에서 work_process/MVP2/MVP-B/L/research.md, work_process/MVP2/MVP-B/L/test.md 파일을 읽고 2차 MVP L 단계의 작업을 수행해줘 L단계의 작업 내용은 Player health/행동 가능 상태와 실패 조건을 정의,붕괴·가스·전력 고갈을 같은 Run 실패 Orchestrator에 연결, 미정산 화물 30~50% 손실과 드론 구조 업그레이드 보호 효과를 결정론적으로 계산,체크포인트 또는 Surface Base로 안전하게 복귀하도록 하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼

29. work_process/MVP2/MVP-B 폴더 내에서 work_process/MVP2/MVP-B/M/research.md, work_process/MVP2/MVP-B/M/test.md 파일을 읽고 2차 MVP M 단계의 작업을 수행해줘 M단계의 작업 내용은 기본 월드는 Seed와 generatorVersion으로 재생성 및 채굴/변경 타일, 건물, 가스, 붕괴, 발견 구역만 변경점으로 저장, Player/Inventory/Upgrade/Outpost/Run/Drone cooldown을 함께 복원, 복원 순서 후 파생 상태와 UI를 활성화하는거야 unity MCP도 같이 연결해놓은 상태니까 unity editor에서도 같이 작업을 해주도록 하고 작업 내용에 대해서는 init/rule.md의 내용 참고하면서 한국어 주석도 같이 작성해주면서 진행해주면 돼
