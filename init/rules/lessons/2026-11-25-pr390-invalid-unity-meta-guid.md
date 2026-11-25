# 새 Unity 스크립트의 메타 GUID를 검사한다
- 날짜: 2026-11-25
- 태그: unity, meta, guid, compilation
- 실수: 새 UI 스크립트의 메타 파일에 33자리 GUID를 넣어 Unity가 스크립트를 가져오지 못했다.
- 원인: 독립 C# 검사는 소스를 직접 읽으므로 Unity의 에셋 임포트 실패가 드러나지 않았다.
- 다음부터 할 일: 새 메타의 GUID가 32자리 16진수인지 확인하고, Unity 임포트 로그까지 확인한다.
- 관련: PR #390, LocalizedUiText.cs.meta
