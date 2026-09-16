## 0. 한 줄 결론

함께 제공한 컨셉 이미지는 **최종 분위기와 시각적 목표를 보여 주는 레퍼런스**다.

구현 목표는 컨셉 이미지를 픽셀 단위로 복제하는 것이 아니라,

> **“멸망한 지하 전초기지에서 봉인 너머의 신호를 수신하고 있는 터미널”**

이라는 인상을 실제 Unity 메인 메뉴에서 자연스럽게 전달하는 것이다.

배경 일러스트, 조명, 신호 효과는 이미지 생성으로 품질을 확보하고, 실제 상호작용이 필요한 저장 슬롯·버튼·텍스트는 Unity UI로 구현한다.

**구현이 어려운 장식이나 연출을 억지로 재현하여 전체 퀄리티가 떨어질 경우 해당 요소는 단순화하거나 제거한다.**

판정 문장:

> “메뉴 화면만 보더라도 폐광, 전초기지, 깊은 곳의 봉인된 신호라는 게임의 정체성이 느껴지고, 동시에 저장 슬롯과 메뉴 기능은 빠르고 명확하게 사용할 수 있다.”

이 문장이 성립하지 않으면 메인 메뉴 리워크가 아직 완료되지 않은 것이다.

---

# 1. 기존 기능

현재 메인 메뉴에 존재하는 기능은 모두 유지한다.

| 기능                          | 유지 |
| ----------------------------- | :--: |
| `Sub-Terra` 게임 제목         |  O   |
| 세이브 슬롯 1                 |  O   |
| 세이브 슬롯 2                 |  O   |
| 세이브 슬롯 3                 |  O   |
| 슬롯 선택                     |  O   |
| 이어하기                      |  O   |
| 새 게임                       |  O   |
| 설정                          |  O   |
| 종료                          |  O   |
| 선택 슬롯 상태 표시           |  O   |
| 게임 / Build / Save 버전 표시 |  O   |

기능을 시각적으로 재배치하는 것은 허용하지만, 기존 기능을 삭제하거나 접근성을 떨어뜨리면 안 된다.

메인 메뉴 리워크 때문에 기존 저장·로드 시스템의 동작을 바꾸지 않는다.

---

# 2. 최종 분위기

## 2-1. 핵심 컨셉

다음 두 컨셉을 합친다.

### A. 봉인된 신호

- 깊고 어두운 폐광.
- 인간이 오래전에 만든 산업 시설.
- 깊은 곳에서 올라오는 정체불명의 청록색 신호.
- 봉인된 문 또는 거대한 지하 구조물.
- 아주 오래된 시설이 아직 일부 작동하고 있다는 느낌.
- 아래에 무엇이 있는지 알 수 없는 신비감.

### B. 전초기지 터미널

- 메뉴 UI 자체는 플레이어가 사용하는 전초기지 단말기처럼 보인다.
- 저장 슬롯은 단순한 버튼이 아니라 **탐사 기록**처럼 표현한다.
- 선택된 슬롯에는 명확한 활성 표시.
- SF이지만 지나치게 미래적이거나 깨끗하지 않는다.
- 폐광 장비와 개척 기지에 어울리는 산업적 디자인.

---

# 3. 시각적 우선순위

화면에서 사용자가 보는 순서는 다음과 같아야 한다.

```text
1. Sub-Terra

2. 지금은 40미터다.
   봉인 너머에서 신호가 온다.

3. 저장 슬롯

4. 이어하기 / 새 게임 / 설정

5. 종료

6. 현재 선택 슬롯 상태

7. Build / Save Version
```

배경이 아무리 아름다워도 UI보다 먼저 튀어 나오면 실패다.

반대로 UI가 화면 전체를 덮어 배경의 세계관을 죽여도 실패다.

---

# 4. 배경 이미지

## 4-1. 기본 전략

1차 구현에서는 **고품질 단일 배경 이미지**를 기본으로 한다.

권장:

```text
2560 × 1440
16:9
```

또는 프로젝트 성능과 원본 품질에 따라:

```text
1920 × 1080
```

배경에는 UI 텍스트를 직접 그리지 않는다.

다음 요소만 이미지에 포함한다.

