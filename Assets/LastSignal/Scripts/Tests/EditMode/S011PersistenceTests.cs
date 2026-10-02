using System;
using System.Collections.Generic;
using System.IO;
using LastSignal.Objectives;
using LastSignal.Persistence;
using NUnit.Framework;
namespace LastSignal.Tests
{
    public sealed class S011PersistenceTests
    {
        static SaveCodec Codec() => new SaveCodec(new SaveValidation("world.fixture","content.1",new Dictionary<string,int> { ["ammo.rifle"]=60,["medical.bandage"]=10,[RelayProgression.FuseId]=1 },"rifle.1",30));
        static SaveGame Fixture(bool completed,long generation=1)
        {
            var save=SaveFoundationTests.Fixture(generation); save.header.progressionVersion=1; save.progression=new RelayProgression().Capture(); save.progression.radio=true; save.progression.acquired=true;
            if(completed)
            {
                save.progression.tools=true; save.progression.repairReceipt=RelayProgression.RepairReceipt; save.progression.rewardReceipt=RelayProgression.RewardReceipt;
                save.progression.phase=save.progression.rewardCount=save.progression.completionCount=1;
            }
            else save.inventory.slots[1]=new SlotSnapshot { definitionId=RelayProgression.FuseId,quantity=1 };
            return save;
        }
        [TestCase(false)] [TestCase(true)] public void ActiveAndCompletedSaveCodecRoundtrip(bool complete)
        {
            var codec=Codec(); Assert.IsTrue(codec.Encode(Fixture(complete),out var json).Success);
            for(int i=0;i<50;i++) { Assert.IsTrue(codec.Decode(json,out var restored).Success); Assert.AreEqual(complete?1:0,restored.progression.rewardCount); Assert.IsTrue(codec.Encode(restored,out json).Success); }
        }
        [Test] public void ContradictoryFuseOwnerAndReceiptRejected()
        {
            var state=Fixture(true); state.inventory.slots[1]=new SlotSnapshot { definitionId=RelayProgression.FuseId,quantity=1 };
            Assert.IsFalse(Codec().Encode(state,out _).Success); state=Fixture(false); state.shelter.storage.slots[1]=state.inventory.slots[1]; Assert.IsFalse(Codec().Encode(state,out _).Success);
            state=Fixture(false); state.inventory.slots[1]=new SlotSnapshot(); Assert.IsFalse(Codec().Encode(state,out _).Success);
        }
        [Test] public void MissingOrEmptyNewProgressionCannotMasqueradeAsLegacy()
        {
            var state=Fixture(false); state.progression=null; Assert.IsFalse(Codec().Encode(state,out _).Success);
            state.progression=new RelaySnapshot(); Assert.IsFalse(Codec().Encode(state,out _).Success);
            state.header.progressionVersion=0; state.progression=new RelaySnapshot { rewardCount=1 }; Assert.IsFalse(Codec().Encode(state,out _).Success);
        }
        [Test] public void LegacySaveHasNoFabricatedCompletion()
        {
            var codec=Codec(); Assert.IsTrue(codec.Encode(SaveFoundationTests.Fixture(),out var json).Success); Assert.IsTrue(codec.Decode(json,out var state).Success); Assert.IsNull(state.progression);
        }
        [TestCase("beforeWrite")] [TestCase("partialWrite")] [TestCase("beforePublish")] [TestCase("afterPublish")]
        public void RewardPublicationFaultRetainsOneCoherentGeneration(string fault)
        {
            string directory=Path.Combine(Path.GetTempPath(),"S011-fault-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); string path=Path.Combine(directory,"save.json");
            try
            {
                var files=new FaultFiles(); var store=new SaveFileStore(path,Codec(),files); Assert.IsTrue(store.Write(Fixture(false),0).Success);
                files.fault=fault; Assert.IsFalse(store.Write(Fixture(true,2),0).Success); files.fault=null;
                Assert.IsTrue(store.Read(0,out var restored).Success); bool published=fault=="afterPublish";
                Assert.AreEqual(published?1:0,restored.progression.rewardCount); if(published) Assert.IsTrue(string.IsNullOrEmpty(restored.inventory.slots[1].definitionId)); else Assert.AreEqual(RelayProgression.FuseId,restored.inventory.slots[1].definitionId);
                if(published) { Assert.IsTrue(store.Read(0,out var backup,true).Success); Assert.AreEqual(0,backup.progression.rewardCount); }
            }
            finally { Directory.Delete(directory,true); }
        }
        sealed class FaultFiles : ISaveFiles
        {
            readonly PhysicalSaveFiles real=new PhysicalSaveFiles(); public string fault;
            public bool Exists(string p)=>real.Exists(p); public string Read(string p)=>real.Read(p); public void DeleteCandidate(string p)=>real.DeleteCandidate(p);
            public void WriteCandidate(string p,string data)
            {
                if(fault=="beforeWrite") throw new IOException("Injected before candidate");
                if(fault=="partialWrite") { real.WriteCandidate(p,data.Substring(0,data.Length/2)); throw new IOException("Injected partial write"); }
                real.WriteCandidate(p,data);
            }
            public void Publish(string a,string b,string c)
            { if(fault=="beforePublish") throw new IOException("Injected before atomic publication"); real.Publish(a,b,c); if(fault=="afterPublish") throw new IOException("Injected after atomic publication"); }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] public void InvalidGraphRejectsDuplicateMissingAndCycle(int variant)
        {
            var nodes=RelayGraph.Definitions(); Assert.IsTrue(RelayGraph.Validate(nodes));
            var n=nodes[0]; nodes[0]=new RelayNode(variant==0?nodes[1].Id:n.Id,variant==1?"missing":variant==2?nodes[4].Id:null,n.Requirement,n.Early,n.Retry,n.Journal);
            Assert.IsFalse(RelayGraph.Validate(nodes));
        }
    }
}
