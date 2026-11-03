using System;
using CancerTrace.UI.GameMenu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CancerTrace.EditorTools
{
    /// <summary>
    /// Applies the Phase 3D-0E BGM changes without rebuilding the frozen game-menu layout.
    /// </summary>
    public static class GameMenuBgmPrefabUpdater
    {
        private const string AudioPath = "Assets/Audio/BGM/forget_me_not_in_f_major_looped.ogg";
        private const string PrefabPath = "Assets/Resources/UI/GameMenuOverlay.prefab";
        private const float DefaultBgmVolume = 0.15f;

        [MenuItem("CancerTrace/Game Menu/Apply BGM Incremental Update")]
        public static void ApplyIncrementalUpdate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play Mode before updating the game-menu BGM.");
            }

            ConfigureAudioImporter();
            AudioClip clip = LoadRequired<AudioClip>(AudioPath);

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform canvas = RequirePath(root.transform, "GameMenuCanvas");
                Require(root.activeSelf, "GameMenuOverlay prefab root must remain active.");
                Require(canvas.localScale == Vector3.one, "GameMenuCanvas local scale must remain (1,1,1).");

                GameMenuController controller = RequireComponent<GameMenuController>(root);
                AudioSource audioSource = root.GetComponent<AudioSource>();
                if (audioSource == null) audioSource = root.AddComponent<AudioSource>();

                AudioListener audioListener = root.GetComponent<AudioListener>();
                if (audioListener == null) audioListener = root.AddComponent<AudioListener>();

                audioSource.clip = clip;
                audioSource.playOnAwake = false;
                audioSource.loop = true;
                audioSource.spatialBlend = 0f;
                audioSource.volume = DefaultBgmVolume;
                audioListener.enabled = false;

                Transform panel = RequirePath(canvas, "GameMenuOverlay/SettingsPanel");
                RequireComponent<TMP_Text>(RequirePath(panel, "VolumeLabel").gameObject).text = "音乐音量";
                RequireComponent<TMP_Text>(RequirePath(panel, "SfxCheckboxLabel").gameObject).text = "开启音乐";
                RequireComponent<TMP_Text>(RequirePath(panel, "VolumePercentText").gameObject).text = "15%";
                RequireComponent<Slider>(RequirePath(panel, "VolumeSlider").gameObject)
                    .SetValueWithoutNotify(DefaultBgmVolume);

                SerializedObject serialized = new SerializedObject(controller);
                SetObject(serialized, "bgmAudioSource", audioSource);
                SetObject(serialized, "bgmAudioListener", audioListener);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Require(root.activeSelf, "BGM update must not deactivate the prefab root.");
                Require(canvas.localScale == Vector3.one, "BGM update must not alter the canvas scale.");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateBgmSetup();
            Debug.Log("Game menu BGM incremental update completed. No Scene was modified.");
        }

        [MenuItem("CancerTrace/Game Menu/Validate BGM Setup")]
        public static void ValidateBgmSetup()
        {
            AudioClip clip = LoadRequired<AudioClip>(AudioPath);
            Require(clip.frequency == 44100, "BGM sample rate must be 44100 Hz.");
            Require(clip.channels == 2, "BGM must remain stereo.");
            Require(Mathf.Abs(clip.length - 124.1f) < 0.05f, "BGM duration must remain 124.100 seconds.");

            AudioImporter importer = AssetImporter.GetAtPath(AudioPath) as AudioImporter;
            Require(importer != null, "BGM AudioImporter is unavailable.");
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            Require(settings.loadType == AudioClipLoadType.Streaming, "BGM Load Type must be Streaming.");
            Require(settings.compressionFormat == AudioCompressionFormat.Vorbis,
                "BGM Compression Format must be Vorbis.");
            Require(settings.sampleRateSetting == AudioSampleRateSetting.PreserveSampleRate,
                "BGM sample rate must be preserved.");
            Require(!importer.forceToMono, "BGM must not be forced to mono.");

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform canvas = RequirePath(root.transform, "GameMenuCanvas");
                Transform panel = RequirePath(canvas, "GameMenuOverlay/SettingsPanel");
                GameMenuController controller = RequireComponent<GameMenuController>(root);
                AudioSource audioSource = RequireComponent<AudioSource>(root);
                AudioListener audioListener = RequireComponent<AudioListener>(root);

                Require(root.activeSelf, "GameMenuOverlay prefab root must remain active.");
                Require(canvas.localScale == Vector3.one, "GameMenuCanvas local scale must remain (1,1,1).");
                Require(audioSource.clip == clip, "Prefab AudioSource does not reference the BGM AudioClip.");
                Require(audioSource.loop, "BGM AudioSource must loop.");
                Require(!audioSource.playOnAwake, "BGM AudioSource Play On Awake must be disabled.");
                Require(Mathf.Approximately(audioSource.spatialBlend, 0f), "BGM AudioSource must be 2D.");
                Require(Mathf.Approximately(audioSource.volume, DefaultBgmVolume),
                    "BGM AudioSource default volume must be 0.15.");
                Require(!audioListener.enabled,
                    "Prefab AudioListener must start disabled so runtime can avoid duplicate listeners.");

                SerializedObject serialized = new SerializedObject(controller);
                Require(serialized.FindProperty("bgmAudioSource").objectReferenceValue == audioSource,
                    "GameMenuController BGM AudioSource reference is invalid.");
                Require(serialized.FindProperty("bgmAudioListener").objectReferenceValue == audioListener,
                    "GameMenuController BGM AudioListener reference is invalid.");

                Require(RequireComponent<TMP_Text>(RequirePath(panel, "VolumeLabel").gameObject).text == "音乐音量",
                    "Volume label must read 音乐音量.");
                Require(RequireComponent<TMP_Text>(RequirePath(panel, "SfxCheckboxLabel").gameObject).text == "开启音乐",
                    "Checkbox label must read 开启音乐.");
                Require(RequireComponent<TMP_Text>(RequirePath(panel, "VolumePercentText").gameObject).text == "15%",
                    "Initial volume text must read 15%.");
                Require(Mathf.Approximately(
                        RequireComponent<Slider>(RequirePath(panel, "VolumeSlider").gameObject).value,
                        DefaultBgmVolume),
                    "Initial volume slider value must be 0.15.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log("Game menu BGM validation passed.");
        }

        private static void ConfigureAudioImporter()
        {
            AssetDatabase.ImportAsset(AudioPath, ImportAssetOptions.ForceSynchronousImport);
            AudioImporter importer = AssetImporter.GetAtPath(AudioPath) as AudioImporter;
            Require(importer != null, "Could not import BGM as an AudioClip.");

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 1f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.SaveAndReimport();
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required asset is missing: " + path);
            return asset;
        }

        private static T RequireComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(gameObject.name + " is missing " + typeof(T).Name + ".");
            }

            return component;
        }

        private static Transform RequirePath(Transform root, string path)
        {
            Transform child = root.Find(path);
            if (child == null) throw new InvalidOperationException("Missing prefab path: " + path);
            return child;
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException("Missing serialized property: " + propertyName);
            property.objectReferenceValue = value;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