- 암석.
- 폐광 구조물.
- 전초기지 시설.
- 철제 다리.
- 케이블.
- 오래된 장비.
- 안개.
- 먼지.
- 봉인된 구조물.
- 청록색 신호.
- 소량의 주황색 산업 조명.

---

## 4-2. 이미지 생성 지시

배경 이미지 생성 시 다음 방향을 유지한다.

```text
Dark ruined underground mining facility.

A distant sealed structure or shaft is visible deep inside the cavern.

A mysterious cyan / teal signal leaks from the sealed area.

Industrial platforms, cables, mining equipment and abandoned forward-base
structures surround the cavern.

The location feels abandoned but partially operational.

Cold blue-black environment.

Teal signal lighting.

Very small amount of amber industrial warning lighting.

Atmospheric fog and dust.

Cinematic depth.

No characters in the center.

The center area must remain relatively dark and uncluttered
because the save-slot UI will be placed there.

No baked menu text.

No baked buttons.

No game title in the background image.

No fake UI.
```

---

# 5. 컨셉 이미지와 실제 구현의 관계

제공된 컨셉 이미지를 **시각적 방향 기준**으로 사용한다.

그러나 다음은 1:1 복제 대상이 아니다.

- 배경에 적힌 영어 낙서.
- 모든 구조물 위치.
- 우측 모니터의 세부 UI.
- 배경 장비의 작은 라벨.
- 복잡한 holographic detail.
- 매우 많은 작은 조명.

이런 요소는 구현 난이도 대비 품질이 떨어질 경우 제거한다.

### 중요한 원칙

> **“더 복잡하게 만드는 것보다 더 자연스럽게 보이는 것을 우선한다.”**

---

# 6. 배경 레이어

기본은 단일 이미지다.

다음 조건이 만족되는 경우에만 레이어를 나눈다.

```text
Background
Midground
Foreground
Signal / Fog
```

레이어 분리가 이미지 품질을 훼손하거나 생성 흔적을 만드는 경우 사용하지 않는다.

### 허용

- 전경 철골만 아주 천천히 움직임.
- 배경과 전경 사이 2~4px 수준의 패럴랙스.
- 신호 광원만 별도 애니메이션.

### 금지

- 과도한 2.5D 움직임.
- 멀미가 날 정도의 카메라 이동.
- 배경 이미지가 찢어진 것처럼 보이는 레이어 분리.
- 생성 이미지의 잘린 경계가 보이는 패럴랙스.

---

# 7. 분위기 효과

정적 이미지 위에 Unity 효과를 소량 추가한다.

## 7-1. 먼지

화면 전체에 아주 느린 먼지 Particle.

목적:

```text
정적인 그림 → 살아 있는 공간
```

입자 수는 적게 유지한다.

UI 위를 지나가며 텍스트를 방해하지 않는다.

---

## 7-2. 안개

가능하면 매우 약한 반투명 Fog Layer 사용.

애니메이션은 느려야 한다.

```text
10~30초 단위
```

눈에 직접 보이는 움직임보다,

> “가만히 보면 움직이고 있다.”

정도가 적절하다.

---

## 7-3. 봉인 신호

가장 중요한 배경 효과.

청록색 신호 부분의 밝기를 아주 미세하게 변화시킨다.

예:

```text
0.90 → 1.00 → 0.94 → 1.03 → 0.90
```

정확한 값은 실제 화면을 보고 조절한다.

강한 네온 깜빡임은 금지한다.

---

## 7-4. 스캔 노이즈

선택 사항.

사용한다면 다음 수준만 허용한다.

- 아주 희미한 Scanline.
- 아주 약한 UI Noise.
- 드문 Signal Glitch.

지속적으로 번쩍이는 CRT 효과는 사용하지 않는다.

---

# 8. 메인 타이틀

화면 상단 중앙.

```text
Sub-Terra
```

컨셉 이미지처럼 크게 표시한다.

단, 현재 컨셉 이미지의 거대한 로고보다 실제 구현에서는 약간 작게 사용해도 된다.

제목 아래:

```text
지금은 40미터다.
```

그 아래 작은 글씨:

```text
봉인 너머에서 신호가 온다.
```

두 문구는 세계관 전달용이다.

클릭 불가.

---

# 9. 저장 슬롯

기존 3개 슬롯을 **Expedition Record Card** 형태로 변경한다.

