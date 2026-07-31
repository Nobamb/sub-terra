namespace SubTerra.App.Readiness
{
    /// <summary>
    /// 증거 태그 → 상태 판정 순수 규칙.
    /// 대역 테스트만 있는 항목은 Runtime/Play 게이트에서 완료가 될 수 없다.
    /// </summary>
    public static class ReadinessStatusRules
    {
        /// <summary>
        /// 기능 전체 상태. 정의 없으면 미구현, 증거 없으면 미검증,
        /// 실제 Runtime/Play(+필요 시 Restore)가 모두 있을 때만 완료.
        /// </summary>
        public static ReadinessStatus EvaluateOverall(EvidenceKind evidence, bool requiresRestore)
        {
            if ((evidence & EvidenceKind.Definition) == 0)
            {
                return ReadinessStatus.Unimplemented;
            }

            var hasSurrogate = (evidence & EvidenceKind.SurrogateTest) != 0;
            var hasRuntime = (evidence & EvidenceKind.RuntimePrefab) != 0;
            var hasRestore = (evidence & EvidenceKind.Restore) != 0;
            var hasPlay = (evidence & EvidenceKind.Play) != 0;
            var hasAnyTest = hasSurrogate || hasRuntime || hasRestore || hasPlay;

            if (!hasAnyTest)
            {
                return ReadinessStatus.Unverified;
            }

            // 실제 Runtime Prefab/Play 증거가 있고 복원 요구가 충족될 때만 전체 완료.
            if (hasRuntime && hasPlay && (!requiresRestore || hasRestore))
            {
                return ReadinessStatus.Complete;
            }

            // 대역만 있거나 일부 게이트만 충족 → 부분.
            return ReadinessStatus.Partial;
        }

        /// <summary>
        /// 개별 게이트 상태. Runtime/Play는 대역 증거만으로는 완료가 될 수 없다.
        /// </summary>
        public static ReadinessStatus EvaluateGate(ReadinessGateLevel gate, EvidenceKind evidence)
        {
            switch (gate)
            {
                case ReadinessGateLevel.Definition:
                    return (evidence & EvidenceKind.Definition) != 0
                        ? ReadinessStatus.Complete
                        : ReadinessStatus.Unimplemented;

                case ReadinessGateLevel.Runtime:
                    // 대역만 통과한 항목은 Runtime 완료가 아니다.
                    if ((evidence & EvidenceKind.RuntimePrefab) != 0
                        || (evidence & EvidenceKind.Play) != 0)
                    {
                        return ReadinessStatus.Complete;
                    }

                    if ((evidence & EvidenceKind.SurrogateTest) != 0)
                    {
                        return ReadinessStatus.Partial;
                    }

                    if ((evidence & EvidenceKind.Definition) != 0)
                    {
                        return ReadinessStatus.Unverified;
                    }

                    return ReadinessStatus.Unimplemented;

                case ReadinessGateLevel.Restore:
                    if ((evidence & EvidenceKind.Restore) != 0)
                    {
                        return ReadinessStatus.Complete;
                    }

                    if ((evidence & EvidenceKind.Definition) == 0)
                    {
                        return ReadinessStatus.Unimplemented;
                    }

                    if ((evidence & EvidenceKind.SurrogateTest) != 0)
                    {
                        return ReadinessStatus.Partial;
                    }

                    return ReadinessStatus.Unverified;

                case ReadinessGateLevel.Play:
                    if ((evidence & EvidenceKind.Play) != 0)
                    {
                        return ReadinessStatus.Complete;
                    }

                    // 대역 테스트는 Play 완료를 대체하지 않는다.
                    if ((evidence & EvidenceKind.SurrogateTest) != 0)
                    {
                        return ReadinessStatus.Partial;
                    }

                    if ((evidence & EvidenceKind.Definition) != 0
                        || (evidence & EvidenceKind.RuntimePrefab) != 0)
                    {
                        return ReadinessStatus.Unverified;
                    }

                    return ReadinessStatus.Unimplemented;

                default:
                    return ReadinessStatus.Unverified;
            }
        }

        /// <summary>대역만 있는 증거가 Runtime/Play 완료로 승격됐는지 검사한다.</summary>
        public static bool IsInvalidSurrogatePromotion(EvidenceKind evidence, ReadinessStatus claimed)
        {
            if (claimed != ReadinessStatus.Complete)
            {
                return false;
            }

            var onlySurrogateAmongRuntimePlay =
                (evidence & EvidenceKind.SurrogateTest) != 0
                && (evidence & EvidenceKind.RuntimePrefab) == 0
                && (evidence & EvidenceKind.Play) == 0;

            return onlySurrogateAmongRuntimePlay;
        }
    }
}
