# 인게임 우측 메뉴 UI 리워크 — 접이식 Side Menu

| 항목                      | 내용                                                                                         |
| ------------------------- | -------------------------------------------------------------------------------------------- |
| **문서 제목**             | In-Game Right Side Menu UI Rework                                                            |
| **상태**                  | Implementation Spec                                                                          |
| **기준 컨셉 — 열린 상태** | `menu-concept1.png`                                                                          |
| **기준 컨셉 — 닫힌 상태** | `menu-concept2.png`                                                                          |
| **열린 메뉴 베이스**      | `game-menu.png`                                                                              |
| **일반 메뉴 버튼**        | `menu-button-active-off.png`                                                                 |
| **Hover 메뉴 버튼**       | `menu-button-active-on.png`                                                                  |
| **일반 접기/열기 버튼**   | `menu-close-off.png`                                                                         |
| **Hover 접기/열기 버튼**  | `menu-close-on.png`                                                                          |
| **닫힌 메뉴 베이스**      | `menu-close-state.png`                                                                       |
| **범위**                  | 게임 화면 우측 상단 메뉴 버튼 영역만 리워크                                                  |
| **핵심 목표**             | 기존 6개 메뉴 기능을 그대로 유지하면서 메뉴를 열고 닫을 수 있는 접이식 우측 Side Menu로 변경 |
| **최우선 규칙**           | 작업 시작 전 `rule.md`를 읽고 기존 구조와 기능을 우선한다                                    |

---

## 0. 한 줄 결론

현재 게임 화면 우측 상단에 항상 노출되고 있는 메뉴 버튼 목록을 `menu-concept1.png` / `menu-concept2.png` 형태의 **접이식 Side Menu**로 교체한다.

기본 상태는:

```text
menu-concept1.png
=
메뉴 열린 상태
```

이며,

접기 버튼을 사용하면:

```text
열린 메뉴
→ 오른쪽 화면 밖으로 이동
→ 열린 UI 비활성화
→ 닫힌 UI 상태로 전환
→ 닫힌 메뉴 표시
```

한다.

닫힌 메뉴의 버튼을 다시 누르면 반대로 열린 메뉴로 전환한다.

기존 메뉴 버튼이 수행하던 기능은 **새로 작성하지 않고 기존 기능을 그대로 새 버튼에 연결한다.**

---

# 1. 작업 범위

이번 작업에서 수정하는 영역은 **게임 화면 우측 상단 메뉴 UI만**이다.

현재 게임 화면의 다음 요소는 이번 작업 범위가 아니다.

- 좌측 플레이어 상태 HUD
- 전력 / 연결 상태
- 목표 / 퀘스트 UI
- 시설 건설 창
- 중앙 광산 초기화 Timer
- 플레이어 / 시설 / 월드 오브젝트
- 미니맵
- 구조 주의 표시
- 기타 Gameplay HUD

위 요소는 이번 작업 때문에 위치·크기·색상·기능을 변경하지 않는다.

---

# 2. 작업 전 필수 조사

코드를 수정하기 전에 `rule.md`를 먼저 확인한다.

그 후 현재 프로젝트에서 다음을 조사한다.

```text
현재 우측 메뉴 Hierarchy
시설 버튼 연결 함수
인벤토리 버튼 연결 함수
업그레이드 버튼 연결 함수
게임 가이드 버튼 연결 함수
설정 버튼 연결 함수
게임 종료 버튼 연결 함수

각 버튼의 단축키 처리
현재 Button / Event 연결 구조
Canvas / CanvasScaler 구조
Prefab 여부
Runtime 생성 여부
기존 메뉴 관련 Script
```

기존 기능이 존재하는 상태에서 같은 기능을 새로 구현하지 않는다.

---

# 3. 기준 이미지

## 3-1. `menu-concept1.png`

메뉴가 **열려 있을 때의 최종 화면 정본**이다.

다음의 기준으로 사용한다.

- 우측 메뉴 위치
- 전체 메뉴 크기
- 화면 우측 여백
- 버튼 배치
- 버튼 간격
- 메뉴 프레임 크기
- 접기 버튼 위치
- UI의 전체적인 시각적 균형

