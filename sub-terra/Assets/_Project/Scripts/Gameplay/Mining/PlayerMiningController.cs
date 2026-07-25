using SubTerra.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.Gameplay.Mining
{
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerMiningController : MonoBehaviour
    {
        [SerializeField] private MiningSystem miningSystem;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string mineActionPath = "Player/Attack";
        [SerializeField, Min(0f)] private float reach = 1.1f;

        private PlayerMovement movement;
        private InputAction mineAction;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            if (inputActions != null) mineAction = inputActions.FindAction(mineActionPath, false);
        }

        private void OnEnable() => mineAction?.Enable();

        private void OnDisable()
        {
            mineAction?.Disable();
            miningSystem?.CancelMining();
        }

        private void Update()
        {
            if (miningSystem == null || mineAction == null || !mineAction.IsPressed())
            {
                miningSystem?.CancelMining();
                return;
            }

            if (!miningSystem.IsMining)
            {
                miningSystem.TryStartMiningFrom(movement.Position, movement.FacingDirection, reach);
            }

            miningSystem.TickMining(Time.deltaTime);
        }
    }
}