예:

```text
┌───────────────────────────────────────────────────┐
│ [ SNAPSHOT ]   SLOT 01                            │
│                Gold 371       Depth 4m            │
│                Last Save  2026.09.14 15:42        │
└───────────────────────────────────────────────────┘
```

실제 언어는 현재 프로젝트 UI 스타일에 맞게 한글 또는 혼합 사용 가능.

최소 표시 정보:

- 슬롯 번호.
- Gold.
- Depth.

가능하면:

- 마지막 저장 시간.

---

# 10. 플레이어 스냅샷

각 세이브 슬롯 왼쪽에 실제 게임 플레이 스냅샷을 표시한다.

이 기능은 이번 리워크에 포함한다.

---

## 10-1. 이미지 규격

권장:

```text
512 × 288
```

16:9.

또는 UI 크기에 따라:

```text
384 × 216
```

저장 형식:

```text
PNG
```

또는 저장 속도/용량이 문제가 될 경우:

```text
JPG 85~90 Quality
```

---

# 11. 스냅샷 촬영

세이브 성공 시 게임 카메라 화면을 캡처한다.

가능하면:

```text
Gameplay World
+ Player
+ Digger-Bot
+ 주변 광산
```

만 캡처한다.

게임 HUD는 제외하는 것을 우선한다.

---

## 11-1. 권장 방법

Gameplay Camera를 RenderTexture로 캡처한다.

예:

```text
RenderTexture
↓
Texture2D
↓
EncodeToPNG
↓
save slot thumbnail
```

단, 현재 카메라 구조상 HUD 제외 캡처가 복잡하다면 억지로 카메라 시스템을 뜯지 않는다.

그 경우 전체 화면 캡처 후 메뉴에서 적절히 crop하는 방법도 허용한다.

---

# 12. 스냅샷 저장 파일

세이브 JSON에 이미지 데이터를 넣지 않는다.

예:

```text
slot_1.json
slot_1_thumbnail.png

slot_2.json
slot_2_thumbnail.png

slot_3.json
slot_3_thumbnail.png
```

파일 이름은 기존 Save System 구조를 확인한 뒤 현재 규칙에 맞춘다.

가능하면 슬롯 번호만으로 thumbnail path를 유추할 수 있게 한다.

따라서 **썸네일만을 위해 Save Schema Version을 올리지 않는다.**

---

# 13. 스냅샷 실패 처리

썸네일이 없어도 세이브 파일은 정상적으로 로드되어야 한다.

다음 상황을 모두 허용한다.

- 구세이브.
- 썸네일 파일 없음.
- 썸네일 파일 손상.
- 이미지 로드 실패.

이 경우 슬롯에는 기본 placeholder를 표시한다.

예:

```text
NO VISUAL RECORD
```

또는 현재 광산 분위기에 맞는 기본 이미지.

썸네일 오류 때문에 이어하기가 실패하면 안 된다.

---

# 14. 스냅샷 갱신 규칙

세이브가 성공한 경우에만 스냅샷을 갱신한다.

금지:

```text
Save 실패
→ 기존 thumbnail 덮어쓰기
```

권장 순서:

```text
1. Save 성공
2. Thumbnail capture
3. Thumbnail write
```

또는 안전성을 위해:

```text
1. Temporary thumbnail write
2. Save success 확인
3. Replace
```

현재 세이브 구조에 가장 안전한 방법을 선택한다.

---

# 15. 선택 슬롯

선택된 슬롯은 명확히 보여야 한다.

컨셉 이미지처럼:

- cyan border.
- soft glow.
- 작은 선택 화살표.
- 약한 밝기 증가.

를 사용한다.

### 사용 금지

- 심한 확대.
- 번쩍이는 애니메이션.
- 강한 neon bloom.
- 텍스트가 흐려지는 효과.

---

# 16. 버튼

버튼 배치는 기존 구조를 최대한 유지한다.

```text
[ 이어하기 ] [ 새 게임 ] [ 설정 ]

             [ 종료 ]
```

### 이어하기

Primary Action.

선택 가능한 슬롯이 있을 경우 가장 강하게 표시한다.

### 새 게임

Secondary Action.

### 설정

Secondary Action.

### 종료

Tertiary Action.

---

