using UnityEngine;

namespace CancerTrace.UI.GameMenu
{
    internal static class GameMenuBootstrap
    {
        private const string PrefabResourcePath = "UI/GameMenuOverlay";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateHost()
        {
            if (Object.FindObjectOfType<GameMenuController>() != null) return;

            GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogError(
                    "Game menu prefab is missing. Expected Resources/UI/GameMenuOverlay.prefab.");
                return;
            }

            GameObject host = Object.Instantiate(prefab);
            host.name = "GameMenuHost";
        }
    }
}
