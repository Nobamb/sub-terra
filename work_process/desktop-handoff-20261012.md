# 데스크탑 작업 인계 — 2026-10-12

2026-10-13 후속 상태와 재개 순서는 `desktop-handoff-20261013.md`를 먼저 읽는다. 아래 미통합·승인 대기·미커밋 설명은 작성 당시 기록이며 최신 상태로 간주하지 않는다.


공유 상태 갱신: 최신 main f23f00d6은 통합 커밋 a30f7470에 포함되며, 귀환 안내 수정·이 문서·관리 코드 검사 스크립트는 이번 후속 커밋에 포함해 기능 브랜치와 PR #168로 전달한다. 아래의 미커밋/미푸시 표현은 당시 점검 시점의 기록이며, 데스크탑에서는 실제 원격 HEAD를 확인한다. 실제 Demo 완주·이어하기·Windows 종료 검증은 여전히 미완료다.

이 문서를 작업 시작점으로 사용한다. 과거 대화의 완료 표현보다 현재 Git 상태와 아래 검증 범위를 우선한다.

## 1. 작업 위치와 환경을 먼저 구분

| 항목 | 기준 |
| --- | --- |
| 저장소 | Nobamb/sub-terra |
| 노트북 Git 루트 | C:/Users/jeone/sub-terra |
| 노트북 Unity 프로젝트 | C:/Users/jeone/sub-terra/sub-terra |
| 데스크탑 경로 | `git rev-parse --show-toplevel` 결과 아래 `sub-terra/`를 사용한다. 노트북 절대 경로를 그대로 사용하지 않는다. |
| Unity Editor | ProjectSettings/ProjectVersion.txt 기준 6000.5.4f1 |
| 시작 씬 | Assets/_Project/Scenes/Bootstrap/Bootstrap.unity |
| 기능 브랜치 | codex/facility-move-runtime-followup |
| 관련 PR | https://github.com/Nobamb/sub-terra/pull/168 |
| 마지막 게시 기능 커밋 | 25b52143 — 충전기·보관함 이동 확대 |

Unity Hub에서는 프로젝트 이름이나 목록의 첫 번째 위치만 보고 선택하지 않는다. 선택한 항목의 경로가 현재 Git 체크아웃의 내부 Unity 프로젝트와 같은지 확인한다. 예전 `.worktrees` 프로젝트로 실행하면 다른 코드가 보인다.

- Unity 버전·패키지 버전·ProjectSettings/manifest/packages-lock을 임의 변경하지 않는다.
- Personal 라이선스와 Unity Hub 로그인은 PC별로 확인한다. 노트북 활성화가 데스크탑 검증 성공을 보장하지 않는다.
- Library/Temp/obj/bin/생성 csproj/빌드를 PC 간 복사하지 않는다. 데스크탑에서 임포트·컴파일 및 필요 시 프로젝트 파일을 한 번 생성한다.
- Unity와 Codex가 동일한 Git 체크아웃을 사용해야 한다. 같은 프로젝트를 여는 Editor 두 개 또는 자동 검사와 수동 Editor의 동시 실행을 피한다.
- 코딩 폴더에서 `.agents`와 init/rules 등 해당 프로젝트 지침을 먼저 적용한다. 전체 문서와 과거 로그를 일괄 읽지 않는다.

## 2. Git 및 기록의 공유 상태

1. `git status --short`, 현재 브랜치, HEAD를 확인한다. 사용자 변경을 reset/clean으로 지우지 않는다.
2. origin/main과 기능 브랜치를 fetch하고 PR #168 병합 상태를 확인한다. 팀원이 병합하므로 직접 PR을 병합하지 않는다.
3. PR #168이 미병합이면 해당 기능 브랜치를 사용한다. 병합됐다면 최신 main을 기준으로 후속 브랜치를 만든다. main만 받아놓고 미병합 기능이 있다고 가정하지 않는다.
4. **25b52143에 포함된 것은 이동 대상 4종(조명·정산 콘솔·충전기·보관함)이다. 보건소 추가 및 새 인계/측정 도구는 25b52143 이후의 후속 커밋에 포함된다.** 데스크탑에서 기능 브랜치의 후속 커밋까지 받아야 한다. 문서나 보건소 코드가 없으면 작업 완료로 추측하지 않는다.
5. 노트북 폰트 변경은 `preserve-local-font-before-main-sync-20261012` 이름의 stash에만 보존된다. stash는 GitHub에 전송되지 않는다. 데스크탑에서 stash@{0}을 임의로 적용하거나 폰트 파일을 덮어쓰지 않는다.
6. 노트북의 데이터 에셋 3개(Clinic/Light/Settlement) 상태는 줄바꿈 차이로 `M`이지만 의미 diff는 비어 있었다. 데스크탑에서는 상태를 다시 판단하고 일괄 stage/restore하지 않는다.

