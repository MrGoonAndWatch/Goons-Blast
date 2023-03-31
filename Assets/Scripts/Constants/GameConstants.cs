using System.Collections.Generic;
using System.IO;

namespace Assets.Scripts.Constants
{
    public static class GameConstants
    {
        // What could possibly go wrong? :)
        public enum LevelIndexes
        {
            Startup = 0,
            MainMenu = 1,
            Game = 2,
            Editor = 3,
        }

        public enum DestructableContents
        {
            Nothing = 0,
            Random = 1,
            FirepowerUp = 2,
            BombsUp = 3,
            RemoteBombs = 4,
        }

        public enum SaveLevelResult
        {
            Success = 1,
            Failure = 2,
            ConfirmOverwrite = 3,
        }

        public enum SaveWindowState
        {
            ParameterWindow = 0,
            ErrorWindow = 1,
            OverwriteWindow = 2,
            SuccessWindow = 3
        }

        public static class SpawnablePrefabs
        {
            public const string PlayerManager = "PlayerManager";
            public const string PlayerController = "PlayerController";
            public const string PowerupSpawner = "Spawner-Powerups";
            
            public const string BasicBomb = "Bombs/Bomb-Basic";
            public const string RemoteBomb = "Bombs/Bomb-Remote";

            public const string PowerupFireUp = "Powerups/Pickup-FireUp";
            public const string PowerupBombUp = "Powerups/Pickup-BombUp";
            public const string PowerupRemote = "Powerups/Pickup-Remote";
            public static string[] Powerups = new[]
            {
                PowerupFireUp,
                PowerupBombUp,
                PowerupRemote
            };
        }

        public class OfficialLevelList
        {
            public List<string> CampaignLevels { get; set; }
            public List<string> VsLevels { get; set; }
        }

        public static class LevelFilePaths
        {
            public const string CustomLevelFolder = "CustomLevels";
            public const string CampaignFolder = "Campaign";
            public const string VersusFolder = "Versus";
            private const string BaseLevelPath = "Levels";
            public static string CampaignLevelResourceFolderPath = $"{BaseLevelPath}/Campaign";
            public static string VsLevelResourceFolderPath = $"{BaseLevelPath}/Versus";
            public static string LevelListFilePath = $"{BaseLevelPath}/LevelList";
        }
    }
}
