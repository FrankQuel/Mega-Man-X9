using System;
using UnityEngine;

namespace MegaManX9.Gameplay
{
    /// <summary>Level gates, checkpoints and authored hazards. The game owns health and session transitions.</summary>
    public sealed class RainLevelWorld : MonoBehaviour
    {
        public const int TerrainLayer = 8;
        public const float LevelLength = 264;
        public const float ArenaLeft = 238, ArenaRight = 260;
        public const float BossDoorX = 236.5f, FallLimit = -8;

        [Serializable] public sealed class CheckpointZone
        {
            public string label;
            public Vector2 spawn;
            public Bounds area;
            public SpriteRenderer beacon;
            [NonSerialized] public bool reached;
        }

        [Serializable] public sealed class ElectricZone
        {
            public Bounds area;
            public SpriteRenderer[] lights;
            public float phaseOffset;
            public int damage = 4;
            [NonSerialized] public float nextHit;
        }

        [Serializable] public sealed class PickupPlacement
        {
            public string id;
            public RainPickupKind kind;
            public Vector2 position;
        }

        [SerializeField] private RainLevelGame game;
        [SerializeField] private RainHeroController player;
        [SerializeField] private RainDragonBoss boss;
        [SerializeField] private GameObject entranceGate;
        [SerializeField] private CheckpointZone[] checkpoints = Array.Empty<CheckpointZone>();
        [SerializeField] private ElectricZone[] electricZones = Array.Empty<ElectricZone>();
        [SerializeField] private PickupPlacement[] pickupPlacements=Array.Empty<PickupPlacement>();
        [SerializeField] private SpriteRenderer doorFace,doorTop,doorBottom;
        [SerializeField] private BoxCollider2D doorCollider;
        [SerializeField] private Sprite[] doorUnlockFrames=Array.Empty<Sprite>(),doorOpenFrames=Array.Empty<Sprite>();
        [SerializeField] private Sprite[] doorTopFrames=Array.Empty<Sprite>(),doorBottomFrames=Array.Empty<Sprite>();
        private float clock;
        private float doorTime=-1;

        public CheckpointZone[] Checkpoints => checkpoints;
        public ElectricZone[] ElectricZones => electricZones;
        public PickupPlacement[] PickupPlacements=>pickupPlacements;
        public bool ArenaSealed => game != null && (game.Phase == RainLevelPhase.BossIntro ||
            game.Phase == RainLevelPhase.Boss || game.Phase == RainLevelPhase.BossCinematic ||
            game.Phase == RainLevelPhase.AxlBoss || game.Phase == RainLevelPhase.Ending || game.BossWasDefeated);
        public float DoorProgress => Mathf.Clamp01(Mathf.Max(0,doorTime)/1.15f);
        public bool DoorIsOpen => doorCollider!=null&&!doorCollider.enabled;

        public void ConfigurePickups(PickupPlacement[] placements)
        {pickupPlacements=placements??Array.Empty<PickupPlacement>();}

        private void RespawnAuthoredPickups()
        {
            if(game==null||!game.HasStarted)return;
            foreach(var placement in pickupPlacements)
                if(placement!=null&&!string.IsNullOrEmpty(placement.id))
                    game.SpawnPickup(placement.kind,placement.position,placement.id);
        }

        public void ConfigureDoor(SpriteRenderer face,SpriteRenderer upper,SpriteRenderer lower,BoxCollider2D solid,
            Sprite[] unlock,Sprite[] opening,Sprite[] retractUpper,Sprite[] retractLower)
        {
            doorFace=face;doorTop=upper;doorBottom=lower;doorCollider=solid;
            doorUnlockFrames=unlock??Array.Empty<Sprite>();doorOpenFrames=opening??Array.Empty<Sprite>();
            doorTopFrames=retractUpper??Array.Empty<Sprite>();doorBottomFrames=retractLower??Array.Empty<Sprite>();
            doorTime=-1;UpdateDoor(0);
        }

        public void Configure(RainLevelGame session, RainHeroController hero, RainDragonBoss dragon,
            GameObject leftGate, GameObject rightGate, CheckpointZone[] checkpointZones, ElectricZone[] hazards)
        {
            game = session; player = hero; boss = dragon;
            entranceGate = leftGate;
            checkpoints = checkpointZones ?? Array.Empty<CheckpointZone>();
            electricZones = hazards ?? Array.Empty<ElectricZone>();
            UpdateGates();
        }