## 3. 완료/미완료를 정확히 구분

| 작업 | 구현 | 확인한 범위 | 남은 범위 |
| --- | --- | --- | --- |
| 조명·정산 콘솔 이동 | PR #168 게시 | 이전 Unity 검사 11개, main 통합 후 관련 7개 통과 | 실제 마우스/화면 검수는 별도 |
| 충전기·보관함 이동 | 25b52143 게시 | 관련 어셈블리 4개 경량 C# 컴파일 성공 | 새 Unity 검사 7개 미실행 |
| 보건소 이동 | 이번 후속 구현 | 아래 작업 결과에 별도 기록 | Native Unity 검사 5개 미실행 |
| ComputeBuffer | 사용자 승인 후 15.1.0 embedded 패치 적용(후속 커밋) | 패치 Runtime 컴파일 성공 | Unity 패키지 재해석·양쪽 PC·Windows 종료 재검증 |
| 채굴 끊김 | 구간 마커·진단 소스 준비, 기존 CSV 분석 | 과거 샘플의 순간 지연 확인 | 새 프레임별 측정·병목 확정·최적화 전후 비교 |

경량 컴파일 성공을 Unity 전체 임포트/소스 생성/플레이모드/Windows 빌드 성공으로 보고하지 않는다. 테스트 소스가 컴파일된 것과 실제 통과한 것은 다르다.

## 4. 다음 실행 순서

### A. 보건소 이동 및 필수 검사

- 시설 5종 모두 기존 오브젝트/ID를 유지한다. 보건소·충전기 2×2, 조명·정산 콘솔 1×2, 보관함 1×1이다.
- 보관 수량은 공용 OutpostState.Storage, 충전/회복 대기는 FacilityCooldownState.InstanceId에 저장된다. 이동 시 재생성/ID 변경/회복 호출/타이머 초기화를 추가하지 않는다.
- 데스크탑에서 미실행 Native 검사 12개만 선택한다. 충전기/보관함 7개와 보건소 5개이며 아래 네 메서드 이름이 필터다.

```text
MoveChargerOrStorage_BlockedCellAndCancelPreserveOriginal
MovedChargerAndStorage_PreserveIdentityAndSaveFootprint
ClinicMovement_BlockedFootprintKeepsOriginal
ClinicMovement_PreservesIdentityAndSaveFootprint
```

- 회복 대기 순수 상태 검사 `ClinicMovement_ReturnToRangeDoesNotResetHealingCooldown`은 별도 관리형 실행 결과를 아래에서 확인한다. 이미 통과했다면 반복 실행하지 않는다.
- 최소 오류 확인만 원한다면 `work_process/tools/CompileUnityChanges.ps1 -CheckClinicCooldown`을 사용한다. 이 PC의 Unity가 기본 경로가 아니면 `-UnityEditorPath`를 전달한다. 생성 csproj나 Library/ScriptAssemblies가 없으면 이 PC에서 정상 Unity 컴파일/프로젝트 파일 생성을 한 번 하고 재시도한다. 임시 출력 폴더는 work_process/tools/unity-light-check-output이며 Assets/Library의 DLL을 덮어쓰지 않는다.
- runtime 컴포넌트/Prefab/Scene에 추가 Inspector 배선은 필요 없다. 수동 실행은 Bootstrap부터 시작한다.

### B. ComputeBuffer 종료 경고

