# B-168-2 구조 홀로그램 효과음 음량 조정 결과

- 기준 커밋: `3aedad9d39a93e6ac88b85f3487922078b1acabf` (MVP2-fix111, B-168-1). 작업 시작 HEAD와 같으며 조상 검사 통과.
- 작업 브랜치: `MVP2-fix112`. 커밋·push는 하지 않았다.
- `prompt-B-168-2.md`대로 RescueHoloOpen(70)만 조정했다. 실제 청감 확인은 **미검증**이다.

## 적용한 변경

| 파일 | 변경 |
|---|---|
| `sub-terra/Assets/_Project/Audio/SFX/World/rescue_holo_open.wav` | 작업 전에 사용자가 교체한 파일을 그대로 채택. 모노 44.1kHz, 16bit, 1.629342초, 피크 -1.000dBFS 확인 |
| `sub-terra/Assets/_Project/Scripts/App/Audio/SfxLibraryDefaults.cs` | 70번 volume `0.7 → 0.8`, startTime `0.12` 유지 |
| `sub-terra/Assets/_Project/Resources/SfxLibrary.asset` | Unity Editor `SerializedObject`로 70번 volume만 `0.8`로 수정하고 해당 에셋만 저장 |
| `sub-terra/Assets/_Project/Tests/EditMode/App/Audio/PromptB168_1SfxFixTests.cs` | 70번 기대 음량 `0.8` |
| `sub-terra/Assets/_Project/Tests/EditMode/App/Audio/PromptB168SfxD2Tests.cs` | 기존 D2 표의 70번 기대 음량도 `0.8`로 동기화 |
| `init/sound-effect-address.md` | 해당 행 비고에 `B-168-2: +5dB 후 리미터(피크 -1dBFS)` 추가 |
| `init/rules/lessons.md` | 초반 구간 RMS와 실제 청감 확인에 관한 재발 방지 기록 추가 |
| 이 보고서 | 변경·분담·검증 결과 및 수동 체크리스트 |

음원 `.meta`는 변경하지 않았다. 클립 GUID `634e400a3e8ff674cbc2e88e9e7f456c`와 라이브러리 GUID도 유지했다. Unity 재가져오기 후 channels=1, frequency=44100, forceToMono=true, DecompressOnLoad, Vorbis quality=0.7로 기존 설정과 같다. 다른 칸·UiPopupClose·버스·설정·게임 로직·연출은 변경하지 않았다. 새 Inspector 연결이나 세이브 호환성 조치는 필요 없다.

## 음량 측정

Python 표준 라이브러리 `wave`·`array`·`math`로 Git HEAD 음원과 현재 음원의 PCM을 메모리에서 비교했다. 두 파일의 길이·형식은 같고, 피크는 모두 -1.000dBFS이며 현재 음원에 풀스케일 클리핑 샘플은 없다.

| PCM 구간 RMS에 칸 음량 적용 | 기존 음원 × 0.7 | 새 음원 × 0.8 | 증가 |
|---|---:|---:|---:|
| 시작 위치 0.12초부터 0.32초 구간 | -16.319dBFS | -10.995dBFS | +5.324dB |
| 클립 내 가장 큰 연속 0.1초 구간 | 약 -11.438dBFS | 약 -7.661dBFS | +3.777dB |

0.1초 최대 구간은 두 음원 모두 약 0.366초부터다. 새 WAV 자체의 초반 구간 증가는 약 4.164dB이며, 칸 0.7→0.8이 약 1.160dB를 더한다. 이는 Unity 가져오기 압축·버스·설정·다른 소리의 혼합을 반영하지 않은 원본 PCM 계산이다. 청감상 크기와 혼합 출력 왜곡을 입증하지는 않는다.

## 하위 에이전트 분담

실제 `gpt-6-luna` 하위 에이전트에 다음처럼 범위를 지정해 맡겼다.

1. 목표와 소유 파일: Defaults, B-168 두 EditMode 검사, 음원 주소표의 네 파일만 수정한다.
2. 입력과 치환: RescueHoloOpen(70)의 `0.7f`를 `0.8f`로 바꾸고 `startTime 0.12f`를 유지한다. 주소표에는 요청한 문구만 추가한다.
3. 금지 사항: WAV·메타·Resources 에셋·씬·프리팹·설정·패키지·다른 칸 수정, Unity 조작, 테스트 실행, 브랜치 조작, 커밋·push 금지. 기존 사용자 변경을 보존한다.
4. 완료 조건: 네 파일 diff를 검토하고 Scripts/Tests의 해당 칸에 옛 기대값 0.7이 남지 않았는지 검색해 보고한다.
5. 후속 반복 확인: WAV 형식·피크·구간 RMS·클리핑·메타 diff를 읽기 전용으로 비교하고, 마지막 diff에서 GUID·다른 칸 불변 및 기존 실패의 HEAD 재현 조건을 확인한다.

