using System;
using System.IO;
using MegaManX9.Gameplay;
using UnityEditor;
using UnityEngine;

namespace MegaManX9.EditorTools
{
    public static class RainNarrativeAssetsBuilder
    {
        public const string AssetPath = "Assets/Recursos/Resources/RainNarrativeAssets.asset";

        [MenuItem("Mega Man X9/Playable Rain Level/Build Narrative Assets", priority = 7)]
        private static void BuildMenu() => Build();

        public static RainNarrativeAssets Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Construye los recursos narrativos fuera de Play.");
            var one = Texture("Assets/Recursos/Scenes/Cinematic/Cinematica uno.png");
            var two = Texture("Assets/Recursos/Scenes/Cinematic/Cinematica dos.png");
            var three = Texture("Assets/Recursos/Scenes/Cinematic/Cinematica tres.png");
            var four = Texture("Assets/Recursos/Scenes/Cinematic/Cinematica cuatro.png");
            var x = Texture("Assets/Recursos/Sprites/Mugshot/x.png");
            var zero = Texture("Assets/Recursos/Sprites/Mugshot/zero.png");
            var axl = Texture("Assets/Recursos/Sprites/Mugshot/axl.png");
            EnsureFolder("Assets/Recursos/Resources");
            var asset = AssetDatabase.LoadAssetAtPath<RainNarrativeAssets>(AssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RainNarrativeAssets>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            asset.xPortrait = x; asset.zeroPortrait = zero; asset.axlPortrait = axl;
            // Spanish numeric order is explicit: lexical sorting would place cuatro before dos.
            asset.introduction = new[]
            {
                Page(RainNarrativeSpeaker.Narrator, "", "Tras derrotar a Lumine, X, Zero y Axl descienden del elevador Jakob. Axl sigue inconsciente; X lo lleva en brazos mientras Zero vigila el regreso.", one),
                Page(RainNarrativeSpeaker.Narrator, "", "En la base, Axl permanece en recuperación. Los sistemas médicos estabilizan su energía, pero aún no despierta. X y Zero esperan una señal de mejoría.", two),
                Page(RainNarrativeSpeaker.Narrator, "", "Un estruendo rompe el silencio junto a la sala de Axl. X y Zero corren a buscarlo: encuentran la sala destrozada y una enorme brecha en la pared. Axl ya no está.", three),
                Page(RainNarrativeSpeaker.Narrator, "", "Una alerta de los monitores señala una nueva ubicación. La firma energética coincide con Axl. X y Zero confirman la señal y preparan el rescate.", four),
                Page(RainNarrativeSpeaker.Narrator, "", "X y Zero se dirigen a Central Highway.")
            };
            asset.confrontation = new[]
            {
                Page(RainNarrativeSpeaker.X, "X", "¡AXL!"),
                Page(RainNarrativeSpeaker.Zero, "Zero", "¡Axl! ¿Qué haces aquí? ¿Por qué nos estás atacando?"),
                Page(RainNarrativeSpeaker.Axl, "Axl", "¿Axl...? Ja... ja, ja, ja."),
                Page(RainNarrativeSpeaker.X, "X", "Esa voz... No. Tú no eres Axl."),
                Page(RainNarrativeSpeaker.Axl, "Axl", "Por fin lo entienden. Su cuerpo me ha dado una nueva oportunidad."),
                Page(RainNarrativeSpeaker.Zero, "Zero", "¡Lumine! Estás controlando a Axl. ¡Déjalo libre!"),
                Page(RainNarrativeSpeaker.Axl, "Lumine · en Axl", "Este cuerpo ahora me pertenece. Si quieren recuperarlo, tendrán que sobrevivir.")
            };
            asset.ending = new[]
            {
                Page(RainNarrativeSpeaker.X, "X", "Lumine... ¿cómo pudiste sobrevivir?"),
                Page(RainNarrativeSpeaker.Axl, "Lumine · en Axl", "Antes de mi derrota, introduje una parte de mí en Axl."),
                Page(RainNarrativeSpeaker.Zero, "Zero", "Lo usaste para esconderte... y ahora intentas convertirlo en tu arma."),
                Page(RainNarrativeSpeaker.Axl, "Lumine · en Axl", "No será la última vez que nos encontremos. Axl vendrá conmigo."),
                Page(RainNarrativeSpeaker.X, "X", "Puedes huir, pero no vamos a abandonarlo."),
                Page(RainNarrativeSpeaker.Zero, "Zero", "Te encontraremos, Lumine. Rescataremos a Axl.")
            };
            asset.missionArrivalX = new[]
            {
                Page(RainNarrativeSpeaker.X, "X", "La señal de Axl está más adelante."),
                Page(RainNarrativeSpeaker.Zero, "Zero", "Entonces vamos a buscarlo.")
            };
            asset.missionArrivalZero = new[]
            {
                Page(RainNarrativeSpeaker.Zero, "Zero", "La señal de Axl está más adelante."),
                Page(RainNarrativeSpeaker.X, "X", "Entonces vamos a buscarlo.")
            };
            EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }

        private static RainNarrativePage Page(RainNarrativeSpeaker speaker, string name, string text, Texture2D image = null)
            => new RainNarrativePage { speaker = speaker, speakerName = name, text = text, image = image };

        private static Texture2D Texture(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new FileNotFoundException("Falta un recurso narrativo original.", path);
            return texture;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
