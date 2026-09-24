using System;
using LastSignal.WorldCells;
using LastSignal.WorldTime;
using NUnit.Framework;
using UnityEngine;
namespace LastSignal.Tests
{
    public class WorldCellTests
    {
        [TestCase(0,0,0,0)] [TestCase(64,64,1,1)] [TestCase(-64,-64,-1,-1)]
        [TestCase(-.001f,.001f,-1,0)] [TestCase(63.999f,64.001f,0,1)] [TestCase(-64.001f,-63.999f,-2,-1)]
        public void CanonicalHalfOpenCoordinates(float x,float z,int a,int b)
        { Assert.AreEqual(new CellCoordinate(a,b),CellCoordinate.FromWorld(new Vector3(x,0,z))); }
        [Test] public void IdentityAndSerializationAreStable()
        {
            var value=new CellCoordinate(-12,7);Assert.AreEqual("cell:-12:7",value.Id);
            Assert.AreEqual(value,JsonUtility.FromJson<CellCoordinate>(JsonUtility.ToJson(value)));
            Assert.AreNotEqual(value.Id,new CellCoordinate(12,7).Id);
            Assert.Throws<ArgumentOutOfRangeException>(()=>CellCoordinate.FromWorld(new Vector3(float.NaN,0,0)));
        }
        [TestCase(false,true,true,true,true,true)] [TestCase(true,false,true,true,true,true)]
        [TestCase(true,true,false,true,true,true)] [TestCase(true,true,true,false,true,true)]
        [TestCase(true,true,true,true,false,true)] [TestCase(true,true,true,true,true,false)]
        public void EveryReadinessPrerequisiteIsRequired(bool a,bool b,bool c,bool d,bool e,bool f)
        {
            var life=new CellLifecycle();var token=life.Request(1);life.Move(token,CellState.Loading);life.Move(token,CellState.Restoring);
            Assert.IsFalse(life.Complete(token,a,b,c,d,e,f));Assert.AreEqual(CellState.Restoring,life.State);
            Assert.IsTrue(life.Complete(token,true,true,true,true,true,true));Assert.AreEqual(CellState.Ready,life.State);
        }
        [Test] public void OldSessionAndOperationCannotAdvanceRestoreReadyOrUnload()
        {
            var life=new CellLifecycle();var old=life.Request(5);life.Move(old,CellState.Loading);
            life.Invalidate(6);var current=life.Request(6);
            Assert.IsFalse(life.Move(old,CellState.Restoring));Assert.IsFalse(life.Complete(old,true,true,true,true,true,true));
            life.Move(current,CellState.Loading);life.Move(current,CellState.Restoring);life.Complete(current,true,true,true,true,true,true);
            Assert.IsFalse(life.Move(old,CellState.Unloading));Assert.AreEqual(CellState.Ready,life.State);
        }
        [Test] public void InvalidTransitionFailsAndFailureRequiresNewGeneration()
        {
            var life=new CellLifecycle();var token=life.Request(1);
            Assert.Throws<InvalidOperationException>(()=>life.Move(token,CellState.Unloading));
            life.Fail(token,"missing ground");Assert.AreEqual(CellState.Failed,life.State);Assert.AreEqual("missing ground",life.Failure);
            var next=life.Request(1);Assert.IsFalse(life.Current(token));Assert.IsTrue(life.Current(next));
        }
        [Test] public void CellElapsedCheckpointDoesNotDoubleTick()
        {
            var state=new ElapsedWorldState {lastProcessed=100};Assert.AreEqual(40,state.TakeElapsed(140));
            var cell=new CellSnapshot {id="cell:1:0",lastProcessed=state.lastProcessed};
            var restored=JsonUtility.FromJson<CellSnapshot>(JsonUtility.ToJson(cell));
            var rebound=new ElapsedWorldState {lastProcessed=restored.lastProcessed};Assert.AreEqual(0,rebound.TakeElapsed(140));Assert.AreEqual(10,rebound.TakeElapsed(150));
        }
    }
}
