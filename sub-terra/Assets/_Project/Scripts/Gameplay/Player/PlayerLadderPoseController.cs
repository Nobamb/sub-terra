using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    public sealed class PlayerLadderPoseController : MonoBehaviour
    {
        private const float MotionEpsilon = 0.01f;

        [SerializeField] private GameObject rigRoot;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField, Min(0f)] private float cyclesPerSecond = 1.6f;
        [SerializeField, Min(0f)] private float armSwingDegrees = 16f;
        [SerializeField, Min(0f)] private float legSwingDegrees = 10f;
        [SerializeField, Min(0f)] private float armTravel = 0.045f;
        [SerializeField, Min(0f)] private float legTravel = 0.035f;

        private Vector3 torsoNeutralPosition;
        private Quaternion torsoNeutralRotation;
        private Vector3 leftArmNeutralPosition;
        private Vector3 rightArmNeutralPosition;
        private Vector3 leftLegNeutralPosition;
        private Vector3 rightLegNeutralPosition;
        private Quaternion leftArmNeutralRotation;
        private Quaternion rightArmNeutralRotation;
        private Quaternion leftLegNeutralRotation;
        private Quaternion rightLegNeutralRotation;
        private float phase;
        private bool neutralPoseCaptured;

        public bool IsVisible => rigRoot != null && rigRoot.activeSelf;

        private void Awake()
        {
            CaptureNeutralPose();
            Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        public void Configure(
            GameObject root,
            Transform torsoTransform,
            Transform leftArmTransform,
            Transform rightArmTransform,
            Transform leftLegTransform,
            Transform rightLegTransform)
        {
            rigRoot = root;
            torso = torsoTransform;
            leftArm = leftArmTransform;
            rightArm = rightArmTransform;
            leftLeg = leftLegTransform;
            rightLeg = rightLegTransform;
            neutralPoseCaptured = false;
            CaptureNeutralPose();
            Hide();
        }

        public void Show(float signedMotionInput, float deltaTime)
        {
            if (!CaptureNeutralPose())
            {
                return;
            }

            if (!rigRoot.activeSelf)
            {
                rigRoot.SetActive(true);
            }

            if (Mathf.Abs(signedMotionInput) <= MotionEpsilon)
            {
                phase = 0f;
                ApplyPose(0f);
                return;
            }

            phase = Mathf.Repeat(
                phase + signedMotionInput * cyclesPerSecond * Mathf.PI * 2f * Mathf.Max(0f, deltaTime),
                Mathf.PI * 2f);
            ApplyPose(Mathf.Sin(phase));
        }

        public void Hide()
        {
            phase = 0f;
            if (CaptureNeutralPose())
            {
                ApplyPose(0f);
            }

            if (rigRoot != null && rigRoot.activeSelf)
            {
                rigRoot.SetActive(false);
            }
        }

        private bool CaptureNeutralPose()
        {
            if (neutralPoseCaptured)
            {
                return true;
            }

            if (rigRoot == null
                || torso == null
                || leftArm == null
                || rightArm == null
                || leftLeg == null
                || rightLeg == null)
            {
                return false;
            }

            torsoNeutralPosition = torso.localPosition;
            torsoNeutralRotation = torso.localRotation;
            leftArmNeutralPosition = leftArm.localPosition;
            rightArmNeutralPosition = rightArm.localPosition;
            leftLegNeutralPosition = leftLeg.localPosition;
            rightLegNeutralPosition = rightLeg.localPosition;
            leftArmNeutralRotation = leftArm.localRotation;
            rightArmNeutralRotation = rightArm.localRotation;
            leftLegNeutralRotation = leftLeg.localRotation;
            rightLegNeutralRotation = rightLeg.localRotation;
            neutralPoseCaptured = true;
            return true;
        }

        private void ApplyPose(float alternatingPhase)
        {
            torso.localPosition = torsoNeutralPosition;
            torso.localRotation = torsoNeutralRotation;

            leftArm.localPosition = leftArmNeutralPosition + Vector3.up * (alternatingPhase * armTravel);
            rightArm.localPosition = rightArmNeutralPosition - Vector3.up * (alternatingPhase * armTravel);
            leftLeg.localPosition = leftLegNeutralPosition - Vector3.up * (alternatingPhase * legTravel);
            rightLeg.localPosition = rightLegNeutralPosition + Vector3.up * (alternatingPhase * legTravel);

            leftArm.localRotation = leftArmNeutralRotation
                * Quaternion.Euler(0f, 0f, alternatingPhase * armSwingDegrees);
            rightArm.localRotation = rightArmNeutralRotation
                * Quaternion.Euler(0f, 0f, -alternatingPhase * armSwingDegrees);
            leftLeg.localRotation = leftLegNeutralRotation
                * Quaternion.Euler(0f, 0f, -alternatingPhase * legSwingDegrees);
            rightLeg.localRotation = rightLegNeutralRotation
                * Quaternion.Euler(0f, 0f, alternatingPhase * legSwingDegrees);
        }
    }
}
