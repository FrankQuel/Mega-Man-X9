using UnityEngine;

namespace MegaManX9.Gameplay
{
    [RequireComponent(typeof(Camera))]
    public sealed class RainLevelCamera : MonoBehaviour
    {
        [SerializeField] private RainHeroController player;
        [SerializeField] private RainLevelGame game;
        [SerializeField] private Transform[] parallaxLayers = System.Array.Empty<Transform>();
        [SerializeField] private float[] parallaxRates = System.Array.Empty<float>();
        private Vector3[] origins;
        private Camera view;
        private Vector3 velocity;
        private float previousHeroX, lookAhead, shakeTime, shakeStrength, clock;
        private float initialCameraX, verticalTarget;
        private bool verticalInitialized;

        public Camera View => view != null ? view : view = GetComponent<Camera>();
        public float SafeFloorY {get;private set;}
        public bool IsHoldingOverVoid {get;private set;}

        public void Configure(RainHeroController hero, RainLevelGame session, Transform[] layers = null, float[] rates = null)
        {
            player = hero; game = session;
            parallaxLayers = layers ?? System.Array.Empty<Transform>();
            parallaxRates = rates ?? System.Array.Empty<float>();
            CacheOrigins();
            SnapToTarget();
            initialCameraX = transform.position.x;
        }

        private void Awake()
        {
            view = GetComponent<Camera>(); CacheOrigins();
            if (player != null) previousHeroX = player.transform.position.x;
            initialCameraX = transform.position.x;
        }

        private void CacheOrigins()
        {
            origins = new Vector3[parallaxLayers.Length];
            for (int i = 0; i < origins.Length; i++) if (parallaxLayers[i] != null) origins[i] = parallaxLayers[i].position;
        }

        private Vector3 Target()
        {
            float halfWidth = View.orthographicSize * View.aspect;
            bool arena = InArena;
            float left = arena ? RainLevelWorld.ArenaLeft : 0;
            float right = arena ? RainLevelWorld.ArenaRight : RainLevelWorld.LevelLength;
            float focusX=player.transform.position.x+lookAhead;
            if(arena&&game.ActiveBossTransform!=null)focusX=(player.transform.position.x+game.ActiveBossTransform.position.x)*.5f;
            float x = right - left <= halfWidth * 2 ? (left + right) / 2 :
                Mathf.Clamp(focusX, left + halfWidth, right - halfWidth);
            float floorFraming=View.orthographicSize*.58f;
            float y = arena ? (game.InputLocked?4.1f:floorFraming) : verticalTarget;
            return new Vector3(x, y, -20);
        }

        private bool InArena => game!=null&&(game.Phase==RainLevelPhase.BossIntro||game.Phase==RainLevelPhase.Boss||
            game.Phase==RainLevelPhase.BossCinematic||game.Phase==RainLevelPhase.AxlBoss||
            game.Phase==RainLevelPhase.Ending||game.BossWasDefeated);

        private float DesiredSize()
        {
            if(!InArena)return 3;
            if(game.InputLocked)return 6.2f;
            if(game.ActiveBossTransform==null)return 4.2f;
            float separation=Mathf.Abs(player.transform.position.x-game.ActiveBossTransform.position.x);
            return Mathf.Clamp((separation+5)/(2*View.aspect),4.2f,6.2f);
        }

        private void UpdateVerticalTarget(bool reset=false)
        {
            float foot=player.transform.position.y;
            float framing=View.orthographicSize*.58f;
            IsHoldingOverVoid=false;
            if(reset||!verticalInitialized)
            {
                SafeFloorY=foot;verticalTarget=foot+framing;verticalInitialized=true;
            }
            else if(player.IsOnLadder)
            {
                verticalTarget=foot+framing;
            }
            else if(player.IsGrounded&&TryFloorBelow(.25f,out float standingFloor))
            {
                SafeFloorY=standingFloor;verticalTarget=standingFloor+framing;
            }
            else
            {
                // A lower camera target requires real authored terrain below the hero.
                // Sliding off a ledge into a bottomless gap keeps the last safe composition.
                float upper=verticalTarget+View.orthographicSize-1.2f;
                float lower=verticalTarget-View.orthographicSize+.9f;
                if(foot>upper)verticalTarget+=foot-upper;
                else if(foot<lower)
                {
                    if(TryFloorBelow(16,out float landingFloor))
                        verticalTarget=Mathf.Min(verticalTarget,Mathf.Max(foot+framing,landingFloor+framing));
                    else IsHoldingOverVoid=true;
                }
            }
            verticalTarget=Mathf.Clamp(verticalTarget,-2+framing,14);
        }

