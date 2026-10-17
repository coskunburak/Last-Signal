using System;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Authoring;
using LastSignal.Inventory.Data;
using LastSignal.Loot;
using LastSignal.Persistence;
using LastSignal.WorldTime;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace LastSignal.Editor.FirstTown
{
    internal sealed class FirstTownGeometry
    {
        internal readonly List<DoorInteractable> Doors = new List<DoorInteractable>();
        internal readonly List<ZombieEncounter> Encounters = new List<ZombieEncounter>();
        internal readonly List<Transform> Routes = new List<Transform>();
        readonly Dictionary<string, LootProfile> profiles = new Dictionary<string, LootProfile>();
        readonly FirstTownAssets art;
        readonly Transform town;
        internal FirstTownGeometry(FirstTownAssets art, Transform town) { this.art = art; this.town = town; }
        internal static Transform Group(string name, Transform parent, Vector3 position = default)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t; }
        internal GameObject Box(string name, Transform parent, Vector3 p, Vector3 size, string material, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = p; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = art.Mat(material);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        internal static void Identity(GameObject go, string id)
        {
            var component = go.GetComponent<PersistentEntityId>() ?? go.AddComponent<PersistentEntityId>();
            FirstTownAuthoring.Set(component, "id", id);
        }
        internal Transform Route(string name, Transform parent, Vector3 position)
        { var t = Group("Route - " + name, parent, position); Routes.Add(t); return t; }
        internal void Sign(Transform parent, string text, Vector3 position, float yaw = 0, float width = 4)
        {
            var root = Group("Sign - " + text.Replace('\n', ' '), parent, position); root.localRotation = Quaternion.Euler(0, yaw, 0);
            Box("Board", root, Vector3.zero, new Vector3(width,.85f,.10f), "DarkTimber");
            var label = Group("Lettering", root, new Vector3(0,0,-.065f)).gameObject.AddComponent<TextMesh>();
            label.text = text; label.fontSize = 64; label.characterSize = .095f;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = new Color(.86f,.83f,.70f);
        }
        internal static float RiverX(float z) => -112 + 7 * Mathf.Sin(z * .018f);
        internal void GroundAndRoads()
        {
            var land = Group("00 Landscape", town);
            var data = new TerrainData { name = "FirstTown 500m terrain", heightmapResolution = 513, size = new Vector3(500,20,500) };
            var heights = new float[513,513];
            for (int z = 0; z < 513; z++) for (int x = 0; x < 513; x++)
            {
                float wx = -250 + x * 500f / 512, wz = -250 + z * 500f / 512;
                float bank = Mathf.Abs(wx - RiverX(wz));
                float channel = -3.5f * (1 - Mathf.SmoothStep(0,1,Mathf.InverseLerp(4,12,bank)));
                float edge = Mathf.SmoothStep(0,1,Mathf.InverseLerp(228,250,Mathf.Max(Mathf.Abs(wx),Mathf.Abs(wz))));
                // Buildable plateau; channel carved below physical bridge decks, raised outer terrain.
                float hill = edge * (2 + 3 * Mathf.PerlinNoise((wx+400)*.015f,(wz+400)*.015f));
                heights[z,x] = (10 + channel + hill) / 20;
            }
            data.SetHeights(0,0,heights);
            var soil = art.Mat("GroundSoil");
            var layer = new TerrainLayer { name = "FirstTown soil", tileSize = new Vector2(8,8), diffuseTexture = soil.mainTexture as Texture2D };
            art.Save(layer, FirstTownAuthoring.Data + "Soil.terrainlayer"); data.terrainLayers = new[] { layer };
            art.Save(data, FirstTownAuthoring.Data + "Terrain.asset");
            var terrain = Terrain.CreateTerrainGameObject(data); terrain.name = "500 x 500 m core"; terrain.transform.SetParent(land, false);
            terrain.transform.localPosition = new Vector3(-250,-10,-250);
            var terrainMaterial = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")) { name = "FirstTown Terrain" };
            art.Save(terrainMaterial, FirstTownAssets.Root + "/Materials/Terrain.mat"); terrain.GetComponent<Terrain>().materialTemplate = terrainMaterial;
            terrain.GetComponent<Terrain>().drawInstanced = true;
            var water = new Mesh { name = "FirstTown creek ribbon" };
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            for (int i=0;i<=100;i++)
            {
                float z=-250+i*5; vertices.Add(new Vector3(RiverX(z)-5,-1.45f,z)); vertices.Add(new Vector3(RiverX(z)+5,-1.45f,z));
                uv.Add(new Vector2(0,i*.5f)); uv.Add(new Vector2(1,i*.5f));
                if(i==100)continue; int a=i*2; triangles.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});
            }
            water.SetVertices(vertices); water.SetTriangles(triangles,0); water.SetUVs(0,uv); water.RecalculateNormals(); water.RecalculateBounds();
            art.Save(water, FirstTownAuthoring.Data + "Creek.asset");
            var river=Group("Creek - below bridge deck",land).gameObject; river.AddComponent<MeshFilter>().sharedMesh=water;
            river.AddComponent<MeshRenderer>().sharedMaterial=art.Mat("Water");
            // The channel is deliberately non-traversable; bridges are the accepted crossings.
            var modifier=land.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifierVolume>();
            modifier.center=new Vector3(-112,-2,0); modifier.size=new Vector3(38,3,500); modifier.area=1;
            var roads=Group("01 Connected road network",town);
            Road(roads,"Main street",new Vector2(0,-220),new Vector2(0,210),12);
            Road(roads,"South cross street",new Vector2(-200,-140),new Vector2(210,-140),10);
            Road(roads,"North cross street",new Vector2(-190,80),new Vector2(215,80),10);
            Road(roads,"Market street",new Vector2(-75,0),new Vector2(210,0),10);
            Road(roads,"East loop",new Vector2(120,-140),new Vector2(120,210),9);
            Road(roads,"Industrial service road",new Vector2(210,-140),new Vector2(210,80),9);
            Road(roads,"School avenue",new Vector2(-75,0),new Vector2(-75,170),8);
            Road(roads,"Safehouse approach",new Vector2(-180,-170),new Vector2(-180,-140),8);
            Road(roads,"Clinic driveway",new Vector2(-55,-55),new Vector2(0,-55),7);
            Road(roads,"Gas station access",new Vector2(50,-140),new Vector2(50,0),9);
            Road(roads,"Motel access",new Vector2(80,-190),new Vector2(80,-140),8);
            Road(roads,"Farm track",new Vector2(120,200),new Vector2(200,200),7,false);
            Road(roads,"North neighborhood",new Vector2(40,155),new Vector2(210,155),8);
            Road(roads,"Neighborhood connector",new Vector2(40,80),new Vector2(40,155),8);
            Bridge(roads,-140); Bridge(roads,80);
            foreach(var junction in new[]{new Vector2(0,-140),new Vector2(0,0),new Vector2(0,80),new Vector2(120,0),new Vector2(120,80),
                new Vector2(120,-140),new Vector2(210,0),new Vector2(210,80),new Vector2(210,-140),new Vector2(50,0),new Vector2(50,-140),
                new Vector2(40,80),new Vector2(40,155),new Vector2(120,155),new Vector2(-75,0),new Vector2(-75,80),new Vector2(-180,-140),new Vector2(80,-140)})
                Box("Clear junction",roads,new Vector3(junction.x,.02f,junction.y),new Vector3(14,.04f,14),"Asphalt");
            foreach(float z in new[]{-100f,-40,40,110,180})
            {
                Box("West pavement",roads,new Vector3(-9,.04f,z),new Vector3(4,.08f,44),"Stone");
                Box("East pavement",roads,new Vector3(9,.04f,z),new Vector3(4,.08f,44),"Stone");
            }
            // Boundary is terrain backed and visibly fenced; no invisible world wall.
            foreach(float x in new[]{-245f,245f}) for(int i=0;i<49;i++) art.Place("Fence",land,new Vector3(x,data.GetInterpolatedHeight((x+250)/500f,(10+i*10)/500f)-10,-240+i*10),90,10);
            foreach(float z in new[]{-245f,245f}) for(int i=0;i<49;i++) art.Place("Fence",land,new Vector3(-240+i*10,data.GetInterpolatedHeight((10+i*10)/500f,(z+250)/500f)-10,z),0,10);
        }
        void Road(Transform parent,string name,Vector2 a,Vector2 b,float width,bool asphalt=true)
        {
            var delta=b-a; var root=Group(name,parent,new Vector3((a.x+b.x)*.5f,0,(a.y+b.y)*.5f));
            root.localRotation=Quaternion.LookRotation(new Vector3(delta.x,0,delta.y));
            float length=delta.magnitude+width;
            var bed=Box("Roadbed",root,new Vector3(0,-.14f,0),new Vector3(width,.32f,length),asphalt?"Asphalt":"GroundPath");
            bed.GetComponent<Renderer>().enabled=false;
            var mesh=new Mesh { name=name+" surface" };
            mesh.vertices=new[]{new Vector3(-width/2,.021f,-length/2),new Vector3(-width/2,.021f,length/2),new Vector3(width/2,.021f,length/2),new Vector3(width/2,.021f,-length/2)};
            mesh.triangles=new[]{0,1,2,0,2,3}; mesh.uv=new[]{Vector2.zero,new Vector2(0,length/8),new Vector2(width/8,length/8),new Vector2(width/8,0)};
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            art.Save(mesh,FirstTownAuthoring.Data+"Road_"+name.Replace(' ','_')+".asset");
            var surface=Group("Road surface",root).gameObject; surface.AddComponent<MeshFilter>().sharedMesh=mesh;
            surface.AddComponent<MeshRenderer>().sharedMaterial=art.Mat(asphalt?"Asphalt":"GroundPath");
            if(!asphalt)return;
            for(float z=-delta.magnitude*.5f+4;z<delta.magnitude*.5f-3;z+=12)
                Box("Worn center line",root,new Vector3(0,.025f,z),new Vector3(.12f,.008f,4),"Stripe",false);
        }
        void Bridge(Transform roads,float z)
        {
            float x=RiverX(z); var bridge=Group("Bridge " + z,roads,new Vector3(x,0,z));
            Box("Continuous deck",bridge,new Vector3(0,-.10f,0),new Vector3(34,.24f,11),"Stone");
            foreach(float side in new[]{-5.4f,5.4f})
            {
                Box("Vehicle parapet",bridge,new Vector3(0,.55f,side),new Vector3(34,1.1f,.3f),"Stone");
                Box("Iron rail",bridge,new Vector3(0,1.25f,side),new Vector3(34,.12f,.15f),"Iron");
            }
            foreach(float support in new[]{-8f,8f}) Box("River pier",bridge,new Vector3(support,-1.6f,0),new Vector3(1.2f,3,8),"Stone");
            Route("bridge " + z,bridge,new Vector3(0,.05f,0));
        }
        internal DoorInteractable Door(Transform parent,Vector3 p,float yaw,string id)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(FirstTownAssets.Load<GameObject>("Assets/LastSignal/Prefabs/Interactions/Door.prefab"),parent);
            PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            go.name="Persistent wood door"; go.transform.localPosition=p; go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var door=go.GetComponent<DoorInteractable>(); var leaf=go.GetComponentInChildren<BoxCollider>();
            leaf.transform.localPosition=new Vector3(.58f,1.1f,0); leaf.transform.localScale=new Vector3(1.16f,2.1f,.08f);
            var renderer=leaf.GetComponent<Renderer>(); if(renderer) renderer.enabled=false;
            var pivot=leaf.transform.parent;
            var skin=art.Place("WoodDoor",pivot,new Vector3(.58f,.05f,0),0,1.12f);
            foreach(var c in skin.GetComponentsInChildren<Collider>(true)) c.enabled=false;
            var obstacle=leaf.gameObject.AddComponent<NavMeshObstacle>(); obstacle.shape=NavMeshObstacleShape.Box;
            obstacle.center=leaf.center; obstacle.size=leaf.size; obstacle.carving=true;
            Identity(go,id); Doors.Add(door); return door;
        }
        // Fixed assemblies of the existing S013 kit. No runtime/procedural building system.
        internal Transform Hall(Transform district,string id,Vector3 position,int columns=5,int rows=4,string finish="InteriorPlaster",float yaw=0)
        {
            var room=Group(id,district,position+Vector3.up*.03f); room.localRotation=Quaternion.Euler(0,yaw,0);
            float w=columns*2,d=rows*2;
            Box("Level interior foundation",room,new Vector3(0,-.08f,0),new Vector3(w,.16f,d),"Stone");
            for(int i=0;i<columns;i++)
            {
                bool opening=i==columns/2;
                var front=FirstTownAssets.Module(opening?"Doorway_2m":i%2==0?"Window_2m":"Wall_2m",room,new Vector3(-w/2+i*2,0,-d/2));
                var back=FirstTownAssets.Module(opening?"Doorway_2m":"Wall_2m",room,new Vector3(w/2-i*2,0,d/2),180);
                TintWalls(front,finish); TintWalls(back,finish);
            }
            for(int i=0;i<rows;i++)
            {
                TintWalls(FirstTownAssets.Module(i%2==0?"Window_2m":"Wall_2m",room,new Vector3(-w/2,0,d/2-i*2),90),finish);
                TintWalls(FirstTownAssets.Module(i%2==0?"Window_2m":"Wall_2m",room,new Vector3(w/2,0,-d/2+i*2),-90),finish);
            }
            var roof=Box("Rain roof",room,new Vector3(0,3.3f,0),new Vector3(w+.6f,.2f,d+.6f),"RoofMetal"); roof.AddComponent<RainRoof>();
            Box("Roof fascia",room,new Vector3(0,3.55f,-d/2),new Vector3(w+.6f,.4f,.2f),"PaintedTimber");
            Door(room,new Vector3(-.58f,.01f,-d/2-.03f),0,"town."+id+".front");
            Door(room,new Vector3(.58f,.01f,d/2+.03f),180,"town."+id+".rear");
            Sign(room,id.ToUpperInvariant().Replace('-',' '),new Vector3(0,2.8f,-d/2-.18f),0,Mathf.Min(w-1,7));
            Route(id+" entry",room,new Vector3(0,.05f,-d/2-2)); Route(id+" interior",room,new Vector3(0,.05f,0));
            Route(id+" retreat",room,new Vector3(0,.05f,d/2+2));
            return room;
        }
        void TintWalls(GameObject module,string finish)
        {
            foreach(var renderer in module.GetComponentsInChildren<Renderer>())
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++) if(materials[i] && (materials[i].name=="InteriorPlaster" || materials[i].name=="PaintedTimber")) materials[i]=art.Mat(finish);
                renderer.sharedMaterials=materials;
            }
        }
        internal LootSpawnPoint Loot(Transform parent,Vector3 ground,string id,string itemId,int quantity=1,bool bench=true)
        {
            var item=FirstTownAssets.Load<ItemDefinition>("Assets/LastSignal/Data/Items/Definitions/"+itemId+".asset");
            var collider=item.WorldPrefab ? item.WorldPrefab.GetComponent<BoxCollider>() : null;
            if(!collider)throw new InvalidOperationException("Item has no existing world loot collider: "+itemId);
            string key=itemId+"-"+quantity;
            if(!profiles.TryGetValue(key,out var profile))
            {
                profile=ScriptableObject.CreateInstance<LootProfile>(); profile.name=key;
                var so=new SerializedObject(profile); so.FindProperty("emptyBasisPoints").intValue=0;
                var entries=so.FindProperty("entries"); entries.arraySize=1; var e=entries.GetArrayElementAtIndex(0);
                e.FindPropertyRelative("item").objectReferenceValue=item; e.FindPropertyRelative("weight").intValue=1;
                e.FindPropertyRelative("minQuantity").intValue=quantity; e.FindPropertyRelative("maxQuantity").intValue=quantity;
                so.ApplyModifiedPropertiesWithoutUndo(); art.Save(profile,FirstTownAuthoring.Data+key+".asset"); profiles.Add(key,profile);
            }
            float support=bench?.8f:0;
            if(bench) Box("Loot counter",parent,ground+Vector3.up*.4f,new Vector3(1.8f,.8f,1.0f),"Wood");
            var half=collider.size*.5f;
            var point=Group("Loot - "+id,parent,ground+Vector3.up*(support+half.y)).gameObject.AddComponent<LootSpawnPoint>();
            FirstTownAuthoring.Set(point,"stableId","town."+id); FirstTownAuthoring.Set(point,"profile",profile);
            FirstTownAuthoring.Set(point,"clearance",half+new Vector3(.015f,0,.015f));
            return point;
        }
        internal void Encounter(Transform parent,string id,Vector3 position,string archetype="Shambler")
        {
            var spawn=Group("Encounter - "+id,parent,position); var encounter=spawn.gameObject.AddComponent<ZombieEncounter>();
            var prefab=art.Zombie(archetype);
            encounter.Configure(prefab,spawn); Identity(spawn.gameObject,"town.encounter."+id); Encounters.Add(encounter);
            Route(id+" encounter",parent,position);
        }
        internal Transform District(string name,Vector3 position)
        { var root=Group(name,town,position); Route(name,root,new Vector3(0,.05f,-12)); return root; }
        internal void TownCenter()
        {
            var root=District("03 Town Center",new Vector3(0,0,30));
            for(int i=0;i<3;i++)
            {
                // Preserve the native S019 layout and its encounter/loot contracts; rekey only new instances.
                var store=S019.PoiAuthoring.CreateNew(FirstTownAssets.Load<GameObject>(S019.PoiAuthoring.TemplatePath),town.gameObject.scene);
                PrefabUtility.UnpackPrefabInstance(store,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                store.transform.SetParent(root,false); store.transform.localPosition=new Vector3(-25,.03f,-8+i*18); store.name="Rural store " + i;
                foreach(var surface in store.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>(true)) Object.DestroyImmediate(surface);
                var layout=store.GetComponent<PoiAuthoringLayout>();
                foreach(var point in layout.lootAnchors) FirstTownAuthoring.Set(point,"stableId","town.store."+i+"."+point.name);
                Identity(layout.encounter.gameObject,"town.store.encounter."+i); Encounters.Add(layout.encounter);
                foreach(var door in store.GetComponentsInChildren<DoorInteractable>()) { Identity(door.gameObject,"town.store.door."+i+"."+Doors.Count); Doors.Add(door); }
                Routes.Add(layout.entry); Routes.Add(layout.retreat);
                layout.productionNotes="FirstTown: existing S019 rural store, resident ownership, no portals; native bake and QA pending.";
                Sign(store.transform,new[]{"GENERAL STORE","PANTRY","REPAIR SHOP"}[i],new Vector3(2,2.9f,-.2f));
            }
            var diner=Hall(root,"diner",new Vector3(28,0,-3),7,5);
            Loot(diner,new Vector3(-5,0,-2),"diner.food","food.canned",2); Loot(diner,new Vector3(-5,0,2),"diner.water","drink.water",2);
            for(int i=0;i<3;i++) { Box("Diner booth",diner,new Vector3(4,.5f,-3+i*2),new Vector3(2,1,.6f),"Cloth"); }
            art.Place("Awning",diner,new Vector3(0,2.6f,-5.4f),0,6);
            var hall=Hall(root,"town-hall",new Vector3(30,0,36),7,5);
            Box("Civic clock tower",hall,new Vector3(0,5,0),new Vector3(3,4,3),"Stone");
            Sign(hall,"LAST SIGNAL\nTOWN HALL",new Vector3(0,6.5f,-1.6f));
            Loot(hall,new Vector3(-5,0,1),"hall.cloth","material.cloth",3);
            Encounter(root,"market",new Vector3(18,.05f,18));
        }
        internal void HeroDistricts()
        {
            var gas=District("04 Gas Station",new Vector3(65,0,-82));
            Box("Drive-through apron",gas,new Vector3(5,-.07f,0),new Vector3(48,.16f,46),"Asphalt");
            art.Place("Canopy",gas,new Vector3(7,0,-1),0,22);
            foreach(float x in new[]{1f,13f}) art.Place("Pump",gas,new Vector3(x,0,-1),0,1.2f);
            art.Place("GasSign",gas,new Vector3(-23,0,-16),0,4);
            art.Place("GasOffice",gas,new Vector3(17,0,17),180,10);
            art.Place("Cistern",gas,new Vector3(-24,0,14),90,5);
            var service=Hall(gas,"service-shop",new Vector3(5,0,27));
            Loot(service,new Vector3(-3,0,1),"gas.fuel","fuel.generator",2);
            Loot(service,new Vector3(3,0,1),"gas.scrap","material.scrap",4);
            Loot(gas,new Vector3(16,.01f,-14),"gas.wrench","tool.wrench");
            art.Place("Workbench",service,new Vector3(3,0,-2),0,2);
            Encounter(gas,"gas",new Vector3(-18,.05f,7));
            Route("gas forecourt",gas,new Vector3(7,.05f,-12));

            var industrial=District("05 Industrial Yard",new Vector3(168,0,-65));
            Box("Freight apron",industrial,new Vector3(0,-.08f,0),new Vector3(65,.18f,85),"Stone");
            art.Place("Warehouse",industrial,new Vector3(8,0,15),0,28);
            art.Place("Garage",industrial,new Vector3(-19,0,25),180,15);
            foreach(float x in new[]{-21f,18f}) foreach(float z in new[]{-25f,-9f}) art.Place("Container",industrial,new Vector3(x,0,z),90,11);
            art.Place("OpenContainer",industrial,new Vector3(0,0,-28),0,10);
            var office=Hall(industrial,"dispatch",new Vector3(-19,0,-44));
            Loot(office,new Vector3(-3,0,1),"dispatch.scrap","material.scrap",5);
            Loot(office,new Vector3(3,0,1),"dispatch.bandage","medical.bandage",2);
            art.Place("ToolStand",industrial,new Vector3(27,0,-35),0,2);
            art.Place("GarageRack",industrial,new Vector3(25,0,8),90,3);
            Loot(industrial,new Vector3(-29,.01f,-7),"yard.tools","tool.wrench");
            Loot(industrial,new Vector3(28,.01f,-18),"yard.fuel","fuel.generator",2);
            art.Place("Pallet",industrial,new Vector3(28,0,-27),0,3);
            art.Place("WaterTank",industrial,new Vector3(30,0,29),0,5);
            Encounter(industrial,"yard.runner",new Vector3(0,.05f,-12),"Runner");
            Encounter(industrial,"yard.lurker",new Vector3(-28,.05f,5),"Lurker");

            var residential=District("06 Residential",new Vector3(125,0,115));
            for(int i=0;i<6;i++)
            {
                float x=new[]{-65f,20f,65f}[i%3],z=(i/3)*72-19;
                // European street-front silhouettes plus a clearly signed playable ground-floor annex.
                art.Place("EuropeanHouse",residential,new Vector3(x+9,0,z+11),180,15);
                var house=Hall(residential,"residence-"+(i+1),new Vector3(x-8,0,z),5,4,i%2==0?"InteriorPlaster":"PaintedTimber");
                art.Place("Cornice",house,new Vector3(0,3.1f,-4.2f),0,10);
                Loot(house,new Vector3(-3,0,1),"home."+i,i%2==0?"food.crackers":"drink.soda",2);
                art.Place("Fence",residential,new Vector3(x-14,0,z+10),90,12);
                art.Place("Bush",residential,new Vector3(x-12,0,z-6),0,2);
            }
            Encounter(residential,"residential",new Vector3(-30,.05f,12));
            var clinic=District("07 Clinic Pharmacy",new Vector3(-55,0,-42));
            var clinicRoom=Hall(clinic,"clinic-pharmacy",Vector3.zero,7,6,"Clinic");
            art.Place("Shelf",clinicRoom,new Vector3(-5,0,3),0,3); art.Place("Medicine",clinicRoom,new Vector3(-5,.8f,-2),0,1.2f);
            Box("Medical display counter",clinicRoom,new Vector3(-5,.4f,-2),new Vector3(1.8f,.8f,1),"Stone");
            Loot(clinicRoom,new Vector3(-4,0,0),"clinic.bandage","medical.bandage",3);
            Loot(clinicRoom,new Vector3(4,0,0),"clinic.dressing","medical.pressure-dressing",2);
            Box("Medical cross vertical",clinicRoom,new Vector3(0,4.2f,-6.1f),new Vector3(.4f,1.5f,.1f),"Clinic",false);
            Box("Medical cross horizontal",clinicRoom,new Vector3(0,4.2f,-6.1f),new Vector3(1.4f,.4f,.1f),"Clinic",false);
            Encounter(clinic,"clinic",new Vector3(11,.05f,7),"Lurker");

            var police=District("08 Police Checkpoint",new Vector3(0,0,-140));
            // Staggered barricades keep an 8 m continuous vehicle passage on the main axis.
            art.Place("Barrier",police,new Vector3(-10,0,-7),0,6); art.Place("Barrier",police,new Vector3(10,0,7),0,6);
            var checkpoint=Hall(police,"checkpoint",new Vector3(22,0,0));
            Loot(checkpoint,new Vector3(-3,0,1),"police.ammo","ammo.9mm",12);
            Loot(checkpoint,new Vector3(3,0,1),"police.rag","medical.rag",2);
            art.Place("Wreck",police,new Vector3(-23,0,12),25,5);
            Sign(police,"CHECKPOINT\nKEEP ACCESS CLEAR",new Vector3(-9,3,-12),0,6);
            Encounter(police,"checkpoint",new Vector3(18,.05f,-10));

            var motel=District("09 Motel Roadside",new Vector3(80,0,-189));
            Box("Parking forecourt",motel,new Vector3(0,-.07f,0),new Vector3(56,.16f,20),"Asphalt");
            for(int i=0;i<4;i++)
            {
                var room=Hall(motel,"motel-room-"+(i+1),new Vector3(-21+i*14,0,18),5,4);
                Loot(room,new Vector3(-3,0,1),"motel."+i,i%2==0?"drink.water":"food.canned");
                Box("Parking stripe",motel,new Vector3(-24+i*14,.008f,-2),new Vector3(.10f,.016f,6),"Stripe",false);
            }
            art.Place("Wreck",motel,new Vector3(-17,0,-1),0,5);
            Sign(motel,"MOTEL\nVACANCY",new Vector3(-29,4,-3),0,5);
            Encounter(motel,"motel",new Vector3(23,.05f,1));

            var school=District("10 School",new Vector3(-45,0,139));
            var main=Hall(school,"school",Vector3.zero,9,6,"School");
            var gym=Hall(school,"gym",new Vector3(5,0,24),7,5,"School");
            Box("School court",school,new Vector3(0,-.03f,-25),new Vector3(40,.08f,22),"Stone");
            for(int i=0;i<4;i++) { Box("Class desk",main,new Vector3(-5,.35f,-3+i*2),new Vector3(2,.7f,1),"Wood"); Box("Class desk",main,new Vector3(5,.35f,-3+i*2),new Vector3(2,.7f,1),"Wood"); }
            foreach(float x in new[]{-8f,8f}) { Box("Play frame post",school,new Vector3(x,1.6f,-27),new Vector3(.2f,3.2f,.2f),"Iron"); }
            Box("Play frame crossbar",school,new Vector3(0,3.2f,-27),new Vector3(16,.2f,.2f),"Iron");
            Loot(main,new Vector3(0,0,3),"school.supplies","material.cloth",3); Loot(gym,new Vector3(-5,0,1),"gym.water","drink.water",2);
            Encounter(school,"school",new Vector3(14,.05f,0),"Runner");

            var farm=District("11 Farmland Edge",new Vector3(180,0,205));
            art.Place("Barn",farm,new Vector3(10,0,11),0,18); art.Place("Silo",farm,new Vector3(35,0,10),0,6);
            art.Place("Shed",farm,new Vector3(-28,0,13),0,10);
            for(int i=0;i<7;i++) Box("Cultivated row",farm,new Vector3(32+i*4,.025f,-11),new Vector3(1.8f,.05f,18),"GroundSoil",false);
            art.Place("Hay",farm,new Vector3(-20,0,-18),0,2.2f); Loot(farm,new Vector3(-28,0,5),"farm.food","food.canned",2);
            Encounter(farm,"farm",new Vector3(28,.05f,-6));
        }
        internal void Safehouse(Transform safe)
        {
            var compound=Group("02 Safehouse Compound",town,new Vector3(-180,0,-170));
            for(int i=0;i<6;i++)
            {
                art.Place("Fence",compound,new Vector3(-30+i*12,0,-30),0,12);
                if(i!=2 && i!=3)art.Place("Fence",compound,new Vector3(-30+i*12,0,30),0,12);
                art.Place("Fence",compound,new Vector3(-36,0,-24+i*10),90,10);
                art.Place("Fence",compound,new Vector3(36,0,-24+i*10),90,10);
            }
            Box("Vehicle parking",compound,new Vector3(20,-.07f,14),new Vector3(23,.14f,22),"GroundPath");
            art.Place("Workbench",compound,new Vector3(11,0,-8),90,2);
            art.Place("Shed",compound,new Vector3(-19,0,10),0,9);
            Sign(compound,"FIELD STATION\nSTORAGE / WORKSHOP",new Vector3(-7,2.8f,28),180,6);
            Loot(compound,new Vector3(10,0,-11),"starter.scrap","material.scrap",8);
            Loot(compound,new Vector3(10,0,-15),"starter.food","food.canned",2);
            Loot(compound,new Vector3(10,0,-19),"starter.water","drink.water",2);
            Loot(compound,new Vector3(10,0,-23),"starter.fuel","fuel.generator",2);
            Route("safehouse gate",compound,new Vector3(0,.05f,32));
            Route("safehouse porch",compound,new Vector3(5,.05f,1));
            // Remove the old static art leaf; the existing door interaction now owns this doorway.
            foreach(var t in safe.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Door_1_2m").ToArray()) Object.DestroyImmediate(t.gameObject);
            Door(safe,new Vector3(2.98f,.01f,.42f),-90,"town.safehouse.front");
            var roof=Box("Cabin rain collision",safe,new Vector3(0,3.25f,0),new Vector3(6,.12f,6),"RoofMetal");
            roof.GetComponent<Renderer>().enabled=false; roof.AddComponent<RainRoof>();
        }
        internal void ForestAndLighting()
        {
            var forest=District("12 Forest Edge",new Vector3(-185,0,130));
            var lookout=Hall(forest,"ranger-hut",new Vector3(-12,0,0),5,4,"PaintedTimber");
            Loot(lookout,new Vector3(-3,0,1),"ranger.bandage","medical.bandage");
            Road(forest,"Forest trail",new Vector2(-12,-48),new Vector2(-12,0),3,false);
            // Deterministic clumps, with explicit road/compound/river exclusions; no per-frame scatter.
            var rng=new System.Random(241015); var trees=Group("Authored forest clumps",town);
            for(int i=0;i<420;i++)
            {
                float x=-228+(float)rng.NextDouble()*450,z=-224+(float)rng.NextDouble()*448;
                bool belt=x < -145 || z>217;
                if(!belt || Mathf.Abs(z-80)<14 || Mathf.Abs(z+140)<14 || (x>-222 && x<-138 && z>-212 && z<-126) ||
                    (Mathf.Abs(x+197)<20 && z>72 && z<145) || Mathf.Abs(x-RiverX(z))<17)continue;
                art.Place(i%4==0?"Birch":"Pine",trees,new Vector3(x,0,z),rng.Next(360),i%4==0?4:5);
                if(i%4==0)art.Place("Bush",trees,new Vector3(x+3,0,z+2),0,2);
                if(i%9==0)art.Place("Rock",trees,new Vector3(x-3,0,z-2),0,2.5f);
            }
            for(int i=0;i<40;i++)
            {
                float z=-230+i*12; if(Mathf.Abs(z+140)<13||Mathf.Abs(z-80)<13)continue;
                art.Place("Rock",trees,new Vector3(RiverX(z)+(i%2==0?10:-10),-.5f,z),i*47,3);
            }
            foreach(float z in new[]{-120f,-20,70,150})
            {
                art.Place("LightPole",town,new Vector3(11,0,z),0,1);
                var light=Group("Street lamp",town,new Vector3(11,4.5f,z)).gameObject.AddComponent<Light>();
                light.type=LightType.Point; light.color=new Color(1,.77f,.48f); light.intensity=.8f; light.range=12; light.shadows=LightShadows.None;
            }
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=.0018f;
            RenderSettings.fogColor=new Color(.45f,.52f,.53f); RenderSettings.ambientIntensity=.8f;
            var cameras=Group("99 Manual review cameras",town);
            ReviewCamera(cameras,"Overview",new Vector3(-330,360,-390),new Vector3(0,0,0));
            ReviewCamera(cameras,"Main street",new Vector3(-8,2,-132),new Vector3(0,2,70));
            ReviewCamera(cameras,"Safehouse approach",new Vector3(-156,4,-143),new Vector3(-180,1,-170));
            ReviewCamera(cameras,"Industrial approach",new Vector3(120,3,-120),new Vector3(170,2,-60));
        }
        static void ReviewCamera(Transform parent,string name,Vector3 position,Vector3 target)
        {
            var camera=Group(name,parent,position).gameObject.AddComponent<Camera>(); camera.transform.LookAt(target);
            camera.fieldOfView=55; camera.nearClipPlane=.1f; camera.farClipPlane=1100; camera.enabled=false;
        }
    }
}
