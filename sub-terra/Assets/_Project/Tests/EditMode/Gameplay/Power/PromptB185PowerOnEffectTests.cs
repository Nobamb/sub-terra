using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Snapshot;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SubTerra.Gameplay.Power.Tests
{
    /// <summary>B-185(#292): 시설 전력 켜짐 청록 정전기·빛 연출. 소리는 없고 이벤트만 연다.</summary>
    public sealed class PromptB185PowerOnEffectTests
    {
        private readonly List<Object> created = new();
        private readonly List<(PowerFacility facility, bool isBatch)> started = new();
        private readonly List<(int count, Vector3 center)> batches = new();
        private bool previousReduceMotion;

        [SetUp]
        public void SetUp()
        {
            previousReduceMotion = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            PowerOnEffect.Started += OnStarted;
            PowerOnEffect.BatchStarted += OnBatchStarted;
        }

        [TearDown]
        public void TearDown()
        {
            PowerOnEffect.Started -= OnStarted;
            PowerOnEffect.BatchStarted -= OnBatchStarted;
            AccessibilityPreferences.ReduceMotion = previousReduceMotion;
            for (int i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }
            created.Clear();
            while (PowerOnEffect.IsSuppressed) PowerOnEffect.EndSuppression();
        }

        [Test]
        public void OffToOn_PlaysEffectAndRaisesStartedOnce()
        {
            var f = CreateWorld(1, 10);
            Connect(f.core, f.facilities[0]);

            PowerFacility facility = f.facilities[0];
            Assert.That(facility.IsInteractionAvailable, Is.True);
            Assert.That(facility.PowerOnEffectInstance, Is.Not.Null);
            Assert.That(facility.PowerOnEffectInstance.IsPlaying, Is.True);
            Assert.That(facility.PowerOnEffectInstance.EffectRoot.activeSelf, Is.True);
            Assert.That(started, Has.Count.EqualTo(1));
            Assert.That(started[0].facility, Is.SameAs(facility));
            Assert.That(started[0].isBatch, Is.False);
            Assert.That(batches, Has.Count.EqualTo(1));
            Assert.That(batches[0].count, Is.EqualTo(1));
        }

        [Test]
        public void OnToOn_AndOnToOff_RaiseNothing()
        {
            var f = CreateWorld(1, 10);
            Connect(f.core, f.facilities[0]);
            PowerOnEffect effect = f.facilities[0].facility.PowerOnEffectInstance;
            effect.Tick(PowerOnEffect.PowerOnDuration + 0.01f);
            started.Clear();
            batches.Clear();

            f.network.Rebuild();
            Assert.That(started, Is.Empty, "켜짐→켜짐은 연출 없음");
            Assert.That(effect.IsPlaying, Is.False);

            f.core.Configure(f.network, true, 0, 0, PowerPriority.Critical);
            Assert.That(f.facilities[0].facility.IsInteractionAvailable, Is.False);
            Assert.That(started, Is.Empty, "켜짐→꺼짐은 연출 없음");
            Assert.That(batches, Is.Empty);
            Assert.That(effect.IsPlaying, Is.False);
            Assert.That(f.visuals[0].activeSelf, Is.False);
        }

        [Test]
        public void FirstApplyOnEnable_IsQuiet()
        {
            var f = CreateWorld(1, 10, enableFacilities: false);
            Connect(f.core, f.facilities[0]);
            Assert.That(f.facilities[0].node.IsPowered, Is.True);

            CallLifecycle(f.facilities[0].facility, "Awake");
            CallLifecycle(f.facilities[0].facility, "OnEnable");

            Assert.That(f.facilities[0].facility.IsInteractionAvailable, Is.True);
            Assert.That(f.visuals[0].activeSelf, Is.True, "첫 적용은 지금처럼 바로 켬");
            Assert.That(f.facilities[0].facility.PowerOnEffectInstance, Is.Null);
            Assert.That(started, Is.Empty);
            Assert.That(batches, Is.Empty);
        }

        [Test]
        public void RestoreSnapshot_PowersOnQuietly()
        {
            var f = CreateWorld(1, 10);
            f.core.SetEntityId("core-0001");
            f.facilities[0].node.SetEntityId("facility-0001");
            var prefab = Track(new GameObject("PowerCablePrefab"));
            var prefabCable = prefab.AddComponent<PowerCable>();
            var snapshots = f.root.AddComponent<WorldSnapshotSystem>();
            SetField(snapshots, "powerNetworkSystem", f.network);
            SetField(snapshots, "powerCablePrefab", prefabCable);

            bool restored = snapshots.RestoreSnapshot(new WorldSnapshotDto
            {
                powerState = new PowerSnapshotDto
                {
                    cableConnections = new List<PowerConnectionSnapshotDto>
                    {
                        new() { nodeAInstanceId = "core-0001", nodeBInstanceId = "facility-0001" }
                    }
                }
            });

            Assert.That(restored, Is.True);
            Assert.That(f.facilities[0].facility.IsInteractionAvailable, Is.True);
            Assert.That(f.visuals[0].activeSelf, Is.True);
            PowerOnEffect effect = f.facilities[0].facility.PowerOnEffectInstance;
            Assert.That(effect == null || !effect.IsPlaying, Is.True, "불러오기는 연출 없음");
            Assert.That(started, Is.Empty);
            Assert.That(batches, Is.Empty);
            Assert.That(snapshots.IsRestoring, Is.False);
            Assert.That(PowerOnEffect.IsSuppressed, Is.False, "Restore가 끝나면 생략 구간도 끝난다");
        }

        [Test]
        public void OneRebuildPoweringThree_PlaysThreeEffects_StartedThrice_BatchOnce()
        {
            var f = CreateWorld(3, 0);
            foreach (var facility in f.facilities) Connect(f.core, facility);
            Assert.That(f.facilities.All(entry => !entry.facility.IsInteractionAvailable), Is.True);
            started.Clear();
            batches.Clear();

            f.core.Configure(f.network, true, 10, 0, PowerPriority.Critical);

            Assert.That(f.facilities.All(entry => entry.facility.PowerOnEffectInstance.IsPlaying), Is.True);
            Assert.That(started, Has.Count.EqualTo(3));
            Assert.That(started.All(entry => entry.isBatch), Is.True);
            Assert.That(started.Select(entry => entry.facility).Distinct().Count(), Is.EqualTo(3));
            Assert.That(batches, Has.Count.EqualTo(1));
            Assert.That(batches[0].count, Is.EqualTo(3));
            var effects = f.facilities.Select(entry => entry.facility.PowerOnEffectInstance).ToArray();
            Assert.That(effects.Distinct().Count(), Is.EqualTo(3), "연출은 시설마다 따로");
        }

        [Test]
        public void ReduceMotion_HasNoSparksAndDimGlow_ButKeepsEvent()
        {
            AccessibilityPreferences.ReduceMotion = true;
            var f = CreateWorld(1, 10);
            Connect(f.core, f.facilities[0]);
            PowerOnEffect effect = f.facilities[0].facility.PowerOnEffectInstance;

            Assert.That(effect.IsPlaying, Is.True);
            Assert.That(effect.ActiveSparkCount, Is.Zero);
            Assert.That(effect.PeakGlowAlpha, Is.LessThanOrEqualTo(0.12f));
            Assert.That(f.visuals[0].GetComponent<SpriteRenderer>().color.a, Is.EqualTo(1f), "모션 감소는 켜짐 그림 페이드 없음");
            float maxAlpha = 0f;
            for (int i = 0; i < 40 && effect.IsPlaying; i++)
            {
                effect.Tick(0.01f);
                maxAlpha = Mathf.Max(maxAlpha, effect.CurrentGlowAlpha);
                Assert.That(effect.EffectRoot.GetComponentsInChildren<SpriteRenderer>(false).Count(r => r.enabled && r.name.StartsWith("Spark")), Is.Zero);
            }
            Assert.That(maxAlpha, Is.GreaterThan(0f).And.LessThanOrEqualTo(0.12f + 0.0001f));
            Assert.That(started, Has.Count.EqualTo(1), "모션 감소에서도 빛이 있으면 이벤트 유지");
        }

        [Test]
        public void NormalMotion_SparksAndGlowStayInConfiguredRange()
        {
            var f = CreateWorld(1, 10);
            Connect(f.core, f.facilities[0]);
            PowerOnEffect effect = f.facilities[0].facility.PowerOnEffectInstance;
            SpriteRenderer powered = f.visuals[0].GetComponent<SpriteRenderer>();

            int sparkCount = effect.ActiveSparkCount;
            Assert.That(sparkCount, Is.InRange(PowerOnEffect.SparkCountMin, PowerOnEffect.SparkCountMax));
            Assert.That(powered.color.a, Is.LessThan(0.01f), "켜짐 그림은 처음엔 투명에서 시작");
            float maxAlpha = 0f;
            float peakTime = 0f;
            var sparkSeen = new HashSet<string>();
            for (int i = 0; i < 80 && effect.IsPlaying; i++)
            {
                effect.Tick(0.01f);
                if (effect.CurrentGlowAlpha > maxAlpha)
                {
                    maxAlpha = effect.CurrentGlowAlpha;
                    peakTime = effect.Elapsed;
                }
                foreach (SpriteRenderer spark in effect.EffectRoot.GetComponentsInChildren<SpriteRenderer>(false))
                {
                    if (!spark.enabled || !spark.name.StartsWith("Spark")) continue;
                    sparkSeen.Add(spark.name);
                    Assert.That(effect.Elapsed, Is.LessThanOrEqualTo(PowerOnEffect.SparkWindowSeconds + 0.0001f));
                }
            }
            Assert.That(maxAlpha, Is.LessThanOrEqualTo(PowerOnEffect.GlowMaxAlpha + 0.0001f));
            Assert.That(maxAlpha, Is.GreaterThan(PowerOnEffect.GlowMaxAlpha * 0.9f));
            Assert.That(peakTime, Is.EqualTo(PowerOnEffect.GlowRiseSeconds).Within(0.02f));
            Assert.That(sparkSeen.Count, Is.EqualTo(sparkCount), "예약된 정전기는 모두 한 번씩 보인다");
            Assert.That(powered.color.a, Is.EqualTo(1f), "끝나면 켜짐 그림은 원래 알파");
            Assert.That(effect.IsPlaying, Is.False);
            Assert.That(effect.EffectRoot.activeSelf, Is.False);
        }

        [Test]
        public void PowerLostMidEffect_StopsImmediately_AndPoweredVisualOff()
        {
            var f = CreateWorld(1, 10);
            Connect(f.core, f.facilities[0]);
            PowerOnEffect effect = f.facilities[0].facility.PowerOnEffectInstance;
            SpriteRenderer powered = f.visuals[0].GetComponent<SpriteRenderer>();
            effect.Tick(0.1f);
            Assert.That(effect.IsPlaying, Is.True);

            f.core.Configure(f.network, true, 0, 0, PowerPriority.Critical);

            Assert.That(effect.IsPlaying, Is.False);
            Assert.That(effect.EffectRoot.activeSelf, Is.False);
            Assert.That(f.visuals[0].activeSelf, Is.False);
            Assert.That(powered.color.a, Is.EqualTo(1f), "페이드 중 꺼져도 원래 알파로 돌려 둔다");
            Assert.That(f.facilities[0].facility.IsInteractionAvailable, Is.False);
        }

        [Test]
        public void EffectEndsAfterDuration_AndObjectIsReused()
        {
            var f = CreateWorld(1, 10);
            PowerCable cable = Connect(f.core, f.facilities[0]);
            PowerOnEffect effect = f.facilities[0].facility.PowerOnEffectInstance;
            GameObject root = effect.EffectRoot;

            effect.Tick(PowerOnEffect.PowerOnDuration);
            Assert.That(effect.IsPlaying, Is.False);
            Assert.That(root.activeSelf, Is.False);

            f.network.UnregisterCable(cable);
            f.network.RegisterCable(cable);
            Assert.That(effect.IsPlaying, Is.True);
            Assert.That(f.facilities[0].facility.PowerOnEffectInstance, Is.SameAs(effect));
            Assert.That(effect.EffectRoot, Is.SameAs(root), "시설당 오브젝트 하나를 재사용");
            Assert.That(f.facilities[0].facility.GetComponentsInChildren<Transform>(true)
                .Count(t => t.name == PowerOnEffect.EffectRootName), Is.EqualTo(1));
        }

        [Test]
        public void EffectSortsJustInFrontOfFacility()
        {
            var f = CreateWorld(1, 10);
            Connect(f.core, f.facilities[0]);
            PowerOnEffect effect = f.facilities[0].facility.PowerOnEffectInstance;
            int facilityMax = 6;

            foreach (SpriteRenderer renderer in effect.EffectRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Assert.That(renderer.sortingOrder, Is.InRange(facilityMax + 1, facilityMax + 2), renderer.name);
            }
        }

        [Test]
        public void SfxIdCountAndValues_AreUnchanged()
        {
            int[] values = Enum.GetValues(typeof(SfxId)).Cast<int>().ToArray();
            Assert.That(values.Length, Is.EqualTo(103));
            Assert.That(values.Sum(), Is.EqualTo(5274));
            Assert.That(values.Max(), Is.EqualTo(105));
        }

        private void OnStarted(PowerFacility facility, bool isBatch) => started.Add((facility, isBatch));
        private void OnBatchStarted(int count, Vector3 center) => batches.Add((count, center));

        private sealed class World
        {
            public GameObject root;
            public PowerNetworkSystem network;
            public PowerNode core;
            public readonly List<FacilityEntry> facilities = new();
            public readonly List<GameObject> visuals = new();
        }

        private sealed class FacilityEntry
        {
            public PowerFacility facility;
            public PowerNode node;
            public static implicit operator PowerFacility(FacilityEntry entry) => entry.facility;
        }

        private World CreateWorld(int facilityCount, int coreSupply, bool enableFacilities = true)
        {
            var world = new World { root = Track(new GameObject("B185PowerWorld")) };
            world.network = world.root.AddComponent<PowerNetworkSystem>();
            world.core = new GameObject("Core").AddComponent<PowerNode>();
            world.core.transform.SetParent(world.root.transform);
            world.core.Configure(world.network, true, coreSupply, 0, PowerPriority.Critical);
            world.network.RegisterNode(world.core);

            for (int i = 0; i < facilityCount; i++)
            {
                var facilityObject = new GameObject("Facility" + i);
                facilityObject.transform.SetParent(world.root.transform);
                facilityObject.transform.position = new Vector3(i * 3f, 0f, 0f);

                var visualRoot = new GameObject("VisualRoot");
                visualRoot.transform.SetParent(facilityObject.transform, false);
                var artwork = new GameObject("Artwork").AddComponent<SpriteRenderer>();
                artwork.transform.SetParent(visualRoot.transform, false);
                artwork.sprite = CreateSprite();
                artwork.sortingOrder = 4;
                var port = new GameObject("PowerPortGlow").AddComponent<SpriteRenderer>();
                port.transform.SetParent(facilityObject.transform, false);
                port.sprite = artwork.sprite;
                port.sortingOrder = 6;
                var clearance = new GameObject("GasClearance").AddComponent<SpriteRenderer>();
                clearance.transform.SetParent(facilityObject.transform, false);
                clearance.sprite = artwork.sprite;
                clearance.sortingOrder = 22;
                clearance.color = new Color(1f, 0.12f, 0.08f, 0.05f);

                var poweredVisual = new GameObject("PoweredVisualRoot");
                poweredVisual.transform.SetParent(facilityObject.transform, false);
                poweredVisual.AddComponent<SpriteRenderer>().sprite = artwork.sprite;
                poweredVisual.SetActive(false);

                PowerFacility facility = facilityObject.AddComponent<PowerFacility>();
                PowerNode node = facilityObject.GetComponent<PowerNode>();
                SetField(facility, "powerNode", node);
                SetField(facility, "poweredVisuals", new[] { poweredVisual });
                node.Configure(world.network, false, 0, 1, PowerPriority.Normal);
                node.SetEntityId("facility-" + i);
                world.network.RegisterNode(node);
                if (enableFacilities)
                {
                    CallLifecycle(facility, "Awake");
                    CallLifecycle(facility, "OnEnable");
                }

                world.facilities.Add(new FacilityEntry { facility = facility, node = node });
                world.visuals.Add(poweredVisual);
            }

            started.Clear();
            batches.Clear();
            return world;
        }

        private PowerCable Connect(PowerNode core, FacilityEntry facility)
        {
            PowerCable cable = new GameObject("Cable").AddComponent<PowerCable>();
            cable.transform.SetParent(core.transform.parent);
            cable.Configure(core.Network, core, facility.node);
            core.Network.RegisterCable(cable);
            return cable;
        }

        private Sprite CreateSprite()
        {
            var texture = Track(new Texture2D(16, 16));
            var sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
            Track(sprite);
            return sprite;
        }

        private T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        private static void CallLifecycle(object target, string method)
        {
            MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(info, Is.Not.Null, method);
            info.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
