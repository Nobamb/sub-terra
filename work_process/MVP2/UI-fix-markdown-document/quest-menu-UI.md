# 퀘스트 UI 리워크

기존 게임 내 퀘스트 UI를 `qiest-basic-concept1.png`, `qiest-basic-concept2.png`, `quest-particular-concept.png`의 디자인을 기준으로 전체적으로 리워크한다.

작업 전 반드시 `rule.md`를 확인하고 기존 퀘스트 데이터, 퀘스트 진행 로직, 완료 판정, 보상 데이터 및 기존 UI 연결 구조를 먼저 조사한다.

이번 작업은 **퀘스트 기능 자체를 새로 구현하는 것이 아니라 기존 기능을 유지한 채 UI와 표시 방식을 교체하는 작업**이다.

## 1. 기본 퀘스트창

기본 퀘스트창은 기존 좌측의 퀘스트 표시 위치를 유지하면서 `qiest-basic-concept1.png`를 기준으로 구성한다.

기본 프레임:

```text
quest-active-off.png
```

Hover 상태:

```text
quest-active-on.png
```

마우스를 올리면:

```text
quest-active-off.png
→ 0.5초
→ quest-active-on.png
```

으로 자연스럽게 전환한다.

마우스가 빠지면 반대로 0.5초 동안 기본 상태로 복귀한다.

가능하면 두 이미지를 겹치고 Alpha Cross Fade 방식으로 전환한다.

기본 퀘스트창 전체를 클릭 가능한 영역으로 사용하며, 클릭하면 해당 퀘스트의 **상세 퀘스트창**을 연다.

---

## 2. 기본 퀘스트창 내부 요소

`qiest-basic-concept1.png`와 `qiest-basic-concept2.png`를 기준으로 다음 정보를 배치한다.

- `MISSION`
- 현재 퀘스트 제목
- 현재 퀘스트 설명
- 현재 진행도
- 진행 중 / 완료 상태 아이콘

텍스트는 이미지로 만들지 않고 **TextMeshPro를 사용한다.**

퀘스트 상태와 진행도는 기존 실제 Quest Data를 사용한다.

예:

```text
진행 중
6 / 18

완료
18 / 18
```

완료 상태에서는 Concept Image처럼 체크 아이콘을 표시한다.

진행 중 상태에서는 완료되지 않은 원형 Indicator를 사용한다.

---

## 3. 상세 퀘스트창

기본 퀘스트창을 클릭하면 `quest-particular-concept.png`를 기준으로 상세 퀘스트창을 표시한다.

상세창의 기본 프레임은:

```text
quest-particular-frame.png
```

을 사용한다.

`quest-particular-frame-concept.png`는 **각 UI 요소의 배치 위치를 참고하기 위한 Concept Image**로 사용한다.

Concept Image 자체를 통째로 Background로 사용하지 않는다.

---

## 4. 상세창 닫기 버튼

닫기 버튼 기본 이미지:

```text
quest-close-button-active-off.png
```

Hover 이미지:

```text
quest-close-button-active-on.png
```

배치는 `quest-particular-frame-concept.png`의 우측 상단 위치를 기준으로 한다.

Hover:

```text
OFF
→ 0.5초
→ ON
```

Pointer Exit:

```text
ON
→ 0.5초
→ OFF
```

버튼 클릭 시 상세 퀘스트창을 닫고 기존 Gameplay 화면으로 돌아간다.

---

## 5. 상세창 이전 / 다음 버튼

왼쪽 버튼 기본 이미지:

```text
quest-particular-button-active-off.png
```

Hover 이미지:

```text
quest-particular-button-active-on.png
```

를 사용한다.

왼쪽 버튼은 원본 이미지를 사용한다.

오른쪽 버튼은 **동일한 Sprite를 수평 반전하여 재사용한다.**

새로운 오른쪽 버튼 이미지를 별도로 생성하지 않는다.

기능:

```text
왼쪽
→ 이전 퀘스트

오른쪽
→ 다음 퀘스트
```

로 동작한다.

현재 프로젝트에서 퀘스트가 정렬되는 기존 순서를 그대로 사용한다.

퀘스트 목록을 새로 정의하거나 하드코딩하지 않는다.

첫 퀘스트에서 이전 퀘스트가 없거나 마지막 퀘스트에서 다음 퀘스트가 없다면 해당 버튼은 비활성화하거나 표시하지 않는다.

Hover Transition은 닫기 버튼과 동일하게 0.5초로 한다.

---

## 6. 상세창 표시 정보

`quest-particular-concept.png`의 레이아웃을 기준으로 다음 정보를 표시한다.