주 에이전트는 요구사항·기준 커밋 판단, 브랜치 생성, Unity 에셋 수정과 가져오기, 컴파일·테스트 실행, 로그·화면 검토, 범위 밖 자동 변경 복원과 결과 보고를 담당했다. PlayMode 실행 중에는 소스 파일을 수정하지 않았다.

## 검증 결과

- **Audio EditMode: 141 통과 / 1 실패 / 0 건너뜀.** `Temp/b168-2-editmode-audio.txt`, 2026-10-27 16:35:59 시작. `SubTerra.App.Tests.Audio` 전체를 실행했다. B-168 두 묶음의 새 값 검사는 통과했다.
- 유일한 실패는 `PromptB162SfxCStageTests.Defaults_CSlotsMatchTable_NumberedFrom35_NoAlertBus`: BriefingClose 기대값 0.5, 실제 1.0. 작업 시작 HEAD에서도 Defaults가 1.0이고 기존 검사 표가 0.5인 것을 확인했다. B-168-1 보고서에도 기록된 기존 실패이며 이번 범위에서 수정하지 않았다.
- **B-168 PlayMode: 4 통과 / 0 실패.** `Temp/subterra-prompt-b168-playmode.done`, 2026-10-27 16:36:51 시작. 가스·피격/체력 경고·설치 실패/붕괴·구조 홀로그램 검사 모두 통과했다.
- 구조 홀로그램 검사는 Bootstrap → 메모리 저장소 새 게임 → 지상 탐사 → 전력 0 → 구조 팝업 닫기 경로를 실행한다. 첫 펼침에서 sourceVolume=**0.8**, listener=0.5, entry startTime=**0.12**, sourceTime=**0.12**를 확인했다. 닫기 요청 30.876초, 펼침 요청 31.172초다.
- 전력 회복 후 재고갈하고 팝업을 닫아도 같은 값으로 실제 AudioSource가 1회 시작했다. 팝업 없는 전력 모델 갱신 분기도 같은 값으로 통과했다. 홀로그램을 눌러 창을 다시 열 때는 UiClick·UiPopupOpen만 발생하고 RescueHoloOpen은 0회였다.
- PlayMode 후 Unity Console 오류·예외 조회 0건. 컴파일된 Defaults도 volume=0.8, startTime=0.12로 확인했다.
- 화면: 이번 실행의 `Temp/b168-2-evidence/popup.png`, `chip.png`, 절반 해상도 묶음 `contact.png`에서 전력 0 팝업과 닫은 뒤 머리 위 홀로그램을 확인했다. 기존 검사에서 B-168-1 경로로 저장한 새 캡처를 이번 증거 폴더에 복사했다.
- 테스트가 자동 재직렬화한 `ProjectSettings/TimeManager.asset`은 시작 때 변경이 없었던 것을 확인하고 복원했다. 임시 테스트 씬은 실행 종료 후 정리됐다. 최종 git status·관련 diff·`git diff --check`를 확인했다.
- Unity CLI에 연결된 Pipeline 인스턴스가 없어서, 대상 프로젝트 경로를 확인한 Unity MCP로 실행했다. CLI·Unity·패키지 버전은 변경하지 않았다.

시작부터 있던 `init/prompt-B.md`, 요청 문서 및 업그레이드 음원 `.meta` 네 개는 보존했다. 기존 교체 WAV에는 추가 가공을 하지 않았다.

## 미검증 및 수동 확인 체크리스트

실제 스피커·헤드폰 청취와 최대 효과음 설정에서의 혼합 출력은 **미검증**이다.

- [ ] 전력 0 → 구조 팝업 닫기 직후 홀로그램 펼침 순간의 “지직”이 전보다 분명히 들린다.
- [ ] 효과음 음량 최대에서도 클릭·다른 창 소리보다 과하게 튀거나 찢어지지 않는다.
- [ ] 전력을 채운 뒤 다시 0으로 만들고 팝업을 닫아도 같은 크기로 들린다.
- [ ] 홀로그램을 눌러 창을 열 때는 클릭·팝업 열림 소리만 들린다.

더 크게 조정해야 한다면 칸 음량을 0.8에서 **1.0**까지 올릴 수 있다. 현재 적용값보다 약 **1.94dB(약 2dB)** 추가 증가하며, 이번 작업에는 적용하지 않았다.