---

## 3-2. `menu-concept2.png`

메뉴가 **닫혀 있을 때의 최종 화면 정본**이다.

닫힌 상태에서는 전체 메뉴 버튼 목록을 표시하지 않는다.

화면 오른쪽 가장자리에 작은 Side Menu만 남긴다.

---

# 4. 컨셉 이미지와 구현 이미지 구분

`menu-concept1.png`와 `menu-concept2.png`는 **최종 화면 참고 이미지**다.

실제 게임 UI의 Background로 통째로 사용하지 않는다.

실제 구현에는 다음 에셋을 사용한다.

```text
game-menu.png

menu-button-active-off.png
menu-button-active-on.png

menu-close-off.png
menu-close-on.png

menu-close-state.png
```

---

# 5. 제공 Asset 정보

현재 제공된 이미지 규격은 다음과 같다.

| Asset                        |        크기 |
| ---------------------------- | ----------: |
| `game-menu.png`              |  941 × 1672 |
| `menu-button-active-off.png` |  2048 × 682 |
| `menu-button-active-on.png`  |  2048 × 682 |
| `menu-close-off.png`         | 1254 × 1254 |
| `menu-close-on.png`          | 1254 × 1254 |
| `menu-close-state.png`       |  682 × 2048 |

모두 Alpha가 포함된 PNG이므로 투명도를 유지해서 사용한다.

파일 원본 해상도 그대로 UI 크기로 사용할 필요는 없다.

Unity에서는 컨셉 이미지의 비율과 화면상 크기를 기준으로 축소하여 사용한다.

---

# 6. Asset 배치

제공된 UI 이미지를 현재 프로젝트의 UI Asset 규칙에 맞는 위치로 복사한다.

예:

```text
Assets/_Project/Art/UI/Gameplay/SideMenu/
```

아래와 같은 형태를 권장한다.

```text
SideMenu/
├─ game-menu.png
├─ menu-button-active-off.png
├─ menu-button-active-on.png
├─ menu-close-off.png
├─ menu-close-on.png
└─ menu-close-state.png
```

단, 기존 프로젝트에 UI Asset 폴더 규칙이 있다면 기존 구조를 우선한다.

`rule.md`의 경로 및 변경 범위 규칙을 우선한다.

---

# 7. Sprite Import

모든 이미지의 기본 Import 설정:

```text
Texture Type
Sprite (2D and UI)

Sprite Mode
Single

Mesh Type
Full Rect

Alpha Is Transparency
On
```

UI가 흐려지거나 압축 흔적이 발생하지 않게 품질을 확인한다.

원본 에셋의 Cyan Glow와 외곽선이 훼손되지 않도록 한다.

---

# 8. 열린 상태 구성

메뉴가 열린 상태에서는 다음 요소가 존재한다.

```text
OpenMenuRoot
├─ game-menu.png
│
├─ FacilityButton
├─ InventoryButton
├─ UpgradeButton
├─ GuideButton
├─ SettingsButton
├─ QuitButton
│
└─ MenuToggleButton
```

총:

```text
메뉴 버튼 6개
+
접기 버튼 1개
```

다.

---

# 9. 열린 메뉴 Background

`game-menu.png`를 열린 상태의 메뉴 Background로 사용한다.

컨셉 이미지처럼 게임 화면 우측 상단에 배치한다.

원본 비율을 심하게 왜곡하지 않는다.

화면의 오른쪽 경계와 자연스럽게 연결되도록 배치한다.

---

# 10. 메뉴 버튼 수

열린 상태의 메뉴 버튼은 정확히 **6개**다.

기존 메뉴 버튼과 동일하다.

순서:

```text
시설 [B]

인벤토리 [I]

업그레이드 [U]

게임 가이드 [G]

설정 [Esc]

게임 종료 [Q]
```

기존 순서를 유지한다.

---

# 11. 메뉴 버튼 기본 이미지

모든 6개 메뉴 버튼의 Normal 상태에는 동일한:

```text
menu-button-active-off.png
```

를 사용한다.

버튼마다 별개의 디자인을 만들지 않는다.

