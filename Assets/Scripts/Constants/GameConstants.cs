using System.Collections.Generic;

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

        public enum GameMatchType
        {
            Survival = 0,
            KillCount = 1,
        }

        public enum SuddenDeathType
        {
            None = 0,
            CannonBalls = 1,
            BombRain = 2
        }

        public enum PlayerDeathSound
        {
            None =  0,
            Bomb = 1
        }

        public enum BombType
        {
            Normal = 0,
            Ice = 1,
            Stun = 2,
        }

        public static class SpawnablePrefabs
        {
            public const string PlayerManager = "PlayerManager";
            public const string PlayerController = "PlayerController";
            public const string PowerupSpawner = "Spawner-Powerups";

            public const string SuddenDeathManager = "SuddenDeath/SuddenDeathManager";
            public const string CannonBall = "SuddenDeath/CannonBall";

            public const string BasicBomb = "Bombs/Bomb-Basic";
            public const string IceBomb = "Bombs/Bomb-Ice";
            public const string StunBomb = "Bombs/Bomb-Stun";

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
            public List<string> CampaignLevels;
            public List<string> VsLevels;
        }

        public class ConfigSettings
        {
            public string Username;
            public bool InvertXAxisLook;
            public bool InvertYAxisLook;
            public float? SoundMasterVolume;
            public float? SoundMusicVolume;
            public float? SoundSfxVolume;
        }

        public class MatchSettings
        {
            public GameMatchType MatchType;
            public int TimerSeconds;
            public int KillsToWin;
            public SuddenDeathType SuddenDeathType;
            public int SuddenDeathStartsAt;
            public bool RunBombTimerWhenHeld;
            public bool AllowDetonationsWhenHeld;
            public int SongNumber;
            public byte MaxPlayers;
        }

        public const string ConfigSettingsFilename = "goonsblast.settings";

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

        public static class AnimationVariables
        {
            public const string PickingUp = "PickingUp";
            public const string PlacingBomb = "PlacingBomb";
            public const string SpawningHeldBomb = "SpawningHeldBomb";

            public const string DestructibleBlockHasItem = "HasItemInside";
        }

        public static class RoomCustomProperties
        {
            public const string MatchSettings = "s";
            public const string MatchMap = "m";
        }
    }
}