# 17. 버튼 인터랙션

Hover:

```text
밝기 소폭 증가
border 증가
아주 약한 cyan glow
```

Pressed:

```text
밝기 소폭 감소
1~2px 안쪽 이동 또는 scale 0.98
```

Selection:

```text
0.15~0.25초
```

정도의 짧은 전환 사용.

큰 bounce animation 금지.

---

# 18. UI 색상

기본 팔레트:

| 용도           | 색상 방향       |
| -------------- | --------------- |
| 배경           | Dark Blue Black |
| 패널           | Steel Blue Gray |
| 일반 Border    | Dark Cyan       |
| 선택 Border    | Bright Cyan     |
| 신호           | Cyan / Teal     |
| 경고           | Amber           |
| 주요 Text      | Off White       |
| Secondary Text | Blue Gray       |

추천 범위:

```text
Background      #061117 ~ #0A151B
Panel           #102630 ~ #183945
Cyan            #39D8EA
Signal Teal     #18C6C8
Amber           #E6A23C
Main Text       #EEF4F5
Secondary Text  #91A7AE
```

실제 적용에서는 배경 이미지와 비교하면서 보정한다.

색상값을 그대로 강제할 필요는 없다.

---

# 19. 글꼴

TextMeshPro 사용.

폰트는 다음 이미지를 지향한다.

```text
Industrial
Condensed
Clean
Readable
```

단, 지나치게 SF 스타일 폰트를 사용하여 한글 가독성을 떨어뜨리지 않는다.

제목과 UI 글꼴이 반드시 같을 필요는 없다.

예:

```text
Title        → Industrial Display Font
UI / Korean  → Clean Sans
```

현재 프로젝트에서 이미 사용 중인 품질 좋은 한글 폰트가 있다면 그것을 우선한다.

---

# 20. UI 구현 방식

UI는 이미지 안에 구워 넣지 않는다.

Unity UI 또는 프로젝트의 현재 UI 체계를 사용한다.

권장:

```text
Canvas
 ├─ Background
 ├─ Atmosphere
 │   ├─ Fog
 │   ├─ Dust
 │   └─ SignalGlow
 │
 ├─ MainMenu
 │   ├─ Header
 │   ├─ SaveSlots
 │   ├─ Actions
 │   ├─ Status
 │   └─ Version
 │
 └─ Settings
```

프로젝트가 이미 다른 UI 구조를 사용한다면 기존 구조를 우선한다.

---

# 21. 권장 컴포넌트

기존 코드를 확인한 뒤 필요한 경우 다음 수준으로 분리한다.

```text
MainMenuView
SaveSlotCardView
SaveSlotPresenter
SaveThumbnailService
MenuAtmosphereController
MenuBackgroundController
```

역할 예:

| Component                  | 역할                                 |
| -------------------------- | ------------------------------------ |
| `MainMenuView`             | 버튼 / 슬롯 연결                     |
| `SaveSlotCardView`         | 슬롯 UI 표시                         |
| `SaveSlotPresenter`        | 세이브 데이터 → UI 데이터            |
| `SaveThumbnailService`     | capture / load                       |
| `MenuAtmosphereController` | 신호 / fog / light animation         |
| `MenuBackgroundController` | background scale / optional parallax |

기존 시스템에 같은 책임의 클래스가 있다면 새 클래스를 만들지 않는다.

---

# 22. 이미지 생성 산출물

필요한 이미지부터 최소 단위로 만든다.

### 필수

```text
MainMenu_Background.png
```

### 필요하면

```text
MainMenu_SignalGlow.png
MainMenu_Foreground.png
MainMenu_Panel.png
MainMenu_ButtonFrame.png
MainMenu_SaveSlotFrame.png
```

하지만 **UI 패널을 전부 이미지로 만들 필요는 없다.**

단순 직사각형 / Border / Glow는 Unity UI로 만든다.

---

# 23. 이미지 생성 판단 기준

이미지 생성으로 만들었을 때 Unity 그래픽보다 확실히 좋은 것만 이미지로 만든다.

예:

### 이미지 생성 적합

- 거대한 지하 공간.
- 복잡한 암석.
- 낡은 광산 설비.
- 봉인 구조물.
- 원경.

### 코드/UI 적합

