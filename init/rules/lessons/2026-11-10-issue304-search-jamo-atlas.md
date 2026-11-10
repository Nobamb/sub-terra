# 한글 자모는 조회표뿐 아니라 저장된 아틀라스 픽셀까지 확인한다
- 날짜: 2026-11-10
- 태그: issue304, UI, TMP, IME, font, building
- 실수: 건설 검색창에서 초성 검색은 작동했지만 조합 중 자음은 빈칸으로 보였다.
- 원인: 동적 폰트 조회표는 ㄱ·ㅅ·ㄷ을 보유한 것으로 응답했으나 해당 아틀라스 칸의 실제 픽셀은 비어 있었고, 저장된 글자 표에는 호환 자모가 없었다.
- 다음부터 할 일: HasCharacter 검사와 실제 아틀라스 픽셀 검사를 함께 수행한다. KoreanFontAssetUtility.EnsureCharactersSaved로 조회표를 동기화하고 U+3131~U+318E를 저장한다. 전체 폰트를 초기화하지 않고 기존 글리프·자간을 보존했는지 비교하며 Domain Reload 후 IME 미확정 문자열도 화면에서 확인한다.
- 관련: #304, PR #310, prompt-B-191-1, PromptB188BuildingModeBuilder, PromptB191FollowupTests, BuildingMode_FollowupReclickCompositionAndUnscaledHover.
