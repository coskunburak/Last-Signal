using System;
using System.Collections.Generic;
namespace LastSignal.Objectives
{
    public sealed class RelayNode
    {
        public readonly string Id, Prerequisite, Requirement, Early, Retry, Journal;
        public RelayNode(string id,string prerequisite,string requirement,string early,string retry,string journal)
        { Id=id;Prerequisite=prerequisite;Requirement=requirement;Early=early;Retry=retry;Journal=journal; }
    }
    public static class RelayGraph
    {
        public static RelayNode[] Definitions() => new[] {
            new RelayNode("relay.radio",null,"Committed radio discovery","Available from session start","Reread idempotently","Read the cabin radio maintenance note."),
            new RelayNode("relay.fuse","relay.radio","Ever acquired canonical fuse","Record acquisition before radio; reconcile on activation","Recall existing world drop or withdraw storage","Obtain the gas-station spare; keep it for repair."),
            new RelayNode("relay.tools","relay.fuse","Ever acquired tool; currently held at repair","Record tool acquisition before objective","Retrieve a compatible wrench","Bring a wrench."),
            new RelayNode("relay.repair","relay.tools","Radio known; currently held fuse and tool; valid actor/range/threat","Cannot consume before radio; cleared POI is valid","Cancel has no mutation; new token required","Repair the overlook relay."),
            new RelayNode("relay.listen","relay.repair","Committed cabin radio access after repair","Locked until repair","Reread does not grant reward","Return to the cabin radio for Contact intel.") };
        public static bool Validate(RelayNode[] nodes)
        {
            if(nodes==null || nodes.Length!=5) return false;
            var byId=new Dictionary<string,RelayNode>(StringComparer.Ordinal);
            foreach(var node in nodes)
                if(node==null || string.IsNullOrWhiteSpace(node.Id) || string.IsNullOrWhiteSpace(node.Requirement) || string.IsNullOrWhiteSpace(node.Early) || string.IsNullOrWhiteSpace(node.Retry) || string.IsNullOrWhiteSpace(node.Journal) || !byId.TryAdd(node.Id,node)) return false;
            foreach(var node in nodes)
            {
                var seen=new HashSet<string>(); var current=node;
                while(current!=null)
                {
                    if(!seen.Add(current.Id)) return false;
                    if(string.IsNullOrEmpty(current.Prerequisite)) break;
                    if(!byId.TryGetValue(current.Prerequisite,out current)) return false;
                }
            }
            foreach(var id in RelayProgression.NodeIds) if(!byId.ContainsKey(id)) return false;
            return true;
        }
    }
}
