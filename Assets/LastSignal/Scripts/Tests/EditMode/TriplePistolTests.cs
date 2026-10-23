using System.Collections.Generic;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace LastSignal.Tests
{
    public sealed class TriplePistolTests
    {
        const string Root = "Assets/LastSignal/Art/TriplePistols/";
        [TestCase("DXT", PistolVisualVariant.DxtId)]
        [TestCase("Glock17", PistolVisualVariant.GlockId)]
        [TestCase("Custom9mm", PistolVisualVariant.CustomId)]
        public void SourcePrefabRigMaterialsAndMechanicalGeometryResolve(string name, string id)
        {
            var variant=AssetDatabase.LoadAssetAtPath<PistolVisualVariant>(Root+name+"/Variant.asset");
            Assert.That(variant && variant.Valid, Is.True, name);
            Assert.That(variant.Id, Is.EqualTo(id));
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(variant.SourcePath), Is.Not.Null);
            Assert.That(variant.Pose.PrimaryHand, Is.EqualTo(WeaponPrimaryHand.Right));
            Assert.That(variant.Pose.WorldScale, Is.EqualTo(1));
            Assert.That(variant.Pose.Firearm.Ammunition.Id.Value, Is.EqualTo("ammo.9mm"));
            Assert.That(variant.Pose.WorldPrefab, Is.SameAs(variant.VisualPrefab));
            var sockets=variant.VisualPrefab.GetComponent<CharacterWeaponSockets>();
            Assert.That(sockets.ValidFor(variant.Pose), Is.True);
            Assert.That(sockets.LeftGrip, Is.Null); Assert.That(sockets.StockContact, Is.Null);
            Assert.That(sockets.ReloadReference.IsChildOf(sockets.Magazine), Is.True);
            Assert.That(sockets.AimReference.forward, Is.EqualTo(Vector3.forward));
            Assert.That(sockets.Muzzle.localPosition.z, Is.GreaterThan(sockets.AimReference.localPosition.z));
            var mechanics=variant.VisualPrefab.GetComponent<PistolMechanics>();
            Assert.That(mechanics && mechanics.Slide && mechanics.Trigger && mechanics.EjectionPort, Is.True);
            int triangles=0;var bounds=new Bounds();bool first=true;
            foreach(var filter in variant.VisualPrefab.GetComponentsInChildren<MeshFilter>())
            {
                Assert.That(filter.sharedMesh, Is.Not.Null);triangles+=filter.sharedMesh.triangles.Length/3;
                Assert.That(filter.sharedMesh.normals.Length, Is.EqualTo(filter.sharedMesh.vertexCount));
                Assert.That(filter.sharedMesh.uv.Length, Is.EqualTo(filter.sharedMesh.vertexCount));
                foreach(var vertex in filter.sharedMesh.vertices)
                {var point=filter.transform.TransformPoint(vertex);if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}
            }
            Assert.That(triangles, Is.InRange(2000,40000));
            Assert.That(bounds.size.z, Is.InRange(.18f,.25f));Assert.That(bounds.size.x, Is.InRange(.02f,.05f));
            foreach(var transform in variant.VisualPrefab.GetComponentsInChildren<Transform>()) Assert.That(transform.localScale, Is.EqualTo(Vector3.one));
            foreach(var renderer in variant.VisualPrefab.GetComponentsInChildren<Renderer>())
                foreach(var material in renderer.sharedMaterials)
                {
                    Assert.That(material && material.shader, Is.True);
                    Assert.That(material.shader.name, Is.EqualTo("HDRP/Lit"));
                    foreach(var map in new[]{"_BaseColorMap","_NormalMap","_MaskMap"})
                    {
                        var texture=material.GetTexture(map);Assert.That(texture, Is.Not.Null, map);
                        var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
                        Assert.That(importer.maxTextureSize, Is.LessThanOrEqualTo(2048));
                        Assert.That(importer.sRGBTexture, Is.EqualTo(map=="_BaseColorMap"));
                        if(map=="_NormalMap")Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.NormalMap));
                    }
                }
            var fps=AssetDatabase.LoadAssetAtPath<GameObject>(Root+name+"/FPS.prefab");Assert.That(fps, Is.Not.Null);
            Assert.That(fps.GetComponent<FirstPersonWeaponGrip>().ValidAuthoring, Is.True);
            Assert.That(fps.GetComponent<PistolVariantPresenter>().ValidCatalog, Is.True);
            Assert.That(fps.GetComponent<PistolVariantPresenter>().InitialId, Is.EqualTo(id));
            Assert.That(fps.GetComponentInChildren<Animator>(true).runtimeAnimatorController, Is.Not.Null);
            Assert.That(fps.GetComponent<WeaponAudioPresenter>(), Is.Not.Null);
            Assert.That(fps.GetComponent<WeaponVfxPresenter>(), Is.Not.Null);
        }
        [Test] public void CatalogHasThreeDistinctCalibrationsAndOneAmmoOwner()
        {
            var ids=new HashSet<string>();var poses=new HashSet<Object>();var fpsFingers=new HashSet<Object>();var worldFingers=new HashSet<Object>();
            WeaponDefinition definition=null;
            foreach(var name in new[]{"DXT","Glock17","Custom9mm"})
            {
                var variant=AssetDatabase.LoadAssetAtPath<PistolVisualVariant>(Root+name+"/Variant.asset");
                Assert.That(ids.Add(variant.Id)&&poses.Add(variant.Pose)&&fpsFingers.Add(variant.Pose.FpsPistolFingers)&&worldFingers.Add(variant.Pose.WorldPistolFingers), Is.True);
                if(definition)Assert.That(variant.Pose.Firearm, Is.SameAs(definition));else definition=variant.Pose.Firearm;
            }
            var player=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player/Player.prefab");
            var data=new SerializedObject(player.GetComponent<PlayerCombatController>());
            var pistol=(WeaponController)data.FindProperty("secondaryWeapon").objectReferenceValue;
            Assert.That(pistol.GetComponent<PistolVariantPresenter>().ValidCatalog, Is.True);
        }
        [Test] public void InvalidVariantAndLegacySaveIdentityAreExplicit()
        {
            var variant=ScriptableObject.CreateInstance<PistolVisualVariant>();
            try {variant.Id="unknown";Assert.That(variant.Valid, Is.False);}
            finally {Object.DestroyImmediate(variant);}
            Assert.That(PistolVisualVariant.KnownOrLegacy(null), Is.True);
            Assert.That(PistolVisualVariant.KnownOrLegacy("pistol.unknown"), Is.False);
            var old=JsonUtility.FromJson<CombatEquipmentSnapshot>("{\"version\":2,\"selectedFirearmId\":\"weapon.pistol\"}");
            Assert.That(string.IsNullOrEmpty(old.pistolVisualVariantId), Is.True);
            foreach(var id in new[]{PistolVisualVariant.DxtId,PistolVisualVariant.GlockId,PistolVisualVariant.CustomId})
            {
                old.pistolVisualVariantId=id;
                Assert.That(JsonUtility.FromJson<CombatEquipmentSnapshot>(JsonUtility.ToJson(old)).pistolVisualVariantId, Is.EqualTo(id));
            }
        }
        [Test] public void RealSaveCodecRejectsUnknownVariantsAndReadsLegacyPayloads()
        {
            var codec=new SaveCodec(new SaveValidation("world.fixture","content.1",
                new Dictionary<string,int>{{"ammo.rifle",60},{"medical.bandage",10}},"rifle.1",30,
                additionalWeapons:new Dictionary<string,int>{{"weapon.pistol",15}}));
            var save=SaveFoundationTests.Fixture();save.weapon.condition=900;
            save.combat=new CombatEquipmentSnapshot{version=2,selectedSlot=0,selectedFirearmId="weapon.pistol",
                meleeDefinitionId=MeleeWeaponDefinition.CrowbarId,stamina=PlayerStamina.Maximum,
                firearms=new[]{new WeaponSnapshot{definitionId="rifle.1",magazine=17,condition=900},
                    new WeaponSnapshot{definitionId="weapon.pistol",magazine=7,condition=750}}};
            foreach(var id in new[]{null,PistolVisualVariant.DxtId,PistolVisualVariant.GlockId,PistolVisualVariant.CustomId})
            {
                save.combat.pistolVisualVariantId=id;
                Assert.That(codec.Encode(save,out var json).Success,Is.True);
                Assert.That(codec.Decode(json,out var restored).Success,Is.True);
                Assert.That(restored.combat.pistolVisualVariantId??"",Is.EqualTo(id??""));
                Assert.That(restored.combat.firearms[1].magazine,Is.EqualTo(7));
            }
            save.combat.pistolVisualVariantId="unknown";Assert.That(codec.Encode(save,out _).Success,Is.False);
        }
    }
}
