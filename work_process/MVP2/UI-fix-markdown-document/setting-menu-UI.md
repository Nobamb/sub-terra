# 설정창 UI 리워크 — `setting-menu.png` 컨셉 구현

| 항목                   | 내용                                                                                                                      |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| **문서 제목**          | Settings Menu UI Rework                                                                                                   |
| **상태**               | Implementation Spec                                                                                                       |
| **기준 컨셉 이미지**   | `work_process/MVP2/UI-fix-markdown-document/concept-image/setting-menu.png`                                               |
| **기준 레이아웃 에셋** | `work_process/MVP2/UI-fix-markdown-document/concept-image/setting-menu-asset.png`                                         |
| **범위**               | 기존 설정 기능을 유지하면서 설정창 UI를 컨셉 이미지와 최대한 동일하게 재구성                                              |
| **핵심 방식**          | `setting-menu-asset.png`를 설정창의 시각적 베이스로 사용하고, 텍스트·Slider·Dropdown·Toggle·Button은 Unity UI로 별도 배치 |
| **기능 원칙**          | 기존 설정 데이터·적용·취소·기본값·키 설정 동작을 새로 구현하지 않고 기존 기능에 연결                                      |
| **최우선 규칙**        | 작업 시작 전 `rule.md`를 읽고 프로젝트 규칙과 기존 구조를 우선한다                                                        |

---

## 0. 한 줄 결론

현재 설정창의 기능은 유지하되 기존의 단순한 사각형 UI를 제거하고, 제공된 `setting-menu-asset.png`를 설정창의 기본 레이아웃으로 사용하여 `setting-menu.png`와 최대한 동일한 형태로 재구성한다.

구현 방식은 다음과 같다.

```text
setting-menu-asset.png
+
TextMeshPro 텍스트
+
기존 기능에 연결된 Slider
+
Dropdown
+
Toggle
+
Button
```

`setting-menu-asset.png` 안에 이미 존재하는 프레임, 구분선, 아이콘, 내부 패널 표현은 **다시 코드나 별도 이미지로 재현하지 않는다.**

반대로 실제 값이 바뀌거나 사용자가 조작해야 하는 UI를 이미지에 구워 넣지 않는다.

판정 문장:

> “설정창을 열었을 때 `setting-menu.png`와 거의 같은 구도와 분위기가 나타나며, 기존 설정 기능은 이전과 동일하게 정상 작동한다.”

이 문장이 성립해야 완료다.

---

# 1. 작업 전 필수 확인

코드를 수정하기 전에 다음을 먼저 확인한다.

1. `rule.md`
2. 현재 Main Menu Scene 및 Settings UI Hierarchy
3. 기존 설정창 Prefab 또는 Runtime 생성 코드
4. 마스터 볼륨 처리 코드
5. 해상도 변경 코드
6. Frame 설정 코드
7. 화면 진동 억제 설정 코드
8. 언어 변경 코드
9. 키 조작 변경 UI 연결 코드
10. 적용 / 취소 / 기본값 동작
11. 설정 저장 및 로드 방식
12. Main Menu에서 설정창을 여닫는 코드

**조사 전 새 Settings 시스템을 만들지 않는다.**

기존 구현이 존재하면 반드시 그것을 재사용한다.

---

# 2. 기준 이미지의 역할

두 이미지는 역할이 다르다.

## 2-1. `setting-menu.png`

최종 화면의 **시각적 정본**이다.

다음 항목의 기준으로 사용한다.

- 전체 설정창 크기
- 화면 중앙 위치
- 섹션 간 간격
- 텍스트 위치
- Slider 위치
- Dropdown 위치
- Toggle 위치
- 버튼 위치
- 여백
- 색상 관계
- 시각적 위계

구현 결과는 가능한 범위에서 이 이미지와 동일하게 맞춘다.

---

## 2-2. `setting-menu-asset.png`

실제 Unity 프로젝트에 넣어 사용할 **설정창 베이스 이미지**다.

이 이미지에는 이미 다음이 포함되어 있다.

