using System;

namespace SubTerra.App.Readiness
{
    /// <summary>
    /// 기능 완료 증거 태그. 대역(surrogate) 테스트는 Runtime/Play 완료를 대체하지 않는다.
    /// </summary>
    [Flags]
    public enum EvidenceKind
    {
        None = 0,
        Definition = 1 << 0,
        SurrogateTest = 1 << 1,
        RuntimePrefab = 1 << 2,
        Restore = 1 << 3,
        Play = 1 << 4
    }

    public static class EvidenceKindLabels
    {
        public const string Definition = "definition";
        public const string SurrogateTest = "surrogate-test";
        public const string RuntimePrefab = "runtime-prefab";
        public const string Restore = "restore";
        public const string Play = "play";

        public static string Format(EvidenceKind evidence)
        {
            if (evidence == EvidenceKind.None)
            {
                return "none";
            }

            var parts = new System.Collections.Generic.List<string>(5);
            if ((evidence & EvidenceKind.Definition) != 0)
            {
                parts.Add(Definition);
            }

            if ((evidence & EvidenceKind.SurrogateTest) != 0)
            {
                parts.Add(SurrogateTest);
            }

            if ((evidence & EvidenceKind.RuntimePrefab) != 0)
            {
                parts.Add(RuntimePrefab);
            }

            if ((evidence & EvidenceKind.Restore) != 0)
            {
                parts.Add(Restore);
            }

            if ((evidence & EvidenceKind.Play) != 0)
            {
                parts.Add(Play);
            }

            return string.Join(",", parts);
        }
    }
}
