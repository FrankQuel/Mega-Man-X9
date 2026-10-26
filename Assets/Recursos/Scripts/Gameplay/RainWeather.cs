using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MegaManX9.Gameplay
{
    /// <summary>One world-space rain layer; open air gaps stay wet while solids and the lowest floor stay dry.</summary>
    [DisallowMultipleComponent,DefaultExecutionOrder(300)]
    public sealed class RainWeather : MonoBehaviour
    {
        [SerializeField] private Sprite[] rainFrames=Array.Empty<Sprite>();
        [SerializeField] private Material material;
        [SerializeField] private Camera view;
        [SerializeField] private RainLevelGame game;
        [SerializeField] private Color rainTint=new Color(.50f,.58f,.76f,.27f);
        [SerializeField,Range(.05f,1)] private float surfaceRefreshInterval=.25f;
        [SerializeField,Range(0,.15f)] private float surfaceClearance=.025f;
        private const string MaterialResource="RainSeamlessRain",ShaderName="MegaManX9/RainSeamlessRain";
        private const int TerrainMask=1<<8,RequestedSamples=8192,MaximumSolidRects=128;
        private const float SkyY=128,LowestY=-256,NoSurface=-1024;
        private Mesh quad;
        private MeshRenderer rainRenderer;
        private Transform layer;
        private Material rainMaterial;
        private Texture2D surfaceMap;
        private MaterialPropertyBlock properties;
        private readonly RaycastHit2D[] rayHits=new RaycastHit2D[16];
        private readonly Collider2D[] occupancyHits=new Collider2D[8];
        private readonly Vector4[] solidRects=new Vector4[MaximumSolidRects];
        private int solidCount;
        private ContactFilter2D terrainFilter;
        private PhysicsScene2D terrainPhysics;
        private float[] heights;
        private Color[] rgbaHeights;
        private float mapLeft=-8,mapWidth=280,clock,refreshRemaining,previousCameraX=float.NaN,previousSize;
        private bool mapReady,manualPause;
        public int ActiveTileCount=>rainRenderer!=null&&rainRenderer.enabled?1:0;
        public bool IsContinuous=>quad!=null&&rainRenderer!=null&&rainMaterial!=null&&rainMaterial.shader!=null&&rainMaterial.shader.isSupported;
        public bool SurfaceOcclusionEnabled=>mapReady&&surfaceMap!=null;
        public int OriginalRainFrameCount=>rainFrames.Length;
        public float RainClock=>clock;
        public int SurfaceSampleCount=>heights!=null?heights.Length:0;
        public bool AirGapRainEnabled=>true;
        public int SolidOcclusionCount=>solidCount;

        // Kept unchanged for the existing level builder and serialized scene references.
        public void Configure(Sprite[] frames,Material spriteMaterial,Camera camera,RainLevelGame session)
        {rainFrames=frames??Array.Empty<Sprite>();material=spriteMaterial;view=camera;game=session;}
        public void SetPaused(bool paused)=>manualPause=paused;
        private void Start()=>EnsureLayer();
        private void OnEnable(){if(rainRenderer!=null)rainRenderer.enabled=mapReady;}
        private void OnDisable(){if(rainRenderer!=null)rainRenderer.enabled=false;}

        private void EnsureLayer()
        {
            if(rainRenderer!=null||view==null)return;
            var template=Resources.Load<Material>(MaterialResource);
            var shader=template!=null?template.shader:Shader.Find(ShaderName);
            if(shader==null||!shader.isSupported)
            {Debug.LogError("RainSeamlessRain shader is missing or unsupported. Keep its Resources material in the build.",this);return;}
            rainMaterial=template!=null?new Material(template):new Material(shader);
            rainMaterial.name="Continuous rain runtime";
            var child=new GameObject("Continuous rain layer");layer=child.transform;layer.SetParent(transform,false);
            var filter=child.AddComponent<MeshFilter>();rainRenderer=child.AddComponent<MeshRenderer>();rainRenderer.enabled=false;
            // City/buildings are negative; solid platform bodies start at 3. Rain belongs between them.
            rainRenderer.sharedMaterial=rainMaterial;rainRenderer.sortingOrder=1;
            rainRenderer.shadowCastingMode=ShadowCastingMode.Off;rainRenderer.receiveShadows=false;
            quad=new Mesh{name="Rain continuous unit quad"};
            quad.vertices=new[]{new Vector3(-1,-1,0),new Vector3(1,-1,0),new Vector3(1,1,0),new Vector3(-1,1,0)};
            quad.triangles=new[]{0,1,2,0,2,3};quad.RecalculateBounds();filter.sharedMesh=quad;
            properties=new MaterialPropertyBlock();
            terrainFilter=new ContactFilter2D{useLayerMask=true,layerMask=TerrainMask,useTriggers=false};
            BuildSurfaceMap();UpdateCoverage();ApplyProperties();rainRenderer.enabled=true;
        }

        private void BuildSurfaceMap()
        {
            int count=Mathf.Min(RequestedSamples,Mathf.Max(256,SystemInfo.maxTextureSize));
            mapLeft=-8;mapWidth=RainLevelWorld.LevelLength+16;
            heights=new float[count];
            var format=SystemInfo.SupportsTextureFormat(TextureFormat.RFloat)?TextureFormat.RFloat:TextureFormat.RGBAFloat;
            surfaceMap=new Texture2D(count,1,format,false,true){name="Rain lowest foundation heights",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            if(format!=TextureFormat.RFloat)rgbaHeights=new Color[count];
            terrainPhysics=gameObject.scene.GetPhysicsScene2D();
            Physics2D.SyncTransforms();
            CollectSolidRects();
            for(int i=0;i<count;i++)heights[i]=FindLowestFloor(mapLeft+(i+.5f)*mapWidth/count);
            UploadHeights();mapReady=true;refreshRemaining=surfaceRefreshInterval;
        }
        private float FindLowestFloor(float x)
        {
            int count=terrainPhysics.Raycast(new Vector2(x,SkyY),Vector2.down,SkyY-LowestY,terrainFilter,rayHits);
            float floor=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                var hit=rayHits[i];
                // Thin suspended decks/roof slabs only occlude their occupied volume, never a whole column.
                if(hit.collider!=null&&hit.normal.y>.25f&&hit.collider.bounds.size.y>=1.5f)
                    floor=Mathf.Min(floor,hit.point.y);
            }
            return float.IsPositiveInfinity(floor)?NoSurface:floor;
        }
        private void CollectSolidRects()
        {
            solidCount=0;
            foreach(var root in gameObject.scene.GetRootGameObjects())
            foreach(var collider in root.GetComponentsInChildren<Collider2D>())
            {
                if(!collider.enabled||collider.isTrigger||collider.gameObject.layer!=8||!collider.gameObject.activeInHierarchy)continue;
                if(solidCount>=MaximumSolidRects)break;
                Bounds bounds=collider.bounds;
                solidRects[solidCount++]=new Vector4(bounds.min.x,bounds.min.y,bounds.max.x,bounds.max.y);
            }
        }
        private void RefreshVisibleSurfaces()
        {
            if(heights==null||view==null)return;
            terrainPhysics=gameObject.scene.GetPhysicsScene2D();
            Physics2D.SyncTransforms();
            CollectSolidRects();
            float width=view.orthographicSize*view.aspect+3;
            int first=Mathf.Clamp(Mathf.FloorToInt((view.transform.position.x-width-mapLeft)/mapWidth*heights.Length)-2,0,heights.Length-1);
            int last=Mathf.Clamp(Mathf.CeilToInt((view.transform.position.x+width-mapLeft)/mapWidth*heights.Length)+2,0,heights.Length-1);
            for(int i=first;i<=last;i++)heights[i]=FindLowestFloor(mapLeft+(i+.5f)*mapWidth/heights.Length);
            UploadHeights();refreshRemaining=surfaceRefreshInterval;
        }
        private void UploadHeights()
        {
            if(rgbaHeights==null)surfaceMap.SetPixelData(heights,0);
            else
            {
                for(int i=0;i<heights.Length;i++)rgbaHeights[i]=new Color(heights[i],0,0,1);
                surfaceMap.SetPixelData(rgbaHeights,0);
            }
            surfaceMap.Apply(false,false);
        }

        private void LateUpdate()
        {
            if(view==null)return;EnsureLayer();if(rainRenderer==null)return;
            bool stopped=manualPause||game!=null&&game.InputLocked;
            if(!stopped)clock+=Time.deltaTime;
            // Geometry and the roof mask follow the camera even when its animation clock is paused.
            UpdateCoverage();refreshRemaining-=Time.unscaledDeltaTime;
            if(refreshRemaining<=0||float.IsNaN(previousCameraX)||Mathf.Abs(view.transform.position.x-previousCameraX)>2||Mathf.Abs(view.orthographicSize-previousSize)>.1f)
            {RefreshVisibleSurfaces();previousCameraX=view.transform.position.x;previousSize=view.orthographicSize;}
            ApplyProperties();
        }
        private void UpdateCoverage()
        {
            if(layer==null)return;
            transform.position=new Vector3(view.transform.position.x,view.transform.position.y,-2);
            transform.rotation=Quaternion.identity;transform.localScale=Vector3.one;
            layer.localPosition=Vector3.zero;layer.localRotation=Quaternion.identity;
            layer.localScale=new Vector3(view.orthographicSize*view.aspect+2,view.orthographicSize+2,1);
        }
        private void ApplyProperties()
        {
            properties.SetColor("_RainColor",rainTint);properties.SetFloat("_RainClock",clock);
            properties.SetTexture("_SurfaceHeights",surfaceMap);
            properties.SetVector("_SurfaceRange",new Vector4(mapLeft,1/mapWidth,surfaceClearance,mapReady?1:0));
            properties.SetFloat("_SurfaceMapTexel",1f/heights.Length);rainRenderer.SetPropertyBlock(properties);
            properties.SetFloat("_SolidCount",solidCount);properties.SetVectorArray("_SolidRects",solidRects);
            rainRenderer.SetPropertyBlock(properties);
        }

        /// <summary>Rain is visible in open space above the base floor, including gaps below suspended platforms.</summary>
        public bool SampleRainVisibleAt(Vector2 worldPoint)
        {
            terrainFilter=new ContactFilter2D{useLayerMask=true,layerMask=TerrainMask,useTriggers=false};
            terrainPhysics=gameObject.scene.GetPhysicsScene2D();
            Physics2D.SyncTransforms();
            if(terrainPhysics.OverlapPoint(worldPoint,terrainFilter,occupancyHits)>0)return false;
            return worldPoint.y>FindLowestFloor(worldPoint.x)+surfaceClearance;
        }
        public float SampleExposedSurfaceHeight(float x)
        {
            terrainFilter=new ContactFilter2D{useLayerMask=true,layerMask=TerrainMask,useTriggers=false};
            terrainPhysics=gameObject.scene.GetPhysicsScene2D();
            Physics2D.SyncTransforms();return FindLowestFloor(x);
        }
        public float SampleRainFloorHeight(float x)=>SampleExposedSurfaceHeight(x);
        private void OnDestroy()
        {
            if(quad!=null)Destroy(quad);if(surfaceMap!=null)Destroy(surfaceMap);if(rainMaterial!=null)Destroy(rainMaterial);
        }
    }
}