        private void Update()
        {
            if (game == null || player == null) return;
            UpdateGates();
            if (game.IsPaused) return;
            clock += Time.deltaTime;
            DrawElectricState();
            UpdateDoor(Time.deltaTime);
            if (game.Phase != RainLevelPhase.Playing && game.Phase != RainLevelPhase.Boss && game.Phase != RainLevelPhase.AxlBoss) return;
            Vector3 foot = player.transform.position;
            if (foot.y < FallLimit || foot.x < -4 || foot.x > LevelLength + 3)
            {
                game.LoseLifeAndRespawn();
                return;
            }
            Vector3 body = foot + Vector3.up * .43f;
            foreach (CheckpointZone checkpoint in checkpoints)
            {
                if (checkpoint.reached || !checkpoint.area.Contains(body)) continue;
                checkpoint.reached = true;
                if (checkpoint.beacon != null) checkpoint.beacon.color = new Color(.35f, 1, .62f);
                game.SetCheckpoint(checkpoint.spawn, checkpoint.label);
            }
            foreach (ElectricZone hazard in electricZones)
            {
                if (!IsElectricActive(hazard) || clock < hazard.nextHit || !hazard.area.Contains(foot + Vector3.up * .25f)) continue;
                hazard.nextHit = clock + .8f;
                player.TakeDamage(hazard.damage, Vector2.up);
            }
            if (boss != null && boss.IsAlive && foot.x >= ArenaLeft + .45f && foot.x < ArenaRight && foot.y < .4f &&
                game.Phase == RainLevelPhase.Playing)
                game.BeginBossEncounter();
        }

        private bool IsElectricActive(ElectricZone zone) => Mathf.Repeat(clock + zone.phaseOffset, 3.2f) >= 1.5f;
        private void DrawElectricState()
        {
            foreach(var checkpoint in checkpoints)if(checkpoint.beacon!=null)checkpoint.beacon.enabled=RainPresentationSettings.IsDevelopment;
            foreach (ElectricZone zone in electricZones)
            {
                bool active = IsElectricActive(zone);
                float blink = active ? .72f + Mathf.Sin(clock * 47) * .22f : .16f;
                foreach (var light in zone.lights ?? Array.Empty<SpriteRenderer>())
                    if (light != null) light.color = new Color(.12f, .88f, 1, blink);
            }
        }

        private void UpdateGates()
        {
            if (entranceGate != null) entranceGate.SetActive(true);
        }

        private static Sprite DoorFrame(Sprite[] frames,float progress)
        {
            return frames.Length==0?null:frames[Mathf.Clamp(Mathf.FloorToInt(progress*frames.Length),0,frames.Length-1)];
        }

        private void UpdateDoor(float delta)
        {
            if(doorFace==null||doorCollider==null)return;
            if(ArenaSealed)
            {
                if(!doorCollider.enabled)RainLevelAudio.Play("doorClose");
                doorCollider.enabled=true;doorFace.enabled=true;doorFace.sprite=DoorFrame(doorUnlockFrames,0);
                if(doorTop!=null)doorTop.enabled=false;if(doorBottom!=null)doorBottom.enabled=false;
                return;
            }
            if(game!=null&&game.BossWasDefeated)doorTime=1.15f;
            else if(game!=null&&player!=null&&game.Phase==RainLevelPhase.Playing&&
                player.transform.position.x>BossDoorX-2.6f&&player.transform.position.x<BossDoorX+4)
            {if(doorTime<0)RainLevelAudio.Play("doorOpen");doorTime=Mathf.Max(0,doorTime);}
            if(doorTime>=0)doorTime=Mathf.Min(1.15f,doorTime+delta);
            bool retract=doorTime>=.80f;
            float retraction=Mathf.Clamp01((doorTime-.80f)/.35f);
            doorCollider.enabled=doorTime<1.05f;
            doorFace.enabled=!retract;
            if(!retract)doorFace.sprite=doorTime<.35f?DoorFrame(doorUnlockFrames,Mathf.Max(0,doorTime)/.35f):
                DoorFrame(doorOpenFrames,(doorTime-.35f)/.45f);
            if(doorTop!=null)
            {
                doorTop.enabled=retract&&retraction<1;doorTop.sprite=DoorFrame(doorTopFrames,retraction);
                doorTop.transform.localPosition=new Vector3(0,2.375f+retraction*1.55f,0);
            }
            if(doorBottom!=null)
            {
                doorBottom.enabled=retract&&retraction<1;doorBottom.sprite=DoorFrame(doorBottomFrames,retraction);
            }
        }

        public void ResetWorld()
        {
            clock = 0;doorTime=-1;
            foreach (CheckpointZone checkpoint in checkpoints)
            {
                checkpoint.reached = false;
                if (checkpoint.beacon != null) checkpoint.beacon.color = new Color(.12f, .75f, 1);
            }
            foreach (ElectricZone hazard in electricZones) hazard.nextHit = 0;
            UpdateGates();
            UpdateDoor(0);
            RespawnAuthoredPickups();
        }

        private void OnDrawGizmosSelected()
        {
            if(!RainPresentationSettings.IsDevelopment)return;
            Gizmos.color = Color.cyan;
            foreach (CheckpointZone checkpoint in checkpoints)
                Gizmos.DrawWireCube(checkpoint.area.center, checkpoint.area.size);
            Gizmos.color = new Color(1, .55f, .05f);
            foreach (ElectricZone hazard in electricZones)
                Gizmos.DrawWireCube(hazard.area.center, hazard.area.size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3((ArenaLeft + ArenaRight) / 2, 3.5f, 0), new Vector3(ArenaRight - ArenaLeft, 7, 1));
        }
    }
}
