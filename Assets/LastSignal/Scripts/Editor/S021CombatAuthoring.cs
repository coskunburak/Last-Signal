using System;
using UnityEditor;
using UnityEngine;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;

namespace LastSignal.EditorTools
{
    // Project-owned adapters around already imported models; vendor source assets are untouched.
    public static class S021CombatAuthoring
    {
        const string Data = "Assets/LastSignal/Data/Combat/S021";
        const string Items = "Assets/LastSignal/Data/Items/Definitions";
        const string Prefabs = "Assets/LastSignal/Prefabs/Combat/S021";
        const string RiflePrefab = "Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab";
        const string CrowbarPrefab = "Assets/LastSignal/Prefabs/Combat/Crowbar/Crowbar_Viewmodel.prefab";
        const string PistolModel = "Assets/LastSignal/Prefabs/Combat/MRPoly/World/Tactical Pistol (Black).prefab";
        const string BladeModel = "Assets/ThirdParty/Weapons/machete_4k.blend/machete_4k.blend";
        const string AxeModel = "Assets/ThirdParty/Weapons/wooden_axe_4k.blend/wooden_axe_4k.blend";

        [MenuItem("Last Signal/S021/Author Combat Assets")]
        public static void Author()
        {
            Folder(Data); Folder(Prefabs);
            var ammo = Item("ammo.9mm", "9 mm Rounds", "Tabanca için yedek fişek.", ItemCategory.Ammunition, 30, 12);
            var pistol = Weapon("Weapon_TacticalPistol", ammo);
            var blade = Melee("Machete", MeleeWeaponDefinition.BladeId, .13f, .09f, .29f, 1.45f, .22f, 22f, 13f, false);
            var axe = Melee("WoodenAxe", MeleeWeaponDefinition.AxeId, .48f, .13f, .78f, 1.9f, .28f, 51f, 43f, true);
            Item("weapon.pistol", "Tactical Pistol", "Kısa menzilli acil durum silahı.", ItemCategory.Tool, 1, 850);
            Item("weapon.blade", "Machete", "Hızlı fakat kısa erişimli kesici.", ItemCategory.Tool, 1, 650);
            Item("weapon.axe", "Wooden Axe", "Ağır ve yavaş yakın dövüş silahı.", ItemCategory.Tool, 1, 2200);
            AmmoWorld(ammo);
            FirearmPrefab(pistol);
            MeleePrefab("Machete", blade, BladeModel, "blade");
            MeleePrefab("WoodenAxe", axe, AxeModel, "axe");
            WorldFirearm(pistol);
            WorldMelee("Machete", blade, BladeModel);
            WorldMelee("WoodenAxe", axe, AxeModel);
            WorldRig();
            WeaponGripAuthoring.Author();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("S021 combat assets authored. Inspect model pivots, hand alignment and clips in the Editor before visual acceptance.");
        }