```text
MISSION LOG

퀘스트 제목

현재 상태
현재 진행도
클리어 여부

임무 내용

Quest Thumbnail

클리어 보상
```

모든 텍스트와 수치는 기존 Quest Data를 기반으로 동적으로 표시한다.

Concept Image에 적힌 텍스트를 그대로 하드코딩하지 않는다.

---

## 7. 클리어 보상

클리어 보상 영역에는 기존 게임에서 사용하는 실제 자원 아이콘을 우선적으로 재사용한다.

예:

```text
골드
구리
철
리튬
```

보상 값도 기존 Quest Reward Data에서 가져온다.

예:

```text
리튬 5
골드 300
```

Concept Image에 있는 값은 Layout 참고용일 뿐 실제 데이터로 고정하지 않는다.

추후 시설 설계도 등의 신규 보상이 추가될 수 있으므로 보상 UI는 여러 Reward Type을 표시할 수 있는 구조로 작성한다.

---

## 8. Quest Thumbnail

상세창의 가운데에는 해당 퀘스트를 시각적으로 설명하는 Thumbnail을 표시한다.

우선순위는 다음과 같다.

```text
1. 실제 Gameplay 장면을 Unity에서 캡처
2. 프로젝트 내 기존 Sprite / 시설 Asset을 조합하여 이미지 구성
3. 실제 장면 구성이 어려운 경우에만 이미지 생성 사용
```

가능하다면 AI로 새로운 게임 화면을 임의 생성하기보다는 **실제 게임 Scene에서 해당 퀘스트 내용을 표현하는 장면을 구성하고 캡처하는 것을 우선한다.**

예:

`긴급 탈출 귀환`

```text
플레이어
긴급 탈출 포탈
엘리베이터 또는 전진기지 코어
```

가 한 장면 안에서 퀘스트 내용을 이해할 수 있도록 구성한다.

Thumbnail 제작을 위해 Gameplay Scene, 월드 상태 또는 실제 Game Object를 영구적으로 변경하지 않는다.

필요한 경우 Editor 전용 캡처 방식이나 테스트 상태를 사용한다.

---

## 9. 썸네일이 아직 없는 경우

모든 퀘스트에 Thumbnail을 즉시 새로 만들어야 하는 것은 아니다.

해당 퀘스트에 사용할 적절한 Thumbnail이 없을 경우 임시 Placeholder를 표시할 수 있도록 한다.

추후 별도의 Quest Thumbnail 제작 작업으로 교체 가능해야 한다.

---

## 10. UI Asset 배치

이번 작업에서 사용하는 이미지들은 프로젝트의 기존 UI Asset 구조를 확인한 뒤 적절한 위치에 배치한다.

예:

```text
Assets/_Project/Art/UI/Quest/
```

단, 기존 프로젝트에 Quest/UI 관련 폴더 규칙이 있다면 해당 규칙을 우선한다.

사용 Asset:

```text
quest-active-off.png
quest-active-on.png

quest-particular-frame.png

quest-particular-button-active-off.png
quest-particular-button-active-on.png

quest-close-button-active-off.png
quest-close-button-active-on.png
```

Concept Image:

```text
qiest-basic-concept1.png
qiest-basic-concept2.png
quest-particular-concept.png
quest-particular-frame-concept.png
```

Concept Image는 Layout 참고용이며 Runtime UI Asset으로 직접 사용하지 않는다.

---

## 11. 기존 시스템 보존

이번 작업에서 다음 기능을 새로 작성하거나 변경하지 않는다.

- Quest 진행 조건
- Quest 완료 판정
- Quest 보상 지급
- Save Data
- Quest 순서
- 기존 Gameplay Event
- 기타 Gameplay Logic

새 UI는 기존 Quest 시스템에서 데이터를 받아 표시하도록 연결한다.

---

## 12. 상세창 상태

상세 퀘스트창을 열면 현재 기본 퀘스트창에서 표시 중인 퀘스트를 처음 선택해서 보여준다.

예:

```text
기본 퀘스트창
전력 네트워크 구축하기

↓ 클릭

상세창
전력 네트워크 구축하기
```

이 상태에서 이전/다음 버튼을 통해 다른 퀘스트를 탐색할 수 있다.

---

## 13. UI 상호작용

기본 퀘스트창:

```text
Normal
Hover
Click
```

상세창:

```text
Close

Previous Quest
Next Quest
```

만 구현한다.

새로운 Gameplay 기능이나 불필요한 Animation을 추가하지 않는다.

---

## 14. Visual 기준

최종 결과는 다음 Concept Image와 최대한 일치하도록 한다.

기본 상태:

```text
qiest-basic-concept1.png
```

Hover 상태:

