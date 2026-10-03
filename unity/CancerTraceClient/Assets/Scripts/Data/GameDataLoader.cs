using System;
using System.Collections;
using System.IO;
using System.Text;
using CancerTrace.Data.Models;
using CancerTrace.Data.Validation;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace CancerTrace.Data
{
    public sealed class GameDataLoader
    {
        private const string GameDataDirectory = "GameData";
        private const string GameCasesFile = "game_cases.json";
        private const string GalaxyNodesFile = "galaxy_nodes.json";
        private const string ClassProfilesFile = "class_profiles.json";
        private const string GameConfigFile = "game_config.json";

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            NullValueHandling = NullValueHandling.Include,
            FloatParseHandling = FloatParseHandling.Double,
            DateParseHandling = DateParseHandling.None
        };

        public bool IsLoaded { get; private set; }
        public GameDataContext Data { get; private set; }

        public IEnumerator Load(Action<GameDataLoadResult> onCompleted)
        {
            IsLoaded = false;
            Data = null;

            TextReadResult gameCasesText = null;
            yield return ReadTextFile(GameCasesFile, value => gameCasesText = value);
            if (!gameCasesText.Success)
            {
                CompleteFailure(gameCasesText.ErrorMessage, onCompleted);
                yield break;
            }

            TextReadResult galaxyNodesText = null;
            yield return ReadTextFile(GalaxyNodesFile, value => galaxyNodesText = value);
            if (!galaxyNodesText.Success)
            {
                CompleteFailure(galaxyNodesText.ErrorMessage, onCompleted);
                yield break;
            }

            TextReadResult classProfilesText = null;
            yield return ReadTextFile(ClassProfilesFile, value => classProfilesText = value);
            if (!classProfilesText.Success)
            {
                CompleteFailure(classProfilesText.ErrorMessage, onCompleted);
                yield break;
            }

            TextReadResult gameConfigText = null;
            yield return ReadTextFile(GameConfigFile, value => gameConfigText = value);
            if (!gameConfigText.Success)
            {
                CompleteFailure(gameConfigText.ErrorMessage, onCompleted);
                yield break;
            }

            GameCasesDocument gameCases;
            string parseError;
            if (!TryDeserialize(GameCasesFile, gameCasesText.Text, out gameCases, out parseError))
            {
                CompleteFailure(parseError, onCompleted);
                yield break;
            }

            GalaxyNodesDocument galaxyNodes;
            if (!TryDeserialize(GalaxyNodesFile, galaxyNodesText.Text, out galaxyNodes, out parseError))
            {
                CompleteFailure(parseError, onCompleted);
                yield break;
            }

            ClassProfilesDocument classProfiles;
            if (!TryDeserialize(ClassProfilesFile, classProfilesText.Text, out classProfiles, out parseError))
            {
                CompleteFailure(parseError, onCompleted);
                yield break;
            }

            GameConfigData gameConfig;
            if (!TryDeserialize(GameConfigFile, gameConfigText.Text, out gameConfig, out parseError))
            {
                CompleteFailure(parseError, onCompleted);
                yield break;
            }

            DataValidationResult validation =
                DataValidator.Validate(gameCases, galaxyNodes, classProfiles, gameConfig);
            if (!validation.IsValid)
            {
                StringBuilder message = new StringBuilder();
                message.Append("CancerTrace GameData validation failed with ");
                message.Append(validation.Errors.Count);
                message.Append(" error(s):");
                for (int index = 0; index < validation.Errors.Count; index++)
                {
                    message.AppendLine();
                    message.Append("- ");
                    message.Append(validation.Errors[index]);
                }

                CompleteFailure(message.ToString(), onCompleted);
                yield break;
            }

            Data = new GameDataContext(gameCases, galaxyNodes, classProfiles, gameConfig);
            IsLoaded = true;
            GameDataLoadResult success = GameDataLoadResult.Succeeded(Data);
            if (onCompleted != null)
            {
                onCompleted(success);
            }
        }

        private static IEnumerator ReadTextFile(string fileName, Action<TextReadResult> onCompleted)
        {
            string path = Path.Combine(Application.streamingAssetsPath, GameDataDirectory, fileName);

            if (path.IndexOf("://", StringComparison.Ordinal) < 0)
            {
                if (!File.Exists(path))
                {
                    onCompleted(TextReadResult.Failed(
                        "CancerTrace GameData file does not exist: " + path));
                    yield break;
                }

                try
                {
                    onCompleted(TextReadResult.Succeeded(File.ReadAllText(path, Encoding.UTF8)));
                }
                catch (Exception exception)
                {
                    onCompleted(TextReadResult.Failed(
                        "CancerTrace GameData file could not be read: " + path +
                        Environment.NewLine + exception));
                }

                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(path))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    onCompleted(TextReadResult.Failed(
                        "CancerTrace GameData file could not be read: " + path +
                        Environment.NewLine + request.error));
                    yield break;
                }

                onCompleted(TextReadResult.Succeeded(request.downloadHandler.text));
            }
        }

        private static bool TryDeserialize<T>(
            string fileName,
            string json,
            out T value,
            out string errorMessage)
            where T : class
        {
            try
            {
                value = JsonConvert.DeserializeObject<T>(json, SerializerSettings);
                if (value == null)
                {
                    errorMessage = "CancerTrace GameData JSON produced a null root object: " + fileName;
                    return false;
                }

                errorMessage = null;
                return true;
            }
            catch (JsonException exception)
            {
                value = null;
                errorMessage =
                    "CancerTrace GameData JSON parse/mapping failed: " + fileName +
                    Environment.NewLine + exception;
                return false;
            }
            catch (Exception exception)
            {
                value = null;
                errorMessage =
                    "Unexpected CancerTrace GameData load failure: " + fileName +
                    Environment.NewLine + exception;
                return false;
            }
        }

        private static void CompleteFailure(
            string errorMessage,
            Action<GameDataLoadResult> onCompleted)
        {
            Debug.LogError(errorMessage);
            if (onCompleted != null)
            {
                onCompleted(GameDataLoadResult.Failed(errorMessage));
            }
        }

        private sealed class TextReadResult
        {
            private TextReadResult(bool success, string text, string errorMessage)
            {
                Success = success;
                Text = text;
                ErrorMessage = errorMessage;
            }

            public bool Success { get; private set; }
            public string Text { get; private set; }
            public string ErrorMessage { get; private set; }

            public static TextReadResult Succeeded(string text)
            {
                return new TextReadResult(true, text, null);
            }

            public static TextReadResult Failed(string errorMessage)
            {
                return new TextReadResult(false, null, errorMessage);
            }
        }
    }
}
