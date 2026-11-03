using UnityEngine;

namespace CancerTrace.UI.GameMenu
{
    /// <summary>
    /// Application-level BGM preferences. These values intentionally stay outside formal and Tutorial saves.
    /// </summary>
    public static class GameMenuPreferences
    {
        private const string BgmEnabledKey = "cancertrace.settings.bgm_enabled";
        private const string BgmVolumeKey = "cancertrace.settings.bgm_volume";
        private const float DefaultBgmVolume = 0.15f;

        public static bool BgmEnabled
        {
            get { return PlayerPrefs.GetInt(BgmEnabledKey, 1) != 0; }
            set
            {
                PlayerPrefs.SetInt(BgmEnabledKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static float BgmVolume
        {
            get { return Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, DefaultBgmVolume)); }
            set
            {
                PlayerPrefs.SetFloat(BgmVolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// The value BGM playback should use. Muting BGM does not overwrite the chosen slider value.
        /// </summary>
        public static float EffectiveBgmVolume
        {
            get { return BgmEnabled ? BgmVolume : 0f; }
        }
    }
}
