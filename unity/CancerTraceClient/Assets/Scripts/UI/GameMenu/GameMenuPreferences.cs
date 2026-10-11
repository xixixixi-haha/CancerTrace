using UnityEngine;

namespace CancerTrace.UI.GameMenu
{
    /// <summary>
    /// Application-level SFX preferences. These values intentionally stay outside formal and Tutorial saves.
    /// </summary>
    public static class GameMenuPreferences
    {
        private const string SfxEnabledKey = "cancertrace.settings.sfx_enabled";
        private const string SfxVolumeKey = "cancertrace.settings.sfx_volume";
        private const float DefaultSfxVolume = 0.7f;

        public static bool SfxEnabled
        {
            get { return PlayerPrefs.GetInt(SfxEnabledKey, 1) != 0; }
            set
            {
                PlayerPrefs.SetInt(SfxEnabledKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static float SfxVolume
        {
            get { return Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume)); }
            set
            {
                PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// The value future SFX playback should use. Disabling SFX does not overwrite the chosen slider value.
        /// </summary>
        public static float EffectiveSfxVolume
        {
            get { return SfxEnabled ? SfxVolume : 0f; }
        }
    }
}
