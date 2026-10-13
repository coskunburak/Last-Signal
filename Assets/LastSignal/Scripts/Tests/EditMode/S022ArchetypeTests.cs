using System;
using System.Collections.Generic;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace LastSignal.Tests
{
    public class S022ArchetypeTests
    {
        static ZombieDefinition Profile(string name) => AssetDatabase.LoadAssetAtPath<ZombieDefinition>(
            "Assets/LastSignal/Data/Enemies/Zombie/" + name + ".asset");
        [TestCase("Shambler", ZombieArchetypes.Shambler, 100)]
        [TestCase("Lurker", ZombieArchetypes.Lurker, 75)]
        [TestCase("Runner", ZombieArchetypes.Runner, 60)]
        public void ProfilesAreCanonicalAndBounded(string name, string id, float hp)
        {
            var d = Profile(name); Assert.That(d, Is.Not.Null);
            Assert.That(d.IsValid(out var reason), Is.True, reason);
            Assert.That(d.IsAttackValid(out reason), Is.True, reason);
            Assert.That(d.ArchetypeId, Is.EqualTo(id)); Assert.That(d.ArchetypeHealth, Is.EqualTo(hp));
            Assert.That(ZombieArchetypes.Load(id), Is.SameAs(d));
            foreach (bool alternate in new[] { false, true })
            {
                Assert.That(d.AttackCommitTimeFor(alternate), Is.LessThan(d.AttackContactTimeFor(alternate)));
                Assert.That(d.AttackContactTimeFor(alternate), Is.LessThan(d.AttackRecoveryTimeFor(alternate)));
                Assert.That(d.AttackRecoveryTimeFor(alternate), Is.LessThan(d.AttackDurationFor(alternate)));
                if (id != ZombieArchetypes.Shambler) Assert.That(d.AttackContactTimeFor(alternate), Is.GreaterThanOrEqualTo(.5f));
            }
        }
        [Test] public void CodecRoundtripPreservesTypeDeathAndLootOwnership()
        {
            var codec = new SaveCodec(new SaveValidation("world.fixture","content.1",
                new Dictionary<string,int>{{"ammo.rifle",60},{"medical.bandage",10}},"rifle.1",30));
            var input = SaveFoundationTests.Fixture(); input.world.enemies[0].archetypeId = ZombieArchetypes.Runner;
            Assert.That(codec.Encode(input,out var json).Success,Is.True);
            Assert.That(codec.Decode(json,out var output).Success,Is.True);
            Assert.That(output.world.enemies[0].archetypeId,Is.EqualTo(ZombieArchetypes.Runner));Assert.That(output.world.enemies[0].health,Is.Zero);
            Assert.That(output.world.opportunities[0].entityId,Is.EqualTo(input.world.opportunities[0].entityId));
            Assert.That(output.world.items[0].disposition,Is.EqualTo(EntityDisposition.Consumed));
            input.world.enemies[0].archetypeId="future";Assert.That(codec.Encode(input,out _).Success,Is.False);
        }
        [Test] public void InvalidProfileCannotActivate()
        {
            var d = Object.Instantiate(Profile("Runner"));
            try { var so = new SerializedObject(d); so.FindProperty("burstSeconds").floatValue = float.NaN; so.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(d.IsValid(out _), Is.False); }
            finally { Object.DestroyImmediate(d); }
        }
        [Test] public void IdentifiersAreUniqueAndFutureIdsAreRejected()
        {
            var ids = new HashSet<string>(); foreach (var name in new[] { "Shambler", "Lurker", "Runner" }) Assert.That(ids.Add(Profile(name).ArchetypeId), Is.True);
            Assert.That(ZombieArchetypes.ValidSaved("zombie.future", 30), Is.False);
            Assert.That(ZombieArchetypes.Load("zombie.future"), Is.Null);
        }
        [Test] public void LegacyHealthAndTerminalStateRemainReadable()
        {
            var legacy = JsonUtility.FromJson<EnemySnapshot>("{\"id\":\"legacy\",\"health\":100}");
            Assert.That(ZombieArchetypes.Legacy(legacy.archetypeId), Is.EqualTo(ZombieArchetypes.Shambler));
            Assert.That(ZombieArchetypes.ValidSaved(legacy.archetypeId,legacy.health), Is.True);
            Assert.That(ZombieArchetypes.ValidSaved(ZombieArchetypes.Runner,61), Is.False);
            var dead = JsonUtility.FromJson<EnemySnapshot>(JsonUtility.ToJson(new EnemySnapshot { id="dead", archetypeId=ZombieArchetypes.Runner,health=0 }));
            Assert.That(dead.health, Is.Zero); Assert.That(dead.archetypeId, Is.EqualTo(ZombieArchetypes.Runner));
        }
        [Test] public void RunnerHasUnskippablePreparationAndBoundedRecovery()
        {
            var d=Profile("Runner");var cycle=new ZombieArchetypeCycle();cycle.Reset(false);
            cycle.Advance(d,100,true,true,true,false,8);
            Assert.That(cycle.Phase,Is.EqualTo(ZombieArchetypePhase.Preparation)); Assert.That(cycle.CanAttack,Is.False);
            cycle.Advance(d,d.PreparationSeconds,true,true,true,false,8);
            Assert.That(cycle.Phase,Is.EqualTo(ZombieArchetypePhase.Burst));
            cycle.Advance(d,d.BurstSeconds,true,true,true,false,8);
            Assert.That(cycle.Phase,Is.EqualTo(ZombieArchetypePhase.Recovery)); Assert.That(cycle.Hold,Is.True);
            cycle.Advance(d,d.RecoverySeconds,true,true,true,false,8);
            Assert.That(cycle.Phase,Is.EqualTo(ZombieArchetypePhase.Normal)); Assert.That(cycle.Cooldown,Is.GreaterThan(0));
        }
        [Test] public void LosLossAndStaggerCancelRunnerAndCannotSkipRecovery()
        {
            var d=Profile("Runner");var cycle=new ZombieArchetypeCycle();cycle.Reset(false);
            cycle.Advance(d,.1f,true,true,true,false,8);cycle.Advance(d,1,true,true,true,false,8);
            cycle.Advance(d,.1f,false,false,true,false,8);
            Assert.That(cycle.Phase,Is.EqualTo(ZombieArchetypePhase.Recovery));
            cycle.Interrupt(d);float remaining=cycle.Remaining;cycle.Advance(d,0,true,true,true,false,8);
            Assert.That(cycle.Remaining,Is.EqualTo(remaining));Assert.That(cycle.CanAttack,Is.False);
        }
        [Test] public void LurkerRequiresEvidenceAndAuthoredRangeBeforeCommitment()
        {
            var d=Profile("Lurker");var c=new ZombieArchetypeCycle();c.Reset(true);
            c.Advance(d,10,false,false,false,false,1);Assert.That(c.Phase,Is.EqualTo(ZombieArchetypePhase.Dormant));
            c.Advance(d,.1f,true,false,false,false,9);Assert.That(c.Phase,Is.EqualTo(ZombieArchetypePhase.Suspicious));
            c.Advance(d,10,true,true,true,false,9);Assert.That(c.Phase,Is.EqualTo(ZombieArchetypePhase.Suspicious));
            c.Advance(d,.1f,true,true,true,false,4);Assert.That(c.Phase,Is.EqualTo(ZombieArchetypePhase.Preparation));
            Assert.That(c.Remaining,Is.EqualTo(d.PreparationSeconds));
        }
        [Test] public void HeardPositionReleasesAmbushToSharedInvestigation()
        {
            var c=new ZombieArchetypeCycle();var d=Profile("Lurker");c.Reset(true);
            c.Advance(d,.1f,false,false,false,true,0);c.Advance(d,.1f,false,false,false,true,0);
            Assert.That(c.Phase,Is.EqualTo(ZombieArchetypePhase.Normal));
        }
        [Test] public void SelectionIsStableAndIndependentOfGlobalRandom()
        {
            var saved=UnityEngine.Random.state;
            try { string a=ZombieArchetypes.Select("seed:22:cell:1:0:7"); UnityEngine.Random.InitState(902);
                Assert.That(ZombieArchetypes.Select("seed:22:cell:1:0:7"),Is.EqualTo(a));
                var types=new HashSet<string>();for(int i=0;i<300;i++)types.Add(ZombieArchetypes.Select("cell:"+i)); Assert.That(types.Count,Is.EqualTo(3)); }
            finally { UnityEngine.Random.state=saved; }
        }
        [Test] public void SameCountDifferentCompositionAndContextProduceDifferentRisk()
        {
            var d=AssetDatabase.LoadAssetAtPath<ZombieEncounterRisk>("Assets/LastSignal/Data/Enemies/Zombie/S022EncounterRisk.asset");
            var c=new ZombieEncounterContext { visibility=1,escapeRoutes=2,approachDistance=10,recoverySpacing=2 };
            float baseline=d.Score(3,0,0,c);Assert.That(d.Score(1,1,1,c),Is.GreaterThan(baseline));
            c.visibility=.2f;c.interior=true;c.ambush=true;c.escapeRoutes=1;c.constrainedTraversal=true;
            Assert.That(d.Score(1,1,1,c),Is.GreaterThan(baseline*1.5f));
        }
        [Test] public void InvalidDangerWeightsAndEmptyCompositionWeightsAreRejected()
        {
            var risk = ScriptableObject.CreateInstance<ZombieEncounterRisk>();
            try { risk.runnerWeight = float.NaN; Assert.That(risk.IsValid, Is.False);
                Assert.Throws<ArgumentException>(() => risk.Score(1,1,1,new ZombieEncounterContext())); }
            finally { Object.DestroyImmediate(risk); }
            Assert.Throws<ArgumentException>(() => ZombieArchetypes.Select("fixture",0,0,0));
        }
        static PopulationSnapshot Population() => new PopulationSnapshot {
            pressures=new[] { new CellPressureSnapshot { cellId="cell:1:0",receipts=Array.Empty<NoiseReceiptSnapshot>() },
                new CellPressureSnapshot { cellId="cell:2:0",receipts=Array.Empty<NoiseReceiptSnapshot>() } },
            ledgers=new[] { new PopulationLedgerSnapshot {cellId="cell:1:0",logical=1,dead=1},new PopulationLedgerSnapshot {cellId="cell:2:0",logical=0} },
            actors=new[] {new PopulationActorSnapshot {id="runner",cellId="cell:1:0",archetypeId=ZombieArchetypes.Runner,health=42}},
            migrations=new[] { new MigrationGroupSnapshot {groupId="trip",sourceCellId="cell:2:0",targetCellId="cell:1:0",departureTime=0,arrivalTime=120,size=1,
                actors=new[] {new PopulationActorSnapshot {id="lurker",cellId="cell:2:0",archetypeId=ZombieArchetypes.Lurker,health=75}} } },
            tombstones=new[] {new PopulationActorSnapshot {id="dead",cellId="cell:1:0",archetypeId=ZombieArchetypes.Runner,health=0}} };
        static bool Valid(PopulationSnapshot p) => SaveValidation.ValidPopulation(p,new HashSet<string>{"cell:1:0","cell:2:0"},0);
        [Test] public void PopulationConservesTypedMigrationAndRejectsDuplicateOrDeadLivingActors()
        {
            var p=Population();Assert.That(Valid(p),Is.True);
            p.migrations[0].actors[0].id="runner";Assert.That(Valid(p),Is.False);
            p=Population();p.actors[0].health=0;Assert.That(Valid(p),Is.False);
            p=Population();p.tombstones[0].id="runner";Assert.That(Valid(p),Is.False);
            p=Population();p.migrations[0].actors[0].archetypeId="future";Assert.That(Valid(p),Is.False);
            p=Population();p.migrations[0].size=2;Assert.That(Valid(p),Is.False);
        }
        [Test] public void TombstoneCannotBeLivingAndLegacyMigrationIsAccepted()
        {
            var p=Population();p.tombstones[0].health=1;Assert.That(Valid(p),Is.False);
            p=Population();p.migrations[0].actors=null;p.actors[0].archetypeId=null;p.actors[0].health=100;Assert.That(Valid(p),Is.True);
        }
        [TestCase("Lurker")][TestCase("Runner")]
        public void ArchetypePrefabsPreserveProductionAnatomyAndHumanoidBindings(string name)
        {
            var go=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_"+name+".prefab");Assert.That(go,Is.Not.Null);
            var actor=go.GetComponent<ZombieController>();Assert.That(actor.Definition,Is.SameAs(Profile(name)));
            Assert.That(go.GetComponent<ZombieHealth>().MaxHealth,Is.EqualTo(actor.Definition.ArchetypeHealth));
            Assert.That(go.GetComponent<ZombieDismemberment>(),Is.Not.Null);
            Assert.That(go.GetComponentsInChildren<ZombieHitRegion>(true).Length,Is.GreaterThanOrEqualTo(7));
            var animator=go.GetComponentInChildren<Animator>(true);Assert.That(animator.avatar.isHuman && animator.avatar.isValid,Is.True);
            Assert.That(animator.applyRootMotion,Is.False);
            Assert.That(go.GetComponent<LastSignal.Audio.ZombieAudioPresenter>(),Is.Not.Null);
        }
        [Test] public void NewCriticalAudioCuesHaveReservedPriorityAndLocalClips()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<LastSignal.Audio.AudioCatalog>("Assets/LastSignal/Audio/Config/S015AudioCatalog.asset");
            foreach(var id in new[]{LastSignal.Audio.AudioCue.LurkerTelegraph,LastSignal.Audio.AudioCue.RunnerPreparation,LastSignal.Audio.AudioCue.RunnerTelegraph})
            { var cue=catalog.Get(id);Assert.That(cue,Is.Not.Null);Assert.That(cue.spatial,Is.True);Assert.That(cue.priority,Is.LessThanOrEqualTo(16));
                Assert.That(cue.clips.Length,Is.GreaterThan(0));foreach(var clip in cue.clips)Assert.That(clip,Is.Not.Null); }
        }
    }
}
