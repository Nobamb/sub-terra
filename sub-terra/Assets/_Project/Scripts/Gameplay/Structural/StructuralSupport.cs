using System;
using UnityEngine;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>Represents a placed support pillar or reinforcement in the gameplay layer.</summary>
    public sealed class StructuralSupport : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float radius = 3f;
        [SerializeField, Min(0)] private int strength = 35;

        public float Radius => radius;
        public int Strength => strength;
        private bool isAvailable;
        private bool availabilityInitialized;

        public bool IsAvailable => availabilityInitialized
            ? isAvailable
            : enabled && gameObject.activeInHierarchy;
        public event Action<StructuralSupport> AvailabilityChanged;

        private void OnEnable()
        {
            availabilityInitialized = true;
            isAvailable = true;
            AvailabilityChanged?.Invoke(this);
        }

        private void OnDisable()
        {
            availabilityInitialized = true;
            isAvailable = false;
            AvailabilityChanged?.Invoke(this);
        }

        public bool Supports(Vector3 worldPosition)
        {
            return Vector2.Distance(transform.position, worldPosition) <= radius;
        }
    }
}
