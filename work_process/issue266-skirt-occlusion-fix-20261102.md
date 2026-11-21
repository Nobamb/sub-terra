# #266 치마 관통 표현과 외부 빛 제거

- 사용자 요청: 사다리에서 다리가 치마를 관통해 보이는 부분만 수정. 형광빛/주변 빛은 사용하지 않는다. 전신5자세 방식 유지, 리깅 미사용.
- v3에서 겹침 경계 보정을 시도했으나 오른쪽 허벅지 옆의 피부 쐐기 형태가 남아 v4에서 해당 영역을 불투명 회색 천으로 덮도록 다시 편집했다.
- v4는 의상 경계가 개선됐지만 알파 합성에서 갈색/검정 배경 얼룩이 나타나 미채택했다. 이 원화/GIF를 게임용으로 사용하지 않는다.
- 내장 image_gen의 배경 추출 편집으로 `images/issue266/female-ladder-skirt-v5-clean.png`를 만들었다. 흰색/검정 합성 `female-ladder-skirt-v5-white.jpg`, `female-ladder-skirt-v5-black.jpg`에서 외부 빛/얼룩과 치마 경계를 확인했다. 캐릭터 밖5개 배경 표본의 알파0도 확인했다. 표본 확인은 전체 픽셀이나 모든 향후 모션의 완벽함을 보장하는 검사가 아니다.
- 현재5자세에서는 오른쪽 다리를 든 두 자세의 허벅지 피부가 치마 옆으로 뚫고 나온 듯한 경계를 회색 천으로 덮고, 천 아래로 다리가 나오는 모양을 유지했다. 속옷이나 비침은 보이지 않는다.
- 최종 검토 GIF: `images/issue266/female-ladder-skirt-v5-review/ladder-review.gif`, 기존1→2→3→2→1→4→5→4 순서. 게임 크기 후보5장1254×1254는 `female-ladder-skirt-v5-runtime-candidates/`에 별도 저장한다.
- 프롬프트: `female-ladder-skirt-v3-prompt-20261102.txt`, `female-ladder-skirt-v4-prompt-20261102.txt`, `female-ladder-skirt-v5-clean-prompt-20261102.txt`. 생성 편집이므로 의상 이외 모든 픽셀이 원본과 완전히 동일하다는 뜻은 아니다.
- Unity Assets/씬/프리팹/게임 코드는 변경하지 않았다. 실제 게임 접점·진입/이탈은 미검증. 커밋·푸시·PR #270 갱신하지 않았다. 기존 승인 GIF 및 게임용 후보를 덮어쓰지 않았다.
