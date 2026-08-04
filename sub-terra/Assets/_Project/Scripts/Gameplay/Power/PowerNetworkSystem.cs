using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SubTerra.Gameplay.Power
{
    /// <summary>Rebuilds an explicit cable graph on topology or power changes, never every frame.</summary>
    public sealed class PowerNetworkSystem : MonoBehaviour
    {
        private readonly HashSet<PowerNode> nodes = new();
        private readonly HashSet<PowerCable> cables = new();
        private HashSet<PowerNode> reachableNodes = new();

        public PowerNetworkSnapshot CurrentSnapshot { get; private set; }
        public IReadOnlyCollection<PowerNode> Nodes => nodes;
        public event Action<PowerNetworkSnapshot> NetworkRebuilt;

        public void RegisterNode(PowerNode node)
        {
            if (node != null && nodes.Add(node)) Rebuild();
        }

        public void UnregisterNode(PowerNode node)
        {
            if (node != null && nodes.Remove(node)) Rebuild();
        }

        public void RegisterCable(PowerCable cable)
        {
            if (cable != null && cables.Add(cable)) Rebuild();
        }

        public void UnregisterCable(PowerCable cable)
        {
            if (cable != null && cables.Remove(cable)) Rebuild();
        }

        public void RequestRebuild() => Rebuild();

        public void Rebuild()
        {
            nodes.RemoveWhere(node => node == null);
            cables.RemoveWhere(cable => cable == null || !cable.IsValid);
            reachableNodes = FindReachableNodes();
            int supply = reachableNodes.Where(node => node.IsPowerSource).Sum(node => node.Supply);
            int demand = reachableNodes.Where(node => !node.IsPowerSource).Sum(node => node.Demand);
            int remaining = supply;
            int activeFacilities = 0;

            foreach (PowerNode source in reachableNodes.Where(node => node.IsPowerSource)) source.SetPowered(true);
            foreach (PowerNode facility in reachableNodes.Where(node => !node.IsPowerSource).OrderBy(node => node.Priority).ThenBy(node => node.EntityId).ThenBy(node => node.GetEntityId().ToString()))
            {
                bool powered = facility.Demand <= remaining;
                facility.SetPowered(powered);
                if (!powered) continue;
                remaining -= facility.Demand;
                activeFacilities++;
            }

            foreach (PowerNode disconnected in nodes.Where(node => !reachableNodes.Contains(node))) disconnected.SetPowered(false);
            CurrentSnapshot = new PowerNetworkSnapshot(supply, demand, activeFacilities);
            NetworkRebuilt?.Invoke(CurrentSnapshot);
        }

        public bool IsReachable(PowerNode node)
        {
            return node != null && reachableNodes.Contains(node);
        }

        private HashSet<PowerNode> FindReachableNodes()
        {
            var reachable = new HashSet<PowerNode>();
            var pending = new Queue<PowerNode>();
            foreach (PowerNode source in nodes.Where(node => node.IsPowerSource))
            {
                reachable.Add(source);
                pending.Enqueue(source);
            }

            while (pending.Count > 0)
            {
                PowerNode current = pending.Dequeue();
                foreach (PowerCable cable in cables)
                {
                    if (!cable.Connects(current)) continue;
                    PowerNode next = cable.GetOther(current);
                    if (next != null && reachable.Add(next)) pending.Enqueue(next);
                }
            }
            return reachable;
        }
    }
}
