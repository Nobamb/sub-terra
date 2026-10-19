using System;
using SubTerra.App.UI.Outpost;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>기존 시설 팝업을 보존하며 더 이상 사용하지 않는 큰 패널만 제거한다.</summary>
    public static class PhaseHOutpostUiPrefabBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/OutpostPanel.prefab";

        [MenuItem("SubTerra/UI/Remove Legacy Outpost Panel")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + BuildPrefab());
        }

        // PhaseQ의 기존 호출 진입점은 유지한다. 구 화면을 재생성하지 않는다.
        public static string BuildPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var view = root.GetComponent<OutpostPanelView>();
                if (view == null || root.GetComponent<OutpostPanelBinder>() == null
                    || view.CorePopup == null || view.ServicePopup == null)
                {
                    throw new InvalidOperationException("현재 시설 팝업 연결이 없는 프리팹은 정리할 수 없습니다.");
                }

                var legacy = root.transform.Find("PanelRoot");
                if (legacy == null)
                {
                    if (!view.HasRequiredReferences())
                        throw new InvalidOperationException("시설 팝업 필수 연결을 확인하세요.");
                    return "Outpost popup prefab already cleaned: " + PrefabPath;
                }

                var result = legacy.Find("ResultText")?.GetComponent<TMP_Text>();
                var font = result != null ? result.font : null;
                if (font == null)
                    throw new InvalidOperationException("경고창 폰트를 보존할 수 없어 정리를 중단합니다.");
                if (view.CorePopup.transform.IsChildOf(legacy) || view.ServicePopup.transform.IsChildOf(legacy))
                    throw new InvalidOperationException("현재 팝업이 옛 패널 안에 있어 정리를 중단합니다.");

                var serialized = new SerializedObject(view);
                serialized.FindProperty("messageFont").objectReferenceValue = font;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                UnityEngine.Object.DestroyImmediate(legacy.gameObject);
                if (!view.HasRequiredReferences())
                    throw new InvalidOperationException("정리 후 시설 팝업 연결이 유효하지 않습니다.");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                return "Removed legacy Outpost PanelRoot; preserved modern popups and warning font: " + PrefabPath;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