- 전체 설정창 배경
- 청록색 외곽 프레임
- 내부 금속/터미널 질감
- 상단 장식선
- 오디오 섹션 패널
- 오디오 아이콘
- 디스플레이 섹션 패널
- 디스플레이 아이콘
- 시스템 섹션 패널
- 시스템 아이콘
- 조작 섹션 패널
- 조작 아이콘
- 하단 구분선

따라서 해당 요소를 다시 생성하지 않는다.

---

# 3. Asset 배치

`setting-menu-asset.png`를 Unity 프로젝트 안의 기존 UI Art 구조에 맞는 위치로 복사한다.

권장 예:

```text
Assets/_Project/Art/UI/MainMenu/Settings/setting-menu-asset.png
```

또는:

```text
Assets/_Project/Art/UI/Settings/setting-menu-asset.png
```

단, 이미 프로젝트에 UI 이미지 저장 규칙이 존재하면 **기존 구조를 우선한다.**

`rule.md`의 폴더 규칙을 위반하면서 새 경로를 만들지 않는다.

---

# 4. Asset Import

`setting-menu-asset.png`는 UI Sprite로 사용한다.

권장:

```text
Texture Type: Sprite (2D and UI)
Sprite Mode: Single
Mesh Type: Full Rect
Alpha Is Transparency: On
Compression: None 또는 High Quality
Filter Mode: Bilinear
```

이미지는 실제 투명 배경을 가지고 있으므로 알파 채널을 유지한다.

원본 비율을 임의로 찌그러뜨리지 않는다.

---

# 5. 이미지 비율

현재 제공된 파일 기준:

```text
setting-menu.png
1672 × 941

setting-menu-asset.png
1448 × 1086
```

`setting-menu-asset.png`를 컨셉 이미지와 동일한 설정창 비율로 보이게 하기 위해 **무작정 X/Y Stretch 하지 않는다.**

Unity에서 실제 화면에 배치한 후 컨셉 이미지의 설정창 영역과 비교하면서 RectTransform 크기를 결정한다.

목표는 파일의 원본 픽셀 수가 아니라 **게임 화면에서 보이는 최종 비율**이다.

---

# 6. 전체 화면 구조

권장 Hierarchy는 다음과 같다.

기존 구조가 있다면 이름을 강제로 바꾸지 않고 역할만 동일하게 맞춘다.

```text
SettingsRoot
├─ Backdrop
├─ SettingsPanel
│  ├─ LayoutBackground
│  │  └─ setting-menu-asset.png
│  │
│  ├─ Header
│  │  ├─ Title
│  │  ├─ Subtitle
│  │  └─ CloseButton
│  │
│  ├─ AudioSection
│  │  ├─ SectionTitle
│  │  ├─ MasterVolumeLabel
│  │  ├─ MasterVolumeSlider
│  │  └─ MasterVolumeValue
│  │
│  ├─ DisplaySection
│  │  ├─ SectionTitle
│  │  ├─ ResolutionLabel
│  │  ├─ ResolutionDropdown
│  │  ├─ FrameLabel
│  │  ├─ FrameDropdown
│  │  ├─ ScreenShakeLabel
│  │  └─ ScreenShakeToggle
│  │
│  ├─ SystemSection
│  │  ├─ SectionTitle
│  │  ├─ LanguageLabel
│  │  └─ LanguageDropdown
│  │
│  ├─ ControlsSection
│  │  ├─ SectionTitle
│  │  └─ KeyBindingButton
│  │
│  └─ Footer
│     ├─ DefaultButton
│     ├─ CancelButton
│     └─ ApplyButton
```

---

# 7. 배경 Dim

설정창이 열리면 기존 Main Menu는 유지하되 뒤쪽 화면을 어둡게 한다.

컨셉 이미지처럼:

```text
Main Menu
↓
Dark semi-transparent overlay
↓
Settings Panel
```

방식으로 표시한다.

권장 알파 범위:

```text
Black
Alpha 0.45 ~ 0.65
```

정확한 값은 실제 화면을 보고 조절한다.

배경을 완전히 검게 가리지 않는다.

광산과 봉인 구조물이 여전히 희미하게 보여야 한다.

---

# 8. Settings Panel

`setting-menu-asset.png`를 설정창의 최하위 시각 요소로 사용한다.

