using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MegaManX9.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;

namespace MegaManX9.EditorTools
{
    /// <summary>Builds an articulated rig from a single, consistent set of illustrated parts.</summary>
    public static class AxlRigBuilder
    {
        public const string AtlasPath = "Assets/Recursos/Sprites/Bosses/AxlRig/AxlRigParts-v2.png";
        public const string AnimationFolder = "Assets/Recursos/Animations/Bosses/AxlRig";
        public const string PrefabPath = "Assets/Recursos/Prefabs/Bosses/AxlRig/AxlRig_Animated.prefab";
        public const string ControllerPath = AnimationFolder + "/AxlRig.controller";
        private const string ReportPath = "Tools/AnimationPipeline/axl_rig_build_report.json";
        private const float BodyHeight = .48f;
        private const float FrameRate = 12f;

        private sealed class Part
        {
            public string name;
            public RectInt rect;
            public Vector2 anchor;
            public Vector2 tip;
            public float size;
            public bool sizeIsWidth;
            public Part(string name, int x, int y, int w, int h, float ax, float ay,
                float size, float tx = 0, float ty = 0, bool width = false)
            {
                this.name = name; rect = new RectInt(x, y, w, h);
                anchor = new Vector2(ax, ay); tip = new Vector2(tx, ty);
                this.size = size; sizeIsWidth = width;
            }
            public float Scale => size / (sizeIsWidth ? rect.width : rect.height);
            public Vector2 TipOffset => new Vector2(tip.x - anchor.x, anchor.y - tip.y) * Scale;
        }

        // Explicit rectangles matter: several pieces cross the nominal 6x3 cell borders.
        // Thighs stop at the knee; shins begin at the knee. No lower leg is duplicated.
        private static readonly Part[] Parts =
        {
            new Part("Head",52,76,225,205,180,273,.26f),
            new Part("Torso",366,53,239,197,500,75,.30f),
            new Part("Pelvis",682,132,196,147,780,152,.15f),
            new Part("NearUpperArm",983,65,148,227,1042,128,.20f,1082,267),
            new Part("NearForearmCannon",1212,102,295,178,1256,143,.18f,1494,230),
            new Part("FarUpperArm",1587,65,139,231,1645,124,.20f,1653,273),
            new Part("FarForearmHand",63,392,228,182,103,421,.15f),
            new Part("NearThigh",418,326,140,181,469,355,.20f,506,482),
            new Part("NearShin",694,439,134,155,755,453,.21f,766,587),
            new Part("NearFoot",950,447,225,160,1035,481,.19f,0,0,true),
            new Part("FarThigh",1280,327,126,192,1337,355,.21f,1363,502),
            new Part("FarShin",1579,446,126,153,1650,469,.21f,1660,591),
            new Part("FarFoot",41,679,222,159,177,710,.19f,0,0,true),
            new Part("BackBlade",359,637,229,223,555,670,.34f),
            new Part("ShoulderGuard",685,686,152,120,740,741,.115f),
            new Part("MuzzleFlash",958,691,162,136,969,764,.15f,0,0,true),
            new Part("ChargeOrb",1257,675,155,157,1335,754,.20f),
            new Part("EnergyBolt",1526,703,216,108,1538,757,.18f,0,0,true)
        };

        private enum Joint { Torso, Head, NearArm, NearForearm, FarArm, FarForearm, NearThigh, NearShin, NearFoot, FarThigh, FarShin, FarFoot, Blade }

        private sealed class Rig
        {
            public GameObject root;
            public Transform facing, body;
            public readonly Dictionary<Joint, Transform> joints = new Dictionary<Joint, Transform>();
            public readonly List<SpriteRenderer> bodyArt = new List<SpriteRenderer>();
            public readonly Dictionary<string, SpriteRenderer> effects = new Dictionary<string, SpriteRenderer>();
            public Transform bolt;
        }

        private sealed class Pose
        {
            public float x, y = BodyHeight, tilt, opacity = 1, flash, charge, bolt, shield, jet;
            public float boltTravel;
            public readonly float[] angles = new float[13];
            public Pose()
            {
                Set(Joint.NearArm,0); Set(Joint.NearForearm,-35);
                Set(Joint.FarArm,-15); Set(Joint.FarForearm,-30);
                Set(Joint.NearThigh,10); Set(Joint.NearShin,-8); Set(Joint.NearFoot,-2);
                Set(Joint.FarThigh,-15); Set(Joint.FarShin,12); Set(Joint.FarFoot,3);
            }
            public Pose Set(Joint joint,float angle) { angles[(int)joint]=angle; return this; }
        }