- Button.
- Text.
- Border.
- Slot highlight.
- Status text.
- Version info.
- Selection state.

---

# 24. 가장 중요한 품질 원칙

다음 상황에서는 **컨셉 이미지와 똑같이 만들려고 하지 않는다.**

```text
레이어 분리 때문에 이미지가 부자연스럽다
→ 단일 배경 사용.

실시간 조명이 그림과 충돌한다
→ 조명 효과 감소.

UI hologram 효과가 싸 보인다
→ 효과 제거.

Blur 때문에 글씨가 흐려진다
→ Blur 제거.

패럴랙스 때문에 그림 경계가 보인다
→ 패럴랙스 제거.

신호 애니메이션이 네온 간판처럼 보인다
→ 정적 신호 + 아주 약한 pulse.

복잡한 장식 때문에 UI 가독성이 떨어진다
→ 장식 삭제.
```

판정 우선순위:

```text
1. 자연스러움
2. 가독성
3. 분위기
4. 애니메이션
5. 장식
```

---

# 25. 절대 하지 않을 것

- 컨셉 이미지 자체에 버튼을 그려서 그대로 클릭 영역만 씌우기.
- 배경 이미지 안의 가짜 텍스트를 실제 UI처럼 사용하기.
- AI 생성 텍스트를 그대로 게임에 사용하기.
- 저장 슬롯을 전부 background image에 baked.
- 거대한 Bloom.
- 심한 RGB glitch.
- 지속적인 화면 흔들림.
- 지나친 vignette.
- 과도한 film grain.
- Hover 때 큰 확대.
- UI를 작은 SF 글자들로 채우기.
- 의미 없는 영어 장식을 너무 많이 사용하기.

---

# 26. Background Responsive 처리

기준은 16:9.

다른 화면 비율에서는 background를 왜곡하지 않는다.

```text
Preserve Aspect
+
Center Crop
```

UI는 Canvas Scaler 사용.

권장:

```text
Scale With Screen Size

Reference Resolution:
1920 × 1080

Match:
0.5
```

실제 프로젝트 UI 기준이 있다면 기존 설정 유지.

---

# 27. 해상도별 확인

최소 확인:

```text
1920 × 1080
2560 × 1440
1366 × 768
2560 × 1080
```

Ultrawide에서 배경이 부족한 경우 stretch 금지.

Crop 또는 별도 background safe area를 사용한다.

---

# 28. 구현 순서

한 번에 전부 만들지 않는다.

## 28-1. 프로젝트 조사

먼저 실제 프로젝트를 확인한다.

확인할 것:

```text
MainMenu Scene
현재 UI Hierarchy
세이브 슬롯 관리 코드
Save path
Save data structure
현재 Main Menu script
Settings popup
Scene transition
Continue logic
New Game logic
Quit logic
Canvas scaling
사용 중인 font
```

이 단계에서는 코드 수정하지 않는다.

---

# 29. 구현 계획 먼저 작성

프로젝트 분석 후 다음을 먼저 보고한다.

```text
수정 파일
신규 파일
재사용 파일
생성할 이미지
세이브 썸네일 저장 경로
기존 기능 영향
리스크
```

그 후 구현을 시작한다.

---

# 30. 1단계 — UI 구조

먼저 이미지 없이 UI만 교체한다.

목표:

```text
제목
카피
3개의 Save Card
4개의 버튼
Status
Version
```

기능 동작 확인 후 다음 단계로 이동한다.

---

# 31. 2단계 — Background

컨셉 이미지를 참고하여 고품질 배경을 생성한다.

메인 UI를 실제로 얹어 보고 배경의 밝기를 조절한다.

### 중요한 원칙

이미지를 먼저 완성하고 UI를 맞추지 않는다.

**Unity에서 UI와 합성된 결과를 기준으로 이미지를 수정한다.**

---

# 32. 3단계 — Thumbnail

세이브 성공 시 플레이 화면 capture.

메인 메뉴 진입 시:

```text
SaveSlot
+
Thumbnail
+
Gold
+
Depth
```

를 표시한다.

---

# 33. 4단계 — Atmosphere

다음 순서로 하나씩 추가한다.

```text
Signal Pulse
↓
Dust
↓
Fog
↓
optional subtle parallax
```

