# #266 여자 원본 외형·남자 모션 기준 제작 인계

## 현재 기준

- 브랜치: `codex/issue-266-character-selection`.
- 여자 외형은 `references/issue266/female-miner-original.webp` 기준. 짧은 회색·주황 작업복, 허벅지 피부 띠, 갈색 다리 덮개, 허벅지 장비, 포니테일을 유지한다. 남자 긴 작업복 유지 견본은 폐기한다.
- 기존 남자 모션은 포즈·프레임 순서·접점·재생 타이밍 기준으로만 사용한다. 원본 남자 그림을 덮어쓰지 않는다.

## 이번 게시 자료

- `images/issue266/female-original-outfit-walk-v1.png`: 걷기 핵심4자세, 1254×1254. 남자 Walk 1/11/21/31 참고.
- `images/issue266/female-original-outfit-ladder-v1.png`: 사다리5자세, 1536×1024. 실제 Player.prefab GUID에 연결된 `ladder_back_neutral_v2`와 `ladder_back_style_02..05` 참고. 마지막 셀은 비운다.
- `images/issue266/female-original-outfit-mining-v1.png`: 채굴 핵심6자세, 1536×1024. 남자 Mining 1/5/9/13/17/24 참고.
- 각 `-white.jpg` / `-dark.jpg`: 외형·외곽 검토용 합성.
- `images/issue266/female-original-outfit-ladder-v1-review/`: 사다리 전신 셀5장과 검토 GIF. 기존 순서1→2→3→2→1→4→5→4로 재생한다. GIF의 5fps는 검토용이며 게임의 이동거리 기반 속도가 아니다.
- 포즈 참고 출처: `images/issue266/original-outfit-source-cells.json`, `male-*-original-outfit-reference.png` 3장.
- 내장 image_gen 사용. 프롬프트는 `female-original-outfit-prompts-20261102.txt`. 전신 참고 시트/검토 GIF 재현 도구는 `tools/female-original-outfit-reference-20261102.ps1`, `tools/female-original-outfit-ladder-preview-20261102.ps1`.
- 생성 원화는 원본 남자 프레임의 픽셀을 그대로 보존한 파일이 아니며, 모션 전체의 자연스러움이 검증된 최종 세트도 아니다.

## 다음 작업 순서

1. 걷기: 기존 남자40장과 대응하도록 여자 중간 자세 제작. 4장 단순 복제나 왕복으로 장수를 채우지 않는다. 얼굴/장비 크기, 반대 발 교대, 40→1 연결을 비교한다.
2. 채굴: 기존24장에 대응하도록 중간 자세를 채우고 양손 드릴 그립·궤적·장비 위치를 맞춘다. 사다리5자세와 함께 원본 남자의 크기/발/손 기준으로 정렬한다.
3. 추가 동작: 점프 준비·상승·하강, 사다리 채굴, 피격·쓰러짐을 동일 외형으로 제작한다. 동작마다 남자 원본의 실제 연결 파일과 프레임 수를 먼저 확인한다.
4. 세트가 준비되면 남자와 별도의 여자 세트로 연결하고, 새 게임 선택·슬롯별 저장·구저장 남자 기본값을 구현한다. 불완전한 여자 세트를 남자 그림으로 섞어 완성처럼 표시하지 않는다.
5. 승인 후 최소 실제 확인: 걷기/정지 전환, 사다리 진입·이탈/접점, 드릴 그립, 모션별 크기, 선택 저장·이어하기. 장수나 파일 검사만으로 완료 판정하지 않는다.

## 검증·제한

- 흰색/어두운 배경 합성에서 짧은 작업복·다리 덮개·전신 외곽을 확인했다. 사다리 GIF 순서는 현재 재생 코드와 대조했다.
- 이미지·문서·검토 도구만 게시한다. Unity Assets/씬/프리팹/패키지/저장 코드 변경 없음. Unity 실행·컴파일·게임 검사는 하지 않았다.
- **미검증:** 전체 걷기40장/채굴24장 루프, 모든 동작 간 일관성, 인게임 크기·재생 타이밍, 여자 선택·저장. 현재 원화 수는 걷기4/사다리5/채굴6이며 게임에 적용되지 않았다.
