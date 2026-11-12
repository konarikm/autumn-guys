using UnityEditor;
using UnityEngine;

namespace AutumnGuys.EditorTools
{
    /// <summary>
    /// Import settings for game audio: music streams from disk (small memory footprint), sound effects are
    /// decompressed on load (no latency) and forced to mono since all effects are 2D.
    /// </summary>
    public class AudioImportSettings : AssetPostprocessor
    {
        const string MusicDir = "Assets/_Game/Resources/Audio/Music/";
        const string SfxDir = "Assets/_Game/Resources/Audio/Sfx/";

        void OnPreprocessAudio()
        {
            bool isMusic = assetPath.StartsWith(MusicDir);
            bool isSfx = assetPath.StartsWith(SfxDir);
            if (!isMusic && !isSfx) return;

            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            if (isMusic)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.quality = 0.6f;
                importer.forceToMono = false;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.quality = 0.7f;
                importer.forceToMono = true;
            }
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = isMusic;
        }
    }
}
