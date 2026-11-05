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
    public enum FormalSaveStatus
    {
        NoSave,
        ValidSave,
        InvalidSave
    }

    /// <summary>
    /// Owns the single runtime/service graph used by all formal scenes.
    /// Static data remains read-only; mutable state is owned by GameRuntimeService.
    /// </summary>
    public sealed class CancerTraceApp : MonoBehaviour
    {
        private static CancerTraceApp instance;
        private SaveDataRepository saves;
        private string preservedTutorialProgressPath;

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
        public bool TutorialProgressRecoveryRequired { get; private set; }
        public string TutorialProgressRecoveryError { get; private set; }

        public FormalSaveStatus GetFormalSaveStatus(out string errorMessage)
        {
            if (!IsReady || saves == null)
            {
                errorMessage = "CancerTrace application services are not ready.";
                return FormalSaveStatus.InvalidSave;
            }

            if (!saves.HasSave())
            {
                errorMessage = null;
                return FormalSaveStatus.NoSave;
            }

            SaveLoadResult load = saves.Load();
            if (load.Success)
            {
                errorMessage = null;
                return FormalSaveStatus.ValidSave;
            }

            errorMessage = load.ErrorMessage;
            Debug.LogWarning(
                "Formal save exists but is invalid. Start and Continue remain disabled to protect it.\n" +
                errorMessage);
            return FormalSaveStatus.InvalidSave;
        }

        public CorruptFileRecoveryResult PreserveInvalidFormalSave()
        {
            if (!IsReady || saves == null)
            {
                return CorruptFileRecoveryResult.Failed(
                    "CancerTrace application services are not ready.");
            }
            return saves.PreserveInvalidSave();
        }

        public CorruptFileRecoveryResult RecoverTutorialProgress()
        {
            if (!IsReady || TutorialProgress == null)
            {
                return CorruptFileRecoveryResult.Failed(
                    "CancerTrace Tutorial services are not ready.");
            }
            if (!TutorialProgressRecoveryRequired)
            {
                return CorruptFileRecoveryResult.Failed(
                    "Tutorial progress recovery is not required.");
            }

            string reloadError;
            if (TutorialProgress.TryLoad(out reloadError))
            {
                // The player may have restored a valid file externally, or an earlier
                // recovery may already have preserved it before migration failed.
            }
            else
            {
                CorruptFileRecoveryResult preserve = TutorialProgress.PreserveInvalidProgress();
                if (!preserve.Success) return preserve;
                preservedTutorialProgressPath = preserve.PreservedPath;
            }

            string migrationError;
            if (!TryMigrateTutorialCompletion(TutorialProgress, out migrationError))
            {
                TutorialProgressRecoveryError = migrationError;
                return CorruptFileRecoveryResult.Failed(migrationError, preservedTutorialProgressPath);
            }

            TutorialProgressRecoveryRequired = false;
            TutorialProgressRecoveryError = null;
            return CorruptFileRecoveryResult.Succeeded(preservedTutorialProgressPath);
        }

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
                saves = new SaveDataRepository(
                    loadResult.Data.Cases,
                    loadResult.Data.Config,
                    savePathOverride);
                TutorialProgressRepository tutorialProgress = new TutorialProgressRepository(
                    GetCommandLineValue("-cancerTraceTutorialProgressPath"));
                string progressError;
                bool tutorialProgressLoaded = tutorialProgress.TryLoad(out progressError);
                if (!tutorialProgressLoaded)
                {
                    TutorialProgressRecoveryRequired = true;
                    TutorialProgressRecoveryError = progressError;
                    Debug.LogWarning(
                        "Tutorial progress is invalid. The original file is unchanged and requires explicit player recovery.\n" +
                        progressError);
                }

                if (tutorialProgressLoaded &&
                    !TryMigrateTutorialCompletion(tutorialProgress, out progressError))
                {
                    TutorialProgressRecoveryRequired = true;
                    TutorialProgressRecoveryError = progressError;
                    Debug.LogWarning("Tutorial completion migration needs a retry.\n" + progressError);
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

        private bool TryMigrateTutorialCompletion(
            TutorialProgressRepository tutorialProgress,
            out string errorMessage)
        {
            errorMessage = null;
            if (tutorialProgress.TutorialCompleted || !saves.HasSave()) return true;

            SaveLoadResult legacySave = saves.Load();
            if (!legacySave.Success)
            {
                Debug.LogWarning(
                    "Tutorial progress migration skipped because the formal save could not be read.\n" +
                    legacySave.ErrorMessage);
                return true;
            }
            if (!legacySave.State.TutorialCompleted) return true;

            return tutorialProgress.TryMarkCompleted(out errorMessage);
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
