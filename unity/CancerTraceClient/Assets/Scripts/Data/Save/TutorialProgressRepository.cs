using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace CancerTrace.Data.Save
{
    /// <summary>
    /// Stores application-level Tutorial completion independently from formal Shift saves.
    /// </summary>
    public sealed class TutorialProgressRepository
    {
        public const string ProgressVersion = "1.0";
        public const string DefaultFileName = "cancertrace_tutorial_progress_v1.json";

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            NullValueHandling = NullValueHandling.Include,
            DateParseHandling = DateParseHandling.None
        };

        public TutorialProgressRepository(string progressPathOverride = null)
        {
            ProgressPath = string.IsNullOrWhiteSpace(progressPathOverride)
                ? Path.Combine(Application.persistentDataPath, DefaultFileName)
                : progressPathOverride;
        }

        public string ProgressPath { get; private set; }
        public bool TutorialCompleted { get; private set; }

        public bool TryLoad(out string errorMessage)
        {
            if (!File.Exists(ProgressPath))
            {
                TutorialCompleted = false;
                errorMessage = null;
                return true;
            }

            try
            {
                string json = File.ReadAllText(ProgressPath, Encoding.UTF8);
                TutorialProgressDataV1 data =
                    JsonConvert.DeserializeObject<TutorialProgressDataV1>(json, SerializerSettings);
                if (data == null)
                {
                    errorMessage = "Tutorial progress JSON produced a null root object: " + ProgressPath;
                    return false;
                }
                if (!string.Equals(data.Version, ProgressVersion, StringComparison.Ordinal))
                {
                    errorMessage =
                        "Unsupported Tutorial progress version at " + ProgressPath +
                        ": " + data.Version;
                    return false;
                }

                TutorialCompleted = data.TutorialCompleted;
                errorMessage = null;
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = "Tutorial progress load failed at " + ProgressPath + Environment.NewLine + exception;
                return false;
            }
        }

        public bool TryMarkCompleted(out string errorMessage)
        {
            if (TutorialCompleted)
            {
                errorMessage = null;
                return true;
            }

            TutorialProgressDataV1 data = new TutorialProgressDataV1
            {
                Version = ProgressVersion,
                TutorialCompleted = true
            };

            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented, SerializerSettings);
                WriteAtomically(json);
                TutorialCompleted = true;
                errorMessage = null;
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = "Tutorial progress save failed at " + ProgressPath + Environment.NewLine + exception;
                return false;
            }
        }

        private void WriteAtomically(string json)
        {
            string directory = Path.GetDirectoryName(ProgressPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string temporaryPath = ProgressPath + ".tmp";
            string backupPath = ProgressPath + ".bak";
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
            try
            {
                if (File.Exists(ProgressPath))
                {
                    try
                    {
                        DeleteIfPresent(backupPath);
                        File.Replace(temporaryPath, ProgressPath, backupPath, true);
                        DeleteIfPresent(backupPath);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(temporaryPath, ProgressPath, true);
                        File.Delete(temporaryPath);
                    }
                    catch (NotSupportedException)
                    {
                        File.Copy(temporaryPath, ProgressPath, true);
                        File.Delete(temporaryPath);
                    }
                }
                else
                {
                    File.Move(temporaryPath, ProgressPath);
                }
            }
            finally
            {
                DeleteIfPresent(temporaryPath);
            }
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
