using System;
using System.Collections.Generic;
using LastSignal.Persistence;
using LastSignal.WorldCells;
using LastSignal.WorldTime;
using NUnit.Framework;

namespace LastSignal.Tests
{
    public sealed class FirstTownPersistenceTests
    {
        static SaveCodec Codec() => new SaveCodec(new SaveValidation("world.fixture","content.1",
            new Dictionary<string,int> { { "ammo.rifle",60 }, { "medical.bandage",10 } },"rifle.1",30));
        static SaveGame Resident()
        {
            var state=SaveFoundationTests.Fixture(); state.header.schemaVersion=4;
            state.worldTime=new WorldSimulation(new WorldTimeSettings { startingSeconds=100 }).Capture();
            state.cells=new CellWorldSnapshot { playerCell="resident",cells=Array.Empty<CellSnapshot>() };
            state.population=new PopulationSnapshot {
                pressures=Array.Empty<CellPressureSnapshot>(),ledgers=Array.Empty<PopulationLedgerSnapshot>(),
                actors=Array.Empty<PopulationActorSnapshot>(),tombstones=Array.Empty<PopulationActorSnapshot>(),migrations=Array.Empty<MigrationGroupSnapshot>()
            };
            return state;
        }
        [Test] public void ResidentWorldWithNoPortalCellsAndMultipleEnemiesRoundtrips()
        {
            var state=Resident(); var dead=state.world.enemies[0];
            state.world.enemies=new[]{dead,new EnemySnapshot { id="town.enemy.second",health=70,transform=new TransformSnapshot { x=30,y=.05f,qw=1 } }};
            var codec=Codec(); var result=codec.Encode(state,out var json); Assert.That(result.Success,Is.True,result.Message);
            result=codec.Decode(json,out var copy); Assert.That(result.Success,Is.True,result.Message);
            Assert.That(copy.cells.cells,Is.Empty); Assert.That(copy.world.enemies.Length,Is.EqualTo(2));
            Assert.That(Array.Find(copy.world.enemies,e=>e.id==dead.id).health,Is.Zero);
            Assert.That(Array.Find(copy.world.enemies,e=>e.id=="town.enemy.second").health,Is.EqualTo(70));
        }
        [Test] public void EmptyCellsCannotClaimAnUnvisitedDestination()
        {
            var state=Resident(); state.cells.playerCell="cell:1:0";
            Assert.That(Codec().Encode(state,out _).Success,Is.False);
        }
        [Test] public void ResidentWorldStillRejectsDuplicateEnemyIdentity()
        {
            var state=Resident(); state.world.enemies=new[]{state.world.enemies[0],state.world.enemies[0]};
            Assert.That(Codec().Encode(state,out _).Success,Is.False);
        }
        [Test] public void MissingCellArrayIsNotAResidentWorld()
        {
            var state=Resident(); state.cells.cells=null;
            Assert.That(Codec().Encode(state,out _).Success,Is.False);
        }
    }
}
