# Unity 프로젝트·Scene·Prefab·시각 에셋 규칙

5. Unity Scene과 Prefab 규칙
   5-1. Scene 소유권
   - 담당자 A는 `Scenes/Test/Gameplay/`의 독립 Test Scene에서 기능을 구현한다.
   - 담당자 B는 Bootstrap, MainMenu, SurfaceBase와 Integration Scene을 관리한다.
   - `Mine_Demo_Integration.unity`는 담당자 B만 수정한다.
   - 담당자 A는 통합이 필요한 경우 Runtime Prefab, 설치 지침, 참조 목록과 검증 절차를 전달한다.

   5-2. Prefab 소유권
   - Player, Runtime Building, GasZone과 DiggerBot Runtime Prefab은 담당자 A가 관리한다.
   - HUD, 메뉴, 패널과 DiggerBot View Prefab은 담당자 B가 관리한다.
   - 다른 담당자의 Prefab 내부를 직접 수정하지 않고 Prefab Variant 또는 `ViewSocket`을 활용한다.
   - 기능 Prefab의 시각 요소는 `VisualRoot` 아래에 분리한다.

   5-3. Unity 직렬화 파일
   - Scene과 Prefab은 Force Text Serialization을 사용한다.
   - Unity 파일은 `.meta`와 함께 Unity Editor 안에서 이동하거나 이름을 변경한다.
   - `.meta`를 누락하거나 GUID가 바뀌는 이동을 하지 않는다.
   - Scene과 Prefab YAML을 무리하게 직접 편집하지 않고 Editor API 또는 Unity Editor를 사용한다.

   5-4. 공용 설정
   - `ProjectSettings/*`, `Packages/manifest.json`, `Packages/packages-lock.json`은 합의 없이 수정하지 않는다.
   - Unity 엔진 버전과 패키지 버전을 작업 중 임의로 올리지 않는다.
   - Shared 계약 변경은 별도 Issue와 별도 PR로 먼저 합의·병합한다.
   - 공용 설정 변경은 두 개발자 환경과 Windows 빌드에서 검증한다.

   5-5. Integration Scene 실행 및 바인딩 검증 규칙
   - 테스트 재생(`Play`)은 항상 `Bootstrap` 씬부터 시작하며, `Integration` 씬 단독 재생을 금지한다.
   - 콘솔에 아래 예외/경고 발생 시 입력 장치 버그가 아니라 초기화/시스템 활성화 실패로 진단한다:
     - `Integration scene opened without the Bootstrap runtime`
     - `Integration UI kept disabled: IsUiReady never became true`
     - `MissingReferenceException` / `NullReferenceException` in `IntegrationRuntimeBinder`
   - UI/레이아웃 빌더 스크립트로 씬·프리팹을 재생성한 후에는 `ApplicationRoot`의 `IntegrationRuntimeBinder` Inspector에서 `hudCanvasGroup`, `deferredInputBehaviours`, 패널 바인더 참조를 직접 확인한 뒤 커밋한다.
   - `Mine_Demo_Integration.unity` 등 거대 Scene YAML이 변경되는 커밋은 일반 기능 커밋과 씬 재배선 커밋을 분리하고, Pull 직후 담당자가 "Bootstrap → 새 게임 → 탐사" 스모크 테스트를 실시한다.

7-3. 시각 요소와 그레이박스
   - 핵심 루프 검증 전에는 Primitive Sprite, 단색 타일, 단순 PNG, 텍스트와 Light 2D를 사용한다.
   - 색상만으로 요소를 구분하지 않고 모양이나 무늬를 함께 다르게 한다.
   - 정식 아트는 `VisualRoot` 내부 교체로 적용하고 게임 로직이 Sprite 이름이나 파일명에 의존하지 않게 한다.
   - 정식 에셋, 애니메이션과 사운드는 핵심 루프가 검증된 뒤 추가한다.
