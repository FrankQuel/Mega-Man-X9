using System.IO;
using System.Linq;
using MegaManX9.Gameplay;
using UnityEditor;
using UnityEngine;

namespace MegaManX9.EditorTools
{
    public static class RainFrontEndAssetsBuilder
    {
        public const string AssetPath="Assets/Recursos/Resources/RainFrontEndAssets.asset";
        private const string Generated="Assets/Recursos/UI/Menus/Generated";
        [MenuItem("Mega Man X9/Playable Rain Level/Build Menu and Life Art")]
        public static RainFrontEndAssets Build()
        {
            var health=AssetDatabase.LoadAssetAtPath<MegaManX9.UI.HealthHUDAssets>(HealthHUDBuilder.AssetsPath);
            if(health==null||health.lifeCounterRing==null||health.Find("Blue")?.horizontalFrame==null)
                HealthHUDBuilder.Build();
            Directory.CreateDirectory(Generated);Directory.CreateDirectory("Assets/Recursos/Resources");AssetDatabase.Refresh();
            var art=AssetDatabase.LoadAssetAtPath<RainFrontEndAssets>(AssetPath);
            if(art==null){art=ScriptableObject.CreateInstance<RainFrontEndAssets>();AssetDatabase.CreateAsset(art,AssetPath);}
            art.cover=RequireTexture("Assets/Recursos/UI/Menus/MainMenu/Back-to-Back Cybernetic Heroes.png");
            art.title=RequireTexture("Assets/Recursos/UI/Menus/MainMenu/gallery_10621_80_42351.png");
            art.bluePortrait=Crop("Assets/Recursos/UI/Menus/Character Select/Character Select.png","BlueSelect",20,70,160,135,false);
            art.redPortrait=Crop("Assets/Recursos/UI/Menus/Character Select/Character Select.png","RedSelect",180,70,160,135,false);
            art.blueLifeIcon=LifeHead("BlueEnergyPulse");art.redLifeIcon=LifeHead("RedEnergyPulse");
            if(art.blueLifeIcon==null||art.redLifeIcon==null)throw new InvalidDataException("Faltan las cabezas X/Zero de la hoja original de objetos.");
            EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();return art;
        }
        private static Texture2D RequireTexture(string path)
        {
            var value=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(value==null)throw new FileNotFoundException("Falta arte existente para el menú",path);
            return value;
        }
        private static Sprite LifeHead(string clip)=>AssetDatabase.LoadAllAssetsAtPath("Assets/Recursos/Sprites/Items/Pickups/Generated/"+clip+".png")
            .OfType<Sprite>().OrderBy(sprite=>sprite.name,System.StringComparer.Ordinal).FirstOrDefault();
        // Coordinates are top-left source pixels. No source sheet or original import settings are changed.
        private static Texture2D Crop(string source,string name,int x,int y,int width,int height,bool sprite)
        {
            if(!File.Exists(source))throw new FileNotFoundException("Falta la hoja UI original",source);
            var sheet=new Texture2D(2,2,TextureFormat.RGBA32,false);
            if(!ImageConversion.LoadImage(sheet,File.ReadAllBytes(source)))throw new InvalidDataException(source);
            var image=new Texture2D(width,height,TextureFormat.RGBA32,false);
            image.SetPixels(sheet.GetPixels(x,sheet.height-y-height,width,height));image.Apply();
            string path=Generated+"/"+name+".png";File.WriteAllBytes(path,image.EncodeToPNG());
            Object.DestroyImmediate(sheet);Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=sprite?TextureImporterType.Sprite:TextureImporterType.Default;
            importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}

