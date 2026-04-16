using System.IO;
using Photon.Pun;
using Assets.Scripts.Constants;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    private string _selectedMapFilepath;
    private bool _officialMap;
    private GameConstants.ConfigSettings _configSettings;
    private GameConstants.MatchSettings _matchSettings;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"Found a second RoomManger instance, destroying original instance with instance id {Instance.GetInstanceID()}");
            Destroy(Instance.gameObject);
        }

        LoadSettings();

        _matchSettings = new GameConstants.MatchSettings
        {
            MatchType = GameConstants.GameMatchType.Survival,
            TimerSeconds = 0,
            KillsToWin = 0,
            SuddenDeathType = GameConstants.SuddenDeathType.None,
            SuddenDeathStartsAt = 0
        };

        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    private static string GetConfigSettingsFilepath()
    {
        return Path.Join(Application.persistentDataPath, GameConstants.ConfigSettingsFilename);
    }

    private void LoadSettings()
    {
        var configSettingsFilepath = GetConfigSettingsFilepath();
        if (File.Exists(configSettingsFilepath))
        {
            var configJson = File.ReadAllText(configSettingsFilepath);
            _configSettings = JsonUtility.FromJson<GameConstants.ConfigSettings>(configJson);
        }
        else
            _configSettings = new GameConstants.ConfigSettings
            {
                Username = "Tony Swan"
            };
    }

    public static GameConstants.ConfigSettings GetConfigSettings()
    {
        if (Instance == null)
        {
            Debug.LogError("Cannot get config settings, no RoomManager instance found!");
            return null;
        }

        return Instance._configSettings;
    }

    public static void SaveSettings(GameConstants.ConfigSettings newSettings)
    {
        if (Instance == null)
            Debug.LogError("Cannot update settings data, no RoomManager instance was found!");
        else
            Instance._configSettings = newSettings;

        var configSettingsFilepath = GetConfigSettingsFilepath();
        
        var configJson = JsonUtility.ToJson(newSettings, false);
        File.WriteAllText(configSettingsFilepath, configJson);
    }

    public static GameConstants.MatchSettings GetMatchSettings()
    {
        if (Instance == null)
        {
            Debug.LogError("Cannot get config settings, no RoomManager instance found!");
            return null;
        }

        return Instance._matchSettings;
    }

    public static void SaveMatchSettings(GameConstants.MatchSettings newSettings)
    {
        if (Instance == null)
            Debug.LogError("Cannot update settings data, no RoomManager instance was found!");
        else
            Instance._matchSettings = newSettings;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static string GetMap()
    {
        if (Instance == null)
        {
            Debug.LogError("Cannot GetMap. No RoomManager instance found!");
            return null;
        }

        return Instance._selectedMapFilepath;
    }

    public static bool IsOfficialMap()
    {
        if (Instance == null)
        {
            Debug.LogError("Cannot Check if IsOfficialMap. No RoomManager instance found!");
            return false;
        }

        return Instance._officialMap;
    }

    public static void ClearMap()
    {
        SetMap("", false);
    }

    public static void SetMap(string mapFilepath, bool officialMap)
    {
        if (Instance == null)
        {
            Debug.LogError("Cannot SetMap. No RoomManager instance found!");
            return;
        }
        Instance._selectedMapFilepath = mapFilepath;
        Instance._officialMap = officialMap;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.buildIndex == (int) GameConstants.LevelIndexes.Game)
            PhotonNetwork.Instantiate(GameConstants.SpawnablePrefabs.PlayerManager, Vector3.zero, Quaternion.identity);
        else if(scene.buildIndex != 0)
            GoonsBlastAudioManager.ChangeMusic(GoonsBlastFmodAudioEvents.MenuSong);
    }
}
