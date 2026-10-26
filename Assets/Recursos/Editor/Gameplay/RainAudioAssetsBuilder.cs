using System;
using System.Collections.Generic;
using System.IO;
using MegaManX9.Gameplay;
using UnityEditor;
using UnityEngine;

namespace MegaManX9.EditorTools
{
    /// <summary>Idempotent named-recording bank. Reimports only when settings differ.</summary>
    public static class RainAudioAssetsBuilder
    {
        public const string AssetPath = "Assets/Recursos/Resources/RainAudioAssets.asset";
        public const string ReportPath = "Tools/AudioPipeline/audio_build_report.json";
        public const int ExpectedWavCount = 33;
        public const int ExpectedImportedClipCount = 37;
        private sealed class Source
        {
            public readonly string cue, path; public readonly bool loop; public readonly float gain;
            public Source(string name, string relativePath, bool looping = false, float volume = 1)
            { cue = name; path = "Assets/Recursos/Audio/" + relativePath; loop = looping; gain = volume; }
        }
        private static readonly Source[] Sources =
        {
            new Source("mainTheme", "Music/Theme_Title.wav", true, .90f),
            new Source("levelTheme", "Music/Theme_Stage.wav", true, .72f),
            new Source("dragonTheme", "Music/Theme_DragoonBoss.wav", true, 1),
            new Source("axlTheme", "Music/Theme_AxlBoss.wav", true, .85f),
            new Source("conversationTheme", "Music/Theme_Conversation.wav", true, 1),
            new Source("resultsTheme", "Music/Theme_Results.wav", true, .90f),
            new Source("rain", "Ambience/AMB_Rain.wav", true, 1),
            new Source("jump", "SFX/SFX_Jump.wav", false, 1),
            new Source("dash", "SFX/SFX_DashGround.wav", false, 1),
            new Source("dashAlternate", "SFX/SFX_DashAir.wav", false, 1),
            new Source("shot", "SFX/SFX_BusterShot.wav", false, .80f),
            new Source("chargedShot", "SFX/SFX_BusterChargedShot.wav", false, .90f),
            new Source("charge", "SFX/SFX_BusterCharge.wav", false, .55f),
            new Source("saber", "SFX/SFX_SaberSwing.wav", false, .90f),
            new Source("saber1", "SFX/SFX_SaberCombo01.wav", false, .85f),
            new Source("saber2", "SFX/SFX_SaberCombo02.wav", false, .85f),
            new Source("saber3", "SFX/SFX_SaberCombo03.wav", false, .85f),
            new Source("hurt", "SFX/SFX_HeroHurt.wav", false, 1),
            new Source("death", "SFX/SFX_HeroDeath.wav", false, .90f),
            new Source("arrival", "SFX/SFX_HeroArrival.wav", false, .90f),
            new Source("departure", "SFX/SFX_TeleportDeparture.wav", false, 1),
            new Source("pickup", "SFX/SFX_Heal.wav", false, 1),
            new Source("enemyDeath", "SFX/SFX_EnemyDeath.wav", false, .85f),
            new Source("dragonHurt", "SFX/SFX_DragoonHurt.wav", false, .80f),
            new Source("dragonAttack", "SFX/SFX_DragoonAttack.wav", false, .85f),
            new Source("axlHurt", "SFX/SFX_AxlHurt.wav", false, .80f),
            new Source("ready", "SFX/SFX_StageReady.wav", false, .80f),
            new Source("warning", "SFX/SFX_BossWarning.wav", false, .85f),
            new Source("doorOpen", "SFX/SFX_BossDoorOpen.wav", false, .80f),
            new Source("doorClose", "SFX/SFX_BossDoorClose.wav", false, .50f),
            new Source("menuMove", "SFX/SFX_MenuMove.wav", false, .80f),
            new Source("menuConfirm", "SFX/SFX_MenuConfirm.wav", false, .80f),
            new Source("menuEnter", "SFX/SFX_MenuEnter.wav", false, .90f),
            new Source("voiceXSwitchOut", "Voice/X/VO_XSwitchOut.ogg", false, .95f),
            new Source("voiceXSwitchIn", "Voice/X/VO_XSwitchIn.ogg", false, .95f),
            new Source("voiceZeroSwitchOut", "Voice/Zero/VO_ZeroSwitchOut.ogg", false, .95f),
            new Source("voiceZeroSwitchIn", "Voice/Zero/VO_ZeroSwitchIn.ogg", false, .95f)
        };

        [Serializable] private sealed class Report
        {
            public int importedClipCount, wavCount, oggCount;
            public string bankAsset;
            public List<ClipReport> clips = new List<ClipReport>();
            public List<string> missing = new List<string>();
        }
        [Serializable] private sealed class ClipReport
        {
            public string cue, path, loadType; public bool loop;
            public float duration, gain; public int channels, frequency;
        }

        [MenuItem("Tools/Mega Man X9/Build Rain Audio Assets")]
        public static RainAudioAssets Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Recursos/Resources")) AssetDatabase.CreateFolder("Assets/Recursos", "Resources");
            var assets = AssetDatabase.LoadAssetAtPath<RainAudioAssets>(AssetPath);
            if (assets == null) { assets = ScriptableObject.CreateInstance<RainAudioAssets>(); AssetDatabase.CreateAsset(assets, AssetPath); }
            var report = new Report { bankAsset = AssetPath };
            var cues = new List<RainAudioAssets.Cue>();
            foreach (var source in Sources)
            {
                if (!File.Exists(source.path)) { report.missing.Add(source.path); continue; }
                ConfigureImporter(source);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(source.path);
                if (clip == null) { report.missing.Add(source.path); continue; }
                cues.Add(new RainAudioAssets.Cue { name = source.cue, clip = clip, gain = source.gain, loop = source.loop, sourcePath = source.path });
                report.clips.Add(new ClipReport { cue = source.cue, path = source.path, duration = clip.length,
                    channels = clip.channels, frequency = clip.frequency, loop = source.loop, gain = source.gain,
                    loadType = source.loop ? "Streaming" : "DecompressOnLoad" });
                if (source.path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) report.wavCount++; else report.oggCount++;
            }
            assets.cues = cues; assets.musicVolume=.43f;assets.Invalidate(); EditorUtility.SetDirty(assets); AssetDatabase.SaveAssets();
            report.importedClipCount = assets.ImportedClipCount;
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)); File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            if (report.missing.Count > 0) throw new InvalidOperationException("Audio bank missing recordings: " + string.Join(", ", report.missing));
            Debug.Log("Rain audio: " + report.wavCount + " WAV + " + report.oggCount + " OGG; " + assets.ImportedClipCount + " imported clips.", assets);
            return assets;
        }

        private static void ConfigureImporter(Source source)
        {
            var importer = AssetImporter.GetAtPath(source.path) as AudioImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(source.path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(source.path) as AudioImporter;
            }
            if (importer == null) return;
            var previous = importer.defaultSampleSettings; var settings = previous;
            settings.loadType = source.loop ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = source.loop ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = source.loop ? .85f : 1;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = !source.loop;
            bool changed = previous.loadType != settings.loadType || previous.compressionFormat != settings.compressionFormat ||
                !Mathf.Approximately(previous.quality, settings.quality) || previous.sampleRateSetting != settings.sampleRateSetting ||
                previous.preloadAudioData != settings.preloadAudioData || importer.loadInBackground != source.loop || importer.forceToMono || importer.ambisonic;
            if (!changed) return;
            importer.defaultSampleSettings = settings; importer.loadInBackground = source.loop; importer.forceToMono = false; importer.ambisonic = false;
            importer.SaveAndReimport();
        }
    }
}
