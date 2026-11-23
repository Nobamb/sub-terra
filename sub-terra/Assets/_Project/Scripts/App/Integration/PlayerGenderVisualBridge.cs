using System;
using SubTerra.App.State;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// B-208: 현재 게임의 캐릭터 성별(PlayerGenderPresentation)을 플레이어 그림 세트에 넣는다.
    /// 성별이 바뀌면 상태·채굴/점프 진행은 그대로 두고 다음 그리기부터 그림만 바뀐다.
    /// </summary>
    public sealed class PlayerGenderVisualBridge : MonoBehaviour
    {
        [SerializeField] private PlayerAnimationController animationController;
        private bool subscribed;

        public PlayerAnimationController AnimationController => animationController;

        /// <summary>플레이어 그림 오브젝트에 다리를 붙이고 지금 성별을 바로 적용한다.</summary>
        public static PlayerGenderVisualBridge Attach(PlayerAnimationController controller)
        {
            if (controller == null)
            {
                return null;
            }

            var bridge = controller.GetComponent<PlayerGenderVisualBridge>();
            if (bridge == null)
            {
                bridge = controller.gameObject.AddComponent<PlayerGenderVisualBridge>();
            }

            bridge.Bind(controller);
            return bridge;
        }

        public void Bind(PlayerAnimationController controller)
        {
            animationController = controller;
            Subscribe();
            Apply(PlayerGenderPresentation.Current);
        }

        private void OnEnable()
        {
            if (animationController == null)
            {
                animationController = GetComponent<PlayerAnimationController>();
            }

            Subscribe();
            Apply(PlayerGenderPresentation.Current);
        }

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (subscribed)
            {
                PlayerGenderPresentation.Changed -= Apply;
                subscribed = false;
            }
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            PlayerGenderPresentation.Changed += Apply;
            subscribed = true;
        }

        private void Apply(PlayerGender gender)
        {
            try
            {
                if (animationController != null && animationController.Gender != gender
                    && !animationController.SetGender(gender))
                {
                    Debug.LogWarning("[SubTerra] Player animation set missing for gender: " + gender);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
