using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MegaManX9.Animation
{
    /// <summary>A visual rehearsal stage. It previews art and never supplies combat or movement logic.</summary>
    public sealed partial class AnimationTestStage : MonoBehaviour
    {
        public const int PreviewLayer = 30;
        private const float CanvasWidth = 1600f, CanvasHeight = 900f, CameraSize = 6.5f;
        private const int StationCount = 6;
        [SerializeField] private SpriteAnimationLibrary library;
        [SerializeField] private Camera stageCamera;
        [SerializeField] private Transform[] previewRoots;
        [SerializeField] private GameObject dragonRigPrefab;
        [SerializeField] private GameObject axlRigPrefab;
        [SerializeField] private Material spriteMaterial;
        [SerializeField] private GameObject allPlatforms;
        [SerializeField] private GameObject individualPlatform;

        private sealed class Group
        {
            public string key, unit, label;
            public List<SpriteAnimationLibrary.Entry> entries;
            public GameObject rigPrefab;
            public RuntimeAnimatorController controller;
            public AnimationClip[] rigClips;
            public int selectedClip;
            public bool constantScale, neutralRigReady;
            public float neutralWidth, neutralHeight, neutralCenterX, neutralMinY;
            public bool IsRig => rigPrefab != null;
            public int Count => IsRig ? rigClips.Length : entries.Count;
            public AnimationClip Clip(int index) => IsRig ? rigClips[index] : entries[index].clip;
            public string ClipName(int index) => IsRig ? rigClips[index].name : entries[index].animationName;
        }

        private sealed class Slot
        {
            public Transform root;
            public SpriteRenderer renderer;
            public Animator normalAnimator;
            public Animator activeAnimator;
            public GameObject activeObject;
            public readonly Dictionary<string, GameObject> rigs = new Dictionary<string, GameObject>();
            public int group = -1, clip = -1;
            public float playElapsed, watchElapsed, scale = 1;
            public Vector3 presentationAnchor;
            public float rigCenterX, rigMinY;
            public bool entering;
            public DragonBossAttackPreview bossPreview;
        }

        private readonly List<Group> groups = new List<Group>();
        private Slot[] slots;
        private bool initialized;
        private bool allMode = true, paused, flipped, autoAdvance;
        private int page, selectedGroup;
        private float playbackSpeed = 1;
        private Vector2 unitScroll, clipScroll;
        private string unitSearch = "", clipSearch = "";
        private GUIStyle titleStyle, subtitleStyle, headingStyle, labelStyle, buttonStyle, tinyStyle;
        public int LibraryClipCount => library == null ? 0 : library.entries.Count;
        public int RigClipCount => groups.Where(g => g.IsRig).Sum(g => g.Count);
        public int GroupCount => groups.Count;
        public int PageCount => Mathf.CeilToInt(groups.Count / (float)StationCount);
        public Camera StageCamera => stageCamera;

        public void Configure(SpriteAnimationLibrary value, Camera camera, Transform[] roots,
            GameObject dragon, GameObject axl, Material material, GameObject platforms, GameObject largePlatform)
        {
            library = value; stageCamera = camera; previewRoots = roots;
            dragonRigPrefab = dragon; axlRigPrefab = axl; spriteMaterial = material;
            allPlatforms = platforms; individualPlatform = largePlatform;
        }

        private void Start()
        {
            try { Initialize(); ReplayStageIntroduction(); }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
            }
        }

        private void Initialize()
        {
            if (initialized) return;
            if (library == null || library.entries.Count == 0 || stageCamera == null ||
                previewRoots == null || previewRoots.Length != StationCount)
                throw new InvalidOperationException("La escena de prueba necesita la biblioteca, cámara y seis estaciones.");
            if (dragonRigPrefab == null)
                throw new InvalidOperationException("Falta el prefab del jefe Dragón. Regenera sus recursos y la escena.");

            foreach (var set in library.entries.GroupBy(e => e.category + "/" + e.unit))
            {
                var entries = set.ToList();
                if (entries.Any(e => e.clip == null || e.controller == null || e.firstFrame == null))
                    throw new InvalidOperationException("Hay referencias incompletas en " + set.Key);
                var group = new Group { key = set.Key, unit = entries[0].unit,
                    label = LabelForUnit(entries[0].unit), entries = entries,
                    constantScale = (entries[0].category == "Player" || entries[0].category == "Enemies" || entries[0].category == "Bosses") &&
                        !entries[0].unit.EndsWith("Parts", StringComparison.Ordinal) };
                int idle = entries.FindIndex(e => e.animationName == "Idle");
                group.selectedClip = Mathf.Max(0, idle);
                var neutral = entries[group.selectedClip];
                group.neutralWidth = neutral.frameWidth / neutral.firstFrame.pixelsPerUnit;
                group.neutralHeight = neutral.frameHeight / neutral.firstFrame.pixelsPerUnit;
                groups.Add(group);
            }
            AddRig("DragonRig", "Magma Dragoon · enemigo", dragonRigPrefab);
            string[] preferred = { "Blue", "BlueUltimate", "Red", "Axl", "DragonRig", "GrenadeMan" };
            groups.Sort((a, b) =>
            {
                int ai = Array.IndexOf(preferred, a.unit), bi = Array.IndexOf(preferred, b.unit);
                ai = ai < 0 ? int.MaxValue : ai; bi = bi < 0 ? int.MaxValue : bi;
                return ai != bi ? ai.CompareTo(bi) : string.Compare(a.key, b.key, StringComparison.Ordinal);
            });
            slots = previewRoots.Select(root => new Slot { root = root,
                renderer = root.GetComponent<SpriteRenderer>(), normalAnimator = root.GetComponent<Animator>() }).ToArray();
            if (slots.Any(slot => slot.root == null || slot.renderer == null || slot.normalAnimator == null))
                throw new InvalidOperationException("Cada estación necesita SpriteRenderer y Animator en su raíz.");
            initialized = true;
            InitializePresentation();
            SetCameraViewport();
            ShowAll();
        }

        private void AddRig(string unit, string label, GameObject prefab)
        {
            var animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null)
                throw new InvalidOperationException("El prefab " + unit + " necesita Animator y controlador.");
            var clips = animator.runtimeAnimatorController.animationClips.Where(clip => clip != null)
                .Distinct().OrderBy(clip => clip.name == "Idle" ? 0 : 1)
                .ThenBy(clip => clip.name, StringComparer.Ordinal).ToArray();
            if (clips.Length == 0) throw new InvalidOperationException("El controlador de " + unit + " no contiene clips.");
            int combatIdle=Array.FindIndex(clips,c=>c.name=="CombatIdle");
            groups.Add(new Group { key = "Enemy/" + unit, unit = unit, label = label,
                rigPrefab = prefab, controller = animator.runtimeAnimatorController, rigClips = clips, constantScale = true, selectedClip=Mathf.Max(0,combatIdle) });
        }

        private static string LabelForUnit(string unit)
        {
            switch (unit)
            {
                case "Blue": return "Azul";
                case "BlueUltimate": return "Azul · armadura definitiva";
                case "Red": return "Rojo";
                case "Axl": return "Axl original";
                case "Dragon": return "Magma Dragoon · poses originales";
                case "DragonParts": return "Dragón · piezas originales";
                case "AxlFX": return "Axl · efectos";
                case "RedEffects": return "Rojo · efectos";
                case "Pickups": return "Objetos y mejoras";
                case "StageIntro": return "Presentación del nivel";
                case "MainMenu": return "Menú principal";
                case "StatusHUD": return "Indicadores de pantalla";
                case "Rain": return "Lluvia";
                case "Explosion": return "Explosiones";
                case "BossDoor": return "Puerta de jefe";
                default: return unit;
            }
        }

        public static Vector3 PositionFromCanvas(float x, float y)
        {
            return new Vector3((x / CanvasWidth - .5f) * CameraSize * 2f * CanvasWidth / CanvasHeight,
                (.5f - y / CanvasHeight) * CameraSize * 2f, 0);
        }

        private void ShowAll()
        {
            CancelStageIntroduction();
            allMode = true;
            page = Mathf.Clamp(page, 0, Mathf.Max(0, PageCount - 1));
            allPlatforms.SetActive(true); individualPlatform.SetActive(false);
            for (int i = 0; i < slots.Length; i++)
            {
                int groupIndex = page * StationCount + i;
                slots[i].root.gameObject.SetActive(groupIndex < groups.Count);
                if (groupIndex >= groups.Count) { slots[i].group = -1; continue; }
                slots[i].root.position = PositionFromCanvas(280 + i % 3 * 520, 355 + i / 3 * 310);
                ApplyClip(slots[i], groupIndex, groups[groupIndex].selectedClip);
            }
        }

        private void ShowIndividual(int groupIndex)
        {
            CancelStageIntroduction();
            allMode = false; selectedGroup = Mathf.Clamp(groupIndex, 0, groups.Count - 1);
            allPlatforms.SetActive(false); individualPlatform.SetActive(true);
            for (int i = 0; i < slots.Length; i++) slots[i].root.gameObject.SetActive(i == 0);
            slots[0].root.position = PositionFromCanvas(565, 626);
            ApplyClip(slots[0], selectedGroup, groups[selectedGroup].selectedClip);
            clipScroll = Vector2.zero;
        }

        private void ApplyClip(Slot slot, int groupIndex, int clipIndex)
        {
            CancelSlotEntrance(slot);
            var group = groups[groupIndex];
            clipIndex = (clipIndex % group.Count + group.Count) % group.Count;
            group.selectedClip = clipIndex;
            slot.group = groupIndex; slot.clip = clipIndex;
            slot.playElapsed = 0; slot.watchElapsed = 0;
            slot.root.localScale = Vector3.one;
            // Reset the presentation anchor before computing the bounds of a different rig pose.
            if (allMode)
            {
                int index = Array.IndexOf(slots, slot);
                slot.root.position = PositionFromCanvas(280 + index % 3 * 520, 355 + index / 3 * 310);
            }
            else slot.root.position = PositionFromCanvas(565, 626);
            slot.presentationAnchor = slot.root.position;
            foreach (Transform child in slot.root) child.gameObject.SetActive(false);
            float maxHeight = allMode ? 2.25f : 4.8f, maxWidth = allMode ? 5.9f : 10f;
            if (group.IsRig)
            {
                slot.renderer.enabled = false; slot.normalAnimator.enabled = false;
                if (!slot.rigs.TryGetValue(group.key, out var instance) || instance == null)
                {
                    string savedName = "StageRig_" + group.unit;
                    var saved = slot.root.Find(savedName);
                    instance = saved != null ? saved.gameObject : Instantiate(group.rigPrefab, slot.root);
                    instance.name = savedName;
                    var gameplayDriver=instance.GetComponent<DragonRigActor>();
                    if(gameplayDriver!=null)
                    {
                        gameplayDriver.AutoReturnToIdle=false;
                        gameplayDriver.enabled=false;
                    }
                    slot.rigs[group.key] = instance;
                    SetLayerRecursively(instance.transform);
                    foreach (var renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
                        if (spriteMaterial != null) renderer.sharedMaterial = spriteMaterial;
                }
                instance.SetActive(true);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                slot.activeObject = instance;
                slot.activeAnimator = instance.GetComponentInChildren<Animator>(true);
                slot.activeAnimator.enabled = true;
                slot.activeAnimator.runtimeAnimatorController = group.controller;
            }
            else
            {
                slot.renderer.enabled = true; slot.normalAnimator.enabled = true;
                var entry = group.entries[clipIndex];
                slot.renderer.sprite = entry.firstFrame;
                slot.activeAnimator = slot.normalAnimator;
                slot.activeAnimator.runtimeAnimatorController = entry.controller;
                slot.activeObject = slot.root.gameObject;
            }
            var clip = group.Clip(clipIndex);
            slot.activeAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            slot.activeAnimator.applyRootMotion = false;
            slot.activeAnimator.Rebind();
            if (group.IsRig && !group.neutralRigReady)
            {
                // Use one neutral pose for every action. Measuring the current attack would
                // enlarge crouches and shrink sword swings when the selected clip changes.
                int neutralIndex = Array.FindIndex(group.rigClips, candidate => candidate.name == "Idle");
                if (neutralIndex < 0) neutralIndex = 0;
                slot.activeAnimator.Play(group.ClipName(neutralIndex), 0, 0);
                slot.activeAnimator.Update(0);
                var neutralRenderers = slot.activeObject.GetComponentsInChildren<SpriteRenderer>()
                    .Where(renderer => renderer.enabled && renderer.sprite != null && renderer.color.a > .01f).ToArray();
                if (neutralRenderers.Length == 0) throw new InvalidOperationException("El rig " + group.label + " no muestra sprites en reposo.");
                var neutralBounds = neutralRenderers[0].bounds;
                foreach (var renderer in neutralRenderers.Skip(1)) neutralBounds.Encapsulate(renderer.bounds);
                group.neutralWidth = neutralBounds.size.x; group.neutralHeight = neutralBounds.size.y;
                group.neutralCenterX = neutralBounds.center.x - slot.root.position.x;
                group.neutralMinY = neutralBounds.min.y - slot.root.position.y;
                group.neutralRigReady = true;
            }
            if (!slot.activeAnimator.HasState(0, Animator.StringToHash(group.ClipName(clipIndex))))
                throw new InvalidOperationException("Falta el estado " + group.ClipName(clipIndex) + " en " + group.label);
            slot.activeAnimator.Play(group.ClipName(clipIndex), 0, 0);
            slot.activeAnimator.Update(0);
            slot.bossPreview=group.IsRig?slot.activeObject.GetComponent<DragonBossAttackPreview>():null;
            if(slot.bossPreview!=null)
            {
                slot.bossPreview.AutoAdvance=false;
                slot.bossPreview.SetPaused(paused);
                slot.bossPreview.Flip(flipped);
                if(!slot.bossPreview.BeginAttack(group.ClipName(clipIndex)))throw new InvalidOperationException("Ataque de jefe no disponible: "+group.ClipName(clipIndex));
            }
            if (group.IsRig)
            {
                slot.rigCenterX = group.neutralCenterX; slot.rigMinY = group.neutralMinY;
                slot.scale = Mathf.Min(maxHeight / Mathf.Max(.05f, group.neutralHeight), maxWidth / Mathf.Max(.05f, group.neutralWidth));
            }
            else
            {
                var entry = group.entries[clipIndex];
                float ppu = entry.firstFrame.pixelsPerUnit;
                float height = group.constantScale ? group.neutralHeight : entry.frameHeight / ppu;
                float width = group.constantScale ? group.neutralWidth : entry.frameWidth / ppu;
                slot.scale = Mathf.Min(maxHeight / Mathf.Max(.05f, height), maxWidth / Mathf.Max(.05f, width));
            }
            ApplySpeedAndFlip(slot);
        }

        private static void SetLayerRecursively(Transform root)
        {
            root.gameObject.layer = PreviewLayer;
            foreach (Transform child in root) SetLayerRecursively(child);
        }

        private void ApplySpeedAndFlip(Slot slot)
        {
            var group = groups[slot.group];
            slot.activeAnimator.speed = paused ? 0 : playbackSpeed;
            slot.renderer.flipX = flipped;
            slot.root.localScale = new Vector3(group.IsRig && flipped ? -slot.scale : slot.scale, slot.scale, slot.scale);
            if (group.IsRig)
                slot.root.position = slot.presentationAnchor + new Vector3((flipped ? 1 : -1) * slot.rigCenterX * slot.scale,
                    -slot.rigMinY * slot.scale - (allMode ? 8f : 12f) / CanvasHeight * CameraSize * 2f, 0);
        }

        private void Update()
        {
            if (!initialized) return;
            SetCameraViewport();
            UpdatePresentation(Time.deltaTime);
            foreach (var slot in slots)
            {
                if (!slot.root.gameObject.activeSelf || slot.group < 0) continue;
                if(slot.entering || (readySequence!=null && readySequence.IsPlaying && IsPlayable(groups[slot.group].unit)))continue;
                ApplySpeedAndFlip(slot);
                if(slot.bossPreview!=null)
                {
                    slot.bossPreview.SetPaused(paused);slot.bossPreview.Flip(flipped);
                    if(!paused)slot.bossPreview.Advance(Time.deltaTime*playbackSpeed);
                }
                if (paused) continue;
                slot.playElapsed += Time.deltaTime * playbackSpeed;
                slot.watchElapsed += Time.deltaTime;
                var group = groups[slot.group]; var clip = group.Clip(slot.clip);
                float duration = clip.length * (clip.isLooping ? 2f : 1f);
                if (autoAdvance && slot.playElapsed >= duration && slot.watchElapsed >= 1.5f)
                    ApplyClip(slot, slot.group, slot.clip + 1);
            }
        }

        private void SetCameraViewport()
        {
            if (stageCamera == null || Screen.width <= 0 || Screen.height <= 0) return;
            float scale = Mathf.Min(Screen.width / CanvasWidth, Screen.height / CanvasHeight);
            float width = CanvasWidth * scale / Screen.width, height = CanvasHeight * scale / Screen.height;
            stageCamera.rect = new Rect((1 - width) * .5f, (1 - height) * .5f, width, height);
            stageCamera.aspect = CanvasWidth / CanvasHeight;
        }

        /// <summary>Editor capture/test entry point; no simulated gameplay input is needed.</summary>
        public void PrepareForCapture(int requestedPage = 0, float time = .25f)
        {
            Initialize();
            autoAdvance = false; paused = true; page = requestedPage;
            ShowAll();
            foreach (var slot in slots)
            {
                if (!slot.root.gameObject.activeSelf || slot.group < 0) continue;
                var group = groups[slot.group]; var clip = group.Clip(slot.clip);
                float sampleTime = clip.isLooping && clip.length > 0 ? time % clip.length : Mathf.Min(time, clip.length * .95f);
                if(slot.bossPreview!=null)slot.bossPreview.Seek(sampleTime);
                else {slot.activeAnimator.Play(group.ClipName(slot.clip), 0, clip.length > 0 ? sampleTime / clip.length : 0);slot.activeAnimator.Update(0);}
                slot.activeAnimator.speed = 0;
            }
        }

        /// <summary>Checks that every library entry and every rig clip has a reachable station.</summary>
        public void ValidateCoverage()
        {
            Initialize();
            int covered = groups.Where(group => !group.IsRig).Sum(group => group.Count);
            if (covered != LibraryClipCount) throw new InvalidOperationException("Hay clips fuera de las estaciones.");
            foreach (var group in groups)
            {
                for (int index = 0; index < group.Count; index++)
                {
                    var controller = group.IsRig ? group.controller : group.entries[index].controller;
                    if (!controller.animationClips.Contains(group.Clip(index)))
                        throw new InvalidOperationException("Clip fuera del controlador: " + group.label + "/" + group.ClipName(index));
                }
            }
        }

        private void Styles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 29, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(.45f, .94f, 1);
            subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            subtitleStyle.normal.textColor = new Color(.74f, .84f, .93f);
            headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = true };
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            tinyStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            tinyStyle.normal.textColor = new Color(.7f, .82f, .91f);
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, fixedHeight = 30, alignment = TextAnchor.MiddleLeft };
        }

        private void OnGUI()
        {
            if (!initialized) return;
            Styles();
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / CanvasWidth, Screen.height / CanvasHeight);
            Vector3 offset = new Vector3((Screen.width - CanvasWidth * scale) / 2f, (Screen.height - CanvasHeight * scale) / 2f);
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale);
            try
            {
            GUI.Box(new Rect(20, 12, 1560, 119), GUIContent.none);
            GUI.Label(new Rect(42, 23, 1200, 38), "MEGA MAN X9  /  ESCENARIO DE ANIMACIONES", titleStyle);
            GUI.Label(new Rect(44, 67, 1100, 30), LibraryClipCount + " clips originales · " + RigClipCount +
                " clips articulados · " + GroupCount + " estaciones · escenario industrial de tus recursos", subtitleStyle);
            if (GUI.Toggle(new Rect(1200, 28, 160, 32), allMode, "Todos", GUI.skin.button) && !allMode)
            { ShowAll(); GUIUtility.ExitGUI(); }
            if (GUI.Toggle(new Rect(1370, 28, 180, 32), !allMode, "Individual", GUI.skin.button) && allMode)
            { ShowIndividual(selectedGroup); GUIUtility.ExitGUI(); }
            GUI.Label(new Rect(44, 100, 1400, 24), "Entrada de jugables · vida por personaje y jefe · fuego de boca y manos · READY y teletransporte", tinyStyle);
            bool controlsEnabled=GUI.enabled;GUI.enabled=controlsEnabled&&!IsIntroductionPlaying;
            if (allMode) DrawAll(); else DrawIndividual();
            GUI.enabled=controlsEnabled;
            DrawTransport();
            DrawPresentationHUD();
            if(readySequence!=null)readySequence.Draw(new Rect(0,0,CanvasWidth,CanvasHeight));
            }
            finally { GUI.matrix = previous; }
        }

        private void DrawAll()
        {
            for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                var slot = slots[slotIndex]; if (slot.group < 0) continue;
                var group = groups[slot.group];
                float x = 40 + slotIndex % 3 * 520, y = 153 + slotIndex / 3 * 310;
                GUI.Box(new Rect(x, y, 480, 39), GUIContent.none);
                GUI.Label(new Rect(x + 12, y + 7, 458, 29), group.label, headingStyle);
                GUI.Box(new Rect(x, y + 219, 480, 72), GUIContent.none);
                GUI.Label(new Rect(x + 12, y + 224, 455, 24), slot.entering?"Entrada · teletransporte":group.ClipName(slot.clip) + "  [" + (slot.clip + 1) + "/" + group.Count + "]", labelStyle);
                if (GUI.Button(new Rect(x + 10, y + 253, 62, 30), "◀")) ApplyClip(slot, slot.group, slot.clip - 1);
                if (GUI.Button(new Rect(x + 80, y + 253, 62, 30), "▶")) ApplyClip(slot, slot.group, slot.clip + 1);
                if (GUI.Button(new Rect(x + 154, y + 253, 130, 30), "Reiniciar")) ApplyClip(slot, slot.group, slot.clip);
                if (GUI.Button(new Rect(x + 296, y + 253, 172, 30), "Ver individual"))
                {
                    selectedGroup = slot.group; ShowIndividual(slot.group); GUIUtility.ExitGUI();
                }
            }
        }

        private void DrawIndividual()
        {
            var group = groups[selectedGroup];
            GUI.Label(new Rect(80, 153, 940, 37), group.label, headingStyle);
            GUI.Label(new Rect(80, 190, 940, 28), group.ClipName(group.selectedClip), subtitleStyle);
            GUI.Box(new Rect(1090, 150, 470, 606), GUIContent.none);
            GUILayout.BeginArea(new Rect(1105, 163, 440, 580));
            GUILayout.Label("PERSONAJE / RECURSO", headingStyle);
            unitSearch = GUILayout.TextField(unitSearch, GUILayout.Height(25));
            unitScroll = GUILayout.BeginScrollView(unitScroll, GUILayout.Height(135));
            for (int i = 0; i < groups.Count; i++)
            {
                if (!Contains(groups[i].label + " " + groups[i].unit, unitSearch)) continue;
                GUI.backgroundColor = i == selectedGroup ? new Color(.2f, .7f, .85f) : Color.white;
                if (GUILayout.Button(groups[i].label + "  [" + groups[i].Count + "]", buttonStyle))
                {
                    selectedGroup = i; clipSearch = ""; ShowIndividual(i);
                    GUI.backgroundColor = Color.white;
                    GUIUtility.ExitGUI();
                }
            }
            GUI.backgroundColor = Color.white; GUILayout.EndScrollView();
            GUILayout.Space(7); GUILayout.Label("CLIPS · " + group.Count, headingStyle);
            clipSearch = GUILayout.TextField(clipSearch, GUILayout.Height(25));
            clipScroll = GUILayout.BeginScrollView(clipScroll, GUILayout.Height(270));
            // Re-read selection after a resource button changed it in this same GUI event.
            group = groups[selectedGroup];
            for (int i = 0; i < group.Count; i++)
            {
                if (!Contains(group.ClipName(i), clipSearch)) continue;
                GUI.backgroundColor = i == group.selectedClip ? new Color(.2f, .7f, .85f) : Color.white;
                if (GUILayout.Button(group.ClipName(i), buttonStyle)) ApplyClip(slots[0], selectedGroup, i);
            }
            GUI.backgroundColor = Color.white; GUILayout.EndScrollView(); GUILayout.EndArea();
            var clip = group.Clip(group.selectedClip);
            GUI.Box(new Rect(60, 681, 1000, 75), GUIContent.none);
            string info = group.IsRig ? "Rig articulado · " : group.entries[group.selectedClip].frameCount + " fotogramas · ";
            GUI.Label(new Rect(76, 690, 960, 28), info + clip.frameRate + " FPS · " + (clip.isLooping ? "Bucle" : "Una reproducción"), labelStyle);
            GUI.Label(new Rect(76, 721, 960, 25), "Jefe enemigo con ataques y proyectiles; vida y entradas disponibles en los controles.", tinyStyle);
        }

        private static bool Contains(string text, string search)
        {
            return string.IsNullOrWhiteSpace(search) || text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void DrawTransport()
        {
            GUI.Box(new Rect(20, 784, 1560, 101), GUIContent.none);
            if (GUI.Button(new Rect(42, 801, 140, 34), paused ? "Reanudar" : "Pausa")) paused = !paused;
            if (GUI.Button(new Rect(194, 801, 150, 34), "Reiniciar todos"))
            {
                if(IsIntroductionPlaying)ReplayStageIntroduction();
                else foreach (var slot in slots) if (slot.root.gameObject.activeSelf && slot.group >= 0) ApplyClip(slot, slot.group, slot.clip);
            }
            flipped = GUI.Toggle(new Rect(363, 808, 180, 28), flipped, "Reflejar personaje");
            autoAdvance = GUI.Toggle(new Rect(560, 808, 240, 28), autoAdvance, "Recorrer todos los clips");
            GUI.Label(new Rect(822, 804, 175, 28), "Velocidad " + playbackSpeed.ToString("0.0") + "×", labelStyle);
            playbackSpeed = GUI.HorizontalSlider(new Rect(995, 814, 220, 23), playbackSpeed, .1f, 2f);
            if (allMode)
            {
                if (GUI.Button(new Rect(1240, 800, 65, 34), "◀")) { page = (page - 1 + PageCount) % PageCount; ShowAll(); }
                GUI.Label(new Rect(1315, 807, 150, 30), "Página " + (page + 1) + "/" + PageCount, labelStyle);
                if (GUI.Button(new Rect(1474, 800, 65, 34), "▶")) { page = (page + 1) % PageCount; ShowAll(); }
            }
            else if (GUI.Button(new Rect(1240, 800, 299, 34), "Volver a todas las estaciones"))
            { ShowAll(); GUIUtility.ExitGUI(); }
            DrawPresentationControls();
        }
    }
}
