# 코루틴 진행기는 안쪽 IEnumerator를 직접 펼쳐 돌린다

- 날짜: 2026-11-13
- 태그: coroutine, test, warp
- 실수: `EscapeWarpRunner.Run()`이 `yield return RunWormholeWarp();`처럼 안쪽 IEnumerator를 그대로 넘겼다. Unity 코루틴 안에서는 돌지만, EditMode 테스트가 `MoveNext()`로 구동하면 안쪽이 한 번도 실행되지 않아 도착 처리 0회·오버레이 없음으로 실패했다.
- 원인: 중첩 IEnumerator를 풀어 주는 것은 Unity 스케줄러이고 C# 이터레이터 자체는 아니다.
- 다음부터 할 일: 테스트로 구동할 진행기는 `while (inner.MoveNext()) yield return inner.Current;`로 직접 펼친다. 호출하는 쪽은 `StartCoroutine(runner.Run())` 한 번만 쓴다.
- 관련: #151