각 효과를 넣을 때 전후 화면을 비교한다.

화면이 더 좋아지지 않으면 해당 효과는 사용하지 않는다.

---

# 34. 5단계 — Polish

최종적으로:

- 버튼 hover.
- slot transition.
- 선택 glow.
- signal animation.
- typography.
- spacing.

을 조절한다.

---

# 35. 세이브 슬롯 예시

최종 슬롯은 대략 다음 느낌.

```text
┌──────────────────────────────────────────────┐
│ ┌────────────┐                               │
│ │            │   탐사 기록 01               │
│ │ SNAPSHOT   │                               │
│ │            │   Gold       371              │
│ └────────────┘   Depth      4m               │
│                  Last Save  2026.09.14 15:32 │
└──────────────────────────────────────────────┘
```

선택 시:

```text
cyan outer glow
+
selector
+
background opacity increase
```

---

# 36. 빈 슬롯

세이브가 없는 슬롯은 플레이 스냅샷을 표시하지 않는다.

예:

```text
탐사 기록 02

— 기록 없음 —

새 탐사를 시작할 수 있습니다.
```

빈 슬롯을 누른 뒤 이어하기를 누를 수 없게 기존 정책을 유지한다.

---

# 37. 메뉴 상태 텍스트

현재:

```text
선택 슬롯 1 — 이어하기 가능
```

은 유지한다.

필요하면 표현만 다듬는다.

예:

```text
탐사 기록 01 선택됨 · 이어하기 가능
```

단, 기존 UI와의 일관성을 우선한다.

---

# 38. 버전 표시

하단:

```text
Game 1.0 | Build Editor | Save v5
```

유지.

다만 시각적 중요도는 낮춘다.

```text
opacity 약 50~70%
small font
```

---

# 39. 성능

메인 메뉴 때문에 불필요한 GPU 부하를 만들지 않는다.

권장:

- Background Sprite 1장.
- 최소 Particle.
- 최소 Shader.
- Post Processing 최소.
- RenderTexture는 thumbnail capture 때만 사용.
- Menu thumbnail은 decode 후 캐싱.

---

# 40. Thumbnail 메모리

메뉴에서 3개 썸네일만 필요하다.

원본 gameplay resolution 이미지를 그대로 사용하지 않는다.

로드 후 UI 용도에 맞는 크기로 관리한다.

512×288 수준이면 충분하다.

---

# 41. 썸네일 보안성 / 저장 안정성

썸네일은 **부가 데이터**다.

따라서:

```text
thumbnail corruption
≠
save corruption
```

이어야 한다.

Thumbnail load 실패를 Save invalid 판정으로 사용하지 않는다.

---

# 42. UI 애니메이션 기준

모든 transition은 빠르고 절제한다.

권장:

```text
Hover       0.10~0.15 s
Selection   0.15~0.25 s
Menu Open   0.20~0.35 s
```

큰 easing animation 불필요.

---

# 43. Audio

이미 프로젝트에 UI 효과음 시스템이 있다면 활용 가능.

권장:

- 슬롯 이동: 짧은 terminal click.
- 버튼 hover: 아주 작은 signal tick.
- 결정: 짧은 mechanical confirmation.

새 오디오 시스템은 만들지 않는다.

---

# 44. 신호 사운드

선택 사항.

메인 메뉴 배경에 아주 작은 ambient hum 또는 먼 신호음을 넣을 수 있다.

단:

- 반복이 명확하게 들리면 안 된다.
- 메뉴를 오래 켜 두었을 때 피곤하면 안 된다.
- 음악보다 튀면 안 된다.

---

# 45. 접근성

배경 때문에 텍스트 가독성이 떨어지지 않아야 한다.

필요하면 Save Card 뒤에:

```text
semi-transparent dark plate
```

를 사용한다.

텍스트를 readable하게 만들기 위해 glow를 세게 넣지 않는다.

---

# 46. 기능 회귀 방지

이번 작업은 **visual rework**가 중심이다.

다음 기존 기능은 변경하지 않는다.

```text
Save slot count
Continue behavior
New Game behavior
Settings behavior
Quit behavior
Save data schema
Scene transition
Gameplay save contents
```

Thumbnail은 기존 저장 시스템에 부가적으로 붙인다.

---

# 47. 테스트

