# B-162 결과: 효과음 C 단계 연결

- 기준 커밋: `58fa484c` (MVP2-fix100), 브랜치: `feature/b-162-sfx-c-stage`
- 커밋·PR: 아직 없음(사용자 확인 대기)

## 칸 표 (`SfxLibraryDefaults.Create`, `Resources/SfxLibrary.asset`)
| 번호 | SfxId | 파일 | 버스 | 음량 / 구간 | 클립 길이 | 0.1초 최대 × 칸 음량 |
|---|---|---|---|---|---|---|
| 35 | RunFailGlitch | UI/run_fail_collapse_glitch.wav | UI | 0.6 | 1.17s | -15.2 dB |
| 36 | RunFailBlackout | UI/run_fail_blackout.wav | UI | 0.6 | 0.83s | -10.5 dB |
| 37 | RunFailRowTick | UI/run_fail_row_tick.wav | UI | 0.5 | 0.19s | -15.9 dB |
| 38 | EscapeArrive | World/escape_arrive.wav | World | 0.6 | 0.46s | -14.2 dB |
| 39 | BriefingGlitch | UI/briefing_glitch_tick.wav | UI | 0.5 | 1.02s | -10.7 dB |
| 40 | BriefingGlitchShort | UI/briefing_glitch_tick_short.wav | UI | 0.45 | 0.42s | -11.6 dB |
| 41 | BriefingClose | UI/briefing_close.wav | UI | 0.5 / max 1.0, fade 0.3 | 1.82s | -18.2 dB |
| 42 | SellCoinStack | UI/sell_open_coin_stack.wav | UI | 0.5 | 0.30s | -18.7 dB (4번 겹침 전) |
| 43 | SellCoinBurst | UI/sell_open_coin_burst.wav | UI | 0.55, 끝까지 | 1.42s | -19.8 dB |
| 44 | PortalIdleLoop | World/portal_idle_loop.wav | World | 0.35, 반복 전용 | 20.01s | 측정 못 함(압축 클립), README RMS -13.3 dB |

- 참고(기존 칸): PortalWarp -9.7, UiPopupOpen -11.3, SellConfirm -16.1, KeycapPop -23.5, TimerTickSingle -22.2 dB. 새 소리는 모두 기존 범위 안이다. 버스 음량은 모두 1이고 최종 음량에는 설정의 효과음 음량이 곱해진다.
- 음원은 `sfx-C`에서 복사만 했다(cmp로 같은 바이트 확인). 대체 표는 그대로.
- `portal_idle_loop.wav`는 Load Type `Compressed In Memory`(빌더 Postprocessor의 `SfxLibraryDefaults.IsLoopFile`), 나머지는 기존과 같은 Decompress On Load.
- Unity 에디터에서 빌더(`PromptB156SfxLibraryBuilder.EnsureAsset`)를 돌려 asset 칸과 .meta를 만들었다.

## 연결 위치
| 소리 | 클래스·메서드 | 시점 |
|---|---|---|
| RunFailGlitch | `RunFailurePanelView.Show` | 창 글리치 시작(0초) 한 번 |
| RunFailRowTick | `RunFailurePanelView.AdvanceReveal` → `PlayRowTicks` | 각 줄 `GetItemStartTime(i)`을 넘을 때. 잃은 자원 없음이면 무음. 스킵은 Advance를 거치지 않아 남은 틱 없음 |
| RunFailBlackout | `RunFailurePanelView.PlayTransitionRoutine` 시작 | Darken 단계(0초) 한 번. HeavyGlitch는 무음 |
| UiPopupOpen | 같은 루틴, `RunFailureTransitionTimeline.CrossedTvTurnOn` | TvTurnOn 시작을 넘을 때 한 번(큰 dt도 한 번) |
| SellCoinStack ×4 | `ResourceSellPopupView.Tick` → `PlayOpeningSfx` | `ResourceSellTimeline.CoinLandTime(i)` 0.15/0.23/0.31/0.39초, 피치 `CoinStackPitch(i)` 1.00→1.12 |
| SellCoinBurst | 같은 곳 | `ResourceSellTimeline.ImpactTime` 0.56초 |
| UiPopupClose | `ResourceSellPopupView.BeginClose` | 닫힘 연출 시작 한 번. 열린 창 다시 열기·닫는 중 다시 닫기는 무음 |
| BriefingGlitch | `StartBriefingPopupMotion.BeginIntro` | 등장 글리치 시작 |
| BriefingGlitchShort | `UpdateScreenBursts`(1·2번 버스트), `TriggerGlitch`(EdgeBurst) | 등장·표시 중만, 0.15초 간격 제한(`SfxThrottle`) |
| BriefingClose | `StartBriefingPopupMotion.RequestClose` | 닫기 버튼·X·ESC 모두 같은 경로, 한 번 |
| EscapeArrive | `EmergencyEscapePanelBinder.ConfirmSelectedDestination` 예약 → `Update`/`TickArriveSfx` | 워프 0.35초 뒤(비스케일) |
| PortalIdleLoop | `PortalIdleLoopAudio`(새 컴포넌트, `EmergencyEscapePortalRuntimeBridge.Start`가 붙임) | 3절 |