        private sealed class ActionClip
        {
            public string name;
            public float duration;
            public bool loop;
            public Func<float, Pose> sample;
            public ActionClip(string name,float duration,bool loop,Func<float,Pose> sample)
            { this.name=name;this.duration=duration;this.loop=loop;this.sample=sample; }
        }

        [Serializable] private sealed class BuildReport
        {
            public string atlas, prefab, controller, validation;
            public string[] canonicalClips;
            public AliasReport[] aliases;
        }
        [Serializable] private sealed class AliasReport { public string state, clip, notes; }

        [MenuItem("Mega Man X9/Animations/Rebuild Axl Rig")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding the Axl rig.");
            if(!File.Exists(AtlasPath)) throw new FileNotFoundException("Generate the Axl part atlas first.",AtlasPath);
            EnsureFolder(AnimationFolder); EnsureFolder(Path.GetDirectoryName(PrefabPath).Replace('\\','/'));
            Dictionary<string,Sprite> sprites=ImportParts();
            Rig rig=CreateRig(sprites);
            try
            {
                ActionClip[] actions=Actions();
                var clips=actions.ToDictionary(a=>a.name,a=>BuildClip(rig,a));
                AnimatorController controller=BuildController(clips);
                var animator=rig.root.AddComponent<Animator>();
                animator.runtimeAnimatorController=controller; animator.applyRootMotion=false;
                animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                rig.root.AddComponent<AxlRigActor>().Configure(rig.facing);
                PrefabUtility.SaveAsPrefabAsset(rig.root,PrefabPath);
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath,JsonUtility.ToJson(new BuildReport
                {
                    atlas=AtlasPath,prefab=PrefabPath,controller=ControllerPath,
                    validation="Asset construction completed; pose rendering and animation playback still require visual verification in Unity.",
                    canonicalClips=actions.Select(a=>a.name).ToArray(),
                    aliases=Aliases().Select(a=>new AliasReport{state=a.Key,clip=a.Value,notes=AliasNote(a.Key,a.Value)}).ToArray()
                },true));
                Debug.Log($"Axl rig: {actions.Length} articulated clips, {controller.layers[0].stateMachine.states.Length} named states. {PrefabPath}");
            }
            finally { UnityEngine.Object.DestroyImmediate(rig.root); }
        }

