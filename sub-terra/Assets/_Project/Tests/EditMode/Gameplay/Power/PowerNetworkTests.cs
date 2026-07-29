using NUnit.Framework;
using UnityEngine;

namespace SubTerra.Gameplay.Power.Tests
{
    public sealed class PowerNetworkTests
    {
        [Test]
        public void Rebuild_PowersHigherPriorityFacilityBeforeLowerPriorityFacility()
        {
            GameObject root = new("PowerTest");
            PowerNetworkSystem network = root.AddComponent<PowerNetworkSystem>();
            PowerNode core = CreateNode(root, network, true, 5, 0, PowerPriority.Critical);
            PowerNode light = CreateNode(root, network, false, 0, 2, PowerPriority.High);
            PowerNode charger = CreateNode(root, network, false, 0, 4, PowerPriority.Low);
            Connect(root, network, core, light);
            Connect(root, network, light, charger);

            network.Rebuild();

            Assert.That(light.IsPowered, Is.True);
            Assert.That(charger.IsPowered, Is.False);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Rebuild_DisablesFacilityWithoutCablePathToCore()
        {
            GameObject root = new("PowerTest");
            PowerNetworkSystem network = root.AddComponent<PowerNetworkSystem>();
            PowerNode core = CreateNode(root, network, true, 10, 0, PowerPriority.Critical);
            PowerNode facility = CreateNode(root, network, false, 0, 1, PowerPriority.Normal);

            network.Rebuild();

            Assert.That(core.IsPowered, Is.True);
            Assert.That(facility.IsPowered, Is.False);
            Object.DestroyImmediate(root);
        }

        private static PowerNode CreateNode(GameObject root, PowerNetworkSystem network, bool source, int supply, int demand, PowerPriority priority)
        {
            PowerNode node = new GameObject("Node").AddComponent<PowerNode>();
            node.transform.SetParent(root.transform); node.Configure(network, source, supply, demand, priority); network.RegisterNode(node);
            return node;
        }

        private static void Connect(GameObject root, PowerNetworkSystem network, PowerNode first, PowerNode second)
        {
            PowerCable cable = new GameObject("Cable").AddComponent<PowerCable>();
            cable.transform.SetParent(root.transform); cable.Configure(network, first, second); network.RegisterCable(cable);
        }
    }
}
