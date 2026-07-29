using SubTerra.App.Drone.Dialogue;
using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.Drone
{
    /// <summary>확정된 템플릿 대사만 표시하는 View.</summary>
    public sealed class DroneDialoguePanelView : MonoBehaviour, IDroneDialogueView
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text dialogueText;

        public void SetDialogue(DroneDialogueResult dialogue)
        {
            if (dialogueText != null && dialogue != null && !dialogue.IsSuppressed)
            {
                dialogueText.text = dialogue.Text;
            }
        }

        public void SetVisible(bool visible)
        {
            (panelRoot != null ? panelRoot : gameObject).SetActive(visible);
        }

        public bool HasRequiredReferences()
        {
            return panelRoot != null && dialogueText != null;
        }
    }
}
