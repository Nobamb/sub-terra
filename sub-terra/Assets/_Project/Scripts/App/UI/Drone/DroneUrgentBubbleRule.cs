using SubTerra.App.Core.Data;
using SubTerra.App.Drone.Dialogue;

namespace SubTerra.App.UI.Drone
{
    public static class DroneUrgentBubbleRule
    {
        // B-183 ★ 심각한 급한 말. 판단 규칙은 바꾸지 않고 결과의 템플릿만 분류한다.
        public static readonly System.Collections.Generic.IReadOnlyList<string> DroneSeriousUrgentTemplates =
            new[] { DataIds.Dialogue.DroneEmergency, DataIds.Dialogue.DroneStructuralWarning, DataIds.Dialogue.DroneGasWarning };

        public static bool IsSerious(DroneDialogueResult dialogue)
        {
            if (dialogue == null || !dialogue.IsUrgent) return false;
            foreach (var template in DroneSeriousUrgentTemplates)
                if (dialogue.TemplateId == template) return true;
            return false;
        }
    }
}
