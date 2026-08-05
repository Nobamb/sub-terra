using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// Development/Editor 전용 목표 강제 진행 도구.
    /// Release 플레이어 빌드에는 포함되지 않는다.
    /// </summary>
    public sealed class DemoObjectiveDebugTools : MonoBehaviour
    {
#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
        [SerializeField] private TutorialDirectorBinder tutorialBinder;

        private void Awake()
        {
            if (tutorialBinder == null)
            {
                tutorialBinder = GetComponent<TutorialDirectorBinder>();
            }
        }

        private void Update()
        {
            // 프로젝트의 활성 입력 백엔드와 같은 Input System을 사용해 Scene 진입 시 예외를 막는다.
            if (Keyboard.current?.f9Key.wasPressedThisFrame == true
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
