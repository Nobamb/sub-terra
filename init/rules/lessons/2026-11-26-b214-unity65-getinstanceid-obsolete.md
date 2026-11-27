# Unity 6.5에서 GetInstanceID()가 컴파일 오류(CS0619)라서 새 시드 코드가 막혔다

- 날짜: 2026-11-26
- 태그: unity65, compile, random, b214
- 실수: B-214 포탈 시작 각도 시드를 `GetInstanceID()`로 만들었다가 에디터 새로고침 때 `CS0619: GetInstanceID is deprecated`(경고가 아니라 오류)로 컴파일이 멈췄다. 같은 때 다른 asmdef의 타입을 쓰면서 `using`도 빠뜨렸다(CS0246).
- 원인: Unity 6.5는 `Object.GetInstanceID()`를 오류로 막는다. 기억만 믿고 썼고, 쓴 직후 컴파일 확인을 안 했다.
- 다음부터 할 일: 오브젝트 고유 시드는 위치·이름·카운터로 만들고 `GetInstanceID()`는 쓰지 않는다. 새 파일을 쓴 뒤 `AssetDatabase.Refresh` 후 `Unity_GetConsoleLogs`(Error)로 컴파일부터 확인한다.
- 관련: B-214, `PortalHoleSpin`