패널 위에 별도의 네모난 기존 Settings Background가 남아 있으면 제거하거나 비활성화한다.

즉 다음 형태가 되면 안 된다.

```text
기존 사각형 Panel
↓
setting-menu-asset.png
```

불필요한 기존 배경이 겹쳐서 컨셉 이미지의 투명감과 테두리를 죽이지 않게 한다.

---

# 9. Header

컨셉 이미지 기준 상단 중앙.

표시:

```text
설정
```

그 아래:

```text
SYSTEM CONFIGURATION
```

을 작은 Secondary Text로 표시한다.

둘 다 이미지에 구워 넣지 않는다.

TextMeshPro를 사용한다.

---

# 10. 닫기 버튼

컨셉 이미지 우측 상단의 `X` 버튼을 실제 Unity Button으로 구현한다.

`setting-menu-asset.png` 자체에는 X 아이콘이 없으므로 별도 UI 요소로 배치한다.

기능:

```text
Click
→ 현재 변경 사항 처리 정책을 기존 설정창과 동일하게 유지
→ Settings 닫기
```

X를 누르는 동작이 기존 `취소`와 같다면 동일한 코드를 호출한다.

별도 설정 취소 로직을 복제하지 않는다.

---

# 11. 섹션 구성

설정창은 다음 네 구역으로 구성한다.

```text
오디오
디스플레이
시스템
조작
```

아이콘은 `setting-menu-asset.png`에 이미 존재하므로 **아이콘 이미지를 별도로 추가하지 않는다.**

섹션 제목 텍스트만 아이콘 오른쪽에 맞춰 배치한다.

---

# 12. 오디오

컨셉 이미지:

```text
[Speaker Icon] 오디오

마스터 음량        [────────●────] 51%
```

구성:

- `오디오`
- `마스터 음량`
- 기존 Master Volume Slider
- 현재 값 `%`

기존 마스터 볼륨 처리 로직을 그대로 사용한다.

---

# 13. Master Volume Slider

기존 Slider 컴포넌트를 재사용하거나 동일 기능의 Unity Slider를 연결한다.

스타일은 컨셉 이미지와 맞춘다.

### Track

```text
얇은 Dark Track
```

### Fill

```text
Cyan / Teal
```

### Handle

```text
밝은 원형 Handle
```

또는 컨셉과 시각적으로 가장 가까운 형태.

값은 우측에:

```text
51%
```

처럼 표시한다.

Slider 값이 변경되면 숫자도 즉시 갱신한다.

---

# 14. 기존 오디오 기능 유지

기존 Settings 창에서 마스터 볼륨이 다음에 영향을 주고 있다면 모두 유지한다.

예:

- BGM
- 타이틀 음악
- 기지 음악
- 탐사 음악
- 효과음

새 UI 때문에 오디오 처리 범위를 변경하지 않는다.

---

# 15. 디스플레이

컨셉 이미지:

```text
[Monitor Icon] 디스플레이

해상도             [1920 × 1080        ▼]
프레임             [자동(기본값)      ▼]
화면 진동 억제                         [Toggle]
```

이 배치를 최대한 동일하게 구현한다.

---

# 16. 해상도

기존 해상도 선택 기능을 그대로 Dropdown에 연결한다.

표시 예:

```text
1920 × 1080
2560 × 1440
...
```

지원 해상도 생성 방식도 기존 코드를 사용한다.

UI 리워크를 이유로 지원 해상도 목록 생성 규칙을 변경하지 않는다.

---

# 17. Frame

기존 Frame 설정을 컨셉 이미지의 두 번째 Dropdown에 연결한다.

예:

```text
자동(기본값)
60
120
144
...
```

실제 항목은 현재 게임에서 지원하는 값이 정본이다.

컨셉 이미지의 텍스트를 이유로 지원하지 않는 값을 새로 만들지 않는다.

---

# 18. 화면 진동 억제

기존 CheckBox 형태를 컨셉 이미지처럼 **Toggle Switch 형태**로 변경한다.

기능 자체는 기존 설정과 동일하다.

```text
ON
→ 화면 진동 억제 활성

OFF
→ 화면 진동 억제 비활성
```

기존 Boolean 설정과 그대로 연결한다.

