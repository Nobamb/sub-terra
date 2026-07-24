using UnityEngine;

namespace SubTerra.App.Core.Data
{
    /// <summary>드론/시스템 대사 템플릿 정적 정의. 런타임 쿨다운 상태는 넣지 않는다.</summary>
    [CreateAssetMenu(fileName = "DialogueTemplateData", menuName = "SubTerra/Data/Dialogue Template", order = 50)]
    public sealed class DialogueTemplateData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string situationKey;
        [SerializeField] private int priority;
        [SerializeField] [TextArea(2, 6)] private string template;

        public string Id => id;
        public string DisplayName => displayName;
        public string SituationKey => situationKey;
        public int Priority => priority;
        public string Template => template;

#if UNITY_EDITOR
        public void EditorSet(
            string permanentId,
            string name,
            string situation,
            int priorityValue,
            string templateText)
        {
            id = permanentId;
            displayName = name;
            situationKey = situation;
            priority = priorityValue;
            template = templateText;
        }
#endif
    }
}