## UI

| ID    | 내용                                |
| ----- | ----------------------------------- |
| M-U01 | MainMenu 진입 시 슬롯 3개 정상 표시 |
| M-U02 | 슬롯 클릭 시 기존 선택 로직 정상    |
| M-U03 | 선택 슬롯만 cyan highlight          |
| M-U04 | Continue 정상                       |
| M-U05 | New Game 정상                       |
| M-U06 | Settings 정상                       |
| M-U07 | Quit 정상                           |
| M-U08 | Status text 정상                    |
| M-U09 | Version text 정상                   |

---

# 48. Thumbnail 테스트

| ID    | 내용                                         |
| ----- | -------------------------------------------- |
| M-T01 | 세이브 성공 후 thumbnail 생성                |
| M-T02 | Menu 재진입 후 thumbnail 표시                |
| M-T03 | 다른 슬롯의 thumbnail과 혼동 없음            |
| M-T04 | thumbnail 없는 구세이브 정상 로드            |
| M-T05 | thumbnail 손상 상태에서도 Continue 가능      |
| M-T06 | 세이브 실패 시 기존 thumbnail 보존           |
| M-T07 | 세이브 재실행 시 thumbnail 갱신              |
| M-T08 | HUD가 screenshot에 포함되지 않는 것이 이상적 |

---

# 49. Visual QA

다음 질문을 실제 화면을 보고 판단한다.

| 질문                                            | 통과 기준 |
| ----------------------------------------------- | --------- |
| 배경이 UI보다 튀는가?                           | 아니오    |
| Sub-Terra 분위기가 느껴지는가?                  | 예        |
| 광산이라는 것을 알 수 있는가?                   | 예        |
| 봉인된 신호라는 미스터리가 느껴지는가?          | 예        |
| 버튼 위치가 바로 이해되는가?                    | 예        |
| Save Slot이 쉽게 읽히는가?                      | 예        |
| Thumbnail이 너무 작거나 복잡한가?               | 아니오    |
| Cyan Glow가 싸 보이는가?                        | 아니오    |
| AI 생성 이미지 특유의 이상한 텍스트가 보이는가? | 아니오    |
| 메뉴가 과하게 SF HUD처럼 보이는가?              | 아니오    |

---

# 50. 퀄리티 실패 조건

다음 중 하나라도 명확하게 보이면 재작업한다.

- 배경 AI artifact.
- 암석이 녹아내린 것처럼 보임.
- 산업 구조물이 물리적으로 이상함.
- UI가 배경에 묻힘.
- Glow가 너무 강함.
- Thumbnail이 뭉개짐.
- 메뉴가 모바일 SF 게임처럼 보임.
- 버튼이 웹페이지처럼 보임.
- 텍스트 대비 부족.
- Background crop 시 중요 오브젝트가 잘림.
- 배경 효과가 반복 패턴처럼 보임.

---

# 51. 구현 에이전트 행동 원칙

에이전트는 레퍼런스 이미지를 보고 다음처럼 판단한다.

### 하지 말 것

> “컨셉 이미지에 있으니까 전부 구현한다.”

### 해야 할 것

> “이 요소가 실제 게임 화면에서도 더 좋은 결과를 만드는가?”

YES면 구현.

NO면 생략.

애매하면 단순한 버전을 먼저 구현한다.

---

# 52. 작업 중 우선순위

```text
기존 기능 안정성
>
UI 가독성
>
배경 이미지 품질
>
세계관 분위기
>
세부 연출
>
장식
```

---

# 53. 에이전트가 이미지 생성 시 반드시 확인할 것

배경을 생성한 후 Unity에 넣기 전에 다음을 본다.

```text
중앙 UI 영역이 충분히 비어 있는가
Title이 올라갈 공간이 있는가
Save Slot 세 장이 올라갈 공간이 있는가
16:9 Crop이 가능한가
AI text가 이미지 안에 들어가 있지 않은가
신호 광원이 너무 밝지 않은가
중앙 대비가 너무 높은가
```

실패하면 프롬프트를 조정해 다시 생성한다.

---

# 54. 첫 번째 배경 생성 프롬프트

아래 프롬프트를 시작점으로 사용할 수 있다.