---

# 19. Toggle 스타일

컨셉 이미지와 비슷하게:

### OFF

```text
Dark background
Handle left
Low cyan
```

### ON

```text
Cyan outline / fill
Handle right
Bright cyan
```

실제 상태 변경은 Unity Toggle의 `isOn`을 사용한다.

외형 때문에 별도 상태 변수를 만들지 않는다.

---

# 20. 시스템

컨셉 이미지:

```text
[Gear Icon] 시스템

언어               [한국어             ▼]
```

현재 게임에서 지원하는 언어 목록을 그대로 사용한다.

현재 한국어만 지원하더라도 Dropdown 구조는 유지할 수 있다.

---

# 21. 언어

기존 Localization 또는 Language 설정 코드와 연결한다.

UI를 바꾸면서 새 Localization 시스템을 만들지 않는다.

---

# 22. 조작

컨셉 이미지:

```text
[Keyboard Icon] 조작

                  [ 키 조작 변경 ]
```

기존 `키 조작 변경` 기능에 연결한다.

버튼을 누르면 현재 구현된 Key Binding UI / Popup을 그대로 연다.

---

# 23. Footer

하단은 세 버튼으로 구성한다.

왼쪽부터:

```text
[ 기본값 ]       [ 취소 ]       [ 적용 ]
```

컨셉 이미지와 동일한 간격과 크기감을 유지한다.

---

# 24. 기본값

기존 Settings의 Default / Reset 기능과 연결한다.

클릭 시 기존 정책을 유지한다.

예:

```text
현재 UI 값만 기본값으로 변경
→ 적용을 누르면 실제 설정 저장
```

또는 기존 코드가 즉시 적용 방식이면 그 방식을 유지한다.

UI 작업 중 정책을 변경하지 않는다.

---

# 25. 취소

기존 Cancel 기능 그대로.

설정창을 열기 전 값으로 복구하는 구조라면 그대로 유지한다.

```text
취소
→ 미적용 변경 폐기
→ Settings 닫기
```

---

# 26. 적용

Primary Action.

컨셉 이미지처럼 다른 두 버튼보다 조금 더 강한 Cyan Highlight를 사용한다.

```text
적용
→ 현재 UI 값 적용
→ 기존 Settings 저장
```

현재 코드가 적용 후 창을 닫는다면 그대로 닫고, 유지한다면 유지한다.

---

# 27. Button 상태

모든 버튼에 최소한 다음 상태를 둔다.

```text
Normal
Hover
Pressed
Disabled
```

하지만 과한 애니메이션은 넣지 않는다.

권장:

```text
Normal
→ Dark transparent panel

Hover
→ Cyan border + 약간 밝아짐

Pressed
→ brightness 감소 또는 scale 0.98

Apply
→ 기본 상태에서도 약간 강한 Cyan
```

---

# 28. 텍스트

모든 텍스트는 TextMeshPro를 사용한다.

이미지에 글자를 추가하지 않는다.

한글이 깨지지 않는 현재 프로젝트 폰트를 재사용한다.

가능하면 Main Menu 리워크에서 사용한 폰트 / Material / 색상과 통일한다.

---

# 29. Text 계층

권장 시각 우선순위:

```text
설정
>
섹션 제목
>
옵션 Label
>
현재 값
>
Secondary subtitle
```

---

# 30. 색상

컨셉 이미지의 색을 기준으로 한다.

대략적인 방향:

| 용도           | 방향                   |
| -------------- | ---------------------- |
| Main Text      | Off White              |
| Section Title  | Cyan White             |
| Secondary Text | Muted Cyan / Blue Gray |
| Border         | Dark Cyan              |
| Active         | Bright Cyan            |
| Background     | Navy / Blue Black      |

정확한 Hex 값보다 `setting-menu.png`와 실제 합성 결과를 우선한다.

---

# 31. 기존 Settings UI 처리

기존 설정창의 UI 오브젝트를 무조건 전부 삭제하지 않는다.

먼저 다음을 분류한다.

```text
기능 Script
→ 유지

Button / Slider / Dropdown event binding
→ 유지 또는 새 UI로 연결

기존 Visual Background
→ 교체

기존 Layout
→ 교체

기존 Text
→ 필요 시 새 위치에 재사용

기존 Runtime Logic
→ 유지
```

