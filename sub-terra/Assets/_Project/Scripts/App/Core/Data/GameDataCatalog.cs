using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.Core.Data
{
    /// <summary>
    /// 단일 데이터 카탈로그. 에셋을 명시 목록으로 등록하고 영구 ID로만 조회한다.
    /// Resources.LoadAll 같은 암묵적 전역 검색은 사용하지 않는다.
    /// Phase A IDataCatalogPort를 구현해 Bootstrap 검증 게이트에 연결한다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameDataCatalog", menuName = "SubTerra/Data/Game Data Catalog", order = 1)]
    public sealed class GameDataCatalog : ScriptableObject, IDataCatalogPort
    {
        [SerializeField] private List<MineralData> minerals = new List<MineralData>();
        [SerializeField] private List<BuildingData> buildings = new List<BuildingData>();
        [SerializeField] private List<RecipeData> recipes = new List<RecipeData>();
        [SerializeField] private List<UpgradeData> upgrades = new List<UpgradeData>();
        [SerializeField] private List<DialogueTemplateData> dialogues = new List<DialogueTemplateData>();

        private Dictionary<string, MineralData> mineralById;
        private Dictionary<string, BuildingData> buildingById;
        private Dictionary<string, RecipeData> recipeById;
        private Dictionary<string, UpgradeData> upgradeById;
        private Dictionary<string, DialogueTemplateData> dialogueById;
        private bool lookupsBuilt;
        private bool lookupsValid;

        public IReadOnlyList<MineralData> Minerals => minerals;
        public IReadOnlyList<BuildingData> Buildings => buildings;
        public IReadOnlyList<RecipeData> Recipes => recipes;
        public IReadOnlyList<UpgradeData> Upgrades => upgrades;
        public IReadOnlyList<DialogueTemplateData> Dialogues => dialogues;

        /// <summary>전체 검증. 이슈를 모으고 Dictionary 초기화 가능 여부를 결과에 표시한다.</summary>
        public CatalogValidationResult ValidateAll()
        {
            var result = CatalogValidator.Validate(this);
            if (result.DictionaryInitialized)
            {
                BuildLookupsInternal();
            }
            else
            {
                ClearLookups();
            }

            return result;
        }

        /// <summary>Bootstrap 포트. 실패 사유는 민감 정보 없이 요약한다.</summary>
        public bool Validate(out string reason)
        {
            var result = ValidateAll();
            if (result.IsValid)
            {
                reason = null;
                return true;
            }

            reason = result.ToFailureReason();
            return false;
        }

        public bool TryGetMineral(string id, out MineralData data)
        {
            EnsureLookups();
            data = null;
            if (!lookupsValid || string.IsNullOrEmpty(id) || mineralById == null)
            {
                return false;
            }

            return mineralById.TryGetValue(id, out data);
        }

        public bool TryGetBuilding(string id, out BuildingData data)
        {
            EnsureLookups();
            data = null;
            if (!lookupsValid || string.IsNullOrEmpty(id) || buildingById == null)
            {
                return false;
            }

            return buildingById.TryGetValue(id, out data);
        }

        public bool TryGetRecipe(string id, out RecipeData data)
        {
            EnsureLookups();
            data = null;
            if (!lookupsValid || string.IsNullOrEmpty(id) || recipeById == null)
            {
                return false;
            }

            return recipeById.TryGetValue(id, out data);
        }

        public bool TryGetUpgrade(string id, out UpgradeData data)
        {
            EnsureLookups();
            data = null;
            if (!lookupsValid || string.IsNullOrEmpty(id) || upgradeById == null)
            {
                return false;
            }

            return upgradeById.TryGetValue(id, out data);
        }

        public bool TryGetDialogue(string id, out DialogueTemplateData data)
        {
            EnsureLookups();
            data = null;
            if (!lookupsValid || string.IsNullOrEmpty(id) || dialogueById == null)
            {
                return false;
            }

            return dialogueById.TryGetValue(id, out data);
        }

#if UNITY_EDITOR
        public void EditorSetLists(
            List<MineralData> mineralList,
            List<BuildingData> buildingList,
            List<RecipeData> recipeList,
            List<UpgradeData> upgradeList,
            List<DialogueTemplateData> dialogueList)
        {
            minerals = mineralList ?? new List<MineralData>();
            buildings = buildingList ?? new List<BuildingData>();
            recipes = recipeList ?? new List<RecipeData>();
            upgrades = upgradeList ?? new List<UpgradeData>();
            dialogues = dialogueList ?? new List<DialogueTemplateData>();
            ClearLookups();
        }
#endif

        private void EnsureLookups()
        {
            if (lookupsBuilt)
            {
                return;
            }

            // 조회 전에 전체 검증을 다시 거친다. 참조 누락·잘못된 수치 등으로
            // 검증에 실패한 카탈로그는 부분 딕셔너리도 노출하지 않는다.
            var validation = CatalogValidator.Validate(this);
            if (!validation.IsValid)
            {
                ClearLookups();
                lookupsBuilt = true;
                lookupsValid = false;
                return;
            }

            BuildLookupsInternal();
        }

        private void BuildLookupsInternal()
        {
            ClearLookups();

            mineralById = new Dictionary<string, MineralData>();
            buildingById = new Dictionary<string, BuildingData>();
            recipeById = new Dictionary<string, RecipeData>();
            upgradeById = new Dictionary<string, UpgradeData>();
            dialogueById = new Dictionary<string, DialogueTemplateData>();

            var ok = true;
            ok &= Fill(mineralById, minerals, m => m != null ? m.Id : null);
            ok &= Fill(buildingById, buildings, b => b != null ? b.Id : null);
            ok &= Fill(recipeById, recipes, r => r != null ? r.Id : null);
            ok &= Fill(upgradeById, upgrades, u => u != null ? u.Id : null);
            ok &= Fill(dialogueById, dialogues, d => d != null ? d.Id : null);

            lookupsBuilt = true;
            lookupsValid = ok;
            if (!ok)
            {
                ClearLookups();
                lookupsBuilt = true;
                lookupsValid = false;
            }
        }

        private static bool Fill<T>(Dictionary<string, T> map, List<T> source, System.Func<T, string> idOf)
            where T : Object
        {
            if (source == null)
            {
                return true;
            }

            var ok = true;
            for (var i = 0; i < source.Count; i++)
            {
                var entry = source[i];
                if (entry == null)
                {
                    continue;
                }

                var id = idOf(entry);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (map.ContainsKey(id))
                {
                    ok = false;
                    continue;
                }

                map[id] = entry;
            }

            return ok;
        }

        private void ClearLookups()
        {
            mineralById = null;
            buildingById = null;
            recipeById = null;
            upgradeById = null;
            dialogueById = null;
            lookupsBuilt = false;
            lookupsValid = false;
        }

        private void OnEnable()
        {
            ClearLookups();
        }
    }
}