- 심층 구역 해금 창·퀘스트 클리어 창은 `ConfigureCompact`에서 `playsBriefingSfx`를 끄므로 브리핑 소리가 나지 않는다.

## 확인한 사실과 판단
- **긴급 탈출 도착**: `TryEscapeTo`가 플레이어를 옮기면 같은 프레임 `PlayerCameraFollow.LateUpdate`가 3칸 이상 이동을 순간이동으로 보고 카메라를 바로 붙인다. 즉 카메라 전환이 PortalWarp와 같은 프레임이라, `SfxDelayedCue`로 0.35초 뒤에 낸다. 닫기 연출(0.5초) 동안 바인더 오브젝트는 켜져 있다(panelRoot가 자식).
- **광산 타이머 만료**: `SaveRuntimeController`의 시간 초기화 경로는 시계 전원 OFF 뒤 지상 Scene을 로드할 뿐 RunFailureTransition을 쓰지 않는다 → **연출 없음, 소리 넣지 않음**.
- **TV 켜짐(UiPopupOpen)**: `PlayTransitionRoutine`은 광산 안 복구(전진기지 체크포인트) 경로에서만 TvTurnOn까지 재생한다. 지상 기지로 가는 경로는 강한 글리치 뒤 바로 Scene을 로드하고 지상 쪽에 TV 켜짐 연출이 없다 → 그 경로에서는 **UiPopupOpen이 나지 않는다(연출 없음)**.
- **몸 발광·번개 착지**: 게임에 없음. 만들지 않았다.

## 포탈 대기음 (`App/Integration/PortalIdleLoopAudio.cs`, 계산은 `App/Audio/PortalIdleLoopMath.cs`)
- 자식 오브젝트의 AudioSource 하나(loop)와 저역 통과 필터. 원샷 풀은 쓰지 않는다.
- 0.5초마다 설치된 `BuildingInstance` 중 긴급 탈출 포탈을 다시 찾고(근접 이름표와 같은 기준), 매 프레임 가장 가까운 포탈 하나로 계산한다. 기준 포탈이 바뀌면 위치만 옮기고 다시 틀지 않는다.
- 음량: 2칸 이내 1, 10칸 이상 0(정지), 사이는 S 곡선. 거리·곡선·정지 배율·차단 주파수는 인스펙터 값.
- 효과음 음량·음소거를 매 프레임 따른다. **UiPauseGate 중에는 0.35배로 작게 한다**(긴급 탈출 창도 정지를 걸어서, 멈추면 포탈 앞에서 소리가 끊기기 때문).
- `RunFailureRuntimeController.IsHandling` 동안(실패 창·암전·이동) 멈춘다.
- 씬에 미리 붙여 두지 않았다(Scene 변경 없음). 값을 영구히 바꾸려면 씬의 브리지 오브젝트에 컴포넌트를 붙이면 그 설정을 쓴다.