핵심은 **UI Skin 교체이지 Settings 기능 재작성**이 아니다.

---

# 32. 하지 않을 것

다음은 금지한다.

- 설정 시스템 전체 재작성.
- 새로운 PlayerPrefs 키를 이유 없이 추가.
- Save Schema 변경.
- 기존 설정 데이터를 삭제.
- 기존 기능을 임시 Mock으로 대체.
- `setting-menu-asset.png`와 비슷한 UI를 코드로 다시 그림.
- 제공된 에셋 대신 새 AI 이미지를 생성.
- 아이콘을 새로 생성.
- 설정창에 컨셉 이미지에 없는 장식을 대량 추가.
- 과도한 Bloom.
- 강한 Glitch.
- 지속적인 애니메이션.
- 의미 없는 Terminal Text 추가.
- 컨셉 이미지를 자의적으로 재해석.

---

# 33. 컨셉 일치 원칙

UI를 배치한 뒤 반드시 `setting-menu.png`와 나란히 비교한다.

다음 순서로 조정한다.

```text
전체 Panel 크기
↓
Panel 위치
↓
Header
↓
각 Section 위치
↓
Control X 위치
↓
Control Y 위치
↓
Text 크기
↓
간격
↓
세부 색상
```

처음부터 픽셀 단위로 맞추려 하지 않아도 되지만, 최종 결과는 육안으로 동일한 레이아웃으로 보여야 한다.

---

# 34. Layout 구현 방식

이번 UI는 자동 Layout Group만으로 억지로 구성하지 않아도 된다.

컨셉 이미지가 고정된 명확한 레이아웃을 가지고 있으므로 다음 방식을 허용한다.

```text
SettingsPanel 기준 RectTransform
+
각 Section Anchor
+
개별 요소 anchoredPosition
```

즉 메인 메뉴 리워크처럼 **각 UI 요소를 정해진 위치에 직접 배치하는 방식**을 우선할 수 있다.

단, Anchor를 전부 화면 절대 좌표로 두지 않는다.

Settings Panel 내부 좌표를 기준으로 한다.

---

# 35. Responsive

기준 화면은 Main Menu와 동일한 16:9 환경.

Canvas Scaler의 현재 설정을 유지한다.

일반적으로:

```text
Scale With Screen Size
```

구조를 사용한다.

설정창 내부의 개별 컨트롤은 `SettingsPanel`에 Anchor하여 해상도가 바뀌어도 패널과 함께 스케일되게 한다.

---

# 36. 화면 비율

최소 확인:

```text
1920 × 1080
2560 × 1440
1366 × 768
```

컨셉 이미지 기준 위치 관계가 크게 무너지지 않아야 한다.

Ultrawide에서는 패널 자체를 과도하게 늘리지 않는다.

---

# 37. Sorting

화면 계층:

```text
Main Menu Background
↓
Main Menu UI
↓
Dim Overlay
↓
Settings Panel Asset
↓
Settings Interactive UI
↓
Dropdown Popup / Key Binding Popup
```

Dropdown 목록이 Settings Panel 뒤로 들어가지 않게 Sorting을 확인한다.

---

# 38. Dropdown Popup

Dropdown을 눌렀을 때 펼쳐지는 목록 역시 기존 기본 Unity 흰색 UI처럼 튀지 않도록 현재 설정창 스타일과 맞춘다.

최소:

- Dark background
- Cyan border
- White / Cyan text
- Hover highlight

단, Dropdown의 데이터와 동작은 기존 기능 사용.

---

# 39. Keyboard Navigation

기존에 키보드 또는 게임패드 UI Navigation을 지원했다면 유지한다.

Visual 리워크 때문에 Navigation 순서를 깨지 않는다.

추천 순서:

```text
Master Volume
↓
Resolution
↓
Frame
↓
Screen Shake
↓
Language
↓
Key Binding
↓
Default
↓
Cancel
↓
Apply
```

---

# 40. Escape 처리

기존 설정창에서 `Esc`로 닫을 수 있었다면 동일하게 유지한다.

보통:

