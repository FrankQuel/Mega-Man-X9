using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MegaManX9.Animation;
using MegaManX9.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MegaManX9.EditorTools
{
    /// <summary>Authors the rainy playable route. Does not enter Play Mode or run gameplay tests.</summary>
    public static class RainLevelBuilder
    {
        public const string ScenePath = "Assets/Recursos/Scenes/Levels/RainyFoundry.unity";
        private const string ArtFolder = "Assets/Recursos/Sprites/Environment/RainyFoundry";
        private const string AtlasPath = "Assets/Recursos/Sprites/Environment/escenario jugable.png";
        private const string RainPath = "Assets/Recursos/Sprites/Effects/Rain/Generated/Fall.png";
        private const string AxlPrefabPath="Assets/Recursos/Prefabs/Bosses/Axl/Axl_Animated.prefab";
        private const string BuildingsSource="Assets/Recursos/Sprites/Environment/edificios.png";
        private const string SpikesSource="Assets/Recursos/Sprites/Environment/pinchos.png";
        private const string ExtrasSource="Assets/Recursos/Sprites/Environment/postes, pinchos y edificios extra.png";
        private const string NativeArtFolder=ArtFolder+"/NativeEnvironment";
        private static Sprite city, rail, pixel;
        private static readonly Dictionary<string,Sprite> nativeArt=new Dictionary<string,Sprite>(StringComparer.Ordinal);
        private static Material material;
        private static Sprite[] doorUnlock,doorOpening,doorUpper,doorLower;

        [Serializable] private sealed class Surface
        {
            public string name;
            public float left, right, top, depth;
            public Surface(string name, float left, float right, float top, float depth)
            { this.name=name;this.left=left;this.right=right;this.top=top;this.depth=depth; }
        }

        private static readonly Surface[] Surfaces =
        {
            new Surface("Azotea inicial",-2,26,0,1.8f),
            new Surface("Azotea y acceso vertical",28,58,0,1.8f),
            new Surface("Peldaño de recuperacion",58.9f,61.8f,.7f,.45f),
            new Surface("Recuperacion bajo pasarelas",63,66,1.2f,1.0f),
            new Surface("Isla entre tuberias",68,73,2.6f,.65f),
            new Surface("Descanso de mantenimiento",76,83,0,1.8f),
            new Surface("Fosa de condensadores",86,95,-1.2f,1.8f),
            new Surface("Drenaje bajo",99,112,-2,1.8f),
            new Surface("Entrada a fundicion",115,139,-2,1.8f),
            new Surface("Maquinas de fundicion",143,155,-2,1.8f),
            new Surface("Descanso de hornos",159,169,-2,1.8f),
            new Surface("Base de la torre",169,181,0,1.8f),
            new Surface("Escalon inferior torre",181,187,1.4f,1.2f),
            new Surface("Escalon central torre",190,197,2.8f,1.2f),
            new Surface("Patio del reactor",201.5f,212,0,1.8f),
            new Surface("Reactor, esclusa y arena",212,265,0,1.8f),
            new Surface("Caja de aproximacion",9.5f,11,.8f,.8f),
            new Surface("Peldaño de aprendizaje",12,13.2f,1.1f,.4f),
            new Surface("Plataforma de salto",14,17.5f,1.8f,.5f),
            new Surface("Pasarela de drenaje",22.5f,24.2f,1.5f,.4f),
            new Surface("Muro de subida",48.7f,49.6f,4.6f,4.6f),
            new Surface("Descanso izquierdo escalera",43.5f,45.3f,4.8f,.45f),
            new Surface("Desembarco escalera oeste",45.3f,46.8f,4.8f,.45f),
            new Surface("Pasarela elevada",46.8f,57,4.8f,.45f),
            new Surface("Techo de tuberias",58.5f,64.6f,6.2f,.5f),
            new Surface("Pasarela alta de antenas",65.8f,70,7.6f,.5f),
            new Surface("Descenso de antenas",71.8f,75.6f,6.1f,.5f),
            new Surface("Descanso de escalera central",77,82,3,.5f),
            new Surface("Ascenso a condensadores",83.2f,89,4.4f,.45f),
            new Surface("Muro de condensadores",94,95,4.2f,5.4f),
            new Surface("Cumbre de condensadores",90.2f,97,5.8f,.45f),
            new Surface("Pasarela de bajada uno",99,104,4.5f,.45f),
            new Surface("Pasarela de bajada dos",106,111,2.6f,.45f),
            new Surface("Pasarela de bajada tres",113,117.5f,.8f,.45f),
            new Surface("Plataforma de horno uno",126,132,0,.45f),
            new Surface("Plataforma de horno dos",133.2f,139,1.4f,.45f),
            new Surface("Plataforma de horno tres",140.2f,146,2.8f,.45f),
            new Surface("Puente sobre la maquina",147.2f,154,4.2f,.45f),
            new Surface("Barrera de fundicion",153,154,3.5f,5.5f),
            new Surface("Descenso del horno",157,162,2.8f,.45f),
            new Surface("Salida de fundicion",164,169,0,.45f),
            new Surface("Descanso de torre",170.5f,176,4.8f,.5f),
            new Surface("Muro de torre",176.2f,177.2f,6,6),
            new Surface("Torre primer nivel",177.5f,182.5f,6.2f,.5f),
            new Surface("Torre segundo nivel",183.7f,188.8f,7.6f,.5f),
            new Surface("Torre cima",189.5f,195,9,.5f),
            new Surface("Torre descenso uno",197,201.5f,7.4f,.5f),
            new Surface("Torre descenso dos",203.2f,208,4.6f,.5f),
            new Surface("Torre descenso tres",209,214,2,.45f),
            new Surface("Escalera de acceso reactor",213.5f,218,3,.45f),
            new Surface("Muro del reactor",219,220,4.2f,4.2f),
            new Surface("Pasarela del reactor",220,225,4.4f,.45f),
            new Surface("Descenso antes de esclusa",227,231.2f,2.4f,.45f),
            new Surface("Arco superior de puerta",235.2f,237.8f,11,7),
            new Surface("Techo de arena",237.8f,261,11,.35f),
            new Surface("Muro oriental de la arena final",260,261,11,11)
        };

        [MenuItem("Mega Man X9/Playable Rain Level/Build Rainy Foundry", priority=1)]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Sal de Play antes de construir Rainy Foundry.");
            BackupDirtyScenes();
            if(string.IsNullOrEmpty(SceneManager.GetActiveScene().path)&&File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            ConfigureLayer(8,"RainTerrain");ConfigureLayer(9,"RainHero");ConfigureLayer(10,"RainEnemies");
            RainLevelAssets assets = RainLevelAssetsBuilder.Build();
            if (assets.spriteMaterial == null || assets.dragonRigPrefab == null)
                throw new InvalidDataException("Rain Level Assets necesita material y el prefab de DragonRig.");
            GameObject axlPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(AxlPrefabPath);
            if(axlPrefab==null)throw new InvalidDataException("Falta el prefab Axl original: "+AxlPrefabPath);
            material = assets.spriteMaterial;
            EnsureFolder(ArtFolder);
            city = Crop("RainCityBackdrop",new RectInt(4,1004,1200,180),false);
            rail = Crop("RainPlatformRail",new RectInt(850,180,560,86),true);
            pixel = UnitPixel();
            PrepareNativeEnvironment();
            Sprite[] rainFrames = AssetDatabase.LoadAllAssetsAtPath(RainPath).OfType<Sprite>()
                .OrderBy(s=>s.name,StringComparer.Ordinal).ToArray();
            if (rainFrames.Length == 0) throw new InvalidDataException("Faltan los fotogramas originales Rain/Fall.");
            doorUnlock=DoorFrames("Unlock");doorOpening=DoorFrames("Open");
            doorUpper=DoorFrames("RetractTop");doorLower=DoorFrames("RetractBottom");

            Scene existing = SceneManager.GetSceneByPath(ScenePath);
            Scene previous = SceneManager.GetActiveScene();
            bool replacingActive = existing.IsValid() && previous == existing;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            bool saved = false;
            try
            {
                SceneManager.SetActiveScene(scene);
                if (existing.IsValid() && existing.isLoaded) EditorSceneManager.CloseScene(existing,true);
                var environment = new GameObject("Rainy Foundry - World").transform;
                Transform far = Background(environment,"City at the horizon only",1.4f,new Color(.24f,.34f,.43f),-100);
                AddBackdropStructures(environment);
                var terrain = new GameObject("Solid terrain - layer 8").transform;terrain.SetParent(environment,false);
                foreach (Surface surface in Surfaces) Solid(terrain,surface);
                Solid(terrain,new Surface("Limite oeste",-2.8f,-2,14,18));
                Solid(terrain,new Surface("Limite este",265,265.8f,14,18));
                Ladder(environment,46,0,4.8f);
                Ladder(environment,79,0,3);
                Ladder(environment,87,-1.2f,4.4f);
                Ladder(environment,129,-2,0);
                Ladder(environment,166.5f,-2,0);
                Ladder(environment,173,0,4.8f);
                Ladder(environment,192,2.8f,9);
                Ladder(environment,216,0,3);
                var checkpoints = new[]
                {
                    Checkpoint(environment,38,"Pasarelas de mantenimiento"),
                    Checkpoint(environment,78,"Antenas y condensadores"),
                    Checkpoint(environment,122,"Fundicion profunda",-2),
                    Checkpoint(environment,164,"Base de la torre",-2),
                    Checkpoint(environment,205,"Patio del reactor"),
                    Checkpoint(environment,232,"Esclusa del dragon")
                };
                var hazards = new[]
                {
                    Electricity(environment,91.1f,93.2f,0,-1.2f),
                    Electricity(environment,130,133,1.6f,-2),
                    Electricity(environment,149,152,.8f,-2),
                    Electricity(environment,216.5f,218.3f,2.1f)
                };
                GameObject leftGate = BossDoor(environment,out var doorFace,out var doorTop,out var doorBottom,out var doorCollider);

                var gameObject = new GameObject("Rain Level Game");
                var game = gameObject.AddComponent<RainLevelGame>();
                var heroObject = new GameObject("Hero - Blue and Red");heroObject.transform.position = new Vector3(4,0,0);
                var hero = heroObject.AddComponent<RainHeroController>();hero.Configure(assets);
                var dragonObject = (GameObject)PrefabUtility.InstantiatePrefab(assets.dragonRigPrefab,scene);
                dragonObject.name = "Magma Dragoon - first boss";dragonObject.transform.position = new Vector3(255,0,0);
                var dragon = dragonObject.GetComponent<RainDragonBoss>();
                if(dragon==null)dragon=dragonObject.AddComponent<RainDragonBoss>();
                dragon.Configure(assets);
                dragon.ConfigureBounds(RainLevelWorld.ArenaLeft,RainLevelWorld.ArenaRight);
                var axlObject=(GameObject)PrefabUtility.InstantiatePrefab(axlPrefab,scene);
                axlObject.name="Axl - Lumine final boss";axlObject.transform.position=new Vector3(256,0,0);
                var axl=axlObject.GetComponent<RainAxlBoss>();
                if(axl==null)axl=axlObject.AddComponent<RainAxlBoss>();
                axl.Configure(assets);
                axl.ConfigureBounds(RainLevelWorld.ArenaLeft,RainLevelWorld.ArenaRight);
                StageReadySequence ready = StageReadySequenceBuilder.AttachTo(gameObject);
                game.Configure(assets,hero,dragon,ready);
                game.ConfigureAxl(axl);
                var hud = gameObject.AddComponent<RainLevelHUD>();hud.Configure(assets);
                var world = environment.gameObject.AddComponent<RainLevelWorld>();
                world.Configure(game,hero,dragon,leftGate,null,checkpoints,hazards);
                world.ConfigureDoor(doorFace,doorTop,doorBottom,doorCollider,doorUnlock,doorOpening,doorUpper,doorLower);
                world.ConfigurePickups(new[]
                {
                    Reward("health-antennas",RainPickupKind.LargeHealth,69.3f,7.6f),
                    Reward("life-condensers",RainPickupKind.ExtraLife,96.1f,5.8f),
                    Reward("health-foundry",RainPickupKind.LargeHealth,152.3f,4.2f),
                    Reward("life-tower",RainPickupKind.ExtraLife,194.2f,9),
                    Reward("health-reactor",RainPickupKind.LargeHealth,224.2f,4.4f),
                    Reward("score-tutorial",RainPickupKind.ScoreOrb,16.7f,1.8f),
                    Reward("score-antennas",RainPickupKind.ScoreOrb,67.9f,7.6f),
                    Reward("score-foundry",RainPickupKind.ScoreOrb,143.9f,2.8f),
                    Reward("score-tower",RainPickupKind.ScoreOrb,185.7f,7.6f),
                    Reward("score-reactor",RainPickupKind.ScoreOrb,229.1f,2.4f)
                });

                var cameraObject = new GameObject("Rain Level Camera");cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();camera.orthographic = true;camera.orthographicSize = 3;
                camera.aspect = 16f/9f;camera.rect = new Rect(0,0,1,1);
                camera.clearFlags = CameraClearFlags.SolidColor;camera.backgroundColor = Color.black;
                camera.cullingMask = ~(1<<30);cameraObject.AddComponent<AudioListener>();
                cameraObject.AddComponent<RainLevelCamera>().Configure(hero,game,new[]{far},new[]{.18f});
                var weather = new GameObject("Original rain overlay").AddComponent<RainWeather>();
                weather.Configure(rainFrames,material,camera,game);
                SpawnEnemies(assets,environment);
                Physics2D.SyncTransforms();
                EnsureFolder("Assets/Recursos/Scenes/Levels");EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene,ScenePath)) throw new IOException("No se pudo guardar "+ScenePath);
                var buildScenes = EditorBuildSettings.scenes.ToList();
                int index = buildScenes.FindIndex(s=>s.path==ScenePath);
                if (index < 0) buildScenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true));
                else buildScenes[index] = new EditorBuildSettingsScene(ScenePath,true);
                EditorBuildSettings.scenes = buildScenes.ToArray();
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                saved = true;
                WriteLayout();
                Debug.Log("RAINY_FOUNDRY_BUILT "+ScenePath+" - 264 unidades, 8 escaleras, 6 checkpoints, 17 enemigos, Dragoon y Axl Original.");
            }
            finally
            {
                if (!saved && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene,true);
                if (!saved && replacingActive && File.Exists(ScenePath))
                    EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
                else if (!saved && previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static Transform Background(Transform parent,string name,float y,Color color,int order)
        {
            var layer = new GameObject(name).transform;layer.SetParent(parent,false);
            for (float x=-72;x<RainLevelWorld.LevelLength+80;x+=24)
                Sprite(layer,"City skyline",city,new Vector2(x+12,y),new Vector2(24,3.6f),color,order);
            return layer;
        }

        private static void AddBackdropStructures(Transform parent)
        {
            string[] buildings={"BlueGarage","BlueFactory","TwinTanks","CommunicationsTower","IndustrialFactory","BlueTower","ExtraGarage","ExtraBannerBuilding","ExtraSlimTower","ExtraWideBuilding","ExtraTallBuilding"};
            int index=0;
            for(float x=6;x<RainLevelWorld.LevelLength;x+=12.5f)
            {
                string name=buildings[index%buildings.Length];
                float height=x>168&&x<211?10.8f:5.4f+(index%3)*.7f;
                PlaceNative(parent,"Original building - "+name,nativeArt[name],new Vector2(x,-3.6f),height,new Color(.48f,.59f,.70f),-32);
                index++;
            }
            for(float x=128;x<169;x+=13)
            {
                string name=x<142?"TwinTanks":"IndustrialFactory";
                PlaceNative(parent,"Original foundry machinery - "+name,nativeArt[name],new Vector2(x,-4.6f),5.5f,new Color(.60f,.69f,.78f),-22);
            }
            foreach(float x in new[]{12f,41f,83f,109f,151f,177f,207f,225f})
                PlaceNative(parent,"Original street lamp",nativeArt[x<100?"LampTall":"LampShort"],new Vector2(x,DecorationFloor(x)),2.1f,Color.white,12);
        }

        private static float DecorationFloor(float x)
        {
            float floor=float.PositiveInfinity;
            foreach(var surface in Surfaces)if(x>surface.left&&x<surface.right)floor=Mathf.Min(floor,surface.top);
            return float.IsPositiveInfinity(floor)?0:floor;
        }
        private static SpriteRenderer PlaceNative(Transform parent,string name,Sprite sprite,Vector2 bottom,float height,Color tint,int order)
        {
            float width=height*sprite.rect.width/sprite.rect.height;
            return Sprite(parent,name,sprite,bottom+Vector2.up*(height*.5f),new Vector2(width,height),tint,order);
        }

        private static GameObject Solid(Transform parent,Surface surface)
        {
            var root = new GameObject(surface.name);root.transform.SetParent(parent,false);root.layer = RainLevelWorld.TerrainLayer;
            root.transform.position = new Vector3((surface.left+surface.right)/2,surface.top-surface.depth/2,0);
            var collider = root.AddComponent<BoxCollider2D>();collider.size = new Vector2(surface.right-surface.left,surface.depth);
            Sprite(root.transform,"Steel body",pixel,root.transform.position,collider.size,new Color(.075f,.15f,.20f),3);
            if(surface.depth>=1.5f)
            {
                // Extend the painted foundation beyond the widest camera view without changing collisions or pits.
                float baseY=surface.top-surface.depth,coverBottom=-32;
                if(baseY>coverBottom)Sprite(root.transform,"Foundation cover",pixel,
                    new Vector2(root.transform.position.x,(baseY+coverBottom)/2),
                    new Vector2(surface.right-surface.left,baseY-coverBottom),new Color(.075f,.15f,.20f),3);
            }
            float width = surface.right-surface.left;
            for (float left=surface.left;left<surface.right;left+=2.8f)
            {
                float tileWidth = Mathf.Min(2.8f,surface.right-left);
                Sprite(root.transform,"Original atlas rail",rail,new Vector2(left+tileWidth/2,surface.top-.22f),new Vector2(tileWidth,.44f),new Color(.52f,.70f,.78f),8);
            }
            Sprite(root.transform,"Readable contact edge",pixel,new Vector2((surface.left+surface.right)/2,surface.top-.02f),new Vector2(width,.04f),new Color(.25f,.64f,.70f),9);
            return root;
        }

        private static void Ladder(Transform parent,float x,float bottom,float top)
        {
            var root = new GameObject("Ladder "+x.ToString("0.0"));root.transform.SetParent(parent,false);
            root.transform.position = new Vector3(x,(bottom+top)/2,0);
            root.AddComponent<BoxCollider2D>();root.AddComponent<RainLadder>().Configure(top-bottom);
            foreach (float side in new[]{-.30f,.30f})
                Sprite(root.transform,"Ladder rail",pixel,new Vector2(x+side,(bottom+top)/2),new Vector2(.07f,top-bottom),new Color(.4f,.7f,.78f),11);
            for (float y=bottom+.15f;y<top;y+=.28f)
                Sprite(root.transform,"Ladder rung",pixel,new Vector2(x,y),new Vector2(.62f,.055f),new Color(.33f,.58f,.67f),10);
        }

        private static RainLevelWorld.CheckpointZone Checkpoint(Transform parent,float x,string label,float floor=0)
        {
            PlaceNative(parent,"Checkpoint original post - "+label,nativeArt["BannerPole"],new Vector2(x,floor),2.1f,Color.white,13);
            var beacon = Sprite(parent,"Checkpoint beacon - "+label,pixel,new Vector2(x,floor+1.8f),new Vector2(.21f,.3f),new Color(.12f,.75f,1),14);
            return new RainLevelWorld.CheckpointZone{label=label,spawn=new Vector2(x,floor),area=new Bounds(new Vector3(x,floor+6,0),new Vector3(1.8f,16,2)),beacon=beacon};
        }

        private static RainLevelWorld.ElectricZone Electricity(Transform parent,float left,float right,float phase,float floor=0)
        {
            var lights = new List<SpriteRenderer>();
            string key=left<100?"SpikeLong":left<140?"SpikeCompactSix":left<200?"SpikeCompactFour":"SpikeTriple";
            Sprite spike=nativeArt[key];float aspect=spike.rect.width/spike.rect.height;
            int count=Mathf.CeilToInt((right-left)/(.48f*aspect));float width=(right-left)/count,height=width/aspect;
            for(int i=0;i<count;i++)
            {
                Vector2 center=new Vector2(left+(i+.5f)*width,floor+height*.5f);
                Sprite(parent,"Original energized spikes - "+key,spike,center,new Vector2(width,height),Color.white,14);
                lights.Add(Sprite(parent,"Spike energy pulse",spike,center,new Vector2(width,height),new Color(.12f,.88f,1,.18f),15));
            }
            return new RainLevelWorld.ElectricZone{area=new Bounds(new Vector3((left+right)/2,floor+.23f,0),new Vector3(right-left,.55f,2)),lights=lights.ToArray(),phaseOffset=phase,damage=4};
        }

        private static RainLevelWorld.PickupPlacement Reward(string id,RainPickupKind kind,float x,float floor)
            =>new RainLevelWorld.PickupPlacement{id="rainy-foundry/"+id,kind=kind,position=new Vector2(x,floor)};

        private static Sprite[] DoorFrames(string clip)
        {
            string path="Assets/Recursos/Sprites/Environment/BossDoor/Generated/"+clip+".png";
            var frames=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name,StringComparer.Ordinal).ToArray();
            if(frames.Length==0)throw new InvalidDataException("Faltan los sprites originales BossDoor/"+clip);
            return frames;
        }

        private static GameObject BossDoor(Transform parent,out SpriteRenderer face,out SpriteRenderer top,out SpriteRenderer bottom,out BoxCollider2D solid)
        {
            var root=new GameObject("Original animated boss door");root.transform.SetParent(parent,false);
            root.transform.position=new Vector3(RainLevelWorld.BossDoorX,0,0);root.layer=8;
            solid=root.AddComponent<BoxCollider2D>();solid.size=new Vector2(1.55f,4);solid.offset=new Vector2(0,2);
            face=DoorArt(root.transform,"Door face",doorUnlock[0],Vector2.zero);
            top=DoorArt(root.transform,"Door upper half",doorUpper[0],new Vector2(0,2.375f));top.enabled=false;
            bottom=DoorArt(root.transform,"Door lower half",doorLower[0],Vector2.zero);bottom.enabled=false;
            foreach(float x in new[]{RainLevelWorld.BossDoorX-1.02f,RainLevelWorld.BossDoorX+1.02f})
                Sprite(parent,"Boss doorway frame",pixel,new Vector2(x,2),new Vector2(.22f,4),new Color(.16f,.24f,.3f),21);
            return root;
        }

        private static SpriteRenderer DoorArt(Transform parent,string name,Sprite frame,Vector2 localPosition)
        {
            var root=new GameObject(name);root.transform.SetParent(parent,false);root.transform.localPosition=localPosition;
            root.transform.localScale=new Vector3(6.25f,6.25f,1);
            var art=root.AddComponent<SpriteRenderer>();art.sprite=frame;art.sharedMaterial=material;art.sortingOrder=23;return art;
        }

        private static void SpawnEnemies(RainLevelAssets assets,Transform parent)
        {
            Enemy(assets,parent,"Burnout",RainEnemy.Kind.Walker,19,0,18.5f,24.5f);
            Enemy(assets,parent,"GrenadeMan",RainEnemy.Kind.Gunner,35,0,32,38.5f);
            Enemy(assets,parent,"Burnout",RainEnemy.Kind.Walker,54,4.8f,50.5f,56);
            Enemy(assets,parent,"GrenadeManGreen",RainEnemy.Kind.Gunner,61,6.2f,59,63.5f);
            Enemy(assets,parent,"BirdGreen",RainEnemy.Kind.Flyer,75,7.8f,72,79);
            Enemy(assets,parent,"GrenadeMan",RainEnemy.Kind.Gunner,95,5.8f,92,96.4f);
            Enemy(assets,parent,"BirdBlue",RainEnemy.Kind.Flyer,106,6.1f,102,111);
            Enemy(assets,parent,"Burnout",RainEnemy.Kind.Walker,119,-2,117.8f,123.5f);
            Enemy(assets,parent,"GrenadeManGreen",RainEnemy.Kind.Gunner,136,1.4f,134.5f,138.4f);
            Enemy(assets,parent,"GigaDeath",RainEnemy.Kind.Turret,151,4.2f,150,152);
            Enemy(assets,parent,"BirdGreen",RainEnemy.Kind.Flyer,160,5.1f,157,164);
            Enemy(assets,parent,"Burnout",RainEnemy.Kind.Walker,174,0,170,175);
            Enemy(assets,parent,"GrenadeManGreen",RainEnemy.Kind.Gunner,180,6.2f,178.2f,181.9f);
            Enemy(assets,parent,"BirdBlue",RainEnemy.Kind.Flyer,193,10.4f,190,197);
            Enemy(assets,parent,"Burnout",RainEnemy.Kind.Walker,204,0,202.2f,210.5f);
            Enemy(assets,parent,"GrenadeMan",RainEnemy.Kind.Gunner,223,4.4f,220.5f,224.5f);
            Enemy(assets,parent,"GigaDeath",RainEnemy.Kind.Turret,229,0,228,230);
        }

        private static void Enemy(RainLevelAssets assets,Transform parent,string unit,RainEnemy.Kind kind,float x,float y,float left,float right)
        {
            if (assets.Find(unit,"Idle") == null) throw new InvalidDataException("No hay banco de animacion para "+unit);
            var root = new GameObject(unit+" - "+kind);root.transform.SetParent(parent,false);root.transform.position = new Vector3(x,y,0);
            root.AddComponent<RainEnemy>().Configure(assets,kind,unit,left,right);
        }

        private static SpriteRenderer Sprite(Transform parent,string name,Sprite sprite,Vector2 position,Vector2 size,Color color,int order)
        {
            var root = new GameObject(name);root.transform.SetParent(parent,false);root.transform.position = new Vector3(position.x,position.y,0);
            root.transform.localScale = new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
            var renderer = root.AddComponent<SpriteRenderer>();renderer.sprite = sprite;renderer.sharedMaterial = material;renderer.color = color;renderer.sortingOrder = order;
            return renderer;
        }

        [Serializable] private sealed class NativeCropSpec
        {
            public string name,source;
            public int x,y,width,height;
            public int[] backgroundSeeds;
            public NativeCropSpec(string key,string path,int left,int top,int w,int h,params int[] seeds)
            {name=key;source=path;x=left;y=top;width=w;height=h;backgroundSeeds=seeds;}
        }
        [Serializable] private sealed class NativeEnvironmentReport
        {
            public string coordinateOrigin="top-left",outputFolder=NativeArtFolder;
            public string alphaPolicy="Edge-connected near-white flood, reviewed hole seeds, one-pixel white-matte defringe";
            public NativeCropSpec[] crops;
        }
        private static readonly NativeCropSpec[] NativeCrops=
        {
            new NativeCropSpec("BlueGarage",BuildingsSource,46,161,358,262,211,260,235,260,258,260,88,369),
            new NativeCropSpec("BlueFactory",BuildingsSource,432,62,483,361,485,350,865,350),
            new NativeCropSpec("TwinTanks",BuildingsSource,933,79,475,344,1167,190,992,320,1349,320),
            new NativeCropSpec("CommunicationsTower",BuildingsSource,18,448,347,565),
            new NativeCropSpec("IndustrialFactory",BuildingsSource,376,551,652,462,804,755,904,827,408,828,428,829,452,829,434,859,415,884,452,883,432,912,431,945,414,965,446,965,430,983),
            new NativeCropSpec("BlueTower",BuildingsSource,1044,458,301,555,1128,688,1280,850),
            new NativeCropSpec("ExtraGarage",ExtrasSource,43,281,245,255),
            new NativeCropSpec("ExtraBannerBuilding",ExtrasSource,311,248,195,288),
            new NativeCropSpec("ExtraSlimTower",ExtrasSource,527,123,194,413),
            new NativeCropSpec("ExtraWideBuilding",ExtrasSource,742,212,305,324),
            new NativeCropSpec("ExtraTallBuilding",ExtrasSource,1067,22,278,514),
            new NativeCropSpec("LampShort",ExtrasSource,66,812,101,179,108,842,84,844),
            new NativeCropSpec("LampTall",ExtrasSource,193,762,112,229,237,787),
            new NativeCropSpec("BannerPole",ExtrasSource,380,762,89,229),
            new NativeCropSpec("SpikeLong",SpikesSource,577,112,663,151),
            new NativeCropSpec("SpikeTriple",SpikesSource,240,115,297,148),
            new NativeCropSpec("SpikeCompactFour",ExtrasSource,52,628,162,64),
            new NativeCropSpec("SpikeCompactSix",ExtrasSource,237,624,191,68)
        };

        private static void PrepareNativeEnvironment()
        {
            EnsureFolder(NativeArtFolder);nativeArt.Clear();
            foreach(var spec in NativeCrops)nativeArt.Add(spec.name,CropNativeEnvironment(spec));
            Directory.CreateDirectory("Tools/WeatherPipeline");
            File.WriteAllText("Tools/WeatherPipeline/environment_crops.json",JsonUtility.ToJson(new NativeEnvironmentReport{crops=NativeCrops},true));
        }
        private static Sprite CropNativeEnvironment(NativeCropSpec spec)
        {
            if(!File.Exists(spec.source))throw new FileNotFoundException("Falta el nuevo arte original del entorno.",spec.source);
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
            var output=new Texture2D(spec.width,spec.height,TextureFormat.RGBA32,false);
            try
            {
                if(!ImageConversion.LoadImage(source,File.ReadAllBytes(spec.source),false))throw new InvalidDataException("PNG de entorno ilegible: "+spec.source);
                if(spec.x<0||spec.y<0||spec.x+spec.width>source.width||spec.y+spec.height>source.height)throw new InvalidDataException("Recorte fuera del original: "+spec.name);
                Color32[] pixels=source.GetPixels32(),crop=new Color32[spec.width*spec.height];
                for(int y=0;y<spec.height;y++)Array.Copy(pixels,(source.height-spec.y-spec.height+y)*source.width+spec.x,crop,y*spec.width,spec.width);
                ClearNativeBackground(crop,spec);
                output.SetPixels32(crop);output.Apply();
                string path=NativeArtFolder+"/"+spec.name+".png";byte[] png=output.EncodeToPNG();
                if(!File.Exists(path)||!File.ReadAllBytes(path).SequenceEqual(png))File.WriteAllBytes(path,png);
                return ImportSprite(path);
            }
            finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(output);}
        }
        private static void ClearNativeBackground(Color32[] colors,NativeCropSpec spec)
        {
            int width=spec.width,height=spec.height;var queue=new Queue<int>();
            Action<int> visit=index=>
            {
                Color32 color=colors[index];int low=Math.Min(color.r,Math.Min(color.g,color.b)),high=Math.Max(color.r,Math.Max(color.g,color.b));
                if(color.a==0||low<230||high-low>24)return;
                color.a=0;colors[index]=color;queue.Enqueue(index);
            };
            for(int x=0;x<width;x++){visit(x);visit((height-1)*width+x);}
            for(int y=0;y<height;y++){visit(y*width);visit(y*width+width-1);}
            for(int i=0;i+1<spec.backgroundSeeds.Length;i+=2)
            {
                int x=spec.backgroundSeeds[i]-spec.x,y=height-1-(spec.backgroundSeeds[i+1]-spec.y);
                if(x>=0&&x<width&&y>=0&&y<height)visit(y*width+x);
            }
            while(queue.Count>0)
            {
                int index=queue.Dequeue(),x=index%width,y=index/width;
                if(x>0)visit(index-1);if(x+1<width)visit(index+1);if(y>0)visit(index-width);if(y+1<height)visit(index+width);
            }
            // Remove the white matte only on the outer one-pixel fringe; enclosed white armor remains opaque.
            var transparent=new bool[colors.Length];for(int i=0;i<colors.Length;i++)transparent[i]=colors[i].a==0;
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                int index=y*width+x;if(transparent[index])continue;
                bool edge=false;
                for(int dy=-1;dy<=1&&!edge;dy++)for(int dx=-1;dx<=1;dx++)
                    if(x+dx>=0&&x+dx<width&&y+dy>=0&&y+dy<height&&transparent[(y+dy)*width+x+dx]){edge=true;break;}
                if(!edge)continue;
                Color32 color=colors[index];int low=Math.Min(color.r,Math.Min(color.g,color.b)),high=Math.Max(color.r,Math.Max(color.g,color.b));
                if(low<=100||high-low>=35)continue;
                float alpha=Mathf.Clamp((255-low)/223f,.02f,1);
                colors[index]=(Color32)new Color(Mathf.Clamp01((color.r/255f-(1-alpha))/alpha),Mathf.Clamp01((color.g/255f-(1-alpha))/alpha),Mathf.Clamp01((color.b/255f-(1-alpha))/alpha),color.a/255f*alpha);
            }
        }

        private static Sprite Crop(string name,RectInt rect,bool clearMagenta)
        {
            string source = File.Exists(AtlasPath) ? AtlasPath : "SourceArt/Sprites/Environment/escenario jugable.png";
            if (!File.Exists(source)) throw new FileNotFoundException("Falta el escenario original.",source);
            var original = new Texture2D(2,2,TextureFormat.RGBA32,false);
            var crop = new Texture2D(rect.width,rect.height,TextureFormat.RGBA32,false);
            try
            {
                if (!ImageConversion.LoadImage(original,File.ReadAllBytes(source),false)) throw new InvalidDataException("El atlas industrial no es legible.");
                if (rect.xMax>original.width || rect.yMax>original.height) throw new InvalidDataException("Recorte industrial fuera del atlas.");
                var colors = original.GetPixels(rect.x,original.height-rect.yMax,rect.width,rect.height);
                if (clearMagenta) for (int i=0;i<colors.Length;i++) if(colors[i].r>.95f && colors[i].g<.06f && colors[i].b>.95f) colors[i].a=0;
                crop.SetPixels(colors);crop.Apply();string path=ArtFolder+"/"+name+".png";File.WriteAllBytes(path,crop.EncodeToPNG());return ImportSprite(path);
            }
            finally { UnityEngine.Object.DestroyImmediate(original);UnityEngine.Object.DestroyImmediate(crop); }
        }

        private static Sprite UnitPixel()
        {
            string path=ArtFolder+"/RainUnitPixel.png";var image=new Texture2D(1,1,TextureFormat.RGBA32,false);
            try { image.SetPixel(0,0,Color.white);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());return ImportSprite(path); }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }

        private static Sprite ImportSprite(string path)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void ConfigureLayer(int index,string name)
        {
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layer=settings.FindProperty("layers").GetArrayElementAtIndex(index);
            if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue!=name)
                throw new InvalidOperationException("La capa "+index+" ya esta ocupada por "+layer.stringValue+".");
            layer.stringValue=name;settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BackupDirtyScenes()
        {
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                Scene scene=SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || !scene.isDirty || EditorSceneManager.IsPreviewScene(scene)) continue;
                EnsureFolder("Assets/Recursos/Scenes/Levels/WorkspaceBackups");
                string path="Assets/Recursos/Scenes/Levels/WorkspaceBackups/Before_RainyFoundry_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N").Substring(0,6)+".unity";
                if (!EditorSceneManager.SaveScene(scene,path,!string.IsNullOrEmpty(scene.path)))
                    throw new IOException("No se pudo respaldar la escena abierta.");
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }

        [Serializable] private sealed class LayoutReport
        {
            public string scene;public float length,largestGroundGap,jumpApex,dashJumpGap;
            public int ladders,checkpoints,enemies,bosses;public Surface[] solids;
        }
        private static void WriteLayout()
        {
            Directory.CreateDirectory("Tools/Gameplay");
            File.WriteAllText("Tools/Gameplay/rain_level_layout.json",JsonUtility.ToJson(new LayoutReport
            {
                scene=ScenePath,length=RainLevelWorld.LevelLength,largestGroundGap=4.5f,jumpApex=1.85f,dashJumpGap=5.18f,
                ladders=8,checkpoints=6,enemies=17,bosses=2,solids=Surfaces
            },true));
        }
    }
}
