# 긴급 탈출 포탈 시설 아트 적용

## 추천 방향과 적용 범위

- 충전기·보건소와 같은 짙은 회색 금속, 노란 코너 가드, 청록색 발광을 사용한다.
- 원형 에너지 링을 사각 금속 아치로 지지하고, 아래쪽에는 경고 무늬와 수평 받침을 둔다.
- 기존 2×2 설치 영역 안에서 원본 비율을 유지한다. 받침의 실제 불투명 픽셀을 지면보다 0.04칸 아래로 맞춘다.
- 포탈 내부에 들어간 캐릭터가 보이도록 그림의 정렬 순서는 3, 기존 캐릭터는 5를 유지한다.
- 새로운 런타임 애니메이션·매 프레임 로직·콜라이더는 추가하지 않는다.

## 변경 대상

- `Assets/_Project/Art/Facilities/MVP/emergency_escape_portal_v1.png`와 Unity 생성 `.meta`: 실제 시설·건설 아이콘·퀘스트에서 공통으로 쓰는 투명 PNG.
- `EmergencyEscapePortal.prefab`: 임시 OuterFrame/PortalField를 VisualRoot/Artwork로 교체. 루트 및 기존 기능 컴포넌트를 보존한다.
- `Building_EmergencyEscapePortal.asset`: 임시 아이콘을 새 포탈 원본으로 교체한다.
- `FacilityGroundedVisual.cs`: 새 포탈의 배치·복원·미리보기에 동일한 크기와 접지 기준을 적용한다.
- `PromptB46EmergencyEscapePortalBuilder.cs` 및 `PromptB47_1EmergencyEscapePortalBuilder.cs`: 전용 갱신 명령과 기존 재생성 경로 모두 새 아트를 사용한다.
- `PromptB1072QuestThumbnailBuilder.cs` 및 `Mine_Demo_Integration.unity`: 긴급 탈출 퀘스트도 같은 원본을 참조한다.
- 관련 EditMode 테스트: 실제 프리팹·메뉴·미리보기·퀘스트와 탈출 기능을 확인한다.

## 보존되는 기능

2×2 점유, 통과 가능한 Trigger, E키 상호작용, 목적지 선택, 100G와 최대 전력 10%의 사용 비용, 건설 자원 비용, 전력망 연결, 저장 ID 및 프리팹 GUID를 유지한다. 이전 턴의 조명·정산 콘솔 및 저장/UI 수정도 함께 보존한다.

## 이미지 출처 및 생성 프롬프트

Built-in image_gen으로 생성한 새 스프라이트를 프로젝트로 복사했다. 원본 PNG는 RGBA 1254×1254이며 모서리 alpha는 0이다. alpha≥64인 실제 그림 영역은 이미지 좌상단 기준 (49,142)–(1205,1114), Unity 좌하단 기준 (49,140)–(1205,1112)이다. 이미지 편집 또는 강제 비율 변형 없이 이 영역을 기준으로 배치한다.

최종 생성 프롬프트:

> Create a single production-ready 2D game facility sprite for the Korean underground mining game Sub-Terra: an EMERGENCY ESCAPE TELEPORTER. Transparent background, isolated object only, no environment, no floor plane, no cast shadow outside the machine, no text or labels, no characters, no icons around it. Match a charming, chunky sci-fi mining-facility asset set with dark gunmetal grey machined panels, saturated golden yellow/orange corner guards and bolts, bright cyan illuminated strips, thick clean black outlines, clear glossy hand-painted cartoon highlights with dimensional shading. Existing equipment has grey/yellow columns, black-yellow hazard striping on the bottom sill, and luminous cyan windows. Make a standing circular cyan energy portal held within a sturdy rectangular arch frame. Clear open-looking portal center: a dark navy translucent energy field with a thin bright cyan inner ring and restrained curved light streaks, not an opaque doorway. Two thick vertical side supports, small golden corner clamps, compact cyan status light at top. Broad, low, perfectly horizontal metal bottom sill with black-yellow hazard striping and two solid level feet. The lowest physical edge of both feet and sill must share one horizontal grounding line. Front view with only a modest visible top/right thickness, matching a side-view platform-mining game's facilities. Nearly square silhouette, slightly taller than wide, intended to fit a 2 by 2 tile footprint. Object fills roughly 85% of a square canvas with a small even transparent margin. Readable at 64 to 96 pixels tall. Do not add broad bloom, huge halos, wisps below the base, cables, spikes, aerial antenna, stairs, floating pieces, photorealism or ground shadows. One sprite only, no sprite sheet. Preserve crisp edges and real alpha transparency.

## 검증

- 전용 빌더 적용 완료. 새 이미지 `.meta` GUID 생성, 포탈 프리팹의 시각 계층 교체, 건설 데이터 아이콘 참조 및 긴급 탈출 퀘스트 이미지 갱신을 확인했다.
- EditMode **21개 / 21개 통과**, 실패 0개. 결과: `work_process/portal-artwork-editmode-20261008.xml`, Editor 종료 코드 0.
- 실제 프리팹을 복제해 복원 초기화한 그림과 Tilemap 기준 설치 미리보기의 스프라이트·좌표·배율 일치를 확인했다.
- 포탈 발 위치 -0.04, 2×2 내부 크기, 비율 유지, 통과 가능한 Trigger, 캐릭터보다 뒤에 표시되는 순서를 확인했다.
- 건설 자원 비용 및 전력 수요, 목적지 선택 후 100G/최대 전력 10% 차감과 이동, 전력망 연결 조건을 검사했다.
- 변경 시설 5종의 미리보기 일치 및 시설 관련 퀘스트 10개의 원본 참조 일치도 회귀 검사했다.
- diff 검사 통과. Unity 자동 변경 ProjectSettings.asset은 실행 전 바이트로 복원했다. Packages/Assets Settings 의미 변경 없음. 기존 폰트 수정(296줄 추가/69줄 삭제)은 보존했다.
- 테스트 Editor 종료 확인. 작업 후 C 드라이브 여유 약 14.73GB.
- 컴파일 오류 없음. 기존 deprecated API 경고와 Unity AI/MCP 실행 파일 서명 관련 패키지 로그는 별도 기존 항목이며 이번 포탈 코드 수정 대상은 아니다.
- 실제 게임 화면의 시각 검수와 Windows 빌드는 실행하지 않았다.

수동 화면 확인은 첫 번째 Unity 프로젝트에서 `Bootstrap → 새 게임/이어하기 → 지하 탐사 → B → 긴급 탈출 포탈` 순서로 한다. 미리보기와 설치 후 그림, 땅에 닿는 받침, 내부 캐릭터 표시, E키 목적지 선택을 확인한다.
