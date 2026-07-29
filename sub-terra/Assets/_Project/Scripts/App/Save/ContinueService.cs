using System;
using System.Collections.Generic;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.Core;
using SubTerra.Shared;

namespace SubTerra.App.Save
{
    public interface IRestoredStateReceiver
    {
        bool RestoreBState(RestoredSaveState state);
    }

    public interface IWorldSnapshotResolver
    {
        IWorldSnapshotProvider Resolve();
    }

    public interface IDerivedStateRecalculator
    {
        bool Recalculate();
    }

    public interface ILoadedUiGate
    {
        void SetReady(bool ready);
    }

    public enum ContinueStatus
    {
        Success = 0,
        LoadFailed = 1,
        StateRestoreFailed = 2,
        SceneLoadFailed = 3,
        WorldProviderMissing = 4,
        WorldRestoreFailed = 5,
        RecalculationFailed = 6
    }

    public sealed class ContinueResult
    {
        public ContinueStatus Status { get; }
        public LoadResult Load { get; }
        public bool IsSuccess => Status == ContinueStatus.Success;

        public ContinueResult(ContinueStatus status, LoadResult load)
        {
            Status = status;
            Load = load;
        }
    }

    /// <summary>B State → Scene → A World → 파생 재계산 → UI 순서를 고정한다.</summary>
    public sealed class ContinueService
    {
        private readonly LoadService loadService;
        private readonly IRestoredStateReceiver stateReceiver;
        private readonly ISceneLoader scenes;
        private readonly IWorldSnapshotResolver worldResolver;
        private readonly IDerivedStateRecalculator recalculator;
        private readonly ILoadedUiGate uiGate;

        public ContinueService(
            LoadService loader,
            IRestoredStateReceiver restoredStateReceiver,
            ISceneLoader sceneLoader,
            IWorldSnapshotResolver snapshotResolver,
            IDerivedStateRecalculator derivedStateRecalculator,
            ILoadedUiGate loadedUiGate)
        {
            loadService = loader ?? throw new ArgumentNullException(nameof(loader));
            stateReceiver = restoredStateReceiver
                ?? throw new ArgumentNullException(nameof(restoredStateReceiver));
            scenes = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            worldResolver = snapshotResolver
                ?? throw new ArgumentNullException(nameof(snapshotResolver));
            recalculator = derivedStateRecalculator
                ?? throw new ArgumentNullException(nameof(derivedStateRecalculator));
            uiGate = loadedUiGate ?? throw new ArgumentNullException(nameof(loadedUiGate));
        }

        public ContinueResult Continue(int slotId)
        {
            uiGate.SetReady(false);
            var load = loadService.Load(slotId);
            if (!load.IsSuccess || load.State == null)
            {
                return new ContinueResult(ContinueStatus.LoadFailed, load);
            }

            if (!stateReceiver.RestoreBState(load.State))
            {
                return new ContinueResult(ContinueStatus.StateRestoreFailed, load);
            }

            if (!scenes.Load(load.State.TargetSceneName))
            {
                return new ContinueResult(ContinueStatus.SceneLoadFailed, load);
            }

            if (RequiresWorldRestore(load.State.TargetSceneName))
            {
                var world = worldResolver.Resolve();
                if (world == null)
                {
                    return new ContinueResult(ContinueStatus.WorldProviderMissing, load);
                }

                try
                {
                    world.RestoreSnapshot(load.State.World);
                }
                catch
                {
                    return new ContinueResult(ContinueStatus.WorldRestoreFailed, load);
                }

                if (!recalculator.Recalculate())
                {
                    return new ContinueResult(ContinueStatus.RecalculationFailed, load);
                }
            }

            uiGate.SetReady(true);
            return new ContinueResult(ContinueStatus.Success, load);
        }

        internal static bool RequiresWorldRestore(string sceneName)
        {
            return sceneName != SceneNames.SurfaceBase;
        }
    }

    public static class DroneSaveRestorer
    {
        public static bool TryRestore(
            DroneSaveData data,
            TemplateDialogueGenerator generator)
        {
            if (data?.dialogueCooldowns == null || generator == null)
            {
                return false;
            }

            var entries = new List<DroneDialogueCooldownState>(
                data.dialogueCooldowns.Count);
            for (var i = 0; i < data.dialogueCooldowns.Count; i++)
            {
                entries.Add(new DroneDialogueCooldownState(
                    data.dialogueCooldowns[i].templateId,
                    data.dialogueCooldowns[i].lastShownAt));
            }

            return generator.TryRestoreCooldowns(entries);
        }
    }
}