크기와 외형은 6개 모두 동일해야 한다.

차이는:

- 아이콘
- 텍스트
- 단축키 표시

뿐이다.

---

# 12. 메뉴 버튼 Hover 이미지

마우스 Pointer가 버튼 위에 올라가면:

```text
menu-button-active-on.png
```

상태로 전환한다.

컨셉 이미지처럼:

- 외곽 Cyan Light 강화
- 내부 Cyan Glow 강화
- 약한 Particle 표현

이 나타나야 한다.

---

# 13. Hover 전환 시간

모든 일반 메뉴 버튼의 Hover Transition은:

```text
0.3초
```

로 통일한다.

권장 동작:

```text
Pointer Enter
menu-button-active-off
→ 0.3초
→ menu-button-active-on
```

Pointer Exit:

```text
menu-button-active-on
→ 0.3초
→ menu-button-active-off
```

즉시 Sprite를 딱 교체하는 것보다 0.3초의 자연스러운 전환을 구현한다.

---

# 14. Hover 구현

필요하면 두 개의 `Image`를 겹쳐서 Cross Fade하는 방식을 사용할 수 있다.

예:

```text
NormalImage
HoverImage
```

Normal 상태:

```text
Normal Alpha = 1
Hover Alpha = 0
```

Hover:

```text
0.3초 동안

Normal Alpha 1 → 0
Hover Alpha 0 → 1
```

Pointer Exit에서는 반대로 한다.

프로젝트에 기존 Tween 시스템이 있다면 그것을 재사용한다.

단순 Hover 하나를 위해 외부 Tween Package를 새로 설치하지 않는다.

---

# 15. 메뉴 버튼 기능

각 새 메뉴 버튼은 기존 버튼이 사용하던 실제 기능에 그대로 연결한다.

| 새 버튼     | 기존 기능             |
| ----------- | --------------------- |
| 시설        | 기존 시설 창 열기     |
| 인벤토리    | 기존 인벤토리 열기    |
| 업그레이드  | 기존 업그레이드 열기  |
| 게임 가이드 | 기존 게임 가이드 열기 |
| 설정        | 기존 설정 열기        |
| 게임 종료   | 기존 게임 종료 동작   |

기능을 복제하지 않는다.

기존 Method 또는 Controller를 새 버튼에서 호출한다.

---

# 16. 단축키

기존 단축키:

```text
B
I
U
G
Esc
Q
```

의 동작을 그대로 유지한다.

UI를 접었다고 해서 단축키 기능까지 비활성화하지 않는다.

즉 메뉴가 닫혀 있어도 기존 단축키는 정상 작동해야 한다.

---

# 17. 접기/열기 버튼

열린 상태 메뉴 우측 상단에는:

```text
MenuToggleButton
```

하나를 둔다.

Normal:

```text
menu-close-off.png
```

Hover:

```text
menu-close-on.png
```

을 사용한다.

---

# 18. 접기 버튼 위치

접기 버튼은 `menu-concept1.png`를 기준으로:

- 열린 메뉴의 가장 오른쪽
- 가장 위쪽
- Menu Frame 내부와 자연스럽게 붙은 위치

에 배치한다.

Menu Frame과 버튼 사이가 떨어져 별개의 UI처럼 보이면 안 된다.

---

# 19. 접기 버튼 Hover

접기 버튼도 일반 메뉴 버튼과 동일하게:

```text
0.3초
```

Hover Transition을 사용한다.

```text
menu-close-off.png
→ 0.3초
→ menu-close-on.png
```

Pointer Exit에서는 반대로 전환한다.

---

# 20. 닫기 동작

열린 상태에서 접기 버튼을 클릭하면 다음 순서로 동작한다.

```text
OPEN
↓
현재 열린 메뉴 전체를 오른쪽으로 이동
↓
0.5초 이내 화면 밖으로 완전히 이동
↓
OpenMenuRoot 비활성화
↓
ClosedMenuRoot 활성화
↓
화면 오른쪽에서 닫힌 메뉴 등장
↓
0.3초 이내 최종 위치 도달
↓
CLOSED
```

---

# 21. 중요한 Transition 원칙