```text
Esc
=
Cancel
```

역할로 연결한다.

---

# 41. 애니메이션

1차 구현에서는 없어도 된다.

추가한다면:

```text
Settings Open
0.15 ~ 0.25s Fade

Settings Close
0.10 ~ 0.20s Fade
```

정도만 허용한다.

컨셉 재현보다 애니메이션 구현에 시간을 쓰지 않는다.

---

# 42. 구현 순서

## 42-1. 조사

1. `rule.md` 읽기.
2. Main Menu Settings 구조 확인.
3. 기존 기능 Script 확인.
4. 기존 UI 이벤트 연결 확인.
5. Settings 저장 방식 확인.

이 단계에서 기능 코드는 수정하지 않는다.

---

## 42-2. Asset

1. `setting-menu-asset.png`를 프로젝트 UI Art 경로에 복사.
2. Sprite Import.
3. Settings Background에 배치.
4. 기존 설정창 배경 제거.
5. 게임 화면에서 크기·위치 조정.

---

## 42-3. 기본 레이아웃

다음 순서로 배치한다.

```text
Header
Audio
Display
System
Controls
Footer
```

아직 기능 연결 전이라도 먼저 컨셉 이미지와 위치를 맞춘다.

---

## 42-4. 기능 연결

기존 UI에서 사용하던 기능을 새 요소에 하나씩 연결한다.

```text
Master Volume
Resolution
Frame
Screen Shake
Language
Key Binding
Default
Cancel
Apply
Close
```

기존 이벤트 함수가 있으면 그대로 사용한다.

---

## 42-5. Visual Polish

마지막으로:

- Font Size
- Cyan 색상
- Border
- Slider
- Toggle
- Dropdown
- Button Hover
- Spacing

을 조절한다.

---

# 43. 자동 테스트 / 기능 확인

가능한 기능은 기존 테스트를 유지하고 새 UI에서도 동일하게 통과해야 한다.

새 UI를 위해 기존 기능 테스트를 삭제하거나 약화하지 않는다.

---

# 44. 수동 QA

| ID    | 내용                                            |
| ----- | ----------------------------------------------- |
| S-U01 | Main Menu에서 설정 버튼 클릭 시 새 설정 UI 표시 |
| S-U02 | 설정창의 크기와 위치가 컨셉 이미지와 유사       |
| S-U03 | 배경 Dim 정상                                   |
| S-U04 | 마스터 볼륨 Slider 정상                         |
| S-U05 | Slider % 숫자 즉시 갱신                         |
| S-U06 | 해상도 Dropdown 정상                            |
| S-U07 | Frame Dropdown 정상                             |
| S-U08 | 화면 진동 Toggle 정상                           |
| S-U09 | Language Dropdown 정상                          |
| S-U10 | 키 조작 변경 버튼 정상                          |
| S-U11 | 기본값 정상                                     |
| S-U12 | 취소 정상                                       |
| S-U13 | 적용 정상                                       |
| S-U14 | X 닫기 정상                                     |
| S-U15 | Esc 처리 정상                                   |
| S-U16 | Settings 재진입 시 현재 값 정상 표시            |

---

# 45. Visual QA

| 질문                                              | 통과 |
| ------------------------------------------------- | ---- |
| 기존 단순 사각형 설정창 느낌이 사라졌는가         | 예   |
| `setting-menu-asset.png`가 원본 비율을 유지하는가 | 예   |
| 컨셉 이미지와 섹션 위치가 유사한가                | 예   |
| UI가 Main Menu와 같은 게임의 화면처럼 보이는가    | 예   |
| 텍스트가 배경 질감에 묻히지 않는가                | 예   |
| Slider가 컨셉과 자연스럽게 어울리는가             | 예   |
| Toggle이 기존 CheckBox보다 자연스러운가           | 예   |
| Dropdown이 Unity 기본 UI처럼 튀지 않는가          | 예   |
| 적용 버튼의 우선순위가 명확한가                   | 예   |
| Cyan Glow가 과하지 않은가                         | 예   |

---

# 46. 회귀 테스트

설정 UI 변경 전후 다음 결과가 동일해야 한다.