- 원인: 2D Animation 15.1.0의 GpuDeformationSystem이 AfterSceneLoad에서 fallback 버퍼를 만들지만 SpriteSkin/DeformationManager가 없는 장면에서는 기존 Cleanup 호출이 없다. 과거 빈 Windows 장면에서 원인을 분리했다.
- 패치: work_process/2d-animation-fallback-cleanup-15.1.0.patch. 소유 코드에서 Application.quitting에 해제를 등록하며 중복 등록을 방지한다.
- **패키지 변경 승인 답변은 아직 없다.** 동일 버전 패키지를 프로젝트에 포함하는 데 필요한 packages-lock 등록 정보 변경을 먼저 승인받는다. 기능 구현 요청을 이 예외에 대한 답변으로 추측하지 않는다.
- 승인 후에도 Library/PackageCache에 직접 적용한 것을 제품 수정으로 취급하지 않는다. 캐시는 다른 PC에 전달되지 않고 다시 생성하면 사라진다. 같은 15.1.0 버전과 .meta GUID를 유지하는 embedded 패키지로 검토하고 등록 변경을 명시한다.
- private API reflection 강제 해제, 다른 소유자의 버퍼 해제, 경고 숨김은 제품에 넣지 않는다.
- 데스크탑 PackageCache의 `@해시`와 Unity 경로는 노트북과 다를 수 있다. 패키지 package.json에서 실제 버전을 확인하고 해당 소스에 `git apply --check`부터 수행한다.
- 적용 후 최소 재현은 빈 Windows 장면의 정상 종료 경고와 실제 게임 종료를 비교한다. 종료 코드·해당 경고 수·컴파일 결과를 기록한다. 이 검증에만 필요한 빌드를 만들고 중복 출력 폴더를 만들지 않는다.

### C. 채굴 끊김 분석

- 기존 분석 결과: work_process/mining-spike-analysis-20261012.md. 옛 CSV는 노트북의 로컬 work_process/windows-profile-20261008에 있었으며 Git 공유 파일이라고 가정하지 않는다.
- 진단 소스: work_process/tools/MiningSeparationProbe-20261012.cs. **Editor CPU 진단용이며 Windows FPS 검증 도구가 아니다.** Git에 보관된 파일 자체는 런타임에서 실행되지 않는다.
- 메모리 여유가 충분한 데스크탑에서만 임시로 Assets/_Project/Scripts/App/Integration/MiningSeparationProbe.cs로 복사하고 임포트한다. 기존 같은 파일이 있으면 덮어쓰지 않는다. 측정 후 임시 파일과 Unity 생성 .meta를 제거한다.
- 진입점: SubTerra.App.Integration.MiningSeparationEntry.Start. Bootstrap→별도 슬롯→SurfaceBase→광산을 정상 경로로 진행한다. `-subterra-save-root`를 이 PC 저장소 안의 **새 QA 전용 폴더**로 지정해야 하며 기존 사용자 슬롯 경로를 사용하지 않는다.
- 실제 시설 6개와 30초 채굴의 QA 목표 탐색/위치 변경/물리 동기화, 채굴 Tick/완료/거래/타일 제거/후속 이벤트·개별 콜백을 구분한다.
- 결과: work_process/mining-separation-measured-20261012.txt 및 mining-separation-frames-20261012.csv. 마커 값은 ns, interval_ms는 ms, NA는 수집 불가다. 합계/평균과 최대값만으로 원인을 단정하지 않는다.
- CSV 문자열·파일 쓰기는 측정 종료 후 수행하며, 프레임 버퍼는 최대 32,768개다. 누락 행 수가 있으면 보고한다. 진단 래퍼 비용이 있어 근접한 작은 차이를 개선으로 단정하지 않는다.
- 긴 프레임의 CSV와 CPU Timeline을 함께 확인한다. QA 비용이 크면 진단 도구부터 개선하고 게임 변경으로 보고하지 않는다. 게임 병목이 확인되면 한 가지 수정 후 동일 조건으로 비교한다.
- 노트북에서 Editor private 메모리 약 9.85GB와 디스크 0.96GB까지 소진된 적이 있다. 측정 전 RAM/디스크 여유를 기록하고 압박이 생기면 자신이 실행한 측정만 중단한다. 이를 게임 병목 샘플로 채택하지 않는다. Windows 측정은 별도 단계로 수행한다.

## 5. 보존 및 검증 원칙

- 소스/Assets/.meta/사용자 슬롯/폰트 stash는 보존한다. 자동 설정 변경은 실행 전 별도 기준을 확보하고 자기 변경만 복원한다.
- 재생성 캐시 정리는 Unity가 닫힌 상태에서 명시한 경로만 대상으로 한다. Library 전체·Artifacts·PackageCache 삭제로 무거운 재임포트를 유발하지 않는다.
- 검사 후 자신이 시작한 Editor와 임시 출력만 정리한다. 실행 중인 사용자 Editor를 임의 종료하지 않는다.
- 마지막에 git status/관련 diff/검증 결과/미수행 항목을 기록한다. 전체 테스트·전체 문서 읽기·전체 로그 출력·반복 빌드는 기본으로 진행하지 않는다.