열린 UI와 닫힌 UI를 화면 위에서 즉시 교체하지 않는다.

반드시:

```text
현재 UI가 화면 바깥으로 빠져나감
↓
UI State 교체
↓
새 State가 등장
```

순서로 처리한다.

이 방식으로 두 UI가 순간적으로 겹쳐 보이는 것을 방지한다.

---

# 22. 열린 메뉴 Exit Animation

접기 버튼 클릭 시 열린 메뉴 전체가:

```text
오른쪽 방향
```

으로 이동한다.

Duration:

```text
최대 0.5초
```

권장:

```text
0.35 ~ 0.5초
```

움직임은 부드럽지만 지나치게 느리지 않게 한다.

---

# 23. 열린 메뉴 Exit 완료 판정

다음 조건 이후에만 상태를 교체한다.

```text
OpenMenu의 Left Edge
>
Game View의 Right Edge
```

즉 플레이어가 메뉴의 일부가 남은 상태에서 UI가 갑자기 바뀌는 것을 보지 않게 한다.

---

# 24. 닫힌 상태 구성

닫힌 상태에서는 다음만 존재한다.

```text
ClosedMenuRoot
├─ menu-close-state.png
└─ MenuToggleButton
```

메뉴 버튼 6개는 표시하지 않는다.

---

# 25. 닫힌 메뉴 Background

`menu-close-state.png`를 사용한다.

컨셉:

```text
menu-concept2.png
```

처럼 게임 화면 오른쪽 경계에 붙인다.

목적은 메뉴가 닫혀 있어도:

> “여기에 열 수 있는 메뉴가 있다.”

는 것을 자연스럽게 보여주는 것이다.

---

# 26. 닫힌 상태 버튼

닫힌 상태에서도 MenuToggleButton을 제공한다.

Normal:

```text
menu-close-off.png
```

Hover:

```text
menu-close-on.png
```

을 사용한다.

Hover duration은 동일하게:

```text
0.3초
```

다.

---

# 27. 메뉴 다시 열기

닫힌 상태에서 Toggle Button을 클릭하면:

```text
CLOSED
↓
현재 닫힌 메뉴를 오른쪽으로 이동
↓
0.5초 이내 화면 밖으로 완전히 이동
↓
ClosedMenuRoot 비활성화
↓
OpenMenuRoot 활성화
↓
열린 메뉴가 오른쪽 화면 밖에서 등장
↓
0.3초 이내 원래 위치에 도달
↓
OPEN
```

한다.

---

# 28. Open / Close 상태 머신

구현에서는 단순 Boolean보다 Transition 상태를 분리하는 것을 권장한다.

예:

```text
Closed
Opening
Open
Closing
```

이를 통해 Animation 중 중복 입력을 방지한다.

---

# 29. Transition 중 입력

메뉴가:

```text
Opening
또는
Closing
```

상태일 때 Toggle Button을 연속 클릭해도 Animation이 중첩되지 않게 한다.

예:

```text
if (isTransitioning)
    return;
```

또는 프로젝트 스타일에 맞는 상태 제어를 사용한다.

---

# 30. 빠른 연속 클릭 방지

다음 문제가 발생하면 안 된다.

```text
Click
Click
Click
→ Open / Closed UI가 중복 생성
```

또는:

```text
Panel이 중간 위치에 멈춤
```

또는:

```text
Open과 Closed UI가 동시에 표시
```

Transition 완료까지 추가 Toggle 입력을 무시한다.

---

# 31. 메뉴 기본 상태

게임 화면에 처음 진입했을 때 기본값은:

```text
OPEN
```

이다.

즉:

```text
menu-concept1.png
```

상태로 시작한다.

별도의 요구사항이 없다면 메뉴의 열린/닫힌 상태를 Save Data에 저장하지 않는다.

게임 세션 UI 상태로만 관리한다.

---

# 32. UI Hierarchy 권장안

기존 구조에 맞게 이름은 변경 가능하지만 역할은 다음과 같이 분리하는 것을 권장한다.