## 테스트
- EditMode `PromptB162SfxCStageTests` 21개 통과. Audio 네임스페이스 전체 통과(`PromptB156` 칸 수 33→43 수정).
- EditMode 전체: 2025 통과 / 19 실패. 실패 19개(건설 배치 NRE, 패널 폭 580/500, HUD 크기, B-85 칩 위치, B-89 인벤토리 닫기, B-107 씬 저장 안 됨, B-138·B-150 미세 수치 등)는 이번 변경 파일을 쓰지 않는 테스트다. **기준 커밋에서 다시 돌려 비교하지는 않았다(미검증)**.
- 전체 실행 중 빌더 테스트가 건드린 Scene·Prefab·폰트·ProjectSettings·PNG는 `git restore`로 되돌렸다.
- PlayMode: 이번에 추가한 PlayMode 테스트 없음 → 실행 안 함.
- Play Mode 스모크(Bootstrap 시작): 새 원샷 9개가 SfxPlayer에서 모두 재생 시작, PortalIdleLoopAudio가 클립을 반복 재생, Console 오류·경고 없음.

## 미검증
- 실제 플레이 흐름에서 직접 들어 보지 못했다: 쓰러짐 → 실패 창, 긴급 탈출 이동, 새 게임 브리핑, 판매 창(지상·정산 콘솔), 포탈 근처 이동.
- 위험도 경보와 동시에 들리는지(새 소리는 모두 UI·World 버스라 Alert 소스를 건드리지 않음은 코드로 확인).

## 길이·음량 조정 후보
- 브리핑 닫힘: 1.82초 파일을 1.0초 + 0.3초 페이드로 자름. 그래도 길면 maxDuration을 줄인다.
- 브리핑 등장: 긴 지직(1.02초)이 나는 동안 0.17·0.34초 버스트에 짧은 지직이 겹친다. 시끄러우면 BriefingGlitchShort 음량을 낮추거나 등장 중 버스트 소리를 뺀다.
- 동전 터짐: 끝 1.4초까지 "드르르"가 창이 다 열린 뒤 약 0.9초 더 이어진다. 길면 maxDuration 1.0 + fadeOut 0.3.
- 동전 4번 겹침: 0.08초 간격으로 겹쳐 합쳐 커진다(칸 음량 0.5). 크면 0.4 안팎.
- 프레임이 0.05초 넘게 끊기면 동전 두 개가 같은 프레임에 닿아 SfxPlayer 연타 제한(0.05초)에 하나가 묻힐 수 있다.

## 수동 확인 체크리스트
- [ ] 쓰러져서 실패 창이 뜰 때 "지직" 소리가 나고, 잃은 자원이 한 줄 올라올 때마다 "톡"이 난다
- [ ] 확인을 누르면 화면이 꺼질 때 "뚝", 지상 기지 화면이 켜질 때 팝업 켜짐 소리가 난다 (지상 기지 Scene 로드 경로는 TV 켜짐 연출이 없어 무음. 전진기지 복구 경로만 남)
- [ ] 긴급 탈출로 이동하면 워프 소리 뒤, 도착 화면에서 "뿌웅" 소리가 한 번 난다(두 소리가 뭉개지지 않는다)
- [ ] 새 게임 시작 브리핑 창이 뜰 때 지직, 떠 있는 동안 가끔 짧은 지직, 닫을 때 긴 지직이 난다. 심층 구역 해금 창에서는 이 소리가 안 난다
- [ ] 판매 창(지상·정산 콘솔)을 열면 동전 4개가 쌓이며 "칭칭칭칭" 높아지고, 터질 때 "짤랑~드르르"가 난다. 닫으면 팝업 닫힘 소리가 난다
- [ ] 긴급 탈출 포탈에 다가갈수록 "웅~" 소리가 커지고 멀어지면 작아진다. 포탈 두 개 사이를 오가도 소리가 겹치지 않는다
- [ ] 효과음 음량·음소거가 새 소리와 포탈 대기음에도 적용된다. 위험도 경보가 끊기지 않는다
- [ ] Console에 빈 칸 경고나 오류가 없다
