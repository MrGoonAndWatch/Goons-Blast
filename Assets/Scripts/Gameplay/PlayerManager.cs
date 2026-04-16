using System;
using System.IO;
using System.Linq;
using Assets.Scripts.Constants;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    private MatchEndMenu _matchEndMenu;
    private TMP_Text _winningPlayerDisplay;

    private PhotonView _photonView;
    private bool _refreshPlayerList = true;

    private MatchRulesManager _matchRulesManager;

    private bool _matchEnded;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();

        // Note: we need these 2 to be non-null for the instance of this script that belongs to the host
        //          because they will be the one sending the RPC to end the match later,
        //          once the menu gets set to inactive it'll be unreachable by FindObjectOfType
        //          and these instances seem to finish their Awake and Start before other instances even get called.
        _matchEndMenu = FindObjectOfType<MatchEndMenu>();
        _winningPlayerDisplay = FindObjectOfType<GameResultTextbox>()?.gameObject.GetComponent<TMP_Text>();
        if (_matchEndMenu != null && _photonView.Owner.IsMasterClient)
            _matchEndMenu.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (!_photonView.IsMine) return;

        if (PhotonNetwork.IsMasterClient)
        {
            LoadLevel();
            var matchSettings = RoomManager.GetMatchSettings();
            var matchSettingsJson = JsonUtility.ToJson(matchSettings, false);
            _photonView.RPC(nameof(SetupMatchRules), RpcTarget.All, matchSettingsJson);
            SetupPlayerSpawns();
            InitializeOnePerGameItems(matchSettings);
        }
    }

    private void Update()
    {
        if (!_photonView.IsMine) return;
        if (_refreshPlayerList) RefreshPlayerList();

        CheckForEndOfMatch();
    }

    [PunRPC]
    private void SetupMatchRules(string matchSettingsJson)
    {
        var matchSettings = JsonUtility.FromJson<GameConstants.MatchSettings>(matchSettingsJson);
        if (matchSettings == null)
        {
            Debug.LogError("Could not load match settings from provided json in RPC call!");
            matchSettings = new GameConstants.MatchSettings();
        }

        // TODO: Get Campaign mode songs or whatever if that ends up being a different list based on match type or whatever.
        GoonsBlastAudioManager.ChangeMusic(GoonsBlastFmodAudioEvents.GetVsSong(matchSettings.SongNumber));

        MatchRulesManager rulesManager;
        switch (matchSettings.MatchType)
        {
            case GameConstants.GameMatchType.Survival:
                rulesManager = gameObject.AddComponent<SurvivorMatchRulesManager>();
                break;
            case GameConstants.GameMatchType.KillCount:
                rulesManager = gameObject.AddComponent<KillMatchRulesManager>();
                ((KillMatchRulesManager) rulesManager).SetKillsToWin(matchSettings.KillsToWin);
                break;
            default:
                rulesManager = gameObject.AddComponent<SurvivorMatchRulesManager>();
                break;
        }
        
        var timer = gameObject.AddComponent<MatchTimer>();
        timer.Init(matchSettings.TimerSeconds > 0, TimeSpan.FromSeconds(matchSettings.TimerSeconds));
        rulesManager.Init(timer);
        _matchRulesManager = rulesManager;
    }

    private void LoadLevel()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            string levelDataJson;
            // TODO: Need support for campaign official maps.
            if (RoomManager.IsOfficialMap())
                levelDataJson = Resources.Load(Path.Join(GameConstants.LevelFilePaths.VsLevelResourceFolderPath, RoomManager.GetMap())).ToString();
            else
                levelDataJson = File.ReadAllText(RoomManager.GetMap());
            _photonView.RPC(nameof(LoadLevelFromData), RpcTarget.OthersBuffered, levelDataJson);
            // TODO: Make this a coroutine or async.
            LoadLevel(levelDataJson);
        }
    }

    [PunRPC]
    private void LoadLevelFromData(string levelDataJson)
    {
        // TODO: Make this a coroutine or async.
        LoadLevel(levelDataJson);
    }

    private void LoadLevel(string levelDataJson)
    {
        var levelData = JsonUtility.FromJson<LevelData>(levelDataJson);
        var levelLoader = FindObjectOfType<LevelLoader>();
        if (levelLoader == null)
            Debug.LogError("Could not find a LevelLoader in the Game scene, cannot load the game!");
        else
            levelLoader.LoadLevel(levelData);
    }
    
    private void RefreshPlayerList()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var numPlayers = (int)PhotonNetwork.CurrentRoom.PlayerCount;
        var playersFound = FindObjectsByType<PlayerController>();
        if (playersFound.Length != numPlayers) return;
        _matchRulesManager.UpdatePlayerList(playersFound);
        _refreshPlayerList = false;
    }
    
    private void CheckForEndOfMatch()
    {
        if (_matchEnded || !PhotonNetwork.IsMasterClient) return;
        
        if(_matchRulesManager.HasMatchEnded())
        {
            _matchEnded = true;
            var endingMessage = _matchRulesManager.ProcessMatchEnd();
            _photonView.RPC(nameof(EndMatch), RpcTarget.All, endingMessage);
        }
    }

    [PunRPC]
    private void EndMatch(string endingMessage)
    {
        if (!PhotonNetwork.IsMasterClient)
            FindObjectOfType<MatchRulesManager>().ProcessMatchEnd();

        var allPlayers = FindObjectsByType<PlayerController>();
        for (var i = 0; i < allPlayers.Length; i++)
            allPlayers[i].EndMatch();
        
        if (_winningPlayerDisplay != null)
            _winningPlayerDisplay.text = endingMessage;
        else
            Debug.LogWarning("WinningPlayerDisplay IS NULL!");
        if(_matchEndMenu != null)
            _matchEndMenu.gameObject.SetActive(true);
        else
            Debug.LogWarning("MatchEndMenu IS NULL!");
    }

    private void SetupPlayerSpawns()
    {
        var allPlayers = PhotonNetwork.PlayerList;
        foreach (var player in allPlayers)
        {
            var spawnPoint = GetSpawnPoint();
            _photonView.RPC(nameof(CreateController), player, spawnPoint);
        }
    }

    [PunRPC]
    private void CreateController(Vector3 spawnPoint)
    {
        PhotonNetwork.Instantiate(GameConstants.SpawnablePrefabs.PlayerController, spawnPoint, Quaternion.identity);
    }

    private static Vector3 GetSpawnPoint()
    {
        var spawnPoints = FindObjectsByType<PlayerSpawn>();
        var leastUsedSpawnPoints = spawnPoints.Min(p => p.TimesUsedInMatch);
        spawnPoints = spawnPoints.Where(p => p.TimesUsedInMatch == leastUsedSpawnPoints).ToArray();
        Vector3 spawnPoint;
        if (spawnPoints.Length == 0)
            spawnPoint = Vector3.zero;
        else
        {
            var spawnPointObj = spawnPoints[Random.Range(0, spawnPoints.Length)];
            spawnPointObj.TimesUsedInMatch++;
            spawnPoint = spawnPointObj.transform.position;
            Debug.Log($"{DateTime.Now.Ticks} Picked spawn point {spawnPoint}.");
        }

        return spawnPoint;
    }

    private void InitializeOnePerGameItems(GameConstants.MatchSettings matchSettings)
    {
        var spawner = PhotonNetwork.Instantiate(GameConstants.SpawnablePrefabs.PowerupSpawner, Vector3.zero, Quaternion.identity).GetComponent<PowerupSpawner>();
        var destructables = FindObjectsByType<Destructable>();
        for (var i = 0; i < destructables.Length; i++)
            destructables[i].SetPowerupSpawner(spawner);

        var suddenDeathManager = PhotonNetwork.Instantiate(GameConstants.SpawnablePrefabs.SuddenDeathManager, Vector3.zero, Quaternion.identity).GetComponent<SuddenDeathManager>();
        suddenDeathManager.Init(matchSettings);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        _refreshPlayerList = true;
    }

    public override void OnPlayerLeftRoom(Player oldPlayer)
    {
        // TODO: TRANSFER MATCH RULES WHEN HOST LEAVES!
        _refreshPlayerList = true;
    }
}
