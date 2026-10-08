# Correction Delta 1 — Prompt-B 137-1 (Claude 리뷰 → Codex)

## 0. 먼저 알릴 것 (소유권 정리)
앞선 실행 중 Planner 쪽 실수로 **Codex 실행이 두 개 동시에** 같은 작업 트리를 편집했다(네가 본 "외부 동시 수정"의 정체). 두 번째 실행은 **종료했다**. 지금부터 이 트리의 **유일한 편집 주체는 너**다. 현재 트리는 두 실행이 섞인 상태이므로:
1. 변경된 모든 파일을 **처음부터 다시 읽고**(`git diff`, 신규 파일 전체) 현재 내용을 네가 소유한다. 이전에 네가 쓰려던 패치를 가정하지 말 것.
2. 아래 수정 항목을 반영하되, 계획서(`prompt-b137-1-plan.md`)와 핸드오프의 원칙·범위는 그대로다.
3. `Temp/`의 오래된 플래그(`subterra-run-editmode.flag`, `subterra-stop-playmode.flag`, `subterra-refresh-assets.flag`)와 결과 파일은 **새 실행 전에 지우거나 시각을 확인**하고, 결과 파일은 반드시 새 시각·fixture 수를 확인한다. Unity 창 제목이 `InitTestScene…`(임시 씬)이면 이전 테스트가 남긴 상태다 — Unity MCP/플래그 러너로 복구 시도, 불가능하면 증거와 함께 보고(성공 가정 금지). Computer Use는 승인되지 않으므로 쓰지 않는다. 임시 `InitTestScene` 에셋·`.meta`가 남아 있으면 제거하고 `git status`로 확인.

## 1. 코드 리뷰 결과 (Claude가 현재 트리를 읽고 확정한 것)
파일: `Scripts/App/UI/HUD/MineResetClockView.cs`, `MineResetClockOverlay.cs`, `MineResetClockPowerTimeline.cs`.

| # | 심각도 | 문제 | 필요한 수정 |
| --- | --- | --- | --- |
| A | 높음 (요구 §4 미충족) | `MineResetClockPowerTimeline.Evaluate`의 `ContentAlpha = Ramp(p,0.55,0.90)`가 **프레임(철판·림·코너·Display) 전체가 든 `Content`** CanvasGroup에 걸린다. 그러면 창이 확장되는 p 0.35~0.55 구간(퇴장에서는 수축 구간 대부분)에 **프레임이 안 보이고** 빔·엣지 발광만 보인다. 요구는 "내용이 빠르게 어두워지며 **창의 위아래가 중앙으로 수축**→가로선→빛점"이다. | 알파를 둘로 나눈다: **프레임/바탕 알파**(Plate·Bezel·Display·AccentTop·Bracket·FrameGlow; p≈0.30~0.45에서 빠르게 1)와 **판독부 알파**(Digits·DigitGlow·Label; 0.55~0.90). 퇴장(되감기)에서는 판독부가 먼저 꺼지고 프레임이 마스크와 함께 수축해 선→점이 되어야 한다. 기존 경로 `Find("Digits")/("Label")/("AccentTop")/("DigitGlow")` 의미는 유지(경로 변경 시 테스트만 갱신, 단언 약화 금지). 순수 `Pose`에 `FrameAlpha`/`ReadoutAlpha`로 노출하고 시간표 테스트에 단조성·경계값을 추가. |
| B | 높음 (계획 §3-3·검증 위반) | `SetPowered(false)`가 `poweredOn==false && p==0`이면 **멱등 조기 반환**해 최초 생성(`Build→RefreshFromState`, 표시 불가 상태) 때 루트가 **활성 상태로 남는다**(숨김 중 비활성이어야 함, 기존 PlayMode 단언 `activeSelf==false`와도 충돌). | 꺼짐 요청 시 `p==0`이면 방향 변경 여부와 무관하게 루트를 비활성화(멱등은 유지: 이미 비활성이면 no-op). 생성 직후 숨김 상태도 비활성으로 정착. 숨은 동안 `Update` 비용 0 확인. |
| C | 높음 (안정성) | `OnDestroy → ResetPower → ApplyPower`가 **이미 파괴됐을 수 있는 자식 Image/Window/CanvasGroup**을 만진다(자식 파괴 순서 비보장 → MissingReference). 같은 이유로 `OnDisable`의 `ResetPower`도 `UnityEngine.Object` null 검사 없이 접근. 보고된 `NullReferenceException … Apply ← ResetObservation ← OnEnable`도 이 계열일 수 있다. | `OnDestroy`에서는 UI를 건드리지 말고 상태 값만 정리(또는 Unity null 검사 후 접근). `Apply/ApplyPower` 진입에서 필수 참조를 `!= null`로 검사(`?.` 금지). 오버레이 파괴·씬 파괴·EditMode `DestroyImmediate`·`Build()` 중 예외 후 `OnEnable`이 호출되는 경로를 EditMode 테스트로 고정(예외·경고 0). 위 NRE 재현 시 근본 원인(어느 참조인지)을 결과 문서에 적는다. |
| D | 중간 | `RectMask2D.padding`이 고정 `-6`(클립 영역 6px 확대)이라 p 작은 구간에서도 유효 클립이 12×12 이상 + 선 두께보다 넓다. 의도(FrameGlow 번짐 표시)는 맞지만 가로선 구간에서 **선이 두꺼워 보일 수 있다**. | 캡처로 확인 후 필요하면 진행도에 맞춰 패딩을 0→-6으로 올리거나 번짐은 마스크 밖 별도 Image로 분리. 캡처 증거 없이 유지/변경 판단하지 말 것. |
| E | 낮음 | `MineResetClockFrameArt.cs` 주석에 인코딩 손상 문자열. | UTF-8 한국어 주석으로 정리(필요한 곳만). |
| F | 확인 | `RefreshFromState`의 켜짐 경로에서 `clockView.SetPowered(true, !Application.isPlaying)`가 값 갱신(`SetFormattedClock`) **뒤**에 호출되는 순서는 OK. | 유지. 단, "시간값 갱신만으로는 `PowerProgress`/등장 재시작이 없다"를 EditMode+PlayMode 둘 다에서 실제 단언(초 변화 N회, `RefreshFromState` 프레임당 2회 호출 후 p가 정확히 `OnDuration` 기준으로만 진행)으로 검증. |

