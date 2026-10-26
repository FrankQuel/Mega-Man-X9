using System;
using System.IO;
using System.Linq;
using MegaManX9.Gameplay;
using UnityEditor;
using UnityEngine;

namespace MegaManX9.EditorTools
{
    /// <summary>Copies supplied pixels into subassets. The source PNG and its importer are left untouched.</summary>
    public static class RainPauseAssetsBuilder
    {
        public const string AssetPath = "Assets/Recursos/Resources/RainPauseAssets.asset";
        public const string SourcePath = "Assets/Recursos/UI/Menus/PauseMenu/menu de pausa.png";
        private readonly struct SourceGlyph
        {
            public readonly char Character; public readonly int X, Y, Width, Height; public readonly string Label;
            public SourceGlyph(char c, int x, int y, int width, int height, string label)
            { Character = c; X = x; Y = y; Width = width; Height = height; Label = label; }
        }
        // Rectangles use top-left source coordinates. The Latin labels were inspected
        // directly; Q is absent from the sheet and is not invented or aliased to O.
        private static readonly SourceGlyph[] Letters =
        {
            new SourceGlyph('A',276,99,8,11,"MAGMA BLADE/A"), new SourceGlyph('B',280,51,7,11,"X-BUSTER/B"),
            new SourceGlyph('C',271,83,7,11,"ICE BURST/C"), new SourceGlyph('D',305,131,8,11,"GROUND DASH/D"),
            new SourceGlyph('E',309,51,7,11,"X-BUSTER/E"), new SourceGlyph('F',278,260,6,11,"LIFE RECOVER/F"),
            new SourceGlyph('G',268,131,7,11,"GROUND DASH/G"), new SourceGlyph('H',340,131,7,11,"GROUND DASH/H"),
            new SourceGlyph('I',268,83,3,11,"ICE BURST/I"), new SourceGlyph('J',317,425,6,11,"RAKUKOJIN/J"),
            new SourceGlyph('K',334,195,7,11,"GIGA ATTACK/K"), new SourceGlyph('L',297,115,7,11,"METAL ANCHOR/L"),
            new SourceGlyph('M',268,99,8,11,"MAGMA BLADE/M"), new SourceGlyph('N',352,67,8,11,"YAMMAR OPTION/N"),
            new SourceGlyph('O',320,67,7,11,"YAMMAR OPTION/O"), new SourceGlyph('P',327,67,7,11,"YAMMAR OPTION/P"),
            new SourceGlyph('R',316,51,7,11,"X-BUSTER/R"), new SourceGlyph('S',295,51,7,11,"X-BUSTER/S"),
            new SourceGlyph('T',302,51,7,11,"X-BUSTER/T"), new SourceGlyph('U',287,51,8,11,"X-BUSTER/U"),
            new SourceGlyph('V',323,212,8,11,"HYPERDRIVE/V"), new SourceGlyph('W',282,228,9,11,"POWERDRIVE/W"),
            new SourceGlyph('X',268,51,8,11,"X-BUSTER/X"), new SourceGlyph('Y',268,67,7,11,"YAMMAR OPTION/Y"),
            new SourceGlyph('Z',272,361,7,11,"Z-SABER/Z"), new SourceGlyph('-',276,51,4,11,"X-BUSTER/hyphen"),
            new SourceGlyph('.',277,276,3,11,"W.RECOVER/period")
        };

