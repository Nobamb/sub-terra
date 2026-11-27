using UnityEngine;

namespace SubTerra.Gameplay.Power
{
    /// <summary>
    /// B-214: 시설 그림 연출(조명 깜빡임)이 빛 후광 알파를 낮출 때 쓰는 값 하나.
    /// Building은 Hazards를 참조하지 않고 Hazards는 Power를 참조하므로, 둘이 공통으로 아는 Power에 둔다.
    /// 시각 전용이다. 가스 시야 판정(GasVisionClearanceSource)은 이 값을 읽지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FacilityVisualDim : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float multiplier = 1f;

        /// <summary>0이면 후광이 사라지고 1이면 원래 밝기다.</summary>
        public float Multiplier
        {
            get => multiplier;
            set => multiplier = Mathf.Clamp01(value);
        }

        public static FacilityVisualDim GetOrAdd(GameObject host)
        {
            if (host == null) return null;
            var dim = host.GetComponent<FacilityVisualDim>();
            return dim != null ? dim : host.AddComponent<FacilityVisualDim>();
        }
    }
}
