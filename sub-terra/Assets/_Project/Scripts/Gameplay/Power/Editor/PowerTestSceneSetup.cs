using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.Gameplay.Power.Editor
{
    public static class PowerTestSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Test/Gameplay/Gameplay_Power_Test.unity";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Gameplay/Power";

        [MenuItem("Tools/SubTerra/Setup Power Test Scene")]
        public static void CreateScene()
        {
            EnsureFolder("Assets/_Project/Scenes/Test/Gameplay");
            EnsureFolder(PrefabFolder);
            CreateRuntimePrefabs();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("GameplayRoot");
            PowerNetworkSystem network = new GameObject("PowerNetwork").AddComponent<PowerNetworkSystem>();
            network.transform.SetParent(root.transform);

            PowerNode core = CreateNode(root.transform, "OutpostCore", new Vector3(-4f, 0f, 0f), network, true, 5, 0, PowerPriority.Critical, new Color(0.2f, 0.72f, 1f));
            PowerNode light = CreateNode(root.transform, "LightFacility", Vector3.zero, network, false, 0, 2, PowerPriority.High, new Color(1f, 0.88f, 0.25f));
            PowerNode charger = CreateNode(root.transform, "ChargerFacility", new Vector3(4f, 0f, 0f), network, false, 0, 4, PowerPriority.Low, new Color(0.3f, 0.95f, 0.45f));
            CreateCable(root.transform, network, core, light);
            CreateCable(root.transform, network, light, charger);
            CreateCamera();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {ScenePath}. The 5-power core supplies the high-priority light before the low-priority charger.");
        }

        private static PowerNode CreateNode(Transform root, string name, Vector3 position, PowerNetworkSystem network, bool source, int supply, int demand, PowerPriority priority, Color color)
        {
            GameObject nodeObject = new(name); nodeObject.transform.SetParent(root); nodeObject.transform.position = position;
            SpriteRenderer baseRenderer = nodeObject.AddComponent<SpriteRenderer>();
            baseRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); baseRenderer.color = color;
            PowerNode node = nodeObject.AddComponent<PowerNode>(); node.Configure(network, source, supply, demand, priority);
            if (!source)
            {
                GameObject poweredVisual = new("PoweredVisual"); poweredVisual.transform.SetParent(nodeObject.transform); poweredVisual.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                SpriteRenderer visualRenderer = poweredVisual.AddComponent<SpriteRenderer>(); visualRenderer.sprite = baseRenderer.sprite; visualRenderer.color = Color.white;
                PowerFacility facility = nodeObject.AddComponent<PowerFacility>();
                SetReference(facility, "powerNode", node);
                SetObjectArray(facility, "poweredVisuals", new Object[] { poweredVisual });
            }
            return node;
        }

        private static void CreateCable(Transform root, PowerNetworkSystem network, PowerNode first, PowerNode second)
        {
            GameObject cableObject = new($"Cable_{first.name}_{second.name}"); cableObject.transform.SetParent(root);
            cableObject.transform.position = (first.transform.position + second.transform.position) * 0.5f;
            SpriteRenderer renderer = cableObject.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); renderer.color = new Color(0.45f, 0.48f, 0.52f);
            cableObject.transform.localScale = new Vector3(Vector3.Distance(first.transform.position, second.transform.position), 0.12f, 1f);
            cableObject.AddComponent<PowerCable>().Configure(network, first, second);
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new("Main Camera"); cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 4f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateRuntimePrefabs()
        {
            CreateNodePrefab("OutpostCore", true, 5, 0, PowerPriority.Critical, new Color(0.2f, 0.72f, 1f));
            CreateNodePrefab("LightFacility", false, 0, 2, PowerPriority.High, new Color(1f, 0.88f, 0.25f));
            CreateNodePrefab("ChargerFacility", false, 0, 4, PowerPriority.Low, new Color(0.3f, 0.95f, 0.45f));
            string cablePath = $"{PrefabFolder}/PowerCable.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(cablePath) != null) return;
            GameObject cable = new("PowerCable");
            SpriteRenderer renderer = cable.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); renderer.color = new Color(0.45f, 0.48f, 0.52f);
            cable.AddComponent<PowerCable>();
            PrefabUtility.SaveAsPrefabAsset(cable, cablePath);
            Object.DestroyImmediate(cable);
        }

        private static void CreateNodePrefab(string name, bool source, int supply, int demand, PowerPriority priority, Color color)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject nodeObject = new(name);
            SpriteRenderer renderer = nodeObject.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); renderer.color = color;
            PowerNode node = nodeObject.AddComponent<PowerNode>(); node.Configure(null, source, supply, demand, priority);
            if (!source) nodeObject.AddComponent<PowerFacility>();
            PrefabUtility.SaveAsPrefabAsset(nodeObject, path);
            Object.DestroyImmediate(nodeObject);
        }

        private static void SetReference(Object target, string name, Object value)
        {
            SerializedObject serialized = new(target); serialized.FindProperty(name).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(Object target, string name, Object[] values)
        {
            SerializedObject serialized = new(target); SerializedProperty property = serialized.FindProperty(name); property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/'); string current = parts[0];
            for (int index = 1; index < parts.Length; index++) { string next = $"{current}/{parts[index]}"; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]); current = next; }
        }
    }
}
