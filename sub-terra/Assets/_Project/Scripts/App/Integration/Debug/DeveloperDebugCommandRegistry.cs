#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.Tutorial;
using SubTerra.Gameplay.Player;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 터미널 명령 한 건. 이후 단계는 Shared.Register 로 이름·별칭·인자 힌트·예시·핸들러를 추가한다.
    /// 이미 있는 별칭은 거절된다. 핸들러는 출력할 한 덩어리를 반환하고, 실패도 예외 대신 문자열로 알린다.
    /// </summary>
    public sealed class DeveloperDebugCommandSpec
    {
        public DeveloperDebugCommandSpec(
            string name,
            string[] aliases,
            string argHint,
            string example,
            Func<string[], DeveloperDebugCommandContext, string> handler)
        {
            Name = name ?? string.Empty;
            Aliases = aliases;
            ArgHint = argHint ?? string.Empty;
            Example = example ?? string.Empty;
            Handler = handler;
        }

        public string Name { get; }
        public string[] Aliases { get; }
        public string ArgHint { get; }
        public string Example { get; }
        public Func<string[], DeveloperDebugCommandContext, string> Handler { get; }
    }

    /// <summary>자동완성에 보이는 명령 이름과 짧은 인자 힌트.</summary>
    public sealed class DeveloperDebugCommandInfo
    {
        public DeveloperDebugCommandInfo(string name, string argHint)
        {
            Name = name ?? string.Empty;
            ArgHint = argHint ?? string.Empty;
        }

        public string Name { get; }
        public string ArgHint { get; }
    }

    /// <summary>
    /// 명령이 읽는 런타임 상태. 골드는 GameState, 화물은 InventoryService,
    /// 플레이어와 퀘스트는 호출 시점에 찾는다.
    /// 진행도, 세이브 런타임, 스폰 대상 탐색을 비우면 플레이 중 인스턴스를 찾는다.
    /// </summary>
    public sealed class DeveloperDebugCommandContext
    {
        private readonly Func<PlayerSurvivalController> findPlayer;
        private readonly Func<TutorialDirectorBinder> findTutorial;
        private readonly Func<ProgressionService> findProgression;
        private readonly Func<string> findSelectedUpgradeId;
        private readonly Func<SaveRuntimeController> findRuntime;
        private readonly Func<DeveloperDebugSpawnWorld> findSpawnWorld;

        public DeveloperDebugCommandContext(
            GameState state,
            InventoryService inventory,
            Func<PlayerSurvivalController> findPlayer,
            Func<TutorialDirectorBinder> findTutorial,
            Func<ProgressionService> findProgression = null,
            Func<string> findSelectedUpgradeId = null,
            Func<SaveRuntimeController> findRuntime = null,
            Func<DeveloperDebugSpawnWorld> findSpawnWorld = null)
        {
            State = state;
            Inventory = inventory;
            this.findPlayer = findPlayer;
            this.findTutorial = findTutorial;
            this.findProgression = findProgression;
            this.findSelectedUpgradeId = findSelectedUpgradeId;
            this.findRuntime = findRuntime;
            this.findSpawnWorld = findSpawnWorld;
        }

        public GameState State { get; }
        public InventoryService Inventory { get; }

        public PlayerSurvivalController FindPlayer()
        {
            if (findPlayer == null)
            {
                return null;
            }

            return findPlayer();
        }

        public TutorialDirectorBinder FindTutorial()
        {
            if (findTutorial == null)
            {
                return null;
            }

            return findTutorial();
        }

        public ProgressionService FindProgression()
        {
            if (findProgression != null)
            {
                return findProgression();
            }

            var runtime = FindRuntime();
            if (runtime == null)
            {
                return null;
            }

            return runtime.Progression;
        }

        public string FindSelectedUpgradeId()
        {
            if (findSelectedUpgradeId != null)
            {
                var selected = findSelectedUpgradeId();
                return string.IsNullOrEmpty(selected) ? string.Empty : selected;
            }

            var binder = UnityEngine.Object.FindAnyObjectByType<ProgressionPanelBinder>(
                FindObjectsInactive.Include);
            if (binder == null || binder.Presenter == null)
            {
                return string.Empty;
            }

            var id = binder.Presenter.SelectedUpgradeId;
            return string.IsNullOrEmpty(id) ? string.Empty : id;
        }

        public SaveRuntimeController FindRuntime()
        {
            if (findRuntime != null)
            {
                return findRuntime();
            }

            return SaveRuntimeController.Instance;
        }

        public DeveloperDebugSpawnWorld FindSpawnWorld()
        {
            if (findSpawnWorld != null)
            {
                return findSpawnWorld();
            }

            return DeveloperDebugSpawnCommands.FindLive();
        }
    }

    /// <summary>
    /// 개발자 터미널 명령 등록부.
    /// 단순 명령은 여기서 두고, 이후 명령은 DeveloperDebugAdvancedCommands와
    /// DeveloperDebugSpawnCommands가 같은 등록부에 넣는다.
    /// </summary>
    public sealed class DeveloperDebugCommandRegistry
    {
        /// <summary>플레이 세션이 쓰는 등록부. 터미널 생성 전후의 Register 가 이 인스턴스에 쌓인다.</summary>
        public static readonly DeveloperDebugCommandRegistry Shared = CreateWithBuiltins();

        private readonly List<Entry> entries = new List<Entry>();
        private readonly Dictionary<string, Entry> byAlias =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        public static DeveloperDebugCommandRegistry CreateIsolated()
        {
            return CreateWithBuiltins();
        }

        public bool Register(DeveloperDebugCommandSpec spec)
        {
            if (spec == null || string.IsNullOrWhiteSpace(spec.Name) || spec.Handler == null)
            {
                return false;
            }

            var aliases = new List<string>();
            AddAlias(aliases, spec.Name);
            if (spec.Aliases != null)
            {
                for (var i = 0; i < spec.Aliases.Length; i++)
                {
                    AddAlias(aliases, spec.Aliases[i]);
                }
            }

            if (aliases.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < aliases.Count; i++)
            {
                if (byAlias.ContainsKey(aliases[i]))
                {
                    return false;
                }
            }

            var entry = new Entry(
                spec.Name.Trim(),
                aliases.ToArray(),
                spec.ArgHint,
                spec.Example,
                spec.Handler);
            entries.Add(entry);
            for (var i = 0; i < entry.Aliases.Length; i++)
            {
                byAlias.Add(entry.Aliases[i], entry);
            }

            return true;
        }

        public string Execute(string line, DeveloperDebugCommandContext context)
        {
            var tokens = Tokenize(line);
            if (tokens.Count == 0)
            {
                return null;
            }

            if (!byAlias.TryGetValue(tokens[0], out var entry))
            {
                return "알 수 없는 명령: " + tokens[0];
            }

            try
            {
                return entry.Handler(tokens.ToArray(), context);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SubTerra] Debug command failed: " + exception.GetType().Name);
                return "명령 실행 실패";
            }
        }

        public void CollectPrefix(string token, List<DeveloperDebugCommandInfo> destination)
        {
            if (destination == null || string.IsNullOrEmpty(token))
            {
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!MatchesPrefix(entry, token))
                {
                    continue;
                }

                destination.Add(new DeveloperDebugCommandInfo(entry.Name, entry.ArgHint));
            }
        }

        public string BuildHelp()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(entry.Name);
                if (!string.IsNullOrEmpty(entry.ArgHint))
                {
                    builder.Append(' ');
                    builder.Append(entry.ArgHint);
                }

                builder.Append("\n  별칭: ");
                builder.Append(string.Join(", ", entry.Aliases));
                builder.Append("\n  예: ");
                builder.Append(entry.Example);
            }

            return builder.ToString();
        }

        private static DeveloperDebugCommandRegistry CreateWithBuiltins()
        {
            var registry = new DeveloperDebugCommandRegistry();
            RegisterBuiltins(registry);
            DeveloperDebugAdvancedCommands.Register(registry);
            DeveloperDebugSpawnCommands.Register(registry);
            return registry;
        }

        private static void RegisterBuiltins(DeveloperDebugCommandRegistry registry)
        {
            registry.Register(new DeveloperDebugCommandSpec(
                "gold",
                new[] { "gold", "골드" },
                "<n>",
                "gold 100",
                HandleGold));
            RegisterCargo(registry, "copper", new[] { "copper", "구리", DataIds.Minerals.Copper }, DataIds.Minerals.Copper, "copper 10");
            RegisterCargo(registry, "iron", new[] { "iron", "철", DataIds.Minerals.Iron }, DataIds.Minerals.Iron, "iron -5");
            RegisterCargo(registry, "lithium", new[] { "lithium", "리튬", DataIds.Minerals.Lithium }, DataIds.Minerals.Lithium, "lithium 3");
            RegisterCargo(
                registry,
                "engine_fuel",
                new[] { "engine_fuel", "엔진연료", DataIds.RareItems.EngineFuel },
                DataIds.RareItems.EngineFuel,
                "engine_fuel 1");
            registry.Register(new DeveloperDebugCommandSpec(
                "energy",
                new[] { "energy", "전력" },
                "<n|full|가득>",
                "energy full",
                HandleEnergy));
            registry.Register(new DeveloperDebugCommandSpec(
                "hp",
                new[] { "hp", "체력" },
                "<n|full|가득>",
                "hp 50",
                HandleHealth));
            registry.Register(new DeveloperDebugCommandSpec(
                "questclear",
                new[] { "questclear", "qc", "퀘스트클리어", "퀘클" },
                string.Empty,
                "questclear",
                HandleQuest));
            registry.Register(new DeveloperDebugCommandSpec(
                "help",
                new[] { "help", "도움말", "h" },
                string.Empty,
                "help",
                (tokens, context) => HandleHelp(tokens, registry)));
        }

        private static void RegisterCargo(
            DeveloperDebugCommandRegistry registry,
            string name,
            string[] aliases,
            string itemId,
            string example)
        {
            registry.Register(new DeveloperDebugCommandSpec(
                name,
                aliases,
                "<n>",
                example,
                (tokens, context) => HandleCargo(tokens, context, name, itemId, example)));
        }

        private static string HandleHelp(string[] tokens, DeveloperDebugCommandRegistry registry)
        {
            if (tokens == null || tokens.Length != 1)
            {
                return "help 인자 오류. 예: help";
            }

            return registry.BuildHelp();
        }

        private static string HandleGold(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (!TryAmount(tokens, out var amount))
            {
                return "gold 인자 오류. 예: gold 100";
            }

            if (context == null || context.State == null || context.State.Player == null)
            {
                return "GameState 없음";
            }

            var before = context.State.Player.Gold;
            var next = (long)before + amount;
            if (next > int.MaxValue)
            {
                next = int.MaxValue;
            }

            if (next < 0)
            {
                next = 0;
            }

            context.State.SetGold((int)next);
            return "gold " + before.ToString(CultureInfo.InvariantCulture)
                + " -> " + context.State.Player.Gold.ToString(CultureInfo.InvariantCulture);
        }

        private static string HandleCargo(
            string[] tokens,
            DeveloperDebugCommandContext context,
            string name,
            string itemId,
            string example)
        {
            if (!TryAmount(tokens, out var amount) || amount == 0)
            {
                return name + " 인자 오류. 예: " + example;
            }

            if (context == null || context.Inventory == null)
            {
                return "인벤토리 없음";
            }

            var inventory = context.Inventory;
            if (inventory.CatalogLookup == null
                || !inventory.CatalogLookup.TryGetMineral(itemId, out _))
            {
                return "알 수 없는 ID: " + itemId;
            }

            if (amount > 0)
            {
                var added = inventory.TryAddMineral(itemId, amount);
                if (added.Status == InventoryMutationStatus.InvalidId
                    || added.Status == InventoryMutationStatus.CatalogMissing)
                {
                    return "알 수 없는 ID: " + itemId;
                }

                if (added.Status == InventoryMutationStatus.OverflowRisk
                    || added.Status == InventoryMutationStatus.InvalidQuantity)
                {
                    return name + " 실패. 보유 "
                        + inventory.State.GetQuantity(itemId).ToString(CultureInfo.InvariantCulture);
                }

                return name
                    + " 담음 " + added.AcceptedQuantity.ToString(CultureInfo.InvariantCulture)
                    + ", 버림 " + added.RejectedQuantity.ToString(CultureInfo.InvariantCulture)
                    + ", 보유 " + inventory.State.GetQuantity(itemId).ToString(CultureInfo.InvariantCulture);
            }

            var magnitude = amount == int.MinValue
                ? (long)int.MaxValue + 1L
                : (long)-amount;
            var held = inventory.State.GetQuantity(itemId);
            var remove = magnitude > held ? held : (int)magnitude;
            if (remove > 0)
            {
                var reduced = inventory.TryReduceMineral(itemId, remove);
                if (reduced.Status != InventoryMutationStatus.Success)
                {
                    return name + " 실패. 보유 "
                        + inventory.State.GetQuantity(itemId).ToString(CultureInfo.InvariantCulture);
                }
            }

            return name
                + " 차감 " + remove.ToString(CultureInfo.InvariantCulture)
                + ", 보유 " + inventory.State.GetQuantity(itemId).ToString(CultureInfo.InvariantCulture);
        }

        private static string HandleEnergy(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (!TryScalar(tokens, out var amount, out var full))
            {
                return "energy 인자 오류. 예: energy full";
            }

            if (context == null || context.State == null || context.State.Player == null)
            {
                return "GameState 없음";
            }

            var value = full ? context.State.Player.MaxEnergy : amount;
            context.State.SetCurrentEnergy(value);
            var energy = context.State.GetEnergy();
            return "energy " + energy.Current.ToString(CultureInfo.InvariantCulture)
                + "/" + energy.Max.ToString(CultureInfo.InvariantCulture);
        }

        private static string HandleHealth(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (!TryScalar(tokens, out var amount, out var full))
            {
                return "hp 인자 오류. 예: hp 50";
            }

            if (context == null)
            {
                return "플레이어 없음";
            }

            var player = context.FindPlayer();
            if (player == null)
            {
                return "플레이어 없음";
            }

            if (full)
            {
                player.RestoreFull();
            }
            else
            {
                player.SetHealthAbsolute(amount);
            }

            var health = player.GetHealth();
            return "hp " + health.Current.ToString("0", CultureInfo.InvariantCulture)
                + "/" + health.Maximum.ToString(CultureInfo.InvariantCulture);
        }

        private static string HandleQuest(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (tokens == null || tokens.Length != 1)
            {
                return "questclear 인자 오류. 예: questclear";
            }

            if (context == null)
            {
                return "퀘스트 디렉터 없음";
            }

            var binder = context.FindTutorial();
            if (binder == null || binder.Director == null)
            {
                return "퀘스트 디렉터 없음";
            }

            if (binder.Director.IsDemoComplete)
            {
                return "데모 완료 상태라 진행하지 않음";
            }

            var before = binder.Director.CurrentObjectiveId;
            binder.DebugForceAdvanceObjective();
            if (!binder.Director.IsDemoComplete
                && binder.Director.CurrentObjectiveId == before)
            {
                return "퀘스트가 진행되지 않음";
            }

            return "퀘스트 진행: " + binder.Director.CurrentObjectiveId;
        }

        private static bool TryAmount(string[] tokens, out int amount)
        {
            amount = 0;
            if (tokens == null || tokens.Length != 2)
            {
                return false;
            }

            return TryParseInt(tokens[1], out amount);
        }

        private static bool TryScalar(
            string[] tokens,
            out int amount,
            out bool full)
        {
            amount = 0;
            full = false;
            if (tokens == null || tokens.Length != 2)
            {
                return false;
            }

            if (IsFullWord(tokens[1]))
            {
                full = true;
                return true;
            }

            return TryParseInt(tokens[1], out amount);
        }

        private static bool TryParseInt(string token, out int amount)
        {
            return int.TryParse(
                token,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out amount);
        }

        private static bool IsFullWord(string token)
        {
            return string.Equals(token, "full", StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "가득", StringComparison.Ordinal);
        }

        private static bool MatchesPrefix(Entry entry, string token)
        {
            for (var i = 0; i < entry.Aliases.Length; i++)
            {
                if (entry.Aliases[i].StartsWith(token, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddAlias(List<string> aliases, string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                return;
            }

            var trimmed = alias.Trim();
            for (var i = 0; i < aliases.Count; i++)
            {
                if (string.Equals(aliases[i], trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            aliases.Add(trimmed);
        }

        private static List<string> Tokenize(string line)
        {
            var tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(line))
            {
                return tokens;
            }

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                tokens.Add(parts[i]);
            }

            return tokens;
        }

        private sealed class Entry
        {
            public Entry(
                string name,
                string[] aliases,
                string argHint,
                string example,
                Func<string[], DeveloperDebugCommandContext, string> handler)
            {
                Name = name;
                Aliases = aliases;
                ArgHint = argHint ?? string.Empty;
                Example = example ?? string.Empty;
                Handler = handler;
            }

            public string Name { get; }
            public string[] Aliases { get; }
            public string ArgHint { get; }
            public string Example { get; }
            public Func<string[], DeveloperDebugCommandContext, string> Handler { get; }
        }
    }

    /// <summary>
    /// 입력 줄의 자동완성. 후보가 떠 있으면 Enter 는 확정만 하고, 그 다음 Enter 가 실행한다.
    /// </summary>
    public sealed class DeveloperDebugCommandSession
    {
        private readonly DeveloperDebugCommandRegistry registry;
        private readonly List<DeveloperDebugCommandInfo> candidates = new List<DeveloperDebugCommandInfo>();
        private int selected;

        public DeveloperDebugCommandSession(DeveloperDebugCommandRegistry registry)
        {
            this.registry = registry ?? DeveloperDebugCommandRegistry.CreateIsolated();
        }

        public int CandidateCount => candidates.Count;

        public string SelectedName
        {
            get
            {
                if (candidates.Count == 0)
                {
                    return string.Empty;
                }

                var index = selected;
                if (index < 0 || index >= candidates.Count)
                {
                    index = 0;
                }

                return candidates[index].Name;
            }
        }

        public void NotifyTextChanged(string text)
        {
            selected = 0;
            candidates.Clear();
            var token = CommandToken(text);
            if (token == null || registry == null)
            {
                return;
            }

            registry.CollectPrefix(token, candidates);
        }

        public void Move(int delta)
        {
            if (candidates.Count == 0 || delta == 0)
            {
                return;
            }

            selected += delta;
            if (selected < 0)
            {
                selected = candidates.Count - 1;
            }
            else if (selected >= candidates.Count)
            {
                selected = 0;
            }
        }

        public bool TryConfirm(out string confirmed)
        {
            confirmed = null;
            if (candidates.Count == 0)
            {
                return false;
            }

            var index = selected;
            if (index < 0 || index >= candidates.Count)
            {
                index = 0;
            }

            var command = candidates[index];
            confirmed = string.IsNullOrEmpty(command.ArgHint)
                ? command.Name
                : command.Name + " ";
            candidates.Clear();
            selected = 0;
            return true;
        }

        public string FormatCandidates()
        {
            if (candidates.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < candidates.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(i == selected ? "> " : "  ");
                builder.Append(candidates[i].Name);
                if (!string.IsNullOrEmpty(candidates[i].ArgHint))
                {
                    builder.Append(' ');
                    builder.Append(candidates[i].ArgHint);
                }
            }

            return builder.ToString();
        }

        public string Execute(string line, DeveloperDebugCommandContext context)
        {
            if (registry == null)
            {
                return "명령 실행 실패";
            }

            return registry.Execute(line, context);
        }

        private static string CommandToken(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var start = 0;
            while (start < text.Length && (text[start] == ' ' || text[start] == '\t'))
            {
                start++;
            }

            if (start >= text.Length)
            {
                return null;
            }

            for (var i = start; i < text.Length; i++)
            {
                if (text[i] == ' ' || text[i] == '\t')
                {
                    return null;
                }
            }

            return text.Substring(start);
        }
    }
}
#endif