        private static Dictionary<string,Sprite> ImportParts()
        {
            AssetDatabase.ImportAsset(AtlasPath,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
            if(importer==null)throw new InvalidDataException("No importer for the Axl part atlas.");
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=100; importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true; importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=4096;
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            const int atlasHeight=887;
            SpriteRect[] rects=Parts.Select(p=>new SpriteRect
            {
                name=p.name,
                rect=new Rect(p.rect.x,atlasHeight-p.rect.y-p.rect.height,p.rect.width,p.rect.height),
                pivot=new Vector2((p.anchor.x-p.rect.x)/p.rect.width,1-(p.anchor.y-p.rect.y)/p.rect.height),
                alignment=SpriteAlignment.Custom,spriteID=StableGuid(AtlasPath+"/"+p.name)
            }).ToArray();
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply();importer.SaveAndReimport();
            var sprites=AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToDictionary(s=>s.name);
            foreach(Part p in Parts)if(!sprites.ContainsKey(p.name))throw new InvalidDataException("Missing rig sprite: "+p.name);
            return sprites;
        }

        private static Rig CreateRig(Dictionary<string,Sprite> sprites)
        {
            var r=new Rig{root=new GameObject("AxlRig_Animated")};r.root.AddComponent<SortingGroup>();
            r.facing=Node(r.root.transform,"Facing",Vector2.zero);
            r.body=Node(r.facing,"Body",new Vector2(0,BodyHeight));
            Transform torso=Bone(r,Joint.Torso,r.body,"Torso",new Vector2(0,.26f));
            Transform head=Bone(r,Joint.Head,torso,"Head",Vector2.zero);
            Piece(r,torso,"Torso",sprites,0);Piece(r,head,"Head",sprites,5);
            Piece(r,Node(r.body,"Pelvis",new Vector2(0,.02f)),"Pelvis",sprites,1);
            Transform blade=Bone(r,Joint.Blade,torso,"BackBlade",new Vector2(-.06f,-.06f));Piece(r,blade,"BackBlade",sprites,-8);
            Transform far=Bone(r,Joint.FarArm,torso,"FarUpperArm",new Vector2(.105f,-.083f));Piece(r,far,"FarUpperArm",sprites,-4);
            Transform farFore=Bone(r,Joint.FarForearm,far,"FarForearmHand",Definition("FarUpperArm").TipOffset);Piece(r,farFore,"FarForearmHand",sprites,-3);
            Transform near=Bone(r,Joint.NearArm,torso,"NearUpperArm",new Vector2(-.13f,-.10f));Piece(r,near,"NearUpperArm",sprites,6);
            Transform nearFore=Bone(r,Joint.NearForearm,near,"NearForearmCannon",Definition("NearUpperArm").TipOffset);Piece(r,nearFore,"NearForearmCannon",sprites,7);
            Piece(r,Node(near,"ShoulderGuard",new Vector2(-.005f,.01f)),"ShoulderGuard",sprites,8);
            Transform nearThigh=Bone(r,Joint.NearThigh,r.body,"NearThigh",new Vector2(.055f,-.032f));Piece(r,nearThigh,"NearThigh",sprites,3);
            Transform nearShin=Bone(r,Joint.NearShin,nearThigh,"NearShin",Definition("NearThigh").TipOffset);Piece(r,nearShin,"NearShin",sprites,3);
            Transform nearFoot=Bone(r,Joint.NearFoot,nearShin,"NearFoot",Definition("NearShin").TipOffset);Piece(r,nearFoot,"NearFoot",sprites,3);
            Transform farThigh=Bone(r,Joint.FarThigh,r.body,"FarThigh",new Vector2(-.055f,-.032f));Piece(r,farThigh,"FarThigh",sprites,-2);
            Transform farShin=Bone(r,Joint.FarShin,farThigh,"FarShin",Definition("FarThigh").TipOffset);Piece(r,farShin,"FarShin",sprites,-2);
            Transform farFoot=Bone(r,Joint.FarFoot,farShin,"FarFoot",Definition("FarShin").TipOffset);
            SpriteRenderer farShoe=Piece(r,farFoot,"FarFoot",sprites,-2);farShoe.flipX=true;
            Transform muzzle=Node(nearFore,"Muzzle",Definition("NearForearmCannon").TipOffset);
            Effect(r,muzzle,"Flash","MuzzleFlash",sprites,10,Vector2.zero,1);
            Effect(r,muzzle,"Charge","ChargeOrb",sprites,10,Vector2.zero,1);
            r.bolt=Node(muzzle,"Bolt",Vector2.zero);Effect(r,r.bolt,"BoltArt","EnergyBolt",sprites,9,Vector2.zero,1);
            Effect(r,r.body,"Shield","ChargeOrb",sprites,-6,new Vector2(0,.12f),4.6f);
            Transform nearJet=Node(nearFoot,"NearJet",new Vector2(0,-.015f));nearJet.localRotation=Quaternion.Euler(0,0,-90);
            Effect(r,nearJet,"NearJetArt","EnergyBolt",sprites,-1,Vector2.zero,1.3f);
            Transform farJet=Node(farFoot,"FarJet",new Vector2(0,-.015f));farJet.localRotation=Quaternion.Euler(0,0,-90);
            Effect(r,farJet,"FarJetArt","EnergyBolt",sprites,-3,Vector2.zero,1.3f);
            ApplyPose(r,new Pose());return r;
        }

        private static Part Definition(string name)=>Parts.First(p=>p.name==name);
        private static Transform Node(Transform parent,string name,Vector2 position)
        {
            var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=new Vector3(position.x,position.y,0);return o.transform;
        }
        private static Transform Bone(Rig r,Joint joint,Transform parent,string name,Vector2 position)
        { Transform bone=Node(parent,name,position);r.joints.Add(joint,bone);return bone; }
        private static SpriteRenderer Piece(Rig r,Transform bone,string name,Dictionary<string,Sprite> sprites,int order)
        {
            Transform art=Node(bone,"Art",Vector2.zero);Part def=Definition(name);
            art.localScale=Vector3.one*(def.Scale*100);
            var renderer=art.gameObject.AddComponent<SpriteRenderer>();renderer.sprite=sprites[name];renderer.sortingOrder=order;
            renderer.sharedMaterial=Material();r.bodyArt.Add(renderer);return renderer;
        }
        private static void Effect(Rig r,Transform parent,string name,string sprite,Dictionary<string,Sprite> sprites,int order,Vector2 pos,float scale)
        {
            Transform fx=Node(parent,name,pos);Part def=Definition(sprite);fx.localScale=Vector3.one*(def.Scale*100*scale);
            var renderer=fx.gameObject.AddComponent<SpriteRenderer>();renderer.sprite=sprites[sprite];renderer.sortingOrder=order;
            renderer.sharedMaterial=Material();renderer.color=new Color(1,1,1,0);r.effects[name]=renderer;
        }
        private static Material Material()
        {
            const string path=AnimationFolder+"/AxlRig_Unlit.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            Shader shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")??Shader.Find("Sprites/Default");
            if(shader==null)throw new InvalidOperationException("A sprite unlit shader is required.");
            material=new Material(shader);AssetDatabase.CreateAsset(material,path);return material;
        }