## 데스크탑 Codex에 전달할 요청

이 체크아웃의 work_process/desktop-handoff-20261012.md부터 읽어주세요. Git 루트와 내부 Unity 프로젝트, 6000.5.4f1 설치 경로, PR #168 병합 상태와 후속 커밋 포함 여부를 먼저 확인하세요. 노트북 경로나 Library를 재사용하지 말고 사용자 변경을 보존하세요. 보건소 이동의 미실행 Native 검사 5개와 충전기·보관함 검사 7개만 필요한 범위로 진행하세요. ComputeBuffer 패키지는 승인 후 적용된 embedded 파일과 lock 등록을 확인하고, 채굴 분석은 QA 비용과 게임 비용을 분리한 프레임별 자료로 진행하세요. 필수 검사와 컴파일만 수행하고, 통과한 검사 반복·무거운 측정·PR 병합은 임의로 하지 마세요.

## 이번 작업의 실제 검증 결과

- Unity를 열지 않고 관련 어셈블리 5개를 순서대로 컴파일: 모두 성공(종료 코드 0).
- ClinicMovement_ReturnToRangeDoesNotResetHealingCooldown: 순수 관리 코드 실행에서 PASS. 이동으로 상호작용 범위를 벗어났다가 돌아와도 기존 시설 ID의 회복 대기시간은 초기화되지 않음.
- 수정한 MiningSeparationProbe 진단 소스 컴파일: 성공(종료 코드 0). 실제 측정 실행은 하지 않음.
- 2D Animation 15.1.0 패치 git apply --check: 성공. 동일 버전 embedded 패키지에 적용 완료. 실제 Windows 종료 경고 재검증은 미실행.
- git diff --check: 통과. ProjectSettings/씬/프리팹 변경 없음. Packages 변경은 아래 사용자 승인 범위만 포함.
- 보건소 이동의 Unity 오브젝트·저장 복원 검사 5개와 이전 충전기·보관함 검사 7개는 미실행. 위 관리 코드 검사로 대체 완료했다고 주장하지 않음.
- 이번 임시 컴파일 출력은 검증 후 삭제. 소스·문서·검사는 유지하며 공유할 후속 커밋에 포함.

## ComputeBuffer 적용 상태 갱신 — 사용자 승인 후

이 절이 이전의 승인 대기 안내보다 우선한다. 사용자가 동일 버전 패키지 포함과 packages-lock 변경을 조건부 승인했고, 영구 수정 방식에 필요한 범위로 적용했다.

- Packages/com.unity.2d.animation에 원본 15.1.0 전체 파일과 .meta/라이선스를 보존하여 포함(약 49.3MB). 원본 Library/PackageCache는 수정하지 않았다.
- 실제 소스 수정은 Runtime/BatchedDeformation/GpuDeformationSystem.cs 한 파일, 5개 추가 줄이다. 생성 시 Application.quitting 이벤트를 중복 없이 등록하고 해제 함수에서 구독을 제거한다. 기존 Release 및 null 처리는 유지한다.
- manifest.json의 15.1.0 의존성과 ProjectSettings는 그대로다. packages-lock.json에서 이 패키지 항목만 file:com.unity.2d.animation / source=embedded로 변경했다. 다른 의존성 버전은 유지한다.
- 경량 검사: 패치 Runtime 전체 소스를 기존 의존 DLL로 컴파일한 결과 종료 코드 0. Unity 전체 임포트·패키지 재해석·Windows 종료 검증을 대신하지 않는다.
- 데스크탑에서 후속 커밋을 받은 뒤 Package Manager가 Embedded 15.1.0으로 표시하는지 확인한다. Unity가 lock 파일을 재생성하면 관련 diff를 확인하고 다른 버전 변경을 무조건 받아들이지 않는다.
- 남은 최소 실행 확인: 빈 씬 Windows 종료 및 실제 Demo 종료의 경고 비교. 동일 경고가 없어졌는지와 종료 성공 여부를 기록한다. 현재 노트북에서는 메모리 압박 때문에 Editor/빌드를 실행하지 않았다.
- 다른 PC에서 후속 커밋을 받기 전에는 적용되어 있다고 가정하지 않는다. 공유할 때 embedded 폴더 전체(.meta/라이선스 포함)와 lock 변경을 함께 포함해야 한다.
- 이 패치로 채굴 끊김이 개선됐다고 주장하지 않는다. 종료 시 버퍼 해제만 수정한 것이다.