## 2. 그대로 유지할 것 (이미 맞음)
- 단일 스칼라 `p` 되감기 모델, 등장 0.40s / 퇴장 0.28s, `Mathf.Min(dt,1/20)`, `Update` 단일 구동, 코루틴·Tween 없음.
- `RefreshFromState` 한 곳 연결, `SaveRuntimeController`/Style/Glyph/팝업 코드 무변경, 금속색 고정·구간색 분리, 280×78/앵커/위치 불변, 모든 Graphic raycast false.
- 범위 밖 파일 0개(폰트·EditorBuildSettings 자동 변경은 복구해 왔음 — 계속 확인).

## 3. 이후 순서 (핸드오프 §5와 동일, 단 A~C 선행)
1. A~C(+E) 수정 → EditMode 신규/갱신 테스트 → **새 결과 파일(새 시각)로 컴파일 오류 0·통과 확인**.
2. 프레임 정지 캡처(1920×1080, 1280×720)로 프레임·가독성 확인 후 D 판단.
3. PlayMode 시퀀스: 등장 ≥8프레임 연속 캡처(+`PowerProgress`·`Window.sizeDelta` 시계열), 퇴장 동일, 중단 역전(등장 중 퇴장·퇴장 중 재진입), 청록/노랑/빨강/마지막 1분, 저장·불러오기, 0초 종료(중복 실행 0), 일시정지(timeScale 0)에서도 완료, 입력 통과.
4. 캡처를 직접 열어 방향(중앙→가장자리 / 가장자리→중앙)을 확인한 근거를 결과 문서에 적는다(정지 이미지 1장으로 판단 금지).
5. `git status`로 범위 확인, `prompt-b137-1-result.md`를 **현재 사실만**으로 갱신(과거의 "외부 수정" 서술 정리, 검증 표를 통과/실패/미검증으로 재작성).

완료 보고 형식은 핸드오프 "완료 보고" 그대로. 커밋 금지.
