#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SubTerra.App.Save;
using SubTerra.Shared;
using System.Reflection;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Integration
{
    [InitializeOnLoad]
    public static class MiningSeparationEntry
    {
        const string Key = "SubTerra.MiningSeparation";
        static MiningSeparationEntry()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Key, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    var host = new GameObject("Isolated mining separation probe");
                    UnityEngine.Object.DontDestroyOnLoad(host);
                    host.AddComponent<MiningSeparationProbe>().Begin();
                }
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Key, false);
                    EditorApplication.delayCall += () => EditorApplication.Exit(SessionState.GetInt(Key + ".Exit", 1));
                }
            };
        }
        public static void Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-subterra-save-root") < 0)
                throw new InvalidOperationException("Isolated QA save root required.");
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Bootstrap/Bootstrap.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
        public static void Finish(bool ok)
        {
            SessionState.SetInt(Key + ".Exit", ok ? 0 : 1);
            EditorApplication.isPlaying = false;
        }
    }

    public sealed class MiningSeparationProbe : MonoBehaviour
    {
        static readonly ProfilerMarker Preparation = new("QA.PrepareMiningTarget");
        readonly List<string> report = new();
        public void Begin() => StartCoroutine(Guard(Scenario()));
        IEnumerator Guard(IEnumerator scenario)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(scenario); bool ok = true;
            while (stack.Count > 0)
            {
                bool moved; object current = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception error) { report.Add("FAIL " + error); ok = false; break; }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) stack.Push(nested); else yield return current;
            }
            File.WriteAllLines(Path.GetFullPath(Path.Combine(Application.dataPath, "../../work_process/mining-separation-measured-20261012.txt")), report);
            MiningSeparationEntry.Finish(ok);
        }
        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
        static IEnumerator WaitFor(Func<bool> condition)
        {
            float deadline = Time.realtimeSinceStartup + 45;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Require(condition(), "Runtime readiness timeout");
        }
        IEnumerator Scenario()
        {
            yield return WaitFor(() => SaveRuntimeController.Instance != null);
            var runtime = SaveRuntimeController.Instance;
            Require(runtime.StartNewGame(3, true), "Isolated new game");
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "SurfaceBase" && runtime.IsUiReady);
            Require(runtime.TryStartExploration(out var reason), "Exploration: " + reason);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Mine_Demo_Integration" && runtime.IsUiReady);
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            var mine = UnityEngine.Object.FindAnyObjectByType<MiningSystem>();
            Require(player != null && mine != null, "Real gameplay systems");
            player.SetCanMove(false);
            var body = player.GetComponent<Rigidbody2D>(); body.simulated = false;
            var controller = player.GetComponent<PlayerMiningController>(); if (controller != null) controller.enabled = false;
            var placement = UnityEngine.Object.FindAnyObjectByType<BuildingPlacementSystem>();
            runtime.InventoryService.AddMineral("mineral.copper", 30);
            var definition = AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>("Assets/_Project/Data/Buildings/Placement/charger_basicPlacement.asset");
            var terrain = mine.ForegroundTilemap;
            int installed = 0;
            for (int y = terrain.cellBounds.yMax; y >= terrain.cellBounds.yMin && installed < 6; y--)
            for (int x = terrain.cellBounds.xMin; x < terrain.cellBounds.xMax - 1 && installed < 6; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (!terrain.HasTile(cell + Vector3Int.down) || !terrain.HasTile(cell + Vector3Int.down + Vector3Int.right)) continue;
                player.transform.position = terrain.GetCellCenterWorld(cell); body.position = player.transform.position;
                Physics2D.SyncTransforms(); placement.Select(definition);
                if (placement.CanPlaceAt(cell, out _) && placement.TryPlaceAt(cell).IsSuccess) installed++;
            }
            placement.ClearSelection(); Require(installed == 6, "Six actual chargers required");
            var targets = new List<Vector3Int>();
            for (int y = -1; y >= -14; y--) for (int x = -18; x <= 18; x++) targets.Add(new Vector3Int(x, y, 0));
            Vector3Int[] offsets = { Vector3Int.up, Vector3Int.left, Vector3Int.right, Vector3Int.down };
            int mined = 0; mine.TileMined += (_, __) => mined++;
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            yield return new WaitForSecondsRealtime(2);
            var markerNames = new List<string> { "QA.PrepareMiningTarget", "SubTerra.Mining.Tick", "SubTerra.Mining.Complete",
                "SubTerra.Mining.Transaction", "SubTerra.Mining.RemoveTile", "SubTerra.Mining.Subscribers" };
            // Temporary diagnostic wrappers preserve callback order and are removed after measurement.
            // No reflection or delegate-list allocation is added to product code.
            var field = typeof(MiningSystem).GetField("TileMined", BindingFlags.Instance | BindingFlags.NonPublic);
            var original = (Action<Vector3Int, MiningTileDto>)field.GetValue(mine);
            Action<Vector3Int, MiningTileDto> wrapped = null;
            if (original != null)
            {
                int index = 0;
                foreach (var subscriber in original.GetInvocationList())
                {
                    var callback = (Action<Vector3Int, MiningTileDto>)subscriber;
                    string name = "QA.Subscriber." + index++ + "." + callback.Method.DeclaringType.Name + "." + callback.Method.Name;
                    var marker = new ProfilerMarker(name); markerNames.Add(name);
                    wrapped += (cell, tile) => { using (marker.Auto()) callback(cell, tile); };
                }
                field.SetValue(mine, wrapped);
            }
            var names = markerNames.ToArray();
            const int maximumFrameSamples = 32768;
            var sampleFrames = new int[maximumFrameSamples];
            var sampleIntervals = new double[maximumFrameSamples];
            var sampleMined = new int[maximumFrameSamples];
            var sampleCosts = new long[maximumFrameSamples * names.Length];
            int recordedFrames = 0, droppedFrames = 0;
            var recorders = new ProfilerRecorder[names.Length];
            var sum = new double[names.Length]; var maximum = new long[names.Length]; var count = new int[names.Length];
            for (int i = 0; i < names.Length; i++) recorders[i] = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, names[i], 1);
            var frameWatch = System.Diagnostics.Stopwatch.StartNew();
            double previousMs = frameWatch.Elapsed.TotalMilliseconds;
            try
            {
                float end = Time.realtimeSinceStartup + 30;
                while (Time.realtimeSinceStartup < end)
                {
                    int sampledFrame = Time.frameCount;
                    if (!mine.IsMining)
                    {
                        using (Preparation.Auto())
                        {
                            bool begun = false;
                            foreach (var target in targets)
                            {
                                if (!terrain.HasTile(target)) continue;
                                foreach (var offset in offsets)
                                {
                                    var neighbor = target + offset; if (terrain.HasTile(neighbor)) continue;
                                    player.transform.position = terrain.GetCellCenterWorld(neighbor); body.position = player.transform.position;
                                    Physics2D.SyncTransforms();
                                    if (mine.TryStartMiningAtWorldPoint(terrain.GetCellCenterWorld(target), player.transform.position, 1.35f)) { begun = true; break; }
                                }
                                if (begun) break;
                            }
                        }
                    }
                    if (mine.IsMining) mine.TickMining(Time.deltaTime, player.transform.position, 1.35f);
                    yield return null;
                    double nowMs = frameWatch.Elapsed.TotalMilliseconds;
                    bool storeFrame = recordedFrames < maximumFrameSamples;
                    if (storeFrame)
                    {
                        sampleFrames[recordedFrames] = sampledFrame;
                        sampleIntervals[recordedFrames] = nowMs - previousMs;
                        sampleMined[recordedFrames] = mined;
                    }
                    else droppedFrames++;
                    previousMs = nowMs;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (!recorders[i].Valid)
                        {
                            if (storeFrame) sampleCosts[recordedFrames * names.Length + i] = long.MinValue;
                            continue;
                        }
                        long value = recorders[i].Count == 0 ? 0 : recorders[i].LastValue;
                        if (storeFrame) sampleCosts[recordedFrames * names.Length + i] = value;
                        if (value > 0) { sum[i] += value; count[i]++; maximum[i] = Math.Max(maximum[i], value); }
                        // Clear old samples so an inactive marker cannot reuse a previous frame's cost.
                        recorders[i].Reset();
                    }
                    if (storeFrame) recordedFrames++;
                }
                // Format strings and write files only after the measured interval.
                var frameRows = new List<string>(recordedFrames + 1) { "sampled_frame,interval_ms,total_mined," + string.Join(",", names) };
                for (int frame = 0; frame < recordedFrames; frame++)
                {
                    var row = new List<string> { sampleFrames[frame].ToString(CultureInfo.InvariantCulture),
                        sampleIntervals[frame].ToString("F4", CultureInfo.InvariantCulture), sampleMined[frame].ToString(CultureInfo.InvariantCulture) };
                    for (int i = 0; i < names.Length; i++)
                    {
                        long value = sampleCosts[frame * names.Length + i];
                        row.Add(value == long.MinValue ? "NA" : value.ToString(CultureInfo.InvariantCulture));
                    }
                    frameRows.Add(string.Join(",", row));
                }
                File.WriteAllLines(Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../../work_process/mining-separation-frames-20261012.csv")), frameRows);
                report.Add("Editor Play CPU marker separation, 30 seconds; chargers=" + installed + "; mined=" + mined + "; not Windows/GPU FPS");
                report.Add("Per-frame marker values are nanoseconds; rows=" + recordedFrames + "; dropped rows=" + droppedFrames + "; formatting excluded from measured interval.");
                for (int i = 0; i < names.Length; i++) report.Add(names[i] + "; valid=" + recorders[i].Valid
                    + "; unit=" + recorders[i].UnitType + "; active frame samples=" + count[i]
                    + "; mean_ms=" + (count[i] == 0 ? 0 : sum[i] / count[i] / 1000000).ToString("F4", CultureInfo.InvariantCulture)
                    + "; max_ms=" + (maximum[i] / 1000000.0).ToString("F4", CultureInfo.InvariantCulture));
                Require(mined > 0, "Actual successful mining transactions required");
            }
            finally
            {
                foreach (var recorder in recorders) recorder.Dispose();
                if (original != null) field.SetValue(mine, original);
                mine.CancelMining();
            }
        }
    }
}
#endif
