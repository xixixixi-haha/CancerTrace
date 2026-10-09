using System;
using System.Collections;
using CancerTrace.Data;
using CancerTrace.Data.Save;
using CancerTrace.Gameplay.Flow;
using CancerTrace.Gameplay.Runtime;
using CancerTrace.Gameplay.Tutorial;
using UnityEngine;

namespace CancerTrace.UI.Controllers
{
    /// <summary>
    /// Owns the single runtime/service graph used by all formal scenes.
    /// Static data remains read-only; mutable state is owned by GameRuntimeService.
    /// </summary>
    public sealed class CancerTraceApp : MonoBehaviour
    {
        private static CancerTraceApp instance;

        public static CancerTraceApp Instance
        {
            get { return EnsureInstance(); }
        }

        public bool IsReady { get; private set; }
        public bool IsLoading { get; private set; }
        public string ErrorMessage { get; private set; }
        public GameplayService Gameplay { get; private set; }
        public TutorialService Tutorial { get; private set; }
        public TutorialProgressRepository TutorialProgress { get; private set; }
        public string SavePath { get; private set; }
        public string TutorialProgressPath { get; private set; }

        public static CancerTraceApp EnsureInstance()
        {
            if (instance != null) return instance;

            instance = FindObjectOfType<CancerTraceApp>();
            if (instance != null) return instance;

            GameObject appObject = new GameObject("CancerTraceApp");
            instance = appObject.AddComponent<CancerTraceApp>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            if (!IsReady && !IsLoading) StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            IsLoading = true;
            ErrorMessage = null;

            GameDataLoadResult loadResult = null;
            GameDataLoader loader = new GameDataLoader();
            yield return loader.Load(value => loadResult = value);

            if (loadResult == null || !loadResult.Success)
            {
                ErrorMessage = loadResult == null
                    ? "游戏数据加载未返回结果。"
                    : loadResult.ErrorMessage;
                IsLoading = false;
                Debug.LogError("CancerTrace UI startup failed: " + ErrorMessage);
                yield break;
            }

            try
            {
                string savePathOverride = GetCommandLineValue("-cancerTraceSavePath");
                SaveDataRepository saves = new SaveDataRepository(
                    loadResult.Data.Cases,
                    loadResult.Data.Config,
                    savePathOverride);
                TutorialProgressRepository tutorialProgress = new TutorialProgressRepository(
                    GetCommandLineValue("-cancerTraceTutorialProgressPath"));
                string progressError;
                if (!tutorialProgress.TryLoad(out progressError))
                {
                    throw new InvalidOperationException(progressError);
                }

                if (!tutorialProgress.TutorialCompleted && saves.HasSave())
                {
                    SaveLoadResult legacySave = saves.Load();
                    if (legacySave.Success && legacySave.State.TutorialCompleted)
                    {
                        if (!tutorialProgress.TryMarkCompleted(out progressError))
                        {
                            throw new InvalidOperationException(
                                "Could not migrate Tutorial completion from the formal save. " + progressError);
                        }
                    }
                    else if (!legacySave.Success)
                    {
                        Debug.LogWarning(
                            "Tutorial progress migration skipped because the formal save could not be read.\n" +
                            legacySave.ErrorMessage);
                    }
                }
                GameRuntimeService runtime = new GameRuntimeService(
                    loadResult.Data.Cases,
                    loadResult.Data.Config,
                    saves);
                Gameplay = new GameplayService(
                    runtime,
                    loadResult.Data.Cases,
                    loadResult.Data.Galaxy,
                    loadResult.Data.ClassProfiles,
                    loadResult.Data.Config);
                Tutorial = new TutorialService(
                    loadResult.Data.Cases,
                    loadResult.Data.Galaxy,
                    loadResult.Data.Config,
                    tutorialProgress);
                TutorialProgress = tutorialProgress;
                SavePath = saves.SavePath;
                TutorialProgressPath = tutorialProgress.ProgressPath;
                IsReady = true;
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.ToString();
                Debug.LogError("CancerTrace UI startup failed: " + exception);
            }

            IsLoading = false;
        }

        private static string GetCommandLineValue(string argumentName)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], argumentName, StringComparison.Ordinal))
                {
                    return arguments[index + 1];
                }
            }
            return null;
        }
    }
}
