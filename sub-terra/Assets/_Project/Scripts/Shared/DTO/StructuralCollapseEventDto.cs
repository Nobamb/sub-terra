using System;
using System.Collections.Generic;

namespace SubTerra.Shared
{
    /// <summary>Player 피해·행동불능 계층이 해석할 붕괴 강도.</summary>
    public enum StructuralCollapseSeverity
    {
        Minor = 1,
        Major = 2,
        Severe = 3
    }

    /// <summary>Unity 타입에 의존하지 않는 붕괴 셀 좌표.</summary>
    [Serializable]
    public struct CollapseCellDto
    {
        public int x;
        public int y;
    }

    /// <summary>
    /// 구조 시스템이 확정한 붕괴 결과. Player 계층은 이 값만 받아 피해와 행동불능을 결정한다.
    /// </summary>
    [Serializable]
    public sealed class StructuralCollapseEventDto
    {
        public long worldSeed;
        public StructuralCollapseSeverity severity;
        public List<CollapseCellDto> cells = new();
    }
}