```text
qiest-basic-concept2.png
```

상세창:

```text
quest-particular-concept.png
```

특히 다음을 맞춘다.

- Panel 크기
- Panel 위치
- 내부 Padding
- 텍스트 정렬
- Cyan Glow 강도
- 버튼 위치
- Thumbnail 영역
- 보상 영역
- 이전/다음 버튼 위치
- 닫기 버튼 위치

---

## 15. 하지 않을 것

Agent가 임의로 다음을 하지 않는다.

- 제공된 Frame 디자인 변경
- Cyan 색상 변경
- 새로운 Quest Layout 설계
- 버튼 디자인 재생성
- 퀘스트 로직 재작성
- Quest 순서 임의 변경
- 텍스트를 이미지에 구워 넣기
- 관련 없는 HUD 수정
- 시설창 수정
- 미니맵 수정
- 우측 메뉴 수정
- Save Schema 변경
- 공용 Font/TMP 설정 변경

---

## 16. QA

최소 다음 사항을 확인한다.

```text
기본 퀘스트창 정상 표시

Hover 시 0.5초 전환

Hover 해제 시 0.5초 복귀

기본 퀘스트창 클릭 시 상세창 표시

상세창의 현재 퀘스트 데이터 정상 표시

이전 버튼 정상

다음 버튼 정상

첫/마지막 Quest 경계 처리 정상

닫기 버튼 정상

보상 데이터 정상 표시

Thumbnail 정상 표시

Quest 완료 상태 정상 표시

Quest 진행 상태 정상 표시

다른 Gameplay HUD 기능 영향 없음
```

작업 완료 후 실제 Unity Game View에서:

```text
기본 상태
Hover 상태
상세창
```

을 각각 확인한다.

---

## 8. 후속 개선 및 컨셉트 리워크 (프롬프트 B-107-1)

### 8.1 주요 개선 사항
1. **기본 퀘스트창 가독성 및 여백 조정**:
   - 폰트 크기 확대: `MISSION` (16pt Bold), 타이틀 (20pt Bold), 본문 설명 (15pt).
   - 카드 크기 및 배치 비례 확장: 폭 `460px`, 높이 `136px`로 확장하고, 하단 시설 패널(`Y = -426`)과 안전 간격(`22px`)을 유지하여 겹침 방지.
2. **호버 시 중간 투명화 제거**:
   - `QuestSpriteCrossfade.cs`에서 `normalImage`의 알파를 1.0으로 고정하고, `hoverImage`만 알파 0 → 1로 페이드 오버레이하여 중간에 배경이 비치거나 투명해지는 현상 완벽 제거.
3. **퀘스트 상세창 컨셉트 레이아웃 리워크 (`quest-particular-concept.png` 반영)**:
   - 상단 헤더: `MISSION LOG` (사용자 피드백에 따라 `////` 제외하고 깔끔하게 적용).
   - 목표 타이틀: 32pt Bold 화이트로 상단 강조 배치.
   - 상태 표시 행: `quest-status-plate` 슬라이스드 컨테이너 플레이트 도입, 상태 텍스트 시안색 적용, 수평 구분선 배치.
   - 임무 내용: 섹션 우측 수평 구분선 및 임무 아이콘 배치.
   - 썸네일: `quest-thumbnail-frame` 시안 글로우 외곽선 프레임 및 하단 캐러셀 인디케이터 점(`• • •`) 연동.
   - 클리어 보상: 보상 슬롯 카드 너비 310px 확장, 골드 아이콘을 컨셉트의 골드바(`quest-icon-gold.png`)로 전면 교체.
   - 우측 하단 브랜딩: `PROJECT SUB-TERRA` 텍스트 추가.
4. **퀘스트 상황 0/18 고정 버그 수정**:
   - `DemoObjectivePresenter`에서 `ApplyQuestDetailsVisual` 호출 시 `completed` 대신 `viewedIndex + 1`을 전달하도록 수정하여, 퀘스트를 탐색할 때마다 `1 / 18`, `2 / 18`, ... `18 / 18`로 정확히 반영.

### 8.2 검증 결과
- **EditMode 테스트 통과**: `PromptB107QuestUiTests` 2개 테스트 전부 PASS (`Pass: 2, Fail: 0`).
- **증빙 스냅샷**:
  - 기본 상태: `work_process/MVP2/UI-fix-markdown-document/evidence/prompt-b107/quest-basic.png`
  - 호버 상태: `work_process/MVP2/UI-fix-markdown-document/evidence/prompt-b107/quest-hover.png`
  - 상세창 상태: `work_process/MVP2/UI-fix-markdown-document/evidence/prompt-b107/quest-details.png`