```text
GameplaySideMenu
│
├─ OpenMenuRoot
│  ├─ MenuBackground
│  ├─ FacilityButton
│  ├─ InventoryButton
│  ├─ UpgradeButton
│  ├─ GuideButton
│  ├─ SettingsButton
│  ├─ QuitButton
│  └─ ToggleButton
│
└─ ClosedMenuRoot
   ├─ ClosedMenuBackground
   └─ ToggleButton
```

Toggle Button의 Event 로직은 공통 Controller에서 처리한다.

---

# 33. 추천 Controller

기존 UI 관리 코드가 있다면 그것을 우선 사용한다.

새 Controller가 필요할 경우 역할을 제한한다.

예:

```text
GameplaySideMenuController
```

책임:

```text
Open / Close 상태 관리
Slide Animation
Toggle 입력
Transition Lock
OpenMenuRoot / ClosedMenuRoot 활성화
```

다음 기능은 이 Controller에 넣지 않는다.

```text
Inventory Logic
Facility Logic
Upgrade Logic
Settings Logic
Quit Logic
```

기존 기능에 연결만 한다.

---

# 34. Button View 역할

필요한 경우 메뉴 버튼 Hover 효과만 별도 Component로 분리한다.

예:

```text
SideMenuButtonView
```

책임:

```text
PointerEnter
PointerExit
Normal / Hover Image Fade
```

게임 기능 호출은 기존 Button Event를 사용한다.

---

# 35. UI Anchor

Side Menu 전체는 화면 우측 상단에 Anchor한다.

예:

```text
Anchor
Top Right
```

Animation 역시 Absolute Screen Position 대신 Anchor 기준 RectTransform 위치를 사용한다.

---

# 36. 해상도 대응

최소 다음 환경에서 확인한다.

```text
1920 × 1080

2560 × 1440

1366 × 768
```

화면 크기가 달라져도:

- 열린 메뉴가 우측 상단에 유지
- 닫힌 메뉴가 우측 경계에 유지
- Animation이 화면 밖까지 정상 이동

해야 한다.

---

# 37. Off-screen 위치 계산

화면 밖 이동 거리를 고정 Pixel 값만으로 하드코딩하지 않는 것을 권장한다.

가능하면:

```text
Panel Width
+
Safe Margin
```

으로 계산한다.

예:

```text
offscreenX =
panel.rect.width + margin
```

방식.

해상도 또는 UI Scale 변화에도 완전히 화면 밖으로 나가야 한다.

---

# 38. Button Image 처리

메뉴 버튼 Background는 제공된 에셋을 그대로 사용한다.

Agent가 다음을 임의로 하지 않는다.

- 모서리 형태 수정
- Cyan 색상 변경
- Glow 스타일 재해석
- 버튼 비율 변경
- 버튼마다 서로 다른 모양 적용
- 추가 프레임 생성

6개 버튼은 **동일한 외형**을 유지한다.

---

# 39. 버튼 텍스트

텍스트는 이미지에 구워 넣지 않는다.

TextMeshPro를 사용한다.

예:

```text
시설 [B]
인벤토리 [I]
업그레이드 [U]
게임 가이드 [G]
설정 (Esc)
게임 종료 (Q)
```

표현 방식은 기존 버튼과 컨셉 이미지의 표기를 기준으로 한다.

---

# 40. 메뉴 아이콘

컨셉 이미지에 존재하는 각 기능 아이콘을 유지한다.

예:

```text
시설
인벤토리
업그레이드
게임 가이드
설정
게임 종료
```

프로젝트 내에 이미 적절한 아이콘 Asset이 존재한다면 재사용한다.

없다면 컨셉 이미지와 **동일한 스타일을 유지하는 범위에서만** 필요한 아이콘을 추가한다.

아이콘 때문에 버튼 자체 디자인을 변경하지 않는다.

---

# 41. Hover Particle

`menu-button-active-on.png`에 표현된 Cyan Particle / Glow가 이미 이미지 자체에 포함되어 있다면 그것을 기본으로 사용한다.

추가 Runtime Particle은 **필수 요구사항이 아니다.**

즉:

```text
이미지 자체 효과가 충분함
→ 추가 Particle System 만들지 않음
```

