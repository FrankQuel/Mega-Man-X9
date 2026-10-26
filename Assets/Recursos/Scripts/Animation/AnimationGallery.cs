using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MegaManX9.Animation
{
    /// <summary>Visual review scene; intentionally independent from gameplay input.</summary>
    public sealed class AnimationGallery : MonoBehaviour
    {
        [SerializeField] private SpriteAnimationLibrary library;
        [SerializeField] private Animator actor;
        [SerializeField] private SpriteRenderer actorRenderer;
        private readonly List<string> units = new List<string>();
        private int unitIndex;
        private int selectedIndex = -1;
        private List<int> visibleEntries = new List<int>();
        private Vector2 clipScroll;
        private Vector2 unitScroll;
        private float speed = 1f;
        private bool paused;
        private bool flip;
        private bool autoAdvance;
        private float playbackProgress;
        private GUIStyle titleStyle, headingStyle, detailStyle, buttonStyle;

        public void Configure(SpriteAnimationLibrary value, Animator animator, SpriteRenderer renderer)
        {
            library = value; actor = animator; actorRenderer = renderer;
        }

        private void Start()
        {
            if (library == null || library.entries.Count == 0 || actor == null || actorRenderer == null)
            {
                Debug.LogError("Animation gallery needs a populated library, Animator and SpriteRenderer.", this);
                enabled = false; return;
            }
            units.AddRange(library.entries.Select(e => e.category + "/" + e.unit).Distinct());
            SelectUnit(0);
        }

        private void SelectUnit(int index)
        {
            unitIndex = index; clipScroll = Vector2.zero;
            visibleEntries = Enumerable.Range(0, library.entries.Count)
                .Where(i => library.entries[i].category + "/" + library.entries[i].unit == units[index]).ToList();
            int initial = visibleEntries.FirstOrDefault(i => library.entries[i].animationName == "Idle");
            SelectClip(visibleEntries.Contains(initial) ? initial : visibleEntries[0]);
        }

        private void SelectClip(int index)
        {
            selectedIndex = index;
            var entry = library.entries[index];
            actor.runtimeAnimatorController = entry.controller;
            actorRenderer.sprite = entry.firstFrame;
            actorRenderer.flipX = flip;
            actor.Play(entry.animationName, 0, 0);
            actor.Update(0);
            actor.speed = paused ? 0 : speed;
            float height = Mathf.Max(0.08f, entry.frameHeight / 100f);
            float width = Mathf.Max(0.08f, entry.frameWidth / 100f);
            actor.transform.localScale = Vector3.one * Mathf.Min(2.8f / height, 5.4f / width);
            playbackProgress = 0;
        }

        private void Update()
        {
            if (selectedIndex < 0) return;
            actor.speed = paused ? 0 : speed;
            actorRenderer.flipX = flip;
            if (!paused) playbackProgress += Time.deltaTime * speed;
            var entry=library.entries[selectedIndex];
            float duration=Mathf.Max(1.2f,entry.clip.length*(entry.clip.isLooping ? 2f : 1f));
            if (autoAdvance && !paused && playbackProgress >= duration)
            {
                int next = (visibleEntries.IndexOf(selectedIndex) + 1) % visibleEntries.Count;
                SelectClip(visibleEntries[next]);
            }
        }

        private void Styles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(.3f,.9f,1f);
            headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = true };
            detailStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            detailStyle.normal.textColor = new Color(.72f,.79f,.88f);
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, alignment = TextAnchor.MiddleLeft, fixedHeight = 29 };
        }

        private void OnGUI()
        {
            if (selectedIndex < 0 || library == null) return;
            Styles();
            // A fixed logical canvas keeps controls usable at any Game view resolution.
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            var entry = library.entries[selectedIndex];
            GUI.Label(new Rect(25,18,800,45), "MEGA MAN X9  /  ANIMACIONES", titleStyle);
            GUI.Label(new Rect(27,68,760,35), "Biblioteca de sprites · revisión visual · recursos originales conservados", detailStyle);
            GUI.Box(new Rect(835,16,425,686), GUIContent.none);
            GUILayout.BeginArea(new Rect(850,28,395,660));
            GUILayout.Label("PERSONAJE / RECURSO", headingStyle);
            unitScroll = GUILayout.BeginScrollView(unitScroll, GUILayout.Height(125));
            for (int i=0;i<units.Count;i++)
            {
                GUI.backgroundColor = i==unitIndex ? new Color(.15f,.7f,.85f) : Color.white;
                if (GUILayout.Button(units[i],buttonStyle)) SelectUnit(i);
            }
            GUI.backgroundColor=Color.white;
            GUILayout.EndScrollView();
            GUILayout.Space(8);
            GUILayout.Label("CLIPS (" + visibleEntries.Count + ")", headingStyle);
            clipScroll=GUILayout.BeginScrollView(clipScroll,GUILayout.Height(250));
            foreach(int index in visibleEntries)
            {
                var item=library.entries[index];
                GUI.backgroundColor=index==selectedIndex ? new Color(.15f,.7f,.85f) : Color.white;
                if(GUILayout.Button(item.animationName+"   ["+item.frameCount+"]",buttonStyle)) SelectClip(index);
            }
            GUI.backgroundColor=Color.white;
            GUILayout.EndScrollView();
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button(paused ? "Reanudar" : "Pausa",GUILayout.Height(30))) paused=!paused;
            if(GUILayout.Button("Reiniciar",GUILayout.Height(30))) SelectClip(selectedIndex);
            GUILayout.EndHorizontal();
            flip=GUILayout.Toggle(flip,"Mirar hacia la izquierda");
            autoAdvance=GUILayout.Toggle(autoAdvance,"Recorrer clips automáticamente");
            GUILayout.Label("Velocidad: "+speed.ToString("0.0")+"x",detailStyle);
            speed=GUILayout.HorizontalSlider(speed,.1f,2f);
            GUILayout.EndArea();
            GUI.Box(new Rect(25,555,780,145),GUIContent.none);
            GUI.Label(new Rect(40,567,750,32),entry.unit+" / "+entry.animationName,headingStyle);
            GUI.Label(new Rect(40,605,745,70),entry.frameCount+" fotogramas · "+entry.clip.frameRate+" FPS · "+(entry.clip.isLooping ? "Bucle" : "Una reproducción")+"\n"+entry.notes,detailStyle);
            GUI.matrix=Matrix4x4.identity;
        }
    }
}