        private bool TryFloorBelow(float distance,out float floor)
        {
            floor=float.NegativeInfinity;bool found=false;
            for(int i=-1;i<=1;i++)
            {
                Vector2 origin=(Vector2)player.transform.position+new Vector2(i*.18f,.08f);
                foreach(var hit in Physics2D.RaycastAll(origin,Vector2.down,distance,1<<RainLevelWorld.TerrainLayer))
                {
                    if(hit.collider==null||hit.collider.isTrigger||hit.normal.y<.65f)continue;
                    floor=Mathf.Max(floor,hit.point.y);found=true;
                }
            }
            return found;
        }

        public void SnapToTarget()
        {
            if (player == null) return;
            velocity = Vector3.zero; lookAhead = 0;
            View.orthographicSize=DesiredSize();
            UpdateVerticalTarget(true);
            previousHeroX = player.transform.position.x;
            transform.position = Target();
        }

        public void Shake(float seconds = .15f, float strength = .08f)
        {
            shakeTime = Mathf.Max(shakeTime, seconds); shakeStrength = Mathf.Max(shakeStrength, strength);
        }

        private void LateUpdate()
        {
            ConfigureViewport();
            if (player == null || (game != null && game.IsPaused)) return;
            float delta = Time.deltaTime; clock += delta;
            View.orthographicSize=Mathf.Lerp(View.orthographicSize,DesiredSize(),1-Mathf.Exp(-delta*4));
            UpdateVerticalTarget();
            float movement = player.transform.position.x - previousHeroX;
            if (Mathf.Abs(movement) > .001f) lookAhead = Mathf.Lerp(lookAhead, Mathf.Sign(movement) * .7f, delta * 5);
            previousHeroX = player.transform.position.x;
            Vector3 target = Target();
            if (Mathf.Abs(target.x - transform.position.x) > 24 || Mathf.Abs(target.y-transform.position.y)>12) SnapToTarget();
            else transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, .14f, 90, delta);
            if (shakeTime > 0)
            {
                shakeTime -= delta;
                transform.position += new Vector3(Mathf.Sin(clock * 83), Mathf.Cos(clock * 97), 0) * shakeStrength;
            }
            else shakeStrength = 0;
            if (origins == null || origins.Length != parallaxLayers.Length) CacheOrigins();
            for (int i = 0; i < parallaxLayers.Length; i++)
                if (parallaxLayers[i] != null)
                    parallaxLayers[i].position = origins[i] + Vector3.right * (transform.position.x - initialCameraX) *
                        (i < parallaxRates.Length ? parallaxRates[i] : .2f);
        }

        private void ConfigureViewport()
        {
            if (View.targetTexture != null)
            {
                View.rect = new Rect(0,0,1,1);
                View.aspect = View.targetTexture.width/(float)View.targetTexture.height;
                return;
            }
            if (Screen.width <= 0 || Screen.height <= 0) return;
            float scale = Mathf.Min(Screen.width/1600f,Screen.height/900f);
            float width = 1600*scale/Screen.width,height = 900*scale/Screen.height;
            View.rect = new Rect((1-width)/2,(1-height)/2,width,height);
            View.aspect = 16f/9f;
        }

        private void OnGUI()
        {
            if(View.targetTexture!=null||Event.current.type!=EventType.Repaint)return;
            // URP can retain old pixels outside a changed camera viewport. Clear the letterbox explicitly.
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            GUI.matrix=Matrix4x4.identity;GUI.color=Color.black;GUI.depth=10000;
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            float x=(Screen.width-1600*scale)/2,y=(Screen.height-900*scale)/2;
            if(x>.1f){GUI.DrawTexture(new Rect(0,0,x,Screen.height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(Screen.width-x,0,x,Screen.height),Texture2D.whiteTexture);}
            if(y>.1f){GUI.DrawTexture(new Rect(0,0,Screen.width,y),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,Screen.height-y,Screen.width,y),Texture2D.whiteTexture);}
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
    }
}