        [MenuItem("Mega Man X9/Playable Rain Level/Build Pause Assets", priority = 8)]
        private static void BuildMenu() => Build();
        public static RainPauseAssets Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Construye la pausa fuera de Play.");
            var original = AssetDatabase.LoadAssetAtPath<Texture2D>(SourcePath);
            if (original == null) { AssetDatabase.ImportAsset(SourcePath, ImportAssetOptions.ForceSynchronousImport); original = AssetDatabase.LoadAssetAtPath<Texture2D>(SourcePath); }
            if (original == null) throw new FileNotFoundException("Falta la hoja original de pausa.", SourcePath);
            if (!AssetDatabase.IsValidFolder("Assets/Recursos/Resources")) AssetDatabase.CreateFolder("Assets/Recursos", "Resources");
            var asset = AssetDatabase.LoadAssetAtPath<RainPauseAssets>(AssetPath);
            if (asset == null) { asset = ScriptableObject.CreateInstance<RainPauseAssets>(); AssetDatabase.CreateAsset(asset, AssetPath); }
            var readable = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                readable.LoadImage(File.ReadAllBytes(SourcePath));
                asset.sourceSheet = original;
                asset.artwork = Texture(asset, "Pause Original Pixels", readable.width, readable.height);
                asset.artwork.SetPixels32(readable.GetPixels32()); asset.artwork.Apply(false, false);
                asset.fontAtlas = Texture(asset, "Pause Bitmap Glyphs", 128, 80);
                asset.fontAtlas.SetPixels32(new Color32[128 * 80]); asset.glyphs.Clear();
                int index = 0;
                foreach (var letter in Letters) AddGlyph(asset, readable, letter, index++);
                for (int digit = 0; digit < 10; digit++)
                {
                    int x = digit == 1 ? 1054 : 1033 + (digit % 5) * 16;
                    int width = digit == 1 ? 7 : 12;
                    AddGlyph(asset, readable, new SourceGlyph((char)('0' + digit), x, digit < 5 ? 270 : 286, width, 14, "Original numeric row/" + digit), index++);
                }
                asset.fontAtlas.Apply(false, false); asset.Invalidate();
                asset.xPanel = Sprite(asset, "Pause X Panel", new RectInt(687,18,320,240));
                asset.zeroPanel = Sprite(asset, "Pause Zero Panel", new RectInt(687,328,320,240));
                asset.healthFrame = Sprite(asset, "Pause Health Frame", new RectInt(1158,279,110,18));
                asset.cursor = Sprite(asset, "Pause Cursor", new RectInt(12,50,17,18));
                asset.xPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Recursos/Sprites/Mugshot/x.png");
                asset.zeroPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Recursos/Sprites/Mugshot/zero.png");
                var front = RainFrontEndAssets.Load();
                if (front != null) { asset.xLifeIcon = front.blueLifeIcon; asset.zeroLifeIcon = front.redLifeIcon; }
                foreach (string label in new[] { "PAUSA", "X", "ZERO", "ACTIVO", "RESERVA", "VIDA", "VIDAS COMPARTIDAS", "PUNTOS", "TIEMPO", "ENEMIGOS", "PUNTO DE CONTROL", "CONTINUAR", "REINICIAR", "MENU PRINCIPAL", "FLECHAS PARA ELEGIR", "ENTER ACEPTAR", "P ESC CONTINUAR" })
                    if (!asset.SupportsText(label)) throw new InvalidDataException("La fuente de pausa no cubre: " + label);
                EditorUtility.SetDirty(asset); EditorUtility.SetDirty(asset.artwork); EditorUtility.SetDirty(asset.fontAtlas);
                AssetDatabase.SaveAssets();
                return asset;
            }
            finally { UnityEngine.Object.DestroyImmediate(readable); }
        }
        private static Texture2D Texture(RainPauseAssets owner, string name, int width, int height)
        {
            var texture = AssetDatabase.LoadAllAssetsAtPath(AssetPath).OfType<Texture2D>().FirstOrDefault(t => t.name == name);
            if (texture == null) { texture = new Texture2D(width, height, TextureFormat.RGBA32, false); texture.name = name; AssetDatabase.AddObjectToAsset(texture, owner); }
            else if (texture.width != width || texture.height != height) texture.Reinitialize(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp; return texture;
        }
        private static Sprite Sprite(RainPauseAssets owner, string name, RectInt topLeft)
        {
            var rect = new Rect(topLeft.x, owner.artwork.height - topLeft.y - topLeft.height, topLeft.width, topLeft.height);
            var sprite = AssetDatabase.LoadAllAssetsAtPath(AssetPath).OfType<Sprite>().FirstOrDefault(s => s.name == name);
            if (sprite != null && sprite.texture == owner.artwork && sprite.rect == rect) return sprite;
            if (sprite != null) UnityEngine.Object.DestroyImmediate(sprite, true);
            sprite = UnityEngine.Sprite.Create(owner.artwork, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            sprite.name = name; AssetDatabase.AddObjectToAsset(sprite, owner); return sprite;
        }
        private static void AddGlyph(RainPauseAssets owner, Texture2D source, SourceGlyph letter, int index)
        {
            int left = index % 8 * 16 + 1, bottom = index / 8 * 16 + 1;
            for (int y = 0; y < letter.Height; y++)
            for (int x = 0; x < letter.Width; x++)
            {
                Color pixel = source.GetPixel(letter.X + x, source.height - letter.Y - letter.Height + y);
                float alpha = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)) * pixel.a;
                owner.fontAtlas.SetPixel(left + x, bottom + y, new Color(1, 1, 1, alpha));
            }
            owner.glyphs.Add(new RainPauseAssets.Glyph { character = letter.Character.ToString(), sourceLabel = letter.Label,
                sourceTopLeftRect = new RectInt(letter.X, letter.Y, letter.Width, letter.Height), atlasRect = new Rect(left, bottom, letter.Width, letter.Height) });
        }
    }
}
