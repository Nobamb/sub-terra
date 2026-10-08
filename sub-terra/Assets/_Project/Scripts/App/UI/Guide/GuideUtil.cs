using UnityEngine;

namespace SubTerra.App.UI.Guide
{
    internal static class GuideUtil
    {
        /// <summary>플레이 중에는 Destroy, 에디터 테스트에서는 DestroyImmediate로 오브젝트를 정리한다.</summary>
        public static void Dispose(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            target.SetActive(false);
            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