Unity 공식 embedded 안내: https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/dependencies-lp/upm-embed

## 다음 작업의 우선 목표 — Demo 완주 마감

사용자 결정: 지금 새 실행 검증을 시작하지 않는다. 다음 작업에서는 새 콘텐츠 추가를 잠시 멈추고 아래 순서로 진행한다. 몬스터·던전·행성 확장과 작은 연출 보완은 Demo 이후로 둔다.

1. 팀원이 병합한 코드와 후속 커밋을 하나의 후보 버전으로 맞춘다. 팀원의 PR을 임의 병합하지 않는다. 테스트할 Git 커밋과 Windows 빌드 해시를 기록한다.
2. Bootstrap → 새 게임 → 일반 조작으로 Demo 경로 완주 → 지상 귀환 → 판매·업그레이드 → 재탐사를 확인한다. 상세 경로는 docs/MVP2_WINDOWS_QA.md를 사용한다.
3. 같은 빌드를 완전히 종료하고 이어하기로 시설 위치·보관 내용·재사용 대기·진행을 확인한다. 시설 이동 후 저장, 설치 실패 시 비용·원래 위치 유지, 전력 고갈 및 귀환 실패 복구를 확인한다. 사용자 실제 슬롯을 테스트 때문에 덮어쓰지 않는다.
4. 진행 불가와 저장 손실부터 수정한다. 모든 미실행 검사를 이미 존재하는 버그로 보고하지 않는다. 관련 검사만 최소 실행하며 전체 테스트·반복 빌드는 기본으로 하지 않는다.
5. 반복되는 채굴 끊김은 여유 있는 데스크탑에서 게임 CPU 비용과 측정 도구/OS 비용을 분리해 확인된 병목만 수정한다. 작은 사다리 접점·호버·이미지 미세 수정은 플레이를 방해할 때만 우선한다.
6. ComputeBuffer 패치의 실제 Windows 종료 검증과 처음 하는 플레이어의 목표·Demo 종료 안내를 확인한다.

완료 기준: Unity가 없는 다른 Windows PC에서 새 게임으로 Demo를 완주하고, 종료 후 이어하기가 정상 동작한다. 실행 검증은 아직 미완료이며 이 기준을 통과했다고 보고하지 않는다.

## Demo 점검 진행 결과
실제 실행은 데스크탑에서 진행하기로 사용자와 확정했다. 노트북에서 순수 관리 상태 검사 7개 통과. 최신 main f23f00d6의 B-143/B-144 변경은 아직 기능 브랜치에 미통합. 검증 조건·결과·미실행 항목과 다음 순서는 work_process/demo-acceptance-preflight-20261012.md를 읽는다. 이번 추가 기록과 검사 실행 스크립트는 아직 로컬 미커밋이다.

최신 main f23f00d6 통합을 로컬 커밋 a30f7470에서 완료했다(아직 미푸시). 지상 이동 퀘스트 문구 1곳을 실제 탑승·상호작용 귀환으로 수정했고 해당 파일 경량 컴파일 통과. 변경 문구/추가 기록은 미커밋. 완료 카드·첫 안내·저장 경로의 정적 확인 범위와 데스크탑 미검증 항목은 demo-acceptance-preflight-20261012.md의 마지막 절을 우선한다.

## 실제 검증 다음의 작업

demo-acceptance-preflight-20261012.md의 **데스크탑 검증이 끝난 뒤의 작업 순서**를 따른다. 실패가 있으면 진행 불가·저장 문제 → 반복 끊김 → 남은 종료 경고·안내 순으로 필요한 수정만 한다. 필수 흐름이 통과하면 Demo 후보 빌드·PR·실행 안내·알려진 문제·빌드 해시를 마감하고 팀원이 최종 확인한다. 이후 확장은 몬스터 1종의 작은 시험 구간 → 던전 1개 → 행성 이동과 상위 자원/제작 경로 1개 순으로 범위를 합의한다. 신규 콘텐츠를 이미 구현 승인된 작업으로 취급하지 않는다. 이 추가 계획도 아직 로컬 미커밋이며 데스크탑 전달 전 공유 상태를 확인한다.
