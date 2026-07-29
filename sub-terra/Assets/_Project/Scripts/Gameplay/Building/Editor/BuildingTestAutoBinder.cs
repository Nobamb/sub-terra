using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.UI.Building;
using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    /// <summary>
    /// Gameplay_Building_Test 씬 단독 실행 시 건설 UI(BuildingMenuBinder)에
    /// 데이터 카탈로그, 테스트 자원 지갑, 게임 상태를 자동으로 바인딩한다.
    /// </summary>
    public sealed class BuildingTestAutoBinder : MonoBehaviour
    {
        [SerializeField] private BuildingMenuBinder menuBinder;
        [SerializeField] private GameDataCatalog catalog;
        [SerializeField] private BuildingPlacementSystem placementSystem;
        [SerializeField] private BuildingPlacementPreview preview;
        [SerializeField] private BuildingPlacementSceneReferences sceneReferences;

        private void Start()
        {
            if (menuBinder == null)
            {
                menuBinder = FindAnyObjectByType<BuildingMenuBinder>();
            }

            if (menuBinder == null)
            {
                return;
            }

            if (catalog == null)
            {
#if UNITY_EDITOR
                catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<GameDataCatalog>(
                    "Assets/_Project/Data/Catalog/GameDataCatalog.asset");
#endif
            }

            if (placementSystem == null)
            {
                placementSystem = FindAnyObjectByType<BuildingPlacementSystem>();
            }

            if (preview == null)
            {
                preview = FindAnyObjectByType<BuildingPlacementPreview>();
            }

            if (sceneReferences == null)
            {
                sceneReferences = FindAnyObjectByType<BuildingPlacementSceneReferences>();
            }

            var gameState = GameState.CreateNew();
            var mineralCatalog = new GameDataCatalogMineralLookup(catalog);
            var inventory = new InventoryService(mineralCatalog, 1000f, gameState);
            var economy = new SubTerra.App.Economy.EconomyService(
                inventory,
                mineralCatalog,
                gameState);

            // 테스트용 기본 자원 및 골드 지급
            gameState.AddGold(1000);
            inventory.AddMineral("mineral.copper", 100);
            inventory.AddMineral("mineral.iron", 100);
            inventory.AddMineral("mineral.lithium", 100);

            // 통합 브릿지 생성
            var bridgeHost = new GameObject("GameplayBuildingPlacementBridge");
            bridgeHost.transform.SetParent(transform);
            var bridge = bridgeHost.AddComponent<GameplayBuildingPlacementBridge>();

#if UNITY_EDITOR
            if (placementSystem != null && preview != null && sceneReferences != null)
            {
                var serialized = new UnityEditor.SerializedObject(bridge);
                var systemProp = serialized.FindProperty("placementSystem");
                var previewProp = serialized.FindProperty("preview");
                var referencesProp = serialized.FindProperty("sceneReferences");
                if (systemProp != null) systemProp.objectReferenceValue = placementSystem;
                if (previewProp != null) previewProp.objectReferenceValue = preview;
                if (referencesProp != null) referencesProp.objectReferenceValue = sceneReferences;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
#endif

            menuBinder.BindTo(economy, inventory, gameState, bridge);
        }
    }
}
