using System.Collections;
using CancerTrace.Data;
using UnityEngine;

namespace CancerTrace.Debugging
{
    public sealed class GameDataSmokeTest : MonoBehaviour
    {
        [ContextMenu("Run CancerTrace GameData Smoke Test")]
        public void RunSmokeTest()
        {
            StartCoroutine(RunSmokeTestCoroutine());
        }

        private IEnumerator RunSmokeTestCoroutine()
        {
            GameDataLoadResult loadResult = null;
            GameDataLoader loader = new GameDataLoader();
            yield return loader.Load(value => loadResult = value);

            if (loadResult == null || !loadResult.Success)
            {
                Debug.LogError(
                    "CancerTrace GameData smoke test failed." +
                    (loadResult == null ? string.Empty : "\n" + loadResult.ErrorMessage));
                yield break;
            }

            GameDataContext data = loadResult.Data;
            bool expectedCounts =
                data.Cases.Count == 162 &&
                data.Galaxy.Count == 807 &&
                data.ClassProfiles.Count == 8;
            bool expectedConfig =
                data.Config.CasesPerShift == 10 &&
                data.Config.StartingRp == 200;

            if (!expectedCounts || !expectedConfig)
            {
                Debug.LogError(
                    "CancerTrace GameData smoke test loaded data but found unexpected counts/config.\n" +
                    "Cases: " + data.Cases.Count + "\n" +
                    "Galaxy Nodes: " + data.Galaxy.Count + "\n" +
                    "Classes: " + data.ClassProfiles.Count + "\n" +
                    "Shift Cases: " + data.Config.CasesPerShift + "\n" +
                    "Starting RP: " + data.Config.StartingRp);
                yield break;
            }

            Debug.Log(
                "CancerTrace GameData loaded successfully.\n" +
                "Cases: " + data.Cases.Count + "\n" +
                "Galaxy Nodes: " + data.Galaxy.Count + "\n" +
                "Classes: " + data.ClassProfiles.Count + "\n" +
                "Shift Cases: " + data.Config.CasesPerShift + "\n" +
                "Starting RP: " + data.Config.StartingRp);
        }
    }
}