실제 화면에서 효과가 부족하다고 판단되는 경우에만 별도 검토한다.

---

# 42. 다른 UI와의 관계

Side Menu가 열린 상태라고 해서 다른 Gameplay UI의 위치를 밀어내지 않는다.

즉:

```text
Side Menu
=
Overlay UI
```

로 처리한다.

미니맵 또는 기존 Gameplay HUD Layout을 Side Menu Width에 맞춰 동적으로 이동시키지 않는다.

이번 작업은 우측 메뉴 교체만 수행한다.

---

# 43. 메뉴 버튼 클릭 후 Side Menu

기존 메뉴 버튼 클릭 시 해당 기능창을 여는 동작은 유지한다.

Side Menu를 자동으로 접을지는 **현재 요구사항에 포함하지 않는다.**

기존 메뉴 사용 흐름을 먼저 유지한다.

추후 Gameplay UI 전면 리워크 과정에서 필요하면 별도 결정한다.

---

# 44. UI Animation

이번 작업의 핵심 Animation은 두 종류뿐이다.

### Hover

```text
Duration
0.3초
```

### Open / Close Slide

```text
현재 State Exit
≤ 0.5초

새 State Enter
≤ 0.3초
```

불필요한 Scale Bounce, Rotation, Flash 등의 추가 Animation은 넣지 않는다.

---

# 45. Animation Easing

가능하면 Linear보다 부드러운 Ease를 사용한다.

예:

```text
EaseInOut
```

또는 기존 프로젝트의 UI Animation 규칙.

다만 메뉴가 너무 느리고 무거워 보이지 않게 한다.

Sub-Terra UI 특성상:

> 기계 패널이 빠르고 안정적으로 이동하는 느낌

을 우선한다.

---

# 46. Sound

이번 요구사항에는 새로운 Sound 추가가 포함되어 있지 않다.

기존 Button UI Sound 시스템이 있으면 그대로 연결할 수 있다.

새 오디오 시스템을 만들지 않는다.

---

# 47. 기존 기능 보존

이번 작업은 다음을 변경하기 위한 것이 아니다.

```text
시설 시스템
인벤토리 시스템
업그레이드 시스템
게임 가이드 시스템
Settings 시스템
Quit 시스템
단축키 시스템
Save 시스템
```

오직:

```text
해당 기능을 실행하는 우측 메뉴 UI
```

만 교체한다.

---

# 48. 하지 않을 것

다음은 금지한다.

- 우측 메뉴 외 Gameplay HUD 수정
- 새로운 메뉴 기능 추가
- 기존 메뉴 기능 제거
- Button 순서 변경
- 기존 단축키 변경
- Save Schema 변경
- Main Menu 수정
- Settings UI 수정
- Minimap 수정
- 시설창 수정
- Font Asset 임의 수정
- 공용 TMP 설정 임의 수정
- 제공된 UI Asset 디자인 재생성
- 메뉴를 임의의 다른 Layout으로 변경

---

# 49. 구현 순서

## Phase 1 — 조사

```text
rule.md 확인
↓
현재 Side Menu 구조 확인
↓
기존 버튼 Event 확인
↓
기존 단축키 확인
↓
관련 Prefab / Scene 확인
```

아직 UI 수정하지 않는다.

---

## Phase 2 — Asset Import

```text
6종 Asset을 적절한 Assets 경로에 복사
↓
Sprite Import
↓
Alpha / 품질 확인
```

---

## Phase 3 — Open State

먼저 `menu-concept1.png`를 기준으로:

```text
game-menu.png
+
6 menu buttons
+
1 toggle button
```

을 배치한다.

기능 연결 전에 Visual Layout부터 맞춘다.

---

## Phase 4 — Button Function

기존 기능을 6개 버튼에 연결한다.

모든 단축키와 버튼을 확인한다.

---

## Phase 5 — Hover

```text
menu-button-active-off
↔
menu-button-active-on
```

과:

```text
menu-close-off
↔
menu-close-on
```

전환을 구현한다.

Duration:

```text
0.3초
```

---

## Phase 6 — Closed State

`menu-close-state.png`를 사용하여:

