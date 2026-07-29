using SubTerra.App.Drone.Dialogue;

namespace SubTerra.App.UI.Drone
{
    public interface IDroneDialogueView
    {
        void SetDialogue(DroneDialogueResult dialogue);
        void SetVisible(bool visible);
    }
}
