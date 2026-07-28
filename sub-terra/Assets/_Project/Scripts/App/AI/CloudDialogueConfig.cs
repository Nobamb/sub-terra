using UnityEngine;

namespace SubTerra.App.AI
{
    /// <summary>
    /// 자체 endpoint의 공개 주소와 호출 정책만 보관한다. 제공자 API 키나 인증 비밀은 이 에셋에 두지 않는다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CloudDialogueConfig",
        menuName = "SubTerra/AI/Cloud Dialogue Config",
        order = 70)]
    public sealed class CloudDialogueConfig : ScriptableObject
    {
        [SerializeField] private bool cloudEnabled;
        [SerializeField] private string endpoint = string.Empty;
        [SerializeField] private string language = "ko";
        [SerializeField, Min(1)] private int timeoutMilliseconds = 1500;
        [SerializeField, Min(0f)] private float globalCooldownSeconds = 2f;
        [SerializeField, Min(0f)] private float duplicateEventWindowSeconds = 10f;
        [SerializeField, Min(0)] private int maxSessionCalls = 8;
        [SerializeField, Min(0)] private int maxCallsPerEvent = 2;
        [SerializeField, Min(1)] private int maxResponseCharacters = 240;

        public CloudDialogueOptions CreateOptions()
        {
            return new CloudDialogueOptions(
                cloudEnabled,
                endpoint,
                language,
                timeoutMilliseconds,
                globalCooldownSeconds,
                duplicateEventWindowSeconds,
                maxSessionCalls,
                maxCallsPerEvent,
                maxResponseCharacters);
        }
    }
}
