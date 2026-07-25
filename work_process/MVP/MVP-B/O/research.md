# Phase O — Windows 빌드와 배포

## 1. 개요

`process-B`의 B-14를 구현한다. Development, QA, Release Build Profile을 분리하고 Windows x64 실행본을 다른 PC에서 검증한 뒤 완전한 폴더를 ZIP으로 배포한다.

## 2. 작업 목표

- Build Profile별 디버그 기능, 로그, 마이그레이션 검증과 출시 설정을 명확히 분리한다.
- Bootstrap부터 모든 필수 Scene과 에셋을 포함한 재현 가능한 Windows x64 빌드를 만든다.
- 새 게임, 이어하기, 세이브 경로와 전체 데모를 개발 PC 밖에서 검증한다.
- 실행 파일, Data 폴더, DLL, README와 CHANGELOG를 빠짐없이 패키징한다.

## 3. 구현 범위

- `Editor/Build/`, `Scripts/App/Build/`의 필요한 빌드 정보 코드
- Unity Build Profiles: Development, QA, Release
- `docs/RELEASE_CHECKLIST.md`, `docs/CHANGELOG.md`, 배포용 `README.txt`
- 로컬 산출물 `SubTerra_MVP_0.5.0/`과 ZIP(버전은 실제 릴리스 결정에 맞춰 갱신)

## 4. 권장 구현 방향

1. 현재 `6000.5.4f1`과 package lock을 기준으로 작업 환경을 기록한다. 빌드를 위해 엔진/패키지를 임의 변경하지 않는다.
2. Build Profile 공통값을 Windows x64, 제품명, 회사명, 앱 버전, Bootstrap 첫 Scene과 필수 Scene 목록으로 확정한다.
3. Development에는 디버그 HUD, 테스트 자원, 강제 저장/로드와 상세 로그를 넣고 눈에 띄는 개발 빌드 표식을 둔다.
4. QA는 Release와 같은 콘텐츠/품질 설정을 사용하되 오류 로그와 Save Migration 검증 수단을 유지하고 치트는 끈다.
5. Release는 Development Build, Script Debugging, 디버그 메뉴와 테스트 endpoint를 끄고 최종 아이콘·앱 이름·버전/빌드 번호를 확인한다.
6. 빌드 전 데이터 ID 검증, Edit Mode, Play Mode, Integration 전체, 세이브 migration과 Console 오류 검사를 순서대로 실행한다. 실패하면 Release 빌드를 중단한다.
7. 빌드 스크립트를 만들 경우 허용된 Profile과 workspace 아래 출력 경로만 받게 하고, 기존 배포 폴더를 조용히 덮어쓰지 않는다.
8. 산출물 폴더에 `SubTerra.exe`, `SubTerra_Data/`, `UnityPlayer.dll`, Unity가 생성한 필수 파일, `README.txt`, `CHANGELOG.txt`가 있는지 manifest로 검사한다.
9. 개발 PC에서 새 게임→데모→저장→종료→이어하기→월드 복원을 확인한 뒤 깨끗한 다른 Windows PC에서 반복한다.
10. 다른 PC에서 세이브가 `Application.persistentDataPath`에 생성되고 설치 폴더 쓰기 권한에 의존하지 않는지 확인한다.
11. 검증된 폴더 전체를 ZIP으로 묶고 압축을 새 위치에 풀어 다시 실행한다. 개별 exe만 배포하지 않는다.
12. 선택한 GitHub Releases, itch.io 비공개 페이지 또는 제출 플랫폼에 올릴 때 동일 ZIP의 checksum과 버전/변경 내역을 기록한다. 실제 외부 업로드는 명시적 승인 후 수행한다.

## 5. 보안 및 안정성 기준

- Release에 API 키, `.env`, 로컬 세이브, 디버그 치트와 내부 endpoint를 포함하지 않는다.
- 빌드 로그/README에 개인 로컬 경로와 비밀값을 넣지 않는다.
- 실제 사용자 세이브를 QA 정리 스크립트가 삭제하지 않게 한다.
- 배포 폴더를 덮어쓰기 전 정확한 경로와 버전을 확인하고 이전 릴리스를 보존한다.

## 6. 완료 기준

- Release Windows x64 빌드와 전체 ZIP이 생성된다.
- 압축 해제한 빌드가 다른 PC에서 실행되고 새 게임/이어하기/세이브/월드 복원이 작동한다.
- 필수 DLL/Data/문서가 모두 존재하고 비밀·디버그 기능이 없다.
- 릴리스 체크리스트, 테스트 결과, 버전과 checksum이 기록된다.
