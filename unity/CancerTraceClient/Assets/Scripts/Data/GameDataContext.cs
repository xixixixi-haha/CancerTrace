using CancerTrace.Data.Models;
using CancerTrace.Data.Repositories;

namespace CancerTrace.Data
{
    public sealed class GameDataContext
    {
        internal GameDataContext(
            GameCasesDocument gameCases,
            GalaxyNodesDocument galaxyNodes,
            ClassProfilesDocument classProfiles,
            GameConfigData gameConfig)
        {
            Cases = new CaseRepository(gameCases.Cases);
            Galaxy = new GalaxyRepository(galaxyNodes.Nodes);
            ClassProfiles = new ClassProfileRepository(classProfiles.Classes);
            Config = new GameConfigRepository(gameConfig);
        }

        public CaseRepository Cases { get; private set; }
        public GalaxyRepository Galaxy { get; private set; }
        public ClassProfileRepository ClassProfiles { get; private set; }
        public GameConfigRepository Config { get; private set; }
    }

    public sealed class GameDataLoadResult
    {
        private GameDataLoadResult(bool success, GameDataContext data, string errorMessage)
        {
            Success = success;
            Data = data;
            ErrorMessage = errorMessage;
        }

        public bool Success { get; private set; }
        public GameDataContext Data { get; private set; }
        public string ErrorMessage { get; private set; }

        internal static GameDataLoadResult Succeeded(GameDataContext data)
        {
            return new GameDataLoadResult(true, data, null);
        }

        internal static GameDataLoadResult Failed(string errorMessage)
        {
            return new GameDataLoadResult(false, null, errorMessage);
        }
    }
}