```text
menu-concept2.png
```

와 동일한 위치 및 크기로 닫힌 UI를 구성한다.

Toggle Button을 연결한다.

---

## Phase 7 — Transition

```text
Open
→ Closed

Closed
→ Open
```

Slide Animation 및 State Swap을 구현한다.

---

## Phase 8 — 실제 Unity 검증

실행 중인 Unity Editor에서 직접 확인한다.

특히:

```text
Panel Position
Button Position
Hover
Transition
각 기능 연결
해상도 변화
```

를 검증한다.

---

# 50. 기능 QA

| ID    | 내용                                |
| ----- | ----------------------------------- |
| M-U01 | 게임 진입 시 메뉴가 열린 상태       |
| M-U02 | 시설 버튼 기존 기능 정상            |
| M-U03 | 인벤토리 버튼 기존 기능 정상        |
| M-U04 | 업그레이드 버튼 기존 기능 정상      |
| M-U05 | 게임 가이드 버튼 기존 기능 정상     |
| M-U06 | 설정 버튼 기존 기능 정상            |
| M-U07 | 게임 종료 버튼 기존 기능 정상       |
| M-U08 | 기존 단축키 모두 정상               |
| M-U09 | 메뉴가 닫혀 있어도 기존 단축키 정상 |
| M-U10 | 메뉴 Toggle 정상                    |

---

# 51. Hover QA

| ID    | 내용                                          |
| ----- | --------------------------------------------- |
| M-H01 | 모든 일반 버튼 Normal 이미지 동일             |
| M-H02 | Hover 시 ON 이미지 정상                       |
| M-H03 | Hover 진입 Transition 약 0.3초                |
| M-H04 | Hover 해제 Transition 약 0.3초                |
| M-H05 | Toggle Button Hover 정상                      |
| M-H06 | 빠르게 Pointer 이동해도 이미지 상태 꼬임 없음 |

---

# 52. Animation QA

| ID    | 내용                                      |
| ----- | ----------------------------------------- |
| M-A01 | Open → Closed 시 오른쪽 방향 이동         |
| M-A02 | Open Menu가 0.5초 이내 화면 밖으로 이동   |
| M-A03 | 완전히 사라진 뒤 State 교체               |
| M-A04 | Closed Menu가 0.3초 이내 정상 위치 도달   |
| M-A05 | Closed → Open도 동일 구조로 정상          |
| M-A06 | Transition 중 연속 클릭 차단              |
| M-A07 | Open / Closed UI 동시 노출 없음           |
| M-A08 | Animation 종료 후 정확한 Anchor 위치 유지 |

---

# 53. Visual QA

다음을 `menu-concept1.png`와 직접 비교한다.

```text
메뉴 전체 위치
메뉴 전체 크기
6개 버튼 크기
버튼 간격
아이콘 위치
Text 위치
Toggle 위치
우측 화면 여백
Cyan Brightness
```

닫힌 상태는 `menu-concept2.png`와 비교한다.

---

# 54. 해상도 QA

최소:

```text
1920 × 1080
2560 × 1440
1366 × 768
```

에서 확인한다.

특히 화면 밖 이동 Animation이 Resolution에 관계없이 정상 작동해야 한다.

---

# 55. 실패 조건

다음 중 하나라도 발생하면 완료가 아니다.

- 6개 메뉴 버튼 중 기존 기능 하나라도 동작하지 않음.
- 단축키가 끊김.
- 메뉴를 닫은 후 다시 열 수 없음.
- Animation 중 메뉴가 겹침.
- 메뉴가 화면 중간에 멈춤.
- 버튼 Hover가 서로 다른 형태로 보임.
- 6개 버튼의 크기가 다름.
- Toggle Button 위치가 컨셉과 다름.
- 닫힌 메뉴가 화면에서 잘림.
- 해상도 변경 후 메뉴 위치가 어긋남.
- 이번 요청과 관계없는 HUD가 변경됨.

---

# 56. 작업 체크리스트

