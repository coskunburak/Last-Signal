using System.Collections.Generic;
using LastSignal.Persistence;
using NUnit.Framework;
namespace LastSignal.Tests
{
    public sealed class R01PersistenceTests
    {
        SaveCodec Codec() => new SaveCodec(new SaveValidation("world.fixture","content.1",new Dictionary<string,int>{{"ammo.rifle",60},{"medical.bandage",10}},"rifle.1",30));
        [TestCase(0)] [TestCase(1)] public void EquipmentExtensionRoundtripAndLegacyDefault(int slot)
        {
            var codec=Codec();var s=SaveFoundationTests.Fixture();
            Assert.IsTrue(codec.Encode(s,out var old).Success);Assert.IsTrue(codec.Decode(old,out var legacy).Success);Assert.IsNull(legacy.combat);
            s.combat=new CombatEquipmentSnapshot {version=1,selectedSlot=slot,meleeDefinitionId=MeleeWeaponDefinition.CrowbarId,stamina=17,exhausted=true,regenDelay=.75f};
            Assert.IsTrue(codec.Encode(s,out var bytes).Success);Assert.IsTrue(codec.Decode(bytes,out var result).Success);
            Assert.AreEqual(slot,result.combat.selectedSlot);Assert.AreEqual(17,result.combat.stamina);Assert.IsTrue(result.combat.exhausted);Assert.AreEqual(.75f,result.combat.regenDelay);
        }
        [TestCase(-1)] [TestCase(101)] [TestCase(float.NaN)] public void InvalidStaminaFailsValidation(float value)
        {
            var s=SaveFoundationTests.Fixture();s.combat=new CombatEquipmentSnapshot {version=1,meleeDefinitionId=MeleeWeaponDefinition.CrowbarId,stamina=value};Assert.IsFalse(Codec().Encode(s,out _).Success);
        }
    }
}
