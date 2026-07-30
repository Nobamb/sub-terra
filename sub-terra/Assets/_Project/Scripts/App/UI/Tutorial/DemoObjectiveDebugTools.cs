using UnityEngine;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// Development/Editor 전용 목표 강제 진행 도구.
    /// Release 플레이어 빌드에는 포함되지 않는다.
    /// </summary>
    public sealed class DemoObjectiveDebugTools : MonoBehaviour
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        [SerializeField] private TutorialDirectorBinder tutorialBinder;
        [SerializeField] private KeyCode forceAdvanceKey = KeyCode.F9;

        private void Awake()
        {
            if (tutorialBinder == null)
            {
                tutorialBinder = GetComponent<TutorialDirectorBinder>();
            }
        }

        private void Update()
        {
            if (forceAdvanceKey != KeyCode.None
                && Input.GetKeyDown(forceAdvanceKey)
                && tutorialBinder != null)
            {
                tutorialBinder.DebugForceAdvanceObjective();
            }
        }
#else
        private void Awake()
        {
            // Release: 컴포넌트 자체를 비활성화해 노출을 막는다.
            enabled = false;
        }
#endif
    }
}
