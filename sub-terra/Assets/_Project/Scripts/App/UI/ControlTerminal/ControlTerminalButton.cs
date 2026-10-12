namespace SubTerra.App.UI.ControlTerminal
{
    public sealed class ControlTerminalButton : UnityEngine.UI.Button
    {
        public const float HoverSeconds = 0.3f;
        private UnityEngine.UI.Image hoverImage;

        public void BindHover(UnityEngine.UI.Image image)
        {
            hoverImage = image;
            transition = Transition.None;
            DoStateTransition(currentSelectionState, true);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            if (hoverImage == null) return;
            var highlighted = state == SelectionState.Highlighted || state == SelectionState.Selected || state == SelectionState.Pressed;
            hoverImage.CrossFadeAlpha(highlighted ? 1f : 0f, instant ? 0f : HoverSeconds, true);
        }
    }
}