        private static ActionClip[] Actions()
        {
            return new[]
            {
                new ActionClip("Idle",1,true,t=>Idle(t)),
                new ActionClip("Run",.666667f,true,t=>Run(t,false)),
                new ActionClip("RunShoot",.666667f,true,t=>Run(t,true)),
                new ActionClip("Jump",.8f,false,t=>Jump(t)),
                new ActionClip("Dash",.5f,true,t=>Dash(t,false)),
                new ActionClip("DashAttack",.5f,false,t=>Dash(t,true)),
                new ActionClip("DashStop",.333333f,false,t=>RecoverDash(t)),
                new ActionClip("Crouch",.666667f,true,t=>Crouch(t,false)),
                new ActionClip("Aim",1,true,t=>Aim(t,false)),
                new ActionClip("AimUp",1,true,t=>Aim(t,true)),
                new ActionClip("Shoot",.416667f,false,t=>Shoot(t,false,false)),
                new ActionClip("CrouchShoot",.416667f,false,t=>Shoot(t,true,false)),
                new ActionClip("AirShoot",.5f,false,t=>Shoot(t,false,true)),
                new ActionClip("ChargeShot",.833333f,false,t=>Charge(t,false)),
                new ActionClip("AirChargeShot",.833333f,false,t=>Charge(t,true)),
                new ActionClip("AirAimUp",.666667f,true,t=>AirAim(t)),
                new ActionClip("JetRise",.666667f,true,t=>Jet(t,true)),
                new ActionClip("Hover",1,true,t=>Jet(t,false)),
                new ActionClip("Hurt",.416667f,false,t=>Hurt(t)),
                new ActionClip("Death",.833333f,false,t=>Death(t)),
                new ActionClip("Victory",1.166667f,false,t=>Victory(t)),
                new ActionClip("ArmRaise",.666667f,false,t=>Raise(t)),
                new ActionClip("Shield",1,true,t=>Shield(t)),
                new ActionClip("TeleportIn",.833333f,false,t=>Teleport(t,true)),
                new ActionClip("TeleportOut",.833333f,false,t=>Teleport(t,false))
            };
        }
        private static float Wave(float t)=>Mathf.Sin(t*Mathf.PI*2);
        private static float Ease(float t)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(t));
        private static Pose Idle(float t)
        {
            var p=new Pose();float w=Wave(t);p.y+=w*.006f;p.Set(Joint.Torso,w*.8f).Set(Joint.Head,-w*.8f);
            p.Set(Joint.NearArm,w*1.5f).Set(Joint.FarArm,-15-w);return p;
        }
        private static Pose Run(float t,bool shoot)
        {
            var p=new Pose();float a=Wave(t),b=Wave(t+.5f);p.y+=Mathf.Cos(t*4*Mathf.PI)*.014f;p.Set(Joint.Torso,-8).Set(Joint.Head,6);
            p.Set(Joint.NearThigh,a*34+4).Set(Joint.FarThigh,b*34-4);
            p.Set(Joint.NearShin,-Mathf.Max(0,-a)*52-4).Set(Joint.FarShin,-Mathf.Max(0,-b)*52-4);
            p.Set(Joint.NearFoot,-a*18).Set(Joint.FarFoot,-b*18);
            p.Set(Joint.NearArm,-a*34+12).Set(Joint.NearForearm,-35+a*12);
            p.Set(Joint.FarArm,-b*35-5).Set(Joint.FarForearm,-25+b*10);
            if(shoot){p.Set(Joint.NearArm,100).Set(Joint.NearForearm,-92);p.flash=t<.12f?1:0;}
            return p;
        }
        private static Pose Jump(float t)
        {
            var p=new Pose();float lift=Mathf.Sin(t*Mathf.PI);p.y+=lift*.08f;
            p.Set(Joint.NearThigh,15+lift*35).Set(Joint.NearShin,-10-lift*70).Set(Joint.NearFoot,10);
            p.Set(Joint.FarThigh,-10-lift*20).Set(Joint.FarShin,-lift*25).Set(Joint.FarFoot,15);
            p.Set(Joint.NearArm,15+lift*80).Set(Joint.NearForearm,-35-lift*40);
            p.Set(Joint.FarArm,-20-lift*65).Set(Joint.FarForearm,-30);return p;
        }
        private static Pose Dash(float t,bool attack)
        {
            var p=new Pose();p.y-=.08f;p.Set(Joint.Torso,-24).Set(Joint.Head,16);
            p.Set(Joint.NearThigh,65).Set(Joint.NearShin,-100).Set(Joint.NearFoot,35);
            p.Set(Joint.FarThigh,-48).Set(Joint.FarShin,-20).Set(Joint.FarFoot,25);
            p.Set(Joint.NearArm,75).Set(Joint.NearForearm,-51).Set(Joint.FarArm,-70).Set(Joint.FarForearm,-25);
            p.Set(Joint.Blade,8+Wave(t)*2);
            if(attack){p.flash=t>.3f&&t<.48f?1:0;p.bolt=t>.38f&&t<.85f?1:0;p.boltTravel=Mathf.Clamp01((t-.38f)/.47f)*.7f;}
            return p;
        }
        private static Pose RecoverDash(float t)
        {
            Pose a=Dash(t,false),b=new Pose();return Blend(a,b,Ease(t));
        }
        private static Pose Crouch(float t,bool aiming)
        {
            var p=new Pose();p.y-=.11f;p.Set(Joint.Torso,-9).Set(Joint.Head,9);
            p.Set(Joint.NearThigh,48).Set(Joint.NearShin,-92).Set(Joint.NearFoot,44);
            p.Set(Joint.FarThigh,-25).Set(Joint.FarShin,-25).Set(Joint.FarFoot,50);
            p.Set(Joint.NearArm,aiming?105:35).Set(Joint.NearForearm,aiming?-96:-60);return p;
        }
        private static Pose Aim(float t,bool up)
        {
            Pose p=Idle(t);p.Set(Joint.NearArm,up?145:100).Set(Joint.NearForearm,up?-106:-100);
            p.Set(Joint.FarArm,25).Set(Joint.FarForearm,-55);return p;
        }
        private static Pose Shoot(float t,bool crouch,bool air)
        {
            Pose p=crouch?Crouch(t,true):Aim(t,false);
            if(air){p.y+=.04f;p.Set(Joint.NearThigh,35).Set(Joint.NearShin,-60).Set(Joint.NearFoot,15);p.Set(Joint.FarThigh,-30).Set(Joint.FarShin,-15);}
            float recoil=Mathf.Sin(Mathf.Clamp01(t/.35f)*Mathf.PI)*5;
            p.Set(Joint.NearArm,(crouch?105:100)-recoil).Set(Joint.NearForearm,(crouch?-96:-100)+recoil);
            p.flash=t>.08f&&t<.30f?1:0;p.bolt=t>.18f&&t<.82f?1:0;p.boltTravel=Mathf.Clamp01((t-.18f)/.64f)*.8f;return p;
        }
        private static Pose Charge(float t,bool air)
        {
            Pose p=Shoot(t,false,air);p.flash=0;p.bolt=0;
            p.charge=t<.68f?(.25f+Mathf.Clamp01(t/.68f)*.75f):0;
            if(t>.68f&&t<.86f)p.flash=1;
            if(t>.72f&&t<.97f){p.bolt=1;p.boltTravel=(t-.72f)/.25f;}
            return p;
        }
        private static Pose AirAim(float t)
        {
            Pose p=Aim(t,true);p.Set(Joint.NearThigh,35).Set(Joint.NearShin,-55).Set(Joint.FarThigh,-25).Set(Joint.FarShin,-15);p.y+=.035f;return p;
        }
        private static Pose Jet(float t,bool rise)
        {
            var p=new Pose();p.y+=.07f+Wave(t)*.012f;p.Set(Joint.Torso,rise?8:2).Set(Joint.Head,rise?-8:-2);
            p.Set(Joint.NearThigh,15).Set(Joint.NearShin,-15).Set(Joint.NearFoot,0);
            p.Set(Joint.FarThigh,-8).Set(Joint.FarShin,8).Set(Joint.FarFoot,0);
            p.Set(Joint.NearArm,rise?115:50).Set(Joint.NearForearm,rise?-60:-65);
            p.Set(Joint.FarArm,rise?-65:-25).Set(Joint.FarForearm,-30);p.jet=.8f+Wave(t*3)*.2f;return p;
        }
        private static Pose Hurt(float t)
        {
            var p=new Pose();float hit=Mathf.Sin(t*Mathf.PI);p.Set(Joint.Torso,hit*17).Set(Joint.Head,-hit*14);
            p.Set(Joint.NearArm,hit*70).Set(Joint.NearForearm,-35-hit*25).Set(Joint.FarArm,-15-hit*55);
            p.Set(Joint.NearThigh,10+hit*28).Set(Joint.NearShin,-8-hit*30);p.y-=hit*.025f;return p;
        }
        private static Pose Death(float t)
        {
            var p=new Pose();float e=Ease(t);p.y=Mathf.Lerp(BodyHeight,.16f,e);p.tilt=-85*e;
            p.Set(Joint.Torso,12*e).Set(Joint.Head,-18*e).Set(Joint.NearArm,80*e).Set(Joint.NearForearm,-35-45*e);
            p.Set(Joint.FarArm,-15-50*e).Set(Joint.FarForearm,-30+40*e);
            p.Set(Joint.NearThigh,10+30*e).Set(Joint.NearShin,-8-45*e).Set(Joint.NearFoot,15*e);
            p.Set(Joint.FarThigh,-15-10*e).Set(Joint.FarShin,12-15*e);return p;
        }
        private static Pose Raise(float t)
        {
            var p=new Pose();float e=Ease(t);p.Set(Joint.NearArm,160*e).Set(Joint.NearForearm,-35-70*e);
            p.Set(Joint.FarArm,-15-90*e).Set(Joint.FarForearm,-30+30*e);p.Set(Joint.Head,-5*e);return p;
        }
        private static Pose Victory(float t)
        {
            Pose p=Raise(Mathf.Clamp01(t/.6f));if(t>.6f)p.Set(Joint.Head,-5+Wave((t-.6f)/.4f)*2);return p;
        }
        private static Pose Shield(float t)
        {
            Pose p=Crouch(t,false);p.y+=.07f;p.Set(Joint.NearArm,85).Set(Joint.NearForearm,-130).Set(Joint.FarArm,65).Set(Joint.FarForearm,-100);
            p.shield=.32f+Wave(t)*.05f;return p;
        }
        private static Pose Teleport(float t,bool entering)
        {
            float v=entering?t:1-t;Pose p=Raise(.75f);p.opacity=Ease((v-.15f)/.65f);
            p.Set(Joint.NearThigh,15).Set(Joint.NearShin,-15).Set(Joint.FarThigh,-5).Set(Joint.FarShin,5);
            p.shield=Mathf.Sin(v*Mathf.PI)*.6f;p.charge=Mathf.Sin(v*Mathf.PI)*.9f;return p;
        }
        private static Pose Blend(Pose a,Pose b,float t)
        {
            var p=new Pose{x=Mathf.Lerp(a.x,b.x,t),y=Mathf.Lerp(a.y,b.y,t),tilt=Mathf.Lerp(a.tilt,b.tilt,t)};
            for(int i=0;i<p.angles.Length;i++)p.angles[i]=Mathf.Lerp(a.angles[i],b.angles[i],t);return p;
        }

        private static AnimationClip BuildClip(Rig rig,ActionClip action)
        {
            string path=AnimationFolder+"/"+action.name+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
            clip.ClearCurves();clip.name=action.name;clip.frameRate=FrameRate;
            int count=Mathf.Max(2,Mathf.RoundToInt(action.duration*FrameRate)+1);
            Pose[] poses=Enumerable.Range(0,count).Select(i=>action.sample(i/(float)(count-1))).ToArray();
            SetCurve(clip,rig,rig.body,typeof(Transform),"m_LocalPosition.x",poses.Select(p=>p.x),action.duration);
            SetCurve(clip,rig,rig.body,typeof(Transform),"m_LocalPosition.y",poses.Select(p=>p.y),action.duration);
            SetCurve(clip,rig,rig.body,typeof(Transform),"localEulerAnglesRaw.z",poses.Select(p=>p.tilt),action.duration);
            foreach(var pair in rig.joints)
            {
                int i=(int)pair.Key;
                SetCurve(clip,rig,pair.Value,typeof(Transform),"localEulerAnglesRaw.z",poses.Select(p=>p.angles[i]),action.duration);
            }
            foreach(var art in rig.bodyArt)SetCurve(clip,rig,art.transform,typeof(SpriteRenderer),"m_Color.a",poses.Select(p=>p.opacity),action.duration);
            EffectCurve(clip,rig,"Flash",poses.Select(p=>p.flash*p.opacity),action.duration);
            EffectCurve(clip,rig,"Charge",poses.Select(p=>p.charge),action.duration);
            EffectCurve(clip,rig,"BoltArt",poses.Select(p=>p.bolt*p.opacity),action.duration);
            EffectCurve(clip,rig,"Shield",poses.Select(p=>p.shield),action.duration);
            EffectCurve(clip,rig,"NearJetArt",poses.Select(p=>p.jet*p.opacity),action.duration);
            EffectCurve(clip,rig,"FarJetArt",poses.Select(p=>p.jet*p.opacity),action.duration);
            SetCurve(clip,rig,rig.bolt,typeof(Transform),"m_LocalPosition.x",poses.Select(p=>p.boltTravel),action.duration);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=action.loop;settings.loopBlend=false;
            settings.startTime=0;settings.stopTime=action.duration;AnimationUtility.SetAnimationClipSettings(clip,settings);
            EditorUtility.SetDirty(clip);return clip;
        }
        private static void SetCurve(AnimationClip clip,Rig rig,Transform target,Type type,string property,IEnumerable<float> values,float duration)
        {
            float[] samples=values.ToArray();var curve=new AnimationCurve();
            for(int i=0;i<samples.Length;i++)curve.AddKey(i*duration/(samples.Length-1),samples[i]);
            for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(target,rig.root.transform),type,property),curve);
        }
        private static void EffectCurve(AnimationClip clip,Rig rig,string effect,IEnumerable<float> values,float duration)
        {SetCurve(clip,rig,rig.effects[effect].transform,typeof(SpriteRenderer),"m_Color.a",values,duration);}
        private static void ApplyPose(Rig r,Pose p)
        {
            r.body.localPosition=new Vector3(p.x,p.y,0);r.body.localRotation=Quaternion.Euler(0,0,p.tilt);
            foreach(var pair in r.joints)pair.Value.localRotation=Quaternion.Euler(0,0,p.angles[(int)pair.Key]);
        }

        private static Dictionary<string,string> Aliases()
        {
            return new Dictionary<string,string>
            {
                {"PointDiagonal","AimUp"},{"MaterializeSilhouette","TeleportIn"},{"AirDash","Dash"},
                {"ShootUp","AimUp"},{"LowShoot","CrouchShoot"},{"ShootRecover","Aim"},{"Charge","ChargeShot"},
                {"AirSpreadShot","AirShoot"},{"DashAttackReady","Aim"},{"DashAttackStartup","ChargeShot"},
                {"AirCharge","AirChargeShot"},{"DashAfterimage","Dash"},{"ShieldReady","Aim"},{"ShieldRecover","Idle"},
                {"ChargedShot","ChargeShot"},{"HurtRecover","Idle"},{"Knockdown","Death"},
                {"TeleportFinish","Idle"},{"TeleportBeam","TeleportIn"},{"TeleportArrivalPose","Idle"},
                {"AlternateIdle","Idle"},{"TeleportArrivalBeam","TeleportIn"},{"TeleportArrivalEnergy","TeleportIn"}
            };
        }
        private static string AliasNote(string state,string clip)
        {
            if(state=="AirSpreadShot")return "Compatibility alias: one aimed aerial shot; a distinct spread attack is not implemented.";
            if(state=="DashAfterimage")return "Compatibility alias: articulated dash; no duplicated afterimage renderers.";
            if(state.StartsWith("Teleport"))return "Compatibility alias: articulated pose plus energy fade, not the original illustrated beam frames.";
            return "Compatibility alias to the coherent rig clip "+clip+"; not an independently redrawn sequence.";
        }
        private static AnimatorController BuildController(Dictionary<string,AnimationClip> clips)
        {
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var machine=controller.layers[0].stateMachine;var states=new Dictionary<string,string>();
            foreach(string name in clips.Keys)states[name]=name;foreach(var alias in Aliases())states[alias.Key]=alias.Value;
            foreach(var old in machine.states.Where(s=>!states.ContainsKey(s.state.name)).ToArray())machine.RemoveState(old.state);
            int i=0;
            foreach(var entry in states)
            {
                var state=machine.states.FirstOrDefault(s=>s.state.name==entry.Key).state;
                if(state==null)state=machine.AddState(entry.Key,new Vector3(i%6*210,i/6*65,0));
                state.motion=clips[entry.Value];state.writeDefaultValues=false;
                if(entry.Key=="Idle")machine.defaultState=state;i++;
            }
            EditorUtility.SetDirty(controller);return controller;
        }

        /// <summary>Renders a contact sheet without touching an open scene.</summary>
        [MenuItem("Mega Man X9/Animations/Capture Axl Rig Poses")]
        public static void CapturePreview()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if(prefab==null)throw new FileNotFoundException("Build the Axl rig before capturing it.",PrefabPath);
            var scene=EditorSceneManager.NewPreviewScene();
            RenderTexture target=null;Texture2D contact=null;GameObject actor=null;
            RenderTexture previous=RenderTexture.active;
            try
            {
                actor=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                var cameraObject=new GameObject("Rig Preview Camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
                var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=.75f;
                camera.transform.position=new Vector3(.12f,.48f,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.075f,.11f,1);
                target=new RenderTexture(256,256,24);target.Create();camera.targetTexture=target;
                string[] names={"Idle","Run","Jump","Dash","Shoot","ChargeShot","Hover","Hurt","Death","Victory","Shield","TeleportIn"};
                contact=new Texture2D(1024,768,TextureFormat.RGBA32,false);
                for(int i=0;i<names.Length;i++)
                {
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationFolder+"/"+names[i]+".anim");
                    if(clip==null)continue;
                    clip.SampleAnimation(actor,clip.length*(names[i]=="Death"?.99f:.5f));
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;
                    var frame=new Texture2D(256,256,TextureFormat.RGBA32,false);frame.ReadPixels(new Rect(0,0,256,256),0,0);frame.Apply();
                    contact.SetPixels(i%4*256,(2-i/4)*256,256,256,frame.GetPixels());UnityEngine.Object.DestroyImmediate(frame);
                }
                contact.Apply();string folder="Tools/AnimationPipeline/QA/AxlPolish";Directory.CreateDirectory(folder);
                File.WriteAllBytes(folder+"/rig_pose_contact.png",contact.EncodeToPNG());
                File.WriteAllText(folder+"/rig_pose_contact_labels.txt","Rows: Idle, Run, Jump, Dash / Shoot, ChargeShot, Hover, Hurt / Death, Victory, Shield, TeleportIn");
                Debug.Log("Axl rig pose capture: "+folder+"/rig_pose_contact.png");
            }
            finally
            {
                RenderTexture.active=previous;
                if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
                if(contact!=null)UnityEngine.Object.DestroyImmediate(contact);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
        private static GUID StableGuid(string name)
        {
            using(var md5=MD5.Create())return new GUID(BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(name))).Replace("-","").ToLowerInvariant());
        }
        private static void EnsureFolder(string folder)
        {
            if(AssetDatabase.IsValidFolder(folder))return;
            string parent=Path.GetDirectoryName(folder).Replace('\\','/');if(!AssetDatabase.IsValidFolder(parent))EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(folder));
        }
    }
}
