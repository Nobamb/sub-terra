using System;
using SubTerra.App.Save;

namespace SubTerra.App.UI.Save
{
    public sealed class SaveSlotPanelPresenter : IDisposable
    {
        private readonly SaveSlotPanelView view;
        private readonly LoadService loader;
        private int selectedSlot = SavePathPolicy.MinimumSlot;

        public event Action<int> ContinueRequested;
        public event Action<int> StartNewGameRequested;

        public SaveSlotPanelPresenter(SaveSlotPanelView panelView, LoadService loadService)
        {
            view = panelView ?? throw new ArgumentNullException(nameof(panelView));
            loader = loadService ?? throw new ArgumentNullException(nameof(loadService));
            view.SlotSelected += SelectSlot;
            view.ContinueRequested += Continue;
            view.RetryRequested += Continue;
            view.StartNewGameRequested += StartNew;
        }

        public void Refresh()
        {
            for (var slot = SavePathPolicy.MinimumSlot;
                slot <= SavePathPolicy.MaximumSlot;
                slot++)
            {
                view.SetSlot(loader.GetSlotMetadata(slot));
            }

            SelectSlot(selectedSlot);
        }

        public void Dispose()
        {
            view.SlotSelected -= SelectSlot;
            view.ContinueRequested -= Continue;
            view.RetryRequested -= Continue;
            view.StartNewGameRequested -= StartNew;
        }

        private void SelectSlot(int slotId)
        {
            selectedSlot = slotId;
            var metadata = loader.GetSlotMetadata(slotId);
            view.SetSelectedSlot(slotId, metadata.HasSave);
        }

        private void Continue()
        {
            var result = loader.Load(selectedSlot);
            view.ShowLoadResult(result);
            if (result.IsSuccess)
            {
                ContinueRequested?.Invoke(selectedSlot);
            }
        }

        private void StartNew()
        {
            StartNewGameRequested?.Invoke(selectedSlot);
        }
    }
}
