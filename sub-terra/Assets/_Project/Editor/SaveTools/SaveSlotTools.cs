using SubTerra.App.Save;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.SaveTools
{
    public static class SaveSlotTools
    {
        [MenuItem("SubTerra/Save/Inspect Slots")]
        public static void InspectSlots()
        {
            var loader = LoadService.CreateDefault();
            for (var slot = SavePathPolicy.MinimumSlot;
                slot <= SavePathPolicy.MaximumSlot;
                slot++)
            {
                var metadata = loader.GetSlotMetadata(slot);
                Debug.Log(
                    "[SubTerra] Save slot " + slot
                    + " exists=" + metadata.HasSave
                    + " backup=" + metadata.IsRecoverableFromBackup
                    + " version=" + metadata.SaveVersion);
            }
        }

        [MenuItem("SubTerra/Save/Delete Slot 1")]
        public static void DeleteSlot1() => DeleteSlot(1);

        [MenuItem("SubTerra/Save/Delete Slot 2")]
        public static void DeleteSlot2() => DeleteSlot(2);

        [MenuItem("SubTerra/Save/Delete Slot 3")]
        public static void DeleteSlot3() => DeleteSlot(3);

        private static void DeleteSlot(int slotId)
        {
            if (!EditorUtility.DisplayDialog(
                "Sub-Terra Save",
                "개발용 슬롯 " + slotId + "의 정상/백업/tmp 파일을 삭제할까요?",
                "삭제",
                "취소"))
            {
                return;
            }

            var deleted = SaveService.CreateDefault().DeleteSlot(slotId);
            Debug.Log("[SubTerra] Save slot " + slotId + " delete=" + deleted);
        }
    }
}