1. [ ] `rule.md` 확인
2. [ ] 기존 우측 메뉴 구조 조사
3. [ ] 기존 6개 버튼 Event 조사
4. [ ] 기존 단축키 확인
5. [ ] 제공 Asset 프로젝트로 복사
6. [ ] Sprite Import 설정
7. [ ] Open Menu Background 배치
8. [ ] 메뉴 버튼 6개 배치
9. [ ] Toggle Button 배치
10. [ ] 버튼 Text / Icon 배치
11. [ ] 기존 기능 연결
12. [ ] Menu Button Hover 구현
13. [ ] Toggle Button Hover 구현
14. [ ] Closed Menu 구성
15. [ ] Open → Closed Transition
16. [ ] Closed → Open Transition
17. [ ] Transition Lock 구현
18. [ ] 해상도 대응
19. [ ] 실제 Unity Game View 검증
20. [ ] 변경 범위 확인
21. [ ] `git status`
22. [ ] `git diff --stat`
23. [ ] 범위 밖 변경 파일 0개 확인

---

# 57. 커밋 전 범위 확인

`rule.md`에 따라 커밋 전에 반드시:

```text
git status
git diff --stat
```

를 확인한다.

이번 작업과 관련된:

```text
Side Menu UI
관련 Script
관련 Prefab / Scene
이번에 추가된 UI Asset
```

외 파일이 수정되어 있으면 원인을 확인한다.

특히 공용 Font / TMP / ProjectSettings / 관계없는 Scene 또는 Prefab 변경을 그대로 커밋하지 않는다.

---

# 58. 작업 결과 보고

완료 후 다음을 보고한다.

```text
1. 추가한 UI Asset 경로

2. 수정한 Prefab / Scene

3. 수정한 Script

4. 신규 Script

5. 기존 버튼 기능 중 재사용한 부분

6. Open / Closed Animation 구현 방식

7. Hover 구현 방식

8. 테스트 결과

9. 기존 시스템 변경 여부

10. git status / diff 기준 최종 변경 파일

11. 실제 Unity Game View
    - Open 상태 Screenshot
    - Closed 상태 Screenshot
```

---

# 59. 최종 구현 판단 원칙

이번 작업은 **새로운 메뉴를 디자인하는 작업이 아니다.**

시각적 정본은:

```text
menu-concept1.png
menu-concept2.png
```

이다.

실제 구현 Asset은:

```text
game-menu.png

menu-button-active-off.png
menu-button-active-on.png

menu-close-off.png
menu-close-on.png

menu-close-state.png
```

이다.

따라서 Agent는 더 좋아 보인다는 이유로:

- 프레임 디자인 변경
- 버튼 디자인 변경
- 메뉴 구조 변경
- 메뉴 버튼 수 변경
- 버튼 크기 개별 변경
- Cyan 계열 색상 재해석

을 하지 않는다.

---

# 60. 최종 지시

`menu-concept1.png`를 **열린 상태의 최종 목표 화면**, `menu-concept2.png`를 **닫힌 상태의 최종 목표 화면**으로 사용한다.

열린 상태에서는:

```text
game-menu.png

+
동일한 크기의 메뉴 버튼 6개

+
Toggle Button 1개
```

를 사용한다.

메뉴 버튼은:

```text
menu-button-active-off.png
↔
menu-button-active-on.png
```

으로 0.3초 Hover Transition을 구현한다.

Toggle Button은:

```text
menu-close-off.png
↔
menu-close-on.png
```

으로 0.3초 Hover Transition을 구현한다.

메뉴를 닫을 때:

```text
Open Menu
→ 오른쪽으로 Slide
→ 0.5초 이내 화면 밖
→ State 교체
→ Closed Menu
→ 0.3초 이내 화면 오른쪽 최종 위치
```

로 구현한다.

메뉴를 다시 열 때도 동일한 구조를 반대로 수행한다.

닫힌 상태에서는:

```text
menu-close-state.png
+
Toggle Button 1개
```

만 표시한다.

**기존 6개 메뉴 버튼의 기능과 기존 단축키는 그대로 재사용하며, 이번 UI 리워크를 이유로 Gameplay 기능을 새로 작성하지 않는다.**

그리고 이번 작업 외의 HUD와 게임 화면 요소는 수정하지 않는다.
