using LastSignal.Inventory;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class PistolAnatomyTests
    {
        const string Prefab = "Assets/LastSignal/Prefabs/Combat/S021/Weapon_TacticalPistol.prefab";
        [Test] public void PistolHasDistinctValidFpsAndDieselCalibration()
        {
            var g=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponent<FirstPersonWeaponGrip>();
            Assert.That(g.Profile.FpsPistolFingers && g.Profile.FpsPistolFingers.Valid,Is.True);
            Assert.That(g.Profile.WorldPistolFingers && g.Profile.WorldPistolFingers.Valid,Is.True);
            Assert.That(g.Profile.FpsPistolFingers,Is.Not.SameAs(g.Profile.WorldPistolFingers));
            Assert.That(g.Sockets.PrimaryGrip.localPosition.z,Is.LessThan(-.04f),"Palm must sit on the handle, behind the trigger guard.");
            Assert.That(g.Sockets.StockContact,Is.Null);Assert.That(g.Sockets.SecondaryGrip,Is.Null);
        }
        [Test] public void FingersWrapFrontAndOppositeSideWithoutReenteringHandleCore()
        {
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try {
                var g=root.GetComponent<FirstPersonWeaponGrip>();var hand=new CharacterHandPose(g.RightHand,g.RightFingers,false,.012f,false);
                var before=new Quaternion[15];for(int i=0;i<15;i++)before[i]=g.RightFingers[i].localRotation;
                CharacterWeaponSockets.Attach(g.Visual,g.Sockets.PrimaryGrip,hand.PalmPosition,hand.PalmRotation);
                hand.Apply(1,true,0,1,g.Profile,g.Profile.FpsPistolFingers);
                var model=g.Sockets.PrimaryGrip.parent;
                for(int i=6;i<15;i++) {
                    var bone=g.RightFingers[i];var a=model.InverseTransformPoint(bone.position);
                    var b=model.InverseTransformPoint(bone.GetChild(0).position);
                    for(int s=1;s<=10;s++)Assert.That(InsideHandleCore(Vector3.Lerp(a,b,s/10f)),Is.False,bone.name+" crosses the solid handle core");
                    if(i%3==2)Assert.That(b.x,Is.InRange(.023f,.045f),bone.name+" must wrap the far side, not curl back through the grip");
                }
                // Posing again must preserve the original restoration snapshot, including trigger motion.
                hand.Apply(1,true,1,1,g.Profile,g.Profile.FpsPistolFingers); hand.Restore();
                for(int i=0;i<15;i++)Assert.That(Quaternion.Angle(before[i],g.RightFingers[i].localRotation),Is.LessThan(.01f));
            } finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        public static bool InsideHandleCore(Vector3 p) => p.x>.044f && p.x<.056f && p.y>.008f && p.y<.065f && p.z>-.078f && p.z<-.038f;
        [TestCase(false)][TestCase(true)] public void ReloadInsertsAtTransactionAndReleasesOnCancel(bool empty)
        {
            var owner=new GameObject("Pistol reload fixture");
            try {
                var inventory=owner.AddComponent<PlayerInventory>();inventory.Initialize(24);
                var definition=AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/LastSignal/Data/Combat/S021/Weapon_TacticalPistol.asset");
                inventory.TryAdd(definition.Ammunition,12);
                var state=new WeaponRuntimeState(definition,empty?0:2,inventory);
                state.TryEquip();state.Tick(definition.EquipSeconds+.01f);state.TryCompleteEquip();
                Assert.That(state.TryBeginReload(),Is.True);
                float duration=empty?definition.EmptyReloadSeconds:definition.TacticalReloadSeconds;
                float commit=empty?definition.EmptyReloadCommitNormalized:definition.ReloadCommitNormalized;
                state.Tick(duration*commit*.6f);PistolReloadPose.Sample(state,out var assist,out var extraction);
                Assert.That(assist,Is.EqualTo(1).Within(.001f));Assert.That(extraction,Is.EqualTo(1).Within(.001f));
                state.Tick(duration*commit*.4f+.001f);Assert.That(state.TryCommitReload(),Is.True);
                PistolReloadPose.Sample(state,out assist,out extraction);Assert.That(extraction,Is.Zero);
                state.ForceHolster();PistolReloadPose.Sample(state,out assist,out extraction);
                Assert.That(assist,Is.Zero);Assert.That(extraction,Is.Zero);
            } finally { Object.DestroyImmediate(owner); }
        }
    }
}