```text
Create a premium cinematic main-menu background for a 2D sci-fi mining
exploration game called Sub-Terra.

The setting is a ruined underground mining facility and forward outpost
on a post-apocalyptic Earth.

Deep in the cavern is an ancient sealed shaft or massive circular sealed
structure emitting a mysterious faint cyan-teal signal.

The environment is dark, lonely and industrial.

Show abandoned steel platforms, cables, mining equipment, rock walls,
deep cavern fog and distant machinery.

Use mostly blue-black and dark steel-gray colors.

Use cyan and teal only for the mysterious sealed signal.

Use a very small amount of warm amber light from old industrial lamps.

The image should feel mysterious, grounded and believable rather than
flashy or overly futuristic.

The center of the screen must stay relatively dark and uncluttered,
because interactive save-slot cards and buttons will be placed there.

Leave clean negative space in the upper center for the game title.

Strong cinematic depth and atmosphere.

Subtle fog and dust.

No humans.

No characters.

No UI.

No buttons.

No text.

No logos.

No letters.

No readable signs.

No fake holographic interface.

No watermark.

16:9 composition.

Designed specifically as an in-game main-menu background.
```

---

# 55. 실패 시 이미지 프롬프트 조절

중앙이 복잡하다:

```text
Increase negative space in the center.
Move industrial structures toward the outer edges.
```

너무 밝다:

```text
Reduce cyan emission.
Keep the signal distant and subtle.
```

너무 SF다:

```text
Make the technology utilitarian, worn and mining-industrial.
Avoid sleek spaceship aesthetics.
```

너무 일반 폐광이다:

```text
Strengthen the mysterious sealed structure and distant signal.
```

너무 공포게임 같다:

```text
Reduce horror elements.
Focus on isolation, exploration and mystery rather than fear.
```

---

# 56. 작업 결과 보고 형식

구현 종료 후 다음을 정리한다.

```text
1. 생성한 이미지
2. 수정한 Scene
3. 수정한 Prefab
4. 수정한 Script
5. 신규 Script
6. Thumbnail 저장 위치
7. 기존 Save Schema 변경 여부
8. 테스트 결과
9. 남은 리스크
10. 실제 게임 Screenshot
```

---

# 57. 완료 조건

다음이 모두 만족되어야 완료다.

1. [ ] 기존 Main Menu 기능 전부 정상.
2. [ ] 컨셉 이미지와 유사한 세계관 분위기.
3. [ ] 고품질 광산 / 전초기지 배경.
4. [ ] `Sub-Terra` 제목 표시.
5. [ ] `지금은 40미터다.` 표시.
6. [ ] `봉인 너머에서 신호가 온다.` 표시.
7. [ ] Save Slot 3개.
8. [ ] 각 유효 Save Slot의 gameplay thumbnail.
9. [ ] Thumbnail 없는 Save도 정상.
10. [ ] Continue / New Game / Settings / Quit 정상.
11. [ ] 선택 슬롯 시각 강조.
12. [ ] 신호 pulse.
13. [ ] Dust/Fog는 품질이 좋아질 경우만 적용.
14. [ ] Save Schema 불필요한 변경 없음.
15. [ ] 16:9 및 주요 해상도 UI 정상.
16. [ ] AI 생성 텍스트 artifact 없음.
17. [ ] 실제 Unity 실행 Screenshot으로 최종 검증.

---

# 58. 최종 지시

**컨셉 이미지는 목표 분위기의 레퍼런스이지 픽셀 단위 구현 명세가 아니다.**

가장 중요한 것은:

> **게임 안에서 자연스럽게 보이는 것.**

컨셉 이미지에 있는 효과를 구현했지만 실제 Unity 화면의 품질이 떨어진다면 해당 효과를 제거한다.

단순한 구현이라도 더 자연스럽고 고급스럽다면 단순한 쪽을 선택한다.

특히 배경은 **고품질 이미지 생성**, UI와 상호작용은 **Unity 코드**, 저장 슬롯 이미지는 **실제 게임 스냅샷**을 사용하는 하이브리드 방식을 우선한다.

최종 목표는 컨셉 이미지를 흉내 낸 메뉴가 아니라,

> **“Sub-Terra라는 게임에서 실제로 출시해도 어색하지 않은 메인 메뉴”**

를 만드는 것이다.
