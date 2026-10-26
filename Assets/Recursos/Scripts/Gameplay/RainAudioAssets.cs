using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MegaManX9.Gameplay
{
    /// <summary>References the user's imported recordings. Aliases never duplicate audio data.</summary>
    [CreateAssetMenu(menuName = "Mega Man X9/Rain Audio Assets")]
    public sealed class RainAudioAssets : ScriptableObject
    {
        [Serializable]
        public sealed class Cue
        {
            public string name;
            public AudioClip clip;
            [Range(0, 1)] public float gain = 1;
            public bool loop;
            public string sourcePath;
        }

        public List<Cue> cues = new List<Cue>();
        [Range(0, 1)] public float musicVolume = .43f;
        [Range(0, 1)] public float ambienceVolume = .55f;
        [Range(0, 1)] public float effectsVolume = .78f;
        [Range(.05f, 2)] public float musicFadeSeconds = .32f;
        [Range(0, 1)] public float pauseMusicFactor = .55f;
        [Range(0, 1)] public float pauseRainFactor = .25f;
        private readonly Dictionary<string, Cue> indexed = new Dictionary<string, Cue>(StringComparer.OrdinalIgnoreCase);
        private int indexedCount = -1;

        public static RainAudioAssets Load() => Resources.Load<RainAudioAssets>("RainAudioAssets");
        public int ImportedClipCount
        {
            get
            {
                var unique = new HashSet<AudioClip>();
                foreach (var cue in cues) if (cue != null && cue.clip != null) unique.Add(cue.clip);
                return unique.Count;
            }
        }
        public void Invalidate() { indexed.Clear(); indexedCount = -1; }
        private void OnEnable() => Invalidate();
        private void OnValidate() => Invalidate();
        public bool TryGet(string name, out Cue cue)
        {
            if (indexedCount != cues.Count)
            {
                indexed.Clear();
                foreach (var entry in cues) if (entry != null && !string.IsNullOrEmpty(entry.name)) indexed[Key(entry.name)] = entry;
                indexedCount = cues.Count;
            }
            return indexed.TryGetValue(Key(CanonicalCue(name)), out cue) && cue.clip != null;
        }
        public static string Key(string value)
        {
            var key = new StringBuilder();
            foreach (char character in value ?? "") if (char.IsLetterOrDigit(character)) key.Append(char.ToLowerInvariant(character));
            return key.ToString();
        }
        public static string CanonicalCue(string value)
        {
            switch (Key(value))
            {
                case "main": case "mainmusic": case "maintheme": return "mainTheme";
                case "level": case "levelmusic": case "leveltheme": case "mainleveltheme": return "levelTheme";
                case "dragon": case "dragonmusic": case "dragontheme": case "dragonbosstheme": return "dragonTheme";
                case "axl":  case "axlmusic": case "axltheme": case "axlbosstheme": return "axlTheme";
                case "conversation": case "dialogue": case "conversationmusic": case "conversationtheme": return "conversationTheme";
                case "results": case "resultstheme": return "resultsTheme";
                case "rain": case "sonidodelluvia": return "rain";
                case "jump": case "salto": return "jump";
                case "dash": case "dashxyzero": return "dash";
                case "dashalternate": case "dashalt": return "dashAlternate";
                case "shot": case "buster": case "xdisparonormal": case "axlshot": return "shot";
                case "chargedshot": case "chargeshot": case "xdisparocargado": return "chargedShot";
                case "charge": case "charging": case "xcargandodisparo": return "charge";
                case "saber": case "zeroataquesconelsable": return "saber";
                case "saber1": case "zeroprimerataqueconelsable": return "saber1";
                case "saber2": case "zerosegundoataqueconelsableseguido": return "saber2";
                case "saber3": case "zerotercerataqueconelsableseguido": return "saber3";
                case "hurt": case "hit": case "recibirdanio": return "hurt";
                case "death": case "morir": return "death";
                case "arrival": case "spawn": case "ingresanaljuegooreaparecenxyzero": return "arrival";
                case "departure": case "escape": case "xzeroyaxlsemarchan": return "departure";
                case "pickup": case "heal": case "recuperanvidacontanquesdeenergia": return "pickup";
                case "score": case "points": return "score";
                case "extralife": case "life": return "extraLife";
                case "enemydeath": case "explosion": case "enemigonormalalmorir": return "enemyDeath";
                case "dragonhurt": case "dragonrecibedanio": return "dragonHurt";
                case "dragonattack": case "dragonataques": return "dragonAttack";
                case "axlhurt":  case "axlrecibedanio": return "axlHurt";
                case "ready": case "cuandoempiezaconready": return "ready";
                case "warning": case "cuandoaparecewarning": return "warning";
                case "dooropen": case "seabrepuertadejefe": return "doorOpen";
                case "doorclose": case "secierrapuertadejefe": return "doorClose";
                case "menumove": case "selecciondeopcionesopersonajes": return "menuMove";
                case "menuconfirm": case "confirmaciondeseleccion": return "menuConfirm";
                case "menuenter": case "alpresionarenteraliniciodeljuego": return "menuEnter";
                case "voicexswitchout": case "xcambiodepersonajeazero": return "voiceXSwitchOut";
                case "voicexswitchin": case "xingresadespuesdeuncambiodepersonaje": return "voiceXSwitchIn";
                case "voicezeroswitchout": case "zerocambiadepersonajeax": return "voiceZeroSwitchOut";
                case "voicezeroswitchin": case "zeroingresadespuesdeuncambiodepersonaje": return "voiceZeroSwitchIn";
                case "land": case "impact": return "land";
                default: return value ?? "";
            }
        }
    }
}
