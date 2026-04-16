using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Constants;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts.MainMenu
{
    public class Launcher : MonoBehaviourPunCallbacks
    {
        public static Launcher Instance;

        [Header("Room Setup / Display")]
        [SerializeField] private TMP_InputField _roomNameInputField;
        [SerializeField] private TMP_Text _errorDisplay;
        [SerializeField] private TMP_Text _roomNameDisplay;
        [SerializeField] private Transform _roomListContent;
        [SerializeField] private GameObject _roomListItemPrefab;
        [SerializeField] private Transform _playerListContent;
        [SerializeField] private GameObject _playerListItemPrefab;
        [Header("Settings Menu")]
        [SerializeField] private TMP_InputField _usernameInput;
        [SerializeField] private Toggle _invertXAxisCheckbox;
        [SerializeField] private Toggle _invertYAxisCheckbox;
        [SerializeField] private Slider _soundMasterVolumeSlider;
        [SerializeField] private Slider _soundMusicVolumeSlider;
        [SerializeField] private Slider _soundSfxVolumeSlider;
        [Header("Match Settings")]
        [SerializeField] private GameObject _startGameButton;
        [SerializeField] private Transform _mapList;
        [SerializeField] private Transform _mapEditList;
        [SerializeField] private GameObject _mapSelectPrefab;
        [SerializeField] private TMP_Text _selectedMapLabel;
        [SerializeField] private TMP_Dropdown _matchTypePicker;
        [SerializeField] private Slider _matchTimer;
        [SerializeField] private Slider _killsToWin;
        [SerializeField] private TMP_Dropdown _suddenDeathPicker;
        [SerializeField] private Slider _suddenDeathTimer;
        [SerializeField] private Toggle _runBombTimerWhenHeldToggle;
        [SerializeField] private Toggle _detonateBombsWhenHeldToggle;
        [SerializeField] private TMP_Dropdown _matchSongPicker;
        [SerializeField] private Slider _maxPlayers;

        private GameConstants.OfficialLevelList _officialLevelList;
        private List<string> _customVsLevels;
        private List<string> _customCampaignLevels;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning($"A second instance on the Launcher script was found! Destroying the original instance with id {Instance.GetInstanceID()}...");
                Destroy(Instance.gameObject);
            }

            Instance = this;
            RefreshSelectedMap("(select a map)");
        }

        private void Start()
        {
            LoadConfigSettings();
            MenuManager.Instance.OpenMenu(MenuType.Loading);
            Debug.Log("Starting Launcher");
            // Connects to Photon master server using /Photon/PhotonUnityNetworking/Resources/PhotonServerSettings
            PhotonNetwork.ConnectUsingSettings();
            StartCoroutine(StartRefreshLevelList());
            ReadMatchSettings();
        }

        private void LoadConfigSettings()
        {
            var configSettings = RoomManager.GetConfigSettings();
            if (configSettings == null)
                return;

            GoonsBlastAudioManager.UpdateVolume(configSettings);
            PhotonNetwork.NickName = configSettings.Username;
            _usernameInput.text = configSettings.Username;
            _invertXAxisCheckbox.isOn = configSettings.InvertXAxisLook;
            _invertYAxisCheckbox.isOn = configSettings.InvertYAxisLook;
            _soundMasterVolumeSlider.value = configSettings.SoundMasterVolume ?? 0.5f;
            _soundMusicVolumeSlider.value = configSettings.SoundMusicVolume ?? 0.5f;
            _soundSfxVolumeSlider.value = configSettings.SoundSfxVolume ?? 0.5f;
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("On Connected To Master");
            // Need to be in lobby to find/create rooms.
            PhotonNetwork.JoinLobby();
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("Joined Lobby");
            MenuManager.Instance.OpenMenu(MenuType.Title);
        }

        public void CreateRoom()
        {
            if (string.IsNullOrEmpty(_roomNameInputField?.text))
            {
                Debug.Log("Room name was null/empty! Not creating room!");
                return;
            }

            var selectedMap = RoomManager.GetMap();
            if (string.IsNullOrEmpty(selectedMap))
            {
                Debug.Log("No map selected! Not creating room!");
                return;
            }
            MenuManager.Instance.OpenMenu(MenuType.Loading);
            
            var options = new RoomOptions();
            var customProps = new ExitGames.Client.Photon.Hashtable();
            var matchSettings = RoomManager.GetMatchSettings();
            var matchSettingsJson = JsonUtility.ToJson(matchSettings, false);
            customProps.Add(GameConstants.RoomCustomProperties.MatchSettings, matchSettingsJson);
            var matchMap = Path.GetFileName(selectedMap);
            customProps.Add(GameConstants.RoomCustomProperties.MatchMap, matchMap);
            options.CustomRoomProperties = customProps;
            options.CustomRoomPropertiesForLobby = new[] {GameConstants.RoomCustomProperties.MatchSettings, GameConstants.RoomCustomProperties.MatchMap};
            options.MaxPlayers = matchSettings.MaxPlayers;
            PhotonNetwork.CreateRoom(_roomNameInputField.text, options);
        }

        // Also called on create room, but OnJoinedRoom is called in both cases.
        /*
        public override void OnCreatedRoom()
        {
        }
        */

        public override void OnJoinedRoom()
        {
            _roomNameDisplay.text = $"Room Name: {PhotonNetwork.CurrentRoom.Name}";

            for(var i = _playerListContent.childCount - 1; i >= 0; i--)
                Destroy(_playerListContent.GetChild(i).gameObject);
            var playersInRoom = PhotonNetwork.PlayerList;
            for (var j = 0; j < playersInRoom.Length; j++)
                OnPlayerEnteredRoom(playersInRoom[j]);

            RefreshStartGameButton();

            MenuManager.Instance.OpenMenu(MenuType.Room);
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            RefreshStartGameButton();
        }

        private void RefreshStartGameButton()
        {
            _startGameButton.SetActive(PhotonNetwork.IsMasterClient);
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"Failed to create room w/ returnCode = '{returnCode}', message = {message}");
            _errorDisplay.text = $"Failed to create room: {message}";
            MenuManager.Instance.OpenMenu(MenuType.Error);
        }

        public void LeaveRoom()
        {
            PhotonNetwork.LeaveRoom();
            MenuManager.Instance.OpenMenu(MenuType.Loading);
        }

        public override void OnLeftRoom()
        {
            MenuManager.Instance.OpenMenu(MenuType.Title);
        }

        public void ExitGame()
        {
            // TODO: Something better than this xD
            Application.Quit();
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            for (var i = _roomListContent.childCount - 1; i >= 0; i--)
                Destroy(_roomListContent.GetChild(i).gameObject);

            for (var j = 0; j < roomList.Count; j++)
            {
                if (roomList[j].RemovedFromList)
                    continue;
                var item = Instantiate(_roomListItemPrefab, _roomListContent).GetComponent<RoomListItem>();
                item.SetUp(roomList[j]);
            }
        }
        
        private IEnumerator StartRefreshLevelList()
        {
            LoadOfficialLevelList();
            LoadCustomLevelList();
            RefreshCustomVsLevelList();
            RefreshOfficialVsLevelList();
            yield return null;
        }

        public static void RefreshSelectedMap(string displayText)
        {
            if (Instance == null)
            {
                Debug.LogError("Cannot refresh selected map, Launcher instance is null.");
                return;
            }

            Instance._selectedMapLabel.text = displayText;
        }

        private void LoadOfficialLevelList()
        {
            var officialLevelListJson = Resources.Load(GameConstants.LevelFilePaths.LevelListFilePath).ToString();
            _officialLevelList = JsonUtility.FromJson<GameConstants.OfficialLevelList>(officialLevelListJson);
        }

        private void LoadCustomLevelList()
        {
            var customLevelDir = Path.Join(Application.persistentDataPath, GameConstants.LevelFilePaths.CustomLevelFolder);
            var customVsLevelsDir = Path.Join(customLevelDir, GameConstants.LevelFilePaths.VersusFolder);
            var customCampaignLevelsDir = Path.Join(customLevelDir, GameConstants.LevelFilePaths.CampaignFolder);
            if (!Directory.Exists(customVsLevelsDir))
                Directory.CreateDirectory(customVsLevelsDir);
            if (!Directory.Exists(customCampaignLevelsDir))
                Directory.CreateDirectory(customCampaignLevelsDir);

            _customVsLevels = Directory.GetFiles(customVsLevelsDir).ToList();
            _customCampaignLevels = Directory.GetFiles(customCampaignLevelsDir).ToList();
        }

        public void RefreshOfficialCampaignLevelList()
        {
            RefreshMapLists(_officialLevelList.CampaignLevels, true);
        }

        public void RefreshOfficialVsLevelList()
        {
            RefreshMapLists(_officialLevelList.VsLevels, true);
        }

        public void RefreshCustomCampaignLevelList()
        {
            RefreshMapLists(_customCampaignLevels, false);
        }

        public void RefreshCustomVsLevelList()
        {
            RefreshMapLists(_customVsLevels, false);
        }

        private void RefreshMapLists(List<string> mapList, bool officialLevels)
        {
            for (var i = _mapList.childCount - 1; i >= 0; i--)
                Destroy(_mapList.GetChild(i).gameObject);
            if(!officialLevels)
                for (var i = _mapEditList.childCount - 1; i >= 0; i--)
                    Destroy(_mapEditList.GetChild(i).gameObject);

            for (var i = 0; i < mapList.Count; i++)
            {
                var displayName = Path.GetFileName(mapList[i]);
                var mapSelectItem = Instantiate(_mapSelectPrefab, _mapList).GetComponent<MapSelectItem>();
                mapSelectItem.SetUp(mapList[i], displayName, false, officialLevels);
                if (!officialLevels)
                {
                    var editMapSelectItem = Instantiate(_mapSelectPrefab, _mapEditList).GetComponent<MapSelectItem>();
                    editMapSelectItem.SetUp(mapList[i], displayName, true, officialLevels);
                }
            }
        }

        public void JoinRoom(RoomInfo roomInfo)
        {
            PhotonNetwork.JoinRoom(roomInfo.Name);
            MenuManager.Instance.OpenMenu(MenuType.Loading);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            var item = Instantiate(_playerListItemPrefab, _playerListContent).GetComponent<PlayerListItem>();
            item.SetUp(newPlayer);
        }

        public void OnCancelConfigSettings()
        {
            LoadConfigSettings();
            MenuManager.Instance.OpenMenu(MenuType.Title);
        }

        public void OnSaveConfigSettings()
        {
            var newSettings = new GameConstants.ConfigSettings
            {
                Username = _usernameInput.text,
                InvertXAxisLook = _invertXAxisCheckbox.isOn,
                InvertYAxisLook = _invertYAxisCheckbox.isOn,
                SoundMasterVolume = _soundMasterVolumeSlider.value,
                SoundMusicVolume = _soundMusicVolumeSlider.value,
                SoundSfxVolume = _soundSfxVolumeSlider.value
            };
            GoonsBlastAudioManager.UpdateVolume(newSettings);
            RoomManager.SaveSettings(newSettings);
            PhotonNetwork.NickName = _usernameInput.text;

            MenuManager.Instance.OpenMenu(MenuType.Title);
        }

        public void OnSoundSettingsChanged()
        {
            var newSettings = new GameConstants.ConfigSettings
            {
                SoundMasterVolume = _soundMasterVolumeSlider.value,
                SoundMusicVolume = _soundMusicVolumeSlider.value,
                SoundSfxVolume = _soundSfxVolumeSlider.value
            };

            GoonsBlastAudioManager.UpdateVolume(newSettings);
        }

        public void OnLevelEditorClick()
        {
            RoomManager.ClearMap();
            PhotonNetwork.Disconnect();
        }

        public void OnMatchConfigSaved()
        {
            ReadMatchSettings();
            MenuManager.Instance.OpenMenu(MenuType.CreateRoom);
        }

        private void ReadMatchSettings()
        {
            var newSettings = new GameConstants.MatchSettings
            {
                MatchType = (GameConstants.GameMatchType)_matchTypePicker.value,
                TimerSeconds = (int)_matchTimer.value,
                KillsToWin = (int)_killsToWin.value,
                SuddenDeathType = (GameConstants.SuddenDeathType)_suddenDeathPicker.value,
                SuddenDeathStartsAt = (int)_suddenDeathTimer.value,
                RunBombTimerWhenHeld = _runBombTimerWhenHeldToggle.isOn,
                AllowDetonationsWhenHeld = _detonateBombsWhenHeldToggle.isOn,
                SongNumber = _matchSongPicker.value,
                MaxPlayers = (byte)_maxPlayers.value
            };
            RoomManager.SaveMatchSettings(newSettings);
        }

        public void OnMatchConfigCancelled()
        {
            var oldMatchSettings = RoomManager.GetMatchSettings();
            _matchTypePicker.value = (int) oldMatchSettings.MatchType;
            _matchTimer.value = oldMatchSettings.TimerSeconds;
            _killsToWin.value = oldMatchSettings.KillsToWin;
            _maxPlayers.value = oldMatchSettings.MaxPlayers;

            MenuManager.Instance.OpenMenu(MenuType.CreateRoom);
        }

        public static void OpenEditor()
        {
            PhotonNetwork.Disconnect();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            // TODO: This is a horrible assumption, make this logic better!
            if(cause == DisconnectCause.DisconnectByClientLogic)
                SceneManager.LoadScene((int)GameConstants.LevelIndexes.Editor);
        }

        public void StartGame()
        {
            PhotonNetwork.LoadLevel((int)GameConstants.LevelIndexes.Game);
        }
    }
}