        static void Folder(string path)
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) Folder(parent);
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
        static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset) throw new InvalidOperationException("Missing required S021 source: " + path);
            return asset;
        }
        static T New<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
        static void Set(SerializedObject so, string name, string value) => so.FindProperty(name).stringValue = value;
        static ItemDefinition Item(string id, string title, string description, ItemCategory category, int stack, int grams)
        {
            var item = New<ItemDefinition>(Items + "/" + id + ".asset");
            var so = new SerializedObject(item);
            so.FindProperty("stableId").FindPropertyRelative("id").stringValue = id;
            Set(so, "displayName", title); Set(so, "description", description);
            so.FindProperty("category").enumValueIndex = (int)category;
            so.FindProperty("maxStack").intValue = stack;
            so.FindProperty("massGrams").intValue = grams;
            if (id.StartsWith("weapon.", StringComparison.Ordinal))
                so.FindProperty("icon").objectReferenceValue = Load<Sprite>("Assets/LastSignal/Art/ItemIcons/" + id + ".png");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }
        static WeaponDefinition Weapon(string name, ItemDefinition ammo)
        {
            var asset = New<WeaponDefinition>(Data + "/" + name + ".asset");
            var so = new SerializedObject(asset);
            Set(so, "weaponId", "weapon.pistol"); Set(so, "weaponName", "Tactical Pistol");
            so.FindProperty("category").enumValueIndex = (int)WeaponCategory.Pistol;
            so.FindProperty("ammunition").objectReferenceValue = ammo;
            so.FindProperty("magazineCapacity").intValue = 15;
            so.FindProperty("startingMagazine").intValue = 8;
            so.FindProperty("fireMode").enumValueIndex = (int)FireMode.SemiAutomatic;
            so.FindProperty("roundsPerMinute").floatValue = 300;
            so.FindProperty("maxRange").floatValue = 65;
            so.FindProperty("baseDamage").floatValue = 22;
            so.FindProperty("tacticalReloadSeconds").floatValue = 2.1f;
            so.FindProperty("emptyReloadSeconds").floatValue = 2.6f;
            so.FindProperty("recoilVerticalDegrees").floatValue = 2f;
            so.FindProperty("recoilHorizontalRange").floatValue = .65f;
            so.FindProperty("adsFovDegrees").floatValue = 60;
            so.FindProperty("equipSeconds").floatValue = .32f;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(asset);
            return asset;
        }
        static MeleeWeaponDefinition Melee(string name, string id, float windup, float active, float recovery,
            float range, float radius, float damage, float stamina, bool heavy)
        {
            var asset = New<MeleeWeaponDefinition>(Data + "/" + name + ".asset");
            asset.WeaponId = id; asset.WindupSeconds = windup; asset.ActiveSeconds = active;
            asset.RecoverySeconds = recovery; asset.Range = range; asset.Radius = radius;
            asset.Damage = damage; asset.StaminaCost = stamina; asset.Heavy = heavy;
            EditorUtility.SetDirty(asset); return asset;
        }
        static void AmmoWorld(ItemDefinition ammo)
        {
            string path = "Assets/LastSignal/Prefabs/Items/ammo.9mm.prefab";
            var source = Load<GameObject>("Assets/LastSignal/Prefabs/Items/ammo.rifle.prefab");
            var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source));
            try
            {
                root.name = "ammo.9mm";
                var world = root.GetComponent<WorldItem>();
                var so = new SerializedObject(world);
                so.FindProperty("definition").objectReferenceValue = ammo;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var item = new SerializedObject(ammo);
            item.FindProperty("worldPrefab").objectReferenceValue = Load<GameObject>(path);
            item.ApplyModifiedPropertiesWithoutUndo();
        }
        static void FirearmPrefab(WeaponDefinition definition)
        {
            var root = PrefabUtility.LoadPrefabContents(RiflePrefab);
            try
            {
                root.name = "Weapon_TacticalPistol";
                var controller = root.GetComponent<WeaponController>();
                var so = new SerializedObject(controller);
                so.FindProperty("definition").objectReferenceValue = definition;
                so.ApplyModifiedPropertiesWithoutUndo();
                var body = Find(root.transform, "MRPoly_RifleBody");
                var magazine = Find(root.transform, "MRPoly_Magazine");
                if (!body || !magazine) throw new InvalidOperationException("Rifle adapter hierarchy changed.");
                var oldBounds = body.GetComponent<Renderer>().bounds;
                body.GetComponent<Renderer>().enabled = false;
                magazine.GetComponent<Renderer>().enabled = false;
                var model = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(PistolModel));
                model.name = "MRPoly_TacticalPistol";
                model.transform.SetParent(body.parent, false);
                model.transform.SetPositionAndRotation(body.position, body.rotation);
                SetLayer(model.transform, 29);
                Fit(model, .30f, oldBounds.center);
                var bounds = BoundsOf(model);
                var muzzle = Find(root.transform, "Muzzle");
                var sight = Find(root.transform, "AimReference");
                muzzle.position = bounds.center + root.transform.forward * .15f;
                sight.position = bounds.center + root.transform.up * .045f;
                var audio = root.GetComponent<WeaponAudioPresenter>();
                var clips = new SerializedObject(audio);
                var source = root.GetComponent<AudioSource>();
                if (!source) source = root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0;
                source.volume = .55f;
                clips.FindProperty("audioSource").objectReferenceValue = source;
                clips.FindProperty("fireClip").objectReferenceValue = Clip("Handgun/Gunshots/sfx_wpn_hgn_gunshot_01.wav");
                clips.FindProperty("reloadStartClip").objectReferenceValue = Clip("Handgun/Reload/sfx_wpn_hgn_reload_01_slideLock.wav");
                clips.FindProperty("magazineOutClip").objectReferenceValue = Clip("Handgun/Reload/sfx_wpn_hgn_reload_02_magOut.wav");
                clips.FindProperty("magazineInClip").objectReferenceValue = Clip("Handgun/Reload/sfx_wpn_hgn_reload_04_magIn.wav");
                clips.FindProperty("boltClip").objectReferenceValue = Clip("Handgun/Reload/sfx_wpn_hgn_reload_05_charge.wav");
                clips.FindProperty("equipClip").objectReferenceValue = Clip("Handgun/Foley/sfx_wpn_hgn_foley_equip_01.wav");
                clips.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/Weapon_TacticalPistol.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static void MeleePrefab(string name, MeleeWeaponDefinition definition, string modelPath, string prefix)
        {
            var root = PrefabUtility.LoadPrefabContents(CrowbarPrefab);
            try
            {
                root.name = name + "_Viewmodel";
                var controller = root.GetComponent<MeleeWeaponController>();
                var so = new SerializedObject(controller);
                so.FindProperty("definition").objectReferenceValue = definition;
                so.ApplyModifiedPropertiesWithoutUndo();
                var visual = Find(root.transform, "CrowbarMesh");
                if (!visual) throw new InvalidOperationException("Crowbar visual hierarchy changed.");
                var oldBounds = visual.GetComponent<Renderer>().bounds;
                visual.GetComponent<Renderer>().enabled = false;
                var model = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(modelPath));
                model.name = name + "_Model";
                model.transform.SetParent(visual.parent, false);
                model.transform.SetPositionAndRotation(visual.position, visual.rotation);
                SetLayer(model.transform, 2);
                Fit(model, Mathf.Max(oldBounds.size.x, oldBounds.size.y, oldBounds.size.z), oldBounds.center);
                var presenter = root.GetComponent<MeleeWeaponPresenter>();
                var sound = new SerializedObject(presenter);
                string foley = prefix == "axe" ? "Foley_Movement_Clothes_A_003.wav" : "Foley_Movement_Clothes_A_001.wav";
                string impact = prefix == "axe" ? "Impact_Flesh_003.wav" : "Impact_Flesh_001.wav";
                sound.FindProperty("swing").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/ThirdParty/Audio/ZombieHorrorPackageFree/WAV/Foley/" + foley);
                sound.FindProperty("impact").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/ThirdParty/Audio/ZombieHorrorPackageFree/WAV/Impact/" + impact);
                sound.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/" + name + "_Viewmodel.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static void WorldFirearm(WeaponDefinition definition)
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldRifle.prefab");
            try
            {
                root.name = "Diesel_WorldPistol";
                var oldBounds = BoundsOf(root);
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                var model = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(PistolModel));
                model.name = "MRPoly_TacticalPistol";
                model.transform.SetParent(root.transform, false);
                SetLayer(model.transform, 30);
                Fit(model, .30f, oldBounds.center);
                var b = BoundsOf(model);
                var sockets = root.GetComponent<CharacterWeaponSockets>();
                sockets.RightGrip.position = b.center - root.transform.up * .065f;
                sockets.LeftGrip.position = b.center - root.transform.up * .035f + root.transform.forward * .035f;
                sockets.StockContact.position = b.center - root.transform.forward * .12f;
                sockets.AimReference.position = b.center + root.transform.up * .045f;
                sockets.Muzzle.position = b.center + root.transform.forward * .15f;
                sockets.ReloadReference.position = b.center - root.transform.up * .08f;
                // The inherited rifle magazine mesh is hidden; do not animate it as a pistol magazine.
                sockets.Magazine = null;
                PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/Diesel_WorldPistol.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var profile = Pose("PistolPose", "Assets/LastSignal/Art/CharacterIntegration/RiflePose.asset");
            profile.Firearm = definition; profile.Melee = null;
            profile.WorldPrefab = Load<GameObject>(Prefabs + "/Diesel_WorldPistol.prefab");
            profile.WorldScale = 1;
            profile.HipStockOffset = new Vector3(-.07f, -.07f, .12f);
            profile.AimStockOffset = new Vector3(-.065f, -.025f, .10f);
            profile.FingerGripWeight = .8f;
            profile.RecoilPitch = 3;
            EditorUtility.SetDirty(profile);
        }
        static void WorldMelee(string name, MeleeWeaponDefinition definition, string modelPath)
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldCrowbar.prefab");
            try
            {
                root.name = "Diesel_World" + name;
                var old = Find(root.transform, "CrowbarMesh");
                var bounds = old.GetComponent<Renderer>().bounds;
                old.GetComponent<Renderer>().enabled = false;
                var model = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(modelPath));
                model.name = name + "_Model";
                model.transform.SetParent(root.transform, false);
                SetLayer(model.transform, 30);
                Fit(model, Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z), bounds.center);
                PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/Diesel_World" + name + ".prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var profile = Pose(name + "Pose", "Assets/LastSignal/Art/CharacterIntegration/CrowbarPose.asset");
            profile.Firearm = null; profile.Melee = definition;
            profile.WorldPrefab = Load<GameObject>(Prefabs + "/Diesel_World" + name + ".prefab");
            EditorUtility.SetDirty(profile);
        }
        static CharacterWeaponPoseProfile Pose(string name, string source)
        {
            string path = Data + "/" + name + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(path);
            if (profile) return profile;
            profile = UnityEngine.Object.Instantiate(Load<CharacterWeaponPoseProfile>(source));
            profile.name = name;
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }
        static void WorldRig()
        {
            const string path = "Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldBody.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var rig = root.GetComponent<CharacterWeaponRig>();
                if (!rig) throw new InvalidOperationException("Diesel world body is missing CharacterWeaponRig.");
                var profiles = new System.Collections.Generic.List<CharacterWeaponPoseProfile>();
                if (rig.Profiles != null)
                    foreach (var profile in rig.Profiles) if (profile && !profiles.Contains(profile)) profiles.Add(profile);
                foreach (var name in new[] { "PistolPose", "MachetePose", "WoodenAxePose" })
                {
                    var profile = Load<CharacterWeaponPoseProfile>(Data + "/" + name + ".asset");
                    if (!profiles.Contains(profile)) profiles.Add(profile);
                }
                rig.Configure(profiles.ToArray());
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static AudioClip Clip(string suffix, bool required = true)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ThirdParty/Audio/FreeWeaponSFX_v1.1/" + suffix);
            if (!clip && required) throw new InvalidOperationException("Missing audio clip: " + suffix);
            return clip;
        }
        static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
        static void SetLayer(Transform root, int layer)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = layer;
                foreach (var collider in t.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            }
        }
        static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Imported weapon model has no renderers: " + root.name);
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
        static void Fit(GameObject model, float desiredLength, Vector3 center)
        {
            var bounds = BoundsOf(model);
            float length = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (length < .0001f) throw new InvalidOperationException("Imported weapon model has zero-size bounds.");
            model.transform.localScale *= desiredLength / length;
            bounds = BoundsOf(model);
            model.transform.position += center - bounds.center;
        }
    }
}
