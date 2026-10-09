using System;
using Newtonsoft.Json;

namespace CancerTrace.Data.Save
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class TutorialProgressDataV1
    {
        [JsonProperty("version", Required = Required.Always)] public string Version;
        [JsonProperty("tutorialCompleted", Required = Required.Always)] public bool TutorialCompleted;
    }
}