```text
Master Volume 저장
Resolution 저장
Frame 저장
Screen Shake 저장
Language 저장
Key Binding 접근
Default
Cancel
Apply
게임 재실행 후 설정 복원
```

---

# 47. 실패 조건

다음 중 하나가 발생하면 완료 처리하지 않는다.

- 설정창은 예뻐졌지만 기능 하나 이상이 끊김.
- 새 Settings State가 기존 Settings State와 중복됨.
- Apply와 Cancel 정책이 바뀜.
- `setting-menu-asset.png`가 찌그러짐.
- 컨셉과 섹션 위치가 크게 다름.
- 기존 검은 사각형 배경이 뒤에서 보임.
- Dropdown이 Panel 뒤에 표시됨.
- Toggle 표시와 실제 값이 반대.
- 해상도 변경 후 UI 위치가 깨짐.
- Settings를 다시 열었을 때 값이 초기화됨.
- 새 UI가 Main Menu보다 과하게 밝고 화려함.

---

# 48. 작업 체크리스트

1. [ ] `rule.md` 확인
2. [ ] 기존 Settings 구조 및 기능 조사
3. [ ] `setting-menu-asset.png` Unity UI Art 경로로 이동
4. [ ] Sprite Import 설정
5. [ ] 기존 Settings Visual Background 제거
6. [ ] 새 Layout Background 적용
7. [ ] Header 배치
8. [ ] Audio UI 배치
9. [ ] Display UI 배치
10. [ ] System UI 배치
11. [ ] Controls UI 배치
12. [ ] Footer 버튼 배치
13. [ ] X Close Button 배치
14. [ ] 기존 Volume 기능 연결
15. [ ] 기존 Resolution 기능 연결
16. [ ] 기존 Frame 기능 연결
17. [ ] 기존 Screen Shake 기능 연결
18. [ ] 기존 Language 기능 연결
19. [ ] 기존 Key Binding 기능 연결
20. [ ] Default / Cancel / Apply 연결
21. [ ] 컨셉 이미지와 위치 비교
22. [ ] 주요 해상도 QA
23. [ ] 기능 회귀 확인
24. [ ] 최종 실행 Screenshot 기록

---

# 49. 작업 결과 보고

작업 완료 후 다음을 보고한다.

```text
1. 수정한 Scene / Prefab
2. 추가한 Asset
3. 수정한 Script
4. 신규 Script
5. 기존 Settings 기능 중 재사용한 부분
6. Settings 데이터 구조 변경 여부
7. 테스트 결과
8. 컨셉 이미지 대비 구현 차이
9. 남아 있는 리스크
10. 실제 Unity Game View Screenshot
```

---

# 50. 구현 판단 원칙

이번 작업에서는 **디자인을 새롭게 창작하지 않는다.**

정본:

```text
setting-menu.png
```

구현용 베이스:

```text
setting-menu-asset.png
```

이다.

Agent가 더 좋아 보인다는 이유로:

- 패널을 다른 형태로 바꾸거나
- 섹션 순서를 변경하거나
- 아이콘을 교체하거나
- 새로운 SF 장식을 붙이거나
- 레이아웃을 2열로 바꾸는 것

은 하지 않는다.

---

# 51. 최종 지시

`setting-menu.png`를 최종 목표 화면으로 사용한다.

`setting-menu-asset.png`를 설정창의 실제 Background/Layout Asset으로 사용한다.

그 위에 컨셉 이미지에 표시된 위치를 기준으로:

```text
설정
SYSTEM CONFIGURATION

오디오
마스터 음량
Slider
%

디스플레이
해상도
Frame
화면 진동 억제

시스템
언어

조작
키 조작 변경

기본값
취소
적용
X
```

을 Unity UI로 직접 배치한다.

각 조작 요소는 기존 Settings 기능에 연결한다.

**새 UI를 구현하기 위해 기존 설정 기능을 다시 작성하지 않는다.**

기능과 디자인이 충돌할 경우:

```text
기존 기능 안정성
>
컨셉 레이아웃
>
세부 장식
```

순서로 우선한다.

다만 구현상의 특별한 제약이 없는 한 최종 Game View는 `setting-menu.png`와 가능한 한 동일하게 보이도록 한다.
